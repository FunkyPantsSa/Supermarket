namespace Supermarket.UI
{
    internal static class AppTheme
    {
        public static readonly Color Surface = Color.FromArgb(244, 247, 251);
        public static readonly Color Card = Color.White;
        public static readonly Color Accent = Color.FromArgb(26, 99, 180);
        public static readonly Color AccentSoft = Color.FromArgb(227, 238, 250);
        public static readonly Color AccentDark = Color.FromArgb(15, 58, 111);
        public static readonly Color Border = Color.FromArgb(215, 223, 234);
        public static readonly Color TextPrimary = Color.FromArgb(32, 43, 56);
        public static readonly Color TextSecondary = Color.FromArgb(95, 107, 122);
        public static readonly Color Success = Color.FromArgb(28, 131, 89);
        public static readonly Color Danger = Color.FromArgb(196, 62, 62);
        public static readonly Color SelectionStrong = Color.FromArgb(255, 179, 0);

        public static void StyleForm(Form form)
        {
            form.BackColor = Surface;
            form.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
        }

        public static void StyleButton(Button button, bool primary = false, bool danger = false)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Cursor = Cursors.Hand;
            button.ForeColor = Color.White;
            button.BackColor = danger ? Danger : primary ? Accent : Color.FromArgb(79, 101, 128);
            button.FlatAppearance.MouseOverBackColor = danger
                ? Color.FromArgb(170, 48, 48)
                : primary
                    ? Color.FromArgb(19, 84, 157)
                    : Color.FromArgb(66, 87, 112);
        }

        public static void StyleInput(Control control)
        {
            control.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            control.BackColor = Color.White;
            control.ForeColor = TextPrimary;
        }

        public static void StyleCard(Control control, Padding? padding = null)
        {
            control.BackColor = Card;
            control.Padding = padding ?? control.Padding;
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = Card;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = Border;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.RowTemplate.Height = 36;
            grid.ColumnHeadersHeight = 38;
            grid.ColumnHeadersDefaultCellStyle.BackColor = AccentDark;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            grid.DefaultCellStyle.BackColor = Card;
            grid.DefaultCellStyle.ForeColor = TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = SelectionStrong;
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(28, 28, 28);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 253);
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = SelectionStrong;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(28, 28, 28);
            grid.RowsDefaultCellStyle.SelectionBackColor = SelectionStrong;
            grid.RowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(28, 28, 28);
            grid.RowHeadersVisible = false;
        }

        public static Label CreateSectionTitle(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
                ForeColor = TextPrimary
            };
        }
    }
}
