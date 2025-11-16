using System;
using System.Threading;

namespace ParadiseHelper.AI.Core.DetectionLoop.Handlers
{
    /// <summary>
    /// Handler responsible for managing the AI's state based on the player's life cycle (e.g., respawn).
    /// It ensures the AI adapts its behavior (like restoring Aimbot status or triggering auto-buy) 
    /// immediately after the player respawns.
    /// </summary>
    public class PlayerStateManagementHandler : DetectionLoopBase
    {
        // Object tracking the player's current life state and flags (e.g., JustRespawned).
        private readonly PlayerState _playerState;

        // The shared AI state object containing global flags and data (e.g., AimbotEnabled, ShouldBuyWeapon).
        private readonly AIState _state;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerStateManagementHandler"/> class.
        /// </summary>
        /// <param name="playerState">The object tracking the player's current life state (e.g., JustRespawned flag).</param>
        /// <param name="state">The shared AI state object that will be updated upon respawn events.</param>
        public PlayerStateManagementHandler(PlayerState playerState, AIState state)
        {
            _playerState = playerState;
            _state = state;
        }

        /// <summary>
        /// The main execution loop that continuously monitors the player's state for respawn events.
        /// </summary>
        /// <param name="token">A token to observe for cancellation requests for clean thread shutdown.</param>
        protected override void Loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Check if the player has just respawned since the last loop iteration.
                if (_playerState.JustRespawned)
                {
                    _state.LastRespawnTimestamp = DateTime.UtcNow;

                    // Acknowledge the respawn event so it's not processed again.
                    _playerState.ConsumeRespawnedFlag();

                    bool needsToBuy = CheckIfWeaponPurchaseIsNeeded();
                    if (needsToBuy)
                    {
                        // Enable the AutoBuy flag to trigger the weapon purchase mechanism in another handler.
                        _state.ShouldBuyWeapon = true;
                    }
                    else
                    {
                        // Restore Aimbot state to what it was before death, defaulting to enabled if no state was saved.
                        bool wasAimbotEnabledBeforeDeath = _state.AimbotStateBeforeBuy ?? true;

                        _state.AimbotEnabled = wasAimbotEnabledBeforeDeath;

                        // Clear the saved state after use to prepare for the next death/buy cycle.
                        _state.AimbotStateBeforeBuy = null;
                    }
                }

                // Short interval check to prevent high CPU usage.
                Thread.Sleep(250);
            }
        }

        /// <summary>
        /// Determines if the player needs to buy a new weapon based on the currently equipped weapon 
        /// and the list of desired weapons defined in <see cref="AIState"/>.
        /// </summary>
        /// <returns>True if a weapon purchase is needed; otherwise, false.</returns>
        private bool CheckIfWeaponPurchaseIsNeeded()
        {
            // A purchase is needed if the current weapon is unknown (not detected) or if it's not one of the desired weapons.
            if (_state.LatestWeaponName == "Unknown" || !AIState.desiredWeapons.Contains(_state.LatestWeaponName))
            {
                return true;
            }

            return false;
        }
    }
}