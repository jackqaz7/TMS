using CoreAPI.Models;

namespace CoreAPI.Services
{
    public interface IAuditEventQueue
    {
        ValueTask QueueAsync(AuditEventRequest auditEvent, CancellationToken cancellationToken = default);
        ValueTask<AuditEventRequest> DequeueAsync(CancellationToken cancellationToken);
    }
}
