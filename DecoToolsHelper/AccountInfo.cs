namespace DecoToolsHelper
{
    /// Partial representation of the GW2 /v2/account endpoint.
    /// Only the fields required by this helper are included.
    /// This avoids over-modeling unused API data.
    public class AccountInfo
    {
        /// List of guild IDs the account belongs to.
        public List<string> Guilds { get; set; } = new();
    }
}
