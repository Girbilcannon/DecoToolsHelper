using Newtonsoft.Json;
using System;
using System.IO;

namespace DecoToolsHelper
{
    /// Handles loading and saving user-specific configuration data.
    /// - GW2 API key
    /// - Default save paths (Homestead / Guild Hall)
    /// 
    /// IMPORTANT STORAGE NOTE:
    /// -----------------------
    /// The config file is stored in the user's AppData directory:
    ///   %APPDATA%\DecoToolsHelper\config.json
    /// 
    /// This is REQUIRED for:
    /// - Single-file published executables
    /// - Self-contained builds
    /// - Portable EXEs
    /// 
    /// This design ensures:
    /// - API keys persist across launches
    /// - Config is not embedded in the EXE
    /// - Users can manually reset config
    /// - No admin permissions are required
    public static class ConfigManager
    {
        // Base directory in AppData for this helper
        private static readonly string ConfigDirectory =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DecoToolsHelper"
            );

        // Absolute path to config.json
        private static readonly string ConfigPath =
            Path.Combine(ConfigDirectory, "config.json");

        /// Loads configuration from disk.
        /// 
        /// Failure-safe behavior:
        /// - Missing file → return default config
        /// - Corrupt JSON → return default config
        /// 
        /// This ensures the helper never fails to start due to config issues.
        public static HelperConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                    return new HelperConfig();

                var text = File.ReadAllText(ConfigPath);

                // Deserialize into HelperConfig.
                // If deserialization fails or returns null, fall back safely.
                return JsonConvert.DeserializeObject<HelperConfig>(text)
                       ?? new HelperConfig();
            }
            catch
            {
                // Any exception here results in a clean default config.
                // Intentionally silent to avoid blocking startup.
                return new HelperConfig();
            }
        }

        /// Saves configuration to disk.
        /// 
        /// Uses indented JSON for readability and easy manual editing.
        /// Overwrites the existing file if present.
        /// 
        /// The directory is created automatically if missing.
        public static void Save(HelperConfig config)
        {
            // Ensure AppData directory exists
            Directory.CreateDirectory(ConfigDirectory);

            var json = JsonConvert.SerializeObject(
                config,
                Formatting.Indented
            );

            File.WriteAllText(ConfigPath, json);
        }
    }
}
