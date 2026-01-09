namespace DecoToolsHelper
{
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
    public class GuildStorageItem
    {
        /// storage ID.
        /// Matches decoration IDs used in guild hall XML files.
        public int Id { get; set; }

        /// Number of times this storage item is counted.
        /// Locked or unavailable storage items correctly remain at 0.
        public int Count { get; set; }
    }
}
