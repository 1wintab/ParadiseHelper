using System;
using System.Windows.Forms;
using ParadiseHelper.Tools;
using ParadiseHelper.Tools.UITools;
using UI.AI_UI;
using ParadiseHelper.OBS;

/// <summary>
/// The main AI control form, responsible for managing the AI debugger window
/// and handling activation state and hotkeys.
/// </summary>
namespace ParadiseHelper.UI.MainUI
{
    public partial class AIForm : SmartForm
    {
        // Reference to the external window used for debugging AI vision and checks.
        private WindowCheckVision _windowCheckVisionForm;

        // Handler for custom hotkeys (e.g., Ctrl+B) to toggle the overlay.
        private RegisterHotKeyHandler _hotKeyHandlerB;
        
        // Timer instance for potential future frame rate or delay logic.
        private readonly System.Windows.Forms.Timer _frameTimer = new System.Windows.Forms.Timer();
        
        // Indicates whether a user account with AI configuration is currently active in the main application.
        private bool _isAIAccountActive = false;

        /// <summary>
        /// Initializes a new instance of the AIForm class.
        /// </summary>
        public AIForm()
        {
            InitializeComponent();
            ApplyFont();
            ApplyVisualStyle();
            SetupFormBehavior();
        }

        /// <summary>
        /// Overrides the OnPaint event to draw a custom border around the form.
        /// </summary>
        /// <param name="e">Provides the Graphics object used to paint and the clip rectangle.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            UIHelper.DrawFormBorder(e, this);
        }

        // --- Event Handlers and Message Processing ---

