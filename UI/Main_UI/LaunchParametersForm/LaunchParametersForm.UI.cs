using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Core;
using Data.Settings.LaunchParameters;
using ParadiseHelper.Data.Settings.LaunchParameters;
using ParadiseHelper.Tools.UITools;

namespace ParadiseHelper.UI.MainUI
{
    // This partial class contains methods related to UI setup and state management, including visual styling,
    // font application, control initialization, and helper functions that update the UI based on the current state or mode.
    public partial class LaunchParametersForm
    {
        // --- Initialization Helpers ---

        /// <summary>
        /// Sets up the mode switcher scrollbar initial state and event.
        /// </summary>
        private void InitializeModeSwitcher()
        {
            changeMode_vScrollBar.Minimum = 0;
            changeMode_vScrollBar.Maximum = 2;
            changeMode_vScrollBar.LargeChange = 1;

            // Set the initial value to the middle to allow scrolling up or down.
            changeMode_vScrollBar.Value = 1;
            changeMode_vScrollBar.Scroll += changeMode_vScrollBar_Scroll;

            // Update the UI to reflect the initial mode from ModeManager.
            UpdateModeUI();
        }

        /// <summary>
        /// Applies custom fonts to various controls on the form.
        /// </summary>
        private void ApplyFont()
        {
            label1.Font = FontLoader.VAGWorld(20);
            label2.Font = FontLoader.VAGWorld(20);
            label3.Font = FontLoader.VAGWorld(20);
            label5.Font = FontLoader.VAGWorld(20);
            label6.Font = FontLoader.VAGWorld(20);
            saveParams_Button.Font = FontLoader.VAGWorld(20);
            label7.Font = FontLoader.VAGWorld(18);
            params_richTextBox.Font = FontLoader.BIPs(12);
            WindowWidth_RichTextBox.Font = FontLoader.BIPs(9);
            WindowHeight_RichTextBox.Font = FontLoader.BIPs(9);
            changeMode_label.Font = FontLoader.VAGWorld(20);
        }

        /// <summary>
        /// Applies visual styles (rounding, borders) to UI elements.
        /// </summary>
        private void ApplyVisualStyle()
        {
            SetupFormStyle();
            StylePanels();
            StylePictureBoxes();
        }

        /// <summary>
        /// Configures the main form's visual style.
        /// </summary>
        private void SetupFormStyle()
        {
            this.ShowInTaskbar = false;
            UIEffects.ApplyFadeOut(this);
        }

        /// <summary>
        /// Applies rounded corners and borders to panels.
        /// </summary>
        private void StylePanels()
        {
            UIHelper.ApplyRoundedFrame(panel1, 20);
            UIHelper.ApplyRoundedFrame(panel2, 20);
            UIHelper.ApplyRoundedFrame(panel3, 5);
            UIHelper.ApplyRoundedFrame(panel4, 11);
            UIHelper.ApplyRoundedFrame(panel6, 20);
            UIHelper.ApplyRoundedFrame(steam_panel, 15);
            UIHelper.ApplyRoundedFrame(cs2_panel, 15);
        }

        /// <summary>
        /// Applies rounded corners and hover effects to PictureBoxes.
        /// </summary>
        private void StylePictureBoxes()
        {
            UIHelper.ApplyRoundedCorners(exit_pictureBox, 10);
            UIHelper.ApplyHoverWithRounded(resetParams_pictureBox, panel5, 5);
            UIHelper.ApplyHoverWithRounded(deleteParams_button, panel7, 5);
        }

