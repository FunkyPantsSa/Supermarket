using Supermarket.DAL;
using Supermarket.Entities;

namespace Supermarket.BLL
{
    /// <summary>
    /// 交易流水业务逻辑层
    /// </summary>
    public class TransactionLogBLL
    {
        private readonly TransactionLogDAL _logDAL;

        public TransactionLogBLL(string dbPath)
        {
            _logDAL = new TransactionLogDAL(dbPath);
        }

        public List<TransactionLog> GetLogs(DateTime? start = null, DateTime? end = null)
        {
            return _logDAL.GetLogs(start, end);
        }

        public void AddLog(string type, decimal amount, string detail)
        {
            _logDAL.InsertLog(type, amount, detail);
        }

        /// <summary>
        /// 获取当日汇总信息
        /// </summary>
        public (decimal totalRevenue, int orderCount, decimal avgOrderAmount) GetTodaySummary()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var logs = _logDAL.GetLogs(today, tomorrow);
            
            var salesLogs = logs.Where(l => l.Type == "销售").ToList();
            var totalRevenue = salesLogs.Sum(l => l.Amount);
            var orderCount = salesLogs.Count;
            var avgOrderAmount = orderCount > 0 ? totalRevenue / orderCount : 0m;
            
            return (totalRevenue, orderCount, avgOrderAmount);
        }
    }
}
