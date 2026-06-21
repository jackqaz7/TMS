namespace CoreAPI.Models
{
    public class TradeResponse
    {
        public int Id { get; set; }
        public required string TradeReference { get; set; }
        public required string Counterparty { get; set; }
        public required string Instrument { get; set; }
        public required string Currency { get; set; }
        public required string Side { get; set; }
        public decimal Notional { get; set; }
        public decimal Rate { get; set; }
        public decimal LocalAmount { get; set; }
        public DateTime TradeDate { get; set; }
        public DateTime SettlementDate { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
