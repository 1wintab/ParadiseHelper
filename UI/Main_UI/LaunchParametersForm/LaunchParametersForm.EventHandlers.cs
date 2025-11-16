using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using ParadiseHelper.Data.Settings.LaunchParameters;

namespace ParadiseHelper.UI.MainUI
{
    // This partial class contains all UI event handlers for the form, including clicks, scroll events, text changes, and focus management (Enter/Leave) for the parameter and resolution text boxes.
    public partial class LaunchParametersForm
    {
        // --- Event Handlers ---

        /// <summary>
        /// Handles the scrolling of the mode switcher.
        /// </summary>
        private void changeMode_vScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            if (isModeChanging || e.NewValue == e.OldValue)
            {
                return;
            }

            // Save the current UI state to the cache BEFORE changing mode.
            UpdateStateFromUI(selectedApp);

            isModeChanging = true;

            if (e.NewValue > e.OldValue) // Scrolled down
            {
                ModeManager.CycleToNextMode();
            }
            else // Scrolled up
            {
                ModeManager.CycleToPreviousMode();
            }

            // Update UI (icon, text) and reload parameters for the new mode.
            UpdateModeUI();

            if (!string.IsNullOrEmpty(selectedApp))
            {
                // Reload parameters, which will use the new mode.
                LoadLaunchParams(selectedApp);
            }

            // Reset the scrollbar value back to the middle asynchronously.
            this.BeginInvoke((MethodInvoker)delegate
            {
                var scrollBar = sender as VScrollBar;
                if (scrollBar != null)
                {
                    scrollBar.Scroll -= changeMode_vScrollBar_Scroll;
                    scrollBar.Value = 1; // Return to center
                    scrollBar.Scroll += changeMode_vScrollBar_Scroll;
                }
            });

            isModeChanging = false;
        }

        /// <summary>
        /// Handles clicks on the application selection panels (Steam, CS2).
        /// </summary>
        private void AppPanel_MouseDown(object sender, EventArgs e)
        {
            // 1. Save the current UI state to the buffer before switching.
            UpdateStateFromUI(selectedApp);

            // 2. Determine which panel was clicked.
            Panel clicked = (sender as Panel) ?? (sender as Control)?.Parent as Panel;
            var appName = clicked?.Tag?.ToString();

            // 3. Do nothing if the same panel was clicked.
            if (selectedApp == appName)
            {
                return;
            }

            // 4. Switch selection.
            selectedApp = appName;
            UpdateAppHighlight(clicked);

            // 5. Load the new application's state.
            LoadLaunchParams(appName);
        }

        /// <summary>
        /// Handles clicks on the 'Exit' (X) PictureBox.
        /// </summary>
        private void exit_pictureBox_Click(object sender, EventArgs e)
        {
            // Run the centralized close check.
            if (HandleCloseRequest())
            {
                // If user clicked "Yes" or "No", mark as verified.
                _isCloseVerified = true;
                
                // Initiate the standard form closing process.
                this.Close();
            }
            // If "Cancel" was clicked, the form remains open.
        }

