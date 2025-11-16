using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ParadiseHelper.UI.MainUI
{
    /// <summary>
    /// Stores and manages the original and current state of application launch parameters
    /// for comparison and change detection.
    /// </summary>
    public class AppParamsState
    {
        // --- Static Fields for Parameter Parsing ---

        /// <summary>
        /// Flags related to window display mode (e.g., -windowed, -fullscreen).
        /// </summary>
        private static readonly string[] WindowModeFlags = { "-windowed", "-fullscreen", "-noborder" };

        /// <summary>
        /// Flags related to resolution (e.g., -w, -width, -h, -height).
        /// </summary>
        private static readonly string[] ResolutionFlags = { "-w", "+w", "-width", "+width", "-h", "+h", "-height", "+height" };

        // --- Original Values (Initial state, typically loaded from file) ---

        /// <summary>The original raw text of the launch parameters.</summary>
        public string OriginalContent { get; set; } = string.Empty;
        
        /// <summary>The original width value.</summary>
        public string OriginalWidth { get; set; } = string.Empty;
        
        /// <summary>The original height value.</summary>
        public string OriginalHeight { get; set; } = string.Empty;
        
        /// <summary>The original state of the windowed mode setting.</summary>
        public bool OriginalWindowedState { get; set; } = false;

        // --- Current Values (State after user modifications in the UI) ---

        /// <summary>The current raw text of the launch parameters from the UI.</summary>
        public string CurrentParamsText { get; set; } = string.Empty;
        
        /// <summary>The current width value from the UI.</summary>
        public string CurrentWidth { get; set; } = string.Empty;
        
        /// <summary>The current height value from the UI.</summary>
        public string CurrentHeight { get; set; } = string.Empty;
        
        /// <summary>The current state of the windowed mode setting from the UI.</summary>
        public bool CurrentWindowedState { get; set; } = false;

        /// <summary>
        /// Checks if any current value differs from its original counterpart.
        /// </summary>
        /// <returns>True if any parameter (text, resolution, or windowed state) has been changed.</returns>
        public bool HasChanges()
        {
            // 1. Compare simple fields: resolution and window mode.
            bool resolutionChanged = OriginalWidth != CurrentWidth || OriginalHeight != CurrentHeight;
            bool windowedStateChanged = OriginalWindowedState != CurrentWindowedState;

            // 2. Perform reliable comparison of the launch parameters string.
            // Normalize and clean both strings to ignore window/resolution flags before comparison,
            // as these are controlled by separate UI elements.
            string originalCleanedParams = NormalizeAndClean(OriginalContent ?? string.Empty);
            string currentCleanedParams = NormalizeAndClean(CurrentParamsText ?? string.Empty);

            bool paramsChanged = originalCleanedParams != currentCleanedParams;

            // Return true if AT LEAST ONE field has changed.
            return paramsChanged || resolutionChanged || windowedStateChanged;
        }

        /// <summary>
        /// Updates the "Original" state with current values.
        /// This method should be called after a successful save operation.
        /// </summary>
        public void UpdateOriginalState()
        {
            // Clean up new lines from the current text and store it as the new original content.
            var lines = (CurrentParamsText ?? string.Empty)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            OriginalContent = string.Join(" ", lines);

            OriginalWidth = CurrentWidth;
            OriginalHeight = CurrentHeight;
            OriginalWindowedState = CurrentWindowedState;
        }

        /// <summary>
        /// Normalizes the parameter string (removes excess spaces) and filters out flags
        /// that are controlled separately by the UI (Window Mode, Resolution).
        /// This ensures a clean comparison of only user-defined, custom parameters.
        /// </summary>
        /// <param name="rawContent">The raw launch parameter string.</param>
        /// <returns>A clean string containing only non-UI-controlled parameters.</returns>
        private string NormalizeAndClean(string rawContent)
        {
            if (string.IsNullOrWhiteSpace(rawContent)) return string.Empty;

            // Step 1: Replace multiple spaces with a single space and trim the result for normalization.
            string normalized = Regex.Replace(rawContent, @"\s+", " ").Trim();

            var tokens = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var filteredTokens = new List<string>();

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i].Trim();

                // Step 2: Skip window mode flags controlled by the UI.
                if (WindowModeFlags.Any(flag => token.Equals(flag, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                // Step 3: Skip resolution flags and their associated values.
                if (ResolutionFlags.Any(flag => token.Equals(flag, StringComparison.OrdinalIgnoreCase)))
                {
                    // If the flag is followed by a numeric value (e.g., "-w 1920"), skip the value as well.
                    if (i + 1 < tokens.Length && int.TryParse(tokens[i + 1], out _))
                    {
                        i++; // Skip the next token (the resolution value).
                    }
                    continue;
                }

                filteredTokens.Add(token);
            }

            // Step 4: Reassemble the filtered, clean parameters.
            return string.Join(" ", filteredTokens);
        }
    }
}