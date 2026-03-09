using System.Globalization;
using System.Text;
using Supermarket.BLL;
using Supermarket.Entities;
using System.Windows.Forms;
using System.Drawing;

namespace Supermarket.UI
{
    /// <summary>
    /// 收银流水管理页面（采用和统计页面一样的2x2布局）
    /// </summary>
    public partial class TransactionLogForm : Form
    {
        private readonly TransactionLogBLL _logBLL;
        private readonly string _dbPath;
        private DataGridView _dgvLogs = null!;
        private DataGridView _dgvTodaySummary = null!;
        private DataGridView _dgvTypeSummary = null!;
        private DataGridView _dgvRecentLogs = null!;
        private Label _lblTodaySummary = null!;
        private DateTimePicker _dtpStart = null!;
        private DateTimePicker _dtpEnd = null!;
        private Button _btnRefresh = null!;
        private Button _btnExport = null!;
        private Button _btnClearFilter = null!;

        public TransactionLogForm(string dbPath)
        {
            _dbPath = dbPath;
            _logBLL = new TransactionLogBLL(dbPath);
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "收银流水管理";
            Size = new Size(1200, 700);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9F);

            // 顶部面板
            var panelTop = new Panel { Dock = DockStyle.Top, Height = 120 };
            
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

            panelTop.Controls.AddRange(new Control[]
            {
                _lblTodaySummary, lblStart, _dtpStart, lblEnd, _dtpEnd,
                _btnRefresh, _btnClearFilter, _btnExport
            });

            // 主内容区域 - 2x2布局（和统计页面一样）
            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(8)
            };

            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // 左上：全部流水记录
            var panelAllLogs = new Panel { Dock = DockStyle.Fill };
            var lblAllLogs = new Label
            {
                Text = "全部流水记录",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
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
                Width = 80
            });
            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "类型",
                DataPropertyName = "Type",
                Width = 80
            });
            _dgvLogs.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "金额",
                DataPropertyName = "Amount",
                Width = 100
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
                Width = 150
            });
            _dgvLogs.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                if (_dgvLogs.Rows[e.RowIndex].DataBoundItem is not TransactionLog log) return;

                if (e.ColumnIndex == _dgvLogs.Columns["Amount"].Index)
                {
                    e.Value = log.Amount.ToString("F2", CultureInfo.InvariantCulture);
                }
                else if (e.ColumnIndex == _dgvLogs.Columns["Timestamp"].Index)
                {
                    e.Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                }
            };
            panelAllLogs.Controls.Add(_dgvLogs);
            panelAllLogs.Controls.Add(lblAllLogs);

            // 右上：今日汇总
            var panelTodaySummary = new Panel { Dock = DockStyle.Fill };
            var lblTodaySummaryTitle = new Label
            {
                Text = "今日汇总",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            _dgvTodaySummary = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            _dgvTodaySummary.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "指标",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            _dgvTodaySummary.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "数值",
                Width = 150
            });
            panelTodaySummary.Controls.Add(_dgvTodaySummary);
            panelTodaySummary.Controls.Add(lblTodaySummaryTitle);

            // 左下：类型汇总
            var panelTypeSummary = new Panel { Dock = DockStyle.Fill };
            var lblTypeSummary = new Label
            {
                Text = "类型汇总",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            _dgvTypeSummary = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            _dgvTypeSummary.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "类型",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            _dgvTypeSummary.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "笔数",
                Width = 100
            });
            _dgvTypeSummary.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "金额",
                Width = 120
            });
            panelTypeSummary.Controls.Add(_dgvTypeSummary);
            panelTypeSummary.Controls.Add(lblTypeSummary);

            // 右下：最近流水
            var panelRecentLogs = new Panel { Dock = DockStyle.Fill };
            var lblRecentLogs = new Label
            {
                Text = "最近流水（Top 20）",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            _dgvRecentLogs = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            _dgvRecentLogs.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "类型",
                Width = 80
            });
            _dgvRecentLogs.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "金额",
                Width = 100
            });
            _dgvRecentLogs.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "时间",
                Width = 150
            });
            _dgvRecentLogs.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "详情",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            panelRecentLogs.Controls.Add(_dgvRecentLogs);
            panelRecentLogs.Controls.Add(lblRecentLogs);

            tableLayout.Controls.Add(panelAllLogs, 0, 0);
            tableLayout.Controls.Add(panelTodaySummary, 1, 0);
            tableLayout.Controls.Add(panelTypeSummary, 0, 1);
            tableLayout.Controls.Add(panelRecentLogs, 1, 1);

            Controls.Add(panelTop);
            Controls.Add(tableLayout);
        }

        private void LoadData()
        {
            var logs = _logBLL.GetLogs(_dtpStart.Value, _dtpEnd.Value);
            _dgvLogs.DataSource = logs;

            // 更新今日汇总
            var (totalRevenue, orderCount, avgOrderAmount) = _logBLL.GetTodaySummary();
            _lblTodaySummary.Text = 
                $"今日汇总：总收入 {totalRevenue:F2} 元，成交 {orderCount} 笔，平均客单价 {avgOrderAmount:F2} 元";

            // 今日汇总表格
            var todaySummaryRows = new[]
            {
                new { Metric = "总收入", Value = totalRevenue.ToString("F2") + " 元" },
                new { Metric = "成交笔数", Value = orderCount.ToString() + " 笔" },
                new { Metric = "平均客单价", Value = avgOrderAmount.ToString("F2") + " 元" }
            };
            _dgvTodaySummary.DataSource = todaySummaryRows.ToList();

            // 类型汇总
            var typeGroups = logs.GroupBy(l => l.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count(),
                    Amount = g.Sum(x => x.Amount).ToString("F2") + " 元"
                })
                .OrderByDescending(x => x.Count)
                .ToList();
            _dgvTypeSummary.DataSource = typeGroups;

            // 最近流水（Top 20）
            var recentLogs = logs.OrderByDescending(l => l.Timestamp)
                .Take(20)
                .Select(l => new
                {
                    Type = l.Type,
                    Amount = l.Amount.ToString("F2") + " 元",
                    Time = l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    Detail = l.Detail
                })
                .ToList();
            _dgvRecentLogs.DataSource = recentLogs;
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
    }
}
