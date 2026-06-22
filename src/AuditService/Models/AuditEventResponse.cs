namespace AuditService.Models
{
    public class AuditEventResponse
    {
        public long AuditEventId { get; set; }
        public Guid EventId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? ActionBy { get; set; }
        public string SourceSystem { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string? Summary { get; set; }
        public string? PayloadJson { get; set; }
    }
}
