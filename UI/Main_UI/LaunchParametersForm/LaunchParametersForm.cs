using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using Data.Settings.LaunchParameters;

namespace ParadiseHelper.UI.MainUI
{
    // NOTE: This class is split into multiple partial files for better code organization.
    // See LaunchParametersForm.EventHandlers.cs, LaunchParametersForm.Logic.cs, etc. for full implementation.

    /// <summary>
    /// Manages the launch parameters for different applications (e.g., CS2, Steam)
    /// and different launch modes (e.g., Default, AICore).
    /// </summary>
    public partial class LaunchParametersForm : SmartForm
    {
        // --- Fields ---

        // Prevents re-entrant calls when changing the launch mode.
        private bool isModeChanging = false;

        // Flag to prevent event loops during internal UI updates (e.g., setting text).
        private bool isInternalChange = false;

        // Caches the loaded state for each application and mode combination (e.g., "cs2_AICore").
        private readonly Dictionary<string, AppParamsState> _appStates = new Dictionary<string, AppParamsState>();

        // Placeholder text for the main parameters text box when empty.
        private const string Placeholder_LaunchParams = "Here will be your launch parameters...";

        // Placeholder text for the main parameters text box when no app is selected.
        private const string Placeholder_SelectApp = "Please select an application...";

        // Default value for resolution text boxes (not currently used, but was present).
        private const int DefaultResolutionValue = 300;

        // Tracks the currently selected application ("cs2" or "steam").
        private string selectedApp = null;

        // Flag to prevent re-entry into the FormClosing event handler.
        private bool isHandlingClose = false;

        // Flag to indicate that form closing was confirmed via a button (e.g., "Save & Exit").
        private bool _isCloseVerified = false;

        /// <summary>
        /// Defines actions that require permission checks based on the current mode.
        /// </summary>
        private enum LaunchAction { WindowedMode, ResolutionChange }

        // Required launch parameters for Steam to ensure correct functionality.
        private readonly List<string> steamRequiredParams = new List<string>
        {
            "-language english",
            "-no-cef-sandbox"
        };

        // Required parameters for CS2 when in AI Core Mode.
        private readonly List<string> aiModeRequiredParams = new List<string>
        {
            $"+exec {AutoAddtionCfgCS2.AI_CONFIG_FILENAME}"
        };

        // List of parameters to remove for security or compatibility reasons.
        private readonly List<string> blacklistedParams = new List<string>
        {
            "login",
            "log",
            "password",
            "pass",
            "-noreactlogin",
            "-language russian",
            "-language ukrainian"
        };

        // Parameter flags related to window mode, used for parsing/filtering.
        private static readonly string[] WindowModeFlags = new[]
        {
            "-windowed",
            "-fullscreen",
            "-noborder"
        };

        // Parameter flags related to resolution, used for parsing/filtering.
        private static readonly string[] ResolutionFlags = new[]
        {
            "-w",
            "+w",
            "-width",
            "+width",
            "-h",
            "+h",
            "-height",
            "+height"
        };

        // Array of placeholder texts for easy checking.
        private static readonly string[] PlaceholderTexts = new[]
        {
            Placeholder_LaunchParams,
            Placeholder_SelectApp
        };

        // --- Constructor ---

        /// <summary>
        /// Initializes a new instance of the <see cref="LaunchParametersForm"/>.
        /// </summary>
        public LaunchParametersForm()
        {
            InitializeComponent();
            ApplyFont();
            ApplyVisualStyle();
            SetupFormBehavior();
        }

        // --- Form Lifecycle Overrides ---

        /// <summary>
        /// Overrides the OnPaint event to draw a custom border around the form.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            UIHelper.DrawFormBorder(e, this);
        }

        /// <summary>
        /// Overrides the OnLoad event to initialize form state, load parameters,
        /// and set up initial UI highlighting.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            EnsureRequiredSteamParams();

            // Set tags for easy identification in click events.
            steam_panel.Tag = "steam";
            cs2_panel.Tag = "cs2";

            this.MouseDown += LaunchParametersForm_MouseDown;

            InitializeModeSwitcher();

            // Load parameters for "cs2" by default on form start.
            LoadLaunchParams("cs2");

            // Highlight the "cs2" panel since its parameters are loaded.
            UpdateAppHighlight(cs2_panel);

            params_richTextBox.ReadOnly = false;
        }

        // --- Form Closing Logic ---

        /// <summary>
        /// Checks for unsaved changes and prompts the user to save, discard, or cancel.
        /// </summary>
        /// <returns>True if the form should close, false if closing should be cancelled.</returns>
        private bool HandleCloseRequest()
        {
            UpdateStateFromUI(selectedApp);
            if (!_appStates.Values.Any(s => s.HasChanges()))
            {
                return true; // No changes, OK to close.
            }

            var result = MessageBox.Show(
                this,
                "You have unsaved changes. Would you like to save them before closing?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning
            );
            switch (result)
            {
                case DialogResult.Yes:
                    return SaveChanges(); // User wants to save. Close only if saving is successful.
                case DialogResult.No:
                    return true; // User discards changes. OK to close.
                case DialogResult.Cancel:
                    return false; // User cancelled. Do not close.
                default:
                    return false;
            }
        }

        /// <summary>
        /// Handles the FormClosing event (e.g., clicking the 'X' button).
        /// </summary>
        private void LaunchParametersForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If closing was verified by a button (Save & Exit), skip the dialog.
            if (_isCloseVerified)
            {
                return;
            }

            // Prevent re-entry if already handling.
            if (isHandlingClose) return;

            if (e.CloseReason == CloseReason.UserClosing)
            {
                isHandlingClose = true;
                try
                {
                    // Run the check logic. If it returns false, cancel the close event.
                    if (!HandleCloseRequest())
                    {
                        e.Cancel = true;
                    }
                }
                finally
                {
                    isHandlingClose = false;
                }
            }
        }
    }
}