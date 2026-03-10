using Supermarket.Entities;
using Supermarket.Services;
using Supermarket.UI;

namespace Supermarket
{
    public class LoginForm : Form
    {
        private readonly AuthService _authService;
        private readonly TextBox _txtUser = new();
        private readonly TextBox _txtPassword = new();
        private readonly Button _btnLogin = new();
        private readonly Button _btnExit = new();
        private readonly Button _btnChangePassword = new();

        public string LoginUserName => CurrentUser?.UserName ?? "";
        public AppUser? CurrentUser { get; private set; }

        public LoginForm(AuthService authService)
        {
            _authService = authService;
            InitializeUi();
        }

        private void InitializeUi()
        {
            Text = "登录";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(560, 420);
            ClientSize = new Size(760, 460);
            AppTheme.StyleForm(this);

            var shell = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = Color.FromArgb(236, 242, 248)
            };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(28)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 9
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var title = new Label
            {
                Text = "超市营业系统",
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold),
                ForeColor = AppTheme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var subTitle = new Label
            {
                Text = "管理员可管理全部页面，营业员登录后只显示自己的订单与收银工作台。",
                Dock = DockStyle.Fill,
                ForeColor = AppTheme.TextSecondary
            };

            var lblUser = new Label { Text = "账号", Dock = DockStyle.Fill, ForeColor = AppTheme.TextPrimary, TextAlign = ContentAlignment.BottomLeft };
            _txtUser.Dock = DockStyle.Fill;
            _txtUser.Text = "admin";
            AppTheme.StyleInput(_txtUser);

            var lblPassword = new Label { Text = "密码", Dock = DockStyle.Fill, ForeColor = AppTheme.TextPrimary, TextAlign = ContentAlignment.BottomLeft };
            _txtPassword.Dock = DockStyle.Fill;
            _txtPassword.PasswordChar = '*';
            AppTheme.StyleInput(_txtPassword);

            var helper = new Label
            {
                Text = "请输入账号和密码登录。管理员与营业员会按角色展示不同页面。",
                Dock = DockStyle.Fill,
                ForeColor = AppTheme.TextSecondary
            };

            var actionBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight
            };

            _btnLogin.Text = "登录";
            _btnLogin.Width = 132;
            _btnLogin.Height = 38;
            _btnLogin.Click += BtnLogin_Click;
            AppTheme.StyleButton(_btnLogin, primary: true);

            _btnChangePassword.Text = "修改密码";
            _btnChangePassword.Width = 132;
            _btnChangePassword.Height = 38;
            _btnChangePassword.Click += BtnChangePassword_Click;
            AppTheme.StyleButton(_btnChangePassword);

            _btnExit.Text = "退出";
            _btnExit.Width = 132;
            _btnExit.Height = 38;
            _btnExit.Click += (s, e) => Close();
            AppTheme.StyleButton(_btnExit);

            actionBar.Controls.AddRange(new Control[] { _btnLogin, _btnChangePassword, _btnExit });

            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(subTitle, 0, 1);
            layout.Controls.Add(lblUser, 0, 2);
            layout.Controls.Add(_txtUser, 0, 3);
            layout.Controls.Add(lblPassword, 0, 4);
            layout.Controls.Add(_txtPassword, 0, 5);
            layout.Controls.Add(helper, 0, 6);
            layout.Controls.Add(actionBar, 0, 7);
            card.Controls.Add(layout);
            shell.Controls.Add(card);
            Controls.Add(shell);

            AcceptButton = _btnLogin;
            CancelButton = _btnExit;
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            var user = _txtUser.Text.Trim();
            var password = _txtPassword.Text;
            if (_authService.Authenticate(user, password, out var currentUser) && currentUser != null)
            {
                CurrentUser = currentUser;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            MessageBox.Show("账号或密码错误，或该账号已被停用。", "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void BtnChangePassword_Click(object? sender, EventArgs e)
        {
            using var dialog = new ChangePasswordForm(_txtUser.Text.Trim());
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var success = _authService.ChangePassword(
                dialog.UserName.Trim(),
                dialog.OldPassword,
                dialog.NewPassword,
                out var message);

            MessageBox.Show(
                message,
                success ? "成功" : "失败",
                MessageBoxButtons.OK,
                success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private sealed class ChangePasswordForm : Form
        {
            private readonly TextBox _txtUser = new();
            private readonly TextBox _txtOld = new();
            private readonly TextBox _txtNew = new();
            private readonly TextBox _txtConfirm = new();

            public string UserName => _txtUser.Text;
            public string OldPassword => _txtOld.Text;
            public string NewPassword => _txtNew.Text;

            public ChangePasswordForm(string defaultUserName)
            {
                Text = "修改密码";
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                ClientSize = new Size(420, 280);
                AppTheme.StyleForm(this);

                var labels = new[]
                {
                    new Label { Text = "账号", Left = 28, Top = 30, Width = 80 },
                    new Label { Text = "旧密码", Left = 28, Top = 78, Width = 80 },
                    new Label { Text = "新密码", Left = 28, Top = 126, Width = 80 },
                    new Label { Text = "确认新密码", Left = 28, Top = 174, Width = 80 }
                };

                foreach (var label in labels)
                {
                    label.ForeColor = AppTheme.TextPrimary;
                    Controls.Add(label);
                }

                ConfigureInput(_txtUser, 110, 26, 270, defaultUserName);
                ConfigureInput(_txtOld, 110, 74, 270, password: true);
                ConfigureInput(_txtNew, 110, 122, 270, password: true);
                ConfigureInput(_txtConfirm, 110, 170, 270, password: true);

                var btnOk = new Button { Text = "确定", Left = 224, Top = 220, Width = 72, Height = 34, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "取消", Left = 308, Top = 220, Width = 72, Height = 34, DialogResult = DialogResult.Cancel };
                AppTheme.StyleButton(btnOk, primary: true);
                AppTheme.StyleButton(btnCancel);

                btnOk.Click += (s, e) =>
                {
                    if (_txtNew.Text != _txtConfirm.Text)
                    {
                        MessageBox.Show("两次输入的新密码不一致。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        DialogResult = DialogResult.None;
                    }
                };

                Controls.AddRange(new Control[] { _txtUser, _txtOld, _txtNew, _txtConfirm, btnOk, btnCancel });
                AcceptButton = btnOk;
                CancelButton = btnCancel;
            }

            private static void ConfigureInput(TextBox textBox, int left, int top, int width, string text = "", bool password = false)
            {
                textBox.Left = left;
                textBox.Top = top;
                textBox.Width = width;
                textBox.Text = text;
                textBox.PasswordChar = password ? '*' : '\0';
                AppTheme.StyleInput(textBox);
            }
        }
    }
}
