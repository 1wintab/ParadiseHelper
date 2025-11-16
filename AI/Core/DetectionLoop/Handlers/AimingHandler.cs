using System;
using System.Drawing;
using System.Threading;
using System.Collections.Concurrent;
using ParadiseHelper.AI.Movement;
using ParadiseHelper.AI.Control.Mouse;
using ParadiseHelper.AI.Core.Settings;
using ParadiseHelper.AI.Core.Settings.Weapon;
using ParadiseHelper.Tools.WinAPI;
using ParadiseHelper.AI.Video.Detect;

namespace ParadiseHelper.AI.Core.DetectionLoop.Handlers
{
    /// <summary>
    /// Handler responsible for calculating the precise aiming coordinates and executing the firing sequence
    /// based on the best detected target received from the processing queue.
    /// This runs in a continuous loop separate from the detection process.
    /// </summary>
    public class AimingHandler : DetectionLoopBase
    {
        // Queue to receive the best detected target (YoloResult), game window size, and screen position for accurate aiming.
        private readonly BlockingCollection<Tuple<YoloResult, Size, Point>> _aimActionQueue;
        
        // The manager responsible for movement actions (like counter-strafing) to stabilize the shot.
        private readonly NavigationManager _navigationManager;
        
        // The shared AI state object, used to retrieve the currently equipped weapon name for settings lookup.
        private readonly AIState _state;

        /// <summary>
        /// Initializes a new instance of the <see cref="AimingHandler"/> class.
        /// </summary>
        /// <param name="aimActionQueue">The queue providing the best detected target and game window data.</param>
        /// <param name="navigationManager">The manager responsible for movement actions (e.g., strafing).</param>
        /// <param name="state">The current state of the AI, including information like the equipped weapon.</param>
        public AimingHandler(
            BlockingCollection<Tuple<YoloResult, Size, Point>> aimActionQueue,
            NavigationManager navigationManager,
            AIState state)
        {
            _aimActionQueue = aimActionQueue;
            _navigationManager = navigationManager;
            _state = state;
        }

        /// <summary>
        /// The main execution loop that continuously processes incoming targets for aiming and shooting.
        /// </summary>
        /// <param name="token">A token to observe for cancellation requests.</param>
        protected override void Loop(CancellationToken token)
        {
            try
            {
                // Consume targets from the queue as they become available.
                foreach (var (bestTarget, gameWindowSize, gameWindowScreenPos) in _aimActionQueue.GetConsumingEnumerable(token))
                {
                    if (token.IsCancellationRequested) return;

                    // Guard Clause: Check for window focus before aiming or counter-strafing to avoid interfering with other tasks.
                    if (!WindowController.IsGameWindowActiveAndFocused()) continue;

                    // Execute counter-strafe movement to increase firing accuracy.
                    _navigationManager.PerformCounterStrafe();

                    // 1. Get weapon settings (cooldowns, delays).
                    var weaponSettings = GetCurrentWeaponSettings();
                    int delayAfterAimMs = weaponSettings.DelayAfterAimMs;
                    int actionCooldownMs = weaponSettings.ActionCooldownMs;

                    // 2. Calculate target coordinates in the model's normalized coordinate system (0-1000).
                    var (modelX, modelY) = CalculateTargetCoordinates(bestTarget);

                    // 3. Scale coordinates to match the current game window size.

                    // Scale X from [0, ModelSettings.Width] to [0, gameWindowSize.Width].
                    int relativeTargetX = (int)(modelX / ModelSettings.Width * gameWindowSize.Width);

                    // Scale Y from [0, ModelSettings.Height] to [0, gameWindowSize.Height].
                    int relativeTargetY = (int)(modelY / ModelSettings.Height * gameWindowSize.Height);

                    // Move the mouse cursor instantly to the calculated target position.
                    MouseAimer.AimInstantlyAt(relativeTargetX, relativeTargetY, gameWindowScreenPos, gameWindowSize);

                    // Wait for a short period for the aim to settle before shooting.
                    Thread.Sleep(delayAfterAimMs);

                    // 4. Perform the shooting sequence.
                    PerformShootingSequence(actionCooldownMs, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Clean shutdown when the cancellation token is signaled.
            }
        }

        /// <summary>
        /// Calculates the precise target coordinates within the model's space, applying configured offsets (corrections).
        /// </summary>
        /// <param name="bestTarget">The best detected object result from the YOLO model.</param>
        /// <returns>A tuple containing the calculated X and Y coordinates in the model's coordinate space.</returns>
        private (float modelX, float modelY) CalculateTargetCoordinates(YoloResult bestTarget)
        {
            // Center of the detection box.
            float centerX = bestTarget.Box.X + bestTarget.Box.Width / 2f;
            float centerY = bestTarget.Box.Y + bestTarget.Box.Height / 2f;

            if (bestTarget.Label.Contains("head"))
            {
                // Headshot logic: Apply specific corrections for a head target.
                float modelX = centerX + AimbotSettings.AimCorrection.HeadHorizontalAimCorrection;
                float modelY = centerY + AimbotSettings.AimCorrection.HeadVerticalAimCorrection;
                
                return (modelX, modelY);
            }

            // Target is body or other; use general body aiming logic.
            // Apply a vertical offset (e.g., 15% from the top of the box) for the chest area.
            float targetYOffset = bestTarget.Box.Height * 0.15f;
            float bodyModelX = centerX + AimbotSettings.AimCorrection.HorizontalAimCorrection;
            float bodyModelY = bestTarget.Box.Y + targetYOffset + AimbotSettings.AimCorrection.VerticalAimCorrection;
            
            return (bodyModelX, bodyModelY);
        }

        /// <summary>
        /// Retrieves the configuration for the currently equipped weapon from the settings manager.
        /// </summary>
        /// <returns>The <see cref="WeaponConfig"/> for the current weapon, or a default configuration if not found.</returns>
        private WeaponConfig GetCurrentWeaponSettings()
        {
            // Tries to get settings for the current weapon, falls back to "Unknown", then to hardcoded defaults.
            return WeaponSettingsManager.GetSettings(_state.LatestWeaponName)
                ?? WeaponSettingsManager.GetSettings("Unknown")
                ?? new WeaponConfig { DelayAfterAimMs = 15, ActionCooldownMs = 170 };
        }

        /// <summary>
        /// Executes the shooting action for the configured number of shots, applying a cooldown between shots.
        /// </summary>
        /// <param name="actionCooldownMs">The delay in milliseconds between individual shots.</param>
        /// <param name="token">A token to observe for cancellation requests during the sequence.</param>
        private void PerformShootingSequence(int actionCooldownMs, CancellationToken token)
        {
            for (int i = 0; i < AimbotSettings.TimingAndShots.NumberOfShots; i++)
            {
                if (token.IsCancellationRequested) return;

                MouseShooter.Shoot();

                // Apply cooldown only between shots, not after the last one, to avoid unnecessary delay.
                if (i < AimbotSettings.TimingAndShots.NumberOfShots - 1)
                {
                    Thread.Sleep(actionCooldownMs);
                }
            }
        }
    }
}