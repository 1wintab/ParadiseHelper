using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using ParadiseHelper.Core.Enums;
using ParadiseHelper.Managers;
using ParadiseHelper.Tools.UITools;

namespace ParadiseHelper
{
    // This partial class contains methods related to managing the account list UI, including loading, adding, deleting, editing, and handling click events for account selection and termination.
    public partial class MainForm : Form
    {
        /// <summary>
        /// Reloads the account list, triggering a full UI refresh.
        /// </summary>
        public void RefreshAccounts()
        {
            LoadAccounts();
        }

        /// <summary>
        /// Retrieves all accounts from the database, checks their current running status,
        /// and dynamically generates the UI controls for each account in the panel.
        /// </summary>
        private void LoadAccounts()
        {
            // Get all currently running game processes once to optimize the status check loop.
            var gameProcesses = System.Diagnostics.Process.GetProcessesByName(GameProcessName).ToList();

            // Retrieve all account logins from the database.
            List<string> logins = DatabaseHelper.GetLogins();

            // Suspend layout logic to prevent visual flickering during control manipulation.
            accountPanel.SuspendLayout();
            accountPanel.Controls.Clear();

            // Store the panel's background color for use in row creation.
            Color backgroundColor = accountPanel.BackColor;
            
            // Variable to track the total vertical space consumed by account rows.
            int totalHeight = 0;

            // Loop through all logins to build UI rows dynamically.
            foreach (string login in logins)
            {
                // Determine the account's visual status (Red, Green, or Purple) based on running processes.
                var visualStatus = GetAccountWindowState(login, gameProcesses);

                // Determine the internal status representation.
                var initialStatus = (visualStatus == AccountVisualStatus.Red) ? AccountStatus.Idle : AccountStatus.Running;

                // Determine the indicator color based on the visual status.
                Color initialColor;
                if (visualStatus == AccountVisualStatus.Green)
                    initialColor = Color.ForestGreen;
                else if (visualStatus == AccountVisualStatus.Purple)
                    initialColor = Color.MediumPurple;
                else
                    initialColor = Color.Crimson;

                // Create the UI row panel, passing the determined initial state.
                Panel row = CreateAccountRow(login, backgroundColor, initialStatus, initialColor);

                accountPanel.Controls.Add(row);

                // Accumulate the height of the current row for spacer calculation.
                totalHeight += row.Height + row.Margin.Vertical;
            }

            // Resume layout logic and force re-layout of controls.
            accountPanel.ResumeLayout();
            accountPanel.PerformLayout();

            // Add a spacer panel if the content height is less than the panel height.
            AddSpacerIfNeeded(totalHeight, backgroundColor);
        }

