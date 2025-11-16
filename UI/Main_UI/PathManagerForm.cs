using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using ParadiseHelper.Data.Settings;
using ParadiseHelper.Tools.UITools;

namespace ParadiseHelper.UI.MainUI
{
    /// <summary>
    /// A form dedicated to managing and persisting external application paths (Steam, CS2, OBS)
    /// used by the ParadiseHelper application.
    /// It handles path selection, display of placeholders, saving, and resetting of configuration paths.
    /// </summary>
    public partial class PathManagerForm : SmartForm
    {
        /// <summary>
        /// Placeholder text displayed in the path textboxes when no path is currently set.
        /// </summary>
        private const string PathSelectPlaceholder = " Click 📂 to select path";

        /// <summary>
        /// Stores the original Steam application path loaded from settings for change detection.
        /// </summary>
        private string _originalSteamPath;
       
        /// <summary>
        /// Stores the original Counter-Strike 2 (CS2) application path loaded from settings for change detection.
        /// </summary>
        private string _originalCS2Path;
        
        /// <summary>
        /// Stores the original OBS application path loaded from settings for change detection.
        /// </summary>
        private string _originalObsPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathManagerForm"/> class.
        /// Loads saved settings, applies custom fonts and styles, and sets up form behaviors.
        /// </summary>
        public PathManagerForm()
        {
            InitializeComponent();

            ApplyFont();
            ApplyVisualStyle();

            SetupFormBehavior();
        }

        // --- Form lifecycle overrides ---

        /// <summary>
        /// Overrides the OnPaint method to draw a custom border around the form using <see cref="UIHelper.DrawFormBorder"/>.
        /// </summary>
        /// <param name="e">A <see cref="PaintEventArgs"/> that contains the event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            UIHelper.DrawFormBorder(e, this);
        }

        // --- Initialization helpers ---

        /// <summary>
        /// Applies custom VAGWorld and BIPs fonts to all labels, buttons, and textboxes.
        /// </summary>
        private void ApplyFont()
        {
            label1.Font = FontLoader.VAGWorld(20);
            label2.Font = FontLoader.VAGWorld(20);
            label3.Font = FontLoader.VAGWorld(20);
            label4.Font = FontLoader.VAGWorld(20);
            savePaths_Button.Font = FontLoader.VAGWorld(20);

            steamPath_TextBox.Font = FontLoader.BIPs(12);
            CS2Path_TextBox.Font = FontLoader.BIPs(12);
            ObsPath_TextBox.Font = FontLoader.BIPs(12);
        }

        /// <summary>
        /// Sets up the visual style for the form, including panel styling and picture box hover effects.
        /// </summary>
        private void ApplyVisualStyle()
        {
            SetupFormStyle();
            StylePanels();
            StylePictureBoxes();
        }

        /// <summary>
        /// Sets up the initial state of the form, loading existing paths or placeholders, and
        /// binding path selection functionality for Steam, CS2, and OBS.
        /// </summary>
        private void SetupFormStyle()
        {
            UIEffects.ApplyFadeOut(this);

            // 1. Steam Setup
            var steamApp = SettingsManager.GetApp("Steam");
            string storedSteamPath = steamApp?.Path ?? string.Empty;

            // Store the currently loaded path for change detection
            _originalSteamPath = storedSteamPath;

            if (string.IsNullOrWhiteSpace(storedSteamPath))
            {
                UIHelper.ShowPlaceholder(
                    steamPath_TextBox,
                    PathSelectPlaceholder,
                    transparentPanel1
                );
            }
            else
            {
                steamPath_TextBox.Text = storedSteamPath;
                steamPath_TextBox.ForeColor = Color.Black;
            }

            UIHelper.BindPathSelector(
                selectSteamPath_pictureBox,
                steamPath_TextBox,
                PathSelectPlaceholder,
                "steam.exe",
                transparentPanel1
            );

            // 2. CS2 Setup
            var cs2App = SettingsManager.GetApp("CS2");
            string storedCS2Path = cs2App?.Path ?? string.Empty;

            // Store the currently loaded path for change detection
            _originalCS2Path = storedCS2Path;

            if (string.IsNullOrWhiteSpace(storedCS2Path))
            {
                UIHelper.ShowPlaceholder(
                    CS2Path_TextBox,
                    PathSelectPlaceholder,
                    transparentPanel2
                );
            }
            else
            {
                CS2Path_TextBox.Text = storedCS2Path;
                CS2Path_TextBox.ForeColor = Color.Black;
            }

            UIHelper.BindPathSelector(
                selectCS2Path_pictureBox,
                CS2Path_TextBox,
                PathSelectPlaceholder,
                "cs2.exe",
                transparentPanel2
            );

            // 3. OBS Setup
            var obsApp = SettingsManager.GetApp("OBS");
            string storedObsPath = obsApp?.Path ?? string.Empty;

            // Store the currently loaded path for change detection
            _originalObsPath = storedObsPath;

            if (string.IsNullOrWhiteSpace(storedObsPath))
            {
                UIHelper.ShowPlaceholder(
                    ObsPath_TextBox,
                    PathSelectPlaceholder,
                    transparentPanel3
                );
            }
            else
            {
                ObsPath_TextBox.Text = storedObsPath;
                ObsPath_TextBox.ForeColor = Color.Black;
            }

            UIHelper.BindPathSelector(
                selectObsPath_pictureBox,
                ObsPath_TextBox,
                PathSelectPlaceholder,
                "obs64.exe",
                transparentPanel3
            );
        }

