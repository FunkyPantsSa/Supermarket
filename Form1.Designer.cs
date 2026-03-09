
namespace Supermarket
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            panelNav = new Panel();
            btnNavStats = new Button();
            btnNavOrders = new Button();
            btnNavProducts = new Button();
            btnNavCashier = new Button();
            lblNavTitle = new Label();

            panelMain = new Panel();
            tabMain = new TabControl();
            tabCashier = new TabPage();
            tabProducts = new TabPage();
            tabOrders = new TabPage();
            tabStats = new TabPage();

            panelCashierLeft = new Panel();
            panelCashierCenter = new Panel();
            panelCashierRight = new Panel();
            txtProductId = new TextBox();
            numQuantity = new NumericUpDown();
            lblCashier = new Label();
            cmbCashier = new ComboBox();
            btnAddToCart = new Button();
            lblInputTitle = new Label();
            lblId = new Label();
            lblQty = new Label();
            dgvCart = new DataGridView();
            lblTotalAmount = new Label();
            btnCheckout = new Button();
            btnClearCart = new Button();

            panelProductTop = new Panel();
            btnProductAdd = new Button();
            btnProductEditPrice = new Button();
            btnProductDelete = new Button();
            btnProductRefresh = new Button();
            btnProductFilterCategory = new Button();
            btnProductFilterSupplier = new Button();
            btnProductClearFilter = new Button();
            lblProductCategoryFilter = new Label();
            txtProductCategoryFilter = new TextBox();
            lblProductSupplierFilter = new Label();
            txtProductSupplierFilter = new TextBox();
            btnProductApplyInputFilter = new Button();
            btnProductAdjustStock = new Button();
            dgvProducts = new DataGridView();

            panelOrderTop = new Panel();
            lblOrderSummary = new Label();
            lblOrderIncome = new Label();
            btnOrderRefresh = new Button();
            btnOrderPrint = new Button();
            btnOrderToday = new Button();
            btnOrderYesterday = new Button();
            btnOrderMonth = new Button();
            lblOrderIdFilter = new Label();
            txtOrderIdFilter = new TextBox();
            lblOrderCashierIdFilter = new Label();
            txtOrderCashierIdFilter = new TextBox();
            lblOrderCashierNameFilter = new Label();
            txtOrderCashierNameFilter = new TextBox();
            lblOrderTimeFrom = new Label();
            dtpOrderStart = new DateTimePicker();
            lblOrderTimeTo = new Label();
            dtpOrderEnd = new DateTimePicker();
            btnOrderApplyFilter = new Button();
            btnOrderClearFilter = new Button();
            tableOrdersLayout = new TableLayoutPanel();
            dgvOrders = new DataGridView();
            panelOrderDetailContainer = new Panel();
            panelOrderDetailTop = new Panel();
            lblOrderItemsTitle = new Label();
            dgvOrderItems = new DataGridView();

            panelStatsTop = new Panel();
            lblTodayRevenue = new Label();
            lblFilterSummary = new Label();
            lblStatStart = new Label();
            dtpStatStart = new DateTimePicker();
            lblStatEnd = new Label();
            dtpStatEnd = new DateTimePicker();
            btnApplyStatsFilter = new Button();
            btnStatsClearFilter = new Button();
            lblGenStart = new Label();
            dtpGenStart = new DateTimePicker();
            lblGenEnd = new Label();
            dtpGenEnd = new DateTimePicker();
            lblGenerateAmount = new Label();
            txtGenerateAmount = new TextBox();
            lblGenerateCount = new Label();
            txtGenerateCount = new TextBox();
            btnGenerateOrders = new Button();
            dgvStatsOrders = new DataGridView();

            ((System.ComponentModel.ISupportInitialize)numQuantity).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvCart).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvProducts).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvOrders).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvOrderItems).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvStatsOrders).BeginInit();
            SuspendLayout();

            panelNav.Dock = DockStyle.Left;
            panelNav.Width = 190;
            panelNav.BackColor = Color.FromArgb(34, 40, 49);
            panelNav.Controls.AddRange(new Control[] { btnNavStats, btnNavOrders, btnNavProducts, btnNavCashier, lblNavTitle });

            lblNavTitle.Text = "功能区";
            lblNavTitle.ForeColor = Color.White;
            lblNavTitle.Location = new Point(18, 20);
            lblNavTitle.AutoSize = true;

            btnNavCashier.Text = "收银台";
            btnNavCashier.Location = new Point(18, 80);
            btnNavCashier.Size = new Size(154, 42);
            btnNavProducts.Text = "商品管理";
            btnNavProducts.Location = new Point(18, 132);
            btnNavProducts.Size = new Size(154, 42);
            btnNavOrders.Text = "订单管理";
            btnNavOrders.Location = new Point(18, 184);
            btnNavOrders.Size = new Size(154, 42);
            btnNavStats.Text = "统计页面";
            btnNavStats.Location = new Point(18, 288); // 收银流水在236，统计页面在288
            btnNavStats.Size = new Size(154, 42);

            panelMain.Dock = DockStyle.Fill;
            panelMain.Padding = new Padding(8);
            panelMain.Controls.Add(tabMain);

            tabMain.Dock = DockStyle.Fill;
            tabMain.Controls.AddRange(new Control[] { tabCashier, tabProducts, tabOrders, tabStats });

            tabCashier.Text = "收银台";
            tabProducts.Text = "商品管理";
            tabOrders.Text = "订单管理";
            tabStats.Text = "统计页面";
            tabCashier.Controls.AddRange(new Control[] { panelCashierCenter, panelCashierRight, panelCashierLeft });
            panelCashierLeft.Dock = DockStyle.Left;
            panelCashierLeft.Width = 230;
            panelCashierCenter.Dock = DockStyle.Fill;
            panelCashierCenter.Padding = new Padding(8);
            panelCashierRight.Dock = DockStyle.Right;
            panelCashierRight.Width = 230;

            panelCashierLeft.Controls.AddRange(new Control[] { lblInputTitle, lblId, txtProductId, lblQty, numQuantity, lblCashier, cmbCashier, btnAddToCart });
            lblInputTitle.Text = "商品输入";
            lblInputTitle.Location = new Point(12, 18);
            lblInputTitle.AutoSize = true;
            lblId.Text = "商品ID/条码";
            lblId.Location = new Point(12, 54);
            lblId.AutoSize = true;
            txtProductId.Location = new Point(12, 78);
            txtProductId.Width = 200;
            lblQty.Text = "数量";
            lblQty.Location = new Point(12, 114);
            lblQty.AutoSize = true;
            numQuantity.Location = new Point(12, 138);
            numQuantity.Width = 200;
            numQuantity.Minimum = 1;
            numQuantity.Value = 1;
            lblCashier.Text = "营业员";
            lblCashier.Location = new Point(12, 174);
            lblCashier.AutoSize = true;
            cmbCashier.Location = new Point(12, 198);
            cmbCashier.Size = new Size(200, 32);
            cmbCashier.DropDownStyle = ComboBoxStyle.DropDownList;
            btnAddToCart.Text = "添加至购物车";
            btnAddToCart.Location = new Point(12, 240);
            btnAddToCart.Size = new Size(200, 36);

            panelCashierCenter.Controls.Add(dgvCart);
            dgvCart.Dock = DockStyle.Fill;
            dgvCart.ReadOnly = true;
            dgvCart.AllowUserToAddRows = false;
            dgvCart.AllowUserToDeleteRows = false;
            dgvCart.RowHeadersVisible = false;

            panelCashierRight.Controls.AddRange(new Control[] { lblTotalAmount, btnCheckout, btnClearCart });
            lblTotalAmount.Text = "总金额：0";
            lblTotalAmount.Location = new Point(12, 40);
            lblTotalAmount.AutoSize = true;
            btnCheckout.Text = "结算";
            btnCheckout.Location = new Point(12, 100);
            btnCheckout.Size = new Size(200, 46);
            btnClearCart.Text = "清空购物车";
            btnClearCart.Location = new Point(12, 160);
            btnClearCart.Size = new Size(200, 36);

            tabProducts.Controls.Add(dgvProducts);
            tabProducts.Controls.Add(panelProductTop);
            panelProductTop.Dock = DockStyle.Top;
            panelProductTop.Height = 132;
            panelProductTop.Controls.AddRange(new Control[]
            {
                btnProductAdd, btnProductEditPrice, btnProductDelete, btnProductRefresh,
                btnProductFilterCategory, btnProductFilterSupplier, btnProductClearFilter,
                lblProductCategoryFilter, txtProductCategoryFilter, lblProductSupplierFilter, txtProductSupplierFilter,
                btnProductApplyInputFilter, btnProductAdjustStock
            });
            btnProductAdd.Text = "新增商品";
            btnProductAdd.Location = new Point(12, 10);
            btnProductAdd.Size = new Size(110, 34);
            btnProductEditPrice.Text = "修改价格";
            btnProductEditPrice.Location = new Point(128, 10);
            btnProductEditPrice.Size = new Size(110, 34);
            btnProductDelete.Text = "删除商品";
            btnProductDelete.Location = new Point(244, 10);
            btnProductDelete.Size = new Size(110, 34);
            btnProductRefresh.Text = "刷新";
            btnProductRefresh.Location = new Point(360, 10);
            btnProductRefresh.Size = new Size(110, 34);
            btnProductFilterCategory.Text = "同品类筛选";
            btnProductFilterCategory.Location = new Point(12, 52);
            btnProductFilterCategory.Size = new Size(110, 34);
            btnProductFilterSupplier.Text = "同供货商筛选";
            btnProductFilterSupplier.Location = new Point(128, 52);
            btnProductFilterSupplier.Size = new Size(120, 34);
            btnProductClearFilter.Text = "清除筛选";
            btnProductClearFilter.Location = new Point(254, 52);
            btnProductClearFilter.Size = new Size(100, 34);
            lblProductCategoryFilter.Text = "品类:";
            lblProductCategoryFilter.Location = new Point(480, 16);
            lblProductCategoryFilter.AutoSize = true;
            txtProductCategoryFilter.Location = new Point(530, 12);
            txtProductCategoryFilter.Size = new Size(130, 30);
            lblProductSupplierFilter.Text = "供货商:";
            lblProductSupplierFilter.Location = new Point(480, 56);
            lblProductSupplierFilter.AutoSize = true;
            txtProductSupplierFilter.Location = new Point(530, 52);
            txtProductSupplierFilter.Size = new Size(130, 30);
            btnProductApplyInputFilter.Text = "按输入筛选";
            btnProductApplyInputFilter.Location = new Point(670, 12);
            btnProductApplyInputFilter.Size = new Size(110, 34);
            btnProductAdjustStock.Text = "库存管理";
            btnProductAdjustStock.Location = new Point(670, 52);
            btnProductAdjustStock.Size = new Size(110, 34);
            dgvProducts.Dock = DockStyle.Fill;
            dgvProducts.ReadOnly = true;
            dgvProducts.AllowUserToAddRows = false;
            dgvProducts.AllowUserToDeleteRows = false;
            dgvProducts.RowHeadersVisible = false;
            dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            tabOrders.Controls.Add(tableOrdersLayout);
            tabOrders.Controls.Add(panelOrderTop);
            panelOrderTop.Dock = DockStyle.Top;
            panelOrderTop.Height = 168;
            panelOrderTop.Controls.AddRange(new Control[]
            {
                lblOrderSummary, lblOrderIncome, btnOrderToday, btnOrderYesterday, btnOrderMonth, btnOrderPrint, btnOrderRefresh,
                lblOrderIdFilter, txtOrderIdFilter, lblOrderCashierIdFilter, txtOrderCashierIdFilter,
                lblOrderCashierNameFilter, txtOrderCashierNameFilter, lblOrderTimeFrom, dtpOrderStart, lblOrderTimeTo, dtpOrderEnd,
                btnOrderApplyFilter, btnOrderClearFilter
            });
            lblOrderSummary.Text = "订单总数：0";
            lblOrderSummary.Location = new Point(14, 12);
            lblOrderSummary.AutoSize = false;
            lblOrderSummary.Size = new Size(280, 24);
            lblOrderIncome.Text = "收入统计：0.00";
            lblOrderIncome.Location = new Point(14, 44);
            lblOrderIncome.AutoSize = false;
            lblOrderIncome.Size = new Size(280, 24);
            btnOrderToday.Text = "今日收入";
            btnOrderToday.Location = new Point(300, 10);
            btnOrderToday.Size = new Size(100, 34);
            btnOrderYesterday.Text = "昨日收入";
            btnOrderYesterday.Location = new Point(406, 10);
            btnOrderYesterday.Size = new Size(100, 34);
            btnOrderMonth.Text = "本月收入";
            btnOrderMonth.Location = new Point(512, 10);
            btnOrderMonth.Size = new Size(100, 34);
            btnOrderPrint.Text = "打印订单";
            btnOrderPrint.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnOrderPrint.Location = new Point(620, 10);
            btnOrderPrint.Size = new Size(110, 34);
            btnOrderRefresh.Text = "刷新";
            btnOrderRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOrderRefresh.Location = new Point(954, 10);
            btnOrderRefresh.Size = new Size(96, 34);
            lblOrderIdFilter.Text = "订单号:";
            lblOrderIdFilter.Location = new Point(300, 52);
            lblOrderIdFilter.AutoSize = true;
            txtOrderIdFilter.Location = new Point(356, 48);
            txtOrderIdFilter.Size = new Size(160, 30);
            lblOrderCashierIdFilter.Text = "收银员ID:";
            lblOrderCashierIdFilter.Location = new Point(526, 52);
            lblOrderCashierIdFilter.AutoSize = true;
            txtOrderCashierIdFilter.Location = new Point(598, 48);
            txtOrderCashierIdFilter.Size = new Size(90, 30);
            lblOrderCashierNameFilter.Text = "收银员名:";
            lblOrderCashierNameFilter.Location = new Point(696, 52);
            lblOrderCashierNameFilter.AutoSize = true;
            txtOrderCashierNameFilter.Location = new Point(768, 48);
            txtOrderCashierNameFilter.Size = new Size(100, 30);
            lblOrderTimeFrom.Text = "开始:";
            lblOrderTimeFrom.Location = new Point(300, 90);
            lblOrderTimeFrom.AutoSize = true;
            dtpOrderStart.CustomFormat = "yyyy-MM-dd HH:mm";
            dtpOrderStart.Format = DateTimePickerFormat.Custom;
            dtpOrderStart.Location = new Point(348, 86);
            dtpOrderStart.Size = new Size(180, 30);
            lblOrderTimeTo.Text = "结束:";
            lblOrderTimeTo.Location = new Point(536, 90);
            lblOrderTimeTo.AutoSize = true;
            dtpOrderEnd.CustomFormat = "yyyy-MM-dd HH:mm";
            dtpOrderEnd.Format = DateTimePickerFormat.Custom;
            dtpOrderEnd.Location = new Point(584, 86);
            dtpOrderEnd.Size = new Size(180, 30);
            btnOrderApplyFilter.Text = "筛选";
            btnOrderApplyFilter.Location = new Point(772, 84);
            btnOrderApplyFilter.Size = new Size(72, 34);
            btnOrderClearFilter.Text = "清除筛选";
            btnOrderClearFilter.Location = new Point(850, 84);
            btnOrderClearFilter.Size = new Size(96, 34);

            tableOrdersLayout.Dock = DockStyle.Fill;
            tableOrdersLayout.RowCount = 2;
            tableOrdersLayout.ColumnCount = 1;
            tableOrdersLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tableOrdersLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tableOrdersLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableOrdersLayout.Controls.Add(dgvOrders, 0, 0);
            tableOrdersLayout.Controls.Add(panelOrderDetailContainer, 0, 1);

            dgvOrders.Dock = DockStyle.Fill;
            dgvOrders.ReadOnly = true;
            dgvOrders.AllowUserToAddRows = false;
            dgvOrders.AllowUserToDeleteRows = false;
            dgvOrders.RowHeadersVisible = false;
            dgvOrders.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            panelOrderDetailContainer.Dock = DockStyle.Fill;
            panelOrderDetailContainer.Controls.Add(dgvOrderItems);
            panelOrderDetailContainer.Controls.Add(panelOrderDetailTop);

            panelOrderDetailTop.Dock = DockStyle.Top;
            panelOrderDetailTop.Height = 38;
            panelOrderDetailTop.Controls.Add(lblOrderItemsTitle);
            lblOrderItemsTitle.Text = "订单商品明细";
            lblOrderItemsTitle.Location = new Point(12, 8);
            lblOrderItemsTitle.AutoSize = true;

            dgvOrderItems.Dock = DockStyle.Fill;
            dgvOrderItems.ReadOnly = true;
            dgvOrderItems.AllowUserToAddRows = false;
            dgvOrderItems.AllowUserToDeleteRows = false;
            dgvOrderItems.RowHeadersVisible = false;
            dgvOrderItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            tabStats.Controls.Add(dgvStatsOrders);
            tabStats.Controls.Add(panelStatsTop);
            panelStatsTop.Dock = DockStyle.Top;
            panelStatsTop.Height = 188;
            panelStatsTop.Controls.AddRange(new Control[]
            {
                lblTodayRevenue, lblFilterSummary, lblStatStart, dtpStatStart, lblStatEnd, dtpStatEnd,
                btnApplyStatsFilter, btnStatsClearFilter,
                lblGenStart, dtpGenStart, lblGenEnd, dtpGenEnd,
                lblGenerateAmount, txtGenerateAmount, lblGenerateCount, txtGenerateCount, btnGenerateOrders
            });

            lblTodayRevenue.Text = "今日营业额：0";
            lblTodayRevenue.Location = new Point(14, 12);
            lblTodayRevenue.AutoSize = true;
            lblFilterSummary.Text = "筛选结果：0 单，合计 0.00";
            lblFilterSummary.Location = new Point(260, 16);
            lblFilterSummary.AutoSize = true;

            lblStatStart.Text = "从：";
            lblStatStart.Location = new Point(260, 52);
            lblStatStart.AutoSize = true;
            dtpStatStart.Format = DateTimePickerFormat.Custom;
            dtpStatStart.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            dtpStatStart.Location = new Point(300, 48);
            dtpStatStart.Size = new Size(250, 30);

            lblStatEnd.Text = "到：";
            lblStatEnd.Location = new Point(560, 52);
            lblStatEnd.AutoSize = true;
            dtpStatEnd.Format = DateTimePickerFormat.Custom;
            dtpStatEnd.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            dtpStatEnd.Location = new Point(600, 48);
            dtpStatEnd.Size = new Size(250, 30);

            btnApplyStatsFilter.Text = "应用筛选";
            btnApplyStatsFilter.Location = new Point(860, 46);
            btnApplyStatsFilter.Size = new Size(120, 34);
            btnStatsClearFilter.Text = "清除筛选";
            btnStatsClearFilter.Location = new Point(860, 10);
            btnStatsClearFilter.Size = new Size(120, 34);

            lblGenStart.Text = "生成开始:";
            lblGenStart.Location = new Point(14, 98);
            lblGenStart.AutoSize = true;
            dtpGenStart.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            dtpGenStart.Format = DateTimePickerFormat.Custom;
            dtpGenStart.Location = new Point(92, 94);
            dtpGenStart.Size = new Size(240, 30);
            lblGenEnd.Text = "生成结束:";
            lblGenEnd.Location = new Point(342, 98);
            lblGenEnd.AutoSize = true;
            dtpGenEnd.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            dtpGenEnd.Format = DateTimePickerFormat.Custom;
            dtpGenEnd.Location = new Point(420, 94);
            dtpGenEnd.Size = new Size(240, 30);

            lblGenerateAmount.Text = "目标金额";
            lblGenerateAmount.Location = new Point(670, 98);
            lblGenerateAmount.AutoSize = true;
            txtGenerateAmount.Location = new Point(748, 94);
            txtGenerateAmount.Size = new Size(110, 30);

            lblGenerateCount.Text = "订单数量";
            lblGenerateCount.Location = new Point(868, 98);
            lblGenerateCount.AutoSize = true;
            txtGenerateCount.Location = new Point(946, 94);
            txtGenerateCount.Size = new Size(100, 30);

            btnGenerateOrders.Text = "生成订单";
            btnGenerateOrders.Location = new Point(946, 130);
            btnGenerateOrders.Size = new Size(100, 34);

            dgvStatsOrders.Dock = DockStyle.Fill;
            dgvStatsOrders.ReadOnly = true;
            dgvStatsOrders.AllowUserToAddRows = false;
            dgvStatsOrders.AllowUserToDeleteRows = false;
            dgvStatsOrders.RowHeadersVisible = false;
            dgvStatsOrders.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1300, 680);
            Controls.Add(panelMain);
            Controls.Add(panelNav);
            MinimumSize = new Size(1200, 680);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "超市收银管理系统";

            ((System.ComponentModel.ISupportInitialize)numQuantity).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvCart).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvProducts).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvOrders).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvOrderItems).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvStatsOrders).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private Panel panelNav;
        private Button btnNavStats;
        private Button btnNavOrders;
        private Button btnNavProducts;
        private Button btnNavCashier;
        private Label lblNavTitle;
        private Panel panelMain;
        private TabControl tabMain;
        private TabPage tabCashier;
        private Panel panelCashierLeft;
        private Panel panelCashierCenter;
        private Panel panelCashierRight;
        private TextBox txtProductId;
        private NumericUpDown numQuantity;
        private Label lblCashier;
        private ComboBox cmbCashier;
        private Button btnAddToCart;
        private Label lblInputTitle;
        private Label lblId;
        private Label lblQty;
        private DataGridView dgvCart;
        private Label lblTotalAmount;
        private Button btnCheckout;
        private Button btnClearCart;
        private TabPage tabProducts;
        private Panel panelProductTop;
        private Button btnProductAdd;
        private Button btnProductEditPrice;
        private Button btnProductDelete;
        private Button btnProductRefresh;
        private Button btnProductFilterCategory;
        private Button btnProductFilterSupplier;
        private Button btnProductClearFilter;
        private Label lblProductCategoryFilter;
        private TextBox txtProductCategoryFilter;
        private Label lblProductSupplierFilter;
        private TextBox txtProductSupplierFilter;
        private Button btnProductApplyInputFilter;
        private Button btnProductAdjustStock;
        private DataGridView dgvProducts;
        private TabPage tabOrders;
        private Panel panelOrderTop;
        private Label lblOrderSummary;
        private Label lblOrderIncome;
        private Button btnOrderRefresh;
        private Button btnOrderPrint;
        private Button btnOrderToday;
        private Button btnOrderYesterday;
        private Button btnOrderMonth;
        private Label lblOrderIdFilter;
        private TextBox txtOrderIdFilter;
        private Label lblOrderCashierIdFilter;
        private TextBox txtOrderCashierIdFilter;
        private Label lblOrderCashierNameFilter;
        private TextBox txtOrderCashierNameFilter;
        private Label lblOrderTimeFrom;
        private DateTimePicker dtpOrderStart;
        private Label lblOrderTimeTo;
        private DateTimePicker dtpOrderEnd;
        private Button btnOrderApplyFilter;
        private Button btnOrderClearFilter;
        private TableLayoutPanel tableOrdersLayout;
        private DataGridView dgvOrders;
        private Panel panelOrderDetailContainer;
        private Panel panelOrderDetailTop;
        private Label lblOrderItemsTitle;
        private DataGridView dgvOrderItems;
        private TabPage tabStats;
        private Panel panelStatsTop;
        private Label lblTodayRevenue;
        private Label lblFilterSummary;
        private Label lblStatStart;
        private DateTimePicker dtpStatStart;
        private Label lblStatEnd;
        private DateTimePicker dtpStatEnd;
        private Button btnApplyStatsFilter;
        private Button btnStatsClearFilter;
        private Label lblGenStart;
        private DateTimePicker dtpGenStart;
        private Label lblGenEnd;
        private DateTimePicker dtpGenEnd;
        private Label lblGenerateAmount;
        private TextBox txtGenerateAmount;
        private Label lblGenerateCount;
        private TextBox txtGenerateCount;
        private Button btnGenerateOrders;
        private DataGridView dgvStatsOrders;
    }
}