        /// <summary>
        /// Sets up initial behaviors for text boxes and panels.
        /// </summary>
        private void SetupFormBehavior()
        {
            this.ShowInTaskbar = false;
            WindowMode_TransparentPanel.Visible = true;
            WidthAndHeight_TransparentPanel.Visible = true;

            params_richTextBox.TabStop = false;

            // Hook events for placeholder text logic.
            params_richTextBox.Enter += params_richTextBox_Enter;
            params_richTextBox.Leave += params_richTextBox_Leave;
            params_richTextBox.MouseDown += params_richTextBox_MouseDown;
            params_richTextBox.BackColor = Color.White;
            params_richTextBox.ReadOnly = true;
            params_richTextBox.Text = Placeholder_SelectApp;
            params_richTextBox.ForeColor = Color.Gray;

            // Register click events for app selection panels.
            RegisterClickToApp(steam_panel);
            RegisterClickToApp(cs2_panel);

            // Configure resolution text boxes for numeric input.
            WindowWidth_RichTextBox.Multiline = false;
            WindowHeight_RichTextBox.Multiline = false;
            WindowWidth_RichTextBox.KeyPress += NumericTextBox_KeyPress;
            WindowHeight_RichTextBox.KeyPress += NumericTextBox_KeyPress;
            WindowWidth_RichTextBox.Enter += NumericTextBox_Enter;
            WindowHeight_RichTextBox.Enter += NumericTextBox_Enter;
            WindowWidth_RichTextBox.Leave += NumericTextBox_Leave;
            WindowHeight_RichTextBox.Leave += NumericTextBox_Leave;
            WindowWidth_RichTextBox.TextChanged += NumericTextBox_TextChanged;
            WindowHeight_RichTextBox.TextChanged += NumericTextBox_TextChanged;
            WindowWidth_RichTextBox.MouseDown += ResolutionTextBox_MouseDown;
            WindowHeight_RichTextBox.MouseDown += ResolutionTextBox_MouseDown;

            this.FormClosing += LaunchParametersForm_FormClosing;
        }

