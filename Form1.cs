using System.ComponentModel;
using System.Drawing.Printing;
using System.Text;
using Supermarket.Entities;
using Supermarket.Services;
using Supermarket.BLL;
using Supermarket.UI;

namespace Supermarket
{
    public partial class Form1 : Form
    {
        private readonly InventoryService _inventoryService;
        private readonly OrderService _orderService;
        private readonly CashierService _cashierService;
        private readonly ProductBLL _productBLL;
        private readonly OrderBLL _orderBLL;
        private readonly StatisticsBLL _statisticsBLL;
        private readonly TransactionLogBLL _transactionLogBLL;
        private TabPage _tabTransactionLogs = null!;
        private TransactionLogPanel? _transactionLogPanel;
        private Button? _btnNavTransactionLogs;
        private readonly TableLayoutPanel _navButtonLayout = new();
        private readonly BindingList<OrderItem> _cart = new();
        private readonly Random _random = new();
        private readonly PrintDocument _orderPrintDocument = new();
        private readonly List<OrderRecord> _printingOrders = new();
        private int _printingOrderIndex;
        private int _printingLineIndex;
        private List<OrderRecord> _currentOrderView = new();
        private readonly HashSet<string> _checkedOrderIds = new();
        private bool _isApplyingOrderChecks;
        private readonly CheckBox _chkOrderSelectAll = new();
        private readonly DataGridView _dgvStatsSummary = new();
        private readonly ComboBox _cmbPayType = new();
        private readonly NumericUpDown _numDiscount = new();
        private readonly NumericUpDown _numReceived = new();
        private readonly Label _lblChangeAmount = new();
        private readonly Button _btnVoidOrder = new();
        private readonly TextBox _txtProductNameSearch = new();
        private readonly ListBox _lstProductMatches = new();
        private readonly Button _btnAddByName = new();
        private readonly List<Product> _productNameMatches = new();
        private readonly Button _btnStatToday = new();
        private readonly Button _btnStatThisMonth = new();
        private readonly Button _btnStatLastMonth = new();
        private readonly Button _btnStatAll = new();
        private readonly TabPage _tabGenerate = new("订单生成");
        private readonly Button _btnCloseGenerate = new();
        private int _titleClickCount;
        private bool _generatePageUnlocked;

        public Form1(AppUser currentUser, AuthService authService)
        {
            _currentUser = currentUser;
            _authService = authService;
            InitializeComponent();
            KeyPreview = true;

            var dbDir = Path.Combine(AppContext.BaseDirectory, "db");
            _dbPath = Path.Combine(dbDir, "supermarket.db");
            Text = $"超市营业管理系统 - {_currentUser.DisplayTitle}（{_currentUser.DisplayRole}）";

            _inventoryService = new InventoryService(_dbPath);
            _inventoryService.Load();

            _orderService = new OrderService(_dbPath);
            _orderService.Load();
            _cashierService = new CashierService(_dbPath);
            _cashierService.Load();

            // 初始化BLL层
            _productBLL = new ProductBLL(_dbPath);
            _orderBLL = new OrderBLL(_dbPath);
            _statisticsBLL = new StatisticsBLL(_dbPath);
            _transactionLogBLL = new TransactionLogBLL(_dbPath);

            InitCartGrid();
            InitProductGrid();
            InitOrderGrid();
            InitStatsGrid();
            InitStatsSummaryGrid();
            InitCashierSelector();
            InitCheckoutExtension();
            ConfigureAutoLayouts();
            SetupHiddenGeneratePage();
            ApplyUniformButtonSize();
            WireEvents();
            _orderPrintDocument.PrintPage += OrderPrintDocument_PrintPage;

            dtpStatStart.Value = DateTime.Today;
            dtpStatEnd.Value = DateTime.Now;
            dtpGenStart.Value = DateTime.Today;
            dtpGenEnd.Value = DateTime.Now;
            dtpOrderStart.Value = DateTime.Today;
            dtpOrderEnd.Value = DateTime.Now;

            RefreshProductGrid();
            RefreshOrderGrid();
            RefreshStatsPage();
            UpdateTotalAmount();

            InitializeRoleAwareUi();
            ConfigureNavLayout();
            ApplyNavStyles();
            ShowPage(tabCashier);
        }

        private void WireEvents()
        {
            btnAddToCart.Click += btnAddToCart_Click;
            txtProductId.KeyDown += txtProductId_KeyDown;
            _txtProductNameSearch.TextChanged += (s, e) => UpdateProductNameMatches();
            _txtProductNameSearch.KeyDown += txtProductNameSearch_KeyDown;
            _btnAddByName.Click += btnAddByName_Click;
            _lstProductMatches.DoubleClick += (s, e) => AddSelectedMatchToCart();
            btnCheckout.Click += btnCheckout_Click;
            btnClearCart.Click += btnClearCart_Click;
            dgvCart.KeyDown += dgvCart_KeyDown;
            KeyDown += Form1_KeyDown;

            btnNavCashier.Click += (s, e) => ShowPage(tabCashier);
            btnNavProducts.Click += (s, e) => ShowPage(tabProducts);
            btnNavOrders.Click += (s, e) => ShowPage(tabOrders);
            btnNavStats.Click += (s, e) =>
            {
                ShowPage(tabStats);
                RefreshStatsPage();
            };
            
            lblNavTitle.Click += lblNavTitle_Click;

            btnProductAdd.Click += btnProductAdd_Click;
            btnProductEditPrice.Click += btnProductEditPrice_Click;
            btnProductDelete.Click += btnProductDelete_Click;
            btnProductRefresh.Click += (s, e) => RefreshProductGrid();
            btnProductFilterCategory.Click += btnProductFilterCategory_Click;
            btnProductFilterSupplier.Click += btnProductFilterSupplier_Click;
            btnProductClearFilter.Click += (s, e) =>
            {
                txtProductCategoryFilter.Text = "";
                txtProductSupplierFilter.Text = "";
                RefreshProductGrid();
            };
            btnProductApplyInputFilter.Click += btnProductApplyInputFilter_Click;
            btnProductAdjustStock.Click += btnProductAdjustStock_Click;
            dgvProducts.CellDoubleClick += dgvProducts_CellDoubleClick;

            btnOrderRefresh.Click += (s, e) => RefreshOrderGrid();
            btnOrderPrint.Click += btnOrderPrint_Click;
            _btnVoidOrder.Click += btnVoidOrder_Click;
            btnOrderToday.Click += (s, e) => ApplyOrderQuickRange(DateTime.Today, DateTime.Now, "今日收入");
            btnOrderYesterday.Click += (s, e) =>
            {
                var d = DateTime.Today.AddDays(-1);
                ApplyOrderQuickRange(d, d.AddDays(1).AddTicks(-1), "昨日收入");
            };
            btnOrderMonth.Click += (s, e) =>
            {
                var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                ApplyOrderQuickRange(start, DateTime.Now, "本月收入");
            };
            btnOrderApplyFilter.Click += (s, e) => ApplyOrderAdvancedFilter();
            btnOrderClearFilter.Click += (s, e) =>
            {
                txtOrderIdFilter.Text = "";
                txtOrderCashierIdFilter.Text = "";
                txtOrderCashierNameFilter.Text = "";
                dtpOrderStart.Value = DateTime.Today;
                dtpOrderEnd.Value = DateTime.Now;
                _chkOrderSelectAll.Checked = false;
                RefreshOrderGrid();
            };
            dgvOrders.SelectionChanged += dgvOrders_SelectionChanged;

            btnApplyStatsFilter.Click += (s, e) => ApplyStatsFilter();
            btnStatsClearFilter.Click += (s, e) =>
            {
                dtpStatStart.Value = DateTime.Today;
                dtpStatEnd.Value = DateTime.Now;
                ApplyStatsFilter();
            };
            _btnStatToday.Click += (s, e) => ApplyStatQuickRange(DateTime.Today, DateTime.Now);
            _btnStatThisMonth.Click += (s, e) =>
            {
                var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                ApplyStatQuickRange(start, DateTime.Now);
            };
            _btnStatLastMonth.Click += (s, e) =>
            {
                var thisMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var lastMonthStart = thisMonthStart.AddMonths(-1);
                var lastMonthEnd = thisMonthStart.AddTicks(-1);
                ApplyStatQuickRange(lastMonthStart, lastMonthEnd);
            };
            _btnStatAll.Click += (s, e) =>
            {
                var all = _orderService.Orders.OrderBy(o => o.CreatedAt).ToList();
                if (all.Count == 0)
                {
                    ApplyStatQuickRange(DateTime.Today, DateTime.Now);
                    return;
                }
                ApplyStatQuickRange(all.First().CreatedAt, all.Last().CreatedAt);
            };
            btnGenerateOrders.Click += btnGenerateOrders_Click;
        }

