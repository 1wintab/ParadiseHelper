using System;
using System.Threading;
using System.Collections.Generic;
using OpenCvSharp;

namespace ParadiseHelper.AI.Video.GameVideoSource
{
    /// <summary>
    /// Thread-safe class responsible for distributing the latest video frame from a single producer 
    /// to multiple consumers using signaling (<see cref="AutoResetEvent"/>).
    /// </summary>
    public class FrameDistributor : IDisposable
    {
        // Lock object for thread synchronization (accessing _latestFrame).
        private readonly object _lock = new object();

        // The most recently received video frame (OpenCvSharp Mat).
        private Mat _latestFrame;

        // Signals for each waiting consumer thread.
        private readonly List<AutoResetEvent> _consumers = new List<AutoResetEvent>();

        // Flag to track resource disposal.
        private bool _disposed = false;                     

        /// <summary>
        /// Registers a new consumer and provides a unique signal object for it to wait on.
        /// </summary>
        /// <returns>A new <see cref="AutoResetEvent"/> unique to this consumer thread.</returns>
        public AutoResetEvent RegisterConsumer()
        {
            // Initialize signal to non-signaled state.
            var newConsumerSignal = new AutoResetEvent(false);      
            _consumers.Add(newConsumerSignal);

            return newConsumerSignal;
        }

        /// <summary>
        /// Called by the video capture thread to provide a new frame, disposing of the previous one.
        /// </summary>
        /// <param name="newFrame">The latest captured video frame.</param>
        public void DistributeFrame(Mat newFrame)
        {
            if (_disposed) return;

            lock (_lock)
            {
                // Dispose the old frame before replacing it to prevent memory leaks from unmanaged OpenCvSharp memory.
                _latestFrame?.Dispose();

                // Store a deep copy (clone) of the new frame to ensure the distributor owns the memory 
                // and the producer can reuse the original Mat.
                _latestFrame = newFrame.Clone();
            }

            // Signal all registered consumers that a new frame is available.
            foreach (var consumer in _consumers)
            {
                consumer.Set();
            }
        }

        /// <summary>
        /// Called by consumer threads to safely retrieve a clone of the latest frame.
        /// </summary>
        /// <returns>A memory-safe clone of the latest frame, or null if no frame is available.</returns>
        public Mat GetLatestFrame()
        {
            lock (_lock)
            {
                // Return a deep copy (clone) so each consumer gets its own memory-safe instance 
                // and can dispose of it when done.
                return _latestFrame?.Clone();
            }
        }

        /// <summary>
        /// Disposes of managed resources, including the latest frame and all consumer signals.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // Dispose the latest frame safely within the lock.
            lock (_lock)
            {
                _latestFrame?.Dispose();
                _latestFrame = null;
            }

            // Dispose of all AutoResetEvent handles.
            foreach (var consumer in _consumers)
            {
                consumer.Dispose();
            }
        }
    }
}