        /// <summary>
        /// Ensures that the Steam launch parameters file contains required parameters.
        /// </summary>
        private void EnsureRequiredSteamParams()
        {
            string app = "steam";
            string filePath = Path.Combine(
                FilePaths.Standard.Settings.ParamsFoldersDirectory,
                $"{app}_launch.txt"
            );

            if (!File.Exists(filePath)) return;

            string content = File.ReadAllText(filePath);
            bool needsUpdate = false;

            // Add any missing required parameters.
            foreach (var param in steamRequiredParams)
            {
                if (content.IndexOf(param, StringComparison.OrdinalIgnoreCase) == -1)
                {
                    content += $" {param}";
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                File.WriteAllText(filePath, content.Trim());
            }
        }

        // --- UI Update Methods ---

        /// <summary>
        /// Updates the mode switcher UI (icon and text) to match the current mode.
        /// </summary>
        private void UpdateModeUI()
        {
            var currentMode = ModeManager.CurrentMode;
            changeMode_pictureBox.Image = currentMode.Icon;
            changeMode_label.Text = currentMode.Name;
        }

        /// <summary>
        /// Updates the background color of app selection panels to highlight the active one.
        /// </summary>
        /// <param name="selected">The panel to highlight.</param>
        private void UpdateAppHighlight(Panel selected)
        {
            Color selectedColor = Color.FromArgb(192, 255, 192); // Light green
            Color normalColor = Color.FromArgb(255, 255, 255); // White

            foreach (var panel in new[] { steam_panel, cs2_panel })
            {
                bool isSelected = panel == selected;
                Color target = isSelected ? selectedColor : normalColor;

                panel.BackColor = target;

                // Change background of all child controls within the panel.
                foreach (Control ctrl in panel.Controls)
                {
                    ctrl.BackColor = target;
                }
            }
        }

        /// <summary>
        /// Enables or disables the resolution and window mode controls.
        /// </summary>
        /// <param name="isEnabled">True to enable, false to disable.</param>
        private void UpdateResolutionControlsState(bool isEnabled)
        {
            WindowedMode_Checkbox.Enabled = isEnabled;
            WindowWidth_RichTextBox.Enabled = isEnabled;
            WindowHeight_RichTextBox.Enabled = isEnabled;

            // Show/hide the transparent panel that blocks clicks.
            WidthAndHeight_TransparentPanel.Visible = !isEnabled;

            // Change appearance based on availability.
            Color backColor = isEnabled ? Color.White : Color.FromArgb(235, 235, 235);
            Color foreColor = isEnabled ? Color.Black : Color.Gray;

            WindowWidth_RichTextBox.BackColor = backColor;
            WindowHeight_RichTextBox.BackColor = backColor;

            WindowWidth_RichTextBox.ForeColor = foreColor;
            WindowHeight_RichTextBox.ForeColor = foreColor;

            WindowedMode_Checkbox.ForeColor = isEnabled ? Color.Black : Color.DarkGray;
        }

        /// <summary>
        /// Updates the availability of UI controls based on the current mode and selected app.
        /// </summary>
        private void UpdateUIForCurrentMode()
        {
            bool isCS2 = selectedApp?.Equals("cs2", StringComparison.OrdinalIgnoreCase) == true;
            if (!isCS2)
            {
                // For Steam or null selection, all resolution controls are disabled.
                UpdateResolutionControlsState(false);
                return;
            }

            // --- Logic for CS2 ---
            if (ModeManager.CurrentMode.Mode == LaunchMode.AICore)
            {
                // AI Mode: Controls are disabled (values are forced).
                UpdateResolutionControlsState(false);
            }
            else // Default Mode
            {
                // Default Mode: Controls are enabled.
                UpdateResolutionControlsState(true);
            }
        }

        /// <summary>
        /// Saves the current values from the UI controls into the cached state object.
        /// </summary>
        /// <param name="app">The currently selected application.</param>
        private void UpdateStateFromUI(string app)
        {
            string stateKey = GetStateKey(app);
            if (string.IsNullOrEmpty(stateKey) || !_appStates.ContainsKey(stateKey)) return;

            var currentState = _appStates[stateKey];
            currentState.CurrentParamsText = params_richTextBox.Text;

            // Only update resolution/window state if the controls are enabled.
            // This prevents overwriting good cached data with disabled (placeholder) data.
            if (WindowWidth_RichTextBox.Enabled)
            {
                currentState.CurrentWidth = GetNumericValueFromRichTextBox(WindowWidth_RichTextBox);
            }

            if (WindowHeight_RichTextBox.Enabled)
            {
                currentState.CurrentHeight = GetNumericValueFromRichTextBox(WindowHeight_RichTextBox);
            }

            if (app.Equals("cs2", StringComparison.OrdinalIgnoreCase) && WindowedMode_Checkbox.Enabled)
            {
                currentState.CurrentWindowedState = WindowedMode_Checkbox.Checked;
            }
        }

        /// <summary>
        /// Updates all UI elements based on the state stored in the cache.
        /// </summary>
        /// <param name="app">The application state to load into the UI.</param>
        private void UpdateUIFromState(string app)
        {
            string stateKey = GetStateKey(app);
            if (string.IsNullOrEmpty(stateKey) || !_appStates.ContainsKey(stateKey)) return;

            var state = _appStates[stateKey];

            isInternalChange = true;
            // 1. Update resolution/window mode UI from state.
            SetupResolutionPlaceholders(state.CurrentWidth, state.CurrentHeight);
            WindowedMode_Checkbox.Checked = state.CurrentWindowedState;

            // 2. Format and display launch parameters.
            string normalizedParams = NormalizeInput(state.CurrentParamsText);
            if (string.IsNullOrWhiteSpace(normalizedParams))
            {
                params_richTextBox.Text = Placeholder_LaunchParams;
                params_richTextBox.ForeColor = Color.Gray;
            }
            else
            {
                // Format parameters to be one-per-line for readability.
                var result = new List<string>();
                var tokens = normalizedParams.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < tokens.Length; i++)
                {
                    string currentToken = tokens[i];
                    // If token is a flag (-/+) and is followed by a non-flag value,
                    // combine them (e.g., "-w 1280").
                    if ((currentToken.StartsWith("-") || currentToken.StartsWith("+")) &&
                        i + 1 < tokens.Length &&
                        !tokens[i + 1].StartsWith("-") && !tokens[i + 1].StartsWith("+"))
                    {
                        result.Add($"{currentToken} {tokens[i + 1]}");
                        i++; // Skip the value token.
                    }
                    else
                    {
                        result.Add(currentToken);
                    }
                }
                params_richTextBox.Text = string.Join(Environment.NewLine, result);
                params_richTextBox.ForeColor = Color.Black;
            }

            isInternalChange = false;
            params_richTextBox.ReadOnly = false;
        }

