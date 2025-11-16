using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using ParadiseHelper.Tools.UITools;

namespace ParadiseHelper
{
    /// <summary>
    /// A form used for viewing, editing, saving, and deleting a single user account's credentials.
    /// </summary>
    public partial class EditAccountForm : SmartForm
    {
        private string originalLogin;
        private string originalPassword;

        /// <summary>
        /// Occurs when an account has been successfully saved or deleted, prompting the main list to refresh.
        /// </summary>
        public event EventHandler AccountSaved;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditAccountForm"/> class.
        /// </summary>
        public EditAccountForm()
        {
            InitializeComponent();

            ApplyFont();
            ApplyVisualStyle();

            SetupFormBehavior();
            SetupInputFields();
            ConfigureInputs();
        }

        // --- Form lifecycle overrides ---

        /// <summary>
        /// Handles the paint event to draw a custom border around the form.
        /// </summary>
        /// <param name="e">A <see cref="PaintEventArgs"/> that contains the event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            UIHelper.DrawFormBorder(e, this);
        }

        // --- Initialization helpers ---

        /// <summary>
        /// Applies custom fonts to all necessary UI controls.
        /// </summary>
        private void ApplyFont()
        {
            label2.Font = FontLoader.VAGWorld(20);
            label4.Font = FontLoader.VAGWorld(20);
            label5.Font = FontLoader.VAGWorld(20);
            save_button.Font = FontLoader.VAGWorld(20);

            deleteAccount_button.Font = FontLoader.VAGWorld(14);
            restorePrevious_button.Font = FontLoader.VAGWorld(14);

            loginTextBox.Font = FontLoader.VAGRoundedBold(14);
            passwordTextBox.Font = FontLoader.VAGRoundedBold(14);
        }

        /// <summary>
        /// Applies custom visual styles, including fade-out effects and rounded panels.
        /// </summary>
        private void ApplyVisualStyle()
        {
            UIEffects.ApplyFadeOut(this);
            StylePanels();
            StylePictureBoxes();
        }

        /// <summary>
        /// Applies rounded corner frames to interactive panels.
        /// </summary>
        private void StylePanels()
        {
            UIHelper.ApplyRoundedFrame(panel1, 5);
            UIHelper.ApplyRoundedFrame(panel2, 20);
            UIHelper.ApplyRoundedFrame(panel4, 20);
            UIHelper.ApplyRoundedFrame(panel5, 20);
        }

        /// <summary>
        /// Applies rounded corners to specific picture box elements.
        /// </summary>
        private void StylePictureBoxes()
        {
            UIHelper.ApplyRoundedCorners(exit_pictureBox, 10);
        }

        /// <summary>
        /// Configures general form properties and behavior.
        /// </summary>
        private void SetupFormBehavior()
        {
            // Prevents the form icon from appearing in the taskbar.
            this.ShowInTaskbar = false;
        }

        /// <summary>
        /// Attaches event handlers for input validation to the text boxes.
        /// </summary>
        private void SetupInputFields()
        {
            loginTextBox.KeyPress += TextBox_KeyPress;
            passwordTextBox.KeyPress += TextBox_KeyPress;
        }

        /// <summary>
        /// Configures initial input settings, such as masking the password field.
        /// </summary>
        private void ConfigureInputs()
        {
            passwordTextBox.UseSystemPasswordChar = true;
        }

        // --- UI Controls Handlers ---

        /// <summary>
        /// Handles the click event for showing the password (disabling the password mask).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void PictureBox4_Click(object sender, EventArgs e)
        {
            PictureBox4.Visible = false;
            passwordTextBox.UseSystemPasswordChar = false;
        }

        /// <summary>
        /// Handles the click event for hiding the password (enabling the password mask).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void PictureBox2_Click(object sender, EventArgs e)
        {
            PictureBox4.Visible = true;
            passwordTextBox.UseSystemPasswordChar = true;
        }

        /// <summary>
        /// Validates key presses in the text boxes, allowing only specific characters (letters, numbers, and common symbols).
        /// </summary>
        /// <param name="sender">The source of the event (the TextBox).</param>
        /// <param name="e">The <see cref="KeyPressEventArgs"/> that contains the event data.</param>
        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allowed characters: letters, numbers, and symbols commonly used in logins/passwords.
            bool isAllowedChar = Regex.IsMatch(e.KeyChar.ToString(), @"^[a-zA-Z0-9\-_.!@#$%&*+=?]+$");

            // Control characters (like Backspace) are always allowed.
            bool isControlChar = char.IsControl(e.KeyChar);

            if (!isControlChar && !isAllowedChar)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the click event for the exit picture box. Attempts to save changes before closing.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void Exit_pictureBox_Click(object sender, EventArgs e)
        {
            if (TrySaveChanges())
            {
                this.Close();
            }
        }

