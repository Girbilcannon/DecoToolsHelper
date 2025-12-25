using Newtonsoft.Json;
using System;
using System.IO;

namespace DecoToolsHelper
{
    /// <summary>
    /// Handles loading and saving user-specific configuration data.
    /// 
    /// This includes:
    /// - GW2 API key
    /// - Default save paths (Homestead / Guild Hall)
    /// 
    /// The config file is stored alongside the executable as:
    ///   config.json
    /// 
    /// This file is intentionally kept outside the compiled binary so:
    /// - Users can reset it manually
    /// - Personal data is never embedded in the EXE
    /// </summary>
    public static class ConfigManager
    {
        // Absolute path to config.json (same folder as the executable)
        private static readonly string ConfigPath =
            Path.Combine(AppContext.BaseDirectory, "config.json");

        /// <summary>
        /// Loads configuration from disk.
        /// 
        /// Failure-safe behavior:
        /// - Missing file → return default config
        /// - Corrupt JSON → return default config
        /// 
        /// This ensures the helper never fails to start due to config issues.
        /// </summary>
        public static HelperConfig Load()
        {
            if (!File.Exists(ConfigPath))
                return new HelperConfig();

            try
            {
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

        /// <summary>
        /// Saves configuration to disk.
        /// 
        /// Uses indented JSON for readability and easy manual editing.
        /// Overwrites the existing file if present.
        /// </summary>
        public static void Save(HelperConfig config)
        {
            var json = JsonConvert.SerializeObject(
                config,
                Formatting.Indented
            );

            File.WriteAllText(ConfigPath, json);
        }
    }
}
