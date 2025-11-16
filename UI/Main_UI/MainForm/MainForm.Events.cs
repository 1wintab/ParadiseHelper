using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using ParadiseHelper.Managers;
using ParadiseHelper.UI.MainUI;
using ParadiseHelper.OBS;

namespace ParadiseHelper
{
    // This partial class contains all UI event handlers (e.g., button clicks, PictureBox clicks, checkbox changes) that connect user interface actions to the application's business logic.
    public partial class MainForm : Form
    {
        /// <summary>
        /// Handles the click event for the **Add Account** button.
        /// </summary>
        private void AddAccount_Button_Click(object sender, EventArgs e)
        {
            AddAccount();
        }

        /// <summary>
        /// Handles the click event for the **Delete Account** button.
        /// </summary>
        private void DeleteAccount_Button_Click(object sender, EventArgs e)
        {
            DeleteAccount();
        }

        /// <summary>
        /// Handles the click event for the **Edit Account** button.
        /// </summary>
        private void EditAccount_Button_Click(object sender, EventArgs e)
        {
            EditAccount();
        }

        /// <summary>
        /// Handles the click event for the **Reset Accounts** button.
        /// Clears all accounts from the list.
        /// </summary>
        private void ResetAccounts_Button_Click(object sender, EventArgs e)
        {
            ClearAccountList();
        }

        /// <summary>
        /// Handles the click event for the **Update List** PictureBox.
        /// Reloads the accounts displayed in the list control.
        /// </summary>
        private void UpdateList_picturebox_Click(object sender, EventArgs e)
        {
            LoadAccounts();
        }

        /// <summary>
        /// Handles the click event for the **Unselect All** PictureBox.
        /// Deselects all accounts in the list.
        /// </summary>
        private void UnselectAllList_pictureBox_Click(object sender, EventArgs e)
        {
            UnselectAllAccounts();
        }

        /// <summary>
        /// Handles the click event for the **Select All** PictureBox.
        /// Selects all accounts in the list.
        /// </summary>
        private void SelectAllList_pictureBox_Click(object sender, EventArgs e)
        {
            SelectAllAccounts();
        }

        /// <summary>
        /// Handles the click event for the **Start** button.
        /// Initiates the launch of selected accounts.
        /// </summary>
        private void Start_Button_Click(object sender, EventArgs e)
        {
            StartAccounts();
        }

        /// <summary>
        /// Handles the click event for the **Turn Off** button.
        /// Gracefully shuts down all running game processes and resets the application state.
        /// </summary>
        private async void TurnOff_Button_Click(object sender, EventArgs e)
        {
            // Disable the button to prevent repeated clicks during shutdown sequence.
            turnOff_Button.Enabled = false;

            try
            {
                // Attempt to kill target processes (Steam, steamwebhelper, cs2).
                bool processesWereKilled = KillProcesses(new[] { "steam.exe", "steamwebhelper.exe", "cs2.exe" });

                // Clear the account queue and reset visual statuses.
                AccountQueueManager.Clear();
                ResetAllAccountStatuses();
                
                // Define the pause duration for system stabilization.
                const int pauseDurationMs = 500;

                // Log the shutdown result.
                if (processesWereKilled)
                {
                    Log($"🟡 Shutdown complete. Queue cleared, statuses reset. Pausing system for {pauseDurationMs}ms.", Color.MediumPurple);
                }
                else
                {
                    Log($"ℹ️ Nothing was running. Queue cleared. Pausing system for {pauseDurationMs}ms.", Color.Gray);
                }

                // Execute the non-blocking delay.
                await Task.Delay(pauseDurationMs);

                Log("✅ System ready for new actions.", Color.DarkSlateGray);
            }
            finally
            {
                // Re-enable the button after the full process completes.
                turnOff_Button.Enabled = true;
            }
        }

