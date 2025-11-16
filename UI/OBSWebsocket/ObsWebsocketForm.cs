using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using ParadiseHelper;
using ParadiseHelper.OBS;
using UI.OBSWebsocket;
using ParadiseHelper.Tools.UITools;

namespace UI.Main_UI
{
    public partial class OBSWebsocketForm : SmartForm
    {
        // --- Constants for Placeholders ---
        private const string IpPlaceholder = "Enter IP Address";
        private const string PortPlaceholder = "Enter Port";
        private const string PasswordPlaceholder = "Enter Password";

        // Stores the current configuration loaded from the file
        private OBSConnectionParams _currentConfig;

        /// <summary>
        /// Initializes a new instance of the OBSWebsocketForm class.
        /// </summary>
        public OBSWebsocketForm()
        {
            InitializeComponent();

            // 1. Load config and initialize UI management
            InitObsUiManagement();

            ApplyFont();
            ApplyVisualStyle();

            SetupFormBehavior();

            // 2. Load the actual saved data into fields
            LoadSavedConfigToFields();

            // 3. Set up input fields, including applying placeholders only if empty
            SetupInputFields();
        }

        // --- Form Lifecycle Overrides ---

        /// <summary>
        /// Handles the loading of the form.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        /// <summary>
        /// Handles the closing of the form. Unsubscribes from events to prevent memory leaks.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            // Unsubscribe to prevent memory leaks and multiple subscriptions when the form is opened again.
            ObsController.Instance.ConnectionStatusChanged -= OBSStatus_Changed;
        }

