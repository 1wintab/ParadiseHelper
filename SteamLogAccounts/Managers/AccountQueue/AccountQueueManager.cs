using System.Linq;
using System.Collections.Generic;
using ParadiseHelper.Core;

namespace ParadiseHelper.Managers
{
    /// <summary>
    /// A static manager class that provides a single, easy-to-access interface (Singleton facade)
    /// for managing the application's account launch queue.
    /// This queue ensures that account logins are processed in an ordered, non-duplicated manner.
    /// </summary>
    public static class AccountQueueManager
    {
        // Creates a single, read-only instance of the AccountLaunchQueue.
        private static readonly AccountLaunchQueue instance = new AccountLaunchQueue();

        /// <summary>
        /// Gets the current number of accounts waiting in the queue.
        /// </summary>
        public static int Count => instance.GetQueue().Count();

        /// <summary>
        /// Adds a login identifier to the queue.
        /// The underlying queue is responsible for preventing duplicate entries.
        /// </summary>
        /// <param name="login">The unique login identifier (e.g., username or ID) to add.</param>
        public static void Add(string login) => instance.Add(login);

        /// <summary>
        /// Removes a specific login identifier from the queue.
        /// </summary>
        /// <param name="login">The login identifier to remove.</param>
        public static void Remove(string login) => instance.Remove(login);

        /// <summary>
        /// Clears all entries and resets the account launch queue.
        /// </summary>
        public static void Clear() => instance.Reset();

        /// <summary>
        /// Returns the ordered list of login identifiers currently in the queue.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of login identifiers.</returns>
        public static IEnumerable<string> GetQueue() => instance.GetQueue();
    }
}