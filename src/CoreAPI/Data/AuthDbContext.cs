using Microsoft.EntityFrameworkCore;
using CoreAPI.Models;

namespace CoreAPI.Data
{
    public class AuthDbContext : DbContext    {
        public DbSet<User> AppUsers { get; set; }
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }
    }
}
