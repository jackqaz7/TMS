namespace CoreAPI.Models
{
    public class FxRate
    {
        public int Id { get; set; }

        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public DateTime RateDate { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
    }
}
