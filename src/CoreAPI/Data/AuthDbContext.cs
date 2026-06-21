using Microsoft.EntityFrameworkCore;
using CoreAPI.Models;

namespace CoreAPI.Data
{
    public class AuthDbContext : DbContext
    {
        // DbSet<User> represents the AppUsers table. LINQ queries against this property
        // become SQL queries against the authentication database.
        public DbSet<User> AppUsers { get; set; }

        // DbContextOptions is supplied by dependency injection from Program.cs. It contains
        // provider details such as SQL Server and the Auth connection string.
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }
    }
}