        /// <summary>
        /// Handles the click event for the **Run With AI CFG** button.
        /// Toggles the associated checkbox state.
        /// </summary>
        private void RunWithAiCfg_Button_Click(object sender, EventArgs e)
        {
            isStartWithAICFG_checkbox.Checked = !isStartWithAICFG_checkbox.Checked;
        }

        /// <summary>
        /// Handles the click event for the **Settings** PictureBox.
        /// Opens the application settings form.
        /// </summary>
        private void Settings_pictureBox_Click(object sender, EventArgs e)
        {
            OpenSettingForm();
        }

        /// <summary>
        /// Handles the click event for the **AI CS2** PictureBox.
        /// Launches the specialized form for AI-related tools.
        /// </summary>
        private void AI_CS2_pictureBox_Click(object sender, EventArgs e)
        {
            LaunchAIForm();
        }

        /// <summary>
        /// Handles the click event for the **Configuration** PictureBox.
        /// Opens the launch parameters configuration form.
        /// </summary>
        private void Cfg_pictureBox_Click(object sender, EventArgs e)
        {
            UIHelper.ShowForm<LaunchParametersForm>(this);
        }

        /// <summary>
        /// Handles the click event for the **Window Map** PictureBox.
        /// </summary>
        private void WindowMap_pictureBox_Click(object sender, EventArgs e)
        {
            // TODO: Implement window mapping functionality.
        }

        /// <summary>
        /// Handles the click event for the **Zoom Log** PictureBox.
        /// Opens the current session log in a separate, larger window.
        /// </summary>
        private void zoomLog_pictureBox_Click(object sender, EventArgs e)
        {
            OpenSessionLog();
        }

        /// <summary>
        /// Handles the click event for the **Save Log** PictureBox.
        /// Initiates the saving of the current log content to a file.
        /// </summary>
        private void SaveLog_pictureBox_Click(object sender, EventArgs e)
        {
            SaveLog();
        }

        /// <summary>
        /// Handles the click event for the **Delete Log** PictureBox.
        /// Clears the content of the log panel.
        /// </summary>
        private void DeleteLog_pictureBox_Click(object sender, EventArgs e)
        {
            DeleteLog();
        }

        /// <summary>
        /// Handles the click event for the **Quit** menu item.
        /// Closes the application.
        /// </summary>
        private void QuitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Handles the click event for the **Settings** menu item.
        /// Opens the application settings form.
        /// </summary>
        private void settingToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OpenSettingForm();
        }

        /// <summary>
        /// Handles the **GotFocus** event for the log RichTextBox.
        /// Clears the placeholder text when the control receives focus.
        /// </summary>
        private void log_RichTextBox_GotFocus(object sender, EventArgs e)
        {
            // The logger now manages its own placeholder state.
            _logger.HandleGotFocus();
        }

        /// <summary>
        /// Handles the **LostFocus** event for the log RichTextBox.
        /// Displays placeholder text if the control is empty after losing focus.
        /// </summary>
        private void log_RichTextBox_LostFocus(object sender, EventArgs e)
        {
            // The logger now manages its own placeholder state.
            _logger.HandleLostFocus();
        }

        /// <summary>
        /// Custom Paint handler for drawing the status indicator as a colored circle with a border.
        /// </summary>
        private void StatusIndicator_Paint(object sender, PaintEventArgs e)
        {
            // Exit if the sender is not a Panel control.
            if (!(sender is Panel indicator)) return;

            // Set smoothing mode for better visual quality.
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // Determine the status color from the Panel's Tag property.
            Color statusColor = (Color)(indicator.Tag ?? Color.Crimson);
            
            // Define the border width.
            int penWidth = 2;

            // Get the size of the Panel control.
            int size = indicator.ClientRectangle.Width;

            // Calculate half the pen width for positioning.
            int halfPen = penWidth / 2;
            
            // Rectangle for the fill, trimmed slightly at the bottom
            Rectangle fillRect = new Rectangle(
                halfPen,
                halfPen,
                size - penWidth,
                size - penWidth - 1 // Height reduced by 1px for visual centering
            );
            
            // Rectangle for the stroke, shifted up 1px for alignment with the fill.
            Rectangle strokeRect = new Rectangle(
                halfPen,
                halfPen - 1,
                size - penWidth,
                size - penWidth
            );
            
            // Draw the colored fill ellipse.
            using (var fillBrush = new SolidBrush(statusColor))
            {
                e.Graphics.FillEllipse(fillBrush, fillRect);
            }

            // Draw the black border ellipse.
            using (var borderPen = new Pen(Color.Black, penWidth))
            {
                e.Graphics.DrawEllipse(borderPen, strokeRect);
            }
        }

