namespace DecoToolsHelper
{
    /// <summary>
    /// Represents a counted guild upgrade entry.
    /// 
    /// NOTE:
    /// The GW2 guild upgrades API returns only a list of unlocked
    /// upgrade IDs (List<int>), not counts or objects.
    /// 
    /// This model exists to:
    /// - Normalize guild decoration data
    /// - Mirror the homestead decoration structure
    /// - Make downstream XML/count logic consistent
    /// </summary>
    public class GuildUpgrade
    {
        /// <summary>
        /// Guild upgrade ID.
        /// Matches decoration IDs used in guild hall XML files.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Number of times this upgrade is counted.
        /// 
        /// Locked or unavailable upgrades correctly remain at 0.
        /// </summary>
        public int Count { get; set; }
    }
}
