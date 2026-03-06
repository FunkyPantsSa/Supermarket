using System.Globalization;
using System.Text;
using Supermarket.Entities;

namespace Supermarket.Services
{
    public class OrderService
    {
        private readonly string _dbPath;
        public List<OrderRecord> Orders { get; private set; } = new();

        public OrderService(string dbPath)
        {
            _dbPath = dbPath;
        }

        public void Load()
        {
            SqliteDb.EnsureCreated(_dbPath);
            Orders = new List<OrderRecord>();

            var dict = new Dictionary<string, OrderRecord>(StringComparer.OrdinalIgnoreCase);
            var orderRows = SqliteDb.Query(_dbPath, @"
SELECT OrderId, CashierId, CashierName, CreatedAt, CreatedTimestamp, TotalAmount
FROM Orders
ORDER BY CreatedAt DESC");

            foreach (var row in orderRows)
            {
                var createdAtText = row["CreatedAt"] ?? "";
                var createdAt = DateTime.TryParse(createdAtText, out var dt) ? dt : DateTime.Now;
                var order = new OrderRecord
                {
                    OrderId = row["OrderId"] ?? "",
                    CashierId = row["CashierId"] ?? "",
                    CashierName = row["CashierName"] ?? "",
                    CreatedAt = createdAt,
                    CreatedTimestamp = long.TryParse(row["CreatedTimestamp"], out var ts) ? ts : 0L,
                    TotalAmount = decimal.Parse(row["TotalAmount"] ?? "0", CultureInfo.InvariantCulture),
                    Items = new List<OrderLine>()
                };
                dict[order.OrderId] = order;
                Orders.Add(order);
            }

            var lineRows = SqliteDb.Query(_dbPath, @"
SELECT OrderId, ProductId, ProductName, Supplier, Category, UnitPrice, Quantity, SubTotal
FROM OrderLines
ORDER BY Id");

            foreach (var row in lineRows)
            {
                var orderId = row["OrderId"] ?? "";
                if (!dict.TryGetValue(orderId, out var order))
                {
                    continue;
                }

                order.Items.Add(new OrderLine
                {
                    ProductId = int.TryParse(row["ProductId"], out var pid) ? pid : 0,
                    ProductName = row["ProductName"] ?? "",
                    Supplier = row["Supplier"] ?? "",
                    Category = row["Category"] ?? "",
                    UnitPrice = decimal.Parse(row["UnitPrice"] ?? "0", CultureInfo.InvariantCulture),
                    Quantity = int.TryParse(row["Quantity"], out var qty) ? qty : 0,
                    SubTotal = decimal.Parse(row["SubTotal"] ?? "0", CultureInfo.InvariantCulture)
                });
            }
        }

        public void Append(OrderRecord order)
        {
            var sql = BuildInsertScript(new[] { order });
            SqliteDb.ExecuteNonQuery(_dbPath, sql);
            Orders.Add(order);
        }

        public void AppendRange(IEnumerable<OrderRecord> orders)
        {
            var list = orders.ToList();
            if (list.Count == 0)
            {
                return;
            }

            var sql = BuildInsertScript(list);
            SqliteDb.ExecuteNonQuery(_dbPath, sql);
            Orders.AddRange(list);
        }

        public void Save()
        {
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN;");
            sb.AppendLine("DELETE FROM OrderLines;");
            sb.AppendLine("DELETE FROM Orders;");
            foreach (var order in Orders)
            {
                AppendOrderSql(sb, order);
            }
            sb.AppendLine("COMMIT;");
            SqliteDb.ExecuteNonQuery(_dbPath, sb.ToString());
        }

        private static string BuildInsertScript(IEnumerable<OrderRecord> orders)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN;");
            foreach (var order in orders)
            {
                AppendOrderSql(sb, order);
            }
            sb.AppendLine("COMMIT;");
            return sb.ToString();
        }

        private static void AppendOrderSql(StringBuilder sb, OrderRecord order)
        {
            sb.AppendLine($@"
INSERT OR REPLACE INTO Orders (OrderId, CashierId, CashierName, CreatedAt, CreatedTimestamp, TotalAmount)
VALUES ('{SqliteDb.Escape(order.OrderId)}', '{SqliteDb.Escape(order.CashierId)}', '{SqliteDb.Escape(order.CashierName)}', '{order.CreatedAt:yyyy-MM-dd HH:mm:ss}', {order.CreatedTimestamp}, {order.TotalAmount.ToString(CultureInfo.InvariantCulture)});");

            foreach (var line in order.Items)
            {
                sb.AppendLine($@"
INSERT INTO OrderLines (OrderId, ProductId, ProductName, Supplier, Category, UnitPrice, Quantity, SubTotal)
VALUES ('{SqliteDb.Escape(order.OrderId)}', {line.ProductId}, '{SqliteDb.Escape(line.ProductName)}', '{SqliteDb.Escape(line.Supplier)}', '{SqliteDb.Escape(line.Category)}', {line.UnitPrice.ToString(CultureInfo.InvariantCulture)}, {line.Quantity}, {line.SubTotal.ToString(CultureInfo.InvariantCulture)});");
            }
        }
    }
}
