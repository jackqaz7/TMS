using System.Net.Http.Json;
using CoreAPI.Models;

namespace CoreAPI.Services
{
    public class AuditClient : IAuditClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuditClient> _logger;

        public AuditClient(HttpClient httpClient, ILogger<AuditClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task RecordAsync(AuditEventRequest auditEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                // Audit is intentionally non-blocking for the business workflow:
                // a trade save should not be rolled back just because the audit
                // microservice is temporarily offline.
                var response = await _httpClient.PostAsJsonAsync("api/audit-events", auditEvent, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Audit Service returned {StatusCode} for event {EventType}",
                        response.StatusCode,
                        auditEvent.EventType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit Service call failed for event {EventType}", auditEvent.EventType);
            }
        }
    }
}
