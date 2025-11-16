using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using Core;
using ParadiseHelper.Tools.WinAPI;
using ParadiseHelper.OBS;
using ParadiseHelper.Tools.UITools;
using Tools.UITools;
using Microsoft.Extensions.Logging;
using System.Security.Policy;

namespace ParadiseHelper
{
    // NOTE: This class is split into multiple partial files for better code organization.
    // See MainForm.AccountUI.cs, MainForm.Events.cs, etc. for full implementation.

    /// <summary>
    /// The main form for the Paradise Helper application, responsible for UI initialization,
    /// account management visualization, and core logging functionality.
    /// </summary>
    public partial class MainForm : Form
    {
        // --- Fields and Constants ---

        // The dedicated logger instance
        private UILogger _logger;
        // Task used for asynchronous operations, primarily for the application launch sequence.
        private Task launchTask;
        // The default text shown in the log RichTextBox...
        private const string LogPlaceholderText = "Here your logs will appear...";
        // The standard file name for the current session's UI log.
        private const string SessionLogFileName = "session_ui_log.txt";
        // Full file path where the session log is written.
        private string _sessionLogFilePath;
        // Tracks the last known state of the OBS process to detect when it's closed.
        private bool _isObsProcessRunning = false;

        // (NEW) Flag to prevent re-entrancy in the OBS connection logic.
        private bool _isObsConnecting = false;

        // Event that fires when the user selects (or deselects) an account for AI control 
        // (the one indicated by the purple status circle).
        // Passes the 'login' of the selected account, or 'null' if deselected.
        public event Action<string> OnActiveAIAccountChanged;
        // Custom font used for the log display area.
        private Font _logCustomFont = FontLoader.BIPs(12, FontStyle.Regular);
        // --- Window Status Checking Fields ---

        // Timer used for periodically updating the status of accounts and game windows.
        private System.Windows.Forms.Timer _statusUpdateTimer;

        // The process name of the game (CS2) to monitor for window status.
        private const string GameProcessName = "cs2";

        // Cache for storing the last known status of each account to prevent visual flickering.
        private Dictionary<string, AccountVisualStatus> _lastAccountStatuses = new Dictionary<string, AccountVisualStatus>();

        // The standard size (in pixels) for the account status indicator circles.
        private const int StatusIndicatorSize = 15;

        // Defines the possible visual states for an account (Red, Green, or Purple/AI control).
        private enum AccountVisualStatus { Red, Green, Purple }

        // --- Form Lifecycle & Initialization ---

        /// <summary>
        /// Initializes a new instance of the MainForm class.
        /// Configures UI components, initializes logging, and sets up status monitoring.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            // Database initialization must occur before any data loading.
            InitDatabase();
            // Initialize and clear the session log file for the new session.
            InitSessionLog();
            // Set up connection and configuration for OBS integration.
            InitializeOBSManagment();

            // Start the periodic timer for updating account window statuses.
            InitializeStatusUpdater();

            // Apply application-wide custom fonts.
            ApplyFont();

            // Apply custom visual styling and UI effects.
            ApplyVisualStyle();
            // Enables double buffering for smoother panel and control rendering.
            EnableSmoothRendering();
            // Attaches event handlers specific to the account list panel.
            InitAccountPanel();
            // Attaches a focus-clearing click handler to all controls to prevent persistent focus rings.
            AttachClickToAllControls(this);
            // Prevents automatic scaling based on DPI changes.
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
        }

