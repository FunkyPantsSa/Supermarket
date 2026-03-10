using Supermarket.Services;

namespace Supermarket
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var dbDir = Path.Combine(AppContext.BaseDirectory, "db");
            var dbPath = Path.Combine(dbDir, "supermarket.db");

            var authService = new AuthService(dbPath);
            using var loginForm = new LoginForm(authService);
            if (loginForm.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            Application.Run(new Form1(loginForm.CurrentUser!, authService));
        }
    }
}
