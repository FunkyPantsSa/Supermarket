using Supermarket.Services;

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

        public string LoginUserName { get; private set; } = "";

        public LoginForm(AuthService authService)
        {
            _authService = authService;
            InitializeUi();
        }

        private void InitializeUi()
        {
            Text = "系统登录";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, 230);
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);

            var lblTitle = new Label
            {
                Text = "超市收银系统登录",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
                Left = 20,
                Top = 16,
                Width = 380,
                Height = 28
            };

            var lblUser = new Label { Text = "用户名", Left = 48, Top = 72, Width = 70, Height = 24 };
            _txtUser.Left = 118;
            _txtUser.Top = 70;
            _txtUser.Width = 230;
            _txtUser.Text = "admin";

            var lblPassword = new Label { Text = "密码", Left = 48, Top = 110, Width = 70, Height = 24 };
            _txtPassword.Left = 118;
            _txtPassword.Top = 108;
            _txtPassword.Width = 230;
            _txtPassword.PasswordChar = '*';

            _btnLogin.Text = "登录";
            _btnLogin.Left = 118;
            _btnLogin.Top = 160;
            _btnLogin.Width = 88;
            _btnLogin.Height = 34;
            _btnLogin.Click += BtnLogin_Click;

            _btnChangePassword.Text = "修改密码";
            _btnChangePassword.Left = 214;
            _btnChangePassword.Top = 160;
            _btnChangePassword.Width = 88;
            _btnChangePassword.Height = 34;
            _btnChangePassword.Click += BtnChangePassword_Click;

            _btnExit.Text = "退出";
            _btnExit.Left = 310;
            _btnExit.Top = 160;
            _btnExit.Width = 88;
            _btnExit.Height = 34;
            _btnExit.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                lblTitle, lblUser, _txtUser, lblPassword, _txtPassword, _btnLogin, _btnChangePassword, _btnExit
            });

            AcceptButton = _btnLogin;
            CancelButton = _btnExit;
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            var user = _txtUser.Text.Trim();
            var password = _txtPassword.Text;
            if (_authService.ValidateLogin(user, password))
            {
                LoginUserName = user;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            MessageBox.Show("用户名或密码错误。", "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void BtnChangePassword_Click(object? sender, EventArgs e)
        {
            using var dialog = new ChangePasswordForm();
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

            public ChangePasswordForm()
            {
                Text = "修改密码";
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                ClientSize = new Size(390, 240);
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);

                var lblUser = new Label { Text = "用户名", Left = 28, Top = 28, Width = 80 };
                _txtUser.Left = 110;
                _txtUser.Top = 24;
                _txtUser.Width = 240;
                _txtUser.Text = "admin";

                var lblOld = new Label { Text = "旧密码", Left = 28, Top = 66, Width = 80 };
                _txtOld.Left = 110;
                _txtOld.Top = 62;
                _txtOld.Width = 240;
                _txtOld.PasswordChar = '*';

                var lblNew = new Label { Text = "新密码", Left = 28, Top = 104, Width = 80 };
                _txtNew.Left = 110;
                _txtNew.Top = 100;
                _txtNew.Width = 240;
                _txtNew.PasswordChar = '*';

                var lblConfirm = new Label { Text = "确认新密码", Left = 28, Top = 142, Width = 80 };
                _txtConfirm.Left = 110;
                _txtConfirm.Top = 138;
                _txtConfirm.Width = 240;
                _txtConfirm.PasswordChar = '*';

                var btnOk = new Button { Text = "确定", Left = 194, Top = 184, Width = 74, Height = 32, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "取消", Left = 276, Top = 184, Width = 74, Height = 32, DialogResult = DialogResult.Cancel };

                btnOk.Click += (s, e) =>
                {
                    if (_txtNew.Text != _txtConfirm.Text)
                    {
                        MessageBox.Show("两次输入的新密码不一致。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        DialogResult = DialogResult.None;
                        return;
                    }
                };

                Controls.AddRange(new Control[]
                {
                    lblUser, _txtUser, lblOld, _txtOld, lblNew, _txtNew, lblConfirm, _txtConfirm, btnOk, btnCancel
                });

                AcceptButton = btnOk;
                CancelButton = btnCancel;
            }
        }
    }
}
