namespace CoreAPI.Models
{
    public class FxRateResponse
    {
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public DateTime RateDate { get; set; }
        public string Source { get; set; } = string.Empty;
        public bool IsCrossRate { get; set; }
    }
}
