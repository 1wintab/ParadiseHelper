using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Core;
using ParadiseHelper.Data.Settings.LaunchParameters;
using ParadiseHelper.SteamLogAccounts.SteamEnv;
using System.Diagnostics;

namespace Data.Settings.LaunchParameters
{
    /// <summary>
    /// Utility class for managing and applying video configuration settings for Counter-Strike 2 (CS2) 
    /// by merging preset values from a template file with account-specific extra launch parameters.
    /// </summary>
    public class VideoSettingsCS2
    {
        // The name of the game's target video configuration file.
        private const string VIDEO_CONFIG_FILENAME = "cs2_video.txt";

        // The name of the template file used for presets.
        private const string PRESET_FILENAME = "cs2_video.txt";  
        
        /// <summary>
        /// Parameters that should not be overwritten in the user's existing config file,
        /// typically due to being machine-specific or managed by the game engine itself.
        /// </summary>
        private static readonly HashSet<string> ExcludedKeys = new HashSet<string>
        {
            "Version", "VendorID", "DeviceID", "Autoconfig",
            "setting.refreshrate_numerator", "setting.refreshrate_denominator"
        };

        /// <summary>
        /// Orchestrates the process of loading, modifying, and writing the video configuration file 
        /// (<c>cs2_video.txt</c>) for a specific Steam account and launch mode.
        /// </summary>
        /// <param name="accountId">The Steam account ID (32-bit representation) to which the settings should be applied.</param>
        /// <param name="launchMode">The launch mode that defines the associated configuration file with extra parameters.</param>
        /// <exception cref="FileNotFoundException">Thrown when the video preset file is not found.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the CS2 configuration path cannot be determined for the account ID.</exception>
        public static void ApplySettings(uint accountId, LaunchMode launchMode)
        {
            // 1. Load additional parameters (resolution, window mode) 
            ExtraSettings extraSettings = LoadExtraSettings(launchMode);

            // 2. Load the preset file (cs2_video.txt) 
            string presetPath = Path.Combine(FilePaths.Standard.Settings.GamePresetsDirectory, PRESET_FILENAME);
            
            if (!File.Exists(presetPath))
            {
                throw new FileNotFoundException($"Video preset file not found at: {presetPath}");
            }
            
            Dictionary<string, string> presetSettings = ParseVideoConfig(presetPath);

            // 3. Modify preset settings based on extra_params 
            UpdatePresetWithExtraSettings(presetSettings, extraSettings);

            // 4. Get the path to the target video.txt file
            string targetPath = GetVideoConfigPath(accountId);

            // 5. Apply changes
            if (!File.Exists(targetPath))
            {
                // If the file doesn't exist, create it from our modified preset
                string dir = Path.GetDirectoryName(targetPath);
                
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                WriteVideoConfig(targetPath, presetSettings);
            }
            else
            {
                // If it exists, merge the settings
                Dictionary<string, string> targetSettings = ParseVideoConfig(targetPath);
                
                foreach (var kvp in presetSettings)
                {
                    // Update only allowed parameters
                    if (!ExcludedKeys.Contains(kvp.Key)) 
                    {
                        targetSettings[kvp.Key] = kvp.Value;
                    }
                }
                
                WriteVideoConfig(targetPath, targetSettings);
            }
        }

        /// <summary>
        /// Determines the full file path to the target <c>cs2_video.txt</c> configuration file for a given Steam account.
        /// </summary>
        /// <param name="accountId">The Steam account ID.</param>
        /// <returns>The full path to the video configuration file.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the configuration path cannot be resolved.</exception>
        private static string GetVideoConfigPath(uint accountId)
        {
            string cs2CfgPath = SteamUserDataManager.GetCS2CfgPath(accountId);
            
            if (string.IsNullOrWhiteSpace(cs2CfgPath))
            {
                throw new InvalidOperationException($"CS2 configuration path could not be determined for account ID: {accountId}");
            }
            
            return Path.Combine(cs2CfgPath, VIDEO_CONFIG_FILENAME);
        }

