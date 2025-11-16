using System.Collections.Generic;

namespace ParadiseHelper.Managers.AccountQueue
{
    /// <summary>
    /// A static utility class for tracking which accounts are currently active (running) within the application.
    /// Uses a thread-safe approach to manage the running status of accounts efficiently.
    /// </summary>
    public static class AccountStatusTracker
    {
        // Internal collection (Set) to store the logins of accounts that are currently active.
        private static readonly HashSet<string> runningAccounts = new HashSet<string>();

        // Private lock object to serialize access to the runningAccounts HashSet, ensuring thread safety.
        private static readonly object _lock = new object();

        /// <summary>
        /// Checks if a specific account is currently marked as running.
        /// </summary>
        /// <param name="login">The login string (unique account identifier).</param>
        /// <returns>True if the account is in the running set, otherwise false.</returns>
        public static bool IsRunning(string login)
        {
            lock (_lock)
            {
                return runningAccounts.Contains(login);
            }
        }

        /// <summary>
        /// Marks an account as currently running (adds it to the set).
        /// </summary>
        /// <param name="login">The login string to mark.</param>
        public static void MarkAsRunning(string login)
        {
            lock (_lock)
            {
                runningAccounts.Add(login);
            }
        }

        /// <summary>
        /// Marks an account as finished (removes it from the set).
        /// </summary>
        /// <param name="login">The login string to unmark.</param>
        public static void MarkAsFinished(string login)
        {
            lock (_lock)
            {
                runningAccounts.Remove(login);
            }
        }

        /// <summary>
        /// Clears all accounts from the running status tracker.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                runningAccounts.Clear();
            }
        }
    }
}