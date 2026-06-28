using CoreAPI.Models;
using CoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("api/reconciliation")]
    [Authorize]
    public class ReconciliationController : ControllerBase
    {
        private readonly IReconciliationBatchService _reconciliationBatchService;
        private readonly IHubContext<NotificationsHub> _notificationsHub;

        public ReconciliationController(
            IReconciliationBatchService reconciliationBatchService,
            IHubContext<NotificationsHub> notificationsHub)
        {
            _reconciliationBatchService = reconciliationBatchService;
            _notificationsHub = notificationsHub;
        }

        [HttpPost("batches")]
        public async Task<ActionResult<ReconciliationBatchResponse>> RunBatch(
            [FromBody] ReconciliationBatchRequest request,
            CancellationToken cancellationToken)
        {
            if (request.ToSettlementDate.HasValue &&
                request.FromSettlementDate.HasValue &&
                request.ToSettlementDate.Value.Date < request.FromSettlementDate.Value.Date)
            {
                return BadRequest(new { Message = "To settlement date cannot be before from settlement date." });
            }

            var result = await _reconciliationBatchService.RunBatchAsync(request, cancellationToken);

            // SignalR push notification: any connected desktop shell can update its
            // global status even if the user navigated away from Reconciliation.
            await _notificationsHub.Clients.All.SendAsync(
                "ReconciliationCompleted",
                new ReconciliationCompletedNotification
                {
                    BatchId = result.BatchId,
                    MatchedGroupCount = result.MatchedGroupCount,
                    BreakGroupCount = result.BreakGroupCount,
                    ElapsedMilliseconds = result.ElapsedMilliseconds
                },
                cancellationToken);

            return Ok(result);
        }
    }
}
