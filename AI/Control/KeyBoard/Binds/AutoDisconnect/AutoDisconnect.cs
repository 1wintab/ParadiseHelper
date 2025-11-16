using System.Windows.Forms;
using Core;

namespace ParadiseHelper.AI.Control.KeyBoard.Binds.AutoDisconnect
{
    /// <summary>
    /// Responsible for programmatically executing the in-game disconnect command, 
    /// typically bound to a specific hotkey (e.g., F5).
    /// </summary>
    public class AutoDisconnect
    {
        /// <summary>
        /// Executes the automated keyboard command to trigger the in-game 'disconnect' function.
        /// </summary>
        /// <remarks>
        /// Relies on the external <c>KeyboardController</c> class for keyboard press emulation.
        /// </remarks>
        public void PerformAutoDisconnect()
        {
            // Press the F5 key
            KeyboardController.PressKey(Keys.F5);

            // Release the F5 key
            KeyboardController.ReleaseKey(Keys.F5);
        }
    }
}