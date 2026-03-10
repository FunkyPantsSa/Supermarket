using System.Globalization;
using System.Text;
using Supermarket.DAL;
using Supermarket.Entities;

namespace Supermarket.BLL
{
    public class OrderBLL
    {
        private readonly OrderDAL _orderDAL;
        private readonly ProductDAL _productDAL;
        private readonly string _dbPath;

        public OrderBLL(string dbPath)
        {
            _dbPath = dbPath;
            _orderDAL = new OrderDAL(dbPath);
            _productDAL = new ProductDAL(dbPath);
        }

        public List<OrderRecord> GetAllOrders()
        {
            return _orderDAL.GetAllOrders();
        }

        public OrderRecord? GetOrderById(string orderId)
        {
            return _orderDAL.GetOrderById(orderId);
        }

        public bool PlaceOrder(OrderRecord order, out string error)
        {
            error = "";
            if (order.Items.Count == 0)
            {
                error = "\u8BA2\u5355\u660E\u7EC6\u4E3A\u7A7A\u3002";
                return false;
            }

            foreach (var line in order.Items)
            {
                var stock = _productDAL.GetProductStock(line.ProductId);
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
                sb.AppendLine($"UPDATE Products SET StockCount = StockCount - {line.Quantity} WHERE ID = {line.ProductId} AND IsDeleted = 0;");
            }

            AppendOrderSql(sb, order);
            sb.AppendLine(
                $"INSERT INTO TransactionLogs (Type, Amount, CashierName, Detail, Timestamp) VALUES ('\u9500\u552E', {order.TotalAmount.ToString(CultureInfo.InvariantCulture)}, '{Services.SqliteDb.Escape(order.CashierName)}', '\u8BA2\u5355 {Services.SqliteDb.Escape(order.OrderId)} {Services.SqliteDb.Escape(order.PayType)}', '{order.CreatedAt:yyyy-MM-dd HH:mm:ss}');");

            sb.AppendLine("COMMIT;");

            try
            {
                Services.SqliteDb.ExecuteNonQuery(_dbPath, sb.ToString());
                return true;
            }
            catch (Exception ex)
            {
                error = $"\u8BA2\u5355\u521B\u5EFA\u5931\u8D25\uFF1A{ex.Message}";
                return false;
            }
        }

        public bool VoidOrder(string orderId, string reason, out string message)
        {
            message = "";
            var order = _orderDAL.GetOrderById(orderId);
            if (order == null)
            {
                message = "\u672A\u627E\u5230\u8BA2\u5355\u3002";
                return false;
            }

            if (string.Equals(order.Status, "Voided", StringComparison.OrdinalIgnoreCase))
            {
                message = "\u8BA2\u5355\u5DF2\u4F5C\u5E9F\u3002";
                return false;
            }

            var sb = new StringBuilder();
            sb.AppendLine("BEGIN;");

            foreach (var item in order.Items)
            {
                sb.AppendLine($"UPDATE Products SET StockCount = StockCount + {item.Quantity} WHERE ID = {item.ProductId} AND IsDeleted = 0;");
            }

            sb.AppendLine($"UPDATE Orders SET Status = 'Voided' WHERE OrderId = '{Services.SqliteDb.Escape(orderId)}';");
            sb.AppendLine(
                $"INSERT INTO TransactionLogs (Type, Amount, CashierName, Detail, Timestamp) VALUES ('\u9000\u6B3E', {(-order.TotalAmount).ToString(CultureInfo.InvariantCulture)}, '{Services.SqliteDb.Escape(order.CashierName)}', '\u8BA2\u5355 {Services.SqliteDb.Escape(orderId)} \u4F5C\u5E9F: {Services.SqliteDb.Escape(reason)}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}');");

            sb.AppendLine("COMMIT;");

            try
            {
                Services.SqliteDb.ExecuteNonQuery(_dbPath, sb.ToString());
                message = "\u9000\u5355\u6210\u529F\uFF0C\u5DF2\u8FD4\u8FD8\u5E93\u5B58\u3002";
                return true;
            }
            catch (Exception ex)
            {
                message = $"\u9000\u5355\u5931\u8D25\uFF1A{ex.Message}";
                return false;
            }
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
