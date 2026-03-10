using Supermarket.Entities;
using Supermarket.Services;

namespace Supermarket.UI
{
    public sealed class StaffManagementPanel : UserControl
    {
        private readonly AuthService _authService;
        private readonly DataGridView _grid = new();
        private readonly Label _lblSummary = new();
        private readonly Button _btnRefresh = new();
        private readonly Button _btnRename = new();
        private readonly Button _btnResetPassword = new();

        public StaffManagementPanel(AuthService authService)
        {
            _authService = authService;
            InitializeComponent();
            LoadUsers();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            AppTheme.StyleCard(this);

            var host = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(18)
            };
            host.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            host.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var title = AppTheme.CreateSectionTitle("人员管理");
            title.Dock = DockStyle.Fill;

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = true
            };

            _btnRefresh.Text = "刷新账号";
            _btnRefresh.Width = 120;
            _btnRefresh.Click += (s, e) => LoadUsers();
            AppTheme.StyleButton(_btnRefresh);

            _btnRename.Text = "修改姓名";
            _btnRename.Width = 120;
            _btnRename.Click += BtnRename_Click;
            AppTheme.StyleButton(_btnRename);

            _btnResetPassword.Text = "重置密码";
            _btnResetPassword.Width = 140;
            _btnResetPassword.Click += BtnResetPassword_Click;
            AppTheme.StyleButton(_btnResetPassword, primary: true);

            _lblSummary.AutoSize = true;
            _lblSummary.Margin = new Padding(18, 10, 0, 0);
            _lblSummary.ForeColor = AppTheme.TextSecondary;

            toolbar.Controls.Add(_btnRefresh);
            toolbar.Controls.Add(_btnRename);
            toolbar.Controls.Add(_btnResetPassword);
            toolbar.Controls.Add(_lblSummary);

            _grid.Dock = DockStyle.Fill;
            _grid.AutoGenerateColumns = false;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.ReadOnly = true;
            AppTheme.StyleGrid(_grid);
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "登录账号", DataPropertyName = "UserName", Width = 140 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "姓名", DataPropertyName = "DisplayName", Width = 120 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "角色", DataPropertyName = "DisplayRole", Width = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "营业员编号", DataPropertyName = "CashierId", Width = 120 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "营业员名称", DataPropertyName = "CashierName", Width = 140 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "状态", Width = 80 });
            _grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;
                if (_grid.Rows[e.RowIndex].DataBoundItem is not AppUser user) return;
                if (_grid.Columns[e.ColumnIndex].HeaderText == "状态")
                {
                    e.Value = user.IsActive ? "启用" : "禁用";
                }
            };

            host.Controls.Add(title, 0, 0);
            host.Controls.Add(toolbar, 0, 1);
            host.Controls.Add(_grid, 0, 2);
            Controls.Add(host);
        }

        public void LoadUsers()
        {
            _authService.SyncCashierAccounts();
            var users = _authService.GetUsers();
            _grid.DataSource = null;
            _grid.DataSource = users;
            _lblSummary.Text = $"共 {users.Count} 个账号，可修改姓名或重置营业员密码。";
        }

        private void BtnResetPassword_Click(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem is not AppUser user)
            {
                MessageBox.Show("请先选择一个账号。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!user.IsCashier)
            {
                MessageBox.Show("这里只允许重置营业员账号。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"确认将账号 {user.UserName} 的密码重置为 123456？",
                "确认重置",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            if (_authService.ResetPassword(user.UserName, "123456", out var message))
            {
                MessageBox.Show($"{message}\n默认密码：123456", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadUsers();
                return;
            }

            MessageBox.Show(message, "失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void BtnRename_Click(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem is not AppUser user)
            {
                MessageBox.Show("请先选择一个账号。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new RenameDialog(user.DisplayName);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (_authService.UpdateDisplayName(user.UserName, dialog.DisplayName, out var message))
            {
                MessageBox.Show(message, "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadUsers();
                return;
            }

            MessageBox.Show(message, "失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private sealed class RenameDialog : Form
        {
            private readonly TextBox _txtName = new();

            public string DisplayName => _txtName.Text.Trim();

            public RenameDialog(string currentName)
            {
                Text = "修改姓名";
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                ClientSize = new Size(360, 150);
                AppTheme.StyleForm(this);

                var lblName = new Label
                {
                    Text = "姓名",
                    Left = 22,
                    Top = 26,
                    Width = 60,
                    ForeColor = AppTheme.TextPrimary
                };

                _txtName.Left = 84;
                _txtName.Top = 22;
                _txtName.Width = 240;
                _txtName.Text = currentName;
                AppTheme.StyleInput(_txtName);

                var btnOk = new Button { Text = "保存", Left = 168, Top = 86, Width = 72, Height = 34, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "取消", Left = 252, Top = 86, Width = 72, Height = 34, DialogResult = DialogResult.Cancel };
                AppTheme.StyleButton(btnOk, primary: true);
                AppTheme.StyleButton(btnCancel);

                Controls.AddRange(new Control[] { lblName, _txtName, btnOk, btnCancel });
                AcceptButton = btnOk;
                CancelButton = btnCancel;
            }
        }
    }
}