        /// <summary>
        /// Handles clicks on the 'Save & Exit' button.
        /// </summary>
        private void saveParams_Button_Click(object sender, EventArgs e)
        {
            // 1. Update the buffer with the latest UI data.
            UpdateStateFromUI(selectedApp);

            // 2. Check if any changes were actually made.
            bool wereChangesMade = _appStates.Values.Any(s => s.HasChanges());

            // 3. Save all changes.
            if (SaveChanges())
            {
                // 4. Show success message only if changes were saved.
                if (wereChangesMade)
                {
                    MessageBox.Show(
                        "Changes have been saved successfully!",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }

                // 5. Close the form.
                _isCloseVerified = true;
                this.Close();
            }
        }

        /// <summary>
        /// Handles clicks on the 'Delete' (trash) button.
        /// </summary>
        private void deleteParams_button_Click(object sender, EventArgs e)
        {
            // Clears the text box and restores the placeholder.
            params_richTextBox.Text = Placeholder_LaunchParams;
            params_richTextBox.ForeColor = Color.Gray;
            params_richTextBox.DeselectAll();
            this.ActiveControl = null;
        }

        /// <summary>
        /// Handles clicks on the 'Reset' (undo) button.
        /// </summary>
        private void resetParams_pictureBox_Click(object sender, EventArgs e)
        {
            string stateKey = GetStateKey(selectedApp);
            if (string.IsNullOrEmpty(stateKey) || !_appStates.ContainsKey(stateKey))
            {
                params_richTextBox.Text = Placeholder_LaunchParams;
                params_richTextBox.ForeColor = Color.Gray;
                return;
            }

            // Get the state object for the selected app and mode.
            var state = _appStates[stateKey];

            // Reset the current values in the buffer to the original ones.
            state.CurrentParamsText = state.OriginalContent;
            state.CurrentWidth = state.OriginalWidth;
            state.CurrentHeight = state.OriginalHeight;
            state.CurrentWindowedState = state.OriginalWindowedState;

            // Re-apply mode logic (e.g., AI params) to the reset state.
            ApplyModeToState(state, selectedApp);

            // Update the entire UI based on the reset state.
            UpdateUIFromState(selectedApp);
            UpdateUIForCurrentMode();
        }

        /// <summary>
        /// Handles mouse clicks on the main parameter text box for placeholder logic.
        /// </summary>
        private void params_richTextBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (params_richTextBox.ReadOnly) return;
            bool isPlaceholderActive = params_richTextBox.ForeColor == Color.Gray &&
                                       PlaceholderTexts.Contains(params_richTextBox.Text);
            if (isPlaceholderActive)
            {
                params_richTextBox.Text = "";
                params_richTextBox.ForeColor = Color.Black;
                params_richTextBox.Focus();
            }
        }

        /// <summary>
        /// Handles focus entering the main parameter text box.
        /// </summary>
        private void params_richTextBox_Enter(object sender, EventArgs e)
        {
            if (PlaceholderTexts.Contains(params_richTextBox.Text))
            {
                params_richTextBox.Text = "";
                params_richTextBox.ForeColor = Color.Black;
            }
        }

