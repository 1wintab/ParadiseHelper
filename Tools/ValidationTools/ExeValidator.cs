using System.IO;
using ParadiseHelper.Data.Settings;

namespace ParadiseHelper.Validators
{
    /// <summary>
    /// Static utility class for validating the existence and accessibility
    /// of an executable file based on stored application settings.
    /// </summary>
    public static class ExeValidator
    {
        /// <summary>
        /// Checks if the executable file defined in the settings for a given application key actually exists on disk.
        /// </summary>
        /// <param name="appKey">The unique identifier key of the application settings to check.</param>
        /// <returns>
        /// <c>true</c> if the corresponding file exists and all path components are valid; 
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool IsExecutableValid(string appKey)
        {
            // 1. Retrieve application settings using the key.
            var app = SettingsManager.GetApp(appKey);

            // Check if application settings were found.
            if (app == null)
            {
                return false;
            }

            // 2. Check for empty or null directory path or executable name in the settings.
            if (string.IsNullOrWhiteSpace(app.Path) || string.IsNullOrWhiteSpace(app.ExeName))
            {
                return false;
            }

            // 3. Construct the full expected path to the executable.
            // Note: Path.GetDirectoryName extracts the directory part of the stored Path (which might contain the original executable filename).
            string directory = Path.GetDirectoryName(app.Path);

            // Return false if the directory path is somehow invalid or null after extraction.
            if (string.IsNullOrWhiteSpace(directory))
            {
                return false;
            }

            // Combine the directory and the executable name to form the complete file path.
            string expectedPath = Path.Combine(directory, app.ExeName);

            // 4. Verify that the file exists at the constructed path.
            return File.Exists(expectedPath);
        }
    }
}