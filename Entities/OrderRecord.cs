namespace Supermarket.Entities
{
    public class OrderLine
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string Supplier { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class OrderRecord
    {
        public string OrderId { get; set; } = "";
        public string CashierId { get; set; } = "";
        public string CashierName { get; set; } = "";
        public string PayType { get; set; } = "现金";
        public DateTime CreatedAt { get; set; }
        public long CreatedTimestamp { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ReceivedAmount { get; set; }
        public decimal ChangeAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Completed";
        public List<OrderLine> Items { get; set; } = new();
    }
}
