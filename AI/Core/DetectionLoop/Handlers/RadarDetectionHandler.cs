using System;
using System.Threading;
using ParadiseHelper.AI.Movement;
using ParadiseHelper.AI.Orientation.Radar;
using ParadiseHelper.AI.Orientation.Map.Navmesh;
using ParadiseHelper.AI.Video.GameVideoSource;

namespace ParadiseHelper.AI.Core.DetectionLoop.Handlers
{
    /// <summary>
    /// Handler responsible for high-frequency radar detection and processing. 
    /// It continuously monitors the game state, extracts player location and direction from the radar, 
    /// and feeds this crucial data to the navigation and state management systems.
    /// </summary>
    public class RadarDetectionHandler : DetectionLoopBase
    {
        // The fixed interval (in milliseconds) at which the radar detection loop attempts to run.
        // A smaller value (e.g., 32ms) enables high-frequency updates (~30 FPS).
        private const int _radarDetectionIntervalMs = 32;

        // Video capture source manager, used to retrieve the latest raw game frame for radar processing.
        private readonly MultiResolutionCapture _capture;

        // Manager responsible for computer vision processing of the radar image to detect player,
        // direction, and death status.
        private readonly RadarManager _radarManager;

        // Manager responsible for pathfinding and translating detection results into in-game movement commands.
        private readonly NavigationManager _navigationManager;

        // The shared global AI state object, used to read map data and store radar results (e.g., player location).
        private readonly AIState _state;

        // The state object containing data specific to the player's status (e.g., alive/dead, in combat).
        private readonly PlayerState _playerState;

        // The synchronization object used to ensure thread-safe access to shared AIState variables.
        private readonly object _lockObject;

        // Signal that waits for notification when a new frame is available for radar processing.
        private readonly AutoResetEvent _radarFrameSignal;

        /// <summary>
        /// Initializes a new instance of the <see cref="RadarDetectionHandler"/> class.
        /// </summary>
        /// <param name="capture">The video capture source manager.</param>
        /// <param name="radarManager">The radar processing logic manager.</param>
        /// <param name="navigationManager">The AI navigation and movement manager.</param>
        /// <param name="state">The shared global AI state object.</param>
        /// <param name="playerState">The state object tracking the player's life and combat status.</param>
        /// <param name="lockObject">The shared synchronization object for thread safety.</param>
        public RadarDetectionHandler(
            MultiResolutionCapture capture,
            RadarManager radarManager,
            NavigationManager navigationManager,
            AIState state,
            PlayerState playerState,
            object lockObject
            )
        {
            _capture = capture;
            _radarManager = radarManager;
            _navigationManager = navigationManager;
            _state = state;
            _playerState = playerState;
            _lockObject = lockObject;

            // Register as a frame consumer for radar processing.
            _radarFrameSignal = _capture.FrameDistributor.RegisterConsumer();

            // Initialize radar capture templates used by the RadarManager.
            RadarCapture.Initialize(
                @"AI\Data\Templates\Radar\ct_mark.png",
                @"AI\Data\Templates\Radar\t_mark.png",
                @"AI\Data\Templates\Radar\ct_dead_mark.png",
                @"AI\Data\Templates\Radar\tt_dead_mark.png"
            );
        }

        /// <summary>
        /// The main detection loop that runs on a separate thread, continuously checking the radar 
        /// at the defined interval for player state updates.
        /// </summary>
        /// <param name="token">The cancellation token to stop the loop when the application closes.</param>
        protected override void Loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Wait for the signal that a new frame is ready for processing.
                _radarFrameSignal.WaitOne();

                if (token.IsCancellationRequested) break;

                // Check for player life status based on screen elements.
                _playerState.UpdatePlayerLiveness();
                if (_playerState.IsPlayerDead)
                {
                    // If dead, skip processing and wait for the next interval to avoid unnecessary work.
                    Thread.Sleep(_radarDetectionIntervalMs);
                    
                    continue;
                }