        // --- Helper & Utility Methods ---

        /// <summary>
        /// Gets the unique cache key for the given app and current mode.
        /// </summary>
        /// <param name="app">The application name ("cs2" or "steam").</param>
        /// <returns>A unique key (e.g., "cs2_AICore").</returns>
        private string GetStateKey(string app)
        {
            if (string.IsNullOrEmpty(app))
            {
                return null;
            }

            // e.g., "cs2_AICore" or "steam_Default"
            return $"{app}_{ModeManager.CurrentMode.Mode}";
        }

        /// <summary>
        /// Sets the text for a resolution RichTextBox, handling placeholders.
        /// </summary>
        private void UpdateResolutionTextBox(RichTextBox rtb, int maxVal, string valueFromLoad)
        {
            isInternalChange = true;
            // Case 1: Valid number loaded from state (e.g., for CS2).
            if (!string.IsNullOrEmpty(valueFromLoad) && int.TryParse(valueFromLoad, out int parsedValue) && parsedValue > 0)
            {
                rtb.Text = valueFromLoad;
                // Text color is set later by UpdateResolutionControlsState
            }
            // Case 2: No value (e.g., for Steam).
            else
            {
                rtb.Text = "---";
                rtb.ForeColor = Color.Gray;
            }
            rtb.SelectionAlignment = HorizontalAlignment.Center;
            isInternalChange = false;
        }

        /// <summary>
        /// Sets up both resolution text boxes with values from the state.
        /// </summary>
        private void SetupResolutionPlaceholders(string width, string height)
        {
            Size maxRes = GetMaxScreenResolution();
            UpdateResolutionTextBox(WindowWidth_RichTextBox, maxRes.Width, width);
            UpdateResolutionTextBox(WindowHeight_RichTextBox, maxRes.Height, height);
        }

        /// <summary>
        /// Gets the file path for the main launch parameters file based on app and mode.
        /// </summary>
        private string GetConfigFilePath(string app)
        {
            if (string.IsNullOrEmpty(app)) return null;
            string fileName = null;

            if (app.Equals("cs2", StringComparison.OrdinalIgnoreCase))
            {
                fileName = ModeManager.CurrentMode.Cs2ConfigFile;
            }
            else if (app.Equals("steam", StringComparison.OrdinalIgnoreCase))
            {
                fileName = ModeManager.CurrentMode.SteamConfigFile;
            }
            else
            {
                return null;
            }

            if (!string.IsNullOrEmpty(fileName))
            {
                return Path.Combine(FilePaths.Standard.Settings.ParamsFoldersDirectory, fileName);
            }

            return null;
        }

        /// <summary>
        /// Gets the file path for the extra JSON settings file based on the current mode.
        /// </summary>
        private string GetExtraParamsFilePath()
        {
            string fileName = ModeManager.CurrentMode.ExtraParamsFile;
            
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            return Path.Combine(FilePaths.Standard.Settings.ParamsFoldersDirectory, fileName);
        }

