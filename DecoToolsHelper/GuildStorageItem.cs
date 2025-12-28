namespace DecoToolsHelper
{
    /// <summary>
    /// Represents a counted guild storage entry.
    /// 
    /// NOTE:
    /// The GW2 guild storage API returns only a list of unlocked
    /// upgrade IDs (List<int>), not counts or objects.
    /// 
    /// This model exists to:
    /// - Normalize guild decoration data
    /// - Mirror the homestead decoration structure
    /// - Make downstream XML/count logic consistent
    /// </summary>
    public class GuildStorageItem
    {
        /// <summary>
        /// storage ID.
        /// Matches decoration IDs used in guild hall XML files.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Number of times this storage item is counted.
        /// 
        /// Locked or unavailable storage items correctly remain at 0.
        /// </summary>
        public int Count { get; set; }
    }
}
