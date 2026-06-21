using System.ComponentModel.DataAnnotations;

namespace CoreAPI.Models
{
    public class CreateTradeRequest
    {
        [Required]
        public string TradeReference { get; set; } = string.Empty;

        [Required]
        public string Counterparty { get; set; } = string.Empty;

        [Required]
        public string Instrument { get; set; } = string.Empty;

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = string.Empty;

        [Required]
        [RegularExpression("BUY|SELL", ErrorMessage = "Side must be BUY or SELL.")]
        public string Side { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal Notional { get; set; }

        [Range(0.000001, double.MaxValue)]
        public decimal Rate { get; set; }

        public DateTime TradeDate { get; set; }

        public DateTime SettlementDate { get; set; }
    }
}
