namespace Supermarket.Entities
{
    public sealed class AppUser
    {
        public string UserName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Role { get; set; } = "Cashier";
        public string CashierId { get; set; } = "";
        public string CashierName { get; set; } = "";
        public bool IsActive { get; set; } = true;

        public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);
        public bool IsCashier => string.Equals(Role, "Cashier", StringComparison.OrdinalIgnoreCase);
        public string DisplayRole => IsAdmin ? "管理员" : "营业员";
        public string DisplayTitle => string.IsNullOrWhiteSpace(DisplayName) ? UserName : DisplayName;
    }
}
