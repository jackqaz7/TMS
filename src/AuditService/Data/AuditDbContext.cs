using AuditService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Data
{
    public class AuditDbContext : DbContext
    {
        public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
        {
        }

        // AuditEvents is append-only for now. The service writes facts about what
        // happened; the core TMS app should not update these rows directly.
        public DbSet<AuditEvent> AuditEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditEvent>(entity =>
            {
                // AuditEventId is the local database identity key. EventId is the
                // business/event id sent by producers so duplicate events can be detected.
                entity.HasKey(e => e.AuditEventId);
                entity.HasIndex(e => e.EventId).IsUnique();

                // These indexes match the first questions we will ask of audit data:
                // "show this event type", "show this entity", and "show latest events".
                entity.HasIndex(e => new { e.EventType, e.OccurredAtUtc });
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
                entity.HasIndex(e => e.OccurredAtUtc);

                // Max lengths mirror the SQL table design and prevent accidental
                // oversized labels from becoming unbounded nvarchar columns.
                entity.Property(e => e.EventType).HasMaxLength(100);
                entity.Property(e => e.EntityType).HasMaxLength(100);
                entity.Property(e => e.EntityId).HasMaxLength(100);
                entity.Property(e => e.ActionBy).HasMaxLength(150);
                entity.Property(e => e.SourceSystem).HasMaxLength(100);
                entity.Property(e => e.Summary).HasMaxLength(500);
            });
        }
    }
}
