namespace ParadiseHelper.Core.Enums
{
    /// <summary>
    /// Defines the possible operational states for an account within the system lifecycle.
    /// </summary>
    public enum AccountStatus
    {
        /// <summary>
        /// Account is currently idle and not part of any processing queue.
        /// </summary>
        Idle,

        /// <summary>
        /// Account has been added to the queue and is waiting for processing.
        /// </summary>
        Queued,

        /// <summary>
        /// Account is currently active and being processed.
        /// </summary>
        Running,

        /// <summary>
        /// Account has successfully completed its intended operation.
        /// </summary>
        Finished,

        /// <summary>
        /// Account processing was forcefully stopped or encountered a critical, non-recoverable error.
        /// </summary>
        Terminated
    }
}