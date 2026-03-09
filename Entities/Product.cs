namespace Supermarket.Entities
{
    public class Product
    {
        public int ID { get; set; }
        public string Name { get; set; } = "";
        public string Supplier { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal Price { get; set; }
        public int StockCount { get; set; }
        public int LowStockThreshold { get; set; } = 10;
        public bool IsDeleted { get; set; }
    }
}
