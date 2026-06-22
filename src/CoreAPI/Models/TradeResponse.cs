namespace CoreAPI.Models
{
    public class TradeResponse
    {
        public int Id { get; set; }
        public string TradeReference { get; set; } = string.Empty;
        public string TradeType { get; set; } = string.Empty;
        public string Counterparty { get; set; } = string.Empty;
        public string? CounterpartyBankAccount { get; set; }

        public string Currency1 { get; set; } = string.Empty;
        public decimal Amount1 { get; set; }
        public string Currency2 { get; set; } = string.Empty;
        public decimal Amount2 { get; set; }

        public decimal FxRateUsed { get; set; }
        public DateTime RateDate { get; set; }

        public string Side { get; set; } = string.Empty;
        public DateTime TradeDate { get; set; }
        public DateTime SettlementDate { get; set; }

        public decimal Fees { get; set; }
        public string? Comments { get; set; }

        public DateTime? NearLegDate { get; set; }
        public decimal? NearLegRate { get; set; }
        public decimal? NearLegAmount1 { get; set; }
        public decimal? NearLegAmount2 { get; set; }

        public DateTime? FarLegDate { get; set; }
        public decimal? FarLegRate { get; set; }
        public decimal? FarLegAmount1 { get; set; }
        public decimal? FarLegAmount2 { get; set; }

        public decimal? SwapPoints { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
        public string? EditedBy { get; set; }
        public DateTime? LastEditedUtc { get; set; }
    }
}