        /// <summary>
        /// Handles focus leaving the main parameter text box.
        /// </summary>
        private void params_richTextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(params_richTextBox.Text))
            {
                params_richTextBox.Text = Placeholder_LaunchParams;
                params_richTextBox.ForeColor = Color.Gray;
            }
        }

        /// <summary>
        /// Restricts input in resolution text boxes to numbers and control keys.
        /// </summary>
        private void NumericTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            RichTextBox rtb = sender as RichTextBox;

            // Allow only digits and control characters (like Backspace).
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
                return;
            }

            // Prevent Enter key from creating a new line.
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                return;
            }

            // Prevent leading zeros.
            if (rtb != null && e.KeyChar == '0')
            {
                // Block '0' if the textbox is empty or at the start.
                if (rtb.Text.Length == 0 || (rtb.SelectionStart == 0 && !rtb.Text.StartsWith("max:")))
                {
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Handles focus entering a numeric (resolution) text box.
        /// </summary>
        private void NumericTextBox_Enter(object sender, EventArgs e)
        {
            RichTextBox rtb = sender as RichTextBox;
            
            if (rtb == null) return;

            // If it contains a placeholder, clear it on focus.
            if (rtb.ForeColor == Color.Gray)
            {
                rtb.Text = "";
                rtb.ForeColor = Color.Black;
            }

            rtb.SelectionAlignment = HorizontalAlignment.Center;
        }

        /// <summary>
        /// Handles focus leaving a numeric (resolution) text box, validating the value.
        /// </summary>
        private void NumericTextBox_Leave(object sender, EventArgs e)
        {
            RichTextBox rtb = sender as RichTextBox;
            if (rtb == null) return;

            Size maxRes = GetMaxScreenResolution();
            int maxVal = (rtb == WindowWidth_RichTextBox) ? maxRes.Width : maxRes.Height;
            string placeholderText = $"max: {maxVal}";

            // If empty, restore the placeholder.
            if (string.IsNullOrWhiteSpace(rtb.Text) || rtb.Text.StartsWith("max:"))
            {
                rtb.Text = placeholderText;
                rtb.ForeColor = Color.Gray;
                rtb.SelectionAlignment = HorizontalAlignment.Center;
                return;
            }

            // Validate the entered number against min/max bounds.
            if (!long.TryParse(rtb.Text.Trim(), out long enteredLong) || enteredLong < 1 || enteredLong > maxVal)
            {
                int finalValue = (enteredLong > maxVal || enteredLong < 1) ? maxVal : (int)enteredLong;

                string errorMsg =
                    $"Resolution value must be an integer between 1 and {maxVal}. " +
                    $"The maximum value ({maxVal}) will be set.";
                MessageBox.Show(
                    errorMsg,
                    "Invalid Resolution Value",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                rtb.Text = finalValue.ToString();
            }

            rtb.ForeColor = Color.Black;
            rtb.Text = rtb.Text.Trim();
            rtb.SelectionAlignment = HorizontalAlignment.Center;
        }

        /// <summary>
        /// Handles text changing in a numeric (resolution) text box to enforce max value.
        /// </summary>
        private void NumericTextBox_TextChanged(object sender, EventArgs e)
        {
            RichTextBox rtb = sender as RichTextBox;
            if (rtb == null || isInternalChange || rtb.ForeColor == Color.Gray) return;

            string currentText = rtb.Text.Trim();
            if (string.IsNullOrWhiteSpace(currentText))
            {
                return;
            }

            Size maxRes = GetMaxScreenResolution();
            int maxVal = (rtb == WindowWidth_RichTextBox) ? maxRes.Width : maxRes.Height;

            // If the value exceeds the max, automatically correct it.
            if (int.TryParse(currentText, out int enteredValue))
            {
                if (enteredValue > maxVal)
                {
                    isInternalChange = true;
                    rtb.Text = maxVal.ToString();
                    rtb.SelectionStart = rtb.Text.Length;
                    rtb.SelectionAlignment = HorizontalAlignment.Center;
                    isInternalChange = false;
                }
            }
        }

        /// <summary>
        /// Handles mouse clicks on resolution text boxes to check permissions.
        /// </summary>
        private void ResolutionTextBox_MouseDown(object sender, MouseEventArgs e)
        {
            var rtb = sender as RichTextBox;
            if (!rtb.Enabled)
            {
                CheckPermissionAndAlert(LaunchAction.ResolutionChange);
            }
        }

        /// <summary>
        /// Handles clicks on the form itself to de-select the text box.
        /// </summary>
        private void LaunchParametersForm_MouseDown(object sender, MouseEventArgs e)
        {
            // If click is outside the main text box, remove focus.
            if (!params_richTextBox.Bounds.Contains(this.PointToClient(Cursor.Position)))
            {
                this.ActiveControl = null;
                params_richTextBox.DeselectAll();
            }
        }

        /// <summary>
        /// Handles clicks on the transparent panel over the Windowed checkbox.
        /// </summary>
        private void WindowMode_TransparentPanel_Click(object sender, EventArgs e)
        {
            // If permission is granted, toggle the checkbox.
            if (CheckPermissionAndAlert(LaunchAction.WindowedMode) && WindowedMode_Checkbox.Enabled)
            {
                WindowedMode_Checkbox.Checked = !WindowedMode_Checkbox.Checked;
            }
        }

        /// <summary>
        /// Handles clicks on the transparent panel over the resolution boxes.
        /// </summary>
        private void WidthAndHeight_TransparentPanel_Click(object sender, EventArgs e)
        {
            CheckPermissionAndAlert(LaunchAction.ResolutionChange);
        }
    }
}