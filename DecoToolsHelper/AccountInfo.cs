namespace DecoToolsHelper
{
    /// <summary>
    /// Partial representation of the GW2 /v2/account endpoint.
    /// 
    /// Only the fields required by this helper are included.
    /// This avoids over-modeling unused API data.
    /// </summary>
    public class AccountInfo
    {
        /// <summary>
        /// List of guild IDs the account belongs to.
        /// 
        /// Used to:
        /// - Resolve guild names/tags
        /// - Fetch decoration unlock data per guild
        /// </summary>
        public List<string> Guilds { get; set; } = new();
    }
}
