using System;
using System.Windows.Forms;

namespace ParadiseHelper
{
    /// <summary>
    /// Static utility class for applying visual transition effects, such as fade-in and fade-out,
    /// to Windows Forms using System.Windows.Forms.Timer for smooth animation.
    /// </summary>
    public static class UIEffects
    {
        /// <summary>
        /// Implements a smooth fade-in effect when a Form first appears.
        /// IMPORTANT: The form's initial Opacity will be set to 0.
        /// </summary>
        /// <param name="form">The Form to apply the effect to.</param>
        /// <param name="interval">The time interval (in milliseconds) between opacity steps. Default is 30ms.</param>
        /// <param name="step">The amount to increase the Opacity property by in each interval. Default is 0.05.</param>
        public static void ApplyFadeIn(Form form, int interval = 30, double step = 0.05)
        {
            // Safety check for null or already disposed form
            if (form == null || form.IsDisposed) return;

            // 1. Initialize to transparent.
            form.Opacity = 0;

            // 2. Create the timer instance. Must be disposed after use.
            Timer timer = new Timer
            {
                Interval = interval, // Speed of the animation.
                Enabled = false      // Start disabled.
            };

            // 3. Define the event handlers.
            EventHandler tickHandler = null;

            tickHandler = (s, e) =>
            {
                // Stop if the form is disposed unexpectedly during the fade.
                if (form.IsDisposed)
                {
                    timer.Tick -= tickHandler;
                    timer.Stop();
                    timer.Dispose();
                    return;
                }

                form.Opacity += step;

                // When opacity reaches 100%, stop and dispose of the timer.
                if (form.Opacity >= 1.0)
                {
                    form.Opacity = 1.0; // Ensure it reaches exactly 1.0
                    timer.Tick -= tickHandler;
                    timer.Stop();
                    timer.Dispose();
                }
            };

            timer.Tick += tickHandler;

            // 4. Start the animation when the form is fully shown.
            // Using a lambda expression for the Shown handler is simple here, 
            // as the timer cleanup is handled in the Tick event.
            form.Shown += (s, e) =>
            {
                // Check if the timer is already disposed or running before starting.
                if (timer != null)
                {
                    timer.Start();
                }
            };
        }

        /// <summary>
        /// Implements a smooth fade-out effect when a Form is closed by the user.
        /// </summary>
        /// <param name="form">The Form to apply the effect to.</param>
        /// <param name="interval">The time interval (in milliseconds) between opacity steps. Default is 30ms.</param>
        /// <param name="step">The amount to decrease the Opacity property by in each interval. Default is 0.05.</param>
        public static void ApplyFadeOut(Form form, int interval = 30, double step = 0.05)
        {
            // Safety check for null or already disposed form
            if (form == null || form.IsDisposed) return;

            // Attach to the FormClosing event.
            form.FormClosing += (s, e) =>
            {
                // Only trigger the fade-out on user-initiated closes (e.g., clicking 'X' or Alt+F4).
                if (e.CloseReason != CloseReason.UserClosing && e.CloseReason != CloseReason.None)
                {
                    return;
                }

                // If the form is already invisible or disposed, let the closing proceed normally.
                if (form.Opacity == 0 || form.IsDisposed)
                {
                    return;
                }

                // IMPORTANT: Prevent the form from closing immediately while the fade-out occurs.
                e.Cancel = true;

                Timer timer = new Timer { Interval = interval };
                EventHandler tickHandler = null;

                // Event handler that runs repeatedly to decrease opacity.
                tickHandler = (sender, args) =>
                {
                    if (form.IsDisposed)
                    {
                        timer.Tick -= tickHandler;
                        timer.Stop();
                        timer.Dispose();
                        return;
                    }

                    form.Opacity -= step;

                    // When opacity reaches 0, stop the timer, dispose it, and finally close the form.
                    if (form.Opacity <= 0)
                    {
                        form.Opacity = 0; // Ensure it reaches exactly 0
                        timer.Tick -= tickHandler;
                        timer.Stop();
                        timer.Dispose();
                        // Call Close() again to finish the process without cancellation.
                        form.Close();
                    }
                };

                timer.Tick += tickHandler;
                timer.Start();
            };
        }
    }
}