        /// <summary>
        /// Loads resolution and window mode settings from a JSON file associated with the specified launch mode.
        /// </summary>
        /// <param name="mode">The <c>LaunchMode</c> to look up the associated extra parameters file for.</param>
        /// <returns>An <c>ExtraSettings</c> object containing the loaded settings, or defaults if the file is missing or invalid.</returns>
        private static ExtraSettings LoadExtraSettings(LaunchMode mode)
        {
            var modeDefinition = ModeManager.AvailableModes.FirstOrDefault(m => m.Mode == mode);
            if (modeDefinition == null || string.IsNullOrEmpty(modeDefinition.ExtraParamsFile))
            {
                return new ExtraSettings(); // Return defaults if mode is not found
            }

            string filePath = Path.Combine(FilePaths.Standard.Settings.ParamsFoldersDirectory, modeDefinition.ExtraParamsFile);
            if (!File.Exists(filePath))
            {
                return new ExtraSettings();
            }

            try
            {
                string json = File.ReadAllText(filePath);
                
                // Uses System.Text.Json for deserialization
                return JsonSerializer.Deserialize<ExtraSettings>(json) ?? new ExtraSettings();
            }
            catch (Exception ex)
            {
                // Log the error and return defaults on parsing error
                Debug.WriteLine($"Error loading extra settings from {filePath}: {ex.Message}");
               
                return new ExtraSettings();
            }
        }

        /// <summary>
        /// Modifies the loaded video settings dictionary based on the values provided in the <c>ExtraSettings</c> object,
        /// focusing on resolution and fullscreen/windowed settings.
        /// </summary>
        /// <param name="preset">The dictionary of preset video settings to be updated.</param>
        /// <param name="extra">The <c>ExtraSettings</c> containing the desired resolution and window mode.</param>
        private static void UpdatePresetWithExtraSettings(Dictionary<string, string> preset, ExtraSettings extra)
        {
            // Set resolution settings
            preset["setting.defaultres"] = extra.ResolutionWidth.ToString();
            preset["setting.defaultresheight"] = extra.ResolutionHeight.ToString();

            // Apply fullscreen/windowed settings
            if (extra.IsWindowed)
            {
                preset["setting.fullscreen"] = "0";      // 0 for windowed
                preset["setting.coop_fullscreen"] = "0"; // 0 for windowed in coop mode
                preset["setting.nowindowborder"] = "0";  // 0 for bordered window
            }
            else // Fullscreen mode
            {
                preset["setting.fullscreen"] = "1";      // 1 for fullscreen
                preset["setting.coop_fullscreen"] = "1"; // 1 for fullscreen in coop mode
                preset["setting.nowindowborder"] = "1";  // 1 for borderless fullscreen (common preference)
            }
        }

        /// <summary>
        /// Reads and parses a CS2 video configuration file, extracting key-value pairs using a regular expression.
        /// The expected format is Valve's configuration format: <c>"key" "value"</c>.
        /// </summary>
        /// <param name="filePath">The path to the video configuration file (<c>cs2_video.txt</c>).</param>
        /// <returns>A dictionary of key-value settings.</returns>
        private static Dictionary<string, string> ParseVideoConfig(string filePath)
        {
            var settings = new Dictionary<string, string>();
            
            // Regex to match lines in the format: "key" "value"
            var regex = new Regex(@"^\s*""([^""]+)""\s+""([^""]+)""\s*$");

            foreach (var line in File.ReadAllLines(filePath))
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    settings[match.Groups[1].Value] = match.Groups[2].Value;
                }
            }
            return settings;
        }

        /// <summary>
        /// Writes the provided dictionary of settings back into the standard CS2 video configuration file format.
        /// </summary>
        /// <param name="filePath">The path where the configuration file should be saved.</param>
        /// <param name="settings">The dictionary containing the video settings key-value pairs.</param>
        private static void WriteVideoConfig(string filePath, Dictionary<string, string> settings)
        {
            var sb = new StringBuilder();
            sb.AppendLine("\"video.cfg\"");
            sb.AppendLine("{");

            foreach (var kvp in settings)
            {
                // Writes the line in the format: "key"	"value" (using a tab for separation)
                sb.AppendLine($"\t\"{kvp.Key}\"\t\t\"{kvp.Value}\"");
            }

            sb.AppendLine("}");
            File.WriteAllText(filePath, sb.ToString());
        }
    }
}