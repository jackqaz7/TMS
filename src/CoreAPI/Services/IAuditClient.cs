using CoreAPI.Models;

namespace CoreAPI.Services
{
    public interface IAuditClient
    {
        Task RecordAsync(AuditEventRequest auditEvent, CancellationToken cancellationToken = default);
    }
}
