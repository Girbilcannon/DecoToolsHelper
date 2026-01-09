using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DecoToolsHelper
{
    internal static class Program
    {
        private static NotifyIcon? _trayIcon;
        private static MainForm? _mainForm;
        private static LocalServer? _server;

        // 🔑 SINGLE shared config instance for the entire app
        private static HelperConfig? _config;

        [STAThread]
        static void Main()
        {
            // ==================================================
            // 🔐 REQUIRED for GW2 API (TLS 1.2)
            // ==================================================
            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.SecurityProtocolType.Tls12;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Load persisted configuration (API key, paths, etc.)
            _config = ConfigManager.Load();

            // Start the local helper HTTP server
            _server = new LocalServer(_config);
            _server.Start();

            // ==================================================
            // Phase 3: Ensure decoration database is up to date
            // (Silent, non-blocking, production-safe)
            // ==================================================
            _ = Task.Run(async () =>
            {
                try
                {
                    await DecoDBBuilder.EnsureUpToDateAsync();
                }
                catch
                {
                    // Intentionally ignored:
                    // - Never block startup
                    // - Existing DB (if any) remains usable
                }
            });

            // Load tray icon from embedded resource
            using var stream = typeof(Program).Assembly
                .GetManifestResourceStream("DecoToolsHelper.Assets.tray.png");

            var trayBitmap = new Bitmap(stream!);
            var trayIcon = Icon.FromHandle(trayBitmap.GetHicon());

            _trayIcon = new NotifyIcon
            {
                Icon = trayIcon,
                Text = "GW2 Deco Tools Helper",
                Visible = true,
                ContextMenuStrip = BuildTrayMenu()
            };

            _trayIcon.MouseClick += (_, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    ShowMainWindow();
            };

            // First-run experience
            if (string.IsNullOrWhiteSpace(_config.ApiKey))
                ShowMainWindow();

            Application.Run();
        }

        private static ContextMenuStrip BuildTrayMenu()
        {
            var menu = new ContextMenuStrip();

            menu.Items.Add("Open", null, (_, _) => ShowMainWindow());
            menu.Items.Add("Copy Local URL", null, (_, _) =>
                Clipboard.SetText("http://127.0.0.1:61337"));

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (_, _) => ExitApp());

            return menu;
        }

        private static void ShowMainWindow()
        {
            if (_config == null)
                return;

            if (_mainForm == null || _mainForm.IsDisposed)
                _mainForm = new MainForm(_config); // ✅ SAME INSTANCE

            if (!_mainForm.Visible)
                _mainForm.Show();

            _mainForm.BringToFront();
            _mainForm.Activate();
        }

        private static void ExitApp()
        {
            _trayIcon!.Visible = false;
            _trayIcon.Dispose();

            _server?.Stop();

            Environment.Exit(0);
        }
    }
}
