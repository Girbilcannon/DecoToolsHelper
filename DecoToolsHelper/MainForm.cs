using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

using Label = System.Windows.Forms.Label;

namespace DecoToolsHelper
{
    /// Main configuration window for the Deco Tools Helper.
    /// 
    /// Responsibilities:
    /// - Collect and store the user's GW2 API key
    /// - Allow configuration of default XML save paths
    /// - Provide visual confirmation that MumbleLink is working
    /// - Educate users with minimal troubleshooting tips
    /// 
    /// This window is optional and may be hidden while the helper
    /// continues running in the system tray.
    public class MainForm : Form
    {
        // SHARED configuration instance (owned by Program.cs)
        private readonly HelperConfig _config;

        // API key UI
        private TextBox _txtApiKey = null!;
        private Button _btnSaveApiKey = null!;
        private Button _btnRemoveApiKey = null!;
        private Label _lblApiStatus = null!;

        // Default save paths
        private TextBox _txtHomesteadPath = null!;
        private TextBox _txtGuildHallPath = null!;

        /// Creates the configuration window using the shared HelperConfig.
        public MainForm(HelperConfig config)
        {
            _config = config;

            Text = "GW2 Deco Tools Helper";
            Width = 560;
            Height = 560;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            BuildUI();
            RefreshState();
        }

        /// Constructs the entire UI layout programmatically.
        private void BuildUI()
        {
            int y = 15;

            // ================= API KEY =================
            Controls.Add(Header("Guild Wars 2 API Key", ref y));

            _txtApiKey = new TextBox
            {
                Left = 20,
                Top = y,
                Width = 380,
                UseSystemPasswordChar = true
            };
            Controls.Add(_txtApiKey);

            _btnSaveApiKey = new Button
            {
                Text = "Save",
                Left = 420,
                Top = y - 1,
                Width = 100
            };
            _btnSaveApiKey.Click += (_, _) => SaveApiKey();
            Controls.Add(_btnSaveApiKey);

            y += 35;

            _btnRemoveApiKey = new Button
            {
                Text = "Remove API Key",
                Left = 20,
                Top = y,
                Width = 160
            };
            _btnRemoveApiKey.Click += (_, _) => RemoveApiKey();
            Controls.Add(_btnRemoveApiKey);

            y += 30;

            _lblApiStatus = new Label
            {
                Left = 20,
                Top = y,
                AutoSize = true
            };
            Controls.Add(_lblApiStatus);

            y += 30;

            // ================= SAVE PATHS =================
            Controls.Add(Header("Default Save Locations", ref y));

            _txtHomesteadPath = PathRow(
                "Homestead XML folder:",
                GetDefaultHomesteadPath(),
                ref y
            );

            _txtGuildHallPath = PathRow(
                "Guild Hall XML folder:",
                GetDefaultGuildHallPath(),
                ref y
            );

            // ================= MUMBLE LINK =================
            y += 10;

            var mumbleLink = new LinkLabel
            {
                Text = "View live Helper status (http://localhost:61337/status)",
                Left = 20,
                Top = y,
                AutoSize = true
            };

            mumbleLink.LinkClicked += (_, _) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://localhost:61337/status",
                    UseShellExecute = true
                });
            };

            Controls.Add(mumbleLink);

            y += 30;

            // ================= TIPS =================
            Controls.Add(new Label
            {
                Text = "TIPS:",
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Left = 20,
                Top = y,
                AutoSize = true
            });

            y += 22;

            AddTip("• Make sure the game is running with a character on a live map.", ref y);
            AddTip("• If Mumble shows \"false\", refresh the page a few times once in a game map.", ref y);
            AddTip("• Safe to close this window. The tool continues running in the system tray.", ref y);
            AddTip("• If you have trouble with the API key or save paths, fully exit the tool and delete the config file.", ref y);
        }

        /// Updates UI controls based on current configuration state.
        private void RefreshState()
        {
            bool hasKey = !string.IsNullOrWhiteSpace(_config.ApiKey);

            _txtApiKey.Enabled = !hasKey;
            _btnSaveApiKey.Enabled = !hasKey;
            _btnRemoveApiKey.Enabled = hasKey;

            _lblApiStatus.Text = hasKey
                ? "API key stored. Account and guild data enabled."
                : "No API key stored. Mumble still works.";

            _txtHomesteadPath.Text =
                _config.HomesteadPath ?? GetDefaultHomesteadPath();

            _txtGuildHallPath.Text =
                _config.GuildHallPath ?? GetDefaultGuildHallPath();
        }

        // ================= API =================

        private void SaveApiKey()
        {
            var key = _txtApiKey.Text.Trim();
            if (string.IsNullOrEmpty(key))
                return;

            _config.ApiKey = key;
            ConfigManager.Save(_config);
            RefreshState();
        }

        private void RemoveApiKey()
        {
            _config.ApiKey = null;
            ConfigManager.Save(_config);
            _txtApiKey.Text = "";
            RefreshState();
        }

        // ================= PATHS =================

        private TextBox PathRow(string label, string defaultValue, ref int y)
        {
            Controls.Add(new Label
            {
                Text = label,
                Left = 20,
                Top = y + 4,
                AutoSize = true
            });

            var box = new TextBox
            {
                Left = 20,
                Top = y + 24,
                Width = 420,
                Text = defaultValue
            };
            box.TextChanged += (_, _) => SavePaths();
            Controls.Add(box);

            var btn = new Button
            {
                Text = "Browse…",
                Left = 450,
                Top = y + 22,
                Width = 80
            };
            btn.Click += (_, _) => BrowseFolder(box);
            Controls.Add(btn);

            y += 60;
            return box;
        }

        private void BrowseFolder(TextBox target)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                target.Text = dlg.SelectedPath;
                SavePaths();
            }
        }

        private void SavePaths()
        {
            _config.HomesteadPath = _txtHomesteadPath.Text;
            _config.GuildHallPath = _txtGuildHallPath.Text;
            ConfigManager.Save(_config);
        }

        // ================= HELPERS =================

        private static Label Header(string text, ref int y)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Left = 15,
                Top = y,
                AutoSize = true
            };
            y += 28;
            return lbl;
        }

        private void AddTip(string text, ref int y)
        {
            var lbl = new Label
            {
                Text = text,
                Left = 30,
                Top = y,
                AutoSize = true,
                MaximumSize = new Size(500, 0)
            };

            Controls.Add(lbl);
            y += lbl.PreferredHeight + 6;
        }

        private static string GetDefaultHomesteadPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Guild Wars 2",
                "Homesteads"
            );
        }

        private static string GetDefaultGuildHallPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Guild Wars 2",
                "GuildHalls"
            );
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
