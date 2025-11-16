namespace ParadiseHelper.SteamLogAccounts.SteamEnv
{
    /// <summary>
    /// Provides utility methods for converting between the two main Steam ID formats:
    /// the unique 64-bit Steam ID (<c>SteamID64</c>) and the smaller 32-bit Account ID (<c>AccountID</c>).
    /// </summary>
    public static class SteamIdHelper
    {
        // The fixed offset value (76561197960265728) used by Steam to map a 32-bit AccountID
        // into the 64-bit SteamID space. This constant is essential for conversion.
        private const long Offset = 76561197960265728;

        /// <summary>
        /// Converts a full SteamID64 into its corresponding 32-bit Account ID (used in configuration files).
        /// </summary>
        /// <param name="steamId64">The 64-bit Steam ID to convert.</param>
        /// <returns>The corresponding 32-bit Account ID.</returns>
        public static uint ToAccountId(ulong steamId64) =>
            (uint)(steamId64 - Offset);

        /// <summary>
        /// Converts a 32-bit Account ID back into the full 64-bit SteamID64.
        /// </summary>
        /// <param name="accountId">The 32-bit Account ID to convert.</param>
        /// <returns>The corresponding 64-bit Steam ID.</returns>
        public static ulong ToSteamId64(uint accountId) =>
            (ulong)Offset + accountId;
    }
}