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
        private readonly Button _btnExportLogs = new();
        private readonly TabPage _tabGenerate = new("订单生成");
        private int _titleClickCount;
        private bool _generatePageUnlocked;

        public Form1(string? loginUserName = null)
        {
            InitializeComponent();
            KeyPreview = true;

            if (!string.IsNullOrWhiteSpace(loginUserName))
            {
                Text = $"超市收银管理系统 - 当前用户: {loginUserName}";
            }

            var dbDir = Path.Combine(AppContext.BaseDirectory, "db");
            var dbPath = Path.Combine(dbDir, "supermarket.db");

            _inventoryService = new InventoryService(dbPath);
            _inventoryService.Load();

            _orderService = new OrderService(dbPath);
            _orderService.Load();
            _cashierService = new CashierService(dbPath);
            _cashierService.Load();

            // 初始化BLL层
            _productBLL = new ProductBLL(dbPath);
            _orderBLL = new OrderBLL(dbPath);
            _statisticsBLL = new StatisticsBLL(dbPath);
            _transactionLogBLL = new TransactionLogBLL(dbPath);

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
            ApplyNavStyles();
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
            
            // 初始化收银流水TabPage
            InitTransactionLogsTab();
            
            ShowPage(tabCashier);
        }

        private void WireEvents()
        {
            btnAddToCart.Click += btnAddToCart_Click;
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
            
            // 添加收银流水页面入口（放在订单管理下面）
            var btnNavTransactionLogs = new Button
            {
                Text = "收银流水",
                Location = new Point(18, 184 + 52), // 订单管理是184，加上52的间距 = 236
                Size = new Size(154, 42) // 和其他按钮一样大小，样式在ApplyNavStyles中统一设置
            };
            btnNavTransactionLogs.Click += (s, e) =>
            {
                ShowPage(_tabTransactionLogs);
                RefreshTransactionLogsPage();
            };
            panelNav.Controls.Add(btnNavTransactionLogs);
            
            // 调整统计页面按钮位置（放在收银流水下面）
            btnNavStats.Location = new Point(18, 236 + 52); // 收银流水是236，加上52的间距 = 288
            
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
            _btnExportLogs.Click += btnExportLogs_Click;
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
            btnGenerateOrders.Click += btnGenerateOrders_Click;
        }

        private void lblNavTitle_Click(object? sender, EventArgs e)
        {
            if (_generatePageUnlocked) return;
            _titleClickCount++;
            if (_titleClickCount < 5) return;

            _generatePageUnlocked = true;
            tabMain.TabPages.Add(_tabGenerate);
            tabMain.SelectedTab = _tabGenerate;
            MessageBox.Show("订单生成页面已解锁。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ConfigureAutoLayouts()
        {
            ConfigureProductTopLayout();
            ConfigureOrderTopLayout();
            ConfigureStatsTopLayout();
            ConfigureStatsBodyLayout();
            ConfigureFormVisuals();
        }

        private void ConfigureFormVisuals()
        {
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            lblInputTitle.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            lblTotalAmount.Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold);
            btnCheckout.BackColor = Color.FromArgb(27, 94, 32);
            btnCheckout.ForeColor = Color.White;
            btnCheckout.FlatStyle = FlatStyle.Flat;
            btnCheckout.FlatAppearance.BorderSize = 0;
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
            _btnExportLogs.Text = "\u5BFC\u51FA\u6D41\u6C34CSV";
            SetWideButton(_btnVoidOrder);
            SetWideButton(_btnExportLogs);
            row1.Controls.AddRange(new Control[] { lblOrderSummary, lblOrderIncome, btnOrderToday, btnOrderYesterday, btnOrderMonth, btnOrderPrint, _btnVoidOrder, _btnExportLogs, btnOrderRefresh });

            var row2 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
            row2.Controls.AddRange(new Control[] { lblOrderIdFilter, txtOrderIdFilter, lblOrderCashierIdFilter, txtOrderCashierIdFilter, lblOrderCashierNameFilter, txtOrderCashierNameFilter });

            var row3 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
            _chkOrderSelectAll.Text = "全选";
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
            row1.Controls.AddRange(new Control[] { lblTodayRevenue, lblFilterSummary, btnStatsClearFilter });

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
                RowCount = 2
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            panelStatsTop.Dock = DockStyle.Fill;
            dgvStatsOrders.Dock = DockStyle.Fill;

            table.Controls.Add(panelStatsTop, 0, 0);
            table.Controls.Add(dgvStatsOrders, 0, 1);
            tabStats.Controls.Add(table);
        }
        
        private void InitTransactionLogsTab()
        {
            var dbDir = Path.Combine(AppContext.BaseDirectory, "db");
            var dbPath = Path.Combine(dbDir, "supermarket.db");
            
            _tabTransactionLogs = new TabPage("收银流水");
            _transactionLogPanel = new TransactionLogPanel(dbPath);
            _transactionLogPanel.Dock = DockStyle.Fill;
            _tabTransactionLogs.Controls.Add(_transactionLogPanel);
            
            tabMain.Controls.Add(_tabTransactionLogs);
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
            var host = new Panel { Dock = DockStyle.Top, Height = 120, Padding = new Padding(8) };
            var row = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
            row.Controls.AddRange(new Control[]
            {
                lblGenStart, dtpGenStart, lblGenEnd, dtpGenEnd, lblGenerateAmount, txtGenerateAmount,
                lblGenerateCount, txtGenerateCount, btnGenerateOrders
            });
            host.Controls.Add(row);
            _tabGenerate.Controls.Add(host);
            _tabGenerate.BackColor = Color.White;
        }

        private void ShowPage(TabPage page)
        {
            tabMain.SelectedTab = page;
        }

        private void ApplyNavStyles()
        {
            // 获取所有导航按钮（包括收银流水）
            var navButtons = panelNav.Controls.OfType<Button>()
                .Where(b => b.Text == "收银台" || b.Text == "商品管理" || b.Text == "订单管理" || 
                           b.Text == "收银流水" || b.Text == "统计页面")
                .ToList();
            
            foreach (var btn in navButtons)
            {
                btn.ForeColor = Color.White;
                btn.BackColor = Color.FromArgb(57, 62, 70); // 统一的按钮背景色
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 75, 85); // 鼠标悬停时的颜色
            }
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
            cmbCashier.SelectedIndex = 0;
        }

        private void InitCheckoutExtension()
        {
            var lblPayType = new Label { Text = "\u652F\u4ED8\u65B9\u5F0F", Left = 12, Top = 210, Width = 200 };
            _cmbPayType.Left = 12;
            _cmbPayType.Top = 232;
            _cmbPayType.Width = 200;
            _cmbPayType.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbPayType.Items.AddRange(new object[] { "现金", "扫码" });
            _cmbPayType.SelectedIndex = 0;

            var lblDiscount = new Label { Text = "\u624B\u52A8\u6298\u6263", Left = 12, Top = 272, Width = 200 };
            _numDiscount.Left = 12;
            _numDiscount.Top = 294;
            _numDiscount.Width = 200;
            _numDiscount.DecimalPlaces = 2;
            _numDiscount.Maximum = 999999;
            _numDiscount.ValueChanged += (s, e) => UpdateTotalAmount();

            var lblReceived = new Label { Text = "\u5B9E\u6536\u91D1\u989D", Left = 12, Top = 334, Width = 200 };
            _numReceived.Left = 12;
            _numReceived.Top = 356;
            _numReceived.Width = 200;
            _numReceived.DecimalPlaces = 2;
            _numReceived.Maximum = 999999;
            _numReceived.ValueChanged += (s, e) => UpdateTotalAmount();

            _lblChangeAmount.Text = "\u627E\u96F6\uFF1A0.00";
            _lblChangeAmount.Left = 12;
            _lblChangeAmount.Top = 396;
            _lblChangeAmount.Width = 200;

            panelCashierRight.Controls.AddRange(new Control[] { lblPayType, _cmbPayType, lblDiscount, _numDiscount, lblReceived, _numReceived, _lblChangeAmount });
        }

        private void InitCartGrid()
        {
            dgvCart.AutoGenerateColumns = false;
            dgvCart.DataSource = _cart;
            dgvCart.Columns.Clear();
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "名称", Name = "colName", Width = 180 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "供货商", Name = "colSupplier", Width = 120 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "分类", Name = "colCategory", Width = 90 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "单价", Name = "colPrice", Width = 80 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "数量", DataPropertyName = "Quantity", Name = "colQuantity" });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "小计", Name = "colSubTotal", Width = 90 });

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
            dgvOrders.Columns.Clear();
            dgvOrders.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "选", Name = "colPick", Width = 40, ReadOnly = false, SortMode = DataGridViewColumnSortMode.NotSortable });
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

            dgvStatsOrders.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= dgvStatsOrders.Rows.Count) return;
                if (dgvStatsOrders.Columns[e.ColumnIndex].DataPropertyName != "CreatedAt") return;
                if (dgvStatsOrders.Rows[e.RowIndex].DataBoundItem is not OrderRecord order) return;
                e.Value = order.CreatedAt.ToString("MM-dd HH:mm");
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
                var orders = await Task.Run(() => 
                    _orderService.Orders.OrderByDescending(o => o.CreatedAt).ToList());
                ApplyOrderView(orders, "全部订单");
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

        private void ApplyOrderQuickRange(DateTime start, DateTime end, string title)
        {
            var orders = _orderService.Orders
                .Where(o => o.CreatedAt >= start && o.CreatedAt <= end)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            ApplyOrderView(orders, title);
        }

        private void ApplyOrderAdvancedFilter()
        {
            var orderId = txtOrderIdFilter.Text.Trim();
            var cashierId = txtOrderCashierIdFilter.Text.Trim();
            var cashierName = txtOrderCashierNameFilter.Text.Trim();

            var query = _orderService.Orders.AsEnumerable();
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
            var todayRevenue = _orderService.Orders
                .Where(o => o.CreatedAt.Date == DateTime.Today)
                .Sum(o => o.TotalAmount);

            lblTodayRevenue.Text = $"今日营业额：{todayRevenue:F2}";
            ApplyStatsFilter();
        }

        private void ApplyStatsFilter()
        {
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

        private void btnVoidOrder_Click(object? sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow?.DataBoundItem is not OrderRecord order)
            {
                MessageBox.Show("\u8BF7\u5148\u9009\u62E9\u4E00\u6761\u8BA2\u5355\u3002", "\u63D0\u793A", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show($"\u786E\u8BA4\u4F5C\u5E9F\u8BA2\u5355 {order.OrderId} \uFF1F", "\u786E\u8BA4", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            // 使用新的BLL层处理退单
            if (_orderBLL.VoidOrder(order.OrderId, "手工作废", out var message))
            {
                _inventoryService.Load();
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

        private void btnExportLogs_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"transaction_logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            _orderService.ExportLogsToCsv(dialog.FileName);
            MessageBox.Show("\u6D41\u6C34\u5DF2\u5BFC\u51FA\u3002", "\u5B8C\u6210", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                selectedOrders = dgvOrders.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as OrderRecord)
                .Where(o => o != null)
                .Cast<OrderRecord>()
                .ToList();
            }

            if (selectedOrders.Count == 0 && dgvOrders.CurrentRow?.DataBoundItem is OrderRecord current)
            {
                selectedOrders.Add(current);
            }

            if (selectedOrders.Count == 0)
            {
                MessageBox.Show("请至少选择一条订单。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                int quantity = (int)numQuantity.Value;
                if (quantity <= 0)
                {
                    ToastHelper.ShowToast(this, "数量必须大于0");
                    return;
                }

                var product = _productBLL.GetProductById(productId);
                if (product == null)
                {
                    ToastHelper.ShowToast(this, "商品ID不存在");
                    return;
                }

                var existing = _cart.FirstOrDefault(x => x.Product.ID == productId);
                int alreadyInCart = existing?.Quantity ?? 0;
                if (product.StockCount < alreadyInCart + quantity)
                {
                    ToastHelper.ShowToast(this, $"库存不足，当前库存：{product.StockCount}");
                    return;
                }

                if (existing == null)
                {
                    _cart.Add(new OrderItem { Product = product, Quantity = quantity });
                }
                else
                {
                    existing.Quantity += quantity;
                    dgvCart.Refresh();
                }

                UpdateTotalAmount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加商品失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

                var cashier = cmbCashier.SelectedItem as Cashier ?? _cashierService.GetRandom(_random);
                var order = BuildOrderRecord(_cart.ToList(), cashier);
                
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
            lblTotalAmount.Text = $"总金额：{actual:C2}";
            var change = Math.Max(0, _numReceived.Value - actual);
            _lblChangeAmount.Text = $"找零：{change:F2}";
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




