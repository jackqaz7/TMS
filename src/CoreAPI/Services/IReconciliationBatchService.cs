using CoreAPI.Models;

namespace CoreAPI.Services
{
    public interface IReconciliationBatchService
    {
        Task<ReconciliationBatchResponse> RunBatchAsync(
            ReconciliationBatchRequest request,
            CancellationToken cancellationToken = default);
    }
}
