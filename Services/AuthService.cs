using System.Security.Cryptography;
using System.Text;
using Supermarket.Entities;

namespace Supermarket.Services
{
    public class AuthService
    {
        private readonly string _dbPath;

        public AuthService(string dbPath)
        {
            _dbPath = dbPath;
            SqliteDb.EnsureCreated(_dbPath);
        }

        public bool ValidateLogin(string userName, string password)
        {
            return Authenticate(userName, password, out _);
        }

        public bool Authenticate(string userName, string password, out AppUser? appUser)
        {
            appUser = null;
            var user = userName.Trim();
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            SyncCashierAccounts();

            var safeUser = SqliteDb.Escape(user);
            var rows = SqliteDb.Query(_dbPath, $@"
SELECT u.UserName, u.PasswordHash, u.DisplayName, u.Role, u.CashierId, u.IsActive, c.CashierName
FROM Users u
LEFT JOIN Cashiers c ON c.CashierId = u.CashierId
WHERE u.UserName = '{safeUser}'
LIMIT 1");

            if (rows.Count == 0)
            {
                return false;
            }

            var row = rows[0];
            var expectedHash = row["PasswordHash"] ?? string.Empty;
            var actualHash = HashPassword(user, password);
            if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var isActive = row["IsActive"] != "0";
            if (!isActive)
            {
                return false;
            }

            appUser = new AppUser
            {
                UserName = row["UserName"] ?? user,
                DisplayName = row["DisplayName"] ?? "",
                Role = row["Role"] ?? "Cashier",
                CashierId = row["CashierId"] ?? "",
                CashierName = row["CashierName"] ?? row["DisplayName"] ?? "",
                IsActive = isActive
            };
            return true;
        }

        public bool ChangePassword(string userName, string oldPassword, string newPassword, out string message)
        {
            var user = userName.Trim();
            if (string.IsNullOrWhiteSpace(user))
            {
                message = "\u7528\u6237\u540D\u4E0D\u80FD\u4E3A\u7A7A\u3002";
                return false;
            }

            if (!ValidateLogin(user, oldPassword))
            {
                message = "\u65E7\u5BC6\u7801\u4E0D\u6B63\u786E\u3002";
                return false;
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                message = "\u65B0\u5BC6\u7801\u81F3\u5C11 6 \u4F4D\u3002";
                return false;
            }

            var hash = HashPassword(user, newPassword);
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SqliteDb.ExecuteNonQuery(_dbPath,
                $"UPDATE Users SET PasswordHash = '{SqliteDb.Escape(hash)}', UpdatedAt = '{now}' WHERE UserName = '{SqliteDb.Escape(user)}';");

            message = "\u5BC6\u7801\u4FEE\u6539\u6210\u529F\u3002";
            return true;
        }

        public List<AppUser> GetUsers()
        {
            SyncCashierAccounts();
            return SqliteDb.Query(_dbPath, @"
SELECT u.UserName, u.DisplayName, u.Role, u.CashierId, u.IsActive, COALESCE(c.CashierName, u.DisplayName) AS CashierName
FROM Users u
LEFT JOIN Cashiers c ON c.CashierId = u.CashierId
ORDER BY CASE WHEN u.Role = 'Admin' THEN 0 ELSE 1 END, u.UserName")
                .Select(row => new AppUser
                {
                    UserName = row["UserName"] ?? "",
                    DisplayName = row["DisplayName"] ?? "",
                    Role = row["Role"] ?? "Cashier",
                    CashierId = row["CashierId"] ?? "",
                    CashierName = row["CashierName"] ?? "",
                    IsActive = row["IsActive"] != "0"
                })
                .ToList();
        }

        public bool ResetPassword(string userName, string newPassword, out string message)
        {
            var user = userName.Trim();
            if (string.IsNullOrWhiteSpace(user))
            {
                message = "用户不能为空。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                message = "新密码至少 6 位。";
                return false;
            }

            var safeUser = SqliteDb.Escape(user);
            var exists = SqliteDb.ScalarInt(_dbPath, $"SELECT COUNT(1) FROM Users WHERE UserName = '{safeUser}'");
            if (exists == 0)
            {
                message = "账号不存在。";
                return false;
            }

            var hash = HashPassword(user, newPassword);
            SqliteDb.ExecuteNonQuery(_dbPath,
                $"UPDATE Users SET PasswordHash = '{SqliteDb.Escape(hash)}', UpdatedAt = '{DateTime.Now:yyyy-MM-dd HH:mm:ss}' WHERE UserName = '{safeUser}';");

            message = $"账号 {user} 的密码已重置。";
            return true;
        }

        public bool UpdateDisplayName(string userName, string displayName, out string message)
        {
            var user = userName.Trim();
            var name = displayName.Trim();
            if (string.IsNullOrWhiteSpace(user))
            {
                message = "账号不能为空。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                message = "姓名不能为空。";
                return false;
            }

            var rows = SqliteDb.Query(_dbPath, $"SELECT CashierId FROM Users WHERE UserName = '{SqliteDb.Escape(user)}' LIMIT 1");
            if (rows.Count == 0)
            {
                message = "账号不存在。";
                return false;
            }

            var cashierId = rows[0]["CashierId"] ?? "";
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var sql = new StringBuilder();
            sql.AppendLine("BEGIN;");
            sql.AppendLine($"UPDATE Users SET DisplayName = '{SqliteDb.Escape(name)}', UpdatedAt = '{now}' WHERE UserName = '{SqliteDb.Escape(user)}';");
            if (!string.IsNullOrWhiteSpace(cashierId))
            {
                sql.AppendLine($"UPDATE Cashiers SET CashierName = '{SqliteDb.Escape(name)}' WHERE CashierId = '{SqliteDb.Escape(cashierId)}';");
                sql.AppendLine($"UPDATE Orders SET CashierName = '{SqliteDb.Escape(name)}' WHERE CashierId = '{SqliteDb.Escape(cashierId)}';");
                sql.AppendLine($"UPDATE TransactionLogs SET CashierName = '{SqliteDb.Escape(name)}' WHERE CashierName <> '' AND Detail LIKE '%{SqliteDb.Escape(cashierId)}%';");
            }
            sql.AppendLine("COMMIT;");

            SqliteDb.ExecuteNonQuery(_dbPath, sql.ToString());
            message = "姓名修改成功。";
            return true;
        }

        public void SyncCashierAccounts()
        {
            SqliteDb.EnsureCreated(_dbPath);
        }

        public static string HashPassword(string userName, string password)
        {
            var normalizedUser = userName.Trim().ToLowerInvariant();
            var raw = $"{normalizedUser}:{password}";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes);
        }
    }
}
