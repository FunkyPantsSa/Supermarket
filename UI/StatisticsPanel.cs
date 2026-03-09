using Supermarket.BLL;
using System.Windows.Forms;
using System.Drawing;

namespace Supermarket.UI
{
    /// <summary>
    /// 统计面板（使用DataGridView展示数据，避免外部图表库依赖）
    /// </summary>
    public partial class StatisticsPanel : UserControl
    {
        private readonly StatisticsBLL _statisticsBLL;
        private DataGridView _dgvSalesTrend = null!;
        private DataGridView _dgvCategory = null!;
        private DataGridView _dgvTopProducts = null!;
        private DataGridView _dgvProfit = null!;

        public StatisticsPanel(string dbPath)
        {
            _statisticsBLL = new StatisticsBLL(dbPath);
            InitializeComponent();
            LoadCharts();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
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

            // 销售走势图（用表格展示）
            var panelSalesTrend = new Panel { Dock = DockStyle.Fill };
            var lblSalesTrend = new Label
            {
                Text = "销售走势（近30天）",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            _dgvSalesTrend = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            _dgvSalesTrend.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "日期",
                Width = 120
            });
            _dgvSalesTrend.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "销售额（元）",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            panelSalesTrend.Controls.Add(_dgvSalesTrend);
            panelSalesTrend.Controls.Add(lblSalesTrend);

            // 分类占比（用表格展示）
            var panelCategory = new Panel { Dock = DockStyle.Fill };
            var lblCategory = new Label
            {
                Text = "分类销售占比",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            _dgvCategory = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            _dgvCategory.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "分类",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            _dgvCategory.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "销售额（元）",
                Width = 150
            });
            _dgvCategory.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "占比",
                Width = 100
            });
            panelCategory.Controls.Add(_dgvCategory);
            panelCategory.Controls.Add(lblCategory);

            // 畅销排行
            var panelTopProducts = new Panel { Dock = DockStyle.Fill };
            var lblTopProducts = new Label
            {
                Text = "畅销商品排行（Top 10）",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            _dgvTopProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            _dgvTopProducts.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "排名",
                Width = 60,
                ReadOnly = true
            });
            _dgvTopProducts.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "商品名称",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            _dgvTopProducts.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "销售数量",
                Width = 100
            });
            _dgvTopProducts.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "销售金额",
                Width = 120
            });
            panelTopProducts.Controls.Add(_dgvTopProducts);
            panelTopProducts.Controls.Add(lblTopProducts);

            // 毛利估算
            var panelProfit = new Panel { Dock = DockStyle.Fill };
            var lblProfit = new Label
            {
                Text = "毛利估算（Top 10）",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
            };
            _dgvProfit = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            _dgvProfit.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "排名",
                Width = 60
            });
            _dgvProfit.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "商品名称",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            _dgvProfit.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "估算毛利",
                Width = 120
            });
            panelProfit.Controls.Add(_dgvProfit);
            panelProfit.Controls.Add(lblProfit);

            tableLayout.Controls.Add(panelSalesTrend, 0, 0);
            tableLayout.Controls.Add(panelCategory, 1, 0);
            tableLayout.Controls.Add(panelTopProducts, 0, 1);
            tableLayout.Controls.Add(panelProfit, 1, 1);

            Controls.Add(tableLayout);
        }

        public async void LoadCharts()
        {
            try
            {
                await Task.Run(() =>
                {
                    // 需要在UI线程更新数据
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            LoadSalesTrendData();
                            LoadCategoryData();
                            LoadTopProducts();
                            LoadProfitEstimate();
                        }));
                    }
                    else
                    {
                        LoadSalesTrendData();
                        LoadCategoryData();
                        LoadTopProducts();
                        LoadProfitEstimate();
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载统计数据失败：{ex.Message}");
            }
        }

        private void LoadSalesTrendData()
        {
            try
            {
                var trendData = _statisticsBLL.GetSalesTrend(30);
                var rows = trendData.OrderBy(k => k.Key).Select(kvp => new
                {
                    Date = kvp.Key.ToString("MM-dd"),
                    Amount = kvp.Value.ToString("F2")
                }).ToList();

                _dgvSalesTrend.DataSource = rows;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载销售走势数据失败：{ex.Message}");
            }
        }

        private void LoadCategoryData()
        {
            try
            {
                var categoryData = _statisticsBLL.GetCategorySales();
                var total = categoryData.Values.Sum();
                var rows = categoryData.OrderByDescending(kvp => kvp.Value).Select(kvp => new
                {
                    Category = kvp.Key,
                    Amount = kvp.Value.ToString("F2"),
                    Percentage = total > 0 ? (kvp.Value / total * 100).ToString("F1") + "%" : "0%"
                }).ToList();

                _dgvCategory.DataSource = rows;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载分类数据失败：{ex.Message}");
            }
        }

        private void LoadTopProducts()
        {
            var topProducts = _statisticsBLL.GetTopSellingProducts(10);
            var rows = topProducts.Select((p, index) => new
            {
                Rank = index + 1,
                ProductName = p.ProductName,
                Quantity = p.Quantity,
                TotalAmount = p.TotalAmount.ToString("F2")
            }).ToList();

            _dgvTopProducts.DataSource = rows;
        }

        private void LoadProfitEstimate()
        {
            var profitData = _statisticsBLL.GetGrossProfitEstimate()
                .OrderByDescending(kvp => kvp.Value)
                .Take(10)
                .Select((kvp, index) => new
                {
                    Rank = index + 1,
                    ProductName = kvp.Key,
                    Profit = kvp.Value.ToString("F2")
                })
                .ToList();

            _dgvProfit.DataSource = profitData;
        }
    }
}
