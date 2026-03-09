using System.Globalization;
using Supermarket.Entities;

namespace Supermarket.DAL
{
    /// <summary>
    /// 交易流水数据访问层
    /// </summary>
    public class TransactionLogDAL
    {
        private readonly string _dbPath;

        public TransactionLogDAL(string dbPath)
        {
            _dbPath = dbPath;
        }

        public List<TransactionLog> GetLogs(DateTime? start = null, DateTime? end = null)
        {
            Services.SqliteDb.EnsureCreated(_dbPath);
            var sql = new System.Text.StringBuilder(@"
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

            return Services.SqliteDb.Query(_dbPath, sql.ToString())
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

        public void InsertLog(string type, decimal amount, string detail)
        {
            Services.SqliteDb.EnsureCreated(_dbPath);
            var sql = $@"
INSERT INTO TransactionLogs (Type, Amount, Detail, Timestamp)
VALUES ('{Services.SqliteDb.Escape(type)}', {amount.ToString(CultureInfo.InvariantCulture)}, 
        '{Services.SqliteDb.Escape(detail)}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}');";
            
            Services.SqliteDb.ExecuteNonQuery(_dbPath, sql);
        }
    }
}
