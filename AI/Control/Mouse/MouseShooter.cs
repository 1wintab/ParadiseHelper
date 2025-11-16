using System.Runtime.InteropServices;
using ParadiseHelper.Tools.WinAPI;

namespace ParadiseHelper.AI.Control.Mouse
{
    /// <summary>
    /// Provides static methods to simulate mouse button presses (clicks) using the 
    /// Windows API's SendInput function, primarily used for "shooting" or activating game actions.
    /// </summary>
    public static class MouseShooter
    {
        // --- Custom Constants ---
        /// <summary>
        /// The cooldown time in milliseconds between shots in a burst fire sequence.
        /// </summary>
        private const int BurstCooldownMs = 150;

        // --- WinAPI Imports, Constants, and Structs are removed ---
        // --- They are now in NativeMethods ---

        // --- Public Methods ---

        /// <summary>
        /// Simulates a single, instant left mouse button click (press down immediately followed by release).
        /// </summary>
        public static void LeftClick()
        {
            // Use the struct from NativeMethods
            NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[2];

            // 1. Mouse Down event
            inputs[0].type = NativeMethods.INPUT_MOUSE; // Use constant
            inputs[0].mi.dwFlags = NativeMethods.MOUSEEVENTF_LEFTDOWN; // Use constant

            // 2. Mouse Up event
            inputs[1].type = NativeMethods.INPUT_MOUSE; // Use constant
            inputs[1].mi.dwFlags = NativeMethods.MOUSEEVENTF_LEFTUP; // Use constant

            // Send both events simultaneously
            // Use the function from NativeMethods
            NativeMethods.SendInput((uint)inputs.Length, ref inputs[0], Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }

        /// <summary>
        /// Alias for <see cref="LeftClick"/>, representing the action of firing a weapon.
        /// </summary>
        public static void Shoot()
        {
            LeftClick();
        }
    }
}