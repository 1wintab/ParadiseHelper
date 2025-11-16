using System;
using System.Windows.Forms;
using ParadiseHelper;
using ParadiseHelper.Tools.UITools;

namespace UI.Main_UI
{
    /// <summary>
    /// A form designed to display and manage navigation mesh data (Navmesh).
    /// This form inherits from SmartForm for custom UI behavior.
    /// </summary>
    public partial class NavmeshDataForm : SmartForm
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NavmeshDataForm"/> class.
        /// Performs initialization of components, fonts, visual styles, and form behavior.
        /// </summary>
        public NavmeshDataForm()
        {
            InitializeComponent();

            ApplyFont();
            ApplyVisualStyle();

            SetupFormBehavior();
        }

        // --- Form lifecycle overrides ---

        /// <summary>
        /// Handles the paint event to draw a custom border around the form using UIHelper.
        /// </summary>
        /// <param name="e">A <see cref="PaintEventArgs"/> that contains the event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            UIHelper.DrawFormBorder(e, this);
        }

        // --- Initialization helpers ---

        /// <summary>
        /// Applies custom fonts to the necessary UI controls.
        /// </summary>
        private void ApplyFont()
        {
            label_Navmesh.Font = FontLoader.VAGWorld(20);
        }

        /// <summary>
        /// Applies custom visual styles, including fade-out effects and rounded frames/corners.
        /// </summary>
        private void ApplyVisualStyle()
        {
            UIEffects.ApplyFadeOut(this);

            // Apply rounded frame to the main navmesh panel
            UIHelper.ApplyRoundedFrame(pnl_Navmesh, 15);

            // Apply rounded corners to the exit picture box
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

        // --- UI Controls Handlers ---

        /// <summary>
        /// Handles the click event for the exit picture box, closing the current form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void exit_pictureBox_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}