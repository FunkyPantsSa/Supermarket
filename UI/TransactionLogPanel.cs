using System.Globalization;
using System.Text;
using Supermarket.BLL;
using Supermarket.Entities;

namespace Supermarket.UI
{
    public partial class TransactionLogPanel : UserControl
    {
        private readonly TransactionLogBLL _logBLL;
        public event Action<string>? OrderDetailRequested;
        private DataGridView _dgvLogs = null!;
        private Label _lblTodaySummary = null!;
        private DateTimePicker _dtpStart = null!;
        private DateTimePicker _dtpEnd = null!;
        private Button _btnRefresh = null!;
        private Button _btnExport = null!;
        private Button _btnClearFilter = null!;
        private Button _btnToday = null!;
        private Button _btnMonth = null!;
        private Panel _panelTop = null!;

        public TransactionLogPanel(string dbPath)
        {
            _logBLL = new TransactionLogBLL(dbPath);
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            AppTheme.StyleCard(this);
            Font = new Font("Microsoft YaHei UI", 9F);

            _panelTop = new Panel { Dock = DockStyle.Top, Height = 156, Padding = new Padding(12), BackColor = AppTheme.Card };
            _lblTodaySummary = new Label
            {
                Text = "\u4ECA\u65E5\u6C47\u603B\uFF1A\u603B\u6536\u5165 0.00 \u5143\uFF0C\u6210\u4EA4 0 \u7B14\uFF0C\u5E73\u5747\u5BA2\u5355\u4EF7 0.00 \u5143",
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblStart = new Label { Text = "\u5F00\u59CB\u65F6\u95F4", Dock = DockStyle.Fill, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
            _dtpStart = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm:ss",
                Value = DateTime.Today
            };

            var lblEnd = new Label { Text = "\u7ED3\u675F\u65F6\u95F4", Dock = DockStyle.Fill, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
            _dtpEnd = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm:ss",
                Value = DateTime.Now
            };

            _btnToday = new Button { Text = "\u4ECA\u65E5\u6D41\u6C34", Width = 110, Height = 34 };
            _btnToday.Click += (s, e) =>
            {
                _dtpStart.Value = DateTime.Today;
                _dtpEnd.Value = DateTime.Now;
                LoadData();
            };
            AppTheme.StyleButton(_btnToday, primary: true);

            _btnMonth = new Button { Text = "\u672C\u6708\u6D41\u6C34", Width = 110, Height = 34 };
            _btnMonth.Click += (s, e) =>
            {
                _dtpStart.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                _dtpEnd.Value = DateTime.Now;
                LoadData();
            };
            AppTheme.StyleButton(_btnMonth);

            _btnRefresh = new Button { Text = "\u5237\u65B0", Width = 100, Height = 34 };
            _btnRefresh.Click += (s, e) => LoadData();
            AppTheme.StyleButton(_btnRefresh, primary: true);

            _btnClearFilter = new Button { Text = "\u6E05\u9664\u7B5B\u9009", Width = 112, Height = 34 };
            _btnClearFilter.Click += (s, e) =>
            {
                _dtpStart.Value = DateTime.Today;
                _dtpEnd.Value = DateTime.Now;
                LoadData();
            };
            AppTheme.StyleButton(_btnClearFilter);

            _btnExport = new Button { Text = "\u5BFC\u51FACSV", Width = 100, Height = 34 };
            _btnExport.Click += BtnExport_Click;
            AppTheme.StyleButton(_btnExport);

            var summaryPanel = new Panel { Dock = DockStyle.Top, Height = 42 };
            summaryPanel.Controls.Add(_lblTodaySummary);

            var filterLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                ColumnCount = 4,
                Padding = new Padding(0, 8, 0, 0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            filterLayout.Controls.Add(lblStart, 0, 0);
            filterLayout.Controls.Add(_dtpStart, 1, 0);
            filterLayout.Controls.Add(lblEnd, 2, 0);
            filterLayout.Controls.Add(_dtpEnd, 3, 0);

            var filterHost = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52
            };
            filterHost.Controls.Add(filterLayout);

            var buttonRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                WrapContents = true,
                Padding = new Padding(0, 10, 0, 0)
            };
            buttonRow.Controls.AddRange(new Control[] { _btnToday, _btnMonth, _btnRefresh, _btnClearFilter, _btnExport });

            _panelTop.Controls.Add(buttonRow);
            _panelTop.Controls.Add(filterHost);
            _panelTop.Controls.Add(summaryPanel);

            var panelTableTitle = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = AppTheme.Card };
            panelTableTitle.Controls.Add(new Label
            {
                Text = "\u6536\u94F6\u6D41\u6C34\u660E\u7EC6",
                Location = new Point(12, 8),
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary
            });

