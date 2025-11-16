using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using ParadiseHelper.Tools.WinAPI;
using ParadiseHelper.AI.Core.Settings;

namespace ParadiseHelper.AI.Control.Mouse
{
    /// <summary>
    /// Provides static methods for calculating and simulating high-precision mouse movements
    /// to instantly aim the game camera at a target coordinate, factoring in game settings
    /// like Sensitivity and Field of View (FOV).
    /// </summary>
    public static class MouseAimer
    {
        // --- WinAPI Imports, Constants, and Structs are removed ---
        // --- They are now in NativeMethods ---

        // --- Math Constants ---
        private const double DEGREE_TO_RADIAN_FACTOR = Math.PI / 180.0;
        private const double HALF_DEGREE_TO_RADIAN_FACTOR = Math.PI / 360.0;

        // Pre-calculated FOV factors for horizontal and vertical aiming
        private static readonly float HorizontalFovTanFactor =
            (float)(AimbotSettings.SensitivityAndFOV.FieldOfView * HALF_DEGREE_TO_RADIAN_FACTOR);

        private static readonly float VerticalFovTanFactor =
            (float)(AimbotSettings.SensitivityAndFOV.VerticalFieldOfView * HALF_DEGREE_TO_RADIAN_FACTOR);

        // --- Public Methods ---

        /// <summary>
        /// Calculates the required relative mouse movement to instantly position the cursor 
        /// at a target point relative to the game window and simulates the mouse movement.
        /// </summary>
        /// <param name="targetX">The X-coordinate of the target relative to the game window's client area (0,0).</param>
        /// <param name="targetY">The Y-coordinate of the target relative to the game window's client area (0,0).</param>
        /// <param name="gameWindowScreenPos">The screen coordinates of the game window's top-left corner.</param>
        /// <param name="gameWindowSize">The size (Width and Height) of the game window's client area.</param>
        public static void AimInstantlyAt(int targetX, int targetY, Point gameWindowScreenPos, Size gameWindowSize)
        {
            // Calculate cursor displacement in screen pixels (dx_pixels, dy_pixels)
            var currentCursorPos = Cursor.Position;
            var screenTargetX = targetX + gameWindowScreenPos.X;
            var screenTargetY = targetY + gameWindowScreenPos.Y;

            int dx_pixels = screenTargetX - currentCursorPos.X;
            int dy_pixels = screenTargetY - currentCursorPos.Y;

            // --- Horizontal Aiming Calculation ---
            float horizontalAtanDenom = (float)(gameWindowSize.Width / (2.0f * Math.Tan(HorizontalFovTanFactor)));
            float horizontalDegrees = (float)(Math.Atan2(dx_pixels, horizontalAtanDenom) / DEGREE_TO_RADIAN_FACTOR);
            int dx_move = (int)(horizontalDegrees / (AimbotSettings.SensitivityAndFOV.GameSensitivity * AimbotSettings.SensitivityAndFOV.m_yaw));

            // --- Vertical Aiming Calculation ---
            float verticalAtanDenom = (float)(gameWindowSize.Height / (2.0f * Math.Tan(VerticalFovTanFactor)));
            float verticalDegrees = (float)(Math.Atan2(dy_pixels, verticalAtanDenom) / DEGREE_TO_RADIAN_FACTOR);
            int dy_move = (int)(verticalDegrees / (AimbotSettings.SensitivityAndFOV.GameSensitivity * AimbotSettings.SensitivityAndFOV.m_pitch));

            // If movement is too small, but needed, apply a minimal step
            if (dy_move == 0 && dy_pixels != 0)
            {
                dy_move = dy_pixels > 0 ? 1 : -1;
            }

            MoveCursorRelative(dx_move, dy_move);
        }

        /// <summary>
        /// Simulates a horizontal camera turn by a specified angle difference.
        /// </summary>
        /// <param name="angleDifference">The angle (in degrees) by which the camera should be turned horizontally.</param>
        public static void TurnCameraHorizontally(double angleDifference)
        {
            // Calculate the required X mouse displacement (dx_move) to turn the camera by the specified angle.
            int dx_move = (int)(angleDifference / (AimbotSettings.SensitivityAndFOV.GameSensitivity * AimbotSettings.SensitivityAndFOV.m_yaw));

            // Perform relative horizontal mouse movement.
            if (dx_move != 0)
            {
                MoveCursorRelative(dx_move, 0);
            }
        }

        // --- Private Helpers ---

        /// <summary>
        /// Uses the Windows <c>SendInput</c> function to simulate a relative mouse movement.
        /// </summary>
        /// <param name="dx">The relative movement on the X-axis.</param>
        /// <param name="dy">The relative movement on the Y-axis.</param>
        private static void MoveCursorRelative(int dx, int dy)
        {
            // Use the struct from NativeMethods
            NativeMethods.INPUT input = new NativeMethods.INPUT { type = NativeMethods.INPUT_MOUSE }; // Use constant
            input.mi.dx = dx;
            input.mi.dy = dy;
            input.mi.dwFlags = NativeMethods.MOUSEEVENTF_MOVE; // Use constant

            // Call the function from NativeMethods
            NativeMethods.SendInput(1, ref input, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }
    }
}