        /// <summary>
        /// Applies rounded frames to all major container panels.
        /// </summary>
        private void StylePanels()
        {
            UIHelper.ApplyRoundedFrame(panel1, 20);
            UIHelper.ApplyRoundedFrame(panel2, 15);
            UIHelper.ApplyRoundedFrame(panel3, 15);
            UIHelper.ApplyRoundedFrame(panel8, 15);
        }

        /// <summary>
        /// Applies hover effects and rounded corners to path selection and reset picture boxes.
        /// Also applies rounded corners to the exit button.
        /// </summary>
        private void StylePictureBoxes()
        {
            UIHelper.ApplyHoverWithRounded(selectSteamPath_pictureBox, panel4, 5);
            UIHelper.ApplyHoverWithRounded(selectCS2Path_pictureBox, panel5, 5);
            UIHelper.ApplyHoverWithRounded(selectObsPath_pictureBox, panel10, 5);

            UIHelper.ApplyHoverWithRounded(resetSteamPath_button, panel6, 5);
            UIHelper.ApplyHoverWithRounded(resetCS2Path_button, panel7, 5);
            UIHelper.ApplyHoverWithRounded(ResetObsPath_button, panel9, 5);

            UIHelper.ApplyRoundedCorners(exit_pictureBox, 10);
        }

        /// <summary>
        /// Configures core form behaviors, such as taskbar visibility and input focus management.
        /// Also sets up the <see cref="PathTextBox_Leave"/> event handler for path textboxes.
        /// </summary>
        private void SetupFormBehavior()
        {
            this.ShowInTaskbar = false;

            // Prevent textboxes from being tab stops since path selection is done via picture boxes.
            steamPath_TextBox.TabStop = false;
            CS2Path_TextBox.TabStop = false;
            ObsPath_TextBox.TabStop = false;

            steamPath_TextBox.Leave += PathTextBox_Leave;
            CS2Path_TextBox.Leave += PathTextBox_Leave;
            ObsPath_TextBox.Leave += PathTextBox_Leave;
        }

        // --- UI Controls Handlers ---