        /// <summary>
        /// Handles the click event for the Save button. Explicitly saves changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void Save_button_Click(object sender, EventArgs e)
        {
            SaveChanges();
        }

        /// <summary>
        /// Handles the click event for the Delete button. Initiates the account deletion process.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void DeleteAccount_button_Click(object sender, EventArgs e)
        {
            DeleteAccount();
        }

        /// <summary>
        /// Handles the click event for the Restore button. Resets text fields to their original loaded values.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void RestorePrevious_button_Click(object sender, EventArgs e)
        {
            RestorePrevious();
        }

        // --- Logic Methods ---

        /// <summary>
        /// Performs validation and updates the account in the database.
        /// </summary>
        /// <returns><c>true</c> if the save operation was successful or no changes were made; otherwise, <c>false</c>.</returns>
        private bool PerformSave()
        {
            string currentLogin = loginTextBox.Text.Trim();
            string currentPassword = passwordTextBox.Text.Trim();

            // Check if any change has been made
            bool isModified = currentLogin != originalLogin || currentPassword != originalPassword;

            if (!isModified) return true;

            // Validation checks
            if (string.IsNullOrWhiteSpace(currentLogin))
            {
                MessageBox.Show(
                    this,
                    "Login field cannot be empty.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                loginTextBox.Focus();

                return false;
            }

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                MessageBox.Show(
                    this,
                    "Password field cannot be empty.",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                passwordTextBox.Focus();

                return false;
            }

            // Check for duplicate login only if the login field was changed
            if (currentLogin != originalLogin && DatabaseHelper.LoginExists(currentLogin, originalLogin))
            {
                MessageBox.Show(
                    this,
                    "An account with this login already exists.",
                    "Duplicate Login",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                loginTextBox.Focus();

                return false;
            }

            // Perform database update
            DatabaseHelper.UpdateAccount(originalLogin, currentLogin, currentPassword);

            return true;
        }

        /// <summary>
        /// Attempts to save changes and handles success notification and form closing.
        /// </summary>
        private void SaveChanges()
        {
            if (PerformSave())
            {
                // Update the original values to reflect the saved changes
                originalLogin = loginTextBox.Text.Trim();
                originalPassword = passwordTextBox.Text.Trim();

                MessageBox.Show(
                    this,
                    "Account changes saved successfully!",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                AccountSaved?.Invoke(this, EventArgs.Empty);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        /// <summary>
        /// Prompts the user to save changes if any modification was made before exiting.
        /// </summary>
        /// <returns><c>true</c> if the user chose to save successfully or discard changes; <c>false</c> if the user canceled the operation or save failed validation.</returns>
        private bool TrySaveChanges()
        {
            string currentLogin = loginTextBox.Text.Trim();
            string currentPassword = passwordTextBox.Text.Trim();

            bool isModified = currentLogin != originalLogin || currentPassword != originalPassword;

            if (!isModified) return true;

            DialogResult result = MessageBox.Show(
                this,
                "You have modified the login or password.\n" +
                "Do you want to save changes to the database?",
                "Confirm Save",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.No) return true;

            if (result == DialogResult.Cancel) return false;

            // if (result == DialogResult.Yes)
            if (PerformSave())
            {
                this.DialogResult = DialogResult.OK;

                // Update the original values after successful save via prompt
                originalLogin = currentLogin;
                originalPassword = currentPassword;

                // Notify the main form to refresh its list.
                AccountSaved?.Invoke(this, EventArgs.Empty);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Loads the account data into the form based on the provided login.
        /// This should be called immediately after the form is created and before it is shown.
        /// </summary>
        /// <param name="login">The unique login of the account to be edited.</param>
        public void LoadLogin(string login)
        {
            originalLogin = login;
            originalPassword = DatabaseHelper.GetPasswordForLogin(login);

            loginTextBox.Text = login;
            passwordTextBox.Text = originalPassword;
        }

        /// <summary>
        /// Prompts the user for confirmation and deletes the account from the database.
        /// </summary>
        private void DeleteAccount()
        {
            if (string.IsNullOrWhiteSpace(originalLogin))
            {
                MessageBox.Show(
                    this,
                    "Unable to delete: login not loaded.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return;
            }

            DialogResult confirm = MessageBox.Show(
                this,
                $"Are you sure you want to delete the account \"{originalLogin}\"?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm != DialogResult.Yes) return;

            DatabaseHelper.DeleteAccount(originalLogin);

            MessageBox.Show(
                this,
                $"Account \"{originalLogin}\" has been deleted.",
                "Deleted",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            AccountSaved?.Invoke(this, EventArgs.Empty);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// Restores the login and password text boxes to the original values loaded when the form opened.
        /// </summary>
        private void RestorePrevious()
        {
            loginTextBox.Text = originalLogin;
            passwordTextBox.Text = originalPassword;
        }
    }
}