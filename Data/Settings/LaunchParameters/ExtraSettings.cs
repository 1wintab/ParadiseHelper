namespace Data.Settings.LaunchParameters
{
    /// <summary>
    /// Private nested class to mirror the expected structure of extra launch parameters in the JSON configuration.
    /// This is used for deserialization.
    /// </summary>
    public class ExtraSettings
    {
        /// <summary>
        /// Flag to determine if the application should be launched in windowed mode (e.g., adds "-windowed").
        /// </summary>
        public bool IsWindowed { get; set; }

        /// <summary>
        /// The desired width of the application's resolution (e.g., adds "-w 1280").
        /// </summary>
        public int ResolutionWidth { get; set; } = 1280;

        /// <summary>
        /// The desired height of the application's resolution (e.g., adds "-h 720").
        /// </summary>
        public int ResolutionHeight { get; set; } = 720;
    }
}