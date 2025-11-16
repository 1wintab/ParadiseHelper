using System.Drawing;
using System.Windows.Forms;

namespace ParadiseHelper.Tools.UITools
{
    /// <summary>
    /// Represents a custom <see cref="Panel"/> control that is truly transparent.
    /// <para>It allows controls and drawing logic placed underneath it to be fully visible,
    /// ideal for overlaying elements without obstructing the background.</para>
    /// </summary>
    public class TransparentPanel : Panel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransparentPanel"/> class.
        /// </summary>
        public TransparentPanel()
        {
            // Enables the control to handle transparent background colors correctly.
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            // Explicitly set the background color to transparent.
            BackColor = Color.Transparent;
        }

        /// <summary>
        /// Gets the required creation parameters when the control handle is created.
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                // Retrieve the base creation parameters.
                CreateParams cp = base.CreateParams;

                // Add the WS_EX_TRANSPARENT extended window style (0x20).
                // This forces Windows to draw the controls/form beneath the panel first, 
                // ensuring true transparency for this overlay panel.
                const int WS_EX_TRANSPARENT = 0x20;
                cp.ExStyle |= WS_EX_TRANSPARENT;

                // Returns the modified parameters.
                return cp;
            }
        }
    }
}