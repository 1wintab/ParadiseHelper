using ParadiseHelper.Tools.UITools;
using System;
using System.Windows.Forms;
using UI.Main_UI;

namespace ParadiseHelper.UI.MainUI
{
    /// <summary>
    /// Represents the main settings form for the application, inheriting from SmartForm.
    /// </summary>
    public partial class SettingsForm : SmartForm
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsForm"/> class.
        /// </summary>
        public SettingsForm()
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
            label4.Font = FontLoader.VAGWorld(20);
            label_Navmesh.Font = FontLoader.VAGWorld(20);
        }

        /// <summary>
        /// Applies visual styles and effects, such as rounded frames and fade-out.
        /// </summary>
        private void ApplyVisualStyle()
        {
            // Apply a fade-out effect for form closing.
            UIEffects.ApplyFadeOut(this);

            // Apply rounded frames to the main navigation panels.
            UIHelper.ApplyRoundedFrame(panel1, 20);
            UIHelper.ApplyRoundedFrame(panel2, 15);
            UIHelper.ApplyRoundedFrame(panel3, 15);
            UIHelper.ApplyRoundedFrame(panel4, 15);
            UIHelper.ApplyRoundedFrame(pnl_Navmesh, 15);

            // Apply rounded corners to the exit button picture box.
            UIHelper.ApplyRoundedCorners(exit_pictureBox, 10);
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
        private void exit_pictureBox_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // --- Navigation Logic ---

        /// <summary>
        /// Configures and attaches click handlers to groups of controls for form navigation.
        /// </summary>
        private void ConfigureNavigation()
        {
            // Configure click handlers to open the Path Manager form.
            UIHelper.AttachClickHandlers(
                new Control[] { panel2, pictureBox2, label2 },
                () => OpenPathManager()
            );

            // Configure click handlers to open the MaFiles form.
            UIHelper.AttachClickHandlers(
                new Control[] { panel3, pictureBox3, label3 },
                () => OpenMaFilesForm()
            );

            // Configure click handlers to open the Launch Parameters form.
            UIHelper.AttachClickHandlers(
                new Control[] { panel4, pictureBox4, label4 },
                () => OpenLaunchParametersForm()
            );

            // Configure click handlers to open the OBS Websocket form.
            UIHelper.AttachClickHandlers(
                new Control[] { pnl_Navmesh, pb_Navmesh, label_Navmesh },
                () => OBSWebsocketForm()
            );
        }

        /// <summary>
        /// Shows the Path Manager form.
        /// </summary>
        private void OpenPathManager()
        {
            // Displays the PathManagerForm, using this form as the parent.
            UIHelper.ShowForm<PathManagerForm>(this);
        }

        /// <summary>
        /// Shows the MaFiles form.
        /// </summary>
        private void OpenMaFilesForm()
        {
            // Displays the MaFilesForm, using this form as the parent.
            UIHelper.ShowForm<MaFilesForm>(this);
        }

        /// <summary>
        /// Shows the Launch Parameters form.
        /// </summary>
        private void OpenLaunchParametersForm()
        {
            // Displays the LaunchParametersForm, using this form as the parent.
            UIHelper.ShowForm<LaunchParametersForm>(this);
        }

        /// <summary>
        /// Shows the OBS Websocket form.
        /// </summary>
        private void OBSWebsocketForm()
        {
            // Displays the OBSWebsocketForm, using this form as the parent.
            UIHelper.ShowForm<OBSWebsocketForm>(this);
        }
    }
}