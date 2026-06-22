using AuditService.Data;
using AuditService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Controllers
{
    [ApiController]
    [Route("api/audit-events")]
    public class AuditEventsController : ControllerBase
    {
        private readonly AuditDbContext _auditDbContext;

        public AuditEventsController(AuditDbContext auditDbContext)
        {
            _auditDbContext = auditDbContext;
        }

        [HttpPost]
        public async Task<ActionResult<AuditEventResponse>> CreateAuditEvent([FromBody] CreateAuditEventRequest request)
        {
            var now = DateTime.UtcNow;

            // The controller maps the external request DTO into the database entity.
            // That keeps persistence fields such as CreatedAtUtc controlled by this service.
            var auditEvent = new AuditEvent
            {
                EventId = request.EventId ?? Guid.NewGuid(),
                EventType = request.EventType.Trim(),
                EntityType = request.EntityType?.Trim(),
                EntityId = request.EntityId?.Trim(),
                ActionBy = request.ActionBy?.Trim(),
                SourceSystem = request.SourceSystem.Trim(),
                OccurredAtUtc = request.OccurredAtUtc ?? now,
                CreatedAtUtc = now,
                Summary = request.Summary?.Trim(),
                PayloadJson = request.PayloadJson
            };

            _auditDbContext.AuditEvents.Add(auditEvent);
            await _auditDbContext.SaveChangesAsync();

            // CreatedAtAction returns HTTP 201 plus a Location header pointing to
            // GET /api/audit-events/{id}, which is useful when testing from Swagger.
            return CreatedAtAction(nameof(GetAuditEvent), new { id = auditEvent.AuditEventId }, ToResponse(auditEvent));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditEventResponse>>> GetAuditEvents([FromQuery] int take = 100)
        {
            // Clamp prevents someone from accidentally asking for millions of rows
            // from an audit table that will grow quickly over time.
            var safeTake = Math.Clamp(take, 1, 500);

            var events = await _auditDbContext.AuditEvents
                // Read-only audit queries do not need EF change tracking.
                .AsNoTracking()
                .OrderByDescending(e => e.OccurredAtUtc)
                .ThenByDescending(e => e.AuditEventId)
                .Take(safeTake)
                .Select(e => ToResponse(e))
                .ToListAsync();

            return Ok(events);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<AuditEventResponse>> GetAuditEvent(long id)
        {
            var auditEvent = await _auditDbContext.AuditEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.AuditEventId == id);

            if (auditEvent == null)
            {
                return NotFound();
            }

            return Ok(ToResponse(auditEvent));
        }

        private static AuditEventResponse ToResponse(AuditEvent auditEvent)
        {
            // Returning a response DTO avoids exposing EF entities directly as the
            // public API contract. That gives us room to change persistence later.
            return new AuditEventResponse
            {
                AuditEventId = auditEvent.AuditEventId,
                EventId = auditEvent.EventId,
                EventType = auditEvent.EventType,
                EntityType = auditEvent.EntityType,
                EntityId = auditEvent.EntityId,
                ActionBy = auditEvent.ActionBy,
                SourceSystem = auditEvent.SourceSystem,
                OccurredAtUtc = auditEvent.OccurredAtUtc,
                CreatedAtUtc = auditEvent.CreatedAtUtc,
                Summary = auditEvent.Summary,
                PayloadJson = auditEvent.PayloadJson
            };
        }
    }
}