            _dgvLogs = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            AppTheme.StyleGrid(_dgvLogs);
            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "LogId", HeaderText = "\u6D41\u6C34ID", DataPropertyName = "LogId", Width = 100 });
            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "\u7C7B\u578B", DataPropertyName = "Type", Width = 100 });
            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "CashierName", HeaderText = "\u6536\u94F6\u5458", DataPropertyName = "CashierName", Width = 120 });
            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "\u91D1\u989D", DataPropertyName = "Amount", Width = 120 });
            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Detail", HeaderText = "\u8BE6\u60C5", DataPropertyName = "Detail", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Timestamp", HeaderText = "\u65F6\u95F4", DataPropertyName = "Timestamp", Width = 180 });
            _dgvLogs.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "OrderDetail",
                HeaderText = "订单详情",
                Text = "查看订单",
                UseColumnTextForButtonValue = true,
                Width = 110
            });

            _dgvLogs.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _dgvLogs.Rows.Count) return;
                if (_dgvLogs.Rows[e.RowIndex].DataBoundItem is not TransactionLog log) return;
                if (_dgvLogs.Columns[e.ColumnIndex].Name == "Amount")
                {
                    e.Value = log.Amount.ToString("F2", CultureInfo.InvariantCulture);
                }
                else if (_dgvLogs.Columns[e.ColumnIndex].Name == "Timestamp")
                {
                    e.Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                }
            };

            _dgvLogs.CellContentClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                if (_dgvLogs.Columns[e.ColumnIndex].Name != "OrderDetail") return;
                if (_dgvLogs.Rows[e.RowIndex].DataBoundItem is not TransactionLog log) return;

                var orderId = ExtractOrderId(log.Detail);
                if (string.IsNullOrWhiteSpace(orderId))
                {
                    MessageBox.Show("该流水未找到可跳转的订单号。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                OrderDetailRequested?.Invoke(orderId);
            };

            Controls.Add(_dgvLogs);
            Controls.Add(panelTableTitle);
            Controls.Add(_panelTop);
        }

        public void LoadData()
        {
            try
            {
                var logs = _logBLL.GetLogs(_dtpStart.Value, _dtpEnd.Value);
                _dgvLogs.DataSource = null;
                _dgvLogs.DataSource = logs;

                var (totalRevenue, orderCount, avgOrderAmount) = _logBLL.GetTodaySummary();
                var (monthRevenue, monthCount, monthAvg) = _logBLL.GetMonthSummary();
                _lblTodaySummary.Text = $"\u4ECA\u65E5\u6C47\u603B\uFF1A{totalRevenue:F2} \u5143 / {orderCount} \u7B14 / \u5747\u5355 {avgOrderAmount:F2} \u5143    \u672C\u6708\u6C47\u603B\uFF1A{monthRevenue:F2} \u5143 / {monthCount} \u7B14 / \u5747\u5355 {monthAvg:F2} \u5143";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"\u52A0\u8F7D\u6570\u636E\u5931\u8D25\uFF1A{ex.Message}", "\u9519\u8BEF", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"transaction_logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var logs = _logBLL.GetLogs(_dtpStart.Value, _dtpEnd.Value);
                var sb = new StringBuilder();
                sb.AppendLine("LogID,Type,CashierName,Amount,Detail,Timestamp");
                foreach (var log in logs)
                {
                    var detail = log.Detail.Replace("\"", "\"\"");
                    var cashierName = log.CashierName.Replace("\"", "\"\"");
                    sb.AppendLine($"{log.LogId},\"{log.Type}\",\"{cashierName}\",{log.Amount.ToString(CultureInfo.InvariantCulture)},\"{detail}\",{log.Timestamp:yyyy-MM-dd HH:mm:ss}");
                }

                File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("\u6D41\u6C34\u5DF2\u5BFC\u51FA\u3002", "\u5B8C\u6210", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"\u5BFC\u51FA\u5931\u8D25\uFF1A{ex.Message}", "\u9519\u8BEF", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string ExtractOrderId(string detail)
        {
            const string prefix = "订单 ";
            var start = detail.IndexOf(prefix, StringComparison.Ordinal);
            if (start < 0)
            {
                return "";
            }

            start += prefix.Length;
            var end = detail.IndexOf(' ', start);
            return end > start ? detail[start..end].Trim() : detail[start..].Trim();
        }
    }
}
