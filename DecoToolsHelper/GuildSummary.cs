namespace DecoToolsHelper
{
    /// Lightweight representation of a guild.
    /// 
    /// Used by the helper to expose:
    /// - Guild ID (for API calls)
    /// - Guild name
    /// - Guild tag
    /// 
    /// This intentionally avoids pulling full guild details.
    public class GuildSummary
    {
        /// Unique guild ID used by the API.
        public string Id { get; set; } = "";

        /// Guild name.
        public string Name { get; set; } = "";

        /// Guild tag
        public string Tag { get; set; } = "";
    }
}
