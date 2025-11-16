using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using Core;
using UI.Main_UI;
using ParadiseHelper.Tools.UITools;

namespace ParadiseHelper.UI.MainUI
{
    public partial class MaFilesForm : SmartForm
    {
        private static ParcerForm _parcerFormInstance;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaFilesForm"/> class.
        /// </summary>
        public MaFilesForm()
        {
            InitializeComponent();

            ApplyFont();
            ApplyVisualStyle();

            ConfigureNavigation();

            SetupFormBehavior();
        }

        // --- Form Lifecycle Overrides ---

        /// <summary>
        /// Overrides the default paint event to draw a custom border around the form.
        /// </summary>
        /// <param name="e">A <see cref="PaintEventArgs"/> that contains the event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Draws a custom border for the form.
            UIHelper.DrawFormBorder(e, this);
        }


        // --- Initialization Helpers ---

        /// <summary>
        /// Applies the custom VAGWorld font to specific labels on the form.
        /// </summary>
        private void ApplyFont()
        {
            label1.Font = FontLoader.VAGWorld(20);
            label2.Font = FontLoader.VAGWorld(20);
            label3.Font = FontLoader.VAGWorld(20);
        }

        /// <summary>
        /// Applies visual styles and effects, such as rounded corners and fade-out.
        /// </summary>
        private void ApplyVisualStyle()
        {
            // Apply a fade-out effect for form closing.
            UIEffects.ApplyFadeOut(this);

            // Apply rounded frames to the main panels.
            UIHelper.ApplyRoundedFrame(panel1, 15);
            UIHelper.ApplyRoundedFrame(panel2, 15);
            UIHelper.ApplyRoundedFrame(panel3, 15);

            // Apply rounded corners to exit and navigation picture boxes.
            UIHelper.ApplyRoundedCorners(exit_pictureBox, 10);
            UIHelper.ApplyRoundedCorners(pictureBox4, 10);
            UIHelper.ApplyRoundedCorners(pictureBox5, 10);
        }

        /// <summary>
        /// Configures general form behavior settings.
        /// </summary>
        private void SetupFormBehavior()
        {
            // Prevents the form from appearing in the Windows taskbar.
            this.ShowInTaskbar = false;
        }

        // --- UI Controls Event Handlers ---

        /// <summary>
        /// Handles the click event for the exit picture box to close the form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Exit_pictureBox_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // --- Navigation Logic ---

        /// <summary>
        /// Attaches click event handlers to groups of controls to enable navigation actions.
        /// </summary>
        private void ConfigureNavigation()
        {
            // Configure click handlers for the 'MaFiles 2FA' action.
            UIHelper.AttachClickHandlers(
                new Control[] { panel2, pictureBox2, label2, pictureBox5 },
                () => MaFiles2FA()
            );

            // Configure click handlers for the 'Open Parser' action.
            UIHelper.AttachClickHandlers(
                new Control[] { panel1, pictureBox1, label1, pictureBox4 },
                () => OpenParcerMaFiles()
            );
        }

        /// <summary>
        /// Opens the directory for Steam maFiles (2FA).
        /// Creates a configuration directory if the maFiles directory is missing.
        /// </summary>
        private void MaFiles2FA()
        {
            // Check if the maFiles directory exists.
            if (!Directory.Exists(FilePaths.Standard.MaFilesDirectory))
            {
                // Create the config directory.
                Directory.CreateDirectory(FilePaths.Standard.Settings.ConfigDirectory);
            }

            // Open the maFiles directory in Windows Explorer.
            Process.Start("explorer.exe", FilePaths.Standard.MaFilesDirectory);
        }

        /// <summary>
        /// Launches the internal 'ParcerForm' now that it is integrated as a DLL.
        /// </summary>
        private void OpenParcerMaFiles()
        {
            // 1. Check if the form instance exists and is not disposed.
            if (_parcerFormInstance != null && !_parcerFormInstance.IsDisposed)
            {
                // The form is already open. Show a warning and bring it to the foreground.
                _parcerFormInstance.Activate();

                MessageBox.Show(
                    "The Parser is already open. Only one instance can be running.",
                    "Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            try
            {
                // 2. Create a new instance of the form.
                _parcerFormInstance = new ParcerForm();

                // 3. Subscribe to the FormClosed event to clear the reference when it closes.
                _parcerFormInstance.FormClosed += (sender, e) => _parcerFormInstance = null;

                _parcerFormInstance.Show();
            }
            catch (Exception ex)
            {
                // Handle initialization errors for the form (in case of issues with the DLL reference)
                MessageBox.Show(
                    $"Error launching utility: {ex.Message}", 
                    "Loading Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error
                );

                // Clear the reference in case of an initialization error.
                _parcerFormInstance = null;
            }
        }
    }
}