namespace api.Models
{
    public enum StockTxnType { In = 1, Out = 2, Adjust = 3 }

    public class StockTxn
    {
        public long Id { get; set; }
        public int StoreId { get; set; }
        public int ProductId { get; set; }
        public StockTxnType Type { get; set; }
        public decimal Qty { get; set; }
        public string? Reason { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
