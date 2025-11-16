using System.Collections.Generic;

namespace ParadiseHelper.Core
{
    /// <summary>
    /// Manages an ordered, non-duplicate queue of account logins. 
    /// It ensures that each account is added only once and maintains the order of insertion,
    /// making it suitable for sequencing application launches.
    /// </summary>
    public class AccountLaunchQueue
    {
        // Internal list to maintain the order in which logins were added.
        private readonly List<string> orderedLogins = new List<string>();

        /// <summary>
        /// Gets the total number of unique logins currently in the queue.
        /// </summary>
        public int Count => orderedLogins.Count;

        /// <summary>
        /// Adds a login to the end of the queue if it is not already present.
        /// </summary>
        /// <param name="login">The login string (unique identifier for an account).</param>
        public void Add(string login)
        {
            // Only add the login if it does not already exist in the list, ensuring uniqueness.
            if (!orderedLogins.Contains(login))
            {
                orderedLogins.Add(login);
            }
        }

        /// <summary>
        /// Removes a specific login from the queue.
        /// </summary>
        /// <param name="login">The login string to remove.</param>
        public void Remove(string login)
        {
            orderedLogins.Remove(login);
        }

        /// <summary>
        /// Clears all logins from the queue.
        /// </summary>
        public void Reset()
        {
            orderedLogins.Clear();
        }

        /// <summary>
        /// Returns the current queue of logins in their insertion order.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> of strings representing the logins. 
        /// The return type prevents external modification of the internal list.
        /// </returns>
        public IEnumerable<string> GetQueue()
        {
            return orderedLogins;
        }

        /// <summary>
        /// Checks if a specific login is currently in the queue.
        /// </summary>
        /// <param name="login">The login string to check.</param>
        /// <returns><see langword="true"/> if the login is in the queue; otherwise, <see langword="false"/>.</returns>
        public bool Contains(string login)
        {
            return orderedLogins.Contains(login);
        }
    }
}