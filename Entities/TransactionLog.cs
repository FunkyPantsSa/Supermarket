namespace Supermarket.Entities
{
    public class TransactionLog
    {
        public long LogId { get; set; }
        public string Type { get; set; } = "";
        public decimal Amount { get; set; }
        public string Detail { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }
}
