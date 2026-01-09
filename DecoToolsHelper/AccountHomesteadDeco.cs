namespace DecoToolsHelper
{
    /// Represents a single homestead decoration entry
    /// returned by the GW2 API endpoint:
    /// 
    ///   /v2/account/homestead/decorations
    /// 
    /// Each entry maps a decoration ID to the number
    /// of times it is unlocked on the account.
    public class AccountHomesteadDeco
    {
        /// Decoration ID.
        /// Matches the IDs used in decoration XML files.
        public int Id { get; set; }

        /// Number of times this decoration is unlocked.
        public int Count { get; set; }
    }
}
