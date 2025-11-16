using System.Collections.Generic;

namespace ParadiseHelper.Managers.AccountQueue
{
    /// <summary>
    /// A static registry class to map user logins (accounts) to their corresponding CS2 process IDs (PIDs).
    /// This is typically used for managing and monitoring multiple instances of the game.
    /// </summary>
    public static class CS2ProcessRegistry
    {
        // Internal dictionary to store the mapping: Account Login (string) -> Process ID (int).
        private static readonly Dictionary<string, int> processMap = new Dictionary<string, int>();

        /// <summary>
        /// Registers or updates the Process ID (PID) for a specific account login.
        /// If the login already exists, its PID is overwritten.
        /// </summary>
        /// <param name="login">The unique login string of the account.</param>
        /// <param name="pid">The operating system Process ID associated with the running game instance.</param>
        public static void Register(string login, int pid)
        {
            processMap[login] = pid;
        }

        /// <summary>
        /// Attempts to retrieve the Process ID for a given account login.
        /// </summary>
        /// <param name="login">The unique login string of the account.</param>
        /// <param name="pid">
        /// When this method returns, contains the PID associated with the specified login,
        /// if the login is found; otherwise, zero. The retrieval fails silently if not found.
        /// </param>
        /// <returns><see langword="true"/> if the login was found in the registry; otherwise, <see langword="false"/>.</returns>
        public static bool TryGetProcessId(string login, out int pid)
        {
            return processMap.TryGetValue(login, out pid);
        }

        /// <summary>
        /// Removes the mapping for a specific account login from the registry.
        /// </summary>
        /// <param name="login">The unique login string of the account to unregister.</param>
        /// <returns><see langword="true"/> if the login was successfully found and removed; otherwise, <see langword="false"/>.</returns>
        public static bool Unregister(string login)
        {
            return processMap.Remove(login);
        }

        /// <summary>
        /// Clears all account-to-PID mappings from the registry.
        /// </summary>
        public static void ClearAll()
        {
            processMap.Clear();
        }
    }
}