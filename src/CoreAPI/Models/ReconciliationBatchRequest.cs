namespace CoreAPI.Models
{
    public class ReconciliationBatchRequest
    {
        public DateTime? FromSettlementDate { get; set; }
        public DateTime? ToSettlementDate { get; set; }
        public decimal Tolerance { get; set; } = 0.01m;
        public int BatchSize { get; set; } = 100;
        public int MaxDegreeOfParallelism { get; set; } = 4;
        public List<ReconciliationLedgerEntry> LedgerEntries { get; set; } = new();
    }

    public class ReconciliationLedgerEntry
    {
        public string ExternalReference { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string Side { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime SettlementDate { get; set; }
    }
}
