using System;
using System.Threading;
using ParadiseHelper.AI.ClickerAutoRunGame;
using ParadiseHelper.AI.Video.GameVideoSource;

namespace ParadiseHelper.AI.Core.DetectionLoop.Handlers
{
    /// <summary>
    /// A handler responsible for continuously detecting and managing the game's lobby state.
    /// This implementation uses an <see cref="AutoResetEvent"/> signal to efficiently wait 
    /// for a new video frame before processing, following the Producer/Consumer pattern.
    /// </summary>
    public class LobbyDetectionHandler : DetectionLoopBase
    {
        // Reference to the capture system providing video frames.
        private readonly MultiResolutionCapture _capture;

        // The manager responsible for interpreting the lobby state and executing necessary actions.
        private readonly LobbyManager _lobbyManager;

        // Signal used to wait for a new frame to become available. Registered as a consumer 
        // with the MultiResolutionCapture's FrameDistributor.
        private readonly AutoResetEvent _lobbyFrameSignal;

        /// <summary>
        /// Initializes a new instance of the <see cref="LobbyDetectionHandler"/> class.
        /// </summary>
        /// <param name="capture">The source for capturing multi-resolution game video frames.</param>
        /// <param name="lobbyManager">The manager responsible for processing lobby state and initiating actions.</param>
        public LobbyDetectionHandler(MultiResolutionCapture capture, LobbyManager lobbyManager)
        {
            _capture = capture;
            _lobbyManager = lobbyManager;
            // Register this handler as a consumer to receive notifications when a new frame is ready.
            _lobbyFrameSignal = _capture.FrameDistributor.RegisterConsumer();
        }

        /// <summary>
        /// The main execution loop for lobby state detection. It blocks on the frame signal 
        /// and processes the latest frame only when one is available.
        /// </summary>
        /// <param name="token">The cancellation token to stop the loop gracefully.</param>
        protected override void Loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Wait for the AutoResetEvent signal, meaning a new frame is available.
                    _lobbyFrameSignal.WaitOne();

                    if (token.IsCancellationRequested) break;

                    // Get the latest frame from the distributor for processing.
                    using var frame = _capture.FrameDistributor.GetLatestFrame();

                    // Process the frame if it's valid and not empty.
                    if (frame != null && !frame.Empty())
                    {
                        _lobbyManager.ProcessLobbyStateAndActions(frame);
                    }

                    // Add a small delay to avoid excessive looping frequency, especially if the frame rate is inconsistent.
                    Thread.Sleep(200);
                }
                catch (OperationCanceledException)
                {
                    // Exit the loop gracefully when cancellation is requested.
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in LobbyDetectionLoop: {ex.Message}");
                    Thread.Sleep(1000);
                }
            }
        }
    }
}