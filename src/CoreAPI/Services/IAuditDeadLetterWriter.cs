using CoreAPI.Models;

namespace CoreAPI.Services
{
    public interface IAuditDeadLetterWriter
    {
        Task WriteAsync(
            AuditEventRequest auditEvent,
            string failureReason,
            int attemptCount,
            CancellationToken cancellationToken = default);
    }
}
