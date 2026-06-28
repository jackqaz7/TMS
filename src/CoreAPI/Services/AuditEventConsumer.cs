using System.Net.Http.Json;
using CoreAPI.Models;

namespace CoreAPI.Services
{
    public class AuditEventConsumer : BackgroundService
    {
        private readonly IAuditEventQueue _queue;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AuditEventConsumer> _logger;

        public AuditEventConsumer(
            IAuditEventQueue queue,
            IHttpClientFactory httpClientFactory,
            ILogger<AuditEventConsumer> logger)
        {
            _queue = queue;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                AuditEventRequest auditEvent;

                try
                {
                    auditEvent = await _queue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                await SendToAuditServiceAsync(auditEvent, stoppingToken);
            }
        }

        private async Task SendToAuditServiceAsync(
            AuditEventRequest auditEvent,
            CancellationToken cancellationToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("AuditService");
                var response = await client.PostAsJsonAsync("api/audit-events", auditEvent, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Audit Service returned {StatusCode} for event {EventType}",
                        response.StatusCode,
                        auditEvent.EventType);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // App shutdown is expected to cancel the background consumer.
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit Service call failed for event {EventType}", auditEvent.EventType);
            }
        }
    }
}
