using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using Common.Helpers.Tools.MemoryTools;

namespace ParadiseHelper
{
    /// <summary>
    /// Static utility class containing various methods for UI manipulation
    /// and visual effects for Windows Forms controls.
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// Applies rounded corners to all four sides of a control by setting its Region property.
        /// The control's old Region is disposed automatically.
        /// </summary>
        /// <param name="control">The control to apply the effect to.</param>
        /// <param name="radius">The radius of the corners.</param>
        public static void ApplyRoundedCorners(Control control, int radius)
        {
            if (control == null || radius <= 0 || control.IsDisposed) return;

            // Dispose the old Region if it exists before setting a new one.
            control.Region?.Dispose();

            using (GraphicsPath path = new GraphicsPath())
            {
                int correction = 1; // Small offset for drawing accuracy.

                // Draws arcs for all four corners and closes the figure.
                path.AddArc(correction, correction, radius * 2, radius * 2, 180, 90);
                path.AddArc(control.Width - radius * 2 - correction, correction, radius * 2, radius * 2, 270, 90);
                path.AddArc(control.Width - radius * 2 - correction, control.Height - radius * 2 - correction, radius * 2, radius * 2, 0, 90);
                path.AddArc(correction, control.Height - radius * 2 - correction, radius * 2, radius * 2, 90, 90);
                path.CloseFigure();

                control.Region = new Region(path); // Set the control's visual boundary.
            }
        }

        /// <summary>
        /// Applies rounded corners only to the top-left and bottom-left sides of a control.
        /// The control's old Region is disposed automatically.
        /// </summary>
        /// <param name="control">The control to apply the effect to.</param>
        /// <param name="radius">The radius of the corners.</param>
        public static void ApplyLeftRoundedCorners(Control control, int radius)
        {
            // Also taken from code2 due to better resource management practices.
            if (control == null || radius <= 0 || control.IsDisposed) return;

            // Dispose the old Region if it exists.
            control.Region?.Dispose();

            using (GraphicsPath path = new GraphicsPath())
            {
                int correction = 1;

                // Defines the path: top-left arc, straight lines, bottom-left arc.

                // Upper left
                path.AddArc(correction, correction, radius * 2, radius * 2, 180, 90);

                // Top line
                path.AddLine(correction + radius, correction, control.Width - correction, correction);

                // Right line
                path.AddLine(control.Width - correction, correction, control.Width - correction, control.Height - correction);

                // Bottom line
                path.AddLine(control.Width - correction, control.Height - correction, correction + radius, control.Height - correction);

                // Lower left
                path.AddArc(correction, control.Height - radius * 2 - correction, radius * 2, radius * 2, 90, 90); 

                path.CloseFigure();
                control.Region = new Region(path);
            }
        }

