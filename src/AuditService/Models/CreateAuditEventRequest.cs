using System.ComponentModel.DataAnnotations;

namespace AuditService.Models
{
    public class CreateAuditEventRequest
    {
        // Optional so simple callers can let AuditService generate it. Later,
        // message producers should send this for reliable retries/idempotency.
        public Guid? EventId { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EntityType { get; set; }

        [MaxLength(100)]
        public string? EntityId { get; set; }

        [MaxLength(150)]
        public string? ActionBy { get; set; }

        [Required]
        [MaxLength(100)]
        public string SourceSystem { get; set; } = string.Empty;

        // Optional because the service can timestamp the event when received.
        // CoreAPI sends it when it wants to preserve the original business time.
        public DateTime? OccurredAtUtc { get; set; }

        [MaxLength(500)]
        public string? Summary { get; set; }

        // Raw JSON keeps the REST contract flexible while the domain is still evolving.
        public string? PayloadJson { get; set; }
    }
}
