using System.Windows.Forms;

namespace Supermarket.UI
{
    /// <summary>
    /// Toast提示工具类
    /// </summary>
    public static class ToastHelper
    {
        public static void ShowToast(Control parent, string message, int duration = 2000)
        {
            var toast = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                TopMost = true,
                BackColor = Color.FromArgb(60, 60, 60),
                Opacity = 0.9,
                Size = new Size(300, 60)
            };

            var label = new Label
            {
                Text = message,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 10F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };

            toast.Controls.Add(label);
            
            // 计算位置（在父控件中心）
            if (parent != null && parent.IsHandleCreated)
            {
                var parentRect = parent.RectangleToScreen(parent.ClientRectangle);
                toast.Location = new Point(
                    parentRect.Left + (parentRect.Width - toast.Width) / 2,
                    parentRect.Top + (parentRect.Height - toast.Height) / 2
                );
            }
            else
            {
                toast.StartPosition = FormStartPosition.CenterScreen;
            }

            toast.Show();
            
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = duration;
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                toast.Close();
                toast.Dispose();
            };
            timer.Start();
        }
    }
}
