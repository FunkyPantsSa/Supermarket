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
                Orders.Add(order);
            }

            var itemRows = SqliteDb.Query(_dbPath, @"
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
        }

        public bool PlaceSaleOrder(OrderRecord order, out string error)
        {
            error = "";
            if (order.Items.Count == 0)
            {
                error = "\u8BA2\u5355\u660E\u7EC6\u4E3A\u7A7A\u3002";
                return false;
            }

            foreach (var line in order.Items)
            {
                var stock = SqliteDb.ScalarInt(_dbPath, $"SELECT StockCount FROM Products WHERE ID = {line.ProductId} AND IsDeleted = 0");
                if (stock < line.Quantity)
                {
                    error = $"\u5546\u54C1 {line.ProductName} \u5E93\u5B58\u4E0D\u8DB3\u3002";
                    return false;
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("BEGIN;");
            foreach (var line in order.Items)
            {
                sb.AppendLine($"UPDATE Products SET StockCount = StockCount - {line.Quantity} WHERE ID = {line.ProductId};");
            }

            AppendOrderSql(sb, order);
            AppendSalesLogSql(sb, order);
            sb.AppendLine("COMMIT;");

            SqliteDb.ExecuteNonQuery(_dbPath, sb.ToString());
            Orders.Insert(0, order);
            return true;
        }

        public void Append(OrderRecord order)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN;");
            AppendOrderSql(sb, order);
            AppendSalesLogSql(sb, order);
            sb.AppendLine("COMMIT;");
            SqliteDb.ExecuteNonQuery(_dbPath, sb.ToString());
            Orders.Insert(0, order);
        }

        public void AppendRange(IEnumerable<OrderRecord> orders)
        {
            var list = orders.ToList();
            if (list.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("BEGIN;");
            foreach (var order in list)
            {
                AppendOrderSql(sb, order);
                AppendSalesLogSql(sb, order);
            }
            sb.AppendLine("COMMIT;");
            SqliteDb.ExecuteNonQuery(_dbPath, sb.ToString());
            Orders.InsertRange(0, list.OrderByDescending(x => x.CreatedAt));
        }

        public void Save()
        {
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN;");
            sb.AppendLine("DELETE FROM OrderItems;");
            sb.AppendLine("DELETE FROM Orders;");
            foreach (var order in Orders.OrderBy(o => o.CreatedAt))
            {
                AppendOrderSql(sb, order);
            }
            sb.AppendLine("COMMIT;");
            SqliteDb.ExecuteNonQuery(_dbPath, sb.ToString());
        }

        public bool VoidOrder(string orderId, string reason, out string message)
        {
            var safeOrderId = SqliteDb.Escape(orderId);
            var rows = SqliteDb.Query(_dbPath, $"SELECT Status, TotalAmount, CashierName FROM Orders WHERE OrderId = '{safeOrderId}' LIMIT 1");
            if (rows.Count == 0)
            {
                message = "\u672A\u627E\u5230\u8BA2\u5355\u3002";
                return false;
            }

            var status = rows[0]["Status"] ?? "Completed";
            if (string.Equals(status, "Voided", StringComparison.OrdinalIgnoreCase))
            {
                message = "\u8BA2\u5355\u5DF2\u4F5C\u5E9F\u3002";
                return false;
            }

            var totalAmount = decimal.Parse(rows[0]["TotalAmount"] ?? "0", CultureInfo.InvariantCulture);
            var items = SqliteDb.Query(_dbPath, $"SELECT ProductID, Quantity FROM OrderItems WHERE OrderID = '{safeOrderId}'");

            var sb = new StringBuilder();
            sb.AppendLine("BEGIN;");
            foreach (var item in items)
            {
                var pid = int.TryParse(item["ProductID"], out var x) ? x : 0;
                var qty = int.TryParse(item["Quantity"], out var y) ? y : 0;
                if (pid > 0 && qty > 0)
                {
                    sb.AppendLine($"UPDATE Products SET StockCount = StockCount + {qty} WHERE ID = {pid};");
                }
            }

            sb.AppendLine($"UPDATE Orders SET Status = 'Voided' WHERE OrderId = '{safeOrderId}';");
            var cashierName = SqliteDb.Escape(rows[0].ContainsKey("CashierName") ? rows[0]["CashierName"] ?? "" : "");
            sb.AppendLine($"INSERT INTO TransactionLogs (Type, Amount, CashierName, Detail, Timestamp) VALUES ('\u9000\u6B3E', {(-totalAmount).ToString(CultureInfo.InvariantCulture)}, '{cashierName}', '\u8BA2\u5355 {safeOrderId} \u4F5C\u5E9F: {SqliteDb.Escape(reason)}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}');");
            sb.AppendLine("COMMIT;");

            SqliteDb.ExecuteNonQuery(_dbPath, sb.ToString());
            Load();
            message = "\u9000\u5355\u6210\u529F\uFF0C\u5DF2\u56DE\u8865\u5E93\u5B58\u3002";
            return true;
        }

        public List<TransactionLog> QueryLogs(DateTime? start = null, DateTime? end = null)
        {
            var sql = new StringBuilder(@"
SELECT LogID, Type, Amount, Detail, Timestamp
FROM TransactionLogs
WHERE 1 = 1");

            if (start.HasValue)
            {
                sql.Append($" AND Timestamp >= '{start.Value:yyyy-MM-dd HH:mm:ss}'");
            }

            if (end.HasValue)
            {
                sql.Append($" AND Timestamp <= '{end.Value:yyyy-MM-dd HH:mm:ss}'");
            }

            sql.Append(" ORDER BY Timestamp DESC, LogID DESC");

            return SqliteDb.Query(_dbPath, sql.ToString())
                .Select(r => new TransactionLog
                {
                    LogId = long.TryParse(r["LogID"], out var id) ? id : 0,
                    Type = r["Type"] ?? "",
                    Amount = decimal.Parse(r["Amount"] ?? "0", CultureInfo.InvariantCulture),
                    Detail = r["Detail"] ?? "",
                    Timestamp = DateTime.TryParse(r["Timestamp"], out var t) ? t : DateTime.Now
                })
                .ToList();
        }

        public void ExportLogsToCsv(string outputPath, DateTime? start = null, DateTime? end = null)
        {
            var logs = QueryLogs(start, end);
            var sb = new StringBuilder();
            sb.AppendLine("LogID,Type,Amount,Detail,Timestamp");
            foreach (var log in logs)
            {
                var detail = log.Detail.Replace("\"", "\"\"");
                sb.AppendLine($"{log.LogId},\"{log.Type}\",{log.Amount.ToString(CultureInfo.InvariantCulture)},\"{detail}\",{log.Timestamp:yyyy-MM-dd HH:mm:ss}");
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        private static void AppendSalesLogSql(StringBuilder sb, OrderRecord order)
        {
            sb.AppendLine(
                $"INSERT INTO TransactionLogs (Type, Amount, CashierName, Detail, Timestamp) VALUES ('\u9500\u552E', {order.TotalAmount.ToString(CultureInfo.InvariantCulture)}, '{SqliteDb.Escape(order.CashierName)}', '\u8BA2\u5355 {SqliteDb.Escape(order.OrderId)} {SqliteDb.Escape(order.PayType)}', '{order.CreatedAt:yyyy-MM-dd HH:mm:ss}');");
        }

        private static void AppendOrderSql(StringBuilder sb, OrderRecord order)
        {
            sb.AppendLine($@"
INSERT OR REPLACE INTO Orders (OrderId, CashierId, CashierName, PayType, DiscountAmount, ReceivedAmount, ChangeAmount, Status, CreatedAt, CreatedTimestamp, TotalAmount)
VALUES ('{SqliteDb.Escape(order.OrderId)}', '{SqliteDb.Escape(order.CashierId)}', '{SqliteDb.Escape(order.CashierName)}', '{SqliteDb.Escape(order.PayType)}', {order.DiscountAmount.ToString(CultureInfo.InvariantCulture)}, {order.ReceivedAmount.ToString(CultureInfo.InvariantCulture)}, {order.ChangeAmount.ToString(CultureInfo.InvariantCulture)}, '{SqliteDb.Escape(order.Status)}', '{order.CreatedAt:yyyy-MM-dd HH:mm:ss}', {order.CreatedTimestamp}, {order.TotalAmount.ToString(CultureInfo.InvariantCulture)});");

            foreach (var item in order.Items)
            {
                sb.AppendLine($@"
INSERT INTO OrderItems (OrderID, ProductID, Quantity, UnitPriceAtTime, ProductName, Supplier, Category)
VALUES ('{SqliteDb.Escape(order.OrderId)}', {item.ProductId}, {item.Quantity}, {item.UnitPrice.ToString(CultureInfo.InvariantCulture)}, '{SqliteDb.Escape(item.ProductName)}', '{SqliteDb.Escape(item.Supplier)}', '{SqliteDb.Escape(item.Category)}');");
            }
        }
    }
}
