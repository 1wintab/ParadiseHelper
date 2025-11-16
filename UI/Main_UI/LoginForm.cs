using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Common.Helpers.Tools.MemoryTools;
using ParadiseHelper.Tools.UITools;

namespace ParadiseHelper
{
    /// <summary>
    /// A form used to add a new account (login and password) to the application's database.
    /// </summary>
    public partial class LoginForm : SmartForm
    {
        /// <summary>
        /// A reference to an intermediate form that should be closed immediately upon successful login/account addition.
        /// </summary>
        private readonly Form _formToCloseOnSuccess;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginForm"/> class with a form to close upon success.
        /// </summary>
        /// <param name="intermediateForm">The form to close after a successful account insertion.</param>
        public LoginForm(Form intermediateForm) : this()
        {
            _formToCloseOnSuccess = intermediateForm;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginForm"/> class.
        /// </summary>
        public LoginForm()
        {
            InitializeComponent();

            ApplyFont();
            ApplyVisualStyle();
            SetupFormBehavior();
            SetupInputFields();
        }

        // -- Form lifecycle overrides ---

        /// <summary>
        /// Called when the form is closed. Used for resource cleanup and memory management.
        /// </summary>
        /// <param name="e">The <see cref="FormClosedEventArgs"/> instance containing the event data.</param>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            MemoryHelper.EnsureFormRelease(this);

            this.BackgroundImage?.Dispose();
            this.BackgroundImage = null;
        }

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
            save_Button.Font = FontLoader.VAGWorld(20);

            login_TextBox.Font = FontLoader.VAGRoundedBold(14);
            password_TextBox.Font = FontLoader.VAGRoundedBold(14);
        }

        /// <summary>
        /// Applies custom visual styles, including fade-out effects and rounded panels/corners.
        /// </summary>
        private void ApplyVisualStyle()
        {
            UIEffects.ApplyFadeOut(this);

            // Apply rounded frames to panels
            UIHelper.ApplyRoundedFrame(panel1, 5);
            UIHelper.ApplyRoundedFrame(panel2, 20);
            UIHelper.ApplyRoundedFrame(panel4, 20);
            UIHelper.ApplyRoundedFrame(panel5, 20);

            // Apply rounded corners to the exit picture box
            UIHelper.ApplyRoundedCorners(pictureBox1, 10);
        }

        /// <summary>
        /// Sets up input field behavior, including key press handlers and password masking.
        /// </summary>
        private void SetupInputFields()
        {
            login_TextBox.KeyPress += textBox_KeyPress;
            password_TextBox.KeyPress += textBox_KeyPress;

            password_TextBox.UseSystemPasswordChar = true;
        }

        /// <summary>
        /// Configures general form properties and behavior.
        /// </summary>
        private void SetupFormBehavior()
        {
            // Prevents the form icon from appearing in the taskbar.
            this.ShowInTaskbar = false;
        }

        // --- UI Controls Handlers ---

        /// <summary>
        /// Handles the click event for the exit picture box, closing the form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Handles the click event for showing the password (disabling the password mask).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void pictureBox4_Click(object sender, EventArgs e)
        {
            pictureBox4.Visible = false;
            password_TextBox.UseSystemPasswordChar = false;
        }

        /// <summary>
        /// Handles the click event for hiding the password (enabling the password mask).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void pictureBox2_Click(object sender, EventArgs e)
        {
            pictureBox4.Visible = true;
            password_TextBox.UseSystemPasswordChar = true;
        }

        /// <summary>
        /// Validates key presses in the text boxes, allowing only specific characters (letters, numbers, and common symbols).
        /// </summary>
        /// <param name="sender">The source of the event (the TextBox).</param>
        /// <param name="e">The <see cref="KeyPressEventArgs"/> that contains the event data.</param>
        private void textBox_KeyPress(object sender, KeyPressEventArgs e)
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
        /// Handles the click event for the Login/Add button, triggering account data processing.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void save_Button_Click(object sender, EventArgs e)
        {
            ProcessAccountData();
        }

        // --- Logic Methods ---

        /// <summary>
        /// Validates input fields and attempts to insert the new account into the database.
        /// Handles success and error notifications, and manages form closing.
        /// </summary>
        private void ProcessAccountData()
        {
            string login = login_TextBox.Text.Trim();
            string password = password_TextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    "Please fill in all fields!",
                    "Missing Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            bool success = DatabaseHelper.InsertAccount(login, password);

            if (success)
            {
                MessageBox.Show(
                    "Account added successfully!",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // 1. Notify the main form (if it is the owner) to refresh the account list
                if (this.Owner is MainForm mainForm)
                {
                    mainForm.RefreshAccounts();
                }

                // 2. Close the intermediate form (if one was provided in the constructor)
                if (_formToCloseOnSuccess != null && !_formToCloseOnSuccess.IsDisposed)
                {
                    _formToCloseOnSuccess.Close();
                }

                // 3. Close the current login form
                this.Close();
            }
            else
            {
                MessageBox.Show(
                    "Account with this login already exists!",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}