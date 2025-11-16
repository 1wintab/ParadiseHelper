using System.Threading;
using System.Collections.Concurrent;

namespace ParadiseHelper.Managers.Launch
{
    /// <summary>
    /// Static manager to control the registration, signaling, and cleanup of
    /// <see cref="CancellationTokenSource"/> objects for account launch operations.
    /// </summary>
    public static class LaunchControl
    {
        // Stores CancellationTokenSource for each active account, keyed by login.
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> tokens =
            new ConcurrentDictionary<string, CancellationTokenSource>();

        /// <summary>
        /// Registers a new <see cref="CancellationTokenSource"/> for a login and returns its token.
        /// If a token already exists for this login, it is overwritten, but the previous token source is not disposed here.
        /// </summary>
        /// <param name="login">The unique login identifier.</param>
        /// <returns>The <see cref="CancellationToken"/> associated with the new source.</returns>
        public static CancellationToken Register(string login)
        {
            var cts = new CancellationTokenSource();
            // This operation will either add a new item or overwrite an existing one.
            tokens[login] = cts;
            return cts.Token;
        }

        /// <summary>
        /// Signals cancellation for the task associated with the specified login and cleans up the resource.
        /// </summary>
        /// <param name="login">The login identifier of the task to cancel.</param>
        public static void Cancel(string login)
        {
            if (tokens.TryRemove(login, out var cts))
            {
                // Signal cancellation
                cts.Cancel();
                // Dispose of the resource to prevent leaks
                cts.Dispose();
            }
        }

        /// <summary>
        /// Signals cancellation for all currently registered tasks and disposes of all associated resources.
        /// </summary>
        public static void CancelAll()
        {
            // Iterate over all entries to signal cancellation and dispose.
            foreach (var pair in tokens)
            {
                pair.Value.Cancel();
                pair.Value.Dispose();
            }

            // Clear the dictionary of all entries.
            tokens.Clear();
        }

        /// <summary>
        /// Removes the token source for a login without signalling cancellation (used for cleanup after success).
        /// The <see cref="CancellationTokenSource"/> is disposed to free up system resources.
        /// </summary>
        /// <param name="login">The login identifier to remove.</param>
        public static void Remove(string login)
        {
            if (tokens.TryRemove(login, out var cts))
            {
                // Dispose the resource even if cancellation was not signaled.
                cts.Dispose();
            }
        }
    }
}