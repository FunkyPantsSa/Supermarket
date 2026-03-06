using Supermarket.Entities;

namespace Supermarket.Services
{
    public class CashierService
    {
        private readonly string _dbPath;
        public List<Cashier> Cashiers { get; private set; } = new();

        public CashierService(string dbPath)
        {
            _dbPath = dbPath;
        }

        public void Load()
        {
            SqliteDb.EnsureCreated(_dbPath);
            Cashiers = new List<Cashier>();

            var rows = SqliteDb.Query(_dbPath, "SELECT CashierId, CashierName FROM Cashiers ORDER BY CashierId");
            foreach (var row in rows)
            {
                Cashiers.Add(new Cashier
                {
                    CashierId = row["CashierId"] ?? "",
                    CashierName = row["CashierName"] ?? ""
                });
            }
        }

        public Cashier GetRandom(Random random)
        {
            if (Cashiers.Count == 0)
            {
                return new Cashier { CashierId = "C000", CashierName = "默认收银员" };
            }

            return Cashiers[random.Next(Cashiers.Count)];
        }
    }
}