        /// <summary>
        /// Defines the MouseDown event handler for both the account row and the login label,
        /// managing account selection (Queued), deselection (Idle), and termination (Running).
        /// </summary>
        private void HandleAccountClick(Panel row, Label label, CheckBox checkBox, Color backgroundColor)
        {
            // Define a unified event handler for both Panel and Label clicks.
            MouseEventHandler selectionHandler = (sender, args) =>
            {
                // Only process left-mouse button clicks.
                if (args.Button != MouseButtons.Left) return;

                // Attempt to find the login label control within the row.
                var labelControl = row.Controls.OfType<Label>().FirstOrDefault();
                if (labelControl == null) return;

                // Variables for determining click position.
                int clickableXStart = labelControl.Left;
                int clickX = args.Location.X;

                // Adjust click coordinate if the sender was the Label itself.
                if (sender is Label)
                {
                    clickX += labelControl.Left;
                }

                // Skip handling if the click is detected before the login label starts.
                if (clickX < clickableXStart)
                {
                    return;
                }

                // Get the account login and current status from the UI controls.
                string accountLogin = label.Text;
                var status = (AccountStatus)row.Tag;

                // 1. If the account is "Queued", remove it from the launch queue.
                if (status == AccountStatus.Queued)
                {
                    AccountQueueManager.Remove(login: accountLogin);
                    SetAccountIndicatorStatus(login: accountLogin, status: AccountStatus.Idle, indicatorColor: Color.Crimson);
                    Log($"- Account Unselected: {accountLogin}.", Color.Crimson);

                    // Update the footer message with the new total selected count.
                    int selectedCountAfterChange = AccountQueueManager.GetQueue().Count();
                    string footerMessage = $"> Total selected: {selectedCountAfterChange}";
                    Log(footerMessage, Color.Black);

                    // Log a message if the selection list is now empty.
                    if (selectedCountAfterChange == 0)
                    {
                        Log("> Selection cleared.", Color.MediumPurple);
                    }
                    return;
                }

                // 2. If the account is "Running" (Green or Purple), terminate the associated game process.
                if (status == AccountStatus.Running)
                {
                    // Call the helper method to locate and terminate the game process.
                    KillProcessesForAccount(accountLogin);

                    // Status update is handled internally by KillProcessesForAccount.
                    Log($"- Account Stopped: {accountLogin}.", Color.Crimson);

                    return;
                }

                // 3. If the account is "Idle", add it to the launch queue (select it).
                if (status == AccountStatus.Idle)
                {
                    // Update UI state to Queued.
                    row.Tag = AccountStatus.Queued;
                    
                    // Set a visual cue for selection.
                    row.BackColor = Color.FromArgb(120, Color.White);

                    // Add the account login to the global launch queue.
                    AccountQueueManager.Add(login: accountLogin);
                    Log($"+ Account Selected: {accountLogin}.", Color.Green);

                    // Update the footer message with the new total selected count.
                    int selectedCountAfterChange = AccountQueueManager.GetQueue().Count();
                    string footerMessage = $"> Total selected: {selectedCountAfterChange}";
                    Log(footerMessage, Color.Black);
                }
            };

            // Attach the unified handler to the row panel and the login label.
            row.MouseDown += selectionHandler;
            label.MouseDown += selectionHandler;
        }

        /// <summary>
        /// Opens the 'Add Account' form to allow users to input new account credentials
        /// or import accounts. Refreshes the main list upon successful import/save.
        /// </summary>
        private void AddAccount()
        {
            UIHelper.ShowForm<AddAccountForm>(
                this,
                form =>
                {
                    // Subscribe to the event fired when new accounts are added.
                    form.AccountsImported += (sender, e) => RefreshAccounts();
                }
            );
        }

