namespace DecoToolsHelper
{
    /// <summary>
    /// Lightweight representation of a Guild Wars 2 guild.
    /// 
    /// Used by the helper to expose:
    /// - Guild ID (for API calls)
    /// - Human-readable name
    /// - Guild tag
    /// 
    /// This intentionally avoids pulling full guild details.
    /// </summary>
    public class GuildSummary
    {
        /// <summary>
        /// Unique guild ID used by the GW2 API.
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// Full guild name.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Short guild tag displayed in-game.
        /// </summary>
        public string Tag { get; set; } = "";
    }
}
