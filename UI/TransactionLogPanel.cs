using System.Globalization;
using System.Text;
using Supermarket.BLL;
using Supermarket.Entities;

namespace Supermarket.UI
{
    public partial class TransactionLogPanel : UserControl
    {
        private readonly TransactionLogBLL _logBLL;
        private DataGridView _dgvLogs = null!;
        private Label _lblTodaySummary = null!;
        private DateTimePicker _dtpStart = null!;
        private DateTimePicker _dtpEnd = null!;
        private Button _btnRefresh = null!;
        private Button _btnExport = null!;
        private Button _btnClearFilter = null!;
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
            Font = new Font("Microsoft YaHei UI", 9F);

            _panelTop = new Panel { Dock = DockStyle.Top, Height = 120 };
            _lblTodaySummary = new Label
            {
                Text = "\u4ECA\u65E5\u6C47\u603B\uFF1A\u603B\u6536\u5165 0.00 \u5143\uFF0C\u6210\u4EA4 0 \u7B14\uFF0C\u5E73\u5747\u5BA2\u5355\u4EF7 0.00 \u5143",
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                Location = new Point(12, 12)
            };

            var lblStart = new Label { Text = "\u5F00\u59CB\u65F6\u95F4\uFF1A", Location = new Point(12, 50), AutoSize = true };
            _dtpStart = new DateTimePicker
            {
                Location = new Point(90, 46),
                Size = new Size(200, 30),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm:ss",
                Value = DateTime.Today
            };

            var lblEnd = new Label { Text = "\u7ED3\u675F\u65F6\u95F4\uFF1A", Location = new Point(300, 50), AutoSize = true };
            _dtpEnd = new DateTimePicker
            {
                Location = new Point(378, 46),
                Size = new Size(200, 30),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm:ss",
                Value = DateTime.Now
            };

            _btnRefresh = new Button { Text = "\u5237\u65B0", Location = new Point(590, 44), Size = new Size(100, 34) };
            _btnRefresh.Click += (s, e) => LoadData();

            _btnClearFilter = new Button { Text = "\u6E05\u9664\u7B5B\u9009", Location = new Point(700, 44), Size = new Size(100, 34) };
            _btnClearFilter.Click += (s, e) =>
            {
                _dtpStart.Value = DateTime.Today;
                _dtpEnd.Value = DateTime.Now;
                LoadData();
            };

            _btnExport = new Button { Text = "\u5BFC\u51FACSV", Location = new Point(810, 44), Size = new Size(100, 34) };
            _btnExport.Click += BtnExport_Click;

            _panelTop.Controls.AddRange(new Control[]
            {
                _lblTodaySummary, lblStart, _dtpStart, lblEnd, _dtpEnd, _btnRefresh, _btnClearFilter, _btnExport
            });

            var panelTableTitle = new Panel { Dock = DockStyle.Top, Height = 38 };
            panelTableTitle.Controls.Add(new Label
            {
                Text = "\u6536\u94F6\u6D41\u6C34\u660E\u7EC6",
                Location = new Point(12, 8),
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
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

            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "LogId", HeaderText = "\u6D41\u6C34ID", DataPropertyName = "LogId", Width = 100 });
            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "\u7C7B\u578B", DataPropertyName = "Type", Width = 100 });
            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "\u91D1\u989D", DataPropertyName = "Amount", Width = 120 });
            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Detail", HeaderText = "\u8BE6\u60C5", DataPropertyName = "Detail", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Timestamp", HeaderText = "\u65F6\u95F4", DataPropertyName = "Timestamp", Width = 180 });

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
                _lblTodaySummary.Text = $"\u4ECA\u65E5\u6C47\u603B\uFF1A\u603B\u6536\u5165 {totalRevenue:F2} \u5143\uFF0C\u6210\u4EA4 {orderCount} \u7B14\uFF0C\u5E73\u5747\u5BA2\u5355\u4EF7 {avgOrderAmount:F2} \u5143";
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
                sb.AppendLine("LogID,Type,Amount,Detail,Timestamp");
                foreach (var log in logs)
                {
                    var detail = log.Detail.Replace("\"", "\"\"");
                    sb.AppendLine($"{log.LogId},\"{log.Type}\",{log.Amount.ToString(CultureInfo.InvariantCulture)},\"{detail}\",{log.Timestamp:yyyy-MM-dd HH:mm:ss}");
                }

                File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("\u6D41\u6C34\u5DF2\u5BFC\u51FA\u3002", "\u5B8C\u6210", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"\u5BFC\u51FA\u5931\u8D25\uFF1A{ex.Message}", "\u9519\u8BEF", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