        /// <summary>
        /// Deletes all accounts that are currently in the 'Queued' status from the UI and database.
        /// </summary>
        private void DeleteAccount()
        {
            // List to hold the UI panels (rows) marked for deletion.
            List<Panel> selectedPanels = new List<Panel>();

            // Iterate through all controls in the account panel to find queued items.
            foreach (Control ctrl in accountPanel.Controls)
            {
                // Check if the control is a Panel representing a queued account.
                if (ctrl is Panel row && row.Tag is AccountStatus status && status == AccountStatus.Queued)
                {
                    selectedPanels.Add(row);
                }
            }

            // Check if any account was selected.
            if (selectedPanels.Count == 0)
            {
                MessageBox.Show(
                    "Please select at least one account to delete.",
                    "Nothing selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            // Dynamically choose the correct singular/plural form for the confirmation message.
            string accountWord = selectedPanels.Count == 1 ? "account" : "accounts";

            // Show confirmation dialog before performing permanent deletion.
            var confirm = MessageBox.Show(
                $"Are you sure you want to delete {selectedPanels.Count} {accountWord}?",
                "Confirm deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            // Stop if the user cancels the operation.
            if (confirm != DialogResult.Yes) return;

            // Iterate through the selected panels and perform deletion.
            foreach (Panel panel in selectedPanels)
            {
                // The login is stored in the panel's Name property.
                string login = panel.Name;

                if (!string.IsNullOrWhiteSpace(login))
                {
                    // Ensure the account is removed from the active processing queue to maintain queue integrity.
                    AccountQueueManager.Remove(login);

                    // 1. Delete the account record from the database.
                    DatabaseHelper.DeleteAccount(login);

                    // 2. Remove the account's UI element from the panel and dispose of resources.
                    accountPanel.Controls.Remove(panel);
                    panel.Dispose();
                }
            }

            // Re-load and redraw the account list to clean up the UI.
            LoadAccounts();

            // Update the log with the correct remaining count of selected accounts by checking the queue size.
            int selectedCountAfterDeletion = AccountQueueManager.GetQueue().Count();
            string footerMessage = $"> Total selected: {selectedCountAfterDeletion}";
            Log(footerMessage, Color.Black);

            // Optionally log a clear message if the selection list is now empty.
            if (selectedCountAfterDeletion == 0)
            {
                Log("Selection cleared.", Color.MediumPurple);
            }
        }

        /// <summary>
        /// Opens the 'Edit Account' form for the *single* account currently selected (Queued).
        /// </summary>
        private void EditAccount()
        {
            // List to hold the UI panels (rows) marked for editing (status Queued).
            List<Panel> selectedPanels = new List<Panel>();

            // Find all queued accounts.
            foreach (Control ctrl in accountPanel.Controls)
            {
                // Note: Selection for editing relies on the 'Queued' status, which is typically set by clicking the row.
                if (ctrl is Panel row && row.Tag is AccountStatus status && status == AccountStatus.Queued)
                {
                    selectedPanels.Add(row);
                }
            }

            // Editing requires exactly one selected account.
            if (selectedPanels.Count != 1)
            {
                MessageBox.Show(this,
                    "Please select exactly one account to edit.",
                    "Invalid selection",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            // Retrieve the login of the single selected account.
            string login = selectedPanels[0].Name;

            // Input validation for the login string.
            if (string.IsNullOrWhiteSpace(login))
            {
                MessageBox.Show(this, "Internal error: Account login not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Immediately remove the account from the launch queue.
            AccountQueueManager.Remove(login);

            // Open the EditAccountForm and pass the login to load the data.
            UIHelper.ShowForm<EditAccountForm>(
                this,
                form =>
                {
                    // Load the selected account's data into the edit form.
                    form.LoadLogin(login);
                    
                    // Refresh the main list when the account data is successfully saved.
                    form.AccountSaved += (sender, e) => LoadAccounts();
                }
            );  
        }

        /// <summary>
        /// Prompts the user for confirmation and, if confirmed, deletes all accounts
        /// from the database and refreshes the UI.
        /// </summary>
        private void ClearAccountList()
        {
            // Prompt the user for a high-impact confirmation.
            var confirm = MessageBox.Show(
                "Are you sure you want to permanently delete all accounts?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm == DialogResult.Yes)
            {
                // Execute mass deletion on the database.
                DatabaseHelper.DeleteAllAccounts();
               
                // Reload the UI to reflect the empty list.
                LoadAccounts();
            }
        }

        /// <summary>
        /// Selects all accounts that are currently in the 'Idle' status, setting them to 'Queued'.
        /// Logs the number of newly selected accounts.
        /// </summary>
        private void SelectAllAccounts()
        {
            // Counter for accounts that change status (from Idle to Queued).
            int newlySelected = 0;

            // Iterate through all account panels.
            foreach (Panel panel in accountPanel.Controls.OfType<Panel>())
            {
                // Only select accounts that are currently Idle.
                if (panel.Tag is AccountStatus status && status == AccountStatus.Idle)
                {
                    // Change status to Queued.
                    panel.Tag = AccountStatus.Queued;
                    
                    // Apply visual styling for selected accounts.
                    panel.BackColor = Color.FromArgb(120, Color.White);
                    newlySelected++;
                }
            }

            // Calculate the total number of selected accounts after the operation.
            int totalSelected = accountPanel.Controls
                .OfType<Panel>()
                .Count(p => p.Tag is AccountStatus s && s == AccountStatus.Queued);

            // Log the result of the bulk selection operation.
            if (newlySelected > 0)
            {
                Log($"+ Accounts Selected: all ({totalSelected})", Color.ForestGreen);
            }
            else
            {
                Log("All accounts were already selected.", Color.Gray);
            }
        }

        /// <summary>
        /// Deselects all accounts that are currently in the 'Queued' status, setting them back to 'Idle'.
        /// </summary>
        private void UnselectAllAccounts()
        {
            // Counter for accounts that are deselected.
            int count = 0;

            // 1. Change account status and reset UI visuals.
            foreach (Panel panel in accountPanel.Controls.OfType<Panel>())
            {
                // Only deselect accounts that are currently Queued.
                if (panel.Tag is AccountStatus status && status == AccountStatus.Queued)
                {
                    // Change status to Idle.
                    panel.Tag = AccountStatus.Idle;
                    
                    // Restore original background color.
                    panel.BackColor = accountPanel.BackColor;
                    count++;
                }
            }

            // 2. Logging the operation results.
            if (count > 0)
            {
                Log($"- Accounts Deselected: all ({count})", Color.Crimson);
                Log($"🟡 Selection cleared.", Color.MediumPurple);
            }
            else
            {
                Log("All accounts were already deselected.", Color.Gray);
            }
        }

        /// <summary>
        /// Dynamically creates a single Panel control (row) to display an account's login,
        /// status indicator, and interactive elements.
        /// </summary>
        /// <param name="login">The account login to display.</param>
        /// <param name="backgroundColor">The background color for the row.</param>
        /// <param name="initialStatus">The determined initial status of the account (Idle/Running).</param>
        /// <param name="initialColor">The determined initial color of the status indicator.</param>
        /// <returns>A fully configured Panel control representing the account row.</returns>
        private Panel CreateAccountRow(string login, Color backgroundColor, AccountStatus initialStatus, Color initialColor)
        {
            // Create the main container panel for the account row.
            Panel row = new Panel
            {
                Width = accountPanel.ClientSize.Width,
                Height = 28,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                Name = login, // Store the login here for easy retrieval (used for deletion).
                BackColor = backgroundColor,
                Tag = initialStatus // Store the current status for click handlers.
            };

            // 1. Create status indicator (a colored circle drawn via Paint event).
            Panel statusIndicator = new Panel
            {
                Name = "statusIndicator",
                Size = new Size(StatusIndicatorSize, StatusIndicatorSize),
                Location = new Point(8, (row.Height - StatusIndicatorSize) / 2 + 1),
                BackColor = Color.Transparent,
                Tag = initialColor // Store the indicator color to be used in the Paint handler.
            };
            statusIndicator.Paint += StatusIndicator_Paint;

            // 2. Create CheckBox (currently used as a place holder or secondary interaction).
            CheckBox actionCheckBox = new CheckBox
            {
                Name = "actionCheckBox",
                Text = "", // Checkbox does not display text.
                AutoSize = true,
                Location = new Point(statusIndicator.Right + 8, (row.Height - 14) / 2 + 1),
                Tag = login,
                ForeColor = Color.Black,
                Checked = false,
                // Only enable the checkbox if the status is Running/Purple (e.g., to handle special interactions).
                Enabled = (initialStatus == AccountStatus.Running && initialColor == Color.MediumPurple)
            };
            actionCheckBox.CheckedChanged += ActionCheckBox_CheckedChanged;

            // 3. Create Label for the account login name.
            // Load the font style (assuming FontLoader.BIPs loads a custom font).
            Font labelFont = FontLoader.BIPs(14, FontStyle.Bold);
            Label label = new Label
            {
                Text = login,
                Font = labelFont,
                ForeColor = Color.Black,
                AutoSize = true,
                // Position the label slightly to the right of the checkbox.
                Location = new Point(actionCheckBox.Location.X + 20, (row.Height - labelFont.Height) / 2),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            // Add click handlers to enable selection/action functionality.
            HandleAccountClick(row, label, actionCheckBox, backgroundColor);

            // Add all created controls to the main row panel.
            row.Controls.Add(statusIndicator);
            row.Controls.Add(actionCheckBox);
            row.Controls.Add(label);

            return row;
        }

        /// <summary>
        /// Adds an empty panel (spacer) at the bottom of the account list if the content
        /// does not fill the entire panel height, preventing visual artifacts.
        /// </summary>
        /// <param name="totalContentHeight">The combined height of all account rows.</param>
        /// <param name="backgroundColor">The background color to use for the spacer.</param>
        private void AddSpacerIfNeeded(int totalContentHeight, Color backgroundColor)
        {
            // Calculate the remaining height needed to fill the panel.
            int neededPadding = accountPanel.ClientSize.Height - totalContentHeight;

            if (neededPadding > 0)
            {
                // Create a spacer panel to occupy the empty space.
                Panel spacer = new Panel
                {
                    // Subtract a small amount to allow for borders/margins.
                    Height = neededPadding - 4,
                    Dock = DockStyle.Bottom,
                    BackColor = backgroundColor,
                    Enabled = false // Prevent interaction with the spacer.
                };

                accountPanel.Controls.Add(spacer);
            }
        }
    }
}