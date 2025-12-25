namespace DecoToolsHelper
{
    /// <summary>
    /// Request payload used for targeted guild decoration queries.
    /// 
    /// This model is sent to:
    ///   POST /decos/guild/{guildId}
    /// 
    /// It intentionally requires the caller to specify
    /// which upgrade IDs they want to check.
    /// </summary>
    public class GuildIdRequest
    {
        /// <summary>
        /// List of guild upgrade / decoration IDs to query.
        /// 
        /// This prevents:
        /// - Bulk scanning of all guild upgrades
        /// - Excessive API usage
        /// - Accidental locked upgrade disclosure
        /// </summary>
        public List<int> Ids { get; set; } = new();
    }
}
