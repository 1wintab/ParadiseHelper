using System;
using System.Threading.Tasks;

namespace Common.Helpers.Tools.MemoryTools
{
    /// <summary>
    /// Provides utility methods for memory management, primarily focused on forcing
    /// garbage collection to ensure the timely release of specific objects (e.g., UI forms).
    /// </summary>
    /// <remarks>
    /// Note: Explicitly calling <c>GC.Collect()</c> is generally discouraged in favor of the CLR's
    /// optimized automatic collection. This class should be used sparingly and only when necessary
    /// to resolve specific memory pressure scenarios related to object finalization.
    /// </remarks>
    public static class MemoryHelper
    {
        /// <summary>
        /// Attempts to force the garbage collection of a specific target object asynchronously.
        /// </summary>
        /// <remarks>
        /// This method uses a <see cref="WeakReference"/> to monitor the target object's lifetime.
        /// It runs a background task that repeatedly delays, triggers garbage collection (GC), and
        /// waits for finalizers until the target object is no longer reachable or the maximum
        /// number of attempts is reached. This is typically used for UI elements (like Forms)
        /// that might hold onto unmanaged resources.
        /// </remarks>
        /// <param name="target">The object instance (e.g., a Form) to be monitored and released.</param>
        /// <param name="maxAttempts">The maximum number of garbage collection cycles to force.</param>
        /// <param name="delayMs">The delay in milliseconds between each GC attempt.</param>
        public static void EnsureFormRelease(object target, int maxAttempts = 10, int delayMs = 300)
        {
            if (target == null) return;

            // Creates a weak reference that doesn't prevent garbage collection by the GC.
            WeakReference wr = new WeakReference(target);

            // Start the cleanup process asynchronously to avoid blocking the calling thread.
            Task.Run(async () =>
            {
                for (int i = 0; i < maxAttempts; i++)
                {
                    await Task.Delay(delayMs);

                    // Force the immediate garbage collection across all generations.
                    GC.Collect();

                    // Wait for all finalizer threads to complete their work.
                    GC.WaitForPendingFinalizers();

                    // A second GC.Collect() often ensures the finalizers' memory is also collected.
                    GC.Collect();

                    // Check the weak reference: if the target is no longer in memory, the job is done.
                    if (!wr.IsAlive) return;
                }
            });
        }
    }
}