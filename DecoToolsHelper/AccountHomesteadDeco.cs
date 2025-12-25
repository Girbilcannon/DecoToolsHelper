namespace DecoToolsHelper
{
    /// <summary>
    /// Represents a single homestead decoration entry
    /// returned by the GW2 API endpoint:
    /// 
    ///   /v2/account/homestead/decorations
    /// 
    /// Each entry maps a decoration ID to the number
    /// of times it is unlocked on the account.
    /// </summary>
    public class AccountHomesteadDeco
    {
        /// <summary>
        /// Decoration ID.
        /// Matches the IDs used in decoration XML files.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Number of times this decoration is unlocked.
        /// </summary>
        public int Count { get; set; }
    }
}
