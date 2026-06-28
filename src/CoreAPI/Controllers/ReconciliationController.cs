using CoreAPI.Models;
using CoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("api/reconciliation")]
    [Authorize]
    public class ReconciliationController : ControllerBase
    {
        private readonly IReconciliationBatchService _reconciliationBatchService;

        public ReconciliationController(IReconciliationBatchService reconciliationBatchService)
        {
            _reconciliationBatchService = reconciliationBatchService;
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

            return Ok(result);
        }
    }
}
