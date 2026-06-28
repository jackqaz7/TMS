using CoreAPI.Data;
using CoreAPI.Models;
using CoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuditClient _auditClient;

        public UsersController(
            AuthDbContext context,
            IConfiguration configuration,
            IAuditClient auditClient)
        {
            _context = context;
            _configuration = configuration;
            _auditClient = auditClient;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var sw = Stopwatch.StartNew();
            // This is the first EF Core query in the login flow. It asks SQL Server for
            // one user row matching the supplied username.
            var user = _context.AppUsers.SingleOrDefault(u => u.Username == loginDto.Username);

            Console.WriteLine($"Login DB query took {sw.ElapsedMilliseconds} ms");

            sw.Restart();

            // Learning note: this project currently compares plain text for simplicity.
            // Production systems should store salted password hashes and verify via a
            // password hasher, never compare raw passwords.
            if (user == null || user.PasswordHash != loginDto.Password)
            {
                await _auditClient.RecordAsync(new AuditEventRequest
                {
                    EventType = "LoginFailed",
                    EntityType = "User",
                    EntityId = loginDto.Username,
                    ActionBy = loginDto.Username,
                    Summary = $"Login failed for user {loginDto.Username}.",
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        loginDto.Username,
                        Reason = "Invalid username or password"
                    })
                });

                return Unauthorized("Invalid username or password");
            }

            // Claims are facts about the authenticated user that travel inside the JWT.
            // Later we can add role/permission claims for authorization rules.
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            // The signing key proves the token was created by our API. The same key is
            // used by JWT middleware in Program.cs to validate future requests.
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            Console.WriteLine($"Token creation took {sw.ElapsedMilliseconds} ms");

            await _auditClient.RecordAsync(new AuditEventRequest
            {
                EventType = "LoginSucceeded",
                EntityType = "User",
                EntityId = user.Username,
                ActionBy = user.Username,
                Summary = $"Login succeeded for user {user.Username}.",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    user.Id,
                    user.Username
                })
            });

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserAdminResponse>>> GetUsers()
        {
            // This reads from the Auth database configured as TMSAuth/Auth in appsettings.
            // The response deliberately hides PasswordHash so the UI never displays secrets.
            var users = await _context.AppUsers
                .AsNoTracking()
                .OrderBy(u => u.Username)
                .Select(u => new UserAdminResponse
                {
                    Id = u.Id,
                    Username = u.Username,
                    Role = u.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<UserAdminResponse>> CreateUser([FromBody] CreateUserAdminRequest request)
        {
            var username = request.Username.Trim();
            var role = request.Role.Trim();

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(role))
            {
                return BadRequest(new { Message = "Username, password, and role are required." });
            }

            var exists = await _context.AppUsers.AnyAsync(u => u.Username == username);

            if (exists)
            {
                return Conflict(new { Message = $"User '{username}' already exists." });
            }

            var user = new User
            {
                Username = username,
                // Learning note: the existing login flow currently compares this value
                // directly. Later we should replace this with real password hashing.
                PasswordHash = request.Password,
                Role = role
            };

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();

            await _auditClient.RecordAsync(new AuditEventRequest
            {
                EventType = "UserCreated",
                EntityType = "User",
                EntityId = user.Id.ToString(),
                ActionBy = GetCurrentUsername(),
                Summary = $"User {user.Username} was created with role {user.Role}.",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    user.Id,
                    user.Username,
                    user.Role
                })
            });

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ToAdminResponse(user));
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserAdminResponse>> GetUser(int id)
        {
            var user = await _context.AppUsers
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(ToAdminResponse(user));
        }

        [Authorize]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<UserAdminResponse>> UpdateUser(
            int id,
            [FromBody] UpdateUserAdminRequest request)
        {
            var user = await _context.AppUsers.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var username = request.Username.Trim();
            var role = request.Role.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(role))
            {
                return BadRequest(new { Message = "Username and role are required." });
            }

            var usernameTaken = await _context.AppUsers
                .AnyAsync(u => u.Id != id && u.Username == username);

            if (usernameTaken)
            {
                return Conflict(new { Message = $"User '{username}' already exists." });
            }

            var oldUsername = user.Username;
            var oldRole = user.Role;
            var passwordChanged = !string.IsNullOrWhiteSpace(request.Password);

            user.Username = username;
            user.Role = role;

            // Blank password means "leave current password unchanged" from the edit screen.
            if (passwordChanged)
            {
                user.PasswordHash = request.Password;
            }

            await _context.SaveChangesAsync();

            await _auditClient.RecordAsync(new AuditEventRequest
            {
                EventType = "UserUpdated",
                EntityType = "User",
                EntityId = user.Id.ToString(),
                ActionBy = GetCurrentUsername(),
                Summary = $"User {user.Username} was updated.",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    user.Id,
                    OldUsername = oldUsername,
                    NewUsername = user.Username,
                    OldRole = oldRole,
                    NewRole = user.Role,
                    PasswordChanged = passwordChanged
                })
            });

            return Ok(ToAdminResponse(user));
        }

        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var deletedUserPayload = new
            {
                user.Id,
                user.Username,
                user.Role
            };

            _context.AppUsers.Remove(user);
            await _context.SaveChangesAsync();

            await _auditClient.RecordAsync(new AuditEventRequest
            {
                EventType = "UserDeleted",
                EntityType = "User",
                EntityId = id.ToString(),
                ActionBy = GetCurrentUsername(),
                Summary = $"User {deletedUserPayload.Username} was deleted.",
                PayloadJson = JsonSerializer.Serialize(deletedUserPayload)
            });

            return NoContent();
        }

        private static UserAdminResponse ToAdminResponse(User user)
        {
            return new UserAdminResponse
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role
            };
        }

        private string GetCurrentUsername()
        {
            // Admin actions are authenticated by JWT. The subject claim is the username
            // created during login, and becomes the audit "who did this" value.
            return User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.Identity?.Name
                ?? "system";
        }
    }
}
