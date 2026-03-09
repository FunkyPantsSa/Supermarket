using System.Globalization;
using System.Text;
using Supermarket.DAL;
using Supermarket.Entities;

namespace Supermarket.BLL
{
    /// <summary>
    /// 订单业务逻辑层
    /// </summary>
    public class OrderBLL
    {
        private readonly OrderDAL _orderDAL;
        private readonly ProductDAL _productDAL;
        private readonly TransactionLogDAL _logDAL;
        private readonly string _dbPath;

        public OrderBLL(string dbPath)
        {
            _dbPath = dbPath;
            _orderDAL = new OrderDAL(dbPath);
            _productDAL = new ProductDAL(dbPath);
            _logDAL = new TransactionLogDAL(dbPath);
        }

        public List<OrderRecord> GetAllOrders()
        {
            return _orderDAL.GetAllOrders();
        }

        public OrderRecord? GetOrderById(string orderId)
        {
            return _orderDAL.GetOrderById(orderId);
        }

        /// <summary>
        /// 下订单（使用事务确保库存扣减和订单创建的一致性）
        /// </summary>
        public bool PlaceOrder(OrderRecord order, out string error)
        {
            error = "";
            if (order.Items.Count == 0)
            {
                error = "订单明细为空。";
                return false;
            }

            // 检查库存
            foreach (var line in order.Items)
            {
                var stock = _productDAL.GetProductStock(line.ProductId);
                if (stock < line.Quantity)
                {
                    error = $"商品 {line.ProductName} 库存不足。";
                    return false;
                }
            }

            // 使用事务处理：同时更新库存和创建订单
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN;");
            
            // 扣减库存
            foreach (var line in order.Items)
            {
                sb.AppendLine($"UPDATE Products SET StockCount = StockCount - {line.Quantity} WHERE ID = {line.ProductId} AND IsDeleted = 0;");
            }

            // 保存订单
            AppendOrderSql(sb, order);
            
            // 记录交易流水
            sb.AppendLine(
                $"INSERT INTO TransactionLogs (Type, Amount, Detail, Timestamp) VALUES ('销售', {order.TotalAmount.ToString(CultureInfo.InvariantCulture)}, '订单 {Services.SqliteDb.Escape(order.OrderId)} {Services.SqliteDb.Escape(order.PayType)}', '{order.CreatedAt:yyyy-MM-dd HH:mm:ss}');");
            
            sb.AppendLine("COMMIT;");

            try
            {
                Services.SqliteDb.ExecuteNonQuery(_dbPath, sb.ToString());
                return true;
            }
            catch (Exception ex)
            {
                error = $"订单创建失败：{ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 作废订单（返还库存并记录流水）
        /// </summary>
        public bool VoidOrder(string orderId, string reason, out string message)
        {
            message = "";
            var order = _orderDAL.GetOrderById(orderId);
            if (order == null)
            {
                message = "未找到订单。";
                return false;
            }

            if (string.Equals(order.Status, "Voided", StringComparison.OrdinalIgnoreCase))
            {
                message = "订单已作废。";
                return false;
            }

            var sb = new StringBuilder();
            sb.AppendLine("BEGIN;");
            
            // 返还库存
            foreach (var item in order.Items)
            {
                sb.AppendLine($"UPDATE Products SET StockCount = StockCount + {item.Quantity} WHERE ID = {item.ProductId} AND IsDeleted = 0;");
            }

            // 更新订单状态
            sb.AppendLine($"UPDATE Orders SET Status = 'Voided' WHERE OrderId = '{Services.SqliteDb.Escape(orderId)}';");
            
            // 记录退款流水
            sb.AppendLine(
                $"INSERT INTO TransactionLogs (Type, Amount, Detail, Timestamp) VALUES ('退款', {(-order.TotalAmount).ToString(CultureInfo.InvariantCulture)}, '订单 {Services.SqliteDb.Escape(orderId)} 作废: {Services.SqliteDb.Escape(reason)}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}');");
            
            sb.AppendLine("COMMIT;");

            try
            {
                Services.SqliteDb.ExecuteNonQuery(_dbPath, sb.ToString());
                message = "退单成功，已返还库存。";
                return true;
            }
            catch (Exception ex)
            {
                message = $"退单失败：{ex.Message}";
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
