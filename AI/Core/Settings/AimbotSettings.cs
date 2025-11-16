namespace ParadiseHelper.AI.Core.Settings
{
    /// <summary>
    /// Static class containing global configuration parameters for the Aimbot functionality,
    /// organized into nested static classes for better structure and clarity.
    /// </summary>
    public static class AimbotSettings
    {
        /// <summary>
        /// Configuration parameters related to mouse sensitivity and Field of View (FOV).
        /// These values are critical for accurate aim calculation.
        /// </summary>
        public static class SensitivityAndFOV
        {
            /// <summary>
            /// Gets or sets the mouse sensitivity value configured within the game settings.
            /// </summary>
            public static float GameSensitivity { get; set; } = 2.0f;

            /// <summary>
            /// Gets or sets the horizontal sensitivity coefficient (m_yaw) typically used for 
            /// mouse movement calculations in Windows/CS2 environments.
            /// </summary>
            public static float m_yaw { get; set; } = 0.0220022f;

            /// <summary>
            /// Gets or sets the vertical sensitivity coefficient (m_pitch) typically used for 
            /// mouse movement calculations in Windows/CS2 environments.
            /// </summary>
            public static float m_pitch { get; set; } = 0.0220022f;

            /// <summary>
            /// Gets or sets the user's horizontal Field of View (FOV) in the game.
            /// </summary>
            public static float FieldOfView { get; set; } = 106f;

            /// <summary>
            /// Gets or sets the user's vertical Field of View (FOV) in the game.
            /// </summary>
            public static float VerticalFieldOfView { get; set; } = 74f;
        }

        /// <summary>
        /// Configuration factors for fine-tuning the aim position (offset corrections).
        /// These are typically used to account for model inaccuracies or body alignment.
        /// </summary>
        public static class AimCorrection
        {
            /// <summary>
            /// Gets or sets the correction factor applied to vertical aim when targeting the body.
            /// </summary>
            public static float VerticalAimCorrection { get; set; } = 5f;

            /// <summary>
            /// Gets or sets the correction factor applied to horizontal aim when targeting the body.
            /// </summary>
            public static float HorizontalAimCorrection { get; set; } = -0.26f;

            /// <summary>
            /// Gets or sets the vertical aim correction specifically for headshots.
            /// </summary>
            public static float HeadVerticalAimCorrection { get; set; } = 10f;

            /// <summary>
            /// Gets or sets the horizontal aim correction specifically for headshots.
            /// </summary>
            public static float HeadHorizontalAimCorrection { get; set; } = 0.0f;
        }

        /// <summary>
        /// Configuration for controlling the timing of shots and burst fire sequences.
        /// </summary>
        public static class TimingAndShots
        {
            /// <summary>
            /// Gets or sets the number of shots to be fired per single aiming sequence (burst size).
            /// </summary>
            public static int NumberOfShots { get; set; } = 1;

            /// <summary>
            /// Gets or sets the minimum delay (in milliseconds) after the aim is locked 
            /// before the first shot is fired, simulating reaction time.
            /// </summary>
            public static int DelayAfterAimMs { get; set; } = 15;

            /// <summary>
            /// Gets or sets the delay (in milliseconds) between multiple shots in a sequence,
            /// used for controlling burst fire rate.
            /// </summary>
            public static int ActionCooldownMs { get; set; } = 170;
        }
    }
}