        /// <summary>
        /// Handles custom drawing for the form.
        /// </summary>
        /// <param name="e">The paint event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw the custom border for the form.
            UIHelper.DrawFormBorder(e, this);
        }

        /// <summary>
        /// Handles changes in the OBS connection status and updates the UI PictureBoxes accordingly.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OBSStatus_Changed(object sender, EventArgs e)
        {
            // Safe UI update from a background thread
            if (this.InvokeRequired)
            {
                this.Invoke(new EventHandler(OBSStatus_Changed), sender, e);
                return;
            }

            // Update PictureBoxes visibility based on connection status
            if (ObsController.Instance.IsConnected)
            {
                Connected_PictureBox.Visible = true;
                Disconnected_PictureBox.Visible = false;
            }
            else
            {
                Disconnected_PictureBox.Visible = true;
                Connected_PictureBox.Visible = false;
            }
        }

        // --- Initialization Helpers ---

        /// <summary>
        /// Initializes the OBS UI management: subscribes to status events and loads saved configuration.
        /// </summary>
        private void InitObsUiManagement()
        {
            // 1. Subscribe to the connection status change event.
            ObsController.Instance.ConnectionStatusChanged += OBSStatus_Changed;

            // 2. Load configuration into private field.
            _currentConfig = OBSConfigManager.Load();

            // 3. Manually check the current status immediately after opening the form.
            // This ensures the correct connection status icon is displayed instantly.
            OBSStatus_Changed(null, EventArgs.Empty);
        }

        /// <summary>
        /// Loads the saved configuration data into the respective text fields and handles initial password masking.
        /// </summary>
        private void LoadSavedConfigToFields()
        {
            // Set text fields from config.
            loginTextBox.Text = _currentConfig.Ip;

            // Only set port if it's not the default (0)
            portTextBox.Text = _currentConfig.Port > 0 ? _currentConfig.Port.ToString() : string.Empty;
            passwordTextBox.Text = _currentConfig.Password;

            // If a password was loaded, ensure masking is immediately applied 
            // before the SetupInputFields triggers the placeholder logic.
            if (!string.IsNullOrEmpty(_currentConfig.Password))
            {
                // Set to true, assuming the user generally wants the password hidden when loading the form.
                passwordTextBox.UseSystemPasswordChar = true;
                
                // Since text is present, ensure the color is black (no placeholder color)
                passwordTextBox.ForeColor = Color.Black;
            }
        }

        /// <summary>
        /// Applies custom fonts to all relevant UI elements (labels, buttons, text boxes).
        /// </summary>
        private void ApplyFont()
        {
            label1.Font = FontLoader.VAGWorld(20);
            label2.Font = FontLoader.VAGWorld(20);
            label3.Font = FontLoader.VAGWorld(20);
            label4.Font = FontLoader.VAGWorld(20);
            label5.Font = FontLoader.VAGWorld(20);

            saveParams_Button.Font = FontLoader.VAGWorld(20);
            testConnection_Button.Font = FontLoader.VAGWorld(20);

            loginTextBox.Font = FontLoader.VAGRoundedBold(14);
            portTextBox.Font = FontLoader.VAGRoundedBold(14);
            passwordTextBox.Font = FontLoader.VAGRoundedBold(14);
        }

        /// <summary>
        /// Applies visual effects and custom styling to the form elements.
        /// </summary>
        private void ApplyVisualStyle()
        {
            UIEffects.ApplyFadeOut(this);
            StylePanels();
            StylePictureBoxes();
        }

        /// <summary>
        /// Applies rounded corners and custom frames to main container panels.
        /// </summary>
        private void StylePanels()
        {
            // Apply rounded corners to the form close control.
            UIHelper.ApplyRoundedCorners(exit_pictureBox, 10);

            // Apply rounded frames to main input containers.
            UIHelper.ApplyRoundedFrame(panel1, 20);
            UIHelper.ApplyRoundedFrame(panel3, 15);
            UIHelper.ApplyRoundedFrame(panel4, 15);
            UIHelper.ApplyRoundedFrame(panel5, 15);
            UIHelper.ApplyRoundedFrame(panel6, 15);
            UIHelper.ApplyRoundedFrame(AdvanceWindow_Panel, 11);
        }

        /// <summary>
        /// Applies custom hover effects and styling to picture box controls.
        /// </summary>
        private void StylePictureBoxes()
        {
            // Applies a hover effect with rounded corners to the advanced settings button.
            UIHelper.ApplyHoverWithRounded(AdvanceWindow_pictureBox, AdvanceWindow_Hover_Panel, 5);
        }

        /// <summary>
        /// Configures basic form behavior settings (e.g., visibility in the taskbar).
        /// </summary>
        private void SetupFormBehavior()
        {
            this.ShowInTaskbar = false;
        }

        /// <summary>
        /// Attaches event handlers for input validation and placeholder logic on text fields.
        /// </summary>
        private void SetupInputFields()
        {
            // Attach key press handler for IP validation (digits, dot, colon)
            loginTextBox.KeyPress += TextBox_KeyAddress;

            // Attach key press handler to only allow digits for the port field.
            portTextBox.KeyPress += TextBox_KeyDigit;

            // Attach key press handler for password field (allows broader character set).
            passwordTextBox.KeyPress += TextBox_KeyPassword;

            // --- Placeholder Setup ---

            // 1. Assign placeholders to the Tag property for easy reference
            loginTextBox.Tag = IpPlaceholder;
            portTextBox.Tag = PortPlaceholder;
            passwordTextBox.Tag = PasswordPlaceholder;

            // 2. Attach Enter (focus) event handlers
            loginTextBox.Enter += TextBox_Enter;
            portTextBox.Enter += TextBox_Enter;
            passwordTextBox.Enter += TextBox_Enter;

            // 3. Attach Leave (blur) event handlers
            loginTextBox.Leave += TextBox_Leave;
            portTextBox.Leave += TextBox_Leave;
            passwordTextBox.Leave += TextBox_Leave;

            // 4. Manually trigger Leave event to set initial placeholder state for all fields
            // and manage password masking if the field is empty or filled.
            TextBox_Leave(loginTextBox, EventArgs.Empty);
            TextBox_Leave(portTextBox, EventArgs.Empty);
            TextBox_Leave(passwordTextBox, EventArgs.Empty);
        }

        /// <summary>
        /// Closes the form when the exit picture box is clicked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void exit_pictureBox_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // --- UI Controls Event Handlers ---

        /// <summary>
        /// KeyPress handler for the IP/Hostname field. 
        /// Restricts input to digits, dots (.), and colons (:).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The key press event data.</param>
        private void TextBox_KeyAddress(object sender, KeyPressEventArgs e)
        {
            // Allow digits, dot (.), colon (:), and control characters (e.g., Backspace).
            bool isAllowedChar = Regex.IsMatch(e.KeyChar.ToString(), @"^[0-9.:]+$");
            bool isControlChar = char.IsControl(e.KeyChar);

            if (!isControlChar && !isAllowedChar)
            {
                e.Handled = true; // Block letters and other symbols
            }
        }

        /// <summary>
        /// KeyPress handler for the Password field. 
        /// Allows a broad set of special characters for complex passwords.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The key press event data.</param>
        private void TextBox_KeyPassword(object sender, KeyPressEventArgs e)
        {
            // Allows alphanumeric, dots, hyphens, and common symbols for passwords.
            bool isAllowedChar = Regex.IsMatch(e.KeyChar.ToString(), @"^[a-zA-Z0-9\-_.!@#$%&*+=?]+$");
            bool isControlChar = char.IsControl(e.KeyChar);

            if (!isControlChar && !isAllowedChar)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// KeyPress handler for the Port field.
        /// Restricts input to only numeric digits.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The key press event data.</param>
        private void TextBox_KeyDigit(object sender, KeyPressEventArgs e)
        {
            bool isDigitChar = char.IsDigit(e.KeyChar); // Check if the character is a digit.
            bool isControlChar = char.IsControl(e.KeyChar); // Allow control characters (like Backspace).

            if (!isDigitChar && !isControlChar)
            {
                e.Handled = true; // Block the character input.
            }
        }

        /// <summary>
        /// Handles the click event to show the password (disables masking).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void PictureBox4_Click(object sender, EventArgs e)
        {
            PictureBox4.Visible = false; // Hide the 'hide' icon.
            passwordTextBox.UseSystemPasswordChar = false; // Show characters.
        }

        /// <summary>
        /// Handles the click event to hide the password (enables masking).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void PictureBox2_Click(object sender, EventArgs e)
        {
            PictureBox4.Visible = true; // Show the 'hide' icon.

            // Only mask characters if the placeholder is NOT currently shown
            if (passwordTextBox.Text != PasswordPlaceholder)
            {
                passwordTextBox.UseSystemPasswordChar = true; // Mask characters.
            }
        }

        /// <summary>
        /// Handles the Save Parameters button click, which saves the config, attempts connection, and prompts to close.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void saveParams_Button_Click(object sender, EventArgs e)
        {
            // 1. Check if anything was changed at all before proceeding.
            if (!HasConfigChanged())
            {
                // If nothing changed, we assume the user intended to close the window (like pressing 'Cancel' or 'X').
                // We just close the form without any messages or saving logic.
                this.Close();
                return;
            }

            // 2. If data changed, proceed with validation and saving.
            if (!SaveAndInitialize())
            {
                // Validation failed (e.g., empty IP or invalid Port), return early.
                return;
            }

            // 3. Display confirmation message box for CHANGED/SAVED data.
            DialogResult result = MessageBox.Show(
                "OBS settings successfully saved.\n\n" +
                "Close this window?\n" +
                "(Press 'OK' to close, 'Cancel' to keep the window open)",
                "Settings Saved",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information
            );

            if (result == DialogResult.OK)
            {
                this.Close();
            }
        }

        /// <summary>
        /// Handles the Test Connection button click, which saves the config and initiates a connection attempt.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void testConnection_Button_Click(object sender, EventArgs e)
        {
            if (!SaveAndInitialize())
            {
                // Validation failed, return early.
                return;
            }
        }

        /// <summary>
        /// Handles the click on the picture box to open the advanced settings window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void AdvanceWindow_pictureBox_Click(object sender, EventArgs e)
        {
            // Uses a helper to show the form modally or non-modally.

            if (ObsController.Instance.IsConnected)
            {
                UIHelper.ShowForm<AdvancedWindowForm>(this);
            }
            else
            {
                MessageBox.Show(
                    "OBS connection is not established.\n\n" +
                    "Please re-check your IP, Port, and Password.\n" +
                    "Ensure “Enable WebSocket Server” is checked in OBS.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // --- Placeholder Event Handlers ---

        /// <summary>
        /// Handles the Enter (focus) event for text boxes to hide the placeholder.
        /// </summary>
        private void TextBox_Enter(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            string placeholder = tb.Tag as string;

            // If the current text is the placeholder, clear it and set text color to Black
            if (tb.Text == placeholder)
            {
                tb.Text = "";
                tb.ForeColor = Color.Black;

                // Special handling for password field to enable masking
                if (tb == passwordTextBox)
                {
                    // Set masking based on the current state of the show/hide icon
                    // PictureBox4 being visible means the user wants the password HIDDEN.
                    tb.UseSystemPasswordChar = PictureBox4.Visible;
                }
            }
        }

        /// <summary>
        /// Handles the Leave (blur) event for text boxes to show the placeholder if empty.
        /// </summary>
        private void TextBox_Leave(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            string placeholder = tb.Tag as string;

            // If the text box is empty, show the placeholder with DimGray text
            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                tb.ForeColor = Color.DimGray;
                tb.Text = placeholder;

                // Special handling for password field: 
                // When showing placeholder, masking MUST be disabled.
                if (tb == passwordTextBox)
                {
                    tb.UseSystemPasswordChar = false;
                }
            }
        }


        // --- Core Logic Methods ---

        /// <summary>
        /// Compares the current values in the text fields with the initial configuration (_currentConfig).
        /// </summary>
        /// <returns>True if the data has changed, false otherwise.</returns>
        private bool HasConfigChanged()
        {
            // 1. Get current clean values (handling placeholders)
            string currentIp = (loginTextBox.Text == IpPlaceholder) ? string.Empty : loginTextBox.Text;
            string currentPortText = (portTextBox.Text == PortPlaceholder) ? string.Empty : portTextBox.Text;
            string currentPassword = (passwordTextBox.Text == PasswordPlaceholder) ? string.Empty : passwordTextBox.Text;

            // Try to parse the current port. If parsing fails, it's definitely a change/invalid data, 
            // but we'll focus on comparing against the stored integer port.
            int currentPort = 0;
            int.TryParse(currentPortText, out currentPort);

            // 2. Compare with the initial config (_currentConfig)
            // Note: We use StringComparison.Ordinal to ensure case-sensitive comparison.

            // Compare IP
            if (!currentIp.Equals(_currentConfig.Ip, StringComparison.Ordinal))
            {
                return true;
            }

            // Compare Port (If portText is empty, currentPort is 0. If _currentConfig.Port is 0, they match.)
            if (currentPort != _currentConfig.Port)
            {
                // This covers cases where the user types '0' or leaves it empty (parsed as 0), 
                // and the saved config has a non-zero port, or vice versa.
                return true;
            }

            // Compare Password
            if (!currentPassword.Equals(_currentConfig.Password, StringComparison.Ordinal))
            {
                return true;
            }

            // If we reach here, nothing important has changed.
            return false;
        }

        /// <summary>
        /// Validates the input fields, saves the OBS connection parameters, and initializes the connection.
        /// </summary>
        /// <returns>True if validation succeeded and configuration was saved/initialized; otherwise, false.</returns>
        private bool SaveAndInitialize()
        {
            // 1. Get text from fields, checking against placeholders.
            // If text is the placeholder, treat it as an empty string.
            string ip = (loginTextBox.Text == IpPlaceholder) ? string.Empty : loginTextBox.Text;
            string portText = (portTextBox.Text == PortPlaceholder) ? string.Empty : portTextBox.Text;
            string password = (passwordTextBox.Text == PasswordPlaceholder) ? string.Empty : passwordTextBox.Text;

            // 2. Validation: Ensure the Port is a valid integer AND the IP is not empty.
            if (string.IsNullOrWhiteSpace(ip))
            {
                MessageBox.Show(
                    "Please enter a valid IP address or hostname.",
                    "Input Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
            }

            if (!int.TryParse(portText, out int port) || port <= 0)
            {
                // Display error message for invalid or empty port input.
                MessageBox.Show(
                    "Please enter a valid port number (e.g., 4455).",
                    "Input Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return false;
            }

            // 3. Save configuration: Create and persist the new configuration object.
            OBSConnectionParams newConfig = new OBSConnectionParams // New configuration object to be saved
            {
                Ip = ip, // Use the cleaned IP value
                Port = port,
                Password = password // Use the cleaned password value
            };
            OBSConfigManager.Save(newConfig);

            // 4. Initialize and connect: Pass the new config to the OBS controller.
            ObsController.Instance.Initialize(newConfig);

            // 5. Update UI status immediately:
            // This ensures the UI reflects that the attempt has started, 
            // even before the asynchronous connection status event fires.
            OBSStatus_Changed(null, EventArgs.Empty);

            return true;
        }
    }
}