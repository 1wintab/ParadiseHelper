using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using ParadiseHelper.Tools.WinAPI;

namespace ParadiseHelper.AI.Control.Mouse
{
    /// <summary>
    /// Provides methods to simulate mouse clicks at specific, window-relative coordinates
    /// using the Windows SendInput API function.
    /// </summary>
    public static class MouseClicker
    {
        // --- WinAPI Imports, Constants, and Structs are removed ---
        // --- They are now in NativeMethods ---

        /// <summary>
        /// Performs a left mouse click at coordinates relative to the specified window handle.
        /// The cursor is moved to the target position, and then a left-click sequence (down/up) is performed.
        /// </summary>
        /// <param name="gameWindowHandle">The handle (IntPtr) of the target window.</param>
        /// <param name="relativePosition">The X and Y coordinates relative to the window's client area.</param>
        public static void ClickRelativeToWindow(IntPtr gameWindowHandle, Point relativePosition)
        {
            if (gameWindowHandle == IntPtr.Zero)
            {
                // Exit immediately if the provided window handle is invalid.
                return;
            }

            // Create a copy of the point because the ClientToScreen function modifies it in place.
            Point absolutePoint = new Point(relativePosition.X, relativePosition.Y);

            // Convert window-relative coordinates to absolute screen coordinates (pixels).
            // Use the function from NativeMethods
            if (!NativeMethods.ClientToScreen(gameWindowHandle, ref absolutePoint))
            {
                // Conversion failed, potentially because the window is minimized or closed.
                return;
            }

            // Retrieve primary monitor dimensions required for scaling input to the 0-65535 range.
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            // Calculate coordinates scaled to the required 0-65535 range for MOUSEINPUT (absolute position).
            int absoluteX = (absolutePoint.X * 65535) / screenWidth;
            int absoluteY = (absolutePoint.Y * 65535) / screenHeight;

            // An array to hold the sequence of events: Move, Left Down, and Left Up.
            // Use the struct from NativeMethods
            NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[3];

            // 1. Event: Move mouse to the calculated absolute position.
            inputs[0] = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE, // Use constant
                mi = new NativeMethods.MOUSEINPUT
                {
                    dx = absoluteX,
                    dy = absoluteY,
                    // Specify movement and absolute coordinates.
                    // Use constants from NativeMethods
                    dwFlags = NativeMethods.MOUSEEVENTF_MOVE | NativeMethods.MOUSEEVENTF_ABSOLUTE,
                    time = 0
                }
            };

            // 2. Event: Left button down (press).
            inputs[1] = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                mi = new NativeMethods.MOUSEINPUT
                {
                    // No movement (dx/dy) needed as the previous event handled position.
                    dwFlags = NativeMethods.MOUSEEVENTF_LEFTDOWN, // Use constant
                    time = 0
                }
            };

            // 3. Event: Left button up (release).
            inputs[2] = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                mi = new NativeMethods.MOUSEINPUT
                {
                    dwFlags = NativeMethods.MOUSEEVENTF_LEFTUP, // Use constant
                    time = 0
                }
            };

            // Send all 3 events to the operating system in a single call for one quick click.
            // Use the function from NativeMethods
            NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }
    }
}