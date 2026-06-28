using System.Net.Http.Json;
using CoreAPI.Models;

namespace CoreAPI.Services
{
    public class AuditEventConsumer : BackgroundService
    {
        private const int MaxDeliveryAttempts = 3;

        private readonly IAuditEventQueue _queue;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuditDeadLetterWriter _deadLetterWriter;
        private readonly ILogger<AuditEventConsumer> _logger;

        public AuditEventConsumer(
            IAuditEventQueue queue,
            IHttpClientFactory httpClientFactory,
            IAuditDeadLetterWriter deadLetterWriter,
            ILogger<AuditEventConsumer> logger)
        {
            _queue = queue;
            _httpClientFactory = httpClientFactory;
            _deadLetterWriter = deadLetterWriter;
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

                await SendWithRetryAsync(auditEvent, stoppingToken);
            }
        }

        private async Task SendWithRetryAsync(
            AuditEventRequest auditEvent,
            CancellationToken cancellationToken)
        {
            string lastFailureReason = "Unknown failure.";

            for (var attempt = 1; attempt <= MaxDeliveryAttempts; attempt++)
            {
                try
                {
                    var failureReason = await TrySendToAuditServiceAsync(
                        auditEvent,
                        cancellationToken);

                    if (failureReason == null)
                    {
                        return;
                    }

                    lastFailureReason = failureReason;
                    _logger.LogWarning(
                        "Audit event {EventId} delivery attempt {Attempt}/{MaxAttempts} failed. Reason: {FailureReason}",
                        auditEvent.EventId,
                        attempt,
                        MaxDeliveryAttempts,
                        lastFailureReason);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // App shutdown is expected to cancel the background consumer.
                    return;
                }
                catch (Exception ex)
                {
                    lastFailureReason = ex.Message;
                    _logger.LogWarning(
                        ex,
                        "Audit event {EventId} delivery attempt {Attempt}/{MaxAttempts} threw an exception.",
                        auditEvent.EventId,
                        attempt,
                        MaxDeliveryAttempts);
                }

                if (attempt < MaxDeliveryAttempts)
                {
                    await Task.Delay(GetRetryDelay(attempt), cancellationToken);
                }
            }

            await _deadLetterWriter.WriteAsync(
                auditEvent,
                lastFailureReason,
                MaxDeliveryAttempts,
                cancellationToken);
        }

        private async Task<string?> TrySendToAuditServiceAsync(
            AuditEventRequest auditEvent,
            CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient("AuditService");
            using var response = await client.PostAsJsonAsync(
                "api/audit-events",
                auditEvent,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(responseBody)
                ? $"Audit Service returned HTTP {(int)response.StatusCode} {response.ReasonPhrase}."
                : $"Audit Service returned HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {responseBody}";
        }

        private static TimeSpan GetRetryDelay(int attempt)
        {
            return TimeSpan.FromSeconds(attempt);
        }
    }
}