        private void lblNavTitle_Click(object? sender, EventArgs e)
        {
            if (!IsAdmin) return;
            if (_btnNavGenerate?.Visible == true)
            {
                ShowGeneratePage();
                return;
            }
            _titleClickCount++;
            if (_titleClickCount < 5) return;
            _titleClickCount = 0;

            UnlockGeneratePage();
            MessageBox.Show("订单生成页面已解锁。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ConfigureAutoLayouts()
        {
            ConfigureCashierLayouts();
            ConfigureProductTopLayout();
            ConfigureOrderTopLayout();
            ConfigureStatsTopLayout();
            ConfigureStatsBodyLayout();
            ConfigureFormVisuals();
        }

        private void ConfigureCashierLayouts()
        {
            panelCashierLeft.Controls.Clear();
            panelCashierRight.Controls.Clear();
            panelCashierLeft.Width = 320;
            panelCashierRight.Width = 320;
            panelCashierCenter.Padding = new Padding(12, 0, 12, 0);

            var leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 12,
                Padding = new Padding(16)
            };
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            lblInputTitle.Text = "商品输入";
            lblInputTitle.Dock = DockStyle.Fill;
            lblId.Text = "商品ID / 条码";
            var lblNameSearch = new Label { Text = "商品名称模糊搜索", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, ForeColor = AppTheme.TextPrimary };
            var lblMatches = new Label { Text = "匹配商品", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, ForeColor = AppTheme.TextPrimary };
            lblQty.Text = "数量";

            txtProductId.Dock = DockStyle.Fill;
            _txtProductNameSearch.Dock = DockStyle.Fill;
            _txtProductNameSearch.PlaceholderText = "输入商品名关键字，例如：可乐";
            _lstProductMatches.Dock = DockStyle.Fill;
            _lstProductMatches.IntegralHeight = false;
            _lstProductMatches.Height = 96;
            numQuantity.Dock = DockStyle.Left;
            numQuantity.Width = 260;
            cmbCashier.Dock = DockStyle.Fill;
            btnAddToCart.Text = "按编码加入";
            btnAddToCart.Height = 40;
            _btnAddByName.Text = "按名称加入";
            _btnAddByName.Height = 40;

            var actionRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2
            };
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actionRow.Controls.Add(btnAddToCart, 0, 0);
            actionRow.Controls.Add(_btnAddByName, 1, 0);

            leftLayout.Controls.Add(lblInputTitle, 0, 0);
            leftLayout.Controls.Add(lblId, 0, 1);
            leftLayout.Controls.Add(txtProductId, 0, 2);
            leftLayout.Controls.Add(lblNameSearch, 0, 3);
            leftLayout.Controls.Add(_txtProductNameSearch, 0, 4);
            leftLayout.Controls.Add(lblMatches, 0, 5);
            leftLayout.Controls.Add(_lstProductMatches, 0, 6);
            leftLayout.Controls.Add(lblQty, 0, 7);
            leftLayout.Controls.Add(numQuantity, 0, 8);
            leftLayout.Controls.Add(lblCashier, 0, 9);
            leftLayout.Controls.Add(cmbCashier, 0, 10);
            leftLayout.Controls.Add(actionRow, 0, 11);
            panelCashierLeft.Controls.Add(leftLayout);

            AppTheme.StyleInput(_txtProductNameSearch);
            AppTheme.StyleInput(_lstProductMatches);

            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 10,
                Padding = new Padding(16)
            };
            rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            lblTotalAmount.Dock = DockStyle.Fill;
            lblTotalAmount.AutoSize = false;
            lblTotalAmount.TextAlign = ContentAlignment.MiddleLeft;

            _cmbPayType.Dock = DockStyle.Fill;
            _numDiscount.Dock = DockStyle.Fill;
            _numReceived.Dock = DockStyle.Fill;
            _lblChangeAmount.Dock = DockStyle.Fill;
            _lblChangeAmount.TextAlign = ContentAlignment.MiddleLeft;
            btnCheckout.Dock = DockStyle.Fill;
            btnClearCart.Dock = DockStyle.Top;
            btnClearCart.Height = 38;

            rightLayout.Controls.Add(lblTotalAmount, 0, 0);
            rightLayout.Controls.Add(new Label { Text = "支付方式", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, ForeColor = AppTheme.TextPrimary }, 0, 1);
            rightLayout.Controls.Add(_cmbPayType, 0, 2);
            rightLayout.Controls.Add(new Label { Text = "手动折扣", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, ForeColor = AppTheme.TextPrimary }, 0, 3);
            rightLayout.Controls.Add(_numDiscount, 0, 4);
            rightLayout.Controls.Add(new Label { Text = "实收金额", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, ForeColor = AppTheme.TextPrimary }, 0, 5);
            rightLayout.Controls.Add(_numReceived, 0, 6);
            rightLayout.Controls.Add(_lblChangeAmount, 0, 7);
            rightLayout.Controls.Add(btnCheckout, 0, 8);
            rightLayout.Controls.Add(btnClearCart, 0, 9);
            panelCashierRight.Controls.Add(rightLayout);
        }

        private void ConfigureFormVisuals()
        {
            AppTheme.StyleForm(this);
            panelMain.Padding = new Padding(12);
            panelMain.BackColor = AppTheme.Surface;
            tabMain.Appearance = TabAppearance.FlatButtons;
            tabMain.ItemSize = new Size(0, 1);
            tabMain.SizeMode = TabSizeMode.Fixed;

            foreach (var panel in new Control[] { panelCashierLeft, panelCashierCenter, panelCashierRight, panelProductTop, panelOrderTop, panelStatsTop, panelOrderDetailContainer, panelOrderDetailTop })
            {
                AppTheme.StyleCard(panel);
            }

            lblInputTitle.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            lblInputTitle.ForeColor = AppTheme.TextPrimary;
            lblTotalAmount.Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold);
            lblTotalAmount.ForeColor = AppTheme.AccentDark;
            _lblChangeAmount.ForeColor = AppTheme.Success;

            foreach (var grid in new[] { dgvCart, dgvProducts, dgvOrders, dgvOrderItems, dgvStatsOrders, _dgvStatsSummary })
            {
                AppTheme.StyleGrid(grid);
            }

            foreach (var input in EnumerateControls(this).Where(c => c is TextBox or ComboBox or NumericUpDown or DateTimePicker))
            {
                AppTheme.StyleInput(input);
            }