        /// <summary>
        /// Handles the **CheckedChanged** event for an account's action checkbox, managing
        /// exclusive selection and integration with external tools (like OBS).
        /// (MODIFIED) Includes a re-entrancy check (_isObsConnecting)
        /// </summary>
        private async void ActionCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            // Cast sender to CheckBox and validate.
            CheckBox activeCheckBox = sender as CheckBox;
            if (activeCheckBox == null) return;
            
            // Retrieve the account login ID from the CheckBox Tag.
            string login = activeCheckBox.Tag as string;
            if (string.IsNullOrEmpty(login)) return;
            
            // Check if the account has the correct visual status (Purple/Ready) to be activated.
            if (_lastAccountStatuses.TryGetValue(login, out var status) && status != AccountVisualStatus.Purple)
            {
                // If status is not Purple, ensure the checkbox is unchecked and exit.
                if (activeCheckBox.Checked)
                {
                    activeCheckBox.Checked = false;
                }
                return;
            }

            // Handle Activation
            if (activeCheckBox.Checked)
            {
                // Re-entrancy check: Prevent starting a new connection if one is already in progress.
                if (_isObsConnecting)
                {
                    Log("ℹ️ OBS connection is already in progress. Please wait.", Color.Gray);
                    activeCheckBox.Checked = false; // Revert the user's click
                    return;
                }

                // Set the busy flag to true
                _isObsConnecting = true;

                // Exclusive selection logic: uncheck all other action checkboxes in the panel.
                foreach (Panel row in accountPanel.Controls.OfType<Panel>())
                {
                    var otherCheckBox = row.Controls.Find("actionCheckBox", false).FirstOrDefault() as CheckBox;
                    if (otherCheckBox != null && otherCheckBox != activeCheckBox && otherCheckBox.Checked)
                    {
                        otherCheckBox.Checked = false;
                    }
                }

                bool success = false;
                try
                {
                    // Execute the special action (e.g., OBS connection).
                    success = await HandleSpecialAction(login);
                }
                finally
                {
                    // Clear the busy flag regardless of success or failure.
                    _isObsConnecting = false;
                }


                if (!success)
                {
                    // Show error message on connection failure.
                    MessageBox.Show(
                      "Failed to connect to OBS. Check if OBS path is correct, or if the OBS \nWebsocket " +
                      "server is enabled and configured correctly.",
                      "OBS Connection Error",
                      MessageBoxButtons.OK,
                      MessageBoxIcon.Error
                    );
                    
                    // Revert the checkbox state due to failure.
                    activeCheckBox.Checked = false;
                    
                    // Notify that nothing is selected (due to error)
                    OnActiveAIAccountChanged?.Invoke(null);
                }
                else
                {
                    // Notify subscribers that the account is activated
                    OnActiveAIAccountChanged?.Invoke(login);
                }
            }
            // Handle Deactivation
            else
            {
                // If the user unchecks the box, clear the busy flag.
                _isObsConnecting = false;

                // Notify subscribers that the selection is cleared
                OnActiveAIAccountChanged?.Invoke(null);
                
                // Only send disconnect commands if the OBS process is currently running.
                if (_isObsProcessRunning)
                {
                    await VirtualCamTurnOffAsync();
                    ObsController.Instance.Disconnect();
                    Log($"❌️ Disconnected from OBS Websocket.", Color.IndianRed);
                }
            }
        }
    }
}