        /// <summary>
        /// Extracts legacy resolution parameters from raw text.
        /// </summary>
        /// <param name="rawContent">The raw text content.</param>
        /// <param name="width">Output: The extracted width.</param>
        /// <param name="height">Output: The extracted height.</param>
        /// <returns>The content string with resolution parameters removed.</returns>
        private string InternalExtractWindowParams(string rawContent, out string width, out string height)
        {
            width = string.Empty;
            height = string.Empty;
            
            if (string.IsNullOrWhiteSpace(rawContent)) return string.Empty;

            var tokens = rawContent.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var filteredTokens = new List<string>();

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                // Ignore all window mode flags.
                if (WindowModeFlags.Any(flag => token.Equals(flag, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                // Process resolution flags (-w, -h, etc.).
                if (ResolutionFlags.Any(flag => token.Equals(flag, StringComparison.OrdinalIgnoreCase)))
                {
                    // Check if a valid number follows the flag.
                    if (i + 1 < tokens.Length && int.TryParse(tokens[i + 1], out _))
                    {
                        if (token.EndsWith("w", StringComparison.OrdinalIgnoreCase) || token.EndsWith("width", StringComparison.OrdinalIgnoreCase))
                        {
                            width = tokens[i + 1];
                        }
                        else if (token.EndsWith("h", StringComparison.OrdinalIgnoreCase) || token.EndsWith("height", StringComparison.OrdinalIgnoreCase))
                        {
                            height = tokens[i + 1];
                        }
                        i++; // Skip the value token.
                    }

                    continue;
                }
                filteredTokens.Add(token);
            }

            return string.Join(" ", filteredTokens);
        }

        /// <summary>
        /// Gets a validated numeric value from a RichTextBox, ignoring placeholders.
        /// </summary>
        /// <returns>A string containing the number, or an empty string if invalid.</returns>
        private string GetNumericValueFromRichTextBox(RichTextBox rtb)
        {
            string text = rtb.Text.Trim();
            // Ignore placeholders like "---" or "max: 1920".
            if (rtb.ForeColor == Color.Gray || text.StartsWith("max:"))
            {
                return string.Empty;
            }

            if (int.TryParse(text, out int value) && value > 0)
            {
                return value.ToString();
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the primary screen's resolution.
        /// </summary>
        private static Size GetMaxScreenResolution()
        {
            return Screen.PrimaryScreen.Bounds.Size;
        }

        /// <summary>
        /// Registers the AppPanel_MouseDown event for a panel and all its child controls.
        /// </summary>
        private void RegisterClickToApp(Panel panel)
        {
            panel.MouseDown += AppPanel_MouseDown;
            foreach (Control c in panel.Controls)
            {
                c.MouseDown += AppPanel_MouseDown;
            }
        }

        /// <summary>
        /// Normalizes an input string by replacing all whitespace sequences with a single space and trimming.
        /// </summary>
        private string NormalizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            
            // Replace all whitespace (tabs, newlines, multi-spaces) with a single space.
            string normalizedSpacing = System.Text.RegularExpressions.Regex.Replace(input, @"\s+", " ");
            
            // Remove leading/trailing whitespace.
            return normalizedSpacing.Trim();
        }

        /// <summary>
        /// Checks if an action (like changing resolution) is allowed for the current mode/app
        /// and shows an alert to the user if it is not.
        /// </summary>
        /// <param name="action">The action being attempted.</param>
        /// <returns>True if the action is allowed, false otherwise.</returns>
        private bool CheckPermissionAndAlert(LaunchAction action)
        {
            bool isAiModeSelected = ModeManager.CurrentMode.Mode == LaunchMode.AICore;
            bool isCS2Selected = selectedApp?.Equals("cs2", StringComparison.OrdinalIgnoreCase) == true;
            bool isSteamSelected = selectedApp?.Equals("steam", StringComparison.OrdinalIgnoreCase) == true;

            string actionName = (action == LaunchAction.WindowedMode) ? "Windowed mode" : "Resolution change";

            // CHECK 1: Steam (blocked for both actions)
            if (isSteamSelected)
            {
                MessageBox.Show(
                    $"{actionName} is not available for Steam.",
                    "Unavailable Option",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return false;
            }

            // CHECK 2: AI Mode (blocked for both actions)
            if (isAiModeSelected)
            {
                string message = (action == LaunchAction.WindowedMode)
                    ? "Windowed mode option is not available in AI Mode."
                    : "You can't change width and height for windows in AI Mode.";
                MessageBox.Show(
                    message,
                    "Unavailable Option",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return false;
            }

            // CHECK 3: Window mode requires CS2 (Default mode is implied from CHECK 2)
            if (action == LaunchAction.WindowedMode && !isCS2Selected)
            {
                MessageBox.Show(
                    "Windowed mode option is only available when CS2 is selected.",
                    "Unavailable Option",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return false;
            }

            // If we are here, the action is allowed (CS2 in Default Mode).
            return isCS2Selected;
        }
    }
}