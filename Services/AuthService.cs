using System.Security.Cryptography;
using System.Text;

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
            var user = userName.Trim();
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            var safeUser = SqliteDb.Escape(user);
            var rows = SqliteDb.Query(_dbPath, $"SELECT PasswordHash FROM Users WHERE UserName = '{safeUser}' LIMIT 1");
            if (rows.Count == 0)
            {
                return false;
            }

            var expectedHash = rows[0]["PasswordHash"] ?? string.Empty;
            var actualHash = HashPassword(user, password);
            return string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase);
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
