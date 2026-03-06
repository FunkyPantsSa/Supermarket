using System.Globalization;
using System.Text;
using Supermarket.Entities;

namespace Supermarket.Services
{
    public class InventoryService
    {
        private readonly string _dbPath;
        public List<Product> Products { get; private set; } = new();

        public InventoryService(string dbPath)
        {
            _dbPath = dbPath;
        }

        public void Load()
        {
            SqliteDb.EnsureCreated(_dbPath);
            Products = new List<Product>();

            var rows = SqliteDb.Query(_dbPath, "SELECT ID, Name, Supplier, Category, Price, StockCount FROM Products ORDER BY ID");
            foreach (var row in rows)
            {
                Products.Add(new Product
                {
                    ID = int.TryParse(row["ID"], out var id) ? id : 0,
                    Name = row["Name"] ?? "",
                    Supplier = row["Supplier"] ?? "",
                    Category = row["Category"] ?? "",
                    Price = decimal.Parse(row["Price"] ?? "0", CultureInfo.InvariantCulture),
                    StockCount = int.TryParse(row["StockCount"], out var stock) ? stock : 0
                });
            }
        }

        public void Save()
        {
            SqliteDb.EnsureCreated(_dbPath);

            var sb = new StringBuilder();
            sb.AppendLine("BEGIN;");
            sb.AppendLine("DELETE FROM Products;");

            foreach (var p in Products)
            {
                sb.AppendLine(
                    $"INSERT INTO Products (ID, Name, Supplier, Category, Price, StockCount) VALUES ({p.ID}, '{SqliteDb.Escape(p.Name)}', '{SqliteDb.Escape(p.Supplier)}', '{SqliteDb.Escape(p.Category)}', {p.Price.ToString(CultureInfo.InvariantCulture)}, {p.StockCount});");
            }

            sb.AppendLine("COMMIT;");
            SqliteDb.ExecuteNonQuery(_dbPath, sb.ToString());
        }

        public Product? FindById(int id)
        {
            return Products.FirstOrDefault(p => p.ID == id);
        }
    }
}
