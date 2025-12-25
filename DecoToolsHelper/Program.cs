using System;
using System.Drawing;
using System.Windows.Forms;

namespace DecoToolsHelper
{
    /// <summary>
    /// Application entry point.
    /// 
    /// Responsible for:
    /// - Starting the local HTTP server
    /// - Creating the system tray icon
    /// - Managing application lifetime (no visible console)
    /// - Showing the UI only when needed
    /// </summary>
    internal static class Program
    {
        // System tray icon (always present while app is running)
        private static NotifyIcon? _trayIcon;

        // Main configuration / settings window (created on demand)
        private static MainForm? _mainForm;

        // Local HTTP server (GW2 API + Mumble proxy)
        private static LocalServer? _server;

        /// <summary>
        /// Application entry point.
        /// Runs without a main window and lives in the system tray.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Required WinForms initialization
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Load persisted configuration (API key, paths, etc.)
            var config = ConfigManager.Load();

            // Start the local helper HTTP server immediately
            _server = new LocalServer(config);
            _server.Start();

            // Load tray icon from PNG (converted at runtime)
            using var stream = typeof(Program).Assembly
    .GetManifestResourceStream("DecoToolsHelper.Assets.tray.png");

            var trayBitmap = new Bitmap(stream!);
            var trayIcon = Icon.FromHandle(trayBitmap.GetHicon());


            // Create system tray icon
            _trayIcon = new NotifyIcon
            {
                Icon = trayIcon,
                Text = "GW2 Deco Tools Helper",
                Visible = true,
                ContextMenuStrip = BuildTrayMenu()
            };

            // Left-click on tray icon opens the main window
            _trayIcon.MouseClick += (_, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    ShowMainWindow();
            };

            // First-run experience:
            // If no API key exists yet, show the UI immediately.
            // Otherwise start minimized to tray.
            if (string.IsNullOrWhiteSpace(config.ApiKey))
                ShowMainWindow();

            // Run message loop with no main form
            Application.Run();
        }

        /// <summary>
        /// Builds the right-click context menu for the tray icon.
        /// </summary>
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

        /// <summary>
        /// Shows the main configuration window.
        /// Recreates it if it was previously disposed.
        /// </summary>
        private static void ShowMainWindow()
        {
            if (_mainForm == null || _mainForm.IsDisposed)
                _mainForm = new MainForm();

            if (!_mainForm.Visible)
                _mainForm.Show();

            _mainForm.BringToFront();
            _mainForm.Activate();
        }

        /// <summary>
        /// Fully terminates the application.
        /// Ensures no background processes are left running.
        /// </summary>
        private static void ExitApp()
        {
            // Remove tray icon cleanly
            _trayIcon!.Visible = false;
            _trayIcon.Dispose();

            // Stop HTTP listener and free port
            _server?.Stop();

            // Force process exit to prevent zombie background instances
            Environment.Exit(0);
        }
    }
}
