using System.Collections.Generic;

namespace ParadiseHelper.Data.Settings
{
    /// <summary>
    /// Represents the overall configuration structure for the launcher application.
    /// </summary>
    /// <remarks>
    /// This class acts as the root container for all managed application configurations.
    /// </remarks>
    public class LauncherSettings
    {
        /// <summary>
        /// Gets or sets a dictionary of custom applications or game launchers configured by the user.
        /// </summary>
        // The dictionary key is expected to be a unique ID or a friendly name for the application.
        public Dictionary<string, AppConfig> Apps { get; set; } = new Dictionary<string, AppConfig>();
    }
}