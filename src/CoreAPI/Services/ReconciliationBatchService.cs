using System.Collections.Concurrent;
using System.Diagnostics;
using CoreAPI.Data;
using CoreAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CoreAPI.Services
{
    public class ReconciliationBatchService : IReconciliationBatchService
    {
        private const int MinimumBatchSize = 1;
        private const int MaximumBatchSize = 500;
        private const int MinimumDegreeOfParallelism = 1;
        private const int MaximumDegreeOfParallelism = 16;

        private readonly TmsDbContext _tmsDbContext;

        public ReconciliationBatchService(TmsDbContext tmsDbContext)
        {
            _tmsDbContext = tmsDbContext;
        }

        public async Task<ReconciliationBatchResponse> RunBatchAsync(
            ReconciliationBatchRequest request,
            CancellationToken cancellationToken = default)
        {
            var batchId = Guid.NewGuid();
            var stopwatch = Stopwatch.StartNew();
            var batchSize = Clamp(request.BatchSize, MinimumBatchSize, MaximumBatchSize);
            var maxDegreeOfParallelism = Clamp(
                request.MaxDegreeOfParallelism,
                MinimumDegreeOfParallelism,
                MaximumDegreeOfParallelism);
            var tolerance = request.Tolerance < 0 ? 0 : request.Tolerance;

            var trades = await QueryTradeSnapshots(request, cancellationToken);
            var ledgerEntries = NormalizeLedgerEntries(request.LedgerEntries);

            var tradeGroups = trades
                .GroupBy(t => new ReconciliationGroupKey(t.Currency, t.SettlementDate.Date))
                .ToDictionary(g => g.Key, g => g.ToList());

            var ledgerGroups = ledgerEntries
                .GroupBy(l => new ReconciliationGroupKey(l.Currency, l.SettlementDate.Date))
                .ToDictionary(g => g.Key, g => g.ToList());

            var groupKeys = tradeGroups.Keys
                .Union(ledgerGroups.Keys)
                .OrderBy(k => k.Currency)
                .ThenBy(k => k.SettlementDate)
                .ToArray();

            var results = new ConcurrentBag<ReconciliationGroupResult>();
            var workerThreadIds = new ConcurrentDictionary<int, byte>();
            var batches = groupKeys.Chunk(batchSize);

            await Parallel.ForEachAsync(
                batches,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = maxDegreeOfParallelism,
                    CancellationToken = cancellationToken
                },
                (batch, token) =>
                {
                    foreach (var key in batch)
                    {
                        token.ThrowIfCancellationRequested();

                        tradeGroups.TryGetValue(key, out var groupTrades);
                        ledgerGroups.TryGetValue(key, out var groupLedgerEntries);

                        var result = ReconcileGroup(
                            key,
                            groupTrades ?? new List<ReconciliationTradeSnapshot>(),
                            groupLedgerEntries ?? new List<ReconciliationLedgerEntry>(),
                            tolerance);

                        workerThreadIds.TryAdd(result.WorkerThreadId, 0);
                        results.Add(result);
                    }

                    return ValueTask.CompletedTask;
                });

            var orderedResults = results
                .OrderBy(r => r.Currency)
                .ThenBy(r => r.SettlementDate)
                .ToList();

            stopwatch.Stop();

            return new ReconciliationBatchResponse
            {
                BatchId = batchId,
                TradeCount = trades.Count,
                LedgerEntryCount = ledgerEntries.Count,
                GroupCount = orderedResults.Count,
                MatchedGroupCount = orderedResults.Count(r => r.IsMatched),
                BreakGroupCount = orderedResults.Count(r => !r.IsMatched),
                BatchSize = batchSize,
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                WorkerThreadIds = workerThreadIds.Keys.OrderBy(id => id).ToList(),
                Results = orderedResults
            };
        }

        private async Task<List<ReconciliationTradeSnapshot>> QueryTradeSnapshots(
            ReconciliationBatchRequest request,
            CancellationToken cancellationToken)
        {
            var query = _tmsDbContext.Trades.AsNoTracking();

            if (request.FromSettlementDate.HasValue)
            {
                var fromDate = request.FromSettlementDate.Value.Date;
                query = query.Where(t => t.SettlementDate.Date >= fromDate);
            }

            if (request.ToSettlementDate.HasValue)
            {
                var toDate = request.ToSettlementDate.Value.Date;
                query = query.Where(t => t.SettlementDate.Date <= toDate);
            }

            return await query
                .Select(t => new ReconciliationTradeSnapshot(
                    t.Id,
                    t.TradeReference,
                    t.Currency1,
                    t.Side,
                    t.Amount1,
                    t.SettlementDate))
                .ToListAsync(cancellationToken);
        }

        private static List<ReconciliationLedgerEntry> NormalizeLedgerEntries(
            IEnumerable<ReconciliationLedgerEntry> ledgerEntries)
        {
            return ledgerEntries
                .Where(l => !string.IsNullOrWhiteSpace(l.Currency) && !string.IsNullOrWhiteSpace(l.Side))
                .Select(l => new ReconciliationLedgerEntry
                {
                    ExternalReference = l.ExternalReference.Trim(),
                    Currency = l.Currency.Trim().ToUpperInvariant(),
                    Side = l.Side.Trim().ToUpperInvariant(),
                    Amount = l.Amount,
                    SettlementDate = l.SettlementDate.Date
                })
                .ToList();
        }

        private static ReconciliationGroupResult ReconcileGroup(
            ReconciliationGroupKey key,
            IReadOnlyCollection<ReconciliationTradeSnapshot> trades,
            IReadOnlyCollection<ReconciliationLedgerEntry> ledgerEntries,
            decimal tolerance)
        {
            var expectedBuy = trades.Where(t => t.Side == "BUY").Sum(t => t.Amount);
            var expectedSell = trades.Where(t => t.Side == "SELL").Sum(t => t.Amount);
            var actualBuy = ledgerEntries.Where(l => l.Side == "BUY").Sum(l => l.Amount);
            var actualSell = ledgerEntries.Where(l => l.Side == "SELL").Sum(l => l.Amount);
            var buyBreak = actualBuy - expectedBuy;
            var sellBreak = actualSell - expectedSell;

            return new ReconciliationGroupResult
            {
                Currency = key.Currency,
                SettlementDate = key.SettlementDate,
                ExpectedBuyAmount = expectedBuy,
                ActualBuyAmount = actualBuy,
                BuyBreakAmount = buyBreak,
                ExpectedSellAmount = expectedSell,
                ActualSellAmount = actualSell,
                SellBreakAmount = sellBreak,
                IsMatched = Math.Abs(buyBreak) <= tolerance && Math.Abs(sellBreak) <= tolerance,
                TradeCount = trades.Count,
                LedgerEntryCount = ledgerEntries.Count,
                WorkerThreadId = Environment.CurrentManagedThreadId
            };
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        private sealed record ReconciliationGroupKey(string Currency, DateTime SettlementDate);

        private sealed record ReconciliationTradeSnapshot(
            int Id,
            string TradeReference,
            string Currency,
            string Side,
            decimal Amount,
            DateTime SettlementDate);
    }
}
