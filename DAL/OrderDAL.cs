using System.Globalization;
using System.Text;
using Supermarket.Entities;

namespace Supermarket.DAL
{
    public class OrderDAL
    {
        private readonly string _dbPath;

        public OrderDAL(string dbPath)
        {
            _dbPath = dbPath;
        }

        public List<OrderRecord> GetAllOrders()
        {
            Services.SqliteDb.EnsureCreated(_dbPath);
            var orders = new List<OrderRecord>();
            var dict = new Dictionary<string, OrderRecord>(StringComparer.OrdinalIgnoreCase);

            var orderRows = Services.SqliteDb.Query(_dbPath, @"
SELECT OrderId, CashierId, CashierName, PayType, DiscountAmount, ReceivedAmount, ChangeAmount, Status, CreatedAt, CreatedTimestamp, TotalAmount
FROM Orders
ORDER BY CreatedAt DESC");

            foreach (var row in orderRows)
            {
                var createdAt = DateTime.TryParse(row["CreatedAt"], out var dt) ? dt : DateTime.Now;
                var order = new OrderRecord
                {
                    OrderId = row["OrderId"] ?? "",
                    CashierId = row["CashierId"] ?? "",
                    CashierName = row["CashierName"] ?? "",
                    PayType = row["PayType"] ?? "现金",
                    DiscountAmount = decimal.Parse(row["DiscountAmount"] ?? "0", CultureInfo.InvariantCulture),
                    ReceivedAmount = decimal.Parse(row["ReceivedAmount"] ?? "0", CultureInfo.InvariantCulture),
                    ChangeAmount = decimal.Parse(row["ChangeAmount"] ?? "0", CultureInfo.InvariantCulture),
                    Status = row["Status"] ?? "Completed",
                    CreatedAt = createdAt,
                    CreatedTimestamp = long.TryParse(row["CreatedTimestamp"], out var ts) ? ts : 0L,
                    TotalAmount = decimal.Parse(row["TotalAmount"] ?? "0", CultureInfo.InvariantCulture),
                    Items = new List<OrderLine>()
                };
                dict[order.OrderId] = order;
                orders.Add(order);
            }

            var itemRows = Services.SqliteDb.Query(_dbPath, @"
SELECT OrderID, ProductID, ProductName, Supplier, Category, UnitPriceAtTime, Quantity
FROM OrderItems
ORDER BY ItemID");

            foreach (var row in itemRows)
            {
                var orderId = row["OrderID"] ?? "";
                if (!dict.TryGetValue(orderId, out var order))
                {
                    continue;
                }

                var unitPrice = decimal.Parse(row["UnitPriceAtTime"] ?? "0", CultureInfo.InvariantCulture);
                var qty = int.TryParse(row["Quantity"], out var q) ? q : 0;
                order.Items.Add(new OrderLine
                {
                    ProductId = int.TryParse(row["ProductID"], out var pid) ? pid : 0,
                    ProductName = row["ProductName"] ?? "",
                    Supplier = row["Supplier"] ?? "",
                    Category = row["Category"] ?? "",
                    UnitPrice = unitPrice,
                    Quantity = qty,
                    SubTotal = unitPrice * qty
                });
            }

            return orders;
        }

        public OrderRecord? GetOrderById(string orderId)
        {
            var safeOrderId = Services.SqliteDb.Escape(orderId);
            var rows = Services.SqliteDb.Query(_dbPath,
                $"SELECT OrderId, CashierId, CashierName, PayType, DiscountAmount, ReceivedAmount, ChangeAmount, Status, CreatedAt, CreatedTimestamp, TotalAmount FROM Orders WHERE OrderId = '{safeOrderId}' LIMIT 1");
            if (rows.Count == 0)
            {
                return null;
            }

            var row = rows[0];
            var createdAt = DateTime.TryParse(row["CreatedAt"], out var dt) ? dt : DateTime.Now;
            var order = new OrderRecord
            {
                OrderId = row["OrderId"] ?? "",
                CashierId = row["CashierId"] ?? "",
                CashierName = row["CashierName"] ?? "",
                PayType = row["PayType"] ?? "现金",
                DiscountAmount = decimal.Parse(row["DiscountAmount"] ?? "0", CultureInfo.InvariantCulture),
                ReceivedAmount = decimal.Parse(row["ReceivedAmount"] ?? "0", CultureInfo.InvariantCulture),
                ChangeAmount = decimal.Parse(row["ChangeAmount"] ?? "0", CultureInfo.InvariantCulture),
                Status = row["Status"] ?? "Completed",
                CreatedAt = createdAt,
                CreatedTimestamp = long.TryParse(row["CreatedTimestamp"], out var ts) ? ts : 0L,
                TotalAmount = decimal.Parse(row["TotalAmount"] ?? "0", CultureInfo.InvariantCulture),
                Items = new List<OrderLine>()
            };

            var itemRows = Services.SqliteDb.Query(_dbPath,
                $"SELECT ProductID, ProductName, Supplier, Category, UnitPriceAtTime, Quantity FROM OrderItems WHERE OrderID = '{safeOrderId}'");
            foreach (var itemRow in itemRows)
            {
                var unitPrice = decimal.Parse(itemRow["UnitPriceAtTime"] ?? "0", CultureInfo.InvariantCulture);
                var qty = int.TryParse(itemRow["Quantity"], out var q) ? q : 0;
                order.Items.Add(new OrderLine
                {
                    ProductId = int.TryParse(itemRow["ProductID"], out var pid) ? pid : 0,
                    ProductName = itemRow["ProductName"] ?? "",
                    Supplier = itemRow["Supplier"] ?? "",
                    Category = itemRow["Category"] ?? "",
                    UnitPrice = unitPrice,
                    Quantity = qty,
                    SubTotal = unitPrice * qty
                });
            }

            return order;
        }

        public void SaveOrder(OrderRecord order)
        {
            Services.SqliteDb.EnsureCreated(_dbPath);
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN;");
            AppendOrderSql(sb, order);
            sb.AppendLine("COMMIT;");
            Services.SqliteDb.ExecuteNonQuery(_dbPath, sb.ToString());
        }

        public void UpdateOrderStatus(string orderId, string status)
        {
            var safeOrderId = Services.SqliteDb.Escape(orderId);
            Services.SqliteDb.ExecuteNonQuery(_dbPath,
                $"UPDATE Orders SET Status = '{Services.SqliteDb.Escape(status)}' WHERE OrderId = '{safeOrderId}';");
        }

        private static void AppendOrderSql(StringBuilder sb, OrderRecord order)
        {
            sb.AppendLine($@"
INSERT OR REPLACE INTO Orders (OrderId, CashierId, CashierName, PayType, DiscountAmount, ReceivedAmount, ChangeAmount, Status, CreatedAt, CreatedTimestamp, TotalAmount)
VALUES ('{Services.SqliteDb.Escape(order.OrderId)}', '{Services.SqliteDb.Escape(order.CashierId)}', '{Services.SqliteDb.Escape(order.CashierName)}',
        '{Services.SqliteDb.Escape(order.PayType)}', {order.DiscountAmount.ToString(CultureInfo.InvariantCulture)},
        {order.ReceivedAmount.ToString(CultureInfo.InvariantCulture)}, {order.ChangeAmount.ToString(CultureInfo.InvariantCulture)},
        '{Services.SqliteDb.Escape(order.Status)}', '{order.CreatedAt:yyyy-MM-dd HH:mm:ss}', {order.CreatedTimestamp},
        {order.TotalAmount.ToString(CultureInfo.InvariantCulture)});");

            foreach (var item in order.Items)
            {
                sb.AppendLine($@"
INSERT INTO OrderItems (OrderID, ProductID, Quantity, UnitPriceAtTime, ProductName, Supplier, Category)
VALUES ('{Services.SqliteDb.Escape(order.OrderId)}', {item.ProductId}, {item.Quantity}, {item.UnitPrice.ToString(CultureInfo.InvariantCulture)},
        '{Services.SqliteDb.Escape(item.ProductName)}', '{Services.SqliteDb.Escape(item.Supplier)}', '{Services.SqliteDb.Escape(item.Category)}');");
            }
        }
    }
}
