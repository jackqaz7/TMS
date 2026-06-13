using Microsoft.AspNetCore.Mvc;
using CoreAPI.Models;
using CoreAPI.Data;
using CoreAPI.Filters;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController: ControllerBase   
    {
        private readonly AuthDbContext _context;

        public UsersController(AuthDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto loginDto)
        {
            var user = _context.AppUsers.SingleOrDefault(u => u.Username == loginDto.username);

            if (user == null)
            {
                return Unauthorized("Invalid username or password");
            }
            if (user.PasswordHash != loginDto.password)
            {
                return Unauthorized("Invalid username or password");
            }

            return Ok(new { message = "Login successful" });
        }

    }
}
