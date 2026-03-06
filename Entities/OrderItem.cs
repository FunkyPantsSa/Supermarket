namespace Supermarket.Entities
{
    public class OrderItem
    {
        public Product Product { get; set; } = new Product();
        public int Quantity { get; set; }

        public decimal SubTotal => Product.Price * Quantity;
    }
}
