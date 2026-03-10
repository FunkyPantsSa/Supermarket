using Supermarket.DAL;
using Supermarket.Entities;

namespace Supermarket.BLL
{
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

        public void AddLog(string type, decimal amount, string detail, string cashierName = "")
        {
            _logDAL.InsertLog(type, amount, detail, cashierName);
        }

        public (decimal totalRevenue, int orderCount, decimal avgOrderAmount) GetTodaySummary()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var logs = _logDAL.GetLogs(today, tomorrow);

            var salesLogs = logs.Where(l => l.Type == "\u9500\u552E").ToList();
            var totalRevenue = salesLogs.Sum(l => l.Amount);
            var orderCount = salesLogs.Count;
            var avgOrderAmount = orderCount > 0 ? totalRevenue / orderCount : 0m;

            return (totalRevenue, orderCount, avgOrderAmount);
        }

        public (decimal totalRevenue, int orderCount, decimal avgOrderAmount) GetMonthSummary()
        {
            var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var end = start.AddMonths(1);
            var logs = _logDAL.GetLogs(start, end);

            var salesLogs = logs.Where(l => l.Type == "\u9500\u552E").ToList();
            var totalRevenue = salesLogs.Sum(l => l.Amount);
            var orderCount = salesLogs.Count;
            var avgOrderAmount = orderCount > 0 ? totalRevenue / orderCount : 0m;

            return (totalRevenue, orderCount, avgOrderAmount);
        }
    }
}