            foreach (var button in EnumerateControls(this).OfType<Button>())
            {
                var primary = button == btnCheckout || button == btnAddToCart || button == btnApplyStatsFilter || button == btnOrderApplyFilter || button == btnProductApplyInputFilter;
                var danger = button == _btnVoidOrder;
                AppTheme.StyleButton(button, primary, danger);
            }
        }

        private void ConfigureProductTopLayout()
        {
            panelProductTop.Controls.Clear();
            panelProductTop.Height = 120;
            SetWideButton(btnProductClearFilter);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(8)
            };
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var row1 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
            row1.Controls.AddRange(new Control[] { btnProductAdd, btnProductEditPrice, btnProductDelete, btnProductRefresh, btnProductAdjustStock });

            var row2 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
            row2.Controls.AddRange(new Control[]
            {
                btnProductFilterCategory, btnProductFilterSupplier, btnProductClearFilter,
                lblProductCategoryFilter, txtProductCategoryFilter, lblProductSupplierFilter, txtProductSupplierFilter, btnProductApplyInputFilter
            });

            table.Controls.Add(row1, 0, 0);
            table.Controls.Add(row2, 0, 1);
            panelProductTop.Controls.Add(table);
        }

        private void ConfigureOrderTopLayout()
        {
            panelOrderTop.Controls.Clear();
            panelOrderTop.Height = 168;
            SetWideButton(btnOrderClearFilter);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(8)
            };
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var row1 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
            _btnVoidOrder.Text = "\u4F5C\u5E9F\u9000\u5355";
            SetWideButton(_btnVoidOrder);
            row1.Controls.AddRange(new Control[] { lblOrderSummary, lblOrderIncome, btnOrderToday, btnOrderYesterday, btnOrderMonth, btnOrderPrint, _btnVoidOrder, btnOrderRefresh });

            var row2 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
            row2.Controls.AddRange(new Control[] { lblOrderIdFilter, txtOrderIdFilter, lblOrderCashierIdFilter, txtOrderCashierIdFilter, lblOrderCashierNameFilter, txtOrderCashierNameFilter });

            var row3 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
            _chkOrderSelectAll.Text = "全选订单";
            _chkOrderSelectAll.AutoSize = true;
            row3.Controls.AddRange(new Control[] { lblOrderTimeFrom, dtpOrderStart, lblOrderTimeTo, dtpOrderEnd, btnOrderApplyFilter, btnOrderClearFilter, _chkOrderSelectAll });

            table.Controls.Add(row1, 0, 0);
            table.Controls.Add(row2, 0, 1);
            table.Controls.Add(row3, 0, 2);
            panelOrderTop.Controls.Add(table);

            _chkOrderSelectAll.CheckedChanged += chkOrderSelectAll_CheckedChanged;
        }

        private void ConfigureStatsTopLayout()
        {
            panelStatsTop.Controls.Clear();
            panelStatsTop.Height = 120;
            _btnStatToday.Text = "今日数据";
            _btnStatThisMonth.Text = "本月数据";
            _btnStatLastMonth.Text = "上月数据";
            _btnStatAll.Text = "所有数据";
            SetWideButton(_btnStatToday);
            SetWideButton(_btnStatThisMonth);
            SetWideButton(_btnStatLastMonth);
            SetWideButton(_btnStatAll);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(8)
            };
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var row1 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
            row1.Controls.AddRange(new Control[] { lblTodayRevenue, lblFilterSummary, _btnStatToday, _btnStatThisMonth, _btnStatLastMonth, _btnStatAll, btnStatsClearFilter });

            var row2 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
            row2.Controls.AddRange(new Control[] { lblStatStart, dtpStatStart, lblStatEnd, dtpStatEnd, btnApplyStatsFilter });

            table.Controls.Add(row1, 0, 0);
            table.Controls.Add(row2, 0, 1);
            panelStatsTop.Controls.Add(table);
        }

        private void ConfigureStatsBodyLayout()
        {
            tabStats.Controls.Clear();

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));

            panelStatsTop.Dock = DockStyle.Fill;
            dgvStatsOrders.Dock = DockStyle.Fill;
            _dgvStatsSummary.Dock = DockStyle.Fill;

            table.Controls.Add(panelStatsTop, 0, 0);
            table.Controls.Add(dgvStatsOrders, 0, 1);
            table.Controls.Add(_dgvStatsSummary, 0, 2);
            tabStats.Controls.Add(table);
        }
        
        private void InitTransactionLogsTab()
        {
            if (!IsAdmin)
            {
                return;
            }

            _tabTransactionLogs = new TabPage("收银流水");
            _transactionLogPanel = new TransactionLogPanel(_dbPath);
            _transactionLogPanel.Dock = DockStyle.Fill;
            _transactionLogPanel.OrderDetailRequested += orderId => NavigateToOrderManagement(orderId);
            _tabTransactionLogs.Controls.Add(_transactionLogPanel);

            tabMain.Controls.Add(_tabTransactionLogs);

            _btnNavTransactionLogs = new Button
            {
                Text = "收银流水",
                Dock = DockStyle.Fill
            };
            _btnNavTransactionLogs.Click += (s, e) =>
            {
                ShowPage(_tabTransactionLogs);
                RefreshTransactionLogsPage();
            };
        }
        
        private void RefreshTransactionLogsPage()
        {
            _transactionLogPanel?.LoadData();
        }

        private static void SetWideButton(Button button)
        {
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.MinimumSize = new Size(118, 34);
            button.Padding = new Padding(6, 2, 6, 2);
        }

        private void ApplyUniformButtonSize()
        {
            foreach (var button in EnumerateControls(this).OfType<Button>())
            {
                button.AutoSize = false;
                button.MinimumSize = new Size(118, 34);
                button.Padding = new Padding(6, 2, 6, 2);
                if (button.Width < 118) button.Width = 118;
                if (button.Height < 34) button.Height = 34;
            }
        }

        private static IEnumerable<Control> EnumerateControls(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (var nested in EnumerateControls(child))
                {
                    yield return nested;
                }
            }
        }

        private void SetupHiddenGeneratePage()
        {
            _tabGenerate.Controls.Clear();
            _tabGenerate.BackColor = AppTheme.Surface;

            var card = new Panel
            {
                Dock = DockStyle.Top,
                Height = 180,
                Padding = new Padding(18),
                Margin = new Padding(18),
                BackColor = AppTheme.Card
            };

            var title = AppTheme.CreateSectionTitle("订单生成");
            title.Dock = DockStyle.Top;

            var description = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Text = "管理员可按金额和单量快速生成演示订单，关闭后仍可从左侧导航重新进入。",
                ForeColor = AppTheme.TextSecondary
            };

            var row = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = true,
                AutoScroll = true,
                Padding = new Padding(0, 10, 0, 0)
            };

            _btnCloseGenerate.Text = "关闭页面";
            _btnCloseGenerate.Click += (s, e) => CloseGeneratePage();

            row.Controls.AddRange(new Control[]
            {
                lblGenStart, dtpGenStart, lblGenEnd, dtpGenEnd, lblGenerateAmount, txtGenerateAmount,
                lblGenerateCount, txtGenerateCount, btnGenerateOrders, _btnCloseGenerate
            });

            card.Controls.Add(row);
            card.Controls.Add(description);
            card.Controls.Add(title);
            _tabGenerate.Controls.Add(card);
        }

        private void UnlockGeneratePage()
        {
            if (_generatePageUnlocked)
            {
                ShowGeneratePage();
                return;
            }

            _generatePageUnlocked = true;
            EnsureGenerateNavButton();
            if (!tabMain.TabPages.Contains(_tabGenerate))
            {
                tabMain.TabPages.Add(_tabGenerate);
            }
            if (_btnNavGenerate != null)
            {
                _btnNavGenerate.Visible = true;
            }
            ConfigureNavLayout();
            ApplyNavStyles();
            ShowGeneratePage();
        }

        private void ShowGeneratePage()
        {
            if (!_generatePageUnlocked)
            {
                return;
            }

            if (!tabMain.TabPages.Contains(_tabGenerate))
            {
                tabMain.TabPages.Add(_tabGenerate);
            }
            if (_btnNavGenerate != null)
            {
                _btnNavGenerate.Visible = true;
            }

            ConfigureNavLayout();
            ApplyNavStyles();
            ShowPage(_tabGenerate);
        }

        private void CloseGeneratePage()
        {
            if (tabMain.TabPages.Contains(_tabGenerate))
            {
                tabMain.TabPages.Remove(_tabGenerate);
            }
            if (_btnNavGenerate != null)
            {
                _btnNavGenerate.Visible = false;
            }
            _generatePageUnlocked = false;
            _titleClickCount = 0;

            ConfigureNavLayout();
            ApplyNavStyles();
            ShowPage(tabCashier);
        }

        private void ShowPage(TabPage page)
        {
            tabMain.SelectedTab = page;
            UpdateNavActiveState(page);
        }

        private void ConfigureNavLayout()
        {
            panelNav.Controls.Clear();
            panelNav.Width = 236;
            panelNav.Padding = new Padding(16, 18, 16, 14);
            panelNav.BackColor = Color.FromArgb(18, 31, 48);

            lblNavTitle.Dock = DockStyle.Top;
            lblNavTitle.AutoSize = false;
            lblNavTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblNavTitle.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            lblNavTitle.ForeColor = Color.FromArgb(240, 246, 255);
            lblNavTitle.Margin = new Padding(4, 0, 4, 2);
            lblNavTitle.Height = 34;

            _lblNavSubtitle.Dock = DockStyle.Top;
            _lblNavSubtitle.AutoSize = false;
            _lblNavSubtitle.TextAlign = ContentAlignment.MiddleLeft;
            _lblNavSubtitle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            _lblNavSubtitle.ForeColor = Color.FromArgb(169, 189, 213);
            _lblNavSubtitle.Height = 26;
            _lblNavSubtitle.Margin = new Padding(4, 0, 4, 14);

            _navButtonLayout.Dock = DockStyle.Top;
            _navButtonLayout.ColumnCount = 1;
            var buttons = new List<Button>();
            if (btnNavCashier.Visible) buttons.Add(btnNavCashier);
            if (btnNavProducts.Visible) buttons.Add(btnNavProducts);
            buttons.Add(btnNavOrders);
            if (_btnNavGenerate?.Visible == true && _generatePageUnlocked) buttons.Add(_btnNavGenerate);
            if (_btnNavTransactionLogs?.Visible == true) buttons.Add(_btnNavTransactionLogs);
            if (btnNavStats.Visible) buttons.Add(btnNavStats);
            if (_btnNavStaffManagement?.Visible == true) buttons.Add(_btnNavStaffManagement);

            _navButtonLayout.RowCount = buttons.Count;
            _navButtonLayout.Height = buttons.Count * 56;
            _navButtonLayout.Padding = new Padding(0);
            _navButtonLayout.Margin = new Padding(0);
            _navButtonLayout.BackColor = Color.Transparent;
            _navButtonLayout.ColumnStyles.Clear();
            _navButtonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _navButtonLayout.RowStyles.Clear();
            for (var i = 0; i < buttons.Count; i++)
            {
                _navButtonLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            }

            foreach (var btn in buttons)
            {
                btn.Dock = DockStyle.Fill;
                btn.Margin = new Padding(0, 0, 0, 10);
            }

            _navButtonLayout.Controls.Clear();
            for (var i = 0; i < buttons.Count; i++)
            {
                _navButtonLayout.Controls.Add(buttons[i], 0, i);
            }

            panelNav.Controls.Add(_navButtonLayout);
            panelNav.Controls.Add(_lblNavSubtitle);
            panelNav.Controls.Add(lblNavTitle);
        }

        private void ApplyNavStyles()
        {
            var navTitles = new HashSet<string> { "收银台", "商品管理", "订单管理", "订单生成", "收银流水", "统计页面", "人员管理" };
            var navButtons = EnumerateControls(panelNav)
                .OfType<Button>()
                .Where(b => navTitles.Contains(b.Text))
                .ToList();
            
            foreach (var btn in navButtons)
            {
                btn.ForeColor = Color.FromArgb(229, 238, 248);
                btn.BackColor = Color.FromArgb(33, 51, 73);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(43, 69, 99);
                btn.TextAlign = ContentAlignment.MiddleLeft;
                btn.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
                btn.Cursor = Cursors.Hand;
                btn.Padding = new Padding(18, 0, 0, 0);
            }

            UpdateNavActiveState(tabMain.SelectedTab ?? tabCashier);
        }

        private void UpdateNavActiveState(TabPage activePage)
        {
            var activeColor = Color.FromArgb(37, 111, 197);
            var normalColor = Color.FromArgb(33, 51, 73);
            var normalText = Color.FromArgb(229, 238, 248);

            var pairs = new List<(Button? Btn, TabPage Page)>
            {
                (btnNavCashier, tabCashier),
                (btnNavProducts, tabProducts),
                (btnNavOrders, tabOrders),
                (_btnNavGenerate, _tabGenerate),
                (_btnNavTransactionLogs, _tabTransactionLogs),
                (btnNavStats, tabStats),
                (_btnNavStaffManagement, _tabStaffManagement)
            };

            foreach (var pair in pairs.Where(p => p.Btn != null))
            {
                var btn = pair.Btn!;
                var isActive = ReferenceEquals(pair.Page, activePage);
                btn.BackColor = isActive ? activeColor : normalColor;
                btn.ForeColor = normalText;
            }
        }

        private void txtProductId_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            btnAddToCart_Click(sender, EventArgs.Empty);
            txtProductId.SelectAll();
        }

        private void txtProductNameSearch_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            AddSelectedMatchToCart();
        }

        private void InitCashierSelector()
        {
            var cashiers = _cashierService.Cashiers.ToList();
            if (cashiers.Count == 0)
            {
                cashiers.Add(new Cashier { CashierId = "C000", CashierName = "默认收银员" });
            }

            cmbCashier.DataSource = cashiers;
            cmbCashier.DisplayMember = nameof(Cashier.CashierName);
            cmbCashier.ValueMember = nameof(Cashier.CashierId);
            cmbCashier.DropDownWidth = 220;
            if (!string.IsNullOrWhiteSpace(_currentUser.CashierId) && cashiers.Any(c => c.CashierId == _currentUser.CashierId))
            {
                cmbCashier.SelectedValue = _currentUser.CashierId;
            }
            else
            {
                cmbCashier.SelectedIndex = 0;
            }
        }

        private void InitCheckoutExtension()
        {
            _cmbPayType.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbPayType.Items.AddRange(new object[] { "现金", "扫码" });
            _cmbPayType.SelectedIndex = 0;

            _numDiscount.DecimalPlaces = 2;
            _numDiscount.Maximum = 999999;
            _numDiscount.ValueChanged += (s, e) => UpdateTotalAmount();

            _numReceived.DecimalPlaces = 2;
            _numReceived.Maximum = 999999;
            _numReceived.ValueChanged += (s, e) => UpdateTotalAmount();

            _lblChangeAmount.Text = "\u627E\u96F6\uFF1A0.00";
        }

        private void InitCartGrid()
        {
            dgvCart.AutoGenerateColumns = false;
            dgvCart.ReadOnly = false;
            dgvCart.DataSource = _cart;
            dgvCart.Columns.Clear();
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "名称", Name = "colName", Width = 180 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "供货商", Name = "colSupplier", Width = 120 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "分类", Name = "colCategory", Width = 90 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "单价", Name = "colPrice", Width = 80 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "数量", DataPropertyName = "Quantity", Name = "colQuantity", Width = 70 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "小计", Name = "colSubTotal", Width = 90 });
            dgvCart.Columns.Add(new DataGridViewButtonColumn { HeaderText = "操作", Name = "colRemove", Text = "删除", UseColumnTextForButtonValue = true, Width = 72 });

            dgvCart.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _cart.Count) return;
                var item = _cart[e.RowIndex];
                var colName = dgvCart.Columns[e.ColumnIndex].Name;

                if (colName == "colName") e.Value = item.Product.Name;
                else if (colName == "colSupplier") e.Value = item.Product.Supplier;
                else if (colName == "colCategory") e.Value = item.Product.Category;
                else if (colName == "colPrice") e.Value = item.Product.Price.ToString("F2");
                else if (colName == "colSubTotal") e.Value = item.SubTotal.ToString("F2");
            };
            dgvCart.CellContentClick += dgvCart_CellContentClick;
            dgvCart.CellEndEdit += dgvCart_CellEndEdit;
        }

        private void dgvCart_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvCart.Columns[e.ColumnIndex].Name != "colRemove") return;
            if (e.RowIndex >= _cart.Count) return;

            _cart.RemoveAt(e.RowIndex);
            dgvCart.Refresh();
            UpdateTotalAmount();
        }

        private void dgvCart_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _cart.Count) return;
            if (dgvCart.Columns[e.ColumnIndex].Name != "colQuantity") return;

            var cellValue = dgvCart.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
            if (!int.TryParse(cellValue, out var quantity) || quantity <= 0)
            {
                quantity = 1;
            }

            var item = _cart[e.RowIndex];
            quantity = Math.Min(quantity, item.Product.StockCount);
            item.Quantity = quantity;
            dgvCart.Refresh();
            UpdateTotalAmount();
        }

        private void InitProductGrid()
        {
            dgvProducts.AutoGenerateColumns = false;
            dgvProducts.Columns.Clear();
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "商品ID", DataPropertyName = "ID", Name = "colPid", Width = 90 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "名称", DataPropertyName = "Name", Name = "colPName", Width = 160 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "供货商", DataPropertyName = "Supplier", Name = "colPSupplier", Width = 130 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "分类", DataPropertyName = "Category", Name = "colPCategory", Width = 110 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "价格", DataPropertyName = "Price", Name = "colPPrice", Width = 90 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "库存", DataPropertyName = "StockCount", Name = "colPStock", Width = 90 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "预警阈值", DataPropertyName = "LowStockThreshold", Name = "colPLow", Width = 100 });

            dgvProducts.RowPrePaint += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgvProducts.Rows.Count) return;
                if (dgvProducts.Rows[e.RowIndex].DataBoundItem is not Product p) return;
                var low = p.StockCount <= p.LowStockThreshold;
                dgvProducts.Rows[e.RowIndex].DefaultCellStyle.BackColor = low ? Color.MistyRose : Color.White;
                dgvProducts.Rows[e.RowIndex].DefaultCellStyle.ForeColor = low ? Color.DarkRed : Color.Black;
            };
        }

        private void InitOrderGrid()
        {
            dgvOrders.MultiSelect = true;
            dgvOrders.AutoGenerateColumns = false;
            dgvOrders.ReadOnly = false;
            dgvOrders.EditMode = DataGridViewEditMode.EditOnEnter;
            dgvOrders.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 179, 0);
            dgvOrders.DefaultCellStyle.SelectionForeColor = Color.FromArgb(28, 28, 28);
            dgvOrders.RowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 179, 0);
            dgvOrders.RowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(28, 28, 28);
            dgvOrders.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 179, 0);
            dgvOrders.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(28, 28, 28);
            dgvOrders.Columns.Clear();
            btnOrderPrint.Text = "打印所选订单";
            btnOrderPrint.Enabled = false;
            dgvOrders.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "选择", Name = "colPick", Width = 56, ReadOnly = false, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "订单号", DataPropertyName = "OrderId", Width = 180, ReadOnly = true });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "收银员ID", DataPropertyName = "CashierId", Width = 90, ReadOnly = true });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "收银员", DataPropertyName = "CashierName", Width = 100, ReadOnly = true });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "支付方式", DataPropertyName = "PayType", Width = 90, ReadOnly = true });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "状态", DataPropertyName = "Status", Width = 90, ReadOnly = true });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "时间", DataPropertyName = "CreatedAt", Width = 145, ReadOnly = true });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "时间戳", DataPropertyName = "CreatedTimestamp", Width = 120, ReadOnly = true });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "商品数", Name = "colItemCount", Width = 70, ReadOnly = true });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "总金额", DataPropertyName = "TotalAmount", Width = 100, ReadOnly = true });

            dgvOrders.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgvOrders.Rows.Count) return;
                var colName = dgvOrders.Columns[e.ColumnIndex].Name;

                if (colName == "colItemCount")
                {
                    if (dgvOrders.Rows[e.RowIndex].DataBoundItem is not OrderRecord order) return;
                    e.Value = order.Items.Sum(i => i.Quantity);
                    return;
                }

                if (dgvOrders.Columns[e.ColumnIndex].DataPropertyName == "CreatedAt")
                {
                    if (dgvOrders.Rows[e.RowIndex].DataBoundItem is not OrderRecord order) return;
                    e.Value = order.CreatedAt.ToString("MM-dd HH:mm");
                }

                if (dgvOrders.Columns[e.ColumnIndex].DataPropertyName == "CreatedTimestamp")
                {
                    if (dgvOrders.Rows[e.RowIndex].DataBoundItem is not OrderRecord order) return;
                    var t = order.CreatedTimestamp.ToString();
                    e.Value = t.Length > 8 ? t[^8..] : t;
                }
            };
            dgvOrders.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvOrders.IsCurrentCellDirty) dgvOrders.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvOrders.CellContentClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                if (dgvOrders.Columns[e.ColumnIndex].Name != "colPick") return;
                dgvOrders.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvOrders.CellValueChanged += dgvOrders_CellValueChanged;

            dgvOrderItems.AutoGenerateColumns = false;
            dgvOrderItems.Columns.Clear();
            dgvOrderItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "商品ID", DataPropertyName = "ProductId", Width = 90 });
            dgvOrderItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "名称", DataPropertyName = "ProductName", Width = 180 });
            dgvOrderItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "供货商", DataPropertyName = "Supplier", Width = 120 });
            dgvOrderItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "分类", DataPropertyName = "Category", Width = 100 });
            dgvOrderItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "单价", DataPropertyName = "UnitPrice", Width = 90 });
            dgvOrderItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "数量", DataPropertyName = "Quantity", Width = 90 });
            dgvOrderItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "小计", DataPropertyName = "SubTotal", Width = 110 });
        }

        private void dgvOrders_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_isApplyingOrderChecks) return;
            if (e.RowIndex < 0) return;
            if (dgvOrders.Columns[e.ColumnIndex].Name != "colPick") return;
            if (dgvOrders.Rows[e.RowIndex].DataBoundItem is not OrderRecord order) return;
            var picked = Convert.ToBoolean(dgvOrders.Rows[e.RowIndex].Cells["colPick"].Value);
            if (picked) _checkedOrderIds.Add(order.OrderId);
            else _checkedOrderIds.Remove(order.OrderId);
            UpdateOrderSelectionState();
        }

        private void chkOrderSelectAll_CheckedChanged(object? sender, EventArgs e)
        {
            _isApplyingOrderChecks = true;
            foreach (DataGridViewRow row in dgvOrders.Rows)
            {
                row.Cells["colPick"].Value = _chkOrderSelectAll.Checked;
                if (row.DataBoundItem is not OrderRecord order) continue;
                if (_chkOrderSelectAll.Checked) _checkedOrderIds.Add(order.OrderId);
                else _checkedOrderIds.Remove(order.OrderId);
            }
            _isApplyingOrderChecks = false;
            UpdateOrderSelectionState();
        }

        private void InitStatsGrid()
        {
            dgvStatsOrders.AutoGenerateColumns = false;
            dgvStatsOrders.Columns.Clear();
            dgvStatsOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "订单号", DataPropertyName = "OrderId", Width = 180 });
            dgvStatsOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "收银员", DataPropertyName = "CashierName", Width = 90 });
            dgvStatsOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "时间", DataPropertyName = "CreatedAt", Width = 145 });
            dgvStatsOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "时间戳", DataPropertyName = "CreatedTimestamp", Width = 120 });
            dgvStatsOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "总金额", DataPropertyName = "TotalAmount", Width = 140 });
            dgvStatsOrders.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colStatsOrderDetail",
                HeaderText = "订单详情",
                Text = "查看订单",
                UseColumnTextForButtonValue = true,
                Width = 110
            });

            dgvStatsOrders.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgvStatsOrders.Rows.Count) return;
                if (dgvStatsOrders.Columns[e.ColumnIndex].DataPropertyName != "CreatedAt") return;
                if (dgvStatsOrders.Rows[e.RowIndex].DataBoundItem is not OrderRecord order) return;
                e.Value = order.CreatedAt.ToString("MM-dd HH:mm");
            };
            dgvStatsOrders.CellContentClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                if (dgvStatsOrders.Columns[e.ColumnIndex].Name != "colStatsOrderDetail") return;
                if (dgvStatsOrders.Rows[e.RowIndex].DataBoundItem is not OrderRecord order) return;
                NavigateToOrderManagement(order.OrderId);
            };
        }

        private sealed class StatsSummaryRow
        {
            public string Metric { get; set; } = "";
            public string Value { get; set; } = "";
            public string Detail { get; set; } = "";
        }

        private void InitStatsSummaryGrid()
        {
            _dgvStatsSummary.AutoGenerateColumns = false;
            _dgvStatsSummary.AllowUserToAddRows = false;
            _dgvStatsSummary.AllowUserToDeleteRows = false;
            _dgvStatsSummary.ReadOnly = true;
            _dgvStatsSummary.RowHeadersVisible = false;
            _dgvStatsSummary.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _dgvStatsSummary.Columns.Clear();
            _dgvStatsSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "汇总指标", DataPropertyName = "Metric", Width = 180 });
            _dgvStatsSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "统计值", DataPropertyName = "Value", Width = 180 });
            _dgvStatsSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "说明", DataPropertyName = "Detail", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        }

        private async void RefreshProductGrid()
        {
            try
            {
                var products = await Task.Run(() => 
                    _inventoryService.Products.OrderBy(p => p.ID).ToList());
                dgvProducts.DataSource = null;
                dgvProducts.DataSource = products;
                UpdateProductNameMatches();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新商品列表失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnProductFilterCategory_Click(object? sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow?.DataBoundItem is not Product selected)
            {
                MessageBox.Show("请先选择一个商品。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var category = selected.Category?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(category))
            {
                MessageBox.Show("当前商品没有分类信息。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dgvProducts.DataSource = null;
            dgvProducts.DataSource = _inventoryService.Products
                .Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.ID)
                .ToList();
        }

        private void btnProductFilterSupplier_Click(object? sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow?.DataBoundItem is not Product selected)
            {
                MessageBox.Show("请先选择一个商品。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var supplier = selected.Supplier?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(supplier))
            {
                MessageBox.Show("当前商品没有供货商信息。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dgvProducts.DataSource = null;
            dgvProducts.DataSource = _inventoryService.Products
                .Where(p => string.Equals(p.Supplier, supplier, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.ID)
                .ToList();
        }

        private void btnProductApplyInputFilter_Click(object? sender, EventArgs e)
        {
            var category = txtProductCategoryFilter.Text.Trim();
            var supplier = txtProductSupplierFilter.Text.Trim();

            var query = _inventoryService.Products.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category.Contains(category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(supplier))
            {
                query = query.Where(p => p.Supplier.Contains(supplier, StringComparison.OrdinalIgnoreCase)
                                         || p.Name.Contains(supplier, StringComparison.OrdinalIgnoreCase));
            }

            dgvProducts.DataSource = null;
            dgvProducts.DataSource = query.OrderBy(p => p.ID).ToList();
        }

        private void btnProductAdjustStock_Click(object? sender, EventArgs e)
        {
            var selected = GetSelectedProduct();
            if (selected == null) return;

            if (!ShowStockDialog(selected.StockCount, out var stock)) return;
            selected.StockCount = stock;
            _inventoryService.Save();
            RefreshProductGrid();
        }

        private bool ShowStockDialog(int currentStock, out int newStock)
        {
            newStock = currentStock;
            using var dialog = new Form();
            dialog.Text = "库存管理";
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;
            dialog.ClientSize = new Size(320, 150);

            var lbl = new Label { Text = $"当前库存: {currentStock}", Left = 20, Top = 22, Width = 260 };
            var txt = new TextBox { Left = 20, Top = 54, Width = 260, Text = currentStock.ToString() };
            var btnOk = new Button { Text = "确定", Left = 124, Top = 100, Width = 75, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "取消", Left = 205, Top = 100, Width = 75, DialogResult = DialogResult.Cancel };
            dialog.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });

            if (dialog.ShowDialog(this) != DialogResult.OK) return false;
            if (!int.TryParse(txt.Text.Trim(), out var parsed) || parsed < 0)
            {
                MessageBox.Show("库存必须是非负整数。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            newStock = parsed;
            return true;
        }

        private void dgvProducts_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvProducts.Rows[e.RowIndex].DataBoundItem is not Product rowProduct) return;

            var selected = _inventoryService.FindById(rowProduct.ID);
            if (selected == null) return;

            var prop = dgvProducts.Columns[e.ColumnIndex].DataPropertyName;
            if (prop == "StockCount")
            {
                if (!ShowStockDialog(selected.StockCount, out var stock)) return;
                selected.StockCount = stock;
            }
            else if (prop == "Price")
            {
                if (!ShowPriceDialog(selected.Price, out var price)) return;
                selected.Price = price;
            }
            else
            {
                return;
            }

            _inventoryService.Save();
            RefreshProductGrid();
            dgvCart.Refresh();
            UpdateTotalAmount();
        }

        private async void RefreshOrderGrid()
        {
            try
            {
                var orders = await Task.Run(GetVisibleOrderList);
                ApplyOrderView(orders, IsAdmin ? "全部订单" : "我的订单");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新订单列表失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyOrderView(List<OrderRecord> orders, string title)
        {
            _currentOrderView = orders;
            dgvOrders.DataSource = null;
            dgvOrders.DataSource = _currentOrderView;
            _isApplyingOrderChecks = true;
            foreach (DataGridViewRow row in dgvOrders.Rows)
            {
                if (row.DataBoundItem is not OrderRecord order) continue;
                row.Cells["colPick"].Value = _checkedOrderIds.Contains(order.OrderId);
            }
            _isApplyingOrderChecks = false;
            UpdateOrderSelectionState();

            var total = _currentOrderView.Sum(x => x.TotalAmount);
            lblOrderSummary.Text = $"{title}：{_currentOrderView.Count} 单";
            lblOrderIncome.Text = $"合计 {total:F2} 元";

            if (dgvOrders.Rows.Count > 0)
            {
                dgvOrders.Rows[0].Selected = true;
                RefreshOrderDetailForSelection();
            }
            else
            {
                RefreshOrderItems(null);
            }
        }

        private void UpdateOrderSelectionState()
        {
            var selectedCount = dgvOrders.Rows
                .Cast<DataGridViewRow>()
                .Count(r => Convert.ToBoolean(r.Cells["colPick"].Value));

            btnOrderPrint.Enabled = selectedCount > 0;
            _chkOrderSelectAll.Text = selectedCount == 0 ? "全选订单" : $"已选 {selectedCount} 单";
        }

        private void ApplyOrderQuickRange(DateTime start, DateTime end, string title)
        {
            var orders = VisibleOrders
                .Where(o => o.CreatedAt >= start && o.CreatedAt <= end)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            ApplyOrderView(orders, IsAdmin ? title : $"我的{title}");
        }

        private void ApplyOrderAdvancedFilter()
        {
            var orderId = txtOrderIdFilter.Text.Trim();
            var cashierId = txtOrderCashierIdFilter.Text.Trim();
            var cashierName = txtOrderCashierNameFilter.Text.Trim();

            var query = VisibleOrders.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(orderId))
            {
                var tokens = orderId.Split(new[] { ' ', ',', ';', '，', '；', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                var normalizedTokens = tokens.Select(NormalizeDigits).Where(x => x.Length > 0).ToList();
                query = query.Where(o => tokens.Any(t => o.OrderId.Contains(t, StringComparison.OrdinalIgnoreCase))
                                         || normalizedTokens.Any(t => NormalizeDigits(o.OrderId).Contains(t, StringComparison.Ordinal)));
            }

            if (!string.IsNullOrWhiteSpace(cashierId))
            {
                query = query.Where(o => o.CashierId.Contains(cashierId, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(cashierName))
            {
                query = query.Where(o => o.CashierName.Contains(cashierName, StringComparison.OrdinalIgnoreCase));
            }

            var start = dtpOrderStart.Value;
            var end = dtpOrderEnd.Value;
            if (end < start)
            {
                MessageBox.Show("订单筛选结束时间不能早于开始时间。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            query = query.Where(o => o.CreatedAt >= start && o.CreatedAt <= end);

            ApplyOrderView(query.OrderByDescending(o => o.CreatedAt).ToList(), "筛选订单");
        }

        private static string NormalizeDigits(string input)
        {
            return new string(input.Where(char.IsDigit).ToArray());
        }

        private void RefreshStatsPage()
        {
            if (!IsAdmin)
            {
                return;
            }

            var todayRevenue = _orderService.Orders
                .Where(o => o.CreatedAt.Date == DateTime.Today)
                .Sum(o => o.TotalAmount);

            lblTodayRevenue.Text = $"今日营业额：{todayRevenue:F2}";
            ApplyStatsFilter();
        }

        private void ApplyStatsFilter()
        {
            if (!IsAdmin)
            {
                return;
            }

            var start = dtpStatStart.Value;
            var end = dtpStatEnd.Value;

            if (end < start)
            {
                MessageBox.Show("结束时间不能早于开始时间。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var filtered = _orderService.Orders
                .Where(o => o.CreatedAt >= start && o.CreatedAt <= end)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            dgvStatsOrders.DataSource = null;
            dgvStatsOrders.DataSource = filtered;

            var total = filtered.Sum(o => o.TotalAmount);
            lblFilterSummary.Text = $"筛选结果：{filtered.Count} 单，合计 {total:F2}";
            RefreshAllOrdersSummary(filtered, start, end);
        }

        private void ApplyStatQuickRange(DateTime start, DateTime end)
        {
            dtpStatStart.Value = start;
            dtpStatEnd.Value = end;
            ApplyStatsFilter();
        }

        private void RefreshAllOrdersSummary(List<OrderRecord> filteredOrders, DateTime start, DateTime end)
        {
            var totalCount = filteredOrders.Count;
            var totalAmount = filteredOrders.Sum(o => o.TotalAmount);
            var avgAmount = totalCount == 0 ? 0m : totalAmount / totalCount;
            var timeRange = $"{start:yyyy-MM-dd HH:mm} - {end:yyyy-MM-dd HH:mm}";

            var rows = new List<StatsSummaryRow>
            {
                new() { Metric = "筛选订单总数", Value = $"{totalCount}", Detail = timeRange },
                new() { Metric = "筛选订单总金额", Value = $"{totalAmount:F2}", Detail = timeRange },
                new() { Metric = "筛选客单价", Value = $"{avgAmount:F2}", Detail = "筛选总金额 / 筛选总订单数" }
            };

            var cashierGroups = filteredOrders
                .GroupBy(o => new { o.CashierId, o.CashierName })
                .OrderByDescending(g => g.Sum(x => x.TotalAmount))
                .ThenBy(g => g.Key.CashierId)
                .ToList();

            foreach (var g in cashierGroups)
            {
                var amount = g.Sum(x => x.TotalAmount);
                rows.Add(new StatsSummaryRow
                {
                    Metric = $"营业员 {g.Key.CashierName}({g.Key.CashierId})",
                    Value = $"{amount:F2}",
                    Detail = $"{g.Count()} 单"
                });
            }

            _dgvStatsSummary.DataSource = null;
            _dgvStatsSummary.DataSource = rows;
        }

        private void btnGenerateOrders_Click(object? sender, EventArgs e)
        {
            if (!decimal.TryParse(txtGenerateAmount.Text.Trim(), out var targetAmount) || targetAmount <= 0)
            {
                MessageBox.Show("请输入有效的目标金额。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtGenerateCount.Text.Trim(), out var orderCount) || orderCount <= 0)
            {
                MessageBox.Show("请输入有效的订单数量。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var totalCents = (long)Math.Round(targetAmount * 100m);
            if (totalCents < orderCount)
            {
                MessageBox.Show("目标金额过小，无法按该订单数量拆分。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var start = dtpGenStart.Value;
            var end = dtpGenEnd.Value;
            if (end < start)
            {
                MessageBox.Show("生成区间结束时间不能早于开始时间。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var orderTotals = GenerateNaturalOrderTotals(totalCents, orderCount);
            var generated = new List<OrderRecord>();

            foreach (var cents in orderTotals)
            {
                var createdAt = RandomBusinessDateInRange(start, end);
                if (createdAt == null)
                {
                    MessageBox.Show("生成区间内没有有效营业时间（已排除22:00-06:00）。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var cashier = _cashierService.GetRandom(_random);
                var generatedOrder = BuildGeneratedOrder(cents, createdAt.Value, cashier);
                if (generatedOrder == null)
                {
                    MessageBox.Show("库存不足，无法继续生成订单。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                }
                generated.Add(generatedOrder);
            }

            if (generated.Count == 0) return;

            _inventoryService.Save();
            _orderService.AppendRange(generated);
            RefreshOrderGrid();
            RefreshProductGrid();
            RefreshStatsPage();

            var actual = generated.Sum(o => o.TotalAmount);
            MessageBox.Show($"已生成 {generated.Count} 单，合计 {actual:F2}。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private List<long> GenerateNaturalOrderTotals(long totalCents, int count)
        {
            var minPerOrder = totalCents >= count * 500 ? 100L : 1L;
            if (totalCents < minPerOrder * count)
            {
                minPerOrder = 1L;
            }

            var values = Enumerable.Repeat(minPerOrder, count).ToList();
            var remaining = totalCents - minPerOrder * count;

            for (var i = 0; i < count - 1; i++)
            {
                if (remaining <= 0) break;

                var avg = remaining / (count - i);
                var low = Math.Max(0, avg / 3);
                var high = Math.Min(remaining, avg * 2 + 200);
                if (high < low) high = low;

                var part = _random.NextInt64(low, high + 1);
                values[i] += part;
                remaining -= part;
            }

            values[count - 1] += remaining;

            for (var i = 0; i < count - 1; i++)
            {
                var j = _random.Next(i, count);
                (values[i], values[j]) = (values[j], values[i]);
            }

            for (var i = 0; i < count - 1; i++)
            {
                var candidate = AdjustTail(values[i]);
                var delta = candidate - values[i];
                if (candidate <= 0) continue;
                if (values[count - 1] - delta <= 0) continue;

                values[i] = candidate;
                values[count - 1] -= delta;
            }

            return values;
        }

        private long AdjustTail(long cents)
        {
            var tails = new[] { 0L, 5L, 8L, 9L };
            var targetTail = tails[_random.Next(tails.Length)];
            var baseValue = cents / 10 * 10;
            var candidate = baseValue + targetTail;

            if (candidate <= 0)
            {
                candidate = cents;
            }

            if (Math.Abs(candidate - cents) > 30)
            {
                candidate = cents;
            }

            return candidate;
        }

        private OrderRecord? BuildGeneratedOrder(long totalCents, DateTime createdAt, Cashier cashier)
        {
            var itemCount = (int)Math.Min(5, Math.Max(2, totalCents / 800));
            itemCount = _random.Next(2, itemCount + 1);

            var lineTotals = GenerateNaturalOrderTotals(totalCents, itemCount);
            var productPool = _inventoryService.Products.Any()
                ? _inventoryService.Products
                : new List<Product>
                {
                    new() { ID = 900001, Name = "日用品", Supplier = "默认供货商A", Category = "百货", Price = 0 },
                    new() { ID = 900002, Name = "零食", Supplier = "默认供货商B", Category = "食品", Price = 0 },
                    new() { ID = 900003, Name = "饮料", Supplier = "默认供货商C", Category = "饮品", Price = 0 }
                };

            var lines = new List<OrderLine>();
            foreach (var lineCents in lineTotals)
            {
                var candidates = productPool.Where(p => p.StockCount > 0).ToList();
                if (candidates.Count == 0)
                {
                    return null;
                }

                var product = candidates[_random.Next(candidates.Count)];
                var quantity = 1;
                for (var q = 3; q >= 2; q--)
                {
                    if (lineCents % q == 0 && lineCents / q >= 50)
                    {
                        quantity = q;
                        break;
                    }
                }
                quantity = Math.Min(quantity, Math.Max(1, product.StockCount));
                product.StockCount -= quantity;

                var unitCents = lineCents / quantity;
                lines.Add(new OrderLine
                {
                    ProductId = product.ID,
                    ProductName = product.Name,
                    Supplier = product.Supplier,
                    Category = product.Category,
                    Quantity = quantity,
                    UnitPrice = unitCents / 100m,
                    SubTotal = lineCents / 100m
                });
            }

            var totalAmount = totalCents / 100m;
            return new OrderRecord
            {
                OrderId = $"ORD-GEN-{DateTime.Now:yyyyMMddHHmmssfff}-{_random.Next(10, 99)}",
                CashierId = cashier.CashierId,
                CashierName = cashier.CashierName,
                CreatedAt = createdAt,
                CreatedTimestamp = new DateTimeOffset(createdAt).ToUnixTimeMilliseconds(),
                TotalAmount = totalAmount,
                Items = lines
            };
        }

        private DateTime? RandomBusinessDateInRange(DateTime start, DateTime end)
        {
            if (end <= start)
            {
                return IsBusinessHour(start) ? start : null;
            }

            var startTicks = start.Ticks;
            var range = end.Ticks - startTicks;
            for (int i = 0; i < 500; i++)
            {
                var offset = _random.NextInt64(0, range + 1);
                var dt = new DateTime(startTicks + offset);
                if (IsBusinessHour(dt)) return dt;
            }

            var probe = start;
            while (probe <= end)
            {
                if (IsBusinessHour(probe)) return probe;
                probe = probe.AddMinutes(15);
            }
            return null;
        }

        private static bool IsBusinessHour(DateTime dt)
        {
            var hour = dt.Hour;
            return hour >= 6 && hour < 22;
        }

        private void dgvOrders_SelectionChanged(object? sender, EventArgs e)
        {
            RefreshOrderDetailForSelection();
        }

        private void RefreshOrderDetailForSelection()
        {
            if (dgvOrders.CurrentRow?.DataBoundItem is not OrderRecord order)
            {
                RefreshOrderItems(null);
                return;
            }

            RefreshOrderItems(order);
        }

        private void RefreshOrderItems(OrderRecord? order)
        {
            dgvOrderItems.DataSource = null;
            if (order == null)
            {
                lblOrderItemsTitle.Text = "订单商品明细";
                dgvOrderItems.DataSource = new List<OrderLine>();
                return;
            }

            lblOrderItemsTitle.Text = $"订单商品明细 - {order.OrderId}";
            dgvOrderItems.DataSource = order.Items.ToList();
        }

        private void NavigateToOrderManagement(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return;
            }

            ApplyOrderView(GetVisibleOrderList(), IsAdmin ? "全部订单" : "我的订单");
            ShowPage(tabOrders);

            foreach (DataGridViewRow row in dgvOrders.Rows)
            {
                if (row.DataBoundItem is not OrderRecord order) continue;
                if (!string.Equals(order.OrderId, orderId, StringComparison.OrdinalIgnoreCase)) continue;

                dgvOrders.ClearSelection();
                row.Selected = true;
                row.Cells["colPick"].Value = true;
                _checkedOrderIds.Add(order.OrderId);
                UpdateOrderSelectionState();
                dgvOrders.CurrentCell = row.Cells["colPick"];
                if (row.Index >= 0)
                {
                    dgvOrders.FirstDisplayedScrollingRowIndex = row.Index;
                }
                RefreshOrderDetailForSelection();
                return;
            }

            MessageBox.Show($"未找到订单 {orderId}。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnVoidOrder_Click(object? sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow?.DataBoundItem is not OrderRecord order)
            {
                MessageBox.Show("\u8BF7\u5148\u9009\u62E9\u4E00\u6761\u8BA2\u5355\u3002", "\u63D0\u793A", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!IsAdmin && !string.Equals(order.CashierId, _currentUser.CashierId, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("你只能操作自己的订单。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show($"\u786E\u8BA4\u4F5C\u5E9F\u8BA2\u5355 {order.OrderId} \uFF1F", "\u786E\u8BA4", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            // 使用新的BLL层处理退单
            if (_orderBLL.VoidOrder(order.OrderId, "手工作废", out var message))
            {
                _inventoryService.Load();
                _orderService.Load();
                RefreshProductGrid();
                RefreshOrderGrid();
                RefreshStatsPage();
                ToastHelper.ShowToast(this, message, 3000);
            }
            else
            {
                MessageBox.Show(message, "失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnOrderPrint_Click(object? sender, EventArgs e)
        {
            var selectedOrders = dgvOrders.Rows
                .Cast<DataGridViewRow>()
                .Where(r => Convert.ToBoolean(r.Cells["colPick"].Value))
                .Select(r => r.DataBoundItem as OrderRecord)
                .Where(o => o != null)
                .Cast<OrderRecord>()
                .ToList();

            if (selectedOrders.Count == 0)
            {
                MessageBox.Show("请先勾选要打印的订单。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _printingOrders.Clear();
            _printingOrders.AddRange(selectedOrders.OrderBy(o => o.CreatedAt));
            _printingOrderIndex = 0;
            _printingLineIndex = 0;

            using var dialog = new PrintDialog
            {
                UseEXDialog = true,
                Document = _orderPrintDocument
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _orderPrintDocument.Print();
            }
        }

        private void OrderPrintDocument_PrintPage(object? sender, PrintPageEventArgs e)
        {
            if (_printingOrderIndex >= _printingOrders.Count)
            {
                e.HasMorePages = false;
                return;
            }
            var order = _printingOrders[_printingOrderIndex];
            var graphics = e.Graphics;
            if (graphics == null)
            {
                e.HasMorePages = false;
                return;
            }

            float x = e.MarginBounds.Left;
            float y = e.MarginBounds.Top;
            float lineHeight = 22f;
            using var titleFont = new Font("Microsoft YaHei UI", 12, FontStyle.Bold);
            using var textFont = new Font("Microsoft YaHei UI", 9, FontStyle.Regular);
            using var headerFont = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);

            if (_printingLineIndex == 0)
            {
                graphics.DrawString("超市订单打印", titleFont, Brushes.Black, x, y);
                y += lineHeight + 6;
                graphics.DrawString($"订单号: {order.OrderId}", textFont, Brushes.Black, x, y);
                y += lineHeight;
                graphics.DrawString($"收银员: {order.CashierName} ({order.CashierId})", textFont, Brushes.Black, x, y);
                y += lineHeight;
                graphics.DrawString($"时间: {order.CreatedAt:yyyy-MM-dd HH:mm:ss}", textFont, Brushes.Black, x, y);
                y += lineHeight;
                graphics.DrawString($"总金额: {order.TotalAmount:F2}", textFont, Brushes.Black, x, y);
                y += lineHeight;
                graphics.DrawRectangle(Pens.Black, x, y, e.MarginBounds.Width, lineHeight + 4);
                graphics.DrawString("商品名", headerFont, Brushes.Black, x + 4, y + 2);
                graphics.DrawString("分类", headerFont, Brushes.Black, x + 220, y + 2);
                graphics.DrawString("单价", headerFont, Brushes.Black, x + 340, y + 2);
                graphics.DrawString("数量", headerFont, Brushes.Black, x + 410, y + 2);
                graphics.DrawString("小计", headerFont, Brushes.Black, x + 470, y + 2);
                y += lineHeight;
            }

            while (_printingLineIndex < order.Items.Count)
            {
                var item = order.Items[_printingLineIndex];

                if (y + lineHeight > e.MarginBounds.Bottom - 2 * lineHeight)
                {
                    e.HasMorePages = true;
                    return;
                }

                graphics.DrawRectangle(Pens.Black, x, y, e.MarginBounds.Width, lineHeight + 2);
                graphics.DrawString(item.ProductName, textFont, Brushes.Black, x + 4, y + 2);
                graphics.DrawString(item.Category, textFont, Brushes.Black, x + 220, y + 2);
                graphics.DrawString(item.UnitPrice.ToString("F2"), textFont, Brushes.Black, x + 340, y + 2);
                graphics.DrawString(item.Quantity.ToString(), textFont, Brushes.Black, x + 410, y + 2);
                graphics.DrawString(item.SubTotal.ToString("F2"), textFont, Brushes.Black, x + 470, y + 2);
                y += lineHeight;
                _printingLineIndex++;
            }
            _printingLineIndex = 0;
            _printingOrderIndex++;
            e.HasMorePages = _printingOrderIndex < _printingOrders.Count;
        }

        private void btnProductAdd_Click(object? sender, EventArgs e)
        {
            var product = ShowProductDialog();
            if (product == null) return;

            if (_inventoryService.Products.Any(p => p.ID == product.ID))
            {
                MessageBox.Show("商品ID已存在。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _inventoryService.Products.Add(product);
            _inventoryService.Save();
            RefreshProductGrid();
        }

        private void btnProductEditPrice_Click(object? sender, EventArgs e)
        {
            var selected = GetSelectedProduct();
            if (selected == null) return;

            if (!ShowPriceDialog(selected.Price, out decimal newPrice)) return;
            selected.Price = newPrice;

            _inventoryService.Save();
            RefreshProductGrid();
            dgvCart.Refresh();
            UpdateTotalAmount();
        }

        private void btnProductDelete_Click(object? sender, EventArgs e)
        {
            var selected = GetSelectedProduct();
            if (selected == null) return;

            if (_cart.Any(x => x.Product.ID == selected.ID))
            {
                MessageBox.Show("该商品已在购物车中，不能删除。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ok = MessageBox.Show($"确认删除商品 {selected.Name} ?", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ok != DialogResult.Yes) return;

            _inventoryService.LogicalDelete(selected.ID);
            RefreshProductGrid();
        }

        private Product? GetSelectedProduct()
        {
            if (dgvProducts.CurrentRow?.DataBoundItem is not Product product)
            {
                MessageBox.Show("请先选择一条商品记录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            return _inventoryService.FindById(product.ID);
        }

        private Product? ShowProductDialog()
        {
            using var dialog = new Form();
            dialog.Text = "新增商品";
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;
            dialog.ClientSize = new Size(360, 362);

            var lblId = new Label { Text = "商品ID", Left = 20, Top = 22, Width = 80 };
            var txtId = new TextBox { Left = 110, Top = 18, Width = 200 };

            var lblName = new Label { Text = "名称", Left = 20, Top = 64, Width = 80 };
            var txtName = new TextBox { Left = 110, Top = 60, Width = 200 };

            var lblSupplier = new Label { Text = "供货商", Left = 20, Top = 106, Width = 80 };
            var txtSupplier = new TextBox { Left = 110, Top = 102, Width = 200 };

            var lblCategory = new Label { Text = "分类", Left = 20, Top = 148, Width = 80 };
            var txtCategory = new TextBox { Left = 110, Top = 144, Width = 200 };

            var lblPrice = new Label { Text = "价格", Left = 20, Top = 190, Width = 80 };
            var txtPrice = new TextBox { Left = 110, Top = 186, Width = 200 };

            var lblStock = new Label { Text = "库存", Left = 20, Top = 232, Width = 80 };
            var txtStock = new TextBox { Left = 110, Top = 228, Width = 200 };

            var lblLow = new Label { Text = "库存预警值", Left = 20, Top = 274, Width = 80 };
            var txtLow = new TextBox { Left = 110, Top = 270, Width = 200, Text = "10" };

            var btnOk = new Button { Text = "确定", Left = 154, Top = 314, Width = 75, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "取消", Left = 235, Top = 314, Width = 75, DialogResult = DialogResult.Cancel };

            dialog.Controls.AddRange(new Control[]
            {
                lblId, txtId, lblName, txtName, lblSupplier, txtSupplier, lblCategory, txtCategory,
                lblPrice, txtPrice, lblStock, txtStock, lblLow, txtLow, btnOk, btnCancel
            });
            dialog.AcceptButton = btnOk;
            dialog.CancelButton = btnCancel;

            if (dialog.ShowDialog(this) != DialogResult.OK) return null;

            if (!int.TryParse(txtId.Text.Trim(), out int id))
            {
                MessageBox.Show("商品ID必须是数字。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            var name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("商品名称不能为空。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            var supplier = txtSupplier.Text.Trim();
            if (string.IsNullOrWhiteSpace(supplier))
            {
                MessageBox.Show("供货商不能为空。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            var category = txtCategory.Text.Trim();
            if (string.IsNullOrWhiteSpace(category))
            {
                MessageBox.Show("分类不能为空。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            if (!decimal.TryParse(txtPrice.Text.Trim(), out decimal price) || price < 0)
            {
                MessageBox.Show("价格格式错误。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            if (!int.TryParse(txtStock.Text.Trim(), out int stock) || stock < 0)
            {
                MessageBox.Show("库存格式错误。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            if (!int.TryParse(txtLow.Text.Trim(), out int lowStock) || lowStock < 0)
            {
                MessageBox.Show("库存预警值格式错误。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            return new Product
            {
                ID = id,
                Name = name,
                Supplier = supplier,
                Category = category,
                Price = price,
                StockCount = stock,
                LowStockThreshold = lowStock
            };
        }

        private bool ShowPriceDialog(decimal currentPrice, out decimal newPrice)
        {
            newPrice = currentPrice;
            using var dialog = new Form();
            dialog.Text = "修改价格";
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;
            dialog.ClientSize = new Size(310, 145);

            var lbl = new Label { Text = "新价格", Left = 20, Top = 24, Width = 70 };
            var txt = new TextBox { Left = 90, Top = 20, Width = 190, Text = currentPrice.ToString("F2") };
            var btnOk = new Button { Text = "确定", Left = 124, Top = 84, Width = 75, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "取消", Left = 205, Top = 84, Width = 75, DialogResult = DialogResult.Cancel };

            dialog.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
            dialog.AcceptButton = btnOk;
            dialog.CancelButton = btnCancel;

            if (dialog.ShowDialog(this) != DialogResult.OK) return false;

            if (!decimal.TryParse(txt.Text.Trim(), out decimal parsed) || parsed < 0)
            {
                MessageBox.Show("价格格式错误。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            newPrice = parsed;
            return true;
        }

        private void btnAddToCart_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(txtProductId.Text.Trim(), out int productId))
                {
                    ToastHelper.ShowToast(this, "商品ID必须是数字");
                    return;
                }

                var product = _productBLL.GetProductById(productId);
                if (product == null)
                {
                    ToastHelper.ShowToast(this, "商品ID不存在");
                    return;
                }

                AddProductToCart(product);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加商品失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddByName_Click(object? sender, EventArgs e)
        {
            AddSelectedMatchToCart();
        }

        private void UpdateProductNameMatches()
        {
            var keyword = _txtProductNameSearch.Text.Trim();
            var matches = _inventoryService.Products
                .Where(p => p.StockCount > 0)
                .Where(p => string.IsNullOrWhiteSpace(keyword) || p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Name)
                .ThenBy(p => p.ID)
                .Take(8)
                .ToList();

            _productNameMatches.Clear();
            _productNameMatches.AddRange(matches);
            _lstProductMatches.BeginUpdate();
            _lstProductMatches.Items.Clear();
            foreach (var product in matches)
            {
                _lstProductMatches.Items.Add($"{product.Name}  ￥{product.Price:F2}  库存:{product.StockCount}");
            }
            _lstProductMatches.EndUpdate();
            if (_lstProductMatches.Items.Count > 0)
            {
                _lstProductMatches.SelectedIndex = 0;
            }
        }

        private void AddSelectedMatchToCart()
        {
            if (_lstProductMatches.SelectedIndex < 0 || _lstProductMatches.SelectedIndex >= _productNameMatches.Count)
            {
                ToastHelper.ShowToast(this, "请先选择一个匹配商品");
                return;
            }

            var selected = _productNameMatches[_lstProductMatches.SelectedIndex];
            AddProductToCart(selected);
            _txtProductNameSearch.Focus();
            _txtProductNameSearch.SelectAll();
        }

        private void AddProductToCart(Product product)
        {
            int quantity = (int)numQuantity.Value;
            if (quantity <= 0)
            {
                ToastHelper.ShowToast(this, "数量必须大于0");
                return;
            }

            var current = _productBLL.GetProductById(product.ID) ?? product;
            var existing = _cart.FirstOrDefault(x => x.Product.ID == current.ID);
            int alreadyInCart = existing?.Quantity ?? 0;
            if (current.StockCount < alreadyInCart + quantity)
            {
                ToastHelper.ShowToast(this, $"库存不足，当前库存：{current.StockCount}");
                return;
            }

            if (existing == null)
            {
                _cart.Add(new OrderItem { Product = current, Quantity = quantity });
            }
            else
            {
                existing.Quantity += quantity;
                dgvCart.Refresh();
            }

            UpdateTotalAmount();
        }

        private void btnClearCart_Click(object? sender, EventArgs e)
        {
            _cart.Clear();
            UpdateTotalAmount();
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (tabMain.SelectedTab != tabCashier) return;
            if (!dgvCart.Focused && !dgvCart.ContainsFocus) return;
            HandleCartHotKey(e);
        }

        private void dgvCart_KeyDown(object? sender, KeyEventArgs e)
        {
            HandleCartHotKey(e);
        }

        private void HandleCartHotKey(KeyEventArgs e)
        {
            if (dgvCart.CurrentRow?.Index is not int rowIndex || rowIndex < 0 || rowIndex >= _cart.Count)
            {
                return;
            }

            var item = _cart[rowIndex];
            if (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus)
            {
                if (item.Quantity < item.Product.StockCount)
                {
                    item.Quantity++;
                    dgvCart.Refresh();
                    UpdateTotalAmount();
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                    dgvCart.Refresh();
                    UpdateTotalAmount();
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete)
            {
                _cart.RemoveAt(rowIndex);
                dgvCart.Refresh();
                UpdateTotalAmount();
                e.Handled = true;
            }
        }

        private async void btnCheckout_Click(object? sender, EventArgs e)
        {
            if (_cart.Count == 0)
            {
                ToastHelper.ShowToast(this, "购物车为空");
                return;
            }

            try
            {
                btnCheckout.Enabled = false;
                btnCheckout.Text = "结算中...";

                // 异步检查库存
                await Task.Run(() =>
                {
                    foreach (var item in _cart)
                    {
                        var p = _inventoryService.FindById(item.Product.ID);
                        if (p == null || p.StockCount < item.Quantity)
                        {
                            throw new InvalidOperationException($"商品 {item.Product.Name} 库存不足，无法结算。");
                        }
                    }
                });

                var raw = _cart.Sum(i => i.SubTotal);
                var discount = _numDiscount.Value;
                var actual = Math.Max(0, raw - discount);
                if (_cmbPayType.SelectedItem?.ToString() == "现金" && _numReceived.Value < actual)
                {
                    MessageBox.Show("实收金额不能小于应收。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btnCheckout.Enabled = true;
                    btnCheckout.Text = "结算";
                    return;
                }

                var cashier = IsAdmin
                    ? cmbCashier.SelectedItem as Cashier ?? _cashierService.GetRandom(_random)
                    : new Cashier
                    {
                        CashierId = _currentUser.CashierId,
                        CashierName = _currentUser.CashierName
                    };
                var order = BuildOrderRecord(_cart.ToList(), cashier);
                ApplyCashierContextToOrder(order);
                
                // 异步处理订单（使用事务）
                string? errorMessage = null;
                var success = await Task.Run(() =>
                {
                    return _orderBLL.PlaceOrder(order, out errorMessage);
                });

                if (!success)
                {
                    MessageBox.Show($"结算失败：{errorMessage}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnCheckout.Enabled = true;
                    btnCheckout.Text = "结算";
                    return;
                }

                // 异步生成小票
                await Task.Run(() => GenerateReceipt(order));

                ToastHelper.ShowToast(this, "结算成功！", 3000);
                _cart.Clear();
                _numDiscount.Value = 0;
                _numReceived.Value = 0;
                
                // 异步刷新数据
                await Task.Run(() =>
                {
                    _inventoryService.Load();
                    _orderService.Load();
                });

                UpdateTotalAmount();
                RefreshProductGrid();
                RefreshOrderGrid();
                RefreshStatsPage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"结算失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnCheckout.Enabled = true;
                btnCheckout.Text = "结算";
            }
        }

        private OrderRecord BuildOrderRecord(List<OrderItem> items, Cashier cashier)
        {
            var now = DateTime.Now;
            var raw = items.Sum(i => i.SubTotal);
            var discount = _numDiscount.Value;
            var actual = Math.Max(0, raw - discount);
            var payType = _cmbPayType.SelectedItem?.ToString() ?? "现金";
            var received = payType == "现金" ? _numReceived.Value : actual;
            var change = Math.Max(0, received - actual);
            return new OrderRecord
            {
                OrderId = Guid.NewGuid().ToString("N").ToUpperInvariant(),
                CashierId = cashier.CashierId,
                CashierName = cashier.CashierName,
                PayType = payType,
                DiscountAmount = discount,
                ReceivedAmount = received,
                ChangeAmount = change,
                CreatedAt = now,
                CreatedTimestamp = new DateTimeOffset(now).ToUnixTimeMilliseconds(),
                TotalAmount = actual,
                Status = "Completed",
                Items = items.Select(i => new OrderLine
                {
                    ProductId = i.Product.ID,
                    ProductName = i.Product.Name,
                    Supplier = i.Product.Supplier,
                    Category = i.Product.Category,
                    UnitPrice = i.Product.Price,
                    Quantity = i.Quantity,
                    SubTotal = i.SubTotal
                }).ToList()
            };
        }

        private void UpdateTotalAmount()
        {
            var raw = _cart.Sum(x => x.SubTotal);
            var discount = _numDiscount.Value;
            var actual = Math.Max(0, raw - discount);
            lblTotalAmount.Text = $"总金额：{actual:F2} 元";
            var change = Math.Max(0, _numReceived.Value - actual);
            _lblChangeAmount.Text = $"找零：{change:F2} 元";
        }

        private void GenerateReceipt(OrderRecord order)
        {
            var sb = new StringBuilder();
            sb.AppendLine("==== 超市小票 ====");
            sb.AppendLine($"订单号：{order.OrderId}");
            sb.AppendLine($"收银员：{order.CashierName} ({order.CashierId})");
            sb.AppendLine($"支付方式：{order.PayType}");
            sb.AppendLine($"时间：{order.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"时间戳：{order.CreatedTimestamp}");
            sb.AppendLine("------------------");

            foreach (var item in order.Items)
            {
                sb.AppendLine($"{item.ProductName}  x{item.Quantity}  单价:{item.UnitPrice:F2}  小计:{item.SubTotal:F2}");
            }

            sb.AppendLine("------------------");
            sb.AppendLine($"折扣：{order.DiscountAmount:F2}");
            sb.AppendLine($"实收：{order.ReceivedAmount:F2}");
            sb.AppendLine($"找零：{order.ChangeAmount:F2}");
            sb.AppendLine($"总金额：{order.TotalAmount:F2}");
            sb.AppendLine("谢谢惠顾！");

            File.WriteAllText("receipt.txt", sb.ToString(), Encoding.UTF8);
        }
    }
}