        /// <summary>
        /// Closes the form when the exit picture box is clicked, asking the user to save changes if any were made.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void Exit_pictureBox_Click(object sender, EventArgs e)
        {
            // Check if any changes have been made.
            if (HasChanges())
            {
                // Display the confirmation dialog.
                DialogResult result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save your path changes before exiting?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    // User chose to save: call SavePaths() and close on success.
                    if (SavePaths())
                    {
                        this.Close();
                    }
                }
                else if (result == DialogResult.No)
                {
                    // User chose not to save: close the form without saving.
                    this.Close();
                }
                // If result is Cancel, the form remains open.
            }
            else
            {
                // No changes were made, close the form normally.
                this.Close();
            }
        }

        /// <summary>
        /// Attempts to save all configured application paths to the settings manager.
        /// Closes the form on successful saving or if no changes were made.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void SavePaths_button_Click(object sender, EventArgs e)
        {
            // Only proceed to Save if changes were made.
            if (!HasChanges())
            {
                // No changes made (e.g., opened and immediately clicked Save).
                this.Close();
                return;
            }

            // If changes were made (addition OR deletion), call SavePaths().
            if (SavePaths())
            {
                // If SavePaths completed successfully (including path removal), close the form.
                this.Close();
            }
        }

        /// <summary>
        /// Resets the text box for the Steam path back to the placeholder.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void ResetSteamPath_button_Click(object sender, EventArgs e)
        {
            ClearPath(steamPath_TextBox, "Steam");
        }

        /// <summary>
        /// Resets the text box for the CS2 path back to the placeholder.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void ResetCS2Path_button_Click(object sender, EventArgs e)
        {
            ClearPath(CS2Path_TextBox, "CS2");
        }

        /// <summary>
        /// Resets the text box for the OBS path back to the placeholder.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void ResetObsPath_button_Click(object sender, EventArgs e)
        {
            ClearPath(ObsPath_TextBox, "OBS");
        }

        // --- Input Behavior ---

        /// <summary>
        /// Handles the Leave event for path text boxes. If the text box is empty,
        /// it reapplies the placeholder text and color.
        /// </summary>
        /// <param name="sender">The path <see cref="TextBox"/> that lost focus.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void PathTextBox_Leave(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            
            if (textBox == null) return;

            // Show the placeholder only if the field is completely empty.
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = PathSelectPlaceholder;
                textBox.ForeColor = Color.DimGray;
            }
        }

        // --- Logic Methods ---

        /// <summary>
        /// Validates and saves the configured Steam, CS2, and OBS paths to application settings.
        /// Handles the specific validation logic for OBS (checking for obs64.exe or obs32.exe)
        /// and displays appropriate success or warning messages.
        /// After a successful save or removal, it updates the internal state variables to reflect the new saved status.
        /// </summary>
        /// <returns>Always returns <c>true</c>, indicating the save operation has completed.</returns>
        private bool SavePaths()
        {
            string steamPath = steamPath_TextBox.Text?.Trim();
            string cs2Path = CS2Path_TextBox.Text?.Trim();
            string obsPath = ObsPath_TextBox.Text?.Trim();

            // Determine if the field contains a valid path (not null, not empty, and not the placeholder)
            bool hasSteam = !string.IsNullOrWhiteSpace(steamPath) && steamPath != PathSelectPlaceholder.Trim();
            bool hasCS2 = !string.IsNullOrWhiteSpace(cs2Path) && cs2Path != PathSelectPlaceholder.Trim();
            bool hasObs = !string.IsNullOrWhiteSpace(obsPath) && obsPath != PathSelectPlaceholder.Trim();

            bool savedObsPath = false;
            bool anySavedOrRemoved = false;

            string fullObsExePath = string.Empty; 

            // Early exit if all fields are empty and no previous path existed (i.e., nothing to save or remove)
            if (!hasSteam && !hasCS2 && !hasObs && _originalSteamPath == string.Empty && _originalCS2Path == string.Empty && _originalObsPath == string.Empty)
            {
                return true;
            }

            // 1. Steam
            if (hasSteam)
            {
                var steamApp = new AppConfig { Name = "Steam", ExeName = "steam.exe", Path = steamPath };

                SettingsManager.SetApp("Steam", steamApp);
                anySavedOrRemoved = true;
            }
            else
            {
                // Remove if it was previously set but is now cleared.
                if (_originalSteamPath != string.Empty)
                {
                    SettingsManager.RemoveApp("Steam");
                    anySavedOrRemoved = true;
                }
            }

            // 2. CS2
            if (hasCS2)
            {
                var cs2App = new AppConfig { Name = "CS2", ExeName = "cs2.exe", Path = cs2Path };

                SettingsManager.SetApp("CS2", cs2App);
                anySavedOrRemoved = true;
            }
            else
            {
                // Remove if it was previously set but is now cleared.
                if (_originalCS2Path != string.Empty)
                {
                    SettingsManager.RemoveApp("CS2");
                    anySavedOrRemoved = true;
                }
            }

            // 3. OBS (with complex validation)
            if (hasObs)
            {
                string obsExeName = string.Empty;

                // Check if the path points directly to an executable
                if (File.Exists(obsPath)
                    && (obsPath.EndsWith("obs64.exe", StringComparison.OrdinalIgnoreCase)
                    || obsPath.EndsWith("obs32.exe", StringComparison.OrdinalIgnoreCase)))
                {
                    fullObsExePath = obsPath;
                    obsExeName = Path.GetFileName(obsPath);
                }
                // Check if the path points to the OBS directory
                else if (Directory.Exists(obsPath))
                {
                    string obs64FullPath = Path.Combine(obsPath, "obs64.exe");
                    if (File.Exists(obs64FullPath))
                    {
                        obsExeName = "obs64.exe";
                        fullObsExePath = obs64FullPath;
                    }
                    else
                    {
                        string obs32FullPath = Path.Combine(obsPath, "obs32.exe");
                        if (File.Exists(obs32FullPath))
                        {
                            obsExeName = "obs32.exe";
                            fullObsExePath = obs32FullPath;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(obsExeName) && !string.IsNullOrEmpty(fullObsExePath))
                {
                    var obsApp = new AppConfig
                    {
                        Name = "OBS",
                        ExeName = obsExeName,
                        Path = fullObsExePath
                    };
                    SettingsManager.SetApp("OBS", obsApp);
                    savedObsPath = true;
                    anySavedOrRemoved = true;
                }
            }
            else
            {
                // Remove if it was previously set but is now cleared.
                if (_originalObsPath != string.Empty)
                {
                    SettingsManager.RemoveApp("OBS");
                    anySavedOrRemoved = true;
                }
            }

            // --- Feedback and State Update ---

            if (anySavedOrRemoved)
            {
                // Display error only for OBS if it was intended to be set but failed validation
                if (hasObs && !savedObsPath)
                {
                    MessageBox.Show(
                        this,
                        "Error: OBS executable (obs64.exe or obs32.exe) not found in the specified path. OBS path was not saved.",
                        "Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
                else
                {
                    // Show success message if something was successfully saved or removed.
                    MessageBox.Show(
                        this,
                        "Path(s) saved successfully.",
                        "Saved",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }

                // Update the original state variables to match the new saved state.
                // This ensures the HasChanges() check is reset and the form state is correct for the next operation.
                _originalSteamPath = hasSteam ? steamPath : string.Empty;
                _originalCS2Path = hasCS2 ? cs2Path : string.Empty;
                _originalObsPath = savedObsPath ? fullObsExePath : string.Empty;
            }
            else if (hasObs && !savedObsPath)
            {
                // This handles the edge case where ONLY OBS was edited, failed validation, and nothing else changed/was removed.
                MessageBox.Show(
                    this,
                    "Error: OBS executable (obs64.exe or obs32.exe) not found in the specified path. OBS path was not saved.",
                    "Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }

            return true;
        }

        /// <summary>
        /// Checks if any of the displayed paths have been changed (added, modified, or cleared)
        /// compared to the original paths loaded from the settings.
        /// </summary>
        /// <returns>True if any path is different from its original value, otherwise false.</returns>
        private bool HasChanges()
        {
            // Get the current text, treating the placeholder as an empty string for accurate comparison.
            string currentSteamPath = steamPath_TextBox.Text?.Trim() == PathSelectPlaceholder.Trim() ? string.Empty : steamPath_TextBox.Text?.Trim();
            string currentCS2Path = CS2Path_TextBox.Text?.Trim() == PathSelectPlaceholder.Trim() ? string.Empty : CS2Path_TextBox.Text?.Trim();
            string currentObsPath = ObsPath_TextBox.Text?.Trim() == PathSelectPlaceholder.Trim() ? string.Empty : ObsPath_TextBox.Text?.Trim();

            // Compare current values against the original values.
            bool steamChanged = currentSteamPath != _originalSteamPath;
            bool cs2Changed = currentCS2Path != _originalCS2Path;
            bool obsChanged = currentObsPath != _originalObsPath;

            return steamChanged || cs2Changed || obsChanged;
        }

        /// <summary>
        /// Clears the specified text box, setting its value to the path placeholder and updating the text color.
        /// </summary>
        /// <param name="textBox">The <see cref="TextBox"/> control to clear.</param>
        /// <param name="key">The key associated with the path (e.g., "Steam", "CS2", "OBS"). This parameter is currently unused but kept for context.</param>
        private void ClearPath(TextBox textBox, string key)
        {
            textBox.ForeColor = Color.DimGray;
            textBox.Text = PathSelectPlaceholder;
        }
    }
}