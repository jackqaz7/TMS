using CoreAPI.Data;
using CoreAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly IConfiguration _configuration;

        public UsersController(AuthDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto loginDto)
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
                return Unauthorized("Invalid username or password");
            }

            // Claims are facts about the authenticated user that travel inside the JWT.
            // Later we can add role/permission claims for authorization rules.
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, user.Id.ToString())
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

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }
    }
}
