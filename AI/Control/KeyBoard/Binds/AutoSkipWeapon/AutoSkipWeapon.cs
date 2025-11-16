using System.Windows.Forms;

namespace ParadiseHelper.AI.Control.KeyBoard.Binds.AutoSkipWeapon
{
    // Executes the automated keyboard command to quickly switch to the primary weapon slot.
    public class AutoSkipWeapon
    {
        // Presses and releases the '1' key to select the main weapon.
        public void PerformAutoSkipToMainWeapon()
        {
            // Press the '1' key
            KeyboardController.PressKey(Keys.D1);

            // Release the '1' key
            KeyboardController.ReleaseKey(Keys.D1);
        }
    }
}