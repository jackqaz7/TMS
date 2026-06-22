namespace AuditService.Models
{
    public class AuditEvent
    {
        // Database identity key used for fast local lookup and ordering.
        public long AuditEventId { get; set; }

        // Producer-generated event id. This supports idempotency when CoreAPI,
        // Kafka, or a retry policy accidentally sends the same event more than once.
        public Guid EventId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? ActionBy { get; set; }
        public string SourceSystem { get; set; } = string.Empty;

        // OccurredAtUtc is when the business event happened; CreatedAtUtc is when
        // this service persisted it. They can differ if retries or queues are added.
        public DateTime OccurredAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string? Summary { get; set; }

        // PayloadJson keeps event-specific details without forcing one giant table
        // with nullable columns for every possible business event.
        public string? PayloadJson { get; set; }
    }
}
