using CoreAPI.Models;
using CoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/audit-log-test")]
    public class AuditLogTestController : ControllerBase
    {
        private const int EventsPerClick = 50;
        private readonly IAuditClient _auditClient;

        public AuditLogTestController(IAuditClient auditClient)
        {
            _auditClient = auditClient;
        }

        [HttpPost("button-click")]
        public async Task<IActionResult> CreateLogButtonEvents(CancellationToken cancellationToken)
        {
            var batchId = Guid.NewGuid();
            var username = GetCurrentUsername();
            var sw = Stopwatch.StartNew();

            for (var i = 1; i <= EventsPerClick; i++)
            {
                // One UI click deliberately produces many audit events so you can
                // observe queue pressure while the background consumer drains them.
                await _auditClient.RecordAsync(new AuditEventRequest
                {
                    EventType = "LogButtonClicked",
                    EntityType = "AuditQueueDemo",
                    EntityId = batchId.ToString(),
                    ActionBy = username,
                    Summary = $"Log button generated event {i} of {EventsPerClick}.",
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        BatchId = batchId,
                        SequenceNumber = i,
                        EventsPerClick,
                        TriggeredBy = username
                    })
                }, cancellationToken);
            }

            sw.Stop();

            return Ok(new
            {
                BatchId = batchId,
                EventsQueued = EventsPerClick,
                EnqueueElapsedMilliseconds = sw.ElapsedMilliseconds,
                BackpressureHint = "If this number grows, the bounded queue is slowing producers while the consumer catches up."
            });
        }

        private string GetCurrentUsername()
        {
            return User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.Identity?.Name
                ?? "system";
        }
    }
}
