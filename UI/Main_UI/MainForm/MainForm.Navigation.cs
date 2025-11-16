using System.Linq;
using System.Windows.Forms;
using ParadiseHelper.UI.MainUI;

namespace ParadiseHelper
{
    // This partial class handles the creation, opening, and event orchestration of other windows, such as the SettingsForm and AIForm.
    public partial class MainForm : Form
    {
        /// <summary>
        /// Opens the application settings form using the UI helper.
        /// </summary>
        private void OpenSettingForm()
        {
            UIHelper.ShowForm<SettingsForm>(this);
        }

        /// <summary>
        /// Launches the dedicated AI automation configuration form (<see cref="AIForm"/>).
        /// Subscribes to the active account change event to update the bot activation state.
        /// </summary>
        private void LaunchAIForm()
        {
            UIHelper.ShowForm<AIForm>(this, form =>
            {
                // Subscribe the AI form's update method to handle active account changes.
                this.OnActiveAIAccountChanged += form.UpdateBotActivationState;

                // Unsubscribe from the event when the form closes to prevent memory leaks.
                form.FormClosed += (s, e) =>
                {
                    this.OnActiveAIAccountChanged -= form.UpdateBotActivationState;
                };

                // Initialize the form with the currently active account state, if any.
                string currentActiveLogin = FindCurrentActiveAIAccount();
                form.UpdateBotActivationState(currentActiveLogin);
            });
        }

        /// <summary>
        /// Iterates through the account panels to find the login of the currently checked/active AI account.
        /// </summary>
        /// <returns>The login string (Tag) of the active account, or <c>null</c> if no account is selected.</returns>
        private string FindCurrentActiveAIAccount()
        {
            foreach (Panel row in accountPanel.Controls.OfType<Panel>())
            {
                // Find the action CheckBox control in the current account row.
                var cb = row.Controls.Find("actionCheckBox", false).FirstOrDefault() as CheckBox;
                
                if (cb != null && cb.Checked)
                {
                    return cb.Tag as string;
                }
            }

            // Return null if no account is currently checked.
            return null; 
        }
    }
}