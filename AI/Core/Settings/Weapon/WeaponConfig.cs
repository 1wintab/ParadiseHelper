namespace ParadiseHelper.AI.Core.Settings.Weapon
{
    /// <summary>
    /// Configuration settings for weapon-related actions, including aiming delays and 
    /// cooldown periods for repeated actions like shooting or buying.
    /// </summary>
    public class WeaponConfig
    {
        /// <summary>
        /// Gets or sets the delay (in milliseconds) the system waits after the aimbot successfully 
        /// targets an enemy before triggering the fire action. This helps simulate human reaction time.
        /// </summary>
        public int DelayAfterAimMs { get; set; }

        /// <summary>
        /// Gets or sets the cooldown period (in milliseconds) required between successive weapon 
        /// actions (e.g., shooting, trigger checks, or buy commands) to prevent spamming and maintain stability.
        /// </summary>
        public int ActionCooldownMs { get; set; }
    }
}