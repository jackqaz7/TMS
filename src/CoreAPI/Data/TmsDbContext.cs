using CoreAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CoreAPI.Data
{
    public class TmsDbContext : DbContext
    {
        public TmsDbContext(DbContextOptions<TmsDbContext> options) : base(options)
        {
        }

        // DbSet<Trade> is the EF Core representation of the Trades table.
        // Add/Find/LINQ operations here are translated into SQL by EF Core.
        public DbSet<Trade> Trades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Trade>(entity =>
            {
                entity.HasKey(t => t.Id);

                // TradeReference is unique because a real treasury trade should have one
                // external/business identifier that prevents duplicate capture.
                entity.HasIndex(t => t.TradeReference).IsUnique();

                // Length and decimal precision are part of the database contract. Keeping
                // them explicit avoids EF/Core SQL Server choosing broad defaults.
                entity.Property(t => t.TradeReference).HasMaxLength(40);
                entity.Property(t => t.Counterparty).HasMaxLength(120);
                entity.Property(t => t.Instrument).HasMaxLength(40);
                entity.Property(t => t.Currency).HasMaxLength(3);
                entity.Property(t => t.Side).HasMaxLength(4);
                entity.Property(t => t.Notional).HasColumnType("decimal(18, 2)");
                entity.Property(t => t.Rate).HasColumnType("decimal(18, 6)");
            });
        }
    }
}
