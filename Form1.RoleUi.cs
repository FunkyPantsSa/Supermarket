using Supermarket.Entities;
using Supermarket.Services;
using Supermarket.UI;

namespace Supermarket
{
    public partial class Form1
    {
        private readonly AppUser _currentUser;
        private readonly AuthService _authService;
        private readonly string _dbPath = "";
        private readonly Label _lblNavSubtitle = new();
        private readonly TabPage _tabStaffManagement = new("人员管理");
        private StaffManagementPanel? _staffManagementPanel;
        private Button? _btnNavStaffManagement;
        private Button? _btnNavGenerate;

        private bool IsAdmin => _currentUser.IsAdmin;

        private IEnumerable<OrderRecord> VisibleOrders => IsAdmin
            ? _orderService.Orders
            : _orderService.Orders.Where(o => string.Equals(o.CashierId, _currentUser.CashierId, StringComparison.OrdinalIgnoreCase));

        private void InitializeRoleAwareUi()
        {
            InitTransactionLogsTab();
            InitStaffManagementTab();
            ApplyRolePermissions();
        }

        private void InitStaffManagementTab()
        {
            if (!IsAdmin)
            {
                return;
            }

            _staffManagementPanel = new StaffManagementPanel(_authService)
            {
                Dock = DockStyle.Fill
            };
            _tabStaffManagement.Controls.Add(_staffManagementPanel);
            tabMain.TabPages.Add(_tabStaffManagement);

            _btnNavStaffManagement = new Button
            {
                Text = "人员管理",
                Dock = DockStyle.Fill,
                Visible = true
            };
            _btnNavStaffManagement.Click += (s, e) => ShowPage(_tabStaffManagement);
        }

        private void ApplyRolePermissions()
        {
            lblNavTitle.Text = "超市营业系统";
            _lblNavSubtitle.Text = $"{_currentUser.DisplayTitle} · {_currentUser.DisplayRole}";

            if (IsAdmin)
            {
                cmbCashier.Enabled = true;
                cmbCashier.Visible = true;
                lblCashier.Visible = true;
                btnNavProducts.Visible = true;
                btnNavStats.Visible = true;
                btnProductAdd.Enabled = true;
                btnProductEditPrice.Enabled = true;
                btnProductDelete.Enabled = true;
                btnProductAdjustStock.Enabled = true;
                return;
            }

            btnNavProducts.Visible = false;
            btnNavStats.Visible = false;
            btnProductAdd.Enabled = false;
            btnProductEditPrice.Enabled = false;
            btnProductDelete.Enabled = false;
            btnProductAdjustStock.Enabled = false;

            tabMain.TabPages.Remove(tabProducts);
            tabMain.TabPages.Remove(tabStats);
            tabMain.TabPages.Remove(_tabGenerate);
            if (_tabTransactionLogs != null)
            {
                tabMain.TabPages.Remove(_tabTransactionLogs);
            }
            tabMain.TabPages.Remove(_tabStaffManagement);

            txtOrderCashierIdFilter.Text = _currentUser.CashierId;
            txtOrderCashierIdFilter.Enabled = false;
            txtOrderCashierNameFilter.Text = _currentUser.CashierName;
            txtOrderCashierNameFilter.Enabled = false;

            lblCashier.Text = "当前营业员";
            cmbCashier.Enabled = false;
            if (!string.IsNullOrWhiteSpace(_currentUser.CashierId))
            {
                cmbCashier.SelectedValue = _currentUser.CashierId;
            }
        }

        private void ApplyCashierContextToOrder(OrderRecord order)
        {
            if (IsAdmin)
            {
                return;
            }

            order.CashierId = _currentUser.CashierId;
            order.CashierName = _currentUser.CashierName;
        }

        private void EnsureGenerateNavButton()
        {
            if (_btnNavGenerate != null)
            {
                return;
            }

            _btnNavGenerate = new Button
            {
                Text = "订单生成",
                Dock = DockStyle.Fill
            };
            _btnNavGenerate.Click += (s, e) => ShowGeneratePage();
        }

        private List<OrderRecord> GetVisibleOrderList()
        {
            return VisibleOrders
                .OrderByDescending(o => o.CreatedAt)
                .ToList();
        }
    }
}
