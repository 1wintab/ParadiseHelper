using System;
using System.Windows.Forms;
using ParadiseHelper;
using ParadiseHelper.Tools.UITools;

namespace UI.OBSWebsocket
{
    // This partial class contains methods related to visual styling and input field configuration.
    public partial class AdvancedWindowForm
    {
        /// <summary>
        /// Applies custom fonts to all UI elements for consistent styling.
        /// </summary>
        private void ApplyFont()
        {
            label1.Font = FontLoader.VAGWorld(20);
            label2.Font = FontLoader.VAGWorld(20);
            label8.Font = FontLoader.VAGWorld(9);
            label9.Font = FontLoader.VAGWorld(9);
            btnCreateScene.Font = FontLoader.VAGWorld(11);
            btnDeleteScene.Font = FontLoader.VAGWorld(11);
            btnRenameScene.Font = FontLoader.VAGWorld(11);
            btnApplyDefaultObsSettings.Font = FontLoader.VAGWorld(9);
            tvScenes.Font = FontLoader.VAGWorld(9); 
        }

        /// <summary>
        /// Applies overall visual styles and effects, including panel styling.
        /// </summary>
        private void ApplyVisualStyle()
        {
            UIEffects.ApplyFadeOut(this);
            StylePanels();
        }

        /// <summary>
        /// Applies rounded corners and frames to the main container UI panels.
        /// </summary>
        private void StylePanels()
        {
            UIHelper.ApplyRoundedCorners(exit_pictureBox, 10);
            UIHelper.ApplyRoundedFrame(panel1, 20); 
            UIHelper.ApplyRoundedFrame(panel4, 20); 
            UIHelper.ApplyRoundedFrame(panel5, 20); 
            UIHelper.ApplyRoundedFrame(panel7, 20);
        }

        /// <summary>
        /// Configures input fields, setting ReadOnly properties and focus behavior.
        /// </summary>
        private void SetupInputFields()
        {
            tbSceneName.ReadOnly = true;
            tbSceneName.TabStop = false;
            tbSceneName.Enter += PreventTextBoxFocus;

            tbSourceName.ReadOnly = true;
            tbSourceName.TabStop = false;
            tbSourceName.Enter += PreventTextBoxFocus;

            tbSourceWidth.ReadOnly = false;
            tbSourceWidth.TabStop = false;
            tbSourceWidth.Enter += PreventTextBoxFocus;

            tbSourceHeight.ReadOnly = false;
            tbSourceHeight.TabStop = false;
            tbSourceHeight.Enter += PreventTextBoxFocus;

            tbSourceTransform.ReadOnly = false;
            tbSourceTransform.TabStop = false;
            tbSourceTransform.Enter += PreventTextBoxFocus;
        }

        /// <summary>
        /// Prevents a TextBox from gaining focus and redirects focus to the TreeView.
        /// This stops the caret from blinking in read-only or display-only text boxes.
        /// </summary>
        private void PreventTextBoxFocus(object sender, EventArgs e)
        {
            // Redirect focus to the TreeView immediately
            tvScenes.Focus();
        }

        /// <summary>
        /// Handles the KeyPress event for text boxes to allow only OBS-compatible characters 
        /// (letters, numbers, and specific symbols).
        /// </summary>
        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow OBS-friendly characters for names/titles
            bool isAllowedChar = System.Text.RegularExpressions.Regex.IsMatch(e.KeyChar.ToString(), @"^[a-zA-Z0-9\-_.!@#$%&*+=?]+$");
            
            // Allow control characters (like backspace)
            bool isControlChar = char.IsControl(e.KeyChar);

            if (!isControlChar && !isAllowedChar)
            {
                e.Handled = true; // Block the character
            }
        }

        /// <summary>
        /// Handles the KeyPress event for text boxes to allow only digits.
        /// </summary>
        private void TextBox_KeyDigit(object sender, KeyPressEventArgs e)
        {
            // Allow digits
            bool isDigitChar = char.IsDigit(e.KeyChar);
            // Allow control characters (like backspace)
            bool isControlChar = char.IsControl(e.KeyChar);

            if (!isDigitChar && !isControlChar)
            {
                e.Handled = true; // Block the character
            }
        }
    }
}