        /// <summary>
        /// Handles the form loading, setting up the overlay toggle logic and hotkey handler.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Subscribes to the CheckedChanged event to handle overlay toggling.
            CheckBoxOverlay.CheckedChanged += CheckBoxOverlay_CheckedChanged;
            // Initializes the hotkey handler for Ctrl+B to toggle the overlay checkbox state.
            _hotKeyHandlerB = new RegisterHotKeyHandler(this.Handle, Keys.B, RegisterHotKeyHandler.KeyModifiers.Control, () =>
            {
                CheckBoxOverlay.Checked = !CheckBoxOverlay.Checked;
            });
        }

        /// <summary>
        /// Processes Windows messages, specifically to capture and process hotkey events.
        /// </summary>
        /// <param name="m">The Windows message to process.</param>
        protected override void WndProc(ref Message m)
        {
            // Delegates the hotkey message processing to the registered handler.
            _hotKeyHandlerB?.ProcessHotKey(ref m);
            base.WndProc(ref m);
        }

        /// <summary>
        /// Handles form closing events, disposing of the hotkey and safely closing the debug window.
        /// </summary>
        /// <param name="e">The event data associated with the form closing event.</param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Unsubscribe from the checkbox event to prevent re-entrant calls or unwanted behavior during shutdown.
            CheckBoxOverlay.CheckedChanged -= CheckBoxOverlay_CheckedChanged;

            // Ensure the overlay is deactivated before the form fully closes.
            if (_isAIAccountActive || CheckBoxOverlay.Checked)
            {
                CheckBoxOverlay.Checked = false;
            }

            // Safely close the child debugger form.
            CloseWindowCheckVisionFormSafe();

            // Dispose of the hotkey handler to release the key combination from the system.
            _hotKeyHandlerB?.Dispose();

            // Disown the child form by setting its Owner to null.
            // This prevents the SmartForm base class from trying to close it again.
            if (_windowCheckVisionForm != null)
            {
                _windowCheckVisionForm.Owner = null;
            }

            base.OnFormClosing(e);
        }

        /// <summary>
        /// Handles the click event for the Exit picture box, closing the main form.
        /// </summary>
        /// <param name="sender">The source of the event (the PictureBox).</param>
        /// <param name="e">The event data.</param>
        private void Exit_pictureBox_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // --- Core Logic Methods ---

        /// <summary>
        /// Updates the internal state based on whether an AI-enabled account is running in the main application.
        /// If the account becomes inactive while the debug window is open, it forces the window closed.
        /// </summary>
        /// <param name="activeLogin">The login string of the active AI account, or null/empty if none is active.</param>
        public void UpdateBotActivationState(string activeLogin)
        {
            // 1. Update the internal state field based on the login status (checks for null or empty).
            _isAIAccountActive = !string.IsNullOrEmpty(activeLogin);

            // 2. If the AI account was disabled, force-uncheck the overlay checkbox to close the debug window.
            if (!_isAIAccountActive)
            {
                CheckBoxOverlay.Checked = false;
            }
        }

        /// <summary>
        /// Safely creates and displays the AI debug window (WindowCheckVision).
        /// If the window already exists, it brings it to the front.
        /// </summary>
        private void CreateWindowCheckVisionFormSafe()
        {
            // Check if the form is already instantiated and not disposed. If so, bring it to the front.
            if (_windowCheckVisionForm != null && !_windowCheckVisionForm.IsDisposed)
            {
                _windowCheckVisionForm.BringToFront();
                return;
            }

            try
            {
                // Instantiate the debug form.
                _windowCheckVisionForm = new WindowCheckVision();

                // Set up an event handler to automatically uncheck the overlay box
                // if the debug form is manually closed by the user.
                _windowCheckVisionForm.FormClosed += WindowCheckVisionForm_FormClosed;
                _windowCheckVisionForm.Show();
            }
            catch (Exception ex)
            {
                // Handle potential errors during form creation (e.g., UI thread issues).
                MessageBox.Show(
                    $"Error creating AI window: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                // Reset the checkbox state on error to reflect the failure.
                CheckBoxOverlay.Checked = false;
            }
        }

        /// <summary>
        /// Safely closes and cleans up the AI debug window (WindowCheckVision) if it is currently open.
        /// </summary>
        private void CloseWindowCheckVisionFormSafe()
        {
            // 1. Exit if the form reference is null.
            if (_windowCheckVisionForm == null)
            {
                return;
            }

            // 2. Store the reference locally to avoid thread issues during cleanup.
            var formToRelease = _windowCheckVisionForm;

            // 3. Unsubscribe from the event to prevent memory leaks and dangling references.
            formToRelease.FormClosed -= WindowCheckVisionForm_FormClosed;

            // 4. Nullify the class field, guaranteeing the next request creates a new instance.
            _windowCheckVisionForm = null;

            // 5. Close the form only if it hasn't been closed/disposed already.
            if (formToRelease != null && !formToRelease.IsDisposed)
            {
                formToRelease.Close();
            }

            // 6. Use MemoryHelper to forcefully clear the form and its resources from memory.
            Common.Helpers.Tools.MemoryTools.MemoryHelper.EnsureFormRelease(formToRelease, maxAttempts: 10, delayMs: 300);
        }

        /// <summary>
        /// Handles the logic when the overlay checkbox state changes.
        /// Performs checks for AI activation and OBS connectivity before launching the debug window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private async void CheckBoxOverlay_CheckedChanged(object sender, EventArgs e)
        {
            if (CheckBoxOverlay.Checked)
            {
                // --- Check 1: Is an AI-enabled account active in MainForm? ---
                if (!_isAIAccountActive)
                {
                    // 1. Show a detailed warning message to the user.
                    MessageBox.Show(
                        "You must activate an account for AI first:\n\n" +
                        "1. Select the required account in the Main Menu.\n" +
                        "2. Check the 'Run with AI cfg' option.\n" +
                        "3. Launch the account and wait for the status to turn purple.\n" +
                        "4. Check the Action Checkbox next to the purple account.\n" +
                        "5. Try again to click 'Open AI Debug Form\'.",
                        "AI Account Not Activated",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    
                    // 2. Force-uncheck the box since activation failed.
                    CheckBoxOverlay.Checked = false;
                    return;
                }

                // ---  Check 2: Is the OBS connection established? ---
                if (!ObsController.Instance.IsConnected)
                {
                    MessageBox.Show(
                        "OBS connection is not established.\n\n" +
                        "The connection attempt is in progress (up to 15s).\n\n" +
                        "If it fails, please verify:\n" +
                        "1. OBS is running.\n" +
                        "2. Connection details (IP, Port, Password) are correct.\n" +
                        "3. OBS WebSocket Server is enabled.",
                        "OBS Not Connected",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    
                    CheckBoxOverlay.Checked = false; 
                    return;
                }

                // --- Check 3: Is the OBS Virtual Camera active? ---
                bool isVirtualCamStarted = false;
                try
                {
                    isVirtualCamStarted = await ObsController.Instance.IsVirtualCamStartedAsync();
                }
                catch (Exception ex)
                {
                    // Handle exceptions if OBS connection is lost mid-check
                    MessageBox.Show(
                        $"Error checking OBS Virtual Camera status: {ex.Message}\n\n" +
                        "Please ensure OBS is still running and connected.",
                        "OBS Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );

                    CheckBoxOverlay.Checked = false;
                    return;
                }

                if (!isVirtualCamStarted)
                {
                    MessageBox.Show(
                        "OBS Virtual Camera is not running.\n\n" +
                        "The main form is responsible for starting the camera.\n" +
                        "Please uncheck and re-check the account's action box to re-initialize the connection.",
                        "Virtual Camera Not Active",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    CheckBoxOverlay.Checked = false;
                    return;
                }

                // --- All checks passed ---
                // 3. If the checks passed, safely create and show the debug window.
                CreateWindowCheckVisionFormSafe();
            }
            else
            {
                // If unchecking, safely close the debug window.
                CloseWindowCheckVisionFormSafe();
            }
        }

        /// <summary>
        /// Event handler for when the debug window is closed (e.g., manually by the user).
        /// Automatically unchecks the overlay box to keep the UI in sync.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void WindowCheckVisionForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Sync the checkbox state if the form was closed manually.
            if (CheckBoxOverlay.Checked)
            {
                CheckBoxOverlay.Checked = false;
            }
        }

        // --- UI/Styling Methods ---

        /// <summary>
        /// Applies the custom VAG World font to all relevant labels on the form.
        /// </summary>
        private void ApplyFont()
        {
            label1.Font = FontLoader.VAGWorld(20);
            label2.Font = FontLoader.VAGWorld(20);
            label3.Font = FontLoader.VAGWorld(20);
            label4.Font = FontLoader.VAGWorld(20);
            label5.Font = FontLoader.VAGWorld(20);
            label6.Font = FontLoader.VAGWorld(20);
            label7.Font = FontLoader.VAGWorld(20);
            label8.Font = FontLoader.VAGWorld(20);
            label9.Font = FontLoader.VAGWorld(20);
            label10.Font = FontLoader.VAGWorld(20);
            label11.Font = FontLoader.VAGWorld(20);
            label13.Font = FontLoader.VAGWorld(20);
        }

        /// <summary>
        /// Applies custom visual effects and rounded corners to the form's panels and controls.
        /// </summary>
        private void ApplyVisualStyle()
        {
            UIEffects.ApplyFadeOut(this);

            // Apply rounded frame effects to various UI panels.
            UIHelper.ApplyRoundedFrame(panel1, 9);
            UIHelper.ApplyRoundedFrame(panel2, 15);
            UIHelper.ApplyRoundedFrame(panel3, 9);
            UIHelper.ApplyRoundedFrame(panel5, 9);
            UIHelper.ApplyRoundedFrame(panel6, 9);
        }

        /// <summary>
        /// Sets up specific properties for the form's runtime behavior.
        /// </summary>
        private void SetupFormBehavior()
        {
            // Prevents the form from appearing in the Windows taskbar.
            this.ShowInTaskbar = false;
        }
    }
}