        /// <summary>
        /// Called when the form loads.
        /// Ensures account data is loaded and the form becomes visible.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            // Load account data into the UI
            LoadAccounts();
            this.BeginInvoke(new Action(() =>
            {
                // Ensure the form is visible after loading
                this.Visible = true;
            }));
            base.OnLoad(e);
        }

        /// <summary>
        /// Initializes the application's database connection and structure.
        /// </summary>
        private void InitDatabase()
        {
            DatabaseHelper.InitDatabase();
        }

        /// <summary>
        /// Initializes the session log file, clearing previous content and setting the file path.
        /// Includes error handling for file system operations.
        /// </summary>
        private void InitSessionLog()
        {
            try
            {
                this._sessionLogFilePath = Path.Combine(FilePaths.LogsDirectory, SessionLogFileName);
                if (File.Exists(this._sessionLogFilePath))
                {
                    File.WriteAllText(this._sessionLogFilePath, string.Empty);
                }
                else
                {
                    Directory.CreateDirectory(FilePaths.LogsDirectory);
                    File.WriteAllText(this._sessionLogFilePath, string.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not initialize session log file: {ex.Message}",
                    "Log Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                // Deactivate file logging upon failure
                this._sessionLogFilePath = null;
            }

            _logger = new UILogger(
                log_RichTextBox,
                _logCustomFont,
                _sessionLogFilePath,
                LogPlaceholderText
            );
        }

        /// <summary>
        /// Loads the OBS connection configuration and initializes the OBS controller instance.
        /// </summary>
        private void InitializeOBSManagment()
        {
            OBSConnectionParams obsConfig = OBSConfigManager.Load();
            ObsController.Instance.Initialize(obsConfig);
        }

        /// <summary>
        /// Sets up and starts the timer responsible for periodically checking and updating 
        /// the status of managed accounts and OBS.
        /// </summary>
        private void InitializeStatusUpdater()
        {
            _statusUpdateTimer = new System.Windows.Forms.Timer();
            // Set update interval to 2000 ms (2 seconds).
            _statusUpdateTimer.Interval = 2000;
            _statusUpdateTimer.Tick += UpdateAccountStatuses_Tick;

            // Get initial OBS status.
            _isObsProcessRunning = ObsProcessManager.IsObsProcessRunning();
            _statusUpdateTimer.Start();
        }

        /// <summary>
        /// Applies custom fonts to various UI elements (labels and buttons) across the form.
        /// </summary>
        private void ApplyFont()
        {
            label1.Font = FontLoader.VAGWorld(20);
            label2.Font = FontLoader.VAGWorld(20);

            addAccount_Button.Font = FontLoader.VAGWorld(18);
            deleteAccount_Button.Font = FontLoader.VAGWorld(18);
            editAccount_Button.Font = FontLoader.VAGWorld(18);
            resetAccounts_Button.Font = FontLoader.VAGWorld(18);

            log_RichTextBox.Font = _logCustomFont;
        }

        /// <summary>
        /// Applies various custom visual styles and configurations to the form and its controls.
        /// </summary>
        private void ApplyVisualStyle()
        {
            SetupFormStyle();
            StylePanels();
            StylePictureBoxes();
            ConfigureAccountPanel();
            HideTextBoxCaret();
            ConfigureLogRichTextBox();
        }

        /// <summary>
        /// Sets up the main form's window properties (e.g., fixed size, centered, no maximize).
        /// </summary>
        private void SetupFormStyle()
        {
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            // Apply custom background style helper (e.g., a blended or themed background).
            UIHelper.ClearFormBackground(this);
            // Apply a visual fade-in effect when the form first appears.
            UIEffects.ApplyFadeIn(this);
        }

        /// <summary>
        /// Applies custom rounded frames and styling to various container panels.
        /// </summary>
        private void StylePanels()
        {
            UIHelper.ApplyRoundedFrame(Logs_Panel, 15);
            UIHelper.ApplyRoundedFrame(AccountsTitle_Panel, 15);
            UIHelper.ApplyRoundedFrame(ListTools_Panel, 11);
            UIHelper.ApplyRoundedFrame(SettingsAndTools_Panel, 11);
            UIHelper.ApplyRoundedFrame(LogTools_Panel, 11);
            UIHelper.ApplyRoundedFrame(AI_Panel, 11);
            // Applies rounded corners specifically to the left side of the panel.
            UIHelper.ApplyLeftRoundedCorners(accountPanel, 5);
        }

        /// <summary>
        /// Applies custom hover effects and rounded corners to the PictureBox controls used as tool icons.
        /// </summary>
        private void StylePictureBoxes()
        {
            UIHelper.ApplyHoverWithRounded(selectAllList_pictureBox, panel5, 5);
            UIHelper.ApplyHoverWithRounded(unselectAllList_pictureBox, panel6, 5);
            UIHelper.ApplyHoverWithRounded(updateList_picturebox, panel7, 5);
            UIHelper.ApplyHoverWithRounded(windowMap_pictureBox, panel8, 5);
            UIHelper.ApplyHoverWithRounded(cfg_pictureBox, panel9, 5);
            UIHelper.ApplyHoverWithRounded(settings_pictureBox, panel10, 5);
            UIHelper.ApplyHoverWithRounded(zoomLog_pictureBox, panel14, 5);
            UIHelper.ApplyHoverWithRounded(saveLog_pictureBox, panel12, 5);
            UIHelper.ApplyHoverWithRounded(deleteLog_pictureBox, panel13, 5);
            UIHelper.ApplyHoverWithRounded(AI_CS2_pictureBox, panel18, 5);
        }

        /// <summary>
        /// Configures the behavior of the panel containing the list of accounts, primarily for scrolling.
        /// </summary>
        private void ConfigureAccountPanel()
        {
            accountPanel.AutoScroll = true;
            accountPanel.HorizontalScroll.Enabled = false;
            accountPanel.HorizontalScroll.Visible = false;
            accountPanel.AutoScrollMargin = new Size(0, 0);
            accountPanel.AutoScrollMinSize = new Size(0, 0);
        }

        /// <summary>
        /// Enables double buffering for performance-heavy controls to prevent screen flickering
        /// during redrawing and updates.
        /// </summary>
        private void EnableSmoothRendering()
        {
            // Helper method to set the DoubleBuffered property via reflection.
            void SetDoubleBuffered(Control c)
            {
                if (c == null) return;
                try
                {
                    typeof(Control)
                        .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?
                        .SetValue(c, true);
                }
                catch { /* Ignore exceptions if DoubleBuffered property is inaccessible. */ }
            }

            // Enable buffering for controls that frequently redraw or have complex content.
            SetDoubleBuffered(accountPanel);         // Account list panel
            SetDoubleBuffered(log_RichTextBox);
            // Log display (fixes log flickering)

            // Background blur/image control, often a source of flicker.
            SetDoubleBuffered(pictureBox4);
        }

        /// <summary>
        /// Attaches a resize event handler to the account panel to ensure the account list
        /// is reloaded/resized correctly when the panel dimensions change.
        /// </summary>
        private void InitAccountPanel()
        {
            accountPanel.Resize += (s, e) => LoadAccounts();
        }

        /// <summary>
        /// Hides the blinking caret (text cursor) in the log RichTextBox by calling the P/Invoke
        /// method on various events (focus, click).
        /// </summary>
        private void HideTextBoxCaret()
        {
            NativeMethods.HideCaret(log_RichTextBox.Handle);
            log_RichTextBox.GotFocus += (s, e) => NativeMethods.HideCaret(log_RichTextBox.Handle);
            log_RichTextBox.MouseDown += (s, e) => NativeMethods.HideCaret(log_RichTextBox.Handle);
        }

        /// <summary>
        /// Configures properties for the log RichTextBox, making it read-only, setting 
        /// scrollbars, and attaching focus handlers.
        /// </summary>
        private void ConfigureLogRichTextBox()
        {
            log_RichTextBox.ReadOnly = true;
            log_RichTextBox.ScrollBars = RichTextBoxScrollBars.ForcedVertical;
            log_RichTextBox.BackColor = Color.White;
            log_RichTextBox.WordWrap = false;
            log_RichTextBox.ScrollBars = RichTextBoxScrollBars.Both;

            NativeMethods.HideCaret(log_RichTextBox.Handle);
            
            // Ensure the caret remains hidden on all relevant mouse/keyboard interactions.
            log_RichTextBox.GotFocus += (s, e) => NativeMethods.HideCaret(log_RichTextBox.Handle);
            log_RichTextBox.MouseDown += (s, e) => NativeMethods.HideCaret(log_RichTextBox.Handle);
            log_RichTextBox.Enter += (s, e) => NativeMethods.HideCaret(log_RichTextBox.Handle);
            log_RichTextBox.MouseUp += (s, e) => NativeMethods.HideCaret(log_RichTextBox.Handle);
            log_RichTextBox.KeyDown += (s, e) => NativeMethods.HideCaret(log_RichTextBox.Handle);
            log_RichTextBox.KeyUp += (s, e) => NativeMethods.HideCaret(log_RichTextBox.Handle);
            log_RichTextBox.MouseMove += (s, e) => NativeMethods.HideCaret(log_RichTextBox.Handle);
            
            // Attach placeholder text visibility handlers.
            log_RichTextBox.GotFocus += log_RichTextBox_GotFocus;
            log_RichTextBox.LostFocus += log_RichTextBox_LostFocus;

            ShowPlaceholder();
        }

        // --- Focus Handling Methods ---

        /// <summary>
        /// Handles click events across the form to forcibly clear the focus from the log box,
        /// ensuring no control maintains the default focus ring unless explicitly needed.
        /// </summary>
        private void HandleDeepFocusClearing(object sender, EventArgs e)
        {
            Control activeControl = this.ActiveControl;
            // If the log box is active, clear focus entirely.
            if (activeControl == log_RichTextBox)
            {
                this.ActiveControl = null;
            }
            // If another control was clicked, explicitly assign focus to it.
            else if (activeControl != null)
            {
                this.ActiveControl = (Control)sender;
            }
        }

        /// <summary>
        /// Recursively attaches the focus-clearing click handler to all child controls 
        /// within the given container, skipping the log RichTextBox.
        /// </summary>
        private void AttachClickToAllControls(Control container)
        {
            foreach (Control control in container.Controls)
            {
                // Skip log_RichTextBox to prevent focus conflict
                if (control == log_RichTextBox)
                {
                    continue;
                }

                control.Click += HandleDeepFocusClearing;
                // Recurse for nested container controls.
                if (control.HasChildren)
                {
                    AttachClickToAllControls(control);
                }
            }
        }
    }
}