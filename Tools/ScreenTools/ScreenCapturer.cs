using System.Drawing;
using System.Windows.Forms;

namespace ParadiseHelper
{
    /// <summary>
    /// Utility class for capturing the content of the primary screen using Windows Forms and GDI+ functionalities.
    /// </summary>
    /// <remarks>
    /// This class requires references to System.Drawing and System.Windows.Forms.
    /// </remarks>
    internal static class ScreenCapturer
    {
        /// <summary>
        /// Captures the entire primary monitor's screen content into a new <see cref="Bitmap"/> image.
        /// </summary>
        /// <returns>A <see cref="Bitmap"/> object containing the captured image of the primary screen.</returns>
        public static Bitmap CaptureDesktop()
        {
            // 1. Determine the size and boundaries of the primary display.
            Rectangle bounds = Screen.PrimaryScreen.Bounds;

            // 2. Create a new Bitmap object to serve as the destination canvas for the capture.
            // The Bitmap is initialized with the exact dimensions of the primary screen.
            Bitmap bmp = new Bitmap(bounds.Width, bounds.Height);

            // 3. Create a Graphics object from the Bitmap.
            // The 'using' statement ensures the Graphics object is properly disposed of after use.
            using (Graphics g = Graphics.FromImage(bmp))
            {
                // 4. Copy the pixel data from the screen buffer onto the Bitmap's Graphics context.
                // - Source (Screen): Start at Point.Empty (top-left corner of the primary screen).
                // - Destination (Bitmap): Start at Point.Empty (top-left corner of the Bitmap).
                // - Size: bounds.Size (the full width and height of the screen).
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }

            // 5. The Bitmap now holds the captured desktop image.
            return bmp;
        }
    }
}