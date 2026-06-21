using CoreAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CoreAPI.Data
{
    public class TmsDbContext : DbContext
    {
        public TmsDbContext(DbContextOptions<TmsDbContext> options) : base(options)
        {
        }

        public DbSet<Trade> Trades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Trade>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasIndex(t => t.TradeReference).IsUnique();

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
