namespace ParadiseHelper.Data.Settings
{
    /// <summary>
    /// Configuration model for an external application (e.g., a game or launcher).
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Gets or sets the user-friendly name of the application (e.g., "Steam" or "Counter-Strike 2").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the executable file name (e.g., "steam.exe").
        /// </summary>
        public string ExeName { get; set; }

        /// <summary>
        /// Gets or sets the full file path to the executable (.exe) file.
        /// (Renamed from 'Path' to 'ExecutablePath' to avoid conflicts with System.IO.Path).
        /// </summary>
        public string Path { get; set; }
    }
}