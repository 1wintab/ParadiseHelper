using Core;
using System;
using System.IO;
using System.Text.Json;

namespace ParadiseHelper.Data.Settings
{
    /// <summary>
    /// Static class responsible for managing the persistent storage (saving and loading) of 
    /// application settings, including defined application paths.
    /// </summary>
    public static class SettingsManager
    {
        // Defines the full path to the main settings file.
        private static string SettingsFile => Path.Combine(FilePaths.Standard.Settings.ConfigDirectory, "AppLocations.json");

        /// <summary>
        /// Saves the current <c>LauncherSettings</c> object to the designated configuration file path.
        /// </summary>
        /// <param name="settings">The settings object to serialize and save.</param>
        public static void Save(LauncherSettings settings)
        {
            // Serialize the settings object to a JSON string with indentation for readability.
            string json = JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            );

            // Ensure the configuration folder structure exists before writing the file.
            Directory.CreateDirectory(FilePaths.Standard.Settings.ConfigDirectory);
            File.WriteAllText(SettingsFile, json);
        }

        /// <summary>
        /// Loads the application settings from the configuration file.
        /// If the file does not exist or fails to load, default settings are created, saved, and returned.
        /// </summary>
        /// <returns>A populated <c>LauncherSettings</c> object.</returns>
        public static LauncherSettings Load()
        {
            // Check if the configuration file exists.
            if (!File.Exists(SettingsFile))
            {
                // If not found, create and save default settings.
                var defaultSettings = new LauncherSettings();
                Save(defaultSettings);

                return defaultSettings;
            }

            try
            {
                // Read the JSON file content.
                string json = File.ReadAllText(SettingsFile);

                // Deserialize the JSON string. If deserialization fails, return new default settings.
                return JsonSerializer.Deserialize<LauncherSettings>(json) ?? new LauncherSettings();
            }
            catch
            {
                // Return default settings on any file read or deserialization error.
                return new LauncherSettings();
            }
        }

        /// <summary>
        /// Sets or updates the configuration for a specific application key and saves the changes.
        /// </summary>
        /// <param name="key">The unique identifier key for the application (e.g., "CS2", "OBS").</param>
        /// <param name="app">The <c>AppConfig</c> object containing the path and executable name.</param>
        public static void SetApp(string key, AppConfig app)
        {
            // Load current settings.
            var settings = Load();
            // Update the Apps dictionary with the new/modified configuration.
            settings.Apps[key] = app;
            // Save the changes back to the file.
            Save(settings);
        }

        /// <summary>
        /// Retrieves the configuration for a specific application key.
        /// Performs path validation and automatically cleans the configuration file
        /// if the path is no longer valid on the current system (self-cleaning).
        /// </summary>
        /// <param name="key">The unique identifier key for the application.</param>
        /// <returns>The <c>AppConfig</c> object if the path is valid, otherwise <c>null</c>.</returns>
        public static AppConfig GetApp(string key)
        {
            // Load current settings.
            var settings = Load();

            // Try to get the application configuration from the dictionary.
            if (settings.Apps.TryGetValue(key, out var app) && app != null && !string.IsNullOrWhiteSpace(app.Path))
            {
                // Path validation check.
                if (IsValidPath(app.Path, app.ExeName))
                {
                    return app;
                }
                else
                {
                    // If the file/directory does not exist on this PC, clean the setting.
                    RemoveApp(key);

                    // Return null to signal the caller that the path is invalid/cleaned.
                    return null;
                }
            }

            // Return null if key not found or path was empty initially.
            return null;
        }

        /// <summary>
        /// Verifies if the path stored in the configuration is valid and contains the expected executable.
        /// </summary>
        /// <param name="path">The path stored in the configuration (can be a directory or file).</param>
        /// <param name="exeName">The expected executable file name (e.g., "csgo.exe").</param>
        /// <returns><c>true</c> if the path is valid and the executable exists; otherwise, <c>false</c>.</returns>
        private static bool IsValidPath(string path, string exeName)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            // 1. Check if the path points directly to an executable file (e.g., cs2.exe).
            if (File.Exists(path))
            {
                // Ensure the file name matches the expected executable name.
                return Path.GetFileName(path).Equals(exeName, StringComparison.OrdinalIgnoreCase);
            }

            // 2. Check if the path points to a directory containing the executable (e.g., OBS folder).
            if (Directory.Exists(path))
            {
                // Combine the directory path with the expected executable name.
                string combinedPath = Path.Combine(path, exeName);
                
                return File.Exists(combinedPath);
            }

            return false;
        }

        /// <summary>
        /// Removes a specific application configuration entry from the settings and saves the result.
        /// </summary>
        /// <param name="key">The unique identifier key of the application to remove.</param>
        public static void RemoveApp(string key)
        {
            // Load current settings.
            var settings = Load();

            if (settings.Apps.ContainsKey(key))
            {
                // Remove the key and save the changes.
                settings.Apps.Remove(key);
                Save(settings);
            }
        }

        /// <summary>
        /// Checks if a configuration entry exists for the given application key.
        /// </summary>
        /// <param name="key">The unique identifier key for the application.</param>
        /// <returns><c>true</c> if the key exists in the settings; otherwise, <c>false</c>.</returns>
        public static bool ContainsApp(string key)
        {
            // Load current settings.
            var settings = Load();

            // Check if a custom application entry exists.
            return settings.Apps.ContainsKey(key);
        }

        /// <summary>
        /// Retrieves the path for a known application key (SteamPath, CS2Path, ObsPath) by delegating 
        /// to <c>GetApp</c> and extracting the <c>Path</c> property.
        /// </summary>
        /// <param name="key">The settings key containing "Path" suffix (e.g., "SteamPath").</param>
        /// <returns>The validated path string, or <c>null</c> if not found or invalid.</returns>
        public static string Get(string key)
        {
            // Load current settings (redundant since GetApp loads, but kept for clarity/pattern consistency).
            var settings = Load();

            // Attempt to retrieve the AppConfig, which also performs path validation/cleaning.
            var appConfig = GetApp(key);

            // Check if a valid AppConfig was returned and the key is one of the expected path keys.
            if (appConfig != null && (key == "SteamPath" || key == "CS2Path" || key == "ObsPath"))
            {
                // Remove the "Path" suffix to get the key used in the Apps dictionary (e.g., "Steam").
                string appKey = key.Replace("Path", "");
                
                // Retrieve the actual AppConfig using the derived key.
                var app = GetApp(appKey);

                // Return the validated path.
                return app?.Path;
            }
            return null;
        }
    }
}