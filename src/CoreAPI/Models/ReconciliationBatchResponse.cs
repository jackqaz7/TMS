namespace CoreAPI.Models
{
    public class ReconciliationBatchResponse
    {
        public Guid BatchId { get; set; }
        public int TradeCount { get; set; }
        public int LedgerEntryCount { get; set; }
        public int GroupCount { get; set; }
        public int MatchedGroupCount { get; set; }
        public int BreakGroupCount { get; set; }
        public int BatchSize { get; set; }
        public int MaxDegreeOfParallelism { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public List<int> WorkerThreadIds { get; set; } = new();
        public List<ReconciliationGroupResult> Results { get; set; } = new();
    }

    public class ReconciliationGroupResult
    {
        public string Currency { get; set; } = string.Empty;
        public DateTime SettlementDate { get; set; }
        public decimal ExpectedBuyAmount { get; set; }
        public decimal ActualBuyAmount { get; set; }
        public decimal BuyBreakAmount { get; set; }
        public decimal ExpectedSellAmount { get; set; }
        public decimal ActualSellAmount { get; set; }
        public decimal SellBreakAmount { get; set; }
        public bool IsMatched { get; set; }
        public int TradeCount { get; set; }
        public int LedgerEntryCount { get; set; }
        public int WorkerThreadId { get; set; }
    }
}
