using System.Globalization;
using Supermarket.Entities;

namespace Supermarket.DAL
{
    /// <summary>
    /// 商品数据访问层
    /// </summary>
    public class ProductDAL
    {
        private readonly string _dbPath;

        public ProductDAL(string dbPath)
        {
            _dbPath = dbPath;
        }

        public List<Product> GetAllProducts()
        {
            Services.SqliteDb.EnsureCreated(_dbPath);
            var products = new List<Product>();

            var rows = Services.SqliteDb.Query(_dbPath, 
                "SELECT ID, Name, Supplier, Category, Price, StockCount, LowStockThreshold, IsDeleted FROM Products WHERE IsDeleted = 0 ORDER BY ID");
            
            foreach (var row in rows)
            {
                products.Add(new Product
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

            return products;
        }

        public Product? GetProductById(int id)
        {
            var rows = Services.SqliteDb.Query(_dbPath, 
                $"SELECT ID, Name, Supplier, Category, Price, StockCount, LowStockThreshold, IsDeleted FROM Products WHERE ID = {id} AND IsDeleted = 0 LIMIT 1");
            
            if (rows.Count == 0) return null;

            var row = rows[0];
            return new Product
            {
                ID = int.TryParse(row["ID"], out var productId) ? productId : 0,
                Name = row["Name"] ?? "",
                Supplier = row["Supplier"] ?? "",
                Category = row["Category"] ?? "",
                Price = decimal.Parse(row["Price"] ?? "0", CultureInfo.InvariantCulture),
                StockCount = int.TryParse(row["StockCount"], out var stock) ? stock : 0,
                LowStockThreshold = int.TryParse(row["LowStockThreshold"], out var low) ? low : 10,
                IsDeleted = (row["IsDeleted"] ?? "0") == "1"
            };
        }

        public void SaveProduct(Product product)
        {
            Services.SqliteDb.EnsureCreated(_dbPath);
            var sql = $@"
INSERT OR REPLACE INTO Products (ID, Name, Supplier, Category, Price, StockCount, LowStockThreshold, IsDeleted)
VALUES ({product.ID}, '{Services.SqliteDb.Escape(product.Name)}', '{Services.SqliteDb.Escape(product.Supplier)}', 
        '{Services.SqliteDb.Escape(product.Category)}', {product.Price.ToString(CultureInfo.InvariantCulture)}, 
        {product.StockCount}, {Math.Max(0, product.LowStockThreshold)}, {(product.IsDeleted ? 1 : 0)});";
            
            Services.SqliteDb.ExecuteNonQuery(_dbPath, sql);
        }

        public void UpdateProductStock(int productId, int quantityChange)
        {
            Services.SqliteDb.ExecuteNonQuery(_dbPath, 
                $"UPDATE Products SET StockCount = StockCount + {quantityChange} WHERE ID = {productId} AND IsDeleted = 0;");
        }

        public void LogicalDeleteProduct(int productId)
        {
            Services.SqliteDb.ExecuteNonQuery(_dbPath, 
                $"UPDATE Products SET IsDeleted = 1 WHERE ID = {productId};");
        }

        public int GetProductStock(int productId)
        {
            return Services.SqliteDb.ScalarInt(_dbPath, 
                $"SELECT StockCount FROM Products WHERE ID = {productId} AND IsDeleted = 0");
        }
    }
}
