using System;
using System.Threading;
using OpenCvSharp;
using ParadiseHelper.AI.Weapon;
using ParadiseHelper.AI.Control.KeyBoard.Binds.AutoSkipWeapon;
using ParadiseHelper.AI.Video.GameVideoSource;

namespace ParadiseHelper.AI.Core.DetectionLoop.Handlers
{
    /// <summary>
    /// Handler responsible for identifying the currently equipped weapon using visual detection 
    /// and executing actions like skipping 'extra' weapons after a respawn.
    /// </summary>
    public class WeaponDetectionHandler : DetectionLoopBase
    {
        // The fixed interval (in milliseconds) between full detection cycles to manage resource usage.
        private const int _weaponDetectionIntervalMs = 150;

        // Reference to the video frame capture system.
        private readonly MultiResolutionCapture _capture;

        // Manager handling the actual weapon detection logic.
        private readonly WeaponManager _weaponManager;

        // Shared state object containing game and AI status.
        private readonly AIState _state;

        // Logic for automatically switching from secondary/extra weapons to the main weapon.
        private readonly AutoSkipWeapon _autoSkipWeapon;

        // Synchronization object for protecting shared state variables.
        private readonly object _lockObject;

        // Signal used to wait for a new frame to become available for processing.
        private readonly AutoResetEvent _weaponFrameSignal;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponDetectionHandler"/> class.
        /// </summary>
        /// <param name="capture">The source for capturing multi-resolution game video frames.</param>
        /// <param name="weaponManager">The manager responsible for the weapon detection algorithm.</param>
        /// <param name="state">The shared AI state containing game information and debugging flags.</param>
        /// <param name="autoSkipWeapon">The helper class to perform weapon switching.</param>
        /// <param name="lockObject">The object used for synchronizing access to shared state data.</param>
        public WeaponDetectionHandler(
            MultiResolutionCapture capture,
            WeaponManager weaponManager,
            AIState state,
            AutoSkipWeapon autoSkipWeapon,
            object lockObject)
        {
            _capture = capture;
            _weaponManager = weaponManager;
            _state = state;
            _autoSkipWeapon = autoSkipWeapon;
            _lockObject = lockObject;

            // Register as a consumer to receive frame notifications for weapon detection.
            _weaponFrameSignal = _capture.FrameDistributor.RegisterConsumer();
        }

        /// <summary>
        /// The main execution loop for continuous weapon detection and auto-skip logic.
        /// </summary>
        /// <param name="token">The cancellation token to stop the loop gracefully.</param>
        protected override void Loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Wait for the signal that a new frame is ready.
                _weaponFrameSignal.WaitOne();

                if (token.IsCancellationRequested) break;

                // Get the latest frame for processing.
                using var frame = _capture.FrameDistributor.GetLatestFrame();
                if (frame == null || frame.Empty())
                {
                    // Short sleep if the frame is null, then continue waiting for the signal.
                    // This prevents busy-waiting if the frame distributor temporarily fails.
                    Thread.Sleep(10);
                    continue;
                }

                // Perform weapon detection on the frame.
                Mat processedImage = _weaponManager.DetectWeapon(frame, _state.ShowWeaponDebugImage);

                // Update shared state variables under the lock.
                lock (_lockObject)
                {
                    _state.LatestWeaponName = _weaponManager.LatestWeaponName;
                    _state.LatestWeaponMatchValue = _weaponManager.LatestWeaponMatchValue;

                    // Dispose of the previous debug image and store the new one (if debugging is enabled).
                    _state.LatestProcessedWeaponImage?.Dispose();
                    _state.LatestProcessedWeaponImage = processedImage?.Clone();
                }

                // Ensure the local reference to the processed image is disposed after the state update.
                processedImage?.Dispose();

                // Determine whether less than 6 seconds have passed since respawn
                bool isWithinSkipWindow = (DateTime.UtcNow - _state.LastRespawnTimestamp).TotalSeconds < 6;

                // Check if the current map is known
                bool isMapKnown = _state.LatestMapName != "Unknown";

                // Check if the detected weapon is one of the 'extra' weapons (like knife/grenade) that should be skipped.
                if (isWithinSkipWindow && isMapKnown && AIState.ExtraWeapons.Contains(_state.LatestWeaponName))
                {
                    // Automatically switch to the primary weapon.
                    _autoSkipWeapon.PerformAutoSkipToMainWeapon();
                }

                // Wait before processing the next detection, regardless of frame signal timing.
                // This controls the maximum frequency of the detection loop to save CPU resources.
                Thread.Sleep(_weaponDetectionIntervalMs);
            }
        }
    }
}