using CoreAPI.Models;

namespace CoreAPI.Services
{
    public class AuditClient : IAuditClient
    {
        private readonly IAuditEventQueue _queue;
        private readonly ILogger<AuditClient> _logger;

        public AuditClient(IAuditEventQueue queue, ILogger<AuditClient> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        public async Task RecordAsync(AuditEventRequest auditEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                // Producer side of the producer/consumer pattern. API workflows only
                // enqueue audit facts; the background consumer performs the HTTP call.
                await _queue.QueueAsync(auditEvent, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit event enqueue failed for event {EventType}", auditEvent.EventType);
            }
        }
    }
}