        /// <summary>
        /// Applies instant color changes (hover/press) to a panel based on mouse events on a target control.
        /// </summary>
        /// <param name="hoverTarget">The control whose mouse events trigger the color change (e.g., a PictureBox).</param>
        /// <param name="panelToToggle">The panel whose BackColor property will be changed.</param>
        public static void ApplyInstantHoverEffect(Control hoverTarget, Panel panelToToggle)
        {
            Color defaultColor = Color.White;
            Color hoverColor = Color.FromArgb(220, 220, 220);
            Color pressedColor = Color.FromArgb(210, 210, 210);

            void SetColor(Color color)
            {
                panelToToggle.BackColor = color;
            }

            // MouseEnter/MouseLeave events handle standard hover.
            hoverTarget.MouseEnter += (s, e) => SetColor(hoverColor);
            hoverTarget.MouseLeave += (s, e) => SetColor(defaultColor);
            panelToToggle.MouseEnter += (s, e) => SetColor(hoverColor);
            panelToToggle.MouseLeave += (s, e) => SetColor(defaultColor);

            // MouseDown/MouseUp events handle the 'pressed' state.
            hoverTarget.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) SetColor(pressedColor); };
            hoverTarget.MouseUp += (s, e) => SetColor(hoverColor);
            panelToToggle.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) SetColor(pressedColor); };
            panelToToggle.MouseUp += (s, e) => SetColor(hoverColor);
        }

        /// <summary>
        /// Draws a rounded border around a control by handling its Paint event.
        /// This method should be used in conjunction with ApplyRoundedCorners to shape the control first.
        /// </summary>
        /// <param name="ctrl">The control to draw the border on.</param>
        /// <param name="radius">The radius used for the border's rounded corners.</param>
        public static void DrawBorder(Control ctrl, int radius)
        {
            ctrl.Paint += (sender, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(ctrl.BackColor); // Clear background to prevent drawing artifacts.

                const int borderWidth = 2;
                const int cornerSize = 2;

                using (var pen = new Pen(Color.Black, borderWidth))
                {
                    var offset = (int)Math.Ceiling(pen.Width / 2.0);
                    
                    // 1. Define the rounded border path using arcs and lines.
                    var path = new GraphicsPath();
                    path.StartFigure();
                   
                    // Add arcs for corners
                    path.AddArc(offset, offset, radius * 2, radius * 2, 180, 90);
                    path.AddArc(ctrl.Width - radius * 2 - offset - 1, offset, radius * 2, radius * 2, 270, 90);
                    path.AddArc(ctrl.Width - radius * 2 - offset - 1, ctrl.Height - radius * 2 - offset - 1, radius * 2, radius * 2, 0, 90);
                    path.AddArc(offset, ctrl.Height - radius * 2 - offset - 1, radius * 2, radius * 2, 90, 90);
                    path.CloseFigure();

                    // 2. Modify the control's Region (as in code1)
                    using (var panelRegion = new Region(path))
                    {
                        // Exclude small strips to fix potential rendering artifacts at the edges.
                        using (var cutLeft = new GraphicsPath())
                        {
                            cutLeft.AddRectangle(new Rectangle(0, 0, cornerSize, ctrl.Height));
                            panelRegion.Exclude(cutLeft);
                        }
                        using (var cutTop = new GraphicsPath())
                        {
                            cutTop.AddRectangle(new Rectangle(0, 0, ctrl.Width, cornerSize));
                            panelRegion.Exclude(cutTop);
                        }

                        ctrl.Region = panelRegion; // Apply the modified shape.
                        g.DrawPath(pen, path); // Draw the actual border line.
                    }
                }
            };
        }

        /// <summary>
        /// Convenience method to apply both rounded corners (Region) and a drawn border (Paint) to a control.
        /// Also attempts to enable double buffering to reduce flicker.
        /// </summary>
        /// <param name="ctrl">The control to apply the rounded frame to.</param>
        /// <param name="radius">The radius of the corners/border.</param>
        public static void ApplyRoundedFrame(Control ctrl, int radius)
        {
            // Attempt to enable DoubleBuffered property via reflection to reduce flicker.
            try
            {
                typeof(Control)
                    .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?
                    .SetValue(ctrl, true);
            }
            catch { /* Ignore reflection failure */ }

            ApplyRoundedCorners(ctrl, radius);
            DrawBorder(ctrl, radius);
        }

        /// <summary>
        /// Displays a form, ensuring proper ownership (always MainForm), 
        /// UI synchronization (movement), and blur background control.
        /// </summary>
        /// <typeparam name="T">The type of the form to show.</typeparam>
        /// <param name="parentForm">The form that initiates the call (used for centering).</param>
        /// <param name="initializer">Action to perform initialization on the new form (e.g., event subscription).</param>
        /// <param name="modal">If true, shows the form modally (ShowDialog).</param>
        /// <param name="constructorArgs">Optional arguments to pass to the form's constructor.</param>
        public static void ShowForm<T>(Form parentForm, Action<T> initializer = null, bool modal = false, params object[] constructorArgs)
            where T : Form
        {
            // 1. Find the root MainForm to be the main OWNER.
            Form mainForm = parentForm as Form;
            while (mainForm != null && !(mainForm is MainForm))
            {
                mainForm = mainForm.Owner;
            }
            // If mainForm is still null, try to find the actual MainForm among open forms.
            if (mainForm == null)
            {
                mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            }

            // 2. Get the blur background control from MainForm.
            PictureBox blurBackground = mainForm?.Controls.Find("pictureBox4", true).FirstOrDefault() as PictureBox;

            // 3. Activate the blur background *only* if it's found and currently disabled.
            if (blurBackground != null && !blurBackground.Enabled)
            {
                blurBackground.BringToFront();
                blurBackground.Enabled = true;
            }

            // Create the form instance using Activator.CreateInstance to support constructors with arguments.
            T form;
            try
            {
                form = (T)Activator.CreateInstance(typeof(T), constructorArgs);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to create form instance.\n" +
                    $"Check if the form has a constructor matching the arguments provided.\n" +
                    $"Error: {ex.Message}", 
                    "UI Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
                
                // Hide blur if form creation failed
                if (blurBackground != null && blurBackground.Enabled)
                {
                    blurBackground.Enabled = false;
                    blurBackground.SendToBack();
                }

                return;
            }

            // Run initializer action (e.g., event subscription)
            initializer?.Invoke(form); 

            // 4. Set the OWNER - always the mainForm if found, otherwise the parentForm.
            form.Owner = mainForm ?? parentForm;

            form.StartPosition = FormStartPosition.Manual;

            // 5. Center the form relative to the calling form (parentForm).
            int offsetX = (parentForm.Width - form.Width) / 2;
            int offsetY = (parentForm.Height - form.Height) / 2 + (parentForm is MainForm ? 10 : 0);
            Point GetCenteredLocation() => new Point(parentForm.Location.X + offsetX, parentForm.Location.Y + offsetY);

            form.Location = GetCenteredLocation();

            // 6. Synchronize movement with the parentForm.
            EventHandler syncHandler = null;
            syncHandler = (_, __) =>
            {
                if (!form.IsDisposed && form.IsHandleCreated)
                    form.Location = GetCenteredLocation();
            };
            parentForm.Move += syncHandler;

            // 7. Cleanup logic on closing.
            form.FormClosed += (_, __) =>
            {
                // Unsubscribe from movement
                parentForm.Move -= syncHandler; 

                // Hide the blur background ONLY if this was the last owned form closing.
                if (mainForm != null && blurBackground != null)
                {
                    bool anyOtherOwnedFormsVisible = false;
                    
                    // Check all open forms owned by mainForm.
                    foreach (Form openForm in Application.OpenForms)
                    {
                        // Check if the form is owned by mainForm, is not the form we just closed, and is currently visible.
                        if (openForm.Owner == mainForm && openForm != form && openForm.Visible)
                        {
                            anyOtherOwnedFormsVisible = true;
                            break;
                        }
                    }

                    // If no other owned forms are visible, hide the blur background.
                    if (!anyOtherOwnedFormsVisible)
                    {
                        blurBackground.Enabled = false;
                        blurBackground.SendToBack();
                    }

                    mainForm.Activate();
                }

                // Return the memory cleanup call
                MemoryHelper.EnsureFormRelease(form);
            };

            // 8. Show the form.
            if (modal)
            {
                form.ShowDialog(form.Owner);
            }
            else
            {
                form.Show(form.Owner);
            }      
        }

        /// <summary>
        /// Disposes and clears the background image of a form to release resources.
        /// </summary>
        /// <param name="form">The form whose background image is to be cleared.</param>
        public static void ClearFormBackground(Form form)
        {
            if (form == null || form.IsDisposed) return;

            if (form.BackgroundImage != null)
            {
                form.BackgroundImage.Dispose();
                form.BackgroundImage = null;
            }
        }

        /// <summary>
        /// Draws a simple border around a form in its Paint event.
        /// </summary>
        /// <param name="e">The PaintEventArgs object for the Paint event.</param>
        /// <param name="form">The form to draw the border on.</param>
        public static void DrawFormBorder(PaintEventArgs e, Form form)
        {
            using (Pen pen = new Pen(Color.Black, 10))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle borderRect = new Rectangle(0, 0, form.Width - 1, form.Height - 1);
                e.Graphics.DrawRectangle(pen, borderRect);
            }
        }

        /// <summary>
        /// Attaches a single common click action to an array of controls.
        /// </summary>
        /// <param name="clickables">An array of controls to attach the handler to.</param>
        /// <param name="onClick">The Action delegate to execute on click.</param>
        public static void AttachClickHandlers(Control[] clickables, Action onClick)
        {
            foreach (var ctrl in clickables)
                ctrl.Click += (s, e) => onClick();
        }

        /// <summary>
        /// Convenience method to apply both instant hover effects (from PictureBox to Panel) and rounded corners to the target panel.
        /// </summary>
        /// <param name="pic">The PictureBox triggering the hover effect.</param>
        /// <param name="target">The Panel that receives the hover color change and rounded corners.</param>
        /// <param name="radius">The radius for the rounded corners.</param>
        public static void ApplyHoverWithRounded(PictureBox pic, Panel target, int radius)
        {
            UIHelper.ApplyInstantHoverEffect(pic, target);
            UIHelper.ApplyRoundedCorners(target, radius);
        }

        /// <summary>
        /// Sets up a TextBox to display a placeholder text in a dim color.
        /// </summary>
        /// <param name="textBox">The TextBox to apply the placeholder effect to.</param>
        /// <param name="placeholderText">The placeholder string.</param>
        /// <param name="emptyPanel">Optional panel to show/hide when the placeholder is active/inactive.</param>
        public static void ShowPlaceholder(TextBox textBox, string placeholderText, Panel emptyPanel = null)
        {
            textBox.Text = placeholderText;
            textBox.ForeColor = Color.DimGray;

            if (emptyPanel != null)
                emptyPanel.Visible = true;
        }

        /// <summary>
        /// Binds an icon (PictureBox) to open a file dialog, applies the path to a TextBox, and validates the selected file name.
        /// </summary>
        /// <param name="icon">The PictureBox that triggers the OpenFileDialog.</param>
        /// <param name="textBox">The TextBox to display the selected path.</param>
        /// <param name="placeholderText">The initial placeholder text for the TextBox.</param>
        /// <param name="expectedFileName">Optional name the selected file must match (case-insensitive).</param>
        /// <param name="emptyStatePanel">Optional panel to hide when a valid path is selected.</param>
        public static void BindPathSelector(
            PictureBox icon,
            TextBox textBox,
            string placeholderText,
            string expectedFileName = null,
            Panel emptyStatePanel = null)
        {
            const string dialogTitle = "Select File";
            const string filter = "Executable Files (*.exe)|*.exe|All files (*.*)|*.*";
            Color activeTextColor = Color.Black;

            void ApplyPath(string path)
            {
                textBox.Text = path;
                textBox.ForeColor = activeTextColor;

                if (emptyStatePanel != null)
                    emptyStatePanel.Visible = false;
            }

            textBox.ReadOnly = true; // Prevent direct text input.

            // Initialize placeholder state if the box is empty.
            if (string.IsNullOrWhiteSpace(textBox.Text) || textBox.Text == placeholderText)
                ShowPlaceholder(textBox, placeholderText, emptyStatePanel);

            // Click event for the icon to open the file dialog.
            icon.Click += (s, e) =>
            {
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    dialog.Title = dialogTitle;
                    dialog.Filter = filter;

                    // Custom validation logic executed when the user clicks 'OK'.
                    dialog.FileOk += (senderDialog, eventArgs) =>
                    {
                        string selected = dialog.FileName;
                        string fileNameOnly = Path.GetFileName(selected);

                        // Check if the selected file name matches the expected name (if specified).
                        if (!string.IsNullOrEmpty(expectedFileName)
                            && !fileNameOnly.Equals(expectedFileName, StringComparison.OrdinalIgnoreCase))
                        {
                            eventArgs.Cancel = true; // Block the dialog from closing.
                            
                            System.Media.SystemSounds.Exclamation.Play();
                           
                            MessageBox.Show(
                                $"Please select a file named '{expectedFileName}'.",
                                "Invalid file selected",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        }
                    };

                    // Show the dialog and apply the path if selection is successful and valid.
                    if (dialog.ShowDialog() == DialogResult.OK)
                        ApplyPath(dialog.FileName);
                }
            };
        }
    }
}