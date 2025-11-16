using System;

namespace ParadiseHelper.AI.Core
{
    /// <summary>
    /// Manages the player's dynamic state, including tracking death/respawn status
    /// and combat engagement/disengagement timing.
    /// </summary>
    public class PlayerState
    {
        // Timestamp when the player's death was last detected.
        private DateTime _deathTimestamp = DateTime.MinValue;

        // Timestamp when a hostile target was last successfully seen.
        private DateTime _lastTargetSeenTimestamp = DateTime.MinValue;

        // Duration the player is considered dead after death detection, allowing time for respawn animation/fade.
        private static readonly TimeSpan DeathStateDuration = TimeSpan.FromSeconds(4);

        // The grace period delay before automatically exiting the combat state after the target is lost.
        private static readonly TimeSpan CombatExitDelay = TimeSpan.FromSeconds(0.5);

        /// <summary>
        /// Gets a value indicating whether the player has just transitioned from the dead state to alive.
        /// This flag is typically consumed immediately after a check.
        /// </summary>
        public bool JustRespawned { get; private set; } = false;

        /// <summary>
        /// Gets a value indicating whether the player is currently considered dead.
        /// </summary>
        public bool IsPlayerDead { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the player is actively engaged in combat (recently saw a target).
        /// </summary>
        public bool IsInCombat { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the game is in a state where the aimbot should be allowed to function
        /// (e.g., not in a menu, not during a freeze time).
        /// </summary>
        public bool IsGameReadyForAimbot { get; set; }

        /// <summary>
        /// Updates the <see cref="IsInCombat"/> state based on whether a target is currently detected.
        /// Uses a delay before exiting combat to prevent rapid state changes.
        /// </summary>
        /// <param name="hasTarget">True if a target is currently detected, false otherwise.</param>
        public void UpdateCombatState(bool hasTarget)
        {
            if (hasTarget)
            {
                IsInCombat = true;
                _lastTargetSeenTimestamp = DateTime.UtcNow;
            }
            else
            {
                // Exit combat only after the target is lost for longer than the CombatExitDelay.
                if (IsInCombat && DateTime.UtcNow - _lastTargetSeenTimestamp > CombatExitDelay)
                {
                    IsInCombat = false;
                }
            }
        }

        /// <summary>
        /// Marks the player state as dead and records the timestamp of death, preventing redundant calls.
        /// </summary>
        public void SetPlayerAsDead()
        {
            if (!IsPlayerDead)
            {
                IsPlayerDead = true;
                _deathTimestamp = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Checks the elapsed time since death and updates the player's state from dead to alive (respawned)
        /// if the <see cref="DeathStateDuration"/> has passed. Sets <see cref="JustRespawned"/> to true.
        /// </summary>
        public void UpdatePlayerLiveness()
        {
            if (IsPlayerDead && DateTime.UtcNow - _deathTimestamp > DeathStateDuration)
            {
                IsPlayerDead = false;
                JustRespawned = true;
            }
        }

        /// <summary>
        /// Resets the <see cref="JustRespawned"/> flag after its state has been utilized by other systems
        /// (e.g., to trigger a one-time respawn action).
        /// </summary>
        public void ConsumeRespawnedFlag()
        {
            JustRespawned = false;
        }
    }
}