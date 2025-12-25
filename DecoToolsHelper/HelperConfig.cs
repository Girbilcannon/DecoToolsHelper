namespace DecoToolsHelper
{
    /// <summary>
    /// Represents all user-configurable settings for the helper.
    /// 
    /// This object is serialized directly to config.json and
    /// loaded on application startup.
    /// 
    /// All fields are optional and failure-safe:
    /// - null values indicate "use defaults"
    /// </summary>
    public class HelperConfig
    {
        /// <summary>
        /// Guild Wars 2 API key provided by the user.
        /// 
        /// Required for:
        /// - Account data
        /// - Guild list
        /// - Decoration unlock counts
        /// 
        /// Not required for MumbleLink functionality.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Default save path for Homestead decoration XML files.
        /// 
        /// If null, the application should fall back to:
        /// Documents\Guild Wars 2\Homesteads
        /// </summary>
        public string? HomesteadPath { get; set; }

        /// <summary>
        /// Default save path for Guild Hall decoration XML files.
        /// 
        /// If null, the application should fall back to:
        /// Documents\Guild Wars 2\GuildHalls
        /// </summary>
        public string? GuildHallPath { get; set; }
    }
}
