namespace CoreAPI.Models
{
    public class AuditEventRequest
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public string EventType { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? ActionBy { get; set; }
        public string SourceSystem { get; set; } = "TMS.CoreAPI";
        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
        public string? Summary { get; set; }
        public string? PayloadJson { get; set; }
    }
}
