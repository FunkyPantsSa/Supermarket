using System.Globalization;
using System.Text;
using Supermarket.BLL;
using Supermarket.Entities;
using System.Windows.Forms;
using System.Drawing;

namespace Supermarket.UI
{
    /// <summary>
    /// 收银流水面板（采用和订单管理、商品管理一样的布局：顶部工具栏+下方DataGridView）
    /// </summary>
    public partial class TransactionLogPanel : UserControl
    {
        private readonly TransactionLogBLL _logBLL;
        private readonly string _dbPath;
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
            _dbPath = dbPath;
            _logBLL = new TransactionLogBLL(dbPath);
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            Font = new Font("Microsoft YaHei UI", 9F);

            // 顶部面板（和订单管理、商品管理一样的布局）
            _panelTop = new Panel { Dock = DockStyle.Top, Height = 120 };
            
            // 顶部汇总信息
            _lblTodaySummary = new Label
            {
                Text = "今日汇总：总收入 0.00 元，成交 0 笔，平均客单价 0.00 元",
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                Location = new Point(12, 12)
            };

            // 筛选控件
            var lblStart = new Label { Text = "开始时间：", Location = new Point(12, 50), AutoSize = true };
            _dtpStart = new DateTimePicker
            {
                Location = new Point(90, 46),
                Size = new Size(200, 30),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm:ss"
            };
            _dtpStart.Value = DateTime.Today;

            var lblEnd = new Label { Text = "结束时间：", Location = new Point(300, 50), AutoSize = true };
            _dtpEnd = new DateTimePicker
            {
                Location = new Point(378, 46),
                Size = new Size(200, 30),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm:ss"
            };
            _dtpEnd.Value = DateTime.Now;

            _btnRefresh = new Button
            {
                Text = "刷新",
                Location = new Point(590, 44),
                Size = new Size(100, 34)
            };
            _btnRefresh.Click += BtnRefresh_Click;

            _btnClearFilter = new Button
            {
                Text = "清除筛选",
                Location = new Point(700, 44),
                Size = new Size(100, 34)
            };
            _btnClearFilter.Click += BtnClearFilter_Click;

            _btnExport = new Button
            {
                Text = "导出CSV",
                Location = new Point(810, 44),
                Size = new Size(100, 34)
            };
            _btnExport.Click += BtnExport_Click;

            _panelTop.Controls.AddRange(new Control[]
            {
                _lblTodaySummary, lblStart, _dtpStart, lblEnd, _dtpEnd,
                _btnRefresh, _btnClearFilter, _btnExport
            });

            // 表格标题（和订单管理一样）
            var panelTableTitle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38
            };
            var lblTableTitle = new Label
            {
                Text = "收银流水明细",
                Location = new Point(12, 8),
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            panelTableTitle.Controls.Add(lblTableTitle);

            // 主数据表格（和订单管理、商品管理一样，Dock.Fill）
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

            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "流水ID",
                DataPropertyName = "LogId",
                Width = 100
            });
            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "类型",
                DataPropertyName = "Type",
                Width = 100
            });
            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "金额",
                DataPropertyName = "Amount",
                Width = 120
            });
            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "详情",
                DataPropertyName = "Detail",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "时间",
                DataPropertyName = "Timestamp",
                Width = 180
            });

            // 修复空引用错误：在设置DataSource之前先绑定事件，但添加空值检查
            _dgvLogs.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _dgvLogs.Rows.Count) return;
                if (_dgvLogs.Rows[e.RowIndex].DataBoundItem is not TransactionLog log) return;

                var amountCol = _dgvLogs.Columns["Amount"];
                var timestampCol = _dgvLogs.Columns["Timestamp"];
                
                if (amountCol != null && e.ColumnIndex == amountCol.Index)
                {
                    e.Value = log.Amount.ToString("F2", CultureInfo.InvariantCulture);
                }
                else if (timestampCol != null && e.ColumnIndex == timestampCol.Index)
                {
                    e.Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                }
            };

            Controls.Add(_panelTop);
            Controls.Add(panelTableTitle);
            Controls.Add(_dgvLogs);
        }

        public void LoadData()
        {
            try
            {
                var logs = _logBLL.GetLogs(_dtpStart.Value, _dtpEnd.Value);
                _dgvLogs.DataSource = null; // 先清空，避免绑定问题
                _dgvLogs.DataSource = logs;

                // 更新今日汇总
                var (totalRevenue, orderCount, avgOrderAmount) = _logBLL.GetTodaySummary();
                _lblTodaySummary.Text = 
                    $"今日汇总：总收入 {totalRevenue:F2} 元，成交 {orderCount} 笔，平均客单价 {avgOrderAmount:F2} 元";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            LoadData();
        }

        private void BtnClearFilter_Click(object? sender, EventArgs e)
        {
            _dtpStart.Value = DateTime.Today;
            _dtpEnd.Value = DateTime.Now;
            LoadData();
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
                    sb.AppendLine(
                        $"{log.LogId},\"{log.Type}\",{log.Amount.ToString(CultureInfo.InvariantCulture)},\"{detail}\",{log.Timestamp:yyyy-MM-dd HH:mm:ss}");
                }

                File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("流水已导出。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
