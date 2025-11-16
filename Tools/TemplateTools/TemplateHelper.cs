using System;
using System.Drawing;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;
using ParadiseHelper.Tools.WinAPI;
using ParadiseHelper.SteamAuth;

namespace ParadiseHelper.Tools
{
    /// <summary>
    /// Utility class for automating UI interaction using image recognition (template matching)
    /// combined with window focus and input simulation.
    /// </summary>
    public static class TemplateHelper
    {
        /// <summary>
        /// Defines possible click locations relative to the found template image boundary.
        /// </summary>
        public enum TemplateClickPosition
        {
            /// <summary>Clicks the exact center of the detected template area.</summary>
            Center,
            /// <summary>Clicks the center-top edge of the detected template area (with a small offset).</summary>
            TopCenter,
            /// <summary>Clicks the center-bottom edge of the detected template area (with a small offset).</summary>
            BottomCenter,
            /// <summary>Clicks the center-left edge of the detected template area (with a small offset).</summary>
            LeftCenter,
            /// <summary>Clicks the center-right edge of the detected template area (with a small offset).</summary>
            RightCenter
        }

        /// <summary>
        /// Finds the template image on screen, clicks the specified position to activate an input field,
        /// and then types the provided text after clearing the existing content (using Ctrl+A, Backspace).
        /// </summary>
        /// <param name="windowTitle">The title of the window to ensure focus on.</param>
        /// <param name="templatePath">The file path to the template image.</param>
        /// <param name="threshold">The matching confidence threshold (e.g., 0.9).</param>
        /// <param name="text">The text string to type into the field.</param>
        /// <param name="position">The position within the template match area to click.</param>
        public static void TypeIntoTemplate(
            string windowTitle,
            string templatePath,
            double threshold,
            string text,
            TemplateClickPosition position)
        {
            WindowController.EnsureWindowFocusByTitle(windowTitle);

            using (Bitmap screen = ScreenCapturer.CaptureDesktop())
            using (Bitmap template = new Bitmap(templatePath))
            {
                Point? match = TemplateMatcher.FindMatchInMemory(screen, template, threshold);

                if (!match.HasValue) return;

                // 1. Click on the detected area to activate the input field.
                Point clickPoint = GetClickPoint(match.Value, template.Size, position);
                WindowController.ClickAtPoint(clickPoint);
                Thread.Sleep(150);

                // 2. Re-confirm focus, as a click can sometimes change window focus state.
                WindowController.EnsureWindowFocusByTitle(windowTitle);
                Thread.Sleep(50);

                // 3. Type the text: Select all (Ctrl+A), delete (Backspace), then type the new text.
                var sim = new InputSimulator();

                // Select all (Ctrl+A)
                sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_A);
                Thread.Sleep(40);

                // Delete content (Backspace)
                sim.Keyboard.KeyPress(VirtualKeyCode.BACK);
                Thread.Sleep(10);

                // Type the new text
                sim.Keyboard.TextEntry(text);
            }
        }

        /// <summary>
        /// Finds the template image on screen and performs a single mouse click at the specified position.
        /// </summary>
        /// <param name="windowTitle">The title of the window to ensure focus on.</param>
        /// <param name="templatePath">The file path to the template image.</param>
        /// <param name="threshold">The matching confidence threshold (e.g., 0.9).</param>
        /// <param name="position">The position within the template match area to click.</param>
        public static void ClickTemplateCenter(
            string windowTitle,
            string templatePath,
            double threshold,
            TemplateClickPosition position)
        {
            WindowController.EnsureWindowFocusByTitle(windowTitle);

            using (Bitmap screen = ScreenCapturer.CaptureDesktop())
            using (Bitmap template = new Bitmap(templatePath))
            {
                Point? match = TemplateMatcher.FindMatchInMemory(screen, template, threshold);

                if (match.HasValue)
                {
                    Point clickPoint = GetClickPoint(match.Value, template.Size, position);
                    WindowController.ClickAtPoint(clickPoint);
                    Thread.Sleep(300); // Small delay after clicking.
                }
            }
        }

        /// <summary>
        /// Waits for a specific template image to appear on the screen within a given timeout.
        /// </summary>
        /// <param name="templatePath">The file path to the template image.</param>
        /// <param name="threshold">The matching confidence threshold (e.g., 0.9).</param>
        /// <param name="timeoutMs">The maximum time to wait in milliseconds (default: 3000ms).</param>
        /// <param name="pollDelayMs">The delay between checks in milliseconds (default: 10ms).</param>
        /// <returns>True if the template is found before the timeout, false if timeout is reached.</returns>
        public static bool WaitForTemplateVisible(string templatePath, double threshold, int timeoutMs = 3000, int pollDelayMs = 10)
        {
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < timeoutMs)
            {
                // Checks if the template is currently visible.
                if (TemplateMatcher.IsTemplateVisible(templatePath, threshold)) return true;

                Thread.Sleep(pollDelayMs);
            }
            return false;
        }

        /// <summary>
        /// Calculates the exact pixel coordinates to click based on the top-left corner of the
        /// template match and the desired click location relative to the template's size.
        /// </summary>
        /// <param name="topLeft">The top-left corner (X, Y) of the template match area.</param>
        /// <param name="templateSize">The size (Width, Height) of the matched template image.</param>
        /// <param name="position">The desired relative click position.</param>
        /// <returns>The calculated absolute screen coordinates (Point) to click.</returns>
        private static Point GetClickPoint(Point topLeft, Size templateSize, TemplateClickPosition position)
        {
            int x;
            int y;
            const int offset = 2; // Small offset from the edge

            // Calculates the X and Y coordinates based on the desired click position.
            switch (position)
            {
                case TemplateClickPosition.TopCenter:
                    x = topLeft.X + templateSize.Width / 2;
                    y = topLeft.Y + offset;
                    break;

                case TemplateClickPosition.BottomCenter:
                    x = topLeft.X + templateSize.Width / 2;
                    y = topLeft.Y + templateSize.Height - offset;
                    break;

                case TemplateClickPosition.LeftCenter:
                    x = topLeft.X + offset;
                    y = topLeft.Y + templateSize.Height / 2;
                    break;

                case TemplateClickPosition.RightCenter:
                    x = topLeft.X + templateSize.Width - offset;
                    y = topLeft.Y + templateSize.Height / 2;
                    break;

                default: // Center
                    x = topLeft.X + templateSize.Width / 2;
                    y = topLeft.Y + templateSize.Height / 2;
                    break;
            }

            return new Point(x, y);
        }
    }
}