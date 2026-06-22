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

        // DbSet<FxRate> represents dbo.FxRates. The Create Trade UI uses this
        // through the API to auto-calculate Amount2 from Amount1.
        public DbSet<FxRate> FxRates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Trade>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasIndex(t => t.TradeReference).IsUnique();

                entity.Property(t => t.TradeReference).HasMaxLength(40);
                entity.Property(t => t.TradeType).HasMaxLength(20);
                entity.Property(t => t.Counterparty).HasMaxLength(120);
                entity.Property(t => t.CounterpartyBankAccount).HasMaxLength(50);
                entity.Property(t => t.Currency1).HasMaxLength(3).IsFixedLength();
                entity.Property(t => t.Currency2).HasMaxLength(3).IsFixedLength();
                entity.Property(t => t.Side).HasMaxLength(4);
                entity.Property(t => t.Comments).HasMaxLength(500);
                entity.Property(t => t.CreatedBy).HasMaxLength(100);
                entity.Property(t => t.EditedBy).HasMaxLength(100);

                entity.Property(t => t.Amount1).HasColumnType("decimal(18, 2)");
                entity.Property(t => t.Amount2).HasColumnType("decimal(18, 2)");
                entity.Property(t => t.FxRateUsed).HasColumnType("decimal(18, 8)");
                entity.Property(t => t.Fees).HasColumnType("decimal(18, 2)");
                entity.Property(t => t.NearLegRate).HasColumnType("decimal(18, 8)");
                entity.Property(t => t.NearLegAmount1).HasColumnType("decimal(18, 2)");
                entity.Property(t => t.NearLegAmount2).HasColumnType("decimal(18, 2)");
                entity.Property(t => t.FarLegRate).HasColumnType("decimal(18, 8)");
                entity.Property(t => t.FarLegAmount1).HasColumnType("decimal(18, 2)");
                entity.Property(t => t.FarLegAmount2).HasColumnType("decimal(18, 2)");
                entity.Property(t => t.SwapPoints).HasColumnType("decimal(18, 8)");
            });

            modelBuilder.Entity<FxRate>(entity =>
            {
                entity.HasKey(r => r.Id);

                // This index supports the common lookup pattern:
                // latest rate for FromCurrency + ToCurrency.
                entity.HasIndex(r => new { r.FromCurrency, r.ToCurrency, r.RateDate });

                entity.Property(r => r.FromCurrency).HasMaxLength(3).IsFixedLength();
                entity.Property(r => r.ToCurrency).HasMaxLength(3).IsFixedLength();
                entity.Property(r => r.Rate).HasColumnType("decimal(18, 8)");
                entity.Property(r => r.Source).HasMaxLength(50);
            });
        }
    }
}
