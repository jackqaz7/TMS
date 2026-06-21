namespace CoreAPI.Models
{
    public class PositionSummaryDto
    {
        public required string Currency { get; set; }
        public decimal BuyNotional { get; set; }
        public decimal SellNotional { get; set; }
        public decimal NetNotional { get; set; }
        public decimal WeightedAverageRate { get; set; }
        public int TradeCount { get; set; }
    }
}
