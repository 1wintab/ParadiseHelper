using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using System.Collections.Generic;
using ParadiseHelper.Data.Settings.LaunchParameters;
using Data.Settings.LaunchParameters;

namespace ParadiseHelper.UI.MainUI
{
    // This partial class contains the core business logic for managing launch parameters, including loading from files,
    // saving changes, sanitizing input, enforcing required parameters (e.g., for AI mode), and handling the extra JSON settings.
    public partial class LaunchParametersForm
    {
        // --- Main Logic Methods ---

        /// <summary>
        /// Applies mode-specific logic (e.g., AI params) to a cached application state.
        /// </summary>
        /// <param name="state">The state object to modify.</param>
        /// <param name="app">The application name (e.g., "cs2").</param>
        private void ApplyModeToState(AppParamsState state, string app)
        {
            if (state == null || !app.Equals("cs2", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            bool isAiMode = ModeManager.CurrentMode.Mode == LaunchMode.AICore;

            // Normalize params to a single line for easier parsing.
            string normalizedParams = System.Text.RegularExpressions.Regex.Replace(state.CurrentParamsText ?? "", @"\s+", " ").Trim();
            var paramList = normalizedParams.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            var cleanedParamList = new List<string>();
            var aiParamTokens = aiModeRequiredParams[0].Split(' '); // e.g., ["+exec", "config.cfg"]

            // Reliably find and skip the old AI parameter (+exec config.cfg)
            for (int i = 0; i < paramList.Count; i++)
            {
                if (i + 1 < paramList.Count &&
                    paramList[i].Equals(aiParamTokens[0], StringComparison.OrdinalIgnoreCase) &&
                    paramList[i + 1].Equals(aiParamTokens[1], StringComparison.OrdinalIgnoreCase))
                {
                    i++; // Skip both "+exec" and "config.cfg"
                }
                else
                {
                    cleanedParamList.Add(paramList[i]);
                }
            }

            if (isAiMode)
            {
                // Add the current AI parameter and force resolution/window mode.
                cleanedParamList.AddRange(aiModeRequiredParams);
                state.CurrentWidth = "1280";
                state.CurrentHeight = "720";
                state.CurrentWindowedState = true;
            }

            // Update the state with the cleaned/modified list.
            state.CurrentParamsText = string.Join(" ", cleanedParamList);
        }

        /// <summary>
        /// Loads the launch parameters for a specific application and the current mode.
        /// </summary>
        /// <param name="app">The application to load ("cs2" or "steam").</param>
        public void LoadLaunchParams(string app)
        {
            this.ActiveControl = null;
            selectedApp = app;
            string stateKey = GetStateKey(app); // Get unique key (e.g., "cs2_AICore")

            // Load state from file only if it's not already in the cache.
            if (!_appStates.ContainsKey(stateKey))
            {
                var newState = new AppParamsState();

                // 1. Load main parameters file (e.g., cs2_ai_launch.txt)
                string path = GetConfigFilePath(app);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    newState.OriginalContent = File.ReadAllText(path);

                    // Parse legacy resolution params from this file.
                    InternalExtractWindowParams(
                        newState.OriginalContent,
                        out string extractedWidth,
                        out string extractedHeight
                    );
                    newState.OriginalWidth = extractedWidth;
                    newState.OriginalHeight = extractedHeight;
                }

                // 2. Load extra settings JSON (for CS2 only)
                if (app.Equals("cs2", StringComparison.OrdinalIgnoreCase))
                {
                    string extraPath = GetExtraParamsFilePath();
                    if (!string.IsNullOrEmpty(extraPath) && File.Exists(extraPath))
                    {
                        try
                        {
                            var settings = JsonSerializer.Deserialize<ExtraSettings>(File.ReadAllText(extraPath));
                            if (settings != null)
                            {
                                // These values override any legacy params.
                                newState.OriginalWindowedState = settings.IsWindowed;
                                newState.OriginalWidth = settings.ResolutionWidth.ToString();
                                newState.OriginalHeight = settings.ResolutionHeight.ToString();
                            }
                        }
                        catch { /* Ignore deserialization errors */ }
                    }
                }

                // Initialize current values as a copy of the original.
                newState.CurrentParamsText = newState.OriginalContent;
                newState.CurrentWidth = newState.OriginalWidth;
                newState.CurrentHeight = newState.OriginalHeight;
                newState.CurrentWindowedState = newState.OriginalWindowedState;

                // Save the newly loaded state to the cache.
                _appStates[stateKey] = newState;
            }

            // Get the state from the cache.
            var state = _appStates[stateKey];

            // Apply mode-specific logic (e.g., add AI params).
            ApplyModeToState(state, app);

            // Update the UI based on the final state.
            UpdateUIFromState(app);

            // Enable/disable controls based on the mode.
            UpdateUIForCurrentMode();
        }

        /// <summary>
        /// Writes the specified application state to its configuration file(s).
        /// </summary>
        /// <param name="app">The application name ("cs2" or "steam").</param>
        /// <param name="state">The state object to save.</param>
        /// <returns>True on success, false on failure.</returns>
        private bool SaveParams(string app, AppParamsState state)
        {
            string filePath = GetConfigFilePath(app);
            if (string.IsNullOrEmpty(filePath)) return false;

            try
            {
                // 1. Prepare and enforce required/blacklisted parameters.
                List<string> finalParams = SanitizeAndEnforceParameters(app, state.CurrentParamsText);

                // 2. Write the main launch parameters file (e.g., cs2_launch.txt).
                string joined = string.Join(" ", finalParams);
                File.WriteAllText(filePath, joined);

                // 3. Write extra CS2 settings (window/resolution) if applicable.
                if (app.Equals("cs2", StringComparison.OrdinalIgnoreCase))
                {
                    SaveExtraCS2Settings(state);

                    // 4. Install the mandatory AI config file if in AI mode.
                    if (ModeManager.CurrentMode.Mode == LaunchMode.AICore)
                    {
                        AutoAddtionCfgCS2.AddGlobalCS2Cfg();
                    }
                }

                // Update the "original" state to reflect the saved data.
                state.UpdateOriginalState();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save launch parameters: {ex.Message}", "Error");
                return false;
            }
        }

        /// <summary>
        /// Cleans raw parameter text: removes resolution/window flags and enforces required/blacklisted params.
        /// </summary>
        /// <param name="app">The application name ("cs2" or "steam").</param>
        /// <param name="rawParamsText">The raw text from the text box.</param>
        /// <returns>A list of cleaned and enforced parameters.</returns>
        private List<string> SanitizeAndEnforceParameters(string app, string rawParamsText)
        {
            // Split by lines, trim, and filter out any resolution/window flags.
            var sanitizedLines = (rawParamsText ?? "")
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line) &&
                               !WindowModeFlags.Any(flag => line.StartsWith(flag, StringComparison.OrdinalIgnoreCase)) &&
                               !ResolutionFlags.Any(flag => line.StartsWith(flag, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // Add required Steam parameters if not present.
            if (app.Equals("steam", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string required in steamRequiredParams)
                {
                    if (!sanitizedLines.Contains(required, StringComparer.OrdinalIgnoreCase))
                    {
                        sanitizedLines.Add(required);
                    }
                }
            }

            // Add required AI Core parameters if in that mode.
            if (app.Equals("cs2") && ModeManager.CurrentMode.Mode == LaunchMode.AICore)
            {
                foreach (string required in aiModeRequiredParams)
                {
                    if (!sanitizedLines.Contains(required, StringComparer.OrdinalIgnoreCase))
                    {
                        sanitizedLines.Add(required);
                    }
                }
            }

            // Final cleanup: remove any blacklisted parameters.
            sanitizedLines.RemoveAll(p => blacklistedParams.Any(b => p.StartsWith(b, StringComparison.OrdinalIgnoreCase)));

            return sanitizedLines;
        }

        /// <summary>
        /// Saves the windowed mode and resolution settings to the separate CS2 JSON file.
        /// </summary>
        /// <param name="state">The state object containing the settings to save.</param>
        private void SaveExtraCS2Settings(AppParamsState state)
        {
            string extraPath = GetExtraParamsFilePath();
            if (string.IsNullOrEmpty(extraPath)) return;

            try
            {
                var settings = new ExtraSettings
                {
                    IsWindowed = state.CurrentWindowedState,
                    // Use safe parsing with fallback defaults.
                    ResolutionWidth = int.TryParse(state.CurrentWidth, out int w) ? w : 1280,
                    ResolutionHeight = int.TryParse(state.CurrentHeight, out int h) ? h : 720
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);

                // Ensure the directory exists before writing.
                Directory.CreateDirectory(Path.GetDirectoryName(extraPath));
                File.WriteAllText(extraPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save extra settings: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Saves all states in the cache that have registered changes.
        /// </summary>
        /// <returns>True if all saves were successful, false otherwise.</returns>
        private bool SaveChanges()
        {
            bool allSaved = true;
            foreach (var pair in _appStates)
            {
                if (pair.Value.HasChanges())
                {
                    // Key is "cs2_AICore", so split to get "cs2".
                    string app = pair.Key.Split('_').FirstOrDefault();
                    if (string.IsNullOrEmpty(app)) continue;

                    // Try to save the state to its file.
                    if (!SaveParams(app, pair.Value))
                    {
                        allSaved = false;
                        MessageBox.Show($"Failed to save parameters for {pair.Key}.", "Save Error");
                    }
                }
            }
            return allSaved;
        }
    }
}