using System.Windows.Forms;
using Core;

namespace ParadiseHelper.AI.Control.KeyBoard.Binds.AutoBuy
{
    /// <summary>
    /// Responsible for programmatically executing the in-game auto-buy weapon command, 
    /// typically bound to a specific hotkey (e.g., F6).
    /// </summary>
    public class AutoBuyWeapons
    {
        /// <summary>
        /// Performs the auto-buy operation by pressing and releasing the assigned F6 key.
        /// </summary>
        /// <remarks>
        /// Relies on the external <c>KeyboardController</c> class for keyboard press emulation.
        /// </remarks>
        public void PerformAutoBuy()
        {
            // Press the F6 key
            KeyboardController.PressKey(Keys.F6);

            // Release the F6 key
            KeyboardController.ReleaseKey(Keys.F6);
        }
    }
}