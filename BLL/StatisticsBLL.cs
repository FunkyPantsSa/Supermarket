using Supermarket.DAL;
using Supermarket.Entities;

namespace Supermarket.BLL
{
    /// <summary>
    /// 统计业务逻辑层
    /// </summary>
    public class StatisticsBLL
    {
        private readonly OrderDAL _orderDAL;
        private readonly ProductDAL _productDAL;

        public StatisticsBLL(string dbPath)
        {
            _orderDAL = new OrderDAL(dbPath);
            _productDAL = new ProductDAL(dbPath);
        }

        /// <summary>
        /// 获取销售走势数据（按日期分组）
        /// </summary>
        public Dictionary<DateTime, decimal> GetSalesTrend(int days)
        {
            var startDate = DateTime.Today.AddDays(-days);
            var orders = _orderDAL.GetAllOrders()
                .Where(o => o.CreatedAt >= startDate && o.Status != "Voided")
                .GroupBy(o => o.CreatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Sum(o => o.TotalAmount));
            
            // 填充缺失的日期
            for (int i = 0; i < days; i++)
            {
                var date = DateTime.Today.AddDays(-i);
                if (!orders.ContainsKey(date))
                {
                    orders[date] = 0m;
                }
            }
            
            return orders.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// 获取分类占比数据
        /// </summary>
        public Dictionary<string, decimal> GetCategorySales()
        {
            var orders = _orderDAL.GetAllOrders()
                .Where(o => o.Status != "Voided");
            
            var categorySales = new Dictionary<string, decimal>();
            foreach (var order in orders)
            {
                foreach (var item in order.Items)
                {
                    if (!categorySales.ContainsKey(item.Category))
                    {
                        categorySales[item.Category] = 0m;
                    }
                    categorySales[item.Category] += item.SubTotal;
                }
            }
            
            return categorySales;
        }

        /// <summary>
        /// 获取畅销商品排行（Top N）
        /// </summary>
        public List<(string ProductName, int Quantity, decimal TotalAmount)> GetTopSellingProducts(int topN = 10)
        {
            var orders = _orderDAL.GetAllOrders()
                .Where(o => o.Status != "Voided");
            
            var productSales = new Dictionary<string, (int Quantity, decimal TotalAmount)>();
            
            foreach (var order in orders)
            {
                foreach (var item in order.Items)
                {
                    var key = $"{item.ProductId}_{item.ProductName}";
                    if (!productSales.ContainsKey(key))
                    {
                        productSales[key] = (0, 0m);
                    }
                    var current = productSales[key];
                    productSales[key] = (current.Quantity + item.Quantity, current.TotalAmount + item.SubTotal);
                }
            }
            
            return productSales
                .OrderByDescending(kvp => kvp.Value.Quantity)
                .Take(topN)
                .Select(kvp => (kvp.Key.Split('_')[1], kvp.Value.Quantity, kvp.Value.TotalAmount))
                .ToList();
        }

        /// <summary>
        /// 获取毛利估算（需要进价信息，这里简化处理）
        /// </summary>
        public Dictionary<string, decimal> GetGrossProfitEstimate()
        {
            // 注意：实际应用中需要存储进价（CostPrice），这里简化处理
            // 假设毛利率为30%
            const decimal grossProfitRate = 0.3m;
            
            var orders = _orderDAL.GetAllOrders()
                .Where(o => o.Status != "Voided");
            
            var productProfit = new Dictionary<string, decimal>();
            
            foreach (var order in orders)
            {
                foreach (var item in order.Items)
                {
                    var key = item.ProductName;
                    if (!productProfit.ContainsKey(key))
                    {
                        productProfit[key] = 0m;
                    }
                    // 估算毛利 = 销售额 * 毛利率
                    productProfit[key] += item.SubTotal * grossProfitRate;
                }
            }
            
            return productProfit.OrderByDescending(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}
