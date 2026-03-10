using System.Runtime.InteropServices;

namespace Supermarket.Services
{
    public static class SqliteDb
    {
        private const int SQLITE_OK = 0;

        public static void EnsureCreated(string dbPath)
        {
            var dbDir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrWhiteSpace(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }

            ExecuteNonQuery(dbPath, @"
CREATE TABLE IF NOT EXISTS Products (
    ID INTEGER PRIMARY KEY,
    Name TEXT NOT NULL,
    Supplier TEXT NOT NULL,
    Category TEXT NOT NULL,
    Price REAL NOT NULL,
    StockCount INTEGER NOT NULL,
    LowStockThreshold INTEGER NOT NULL DEFAULT 10,
    IsDeleted INTEGER NOT NULL DEFAULT 0
);
CREATE TABLE IF NOT EXISTS Cashiers (
    CashierId TEXT PRIMARY KEY,
    CashierName TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS Orders (
    OrderId TEXT PRIMARY KEY,
    CashierId TEXT NOT NULL,
    CashierName TEXT NOT NULL,
    PayType TEXT NOT NULL DEFAULT '现金',
    DiscountAmount REAL NOT NULL DEFAULT 0,
    ReceivedAmount REAL NOT NULL DEFAULT 0,
    ChangeAmount REAL NOT NULL DEFAULT 0,
    Status TEXT NOT NULL DEFAULT 'Completed',
    CreatedAt TEXT NOT NULL,
    CreatedTimestamp INTEGER NOT NULL,
    TotalAmount REAL NOT NULL
);
CREATE TABLE IF NOT EXISTS OrderItems (
    ItemID INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderID TEXT NOT NULL,
    ProductID INTEGER NOT NULL,
    Quantity INTEGER NOT NULL,
    UnitPriceAtTime REAL NOT NULL,
    ProductName TEXT NOT NULL,
    Supplier TEXT NOT NULL,
    Category TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS TransactionLogs (
    LogID INTEGER PRIMARY KEY AUTOINCREMENT,
    Type TEXT NOT NULL,
    Amount REAL NOT NULL,
    CashierName TEXT NOT NULL DEFAULT '',
    Detail TEXT NOT NULL,
    Timestamp TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS Users (
    UserName TEXT PRIMARY KEY,
    PasswordHash TEXT NOT NULL,
    DisplayName TEXT NOT NULL DEFAULT '',
    Role TEXT NOT NULL DEFAULT 'Cashier',
    CashierId TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1,
    UpdatedAt TEXT NOT NULL
);");

            EnsureColumn(dbPath, "Products", "LowStockThreshold", "INTEGER NOT NULL DEFAULT 10");
            EnsureColumn(dbPath, "Products", "IsDeleted", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(dbPath, "Orders", "PayType", "TEXT NOT NULL DEFAULT '现金'");
            EnsureColumn(dbPath, "Orders", "DiscountAmount", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(dbPath, "Orders", "ReceivedAmount", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(dbPath, "Orders", "ChangeAmount", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(dbPath, "Orders", "Status", "TEXT NOT NULL DEFAULT 'Completed'");
            EnsureColumn(dbPath, "Users", "DisplayName", "TEXT NOT NULL DEFAULT ''");
            EnsureColumn(dbPath, "Users", "Role", "TEXT NOT NULL DEFAULT 'Cashier'");
            EnsureColumn(dbPath, "Users", "CashierId", "TEXT NOT NULL DEFAULT ''");
            EnsureColumn(dbPath, "Users", "IsActive", "INTEGER NOT NULL DEFAULT 1");
            EnsureColumn(dbPath, "TransactionLogs", "CashierName", "TEXT NOT NULL DEFAULT ''");

            var hasOrderItems = ScalarInt(dbPath, "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name='OrderItems'") > 0;
            var hasOrderLines = ScalarInt(dbPath, "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name='OrderLines'") > 0;
            if (hasOrderItems && hasOrderLines)
            {
                var countItems = ScalarInt(dbPath, "SELECT COUNT(1) FROM OrderItems");
                if (countItems == 0)
                {
                    ExecuteNonQuery(dbPath, @"
INSERT INTO OrderItems (OrderID, ProductID, Quantity, UnitPriceAtTime, ProductName, Supplier, Category)
SELECT OrderId, ProductId, Quantity, UnitPrice, ProductName, Supplier, Category FROM OrderLines;");
                }
            }

            SeedDefaultData(dbPath);
            NormalizeText(dbPath);
        }

        private static void SeedDefaultData(string dbPath)
        {
            var productCount = ScalarInt(dbPath, "SELECT COUNT(1) FROM Products WHERE IsDeleted = 0");
            if (productCount == 0)
            {
                ExecuteNonQuery(dbPath, @"
INSERT INTO Products (ID, Name, Supplier, Category, Price, StockCount, LowStockThreshold, IsDeleted) VALUES
(1001, '可乐 330ml', '可口可乐华南', '饮料', 3.50, 120, 20, 0),
(1002, '矿泉水 550ml', '农夫山泉', '饮料', 2.00, 200, 30, 0),
(1003, '全麦面包', '好麦烘焙', '食品', 6.80, 80, 15, 0);");
            }

            var cashierCount = ScalarInt(dbPath, "SELECT COUNT(1) FROM Cashiers");
            if (cashierCount == 0)
            {
                ExecuteNonQuery(dbPath, @"
INSERT INTO Cashiers (CashierId, CashierName) VALUES
('C001', '张明'),
('C002', '李娟'),
('C003', '王磊'),
('C004', '赵雪');");
            }

            var userCount = ScalarInt(dbPath, "SELECT COUNT(1) FROM Users");
            if (userCount == 0)
            {
                var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var defaultHash = AuthService.HashPassword("admin", "123456");
                ExecuteNonQuery(dbPath,
                    $"INSERT INTO Users (UserName, PasswordHash, DisplayName, Role, CashierId, IsActive, UpdatedAt) VALUES ('admin', '{defaultHash}', '系统管理员', 'Admin', '', 1, '{now}');");
            }

            ExecuteNonQuery(dbPath, @"
UPDATE Users
SET DisplayName = CASE
        WHEN UserName = 'admin' AND (DisplayName IS NULL OR DisplayName = '') THEN '系统管理员'
        ELSE DisplayName
    END,
    Role = CASE
        WHEN UserName = 'admin' AND (Role IS NULL OR Role = '' OR Role = 'Cashier') THEN 'Admin'
        ELSE Role
    END,
    CashierId = COALESCE(CashierId, ''),
    IsActive = CASE WHEN IsActive IS NULL OR IsActive = 0 THEN 1 ELSE IsActive END
WHERE UserName = 'admin';");

            SyncCashierUsers(dbPath);
        }

        private static void SyncCashierUsers(string dbPath)
        {
            var rows = Query(dbPath, "SELECT CashierId, CashierName FROM Cashiers ORDER BY CashierId;");
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var sql = new System.Text.StringBuilder();
            sql.AppendLine("BEGIN;");

            foreach (var row in rows)
            {
                var cashierId = row["CashierId"] ?? "";
                var cashierName = row["CashierName"] ?? "";
                if (string.IsNullOrWhiteSpace(cashierId))
                {
                    continue;
                }

                var userName = cashierId.Trim().ToLowerInvariant();
                var defaultHash = AuthService.HashPassword(userName, "123456");
                sql.AppendLine($@"
INSERT INTO Users (UserName, PasswordHash, DisplayName, Role, CashierId, IsActive, UpdatedAt)
SELECT '{Escape(userName)}', '{defaultHash}', '{Escape(cashierName)}', 'Cashier', '{Escape(cashierId)}', 1, '{now}'
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE UserName = '{Escape(userName)}');
UPDATE Users
SET DisplayName = '{Escape(cashierName)}',
    CashierId = '{Escape(cashierId)}',
    Role = CASE WHEN Role IS NULL OR Role = '' THEN 'Cashier' ELSE Role END
WHERE UserName = '{Escape(userName)}';");
            }

            sql.AppendLine("COMMIT;");
            ExecuteNonQuery(dbPath, sql.ToString());
        }

        private static void NormalizeText(string dbPath)
        {
            ExecuteNonQuery(dbPath, @"
BEGIN;
UPDATE Products SET Name='可乐 330ml', Supplier='可口可乐华南', Category='饮料' WHERE ID=1001;
UPDATE Products SET Name='矿泉水 550ml', Supplier='农夫山泉', Category='饮料' WHERE ID=1002;
UPDATE Products SET Name='全麦面包', Supplier='好麦烘焙', Category='食品' WHERE ID=1003;
UPDATE Cashiers SET CashierName='张明' WHERE CashierId='C001';
UPDATE Cashiers SET CashierName='李娟' WHERE CashierId='C002';
UPDATE Cashiers SET CashierName='王磊' WHERE CashierId='C003';
UPDATE Cashiers SET CashierName='赵雪' WHERE CashierId='C004';
COMMIT;");
        }

        private static void EnsureColumn(string dbPath, string tableName, string columnName, string definition)
        {
            var cols = Query(dbPath, $"PRAGMA table_info({tableName});");
            if (cols.Any(c => string.Equals(c["name"], columnName, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            ExecuteNonQuery(dbPath, $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};");
        }

        public static void ExecuteNonQuery(string dbPath, string sql)
        {
            ExecuteInternal(dbPath, sql);
        }

        public static int ScalarInt(string dbPath, string sql)
        {
            var rows = Query(dbPath, sql);
            if (rows.Count == 0 || rows[0].Count == 0) return 0;
            var value = rows[0].Values.FirstOrDefault() ?? "0";
            return int.TryParse(value, out var n) ? n : 0;
        }

        public static List<Dictionary<string, string?>> Query(string dbPath, string sql)
        {
            IntPtr db = IntPtr.Zero;
            Open(dbPath, out db);
            try
            {
                IntPtr resultPtr = IntPtr.Zero;
                IntPtr errPtr = IntPtr.Zero;
                int rows = 0;
                int cols = 0;

                int rc = sqlite3_get_table(db, sql, out resultPtr, out rows, out cols, out errPtr);
                if (rc != SQLITE_OK)
                {
                    var err = PtrToString(errPtr) ?? "sqlite get_table failed";
                    if (errPtr != IntPtr.Zero) sqlite3_free(errPtr);
                    throw new InvalidOperationException(err);
                }

                var result = new List<Dictionary<string, string?>>();
                if (resultPtr == IntPtr.Zero)
                {
                    return result;
                }

                try
                {
                    var values = ReadPointerArray(resultPtr, (rows + 1) * cols);
                    var headers = values.Take(cols).Select(v => v ?? string.Empty).ToList();
                    for (int r = 0; r < rows; r++)
                    {
                        var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                        for (int c = 0; c < cols; c++)
                        {
                            var idx = (r + 1) * cols + c;
                            row[headers[c]] = values[idx];
                        }
                        result.Add(row);
                    }
                }
                finally
                {
                    sqlite3_free_table(resultPtr);
                }

                return result;
            }
            finally
            {
                if (db != IntPtr.Zero) sqlite3_close(db);
            }
        }

        public static string Escape(string value)
        {
            return value.Replace("'", "''");
        }

        private static void ExecuteInternal(string dbPath, string sql)
        {
            IntPtr db = IntPtr.Zero;
            Open(dbPath, out db);
            try
            {
                IntPtr errPtr = IntPtr.Zero;
                int rc = sqlite3_exec(db, sql, IntPtr.Zero, IntPtr.Zero, out errPtr);
                if (rc != SQLITE_OK)
                {
                    var err = PtrToString(errPtr) ?? "sqlite exec failed";
                    if (errPtr != IntPtr.Zero) sqlite3_free(errPtr);
                    throw new InvalidOperationException(err);
                }
            }
            finally
            {
                if (db != IntPtr.Zero) sqlite3_close(db);
            }
        }

        private static void Open(string dbPath, out IntPtr db)
        {
            int rc = sqlite3_open(dbPath, out db);
            if (rc != SQLITE_OK)
            {
                var msg = db != IntPtr.Zero ? PtrToString(sqlite3_errmsg(db)) : "open failed";
                if (db != IntPtr.Zero) sqlite3_close(db);
                throw new InvalidOperationException(msg ?? "sqlite open failed");
            }
        }

        private static List<string?> ReadPointerArray(IntPtr ptr, int count)
        {
            var list = new List<string?>(count);
            int size = IntPtr.Size;
            for (int i = 0; i < count; i++)
            {
                IntPtr p = Marshal.ReadIntPtr(ptr, i * size);
                list.Add(PtrToString(p));
            }
            return list;
        }

        private static string? PtrToString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            return Marshal.PtrToStringUTF8(ptr);
        }

        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_open([MarshalAs(UnmanagedType.LPUTF8Str)] string filename, out IntPtr db);

        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_close(IntPtr db);

        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr sqlite3_errmsg(IntPtr db);

        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_exec(IntPtr db, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, IntPtr callback, IntPtr arg, out IntPtr errMsg);

        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_get_table(IntPtr db, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, out IntPtr result, out int rows, out int columns, out IntPtr errMsg);

        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern void sqlite3_free_table(IntPtr result);

        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
        private static extern void sqlite3_free(IntPtr ptr);
    }
}
