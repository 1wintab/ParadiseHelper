using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using OpenCvSharp.Extensions;
using ParadiseHelper.WinAPI;
using ParadiseHelper.AI.Core.Settings;
using ParadiseHelper.AI.Core.Settings.Weapon;
using ParadiseHelper.AI.Video.Detect;
using ParadiseHelper.AI.Video.GameVideoSource;

namespace ParadiseHelper.AI.Core.DetectionLoop.Handlers
{
    /// <summary>
    /// Handler responsible for running YOLO object detection on frames from the dedicated queue
    /// and selecting the most optimal target for the aiming system.
    /// </summary>
    public class YoloDetectionHandler : DetectionLoopBase
    {
        // Reference to the video frame capture system.
        private readonly MultiResolutionCapture _capture;

        // The YOLO model detector implementation.
        private readonly YoloDetector _detector;

        // Queue to send the best target to the AimingHandler thread.
        private readonly BlockingCollection<Tuple<YoloResult, System.Drawing.Size, System.Drawing.Point>> _aimActionQueue;

        // Shared state object containing game and AI status.
        private readonly AIState _state;

        // Player-specific state information (e.g., combat readiness).
        private readonly PlayerState _playerState;

        // Synchronization object for protecting shared state variables.
        private readonly object _lockObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="YoloDetectionHandler"/> class.
        /// </summary>
        /// <param name="capture">The source for capturing multi-resolution game video frames.</param>
        /// <param name="detector">The YOLO model detector.</param>
        /// <param name="aimActionQueue">The queue for passing detected targets to the aiming handler.</param>
        /// <param name="state">The shared AI state containing global information and debugging flags.</param>
        /// <param name="playerState">The state object containing player-specific combat readiness checks.</param>
        /// <param name="lockObject">The object used for synchronizing access to shared state data.</param>
        public YoloDetectionHandler(
            MultiResolutionCapture capture,
            YoloDetector detector,
            BlockingCollection<Tuple<YoloResult, System.Drawing.Size, System.Drawing.Point>> aimActionQueue,
            AIState state,
            PlayerState playerState,
            object lockObject)
        {
            _capture = capture;
            _detector = detector;
            _aimActionQueue = aimActionQueue;
            _state = state;
            _playerState = playerState;
            _lockObject = lockObject;
        }

        /// <summary>
        /// The main execution loop for continuous YOLO object detection.
        /// </summary>
        /// <param name="token">The cancellation token to stop the loop gracefully.</param>
        protected override void Loop(CancellationToken token)
        {
            var fpsWatch = Stopwatch.StartNew();
            int detectionFrameCount = 0;

            while (!token.IsCancellationRequested)
            {
                // Take the latest frame ready for YOLO processing from the queue.
                using var frameToProcess = _capture.YoloFrameQueue.Take(token);

                if (frameToProcess == null) continue;

                // Perform the object detection.
                var detections = _detector.Detect(frameToProcess, ModelSettings.Width, ModelSettings.Height);

                // Get game window properties for accurate mouse movement and scaling.
                var gameWindowSize = GameWindowHelper.GetGameWindowSize("Counter-Strike 2");
                var gameWindowScreenPos = GameWindowHelper.GetGameWindowScreenPosition("Counter-Strike 2");

                if (gameWindowSize.IsEmpty) continue;

                // Select the best target (prioritizing head and center of screen).
                var bestTarget = FindBestTarget(detections, gameWindowSize);

                // Update player's combat status.
                _playerState.UpdateCombatState(bestTarget != null);

                // Check preconditions for triggering aimbot action.
                if (_state.AimbotEnabled && _playerState.IsGameReadyForAimbot && bestTarget != null)
                {
                    // Get weapon settings to respect cooldowns.
                    var weaponSettings = WeaponSettingsManager.GetSettings(_state.LatestWeaponName)
                        ?? WeaponSettingsManager.GetSettings("Unknown")
                        ?? new WeaponConfig { DelayAfterAimMs = 15, ActionCooldownMs = 170 };

                    // Check if the required weapon cooldown has passed since the last action.
                    if ((DateTime.UtcNow - _state.LastActionTimestamp).TotalMilliseconds >= weaponSettings.ActionCooldownMs)
                    {
                        // Queue the target and window info for the AimingHandler to process.
                        _aimActionQueue.Add(Tuple.Create(bestTarget, gameWindowSize, gameWindowScreenPos), token);
                        _state.LastActionTimestamp = DateTime.UtcNow;
                    }
                }

                // Update shared state variables (frame, results, FPS) under the lock.
                lock (_lockObject)
                {
                    _state.LatestFrame?.Dispose();
                    // Store the frame as a Bitmap for UI/debug display.
                    _state.LatestFrame = frameToProcess.ToBitmap();
                    _state.LatestResults = detections;
                }

                detectionFrameCount++;
                
                // Update FPS counter every second.
                if (fpsWatch.ElapsedMilliseconds >= 1000)
                {
                    lock (_lockObject) { _state.DetectionFps = detectionFrameCount; }
                    detectionFrameCount = 0;
                    fpsWatch.Restart();
                }
            }
        }

        /// <summary>
        /// Selects the most optimal target from the list of detections based on priority (headshot) and proximity to the screen center.
        /// </summary>
        /// <param name="detections">The list of detected objects from the YOLO model.</param>
        /// <param name="gameWindowSize">The size of the game window in pixels.</param>
        /// <returns>The best <see cref="YoloResult"/> target, or null if no targets are found.</returns>
        private YoloResult FindBestTarget(List<YoloResult> detections, System.Drawing.Size gameWindowSize)
        {
            if (detections == null || !detections.Any()) return null;

            double centerX = gameWindowSize.Width / 2.0;
            double centerY = gameWindowSize.Height / 2.0;

            // Priority: 1. Head detections, 2. Closest to the screen center.
            return detections
                .OrderBy(d => d.Label.Contains("head") ? 0 : 1) // Prioritize 'head' label (0 < 1)
                .ThenBy(d => GetDetectionDistanceToCenter(d, gameWindowSize, centerX, centerY))
                .FirstOrDefault();
        }

        /// <summary>
        /// Calculates the Euclidean distance from the detection's center to the screen's center,
        /// ensuring coordinates are scaled from the model's output size to the actual game window size.
        /// </summary>
        /// <param name="detection">The YOLO detection result.</param>
        /// <param name="gameWindowSize">The size of the game window in pixels.</param>
        /// <param name="centerX">The center X coordinate of the game window.</param>
        /// <param name="centerY">The center Y coordinate of the game window.</param>
        /// <returns>The distance in pixels from the target center to the screen center.</returns>
        private double GetDetectionDistanceToCenter(YoloResult detection, System.Drawing.Size gameWindowSize, double centerX, double centerY)
        {
            // 1. Calculate the center of the detection in the model's coordinate system (e.g., 0-1000).
            double detectionCenterXModel = detection.Box.X + detection.Box.Width / 2.0;
            double detectionCenterYModel = detection.Box.Y + detection.Box.Height / 2.0;

            // 2. Scale the detection center coordinates to the actual game window size.
            double detectionCenterX = detectionCenterXModel * (gameWindowSize.Width / (double)ModelSettings.Width);
            double detectionCenterY = detectionCenterYModel * (gameWindowSize.Height / (double)ModelSettings.Height);

            // 3. Calculate the distance vector (delta X and delta Y) from the screen center.
            double deltaX = detectionCenterX - centerX;
            double deltaY = detectionCenterY - centerY;

            // 4. Calculate the Euclidean distance (distance = sqrt(dx^2 + dy^2)).
            return Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));
        }
    }
}