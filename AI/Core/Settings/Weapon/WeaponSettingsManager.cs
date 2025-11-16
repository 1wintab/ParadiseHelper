using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace ParadiseHelper.AI.Core.Settings.Weapon
{
    /// <summary>
    /// Static class for loading, storing, and accessing all weapon configuration settings from a JSON file.
    /// It provides a centralized way to retrieve cooldown and delay values necessary for the aiming logic.
    /// </summary>
    public static class WeaponSettingsManager
    {
        // Use a case-insensitive dictionary to ensure weapon lookups work regardless of casing in the input weapon name or JSON key.
        private static Dictionary<string, WeaponConfig> _settings;

        /// <summary>
        /// Loads weapon settings from a JSON file into the static cache.
        /// The JSON file is expected to be a dictionary where the key is the weapon name (string)
        /// and the value is the <see cref="WeaponConfig"/> object.
        /// </summary>
        /// <param name="filePath">The absolute path to the JSON settings file.</param>
        /// <returns>The loaded dictionary of settings, or null on failure (file not found or deserialization error).</returns>
        public static Dictionary<string, WeaponConfig> LoadSettings(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                var jsonString = File.ReadAllText(filePath);

                // 1. Deserialize into a temporary dictionary first.
                var tempSettings = JsonSerializer.Deserialize<Dictionary<string, WeaponConfig>>(jsonString);

                if (tempSettings == null)
                {
                    return null;
                }

                // 2. Initialize the main settings dictionary using a case-insensitive comparer (OrdinalIgnoreCase).
                // This makes weapon lookup resilient to different casing in the configuration file keys.
                _settings = new Dictionary<string, WeaponConfig>(StringComparer.OrdinalIgnoreCase);

                // 3. Populate the case-insensitive dictionary. We also normalize keys by trimming whitespace
                // just in case the JSON keys have accidental leading/trailing spaces.
                foreach (var kvp in tempSettings)
                {
                    // Trimming the key from the JSON file for robustness
                    _settings[kvp.Key.Trim()] = kvp.Value;
                }

                return _settings;
            }
            catch (Exception)
            {
                // Logging any exception during file reading or deserialization would be recommended here.
                return null;
            }
        }

        /// <summary>
        /// Retrieves the configuration for a specific weapon by its name.
        /// </summary>
        /// <param name="weaponName">The name of the weapon (e.g., "AK-47", "Unknown").</param>
        /// <returns>
        /// The <see cref="WeaponConfig"/> for the specified weapon.
        /// If the specific weapon name is not found, it attempts to return the "Unknown" configuration as a fallback.
        /// Returns null if the settings have not been loaded or if neither the specific weapon nor "Unknown" is defined.
        /// </returns>
        public static WeaponConfig GetSettings(string weaponName)
        {
            // If settings haven't been loaded, we can't look up anything.
            if (_settings == null || _settings.Count == 0) return null;

            // Normalize the weapon name by removing leading/trailing whitespace to ensure accurate lookup.
            string normalizedWeaponName = weaponName.Trim();

            // Try to find the specific weapon configuration using the normalized name.
            if (_settings.TryGetValue(normalizedWeaponName, out var config)) return config;

            // Fall back to the "Unknown" default configuration.
            return _settings.TryGetValue("Unknown", out var defaultConfig) ? defaultConfig : null;
        }
    }
}