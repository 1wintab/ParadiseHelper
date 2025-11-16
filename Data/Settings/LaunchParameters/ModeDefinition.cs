using System.Drawing;

namespace Data.Settings.LaunchParameters
{
    /// <summary>
    /// A model representing the configuration and metadata for a specific launch mode.
    /// </summary>
    public class ModeDefinition
    {
        /// <summary>
        /// Gets or sets the unique identifier for the launch mode.
        /// </summary>
        public LaunchMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the user-friendly name of the mode.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a brief description of what the mode is used for.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the icon associated with this mode for UI display.
        /// </summary>
        public Image Icon { get; set; }

        /// <summary>
        /// Gets or sets the filename containing the Counter-Strike 2 (CS2) launch parameters for this mode.
        /// </summary>
        public string Cs2ConfigFile { get; set; }

        /// <summary>
        /// Gets or sets the filename containing the Steam launch parameters for this mode.
        /// </summary>
        public string SteamConfigFile { get; set; }

        /// <summary>
        /// Gets or sets the filename containing any extra command-line parameters for this mode.
        /// </summary>
        public string ExtraParamsFile { get; set; }
    }
}