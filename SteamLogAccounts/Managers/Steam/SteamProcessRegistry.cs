using System.Collections.Concurrent;

namespace ParadiseHelper.Managers.Steam
{
    /// <summary>
    /// A static, thread-safe registry for mapping Steam account logins to their running Steam client Process IDs (PIDs).
    /// This ensures safe access and modification of the process mapping from multiple concurrent threads.
    /// </summary>
    public static class SteamProcessRegistry
    {
        // Internal thread-safe dictionary: Login (string) -> Process ID (int).
        private static readonly ConcurrentDictionary<string, int> loginToPid = new ConcurrentDictionary<string, int>();

        /// <summary>
        /// Registers or updates the Process ID (PID) for a specific Steam account login.
        /// </summary>
        /// <param name="login">The unique login string of the Steam account.</param>
        /// <param name="processId">The operating system Process ID associated with the running Steam client.</param>
        public static void Register(string login, int processId)
        {
            loginToPid[login] = processId;
        }

        /// <summary>
        /// Safely attempts to retrieve the Process ID associated with a given account login.
        /// </summary>
        /// <param name="login">The unique login string of the Steam account.</param>
        /// <param name="pid">
        /// When this method returns, contains the PID associated with the specified login,
        /// if the login is found; otherwise, zero.
        /// </param>
        /// <returns><see langword="true"/> if the login was found in the registry; otherwise, <see langword="false"/>.</returns>
        public static bool TryGetProcessId(string login, out int pid)
        {
            return loginToPid.TryGetValue(login, out pid);
        }

        /// <summary>
        /// Removes the mapping for a specific account login from the registry.
        /// </summary>
        /// <param name="login">The unique login string of the account to unregister.</param>
        /// <returns><see langword="true"/> if the login was successfully found and removed; otherwise, <see langword="false"/>.</returns>
        public static bool Unregister(string login)
        {
            // Use TryRemove for thread-safe atomic removal.
            return loginToPid.TryRemove(login, out _);
        }

        /// <summary>
        /// Clears all account-to-PID mappings from the registry.
        /// </summary>
        public static void ClearAll()
        {
            loginToPid.Clear();
        }
    }
}