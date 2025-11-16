using System.Threading;
using System.Threading.Tasks;

namespace ParadiseHelper.AI.Core.DetectionLoop
{
    /// <summary>
    /// Provides a base abstract class for all continuous background processing and detection loops.
    /// This class manages the execution on a separate thread (<see cref="Task"/>) and handles cancellation logic 
    /// using a <see cref="CancellationToken"/>.
    /// </summary>
    public abstract class DetectionLoopBase
    {
        // The background task instance that runs the continuous loop logic.
        private Task _task;

        /// <summary>
        /// The core method that must be implemented by derived classes to contain the detection and processing logic.
        /// This method is intended to run continuously until the cancellation token is signaled.
        /// </summary>
        /// <param name="token">A cancellation token that signals when the background operation should be stopped.</param>
        protected abstract void Loop(CancellationToken token);

        /// <summary>
        /// Starts the continuous detection loop on a new background thread using <see cref="Task.Run"/>.
        /// </summary>
        /// <param name="parentToken">The cancellation token used to stop the loop.</param>
        public void Start(CancellationToken parentToken)
        {
            // The Task is started with the parentToken to enable cooperative cancellation.
            _task = Task.Run(() => Loop(parentToken), parentToken);
        }

        /// <summary>
        /// Retrieves the <see cref="Task"/> object representing the currently running loop.
        /// </summary>
        /// <returns>The running Task.</returns>
        public Task GetStopTask()
        {
            return _task;
        }

        /// <summary>
        /// Retrieves the running <see cref="Task"/> object. Can be awaited to ensure the loop completes gracefully.
        /// </summary>
        /// <returns>The running Task, or <see cref="Task.CompletedTask"/> if the loop was never started (i.e., _task is null).</returns>
        public Task Stop()
        {
            return _task ?? Task.CompletedTask;
        }
    }
}