using System.Threading;
using OpenCvSharp;
using ParadiseHelper.AI.Orientation.Map;
using ParadiseHelper.AI.Orientation.Radar;
using ParadiseHelper.AI.Control.KeyBoard.Binds.AutoDisconnect;
using Core;
using ParadiseHelper.AI.Video.GameVideoSource;

namespace ParadiseHelper.AI.Core.DetectionLoop.Handlers
{
    /// <summary>
    /// Handler responsible for continuously monitoring the game's radar region to identify the current map 
    /// using image template matching techniques. This process runs in a separate thread.
    /// </summary>
    public class MapDetectionHandler : DetectionLoopBase
    {
        // The fixed interval (in milliseconds) for map detection attempts when the player is dead or the loop continues.
        private const int _mapDetectionIntervalMs = 250;

        // The video capture source manager used to retrieve the latest game frame for analysis.
        private readonly MultiResolutionCapture _capture;
        
        // The state object holding information about the player's status (e.g., if the player is alive or dead).
        private readonly PlayerState _playerState;
        
        // The shared AI state object where detection results (like the map name and match value) are stored.
        private readonly AIState _state;
        
        // The utility to trigger a forced game disconnection when entering a forbidden map.
        private readonly AutoDisconnect _autoDisconnect;

        // Object used for locking access to shared state variables to ensure thread safety during updates.
        private readonly object _lockObject;

        // Signal used to wait for a notification that a new frame relevant for map detection is available.
        private readonly AutoResetEvent _mapFrameSignal;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapDetectionHandler"/> class.
        /// </summary>
        /// <param name="capture">The video capture source manager.</param>
        /// <param name="playerState">The state object containing player status (e.g., if the player is dead).</param>
        /// <param name="state">The shared AI state object where the detected map name is stored.</param>
        /// <param name="autoDisconnect">The handler responsible for triggering an automatic disconnect on forbidden maps.</param>
        /// <param name="lockObject">The shared synchronization object used for thread-safe access to <see cref="AIState"/>.</param>
        public MapDetectionHandler(
            MultiResolutionCapture capture,
            PlayerState playerState,
            AIState state,
            AutoDisconnect autoDisconnect,
            object lockObject
            )
        {
            _capture = capture;
            _playerState = playerState;
            _state = state;
            _autoDisconnect = autoDisconnect;
            _lockObject = lockObject;

            // Register as a consumer to receive frame notifications only when a new frame is ready.
            _mapFrameSignal = _capture.FrameDistributor.RegisterConsumer();

            // Initialize the MapDetection class by specifying the directory containing map templates.
            MapDetection.Initialize(FilePaths.AI.Templates.MapsDirectory);
        }

        /// <summary>
        /// The main execution loop that waits for new frames, detects the map, and updates the shared state.
        /// </summary>
        /// <param name="token">A token to observe for cancellation requests for clean thread shutdown.</param>
        protected override void Loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Wait for the signal indicating a new frame is ready for map detection.
                _mapFrameSignal.WaitOne();

                if (token.IsCancellationRequested) break;

                // If the player is dead, the radar area might not be visible or reliable. Reset the map state.
                if (_playerState.IsPlayerDead)
                {
                    lock (_lockObject)
                    {
                        // Reset map state to 'Unknown' when the player is dead.
                        _state.LatestMapName = "Unknown";
                        _state.LatestMapMatchValue = 1.0;
                        _state.LatestMapBoundingBox = null;
                    }
                    // Wait briefly before attempting detection again to reduce resource usage.
                    Thread.Sleep(_mapDetectionIntervalMs);
                    continue;
                }

                // Get the latest captured frame for processing. 'using var' ensures the Mat object is disposed of correctly.
                using var frame = _capture.FrameDistributor.GetLatestFrame();
                if (frame == null || frame.Empty())
                {
                    continue;
                }

                // Define the specific region of interest (ROI) where the radar/map is expected in the game window.
                Rect radarRegion = new Rect(
                    RadarCapture.RADAR_X,
                    RadarCapture.RADAR_Y,
                    RadarCapture.RADAR_WIDTH,
                    RadarCapture.RADAR_HEIGHT
                );

                using (Mat radarSquare = new Mat(frame, radarRegion))
                {
                    // Perform template matching against known map images to identify the current map.
                    var mapResult = MapDetection.DetectMap(radarSquare);

                    // Update shared state variables under a lock to ensure thread safety.
                    lock (_lockObject)
                    {
                        _state.LatestMapName = mapResult.MapName;
                        _state.LatestMapMatchValue = mapResult.MatchValue;
                        _state.LatestMapBoundingBox = mapResult.BoundingBox;
                    }
                }

                // Check for forbidden maps only if the match confidence is high (e.g., MatchValue <= 0.2).
                string currentMap = _state.LatestMapMatchValue <= 0.2 ? _state.LatestMapName : "Unknown";
                if (currentMap != "Unknown" && AIState.forbiddenMaps.Contains(currentMap))
                {
                    // Trigger the configured automatic disconnect sequence to exit the forbidden map.
                    _autoDisconnect.PerformAutoDisconnect();
                }

                // Wait before the next detection attempt to control the processing rate.
                Thread.Sleep(_mapDetectionIntervalMs);
            }
        }
    }
}