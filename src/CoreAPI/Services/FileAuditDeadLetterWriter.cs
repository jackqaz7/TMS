using System.Text.Json;
using CoreAPI.Models;

namespace CoreAPI.Services
{
    public class FileAuditDeadLetterWriter : IAuditDeadLetterWriter
    {
        private static readonly SemaphoreSlim FileLock = new(1, 1);
        private readonly string _deadLetterFilePath;
        private readonly ILogger<FileAuditDeadLetterWriter> _logger;

        public FileAuditDeadLetterWriter(
            IWebHostEnvironment environment,
            ILogger<FileAuditDeadLetterWriter> logger)
        {
            _logger = logger;
            _deadLetterFilePath = Path.Combine(
                environment.ContentRootPath,
                "Logs",
                "audit-dead-letter.ndjson");
        }

        public async Task WriteAsync(
            AuditEventRequest auditEvent,
            string failureReason,
            int attemptCount,
            CancellationToken cancellationToken = default)
        {
            var directory = Path.GetDirectoryName(_deadLetterFilePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var deadLetterRecord = new
            {
                DeadLetteredAtUtc = DateTime.UtcNow,
                AttemptCount = attemptCount,
                FailureReason = failureReason,
                AuditEvent = auditEvent
            };

            var line = JsonSerializer.Serialize(deadLetterRecord);

            // File appends are protected so future multiple consumers do not interleave
            // JSON lines. This is the "dead letter" fallback for failed audit delivery.
            await FileLock.WaitAsync(cancellationToken);
            try
            {
                await File.AppendAllTextAsync(
                    _deadLetterFilePath,
                    line + Environment.NewLine,
                    cancellationToken);
            }
            finally
            {
                FileLock.Release();
            }

            _logger.LogError(
                "Audit event {EventId} dead-lettered after {AttemptCount} attempt(s). File: {DeadLetterFilePath}. Reason: {FailureReason}",
                auditEvent.EventId,
                attemptCount,
                _deadLetterFilePath,
                failureReason);
        }
    }
}
