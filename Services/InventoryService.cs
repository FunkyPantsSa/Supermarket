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

            var rows = SqliteDb.Query(_dbPath, "SELECT ID, Name, Supplier, Category, Price, StockCount, LowStockThreshold, IsDeleted FROM Products WHERE IsDeleted = 0 ORDER BY ID");
            foreach (var row in rows)
            {
                Products.Add(new Product
                {
                    ID = int.TryParse(row["ID"], out var id) ? id : 0,
                    Name = row["Name"] ?? "",
                    Supplier = row["Supplier"] ?? "",
                    Category = row["Category"] ?? "",
                    Price = decimal.Parse(row["Price"] ?? "0", CultureInfo.InvariantCulture),
                    StockCount = int.TryParse(row["StockCount"], out var stock) ? stock : 0,
                    LowStockThreshold = int.TryParse(row["LowStockThreshold"], out var low) ? low : 10,
                    IsDeleted = (row["IsDeleted"] ?? "0") == "1"
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
                    $"INSERT INTO Products (ID, Name, Supplier, Category, Price, StockCount, LowStockThreshold, IsDeleted) VALUES ({p.ID}, '{SqliteDb.Escape(p.Name)}', '{SqliteDb.Escape(p.Supplier)}', '{SqliteDb.Escape(p.Category)}', {p.Price.ToString(CultureInfo.InvariantCulture)}, {p.StockCount}, {Math.Max(0, p.LowStockThreshold)}, {(p.IsDeleted ? 1 : 0)});");
            }

            sb.AppendLine("COMMIT;");
            SqliteDb.ExecuteNonQuery(_dbPath, sb.ToString());
        }

        public Product? FindById(int id)
        {
            return Products.FirstOrDefault(p => p.ID == id);
        }

        public void LogicalDelete(int id)
        {
            var target = Products.FirstOrDefault(p => p.ID == id);
            if (target == null) return;
            target.IsDeleted = true;
            Products = Products.Where(p => !p.IsDeleted).ToList();
            Save();
        }
    }
}
