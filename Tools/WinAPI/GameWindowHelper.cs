using System.Drawing;
using ParadiseHelper.Tools.WinAPI;

namespace ParadiseHelper.WinAPI
{
    // Utility class for finding and getting geometry information about an external game window
    // by using Windows API (user32.dll) calls via external helper classes.
    public static class GameWindowHelper
    {
        // Use the substring search method instead of exact title search.
        // This allows the launcher to rename the window (e.g., "Login [Counter-Strike 2]").
        public static Size GetGameWindowSize(string titleSubstring)
        {
            // Find the window handle using the external WindowController.
            // nint is the modern, preferred type for IntPtr.
            nint hWnd = WindowController.FindWindowByTitleSubstring(titleSubstring);

            if (hWnd == nint.Zero)
            {
                // Return default/zero size if window is not found.
                return Size.Empty;
            }

            // Get the bounding rectangle for the entire window (including borders/title bar).
            if (NativeMethods.GetWindowRect(hWnd, out NativeMethods.RECT rect))
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                // Ensure non-negative dimensions before returning.
                if (width > 0 && height > 0)
                {
                    return new Size(width, height);
                }
            }

            // Return default/zero size if GetWindowRect fails or size is invalid.
            return Size.Empty;
        }

        // Gets the screen position of the target window's top-left corner (screen coordinates).
        public static Point GetGameWindowScreenPosition(string titleSubstring)
        {
            // Find the window handle using the external WindowController.
            nint hWnd = WindowController.FindWindowByTitleSubstring(titleSubstring);

            if (hWnd == nint.Zero)
            {
                // Return default/zero position if window is not found.
                return Point.Empty;
            }

            // Get the bounding rectangle for the entire window.
            if (NativeMethods.GetWindowRect(hWnd, out NativeMethods.RECT rect))
            {
                // The top-left corner coordinates are Left and Top.
                return new Point(rect.Left, rect.Top);
            }

            // Return default/zero position if NativeMethods.GetWindowRect fails.
            return Point.Empty;
        }
    }
}