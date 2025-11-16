using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using OpenCvSharp;

namespace ParadiseHelper.AI.Video.GameVideoSource
{
    /// <summary>
    /// Manages video capture (e.g., from a webcam) and distributes frames to different consumers
    /// at potentially different resolutions. It uses a dedicated queue for high-priority AI detection (YOLO)
    /// and a distributor for full-resolution display/monitoring.
    /// </summary>
    public class MultiResolutionCapture : IDisposable
    {
        // OpenCV object for video capture.
        private readonly VideoCapture _capture;         

        // Token source for loop cancellation.
        private readonly CancellationTokenSource _cts;    

        // The dedicated thread/Task running the capture logic.
        private readonly Task _captureLoop;             

        // Flag to track resource disposal status.
        private bool _disposed = false;                 

        /// <summary>
        /// Dedicated <see cref="BlockingCollection{T}"/> queue for the YOLO detection thread.
        /// Frames are resized to the model's required input size before being added to this queue.
        /// </summary>
        public BlockingCollection<Mat> YoloFrameQueue { get; }

        /// <summary>
        /// A <see cref="FrameDistributor"/> for providing the full-resolution frame to multiple other consumers (e.g., display, radar).
        /// </summary>
        public FrameDistributor FrameDistributor { get; }

        /// <summary>
        /// Gets a value indicating whether the capture device is successfully initialized and open.
        /// </summary>
        public bool IsReady => _capture != null && _capture.IsOpened();

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiResolutionCapture"/> class and starts the capture loop.
        /// </summary>
        /// <param name="cameraIndex">The index of the camera to open (default is 0).</param>
        /// <param name="yoloWidth">The target width for frames added to the YOLO queue.</param>
        /// <param name="yoloHeight">The target height for frames added to the YOLO queue.</param>
        /// <exception cref="InvalidOperationException">Thrown if the webcam fails to open.</exception>
        public MultiResolutionCapture(int cameraIndex = 0, int yoloWidth = 512, int yoloHeight = 512)
        {
            // Initialize VideoCapture using the DirectShow backend (often better for webcams).
            _capture = new VideoCapture(cameraIndex, VideoCaptureAPIs.DSHOW);
            if (!_capture.IsOpened())
            {
                throw new InvalidOperationException($"Failed to open webcam with index {cameraIndex} using DSHOW.");
            }

            // Set desired capture resolution and frame rate.
            _capture.FrameWidth = 1280;
            _capture.FrameHeight = 720;
            _capture.Set(VideoCaptureProperties.Fps, 60);

            // Initialize the queue and distributor components.
            YoloFrameQueue = new BlockingCollection<Mat>(2); // Small queue size to minimize latency.
            FrameDistributor = new FrameDistributor();

            // Start the background capture and distribution loop.
            _cts = new CancellationTokenSource();
            _captureLoop = Task.Run(() => CaptureAndDistributeLoop(_cts.Token, yoloWidth, yoloHeight));
        }

        /// <summary>
        /// The main loop that reads frames, resizes for YOLO, and distributes the original frame.
        /// </summary>
        /// <param name="token">A cancellation token to gracefully stop the loop.</param>
        /// <param name="yoloWidth">The width to resize frames for the YOLO queue.</param>
        /// <param name="yoloHeight">The height to resize frames for the YOLO queue.</param>
        private void CaptureAndDistributeLoop(CancellationToken token, int yoloWidth, int yoloHeight)
        {
            while (!token.IsCancellationRequested)
            {
                using var originalMat = new Mat();

                // Read the frame from the video source.
                if (_capture.Read(originalMat) && !originalMat.Empty())
                {
                    // 1. Process for YOLO: Resize the frame to the model's required input size.
                    using (var yoloMat = new Mat())
                    {
                        // Resize the frame using the specified dimensions.
                        Cv2.Resize(originalMat, yoloMat, new Size(yoloWidth, yoloHeight));

                        // Add a clone of the resized frame to the YOLO queue if there is capacity.
                        // Cloning is essential here as the original Mat will be disposed by the 'using' block.
                        if (YoloFrameQueue.Count < YoloFrameQueue.BoundedCapacity)
                        {
                            YoloFrameQueue.Add(yoloMat.Clone(), token);
                        }
                    }

                    // 2. Distribute the high-resolution frame to all other consumers (e.g., UI display).
                    // The distributor handles cloning for thread safety.
                    FrameDistributor.DistributeFrame(originalMat);
                }
                else
                {
                    // Wait briefly if reading fails to prevent a tight loop and reduce CPU usage.
                    Task.Delay(1, token).Wait();
                }
            }
        }

        /// <summary>
        /// Stops the capture loop and disposes of all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // Signal the capture loop to stop and wait for it to finish gracefully.
            _cts?.Cancel();
            try { _captureLoop?.Wait(500); } catch { /* Ignore task cancellation/timeout exceptions */ }

            // Dispose of all unmanaged resources.
            _capture?.Dispose();
            YoloFrameQueue?.Dispose();
            FrameDistributor?.Dispose();
        }
    }
}