                using (var frame = _capture.FrameDistributor.GetLatestFrame())
                {
                    if (frame == null || frame.Empty()) continue;

                    // Step 1: Process the radar region in the frame (detect player/direction/enemies).
                    var processedRadarImage = _radarManager.ProcessRadar(frame);

                    // Step 2: Handle player death detection (if the death skull is visible).
                    if (_radarManager.IsDeathDetected())
                    {
                        _playerState.SetPlayerAsDead();

                        // Reset navigation and temporarily disable aimbot to prevent movement while dead.
                        _navigationManager.ResetNavigationState("Player death");

                        _state.AimbotStateBeforeBuy = _state.AimbotEnabled;
                        _state.AimbotEnabled = false;
                        _state.IsFirstBuyDone = false;

                        processedRadarImage?.Dispose();
                        
                        continue;
                    }

                    // Step 3: Determine map context and visibility.
                    // Get currently detected map name (requires a high confidence match value <= 0.2).
                    string currentMapName = GetLatestMapName();
                    var currentMapBox = _state.LatestMapBoundingBox;

                    // Check if the player is visible on the radar and the map/bounding box are known.
                    bool isPlayerVisible = _radarManager.PlayerLocationOnRadar.HasValue
                        && currentMapName != "Unknown"
                        && currentMapBox.HasValue
                        && IsPlayerOnMapWithBoxMargin();

                    _playerState.IsGameReadyForAimbot = isPlayerVisible;

                    if (isPlayerVisible)
                    {
                        _state.LastTimePlayerSeen = DateTime.UtcNow;

                        // Trigger the initial weapon purchase if not yet done (one-time logic).
                        if (!_state.IsFirstBuyDone)
                        {
                            _state.ShouldBuyWeapon = true;
                            _state.IsFirstBuyDone = true;
                        }

                        var navmesh = NavmeshCore.FindNavmesh(currentMapName);

                        // Update navigation based on current state (move the player).
                        _navigationManager.UpdateMovement(
                            isPlayerVisible: true,
                            isInCombat: _playerState.IsInCombat,
                            shouldBuyWeapon: _state.ShouldBuyWeapon,
                            playerLocation: _radarManager.PlayerLocationOnRadar,
                            playerDirectionDegrees: GetPlayerDirectionInDegrees(),
                            currentMapName: currentMapName,
                            currentMapBox: currentMapBox,
                            navmesh: navmesh
                        ).GetAwaiter().GetResult();
                    }
                    else
                    {
                        // Player not visible/map unknown: reset movement input state immediately.
                        _navigationManager.UpdateMovement(
                            isPlayerVisible: false,
                            isInCombat: false,
                            shouldBuyWeapon: false,
                            playerLocation: null,
                            playerDirectionDegrees: -1,
                            currentMapName: null,
                            currentMapBox: null,
                            navmesh: null
                        ).GetAwaiter().GetResult();

                        // If the navigation timeout has been exceeded, perform a full navigation state reset.
                        if (_navigationManager.CurrentNode != null
                            && DateTime.UtcNow - _state.LastTimePlayerSeen > AIState.NavigationResetTimeout)
                        {
                            _navigationManager.ResetNavigationState("Player not seen for too long");
                        }
                    }

                    // Step 4: Update shared state variables under the lock for thread-safe access.
                    lock (_lockObject)
                    {
                        _state.PlayerLocationOnRadar = _radarManager.PlayerLocationOnRadar;
                        _state.PlayerDirection = _radarManager.PlayerDirection;
                        _state.LatestRadarMatchValue = _radarManager.LatestRadarMatchValue;

                        // Manage the debug image state (dispose previous image and store a clone if debugging is enabled).
                        _state.LatestProcessedRadarImage?.Dispose();
                        _state.LatestProcessedRadarImage = _state.ShowRadarDebugImage
                            ? processedRadarImage?.Clone()
                            : null;
                    }

                    // Dispose the local copy of the processed image if it was not cloned and stored for debug.
                    processedRadarImage?.Dispose();
                }

                Thread.Sleep(_radarDetectionIntervalMs);
            }
        }

        /// <summary>
        /// Retrieves the map name, but only if the map match confidence is high enough (0.2).
        /// </summary>
        /// <returns>The confirmed map name or "Unknown".</returns>
        private string GetLatestMapName()
        {
            lock (_lockObject)
            {
                // If the match value is above the confidence threshold (0.2), assume the map is unknown.
                return _state.LatestMapMatchValue <= 0.2
                    ? _state.LatestMapName
                    : "Unknown";
            }
        }

        /// <summary>
        /// Converts the player's vector direction from the radar into a degree value (0-360).
        /// </summary>
        /// <returns>The player's direction in degrees, or -1 if the direction is unknown.</returns>
        private double GetPlayerDirectionInDegrees()
        {
            lock (_lockObject)
            {
                if (!_state.PlayerDirection.HasValue) return -1;

                // Calculate angle using Atan2. Note the Y-axis inversion (-Y) due to image coordinate system.
                double angleInRadians = Math.Atan2(
                    _state.PlayerDirection.Value.X,
                    -_state.PlayerDirection.Value.Y);

                double angleInDegrees = angleInRadians * 180 / Math.PI;

                // Normalize the angle to the 0-360 degree range.
                if (angleInDegrees < 0)
                {
                    angleInDegrees += 360;
                }

                return angleInDegrees;
            }
        }

        /// <summary>
        /// Checks if the player's location, detected on the radar, is within the bounds of the detected map template.
        /// Includes a margin to allow for slight offsets or template errors.
        /// </summary>
        /// <returns>True if the player is within the map bounds (with margin), otherwise false.</returns>
        private bool IsPlayerOnMapWithBoxMargin()
        {
            var playerLocation = _state.PlayerLocationOnRadar;
            var mapBoundingBox = _state.LatestMapBoundingBox;

            if (!playerLocation.HasValue || !mapBoundingBox.HasValue) return false;

            int templateWidth = RadarCapture.GetTemplateWidth();
            int templateHeight = RadarCapture.GetTemplateHeight();

            // Margins are defined to allow the player to be slightly outside the detected map box 
            // without triggering a false negative boundary check.
            int leftMargin = templateWidth;
            int topMargin = templateHeight;
            int rightMargin = 0;
            int bottomMargin = 0;

            // Check the player's location against the map bounding box plus margins.
            bool isInLeftBound = playerLocation.Value.X >= mapBoundingBox.Value.X - leftMargin;
            bool isInTopBound = playerLocation.Value.Y >= mapBoundingBox.Value.Y - topMargin;
            bool isInRightBound = playerLocation.Value.X <= mapBoundingBox.Value.X + mapBoundingBox.Value.Width + rightMargin;
            bool isInBottomBound = playerLocation.Value.Y <= mapBoundingBox.Value.Y + mapBoundingBox.Value.Height + bottomMargin;

            return isInLeftBound
                && isInTopBound
                && isInRightBound
                && isInBottomBound;
        }
    }
}