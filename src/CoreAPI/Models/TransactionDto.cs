using System;

namespace CoreAPI.Models
{
	public class TransactionDto()
	{
		public required int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public DateTime Tradedate { get; set; }
    }
}
