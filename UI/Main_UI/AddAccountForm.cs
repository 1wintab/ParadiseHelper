using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using Core;
using Common.Helpers.Tools.MemoryTools;
using ParadiseHelper.Tools.UITools;

namespace ParadiseHelper
{
    public partial class AddAccountForm : SmartForm
    {
        /// <summary>
        /// Event triggered when one or more accounts have been successfully imported from a file.
        /// </summary>
        public event EventHandler AccountsImported;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddAccountForm"/> class.
        /// Sets up components, applies styling, and configures event handlers.
        /// </summary>
        public AddAccountForm()
        {
            InitializeComponent();

            ApplyFont();
            ApplyVisualStyle();
            SetupFormBehavior();
            ArrangeControlHierarchy();
            ConfigureNavigation();
        }

        // --- Form Lifecycle Overrides ---

        /// <summary>
        /// Handles the form loading event. Temporarily hides the form and then makes it visible to apply visual effects smoothly.
        /// </summary>
        /// <param name="e">A <see cref="EventArgs"/> that contains the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.Visible = false;

            // Use BeginInvoke to ensure form setup is complete before making it visible.
            this.BeginInvoke(new Action(() =>
            {
                this.Visible = true;
            }));
        }

        /// <summary>
        /// Renders the form's custom border.
        /// </summary>
        /// <param name="e">A <see cref="PaintEventArgs"/> that contains the event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            UIHelper.DrawFormBorder(e, this);
        }

        /// <summary>
        /// Cleans up unmanaged resources when the form is closed.
        /// </summary>
        /// <param name="e">A <see cref="FormClosedEventArgs"/> that contains the event data.</param>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            // Ensure proper memory release, especially for GDI objects.
            MemoryHelper.EnsureFormRelease(this);

            // Dispose of the background image to free up resources.
            this.BackgroundImage?.Dispose();
            this.BackgroundImage = null;
        }

        // --- Initialization Helpers ---

        /// <summary>
        /// Applies custom fonts to all necessary UI labels.
        /// </summary>
        private void ApplyFont()
        {
            label1.Font = FontLoader.VAGWorld(20);
            label2.Font = FontLoader.VAGWorld(20);
            label3.Font = FontLoader.VAGWorld(20);
            label4.Font = FontLoader.VAGWorld(13);
            label5.Font = FontLoader.VAGWorld(14);
        }

        /// <summary>
        /// Applies visual effects and styling to panels and picture boxes.
        /// </summary>
        private void ApplyVisualStyle()
        {
            UIEffects.ApplyFadeOut(this);
            StylePanels();
            StylePictureBoxes();
        }

        /// <summary>
        /// Applies rounded corner frames to interactive panels.
        /// </summary>
        private void StylePanels()
        {
            UIHelper.ApplyRoundedFrame(panel1, 20);
            UIHelper.ApplyRoundedFrame(panel2, 20);
            UIHelper.ApplyRoundedFrame(panel3, 20);
            UIHelper.ApplyRoundedFrame(panel4, 10);
            UIHelper.ApplyRoundedFrame(panel5, 10);
        }

        /// <summary>
        /// Applies rounded corners to specific picture box elements.
        /// </summary>
        private void StylePictureBoxes()
        {
            UIHelper.ApplyRoundedCorners(pictureBox1, 10);
            UIHelper.ApplyRoundedCorners(pictureBox6, 10);
        }

        /// <summary>
        /// Configures general form properties and behavior.
        /// </summary>
        private void SetupFormBehavior()
        {
            // Prevents the form icon from appearing in the taskbar.
            this.ShowInTaskbar = false;
        }

        /// <summary>
        /// Adjusts the Z-order/parenting of specific controls for correct visual layering.
        /// </summary>
        private void ArrangeControlHierarchy()
        {
            label2.Parent = panel2;
            pictureBox3.Parent = panel2;
        }

        // --- UI Control Handlers ---

        /// <summary>
        /// Handles the click event for panel2, which is used to open the manual account login form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void Panel2_Click(object sender, EventArgs e)
        {
            UIHelper.ShowForm<LoginForm>(
                this,
                form => { /* No post-initialization needed for LoginForm */ },
                modal: false,
                // Pass a reference to the current form (this) to the LoginForm constructor.
                constructorArgs: new object[] { this }
            );
        }

        /// <summary>
        /// Handles the click event for pictureBox5, which closes the form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> that contains the event data.</param>
        private void PictureBox5_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Attaches a common click handler (<see cref="ImportAccFromFile"/>) to all navigation controls used for file import.
        /// </summary>
        private void ConfigureNavigation()
        {
            UIHelper.AttachClickHandlers(
                new Control[]
                {
                    panel3,
                    panel4,
                    panel5,
                    label3,
                    label4,
                    label5,
                    pictureBox6,
                },
                // The action to be executed when any of the controls are clicked.
                () => ImportAccFromFile()
            );
        }

        // --- Logic Methods ---

        /// <summary>
        /// Opens a file dialog, reads accounts (login:password) from a selected text file,
        /// validates them, inserts valid accounts into the database, and logs any failures.
        /// </summary>
        private void ImportAccFromFile()
        {
            // 1. Select the file
            var dialog = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt" };

            // FIX for Bug 2: If user cancels the dialog, just return. The form stays open.
            // Це виправляє вашу проблему "коли я там не вбиирав і відміняв"
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            // 2. Read file and initialize counters/lists
            string[] lines = File.ReadAllLines(dialog.FileName);

            // List to hold accounts that failed due to invalid format (missing ':' or empty fields).
            var failedAccounts = new List<string>();

            // List to hold accounts that failed because the login already exists in the database.
            var alreadyExistLogins = new List<string>();

            // Total number of valid lines attempted for insertion.
            int total = 0;

            // Total number of accounts successfully added to the database.
            int added = 0;

            // 3. Process each line
            foreach (string line in lines)
            {
                // Skip lines that are empty or do not contain the required separator.
                if (string.IsNullOrWhiteSpace(line) || !line.Contains(":"))
                {
                    failedAccounts.Add($"Invalid format: {line}");
                    continue;
                }

                string[] parts = line.Split(':');

                if (parts.Length != 2) continue;

                // Trim whitespace from login and password components.
                string login = parts[0].Trim();
                string password = parts[1].Trim();

                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
                {
                    failedAccounts.Add($"Empty login or password: {line}");
                    continue;
                }

                total++;

                // Insert account and update counters
                if (DatabaseHelper.InsertAccount(login, password))
                {
                    added++;
                }
                else
                {
                    // If insert fails (likely due to duplicate login).
                    // Log only the login, not the password.
                    alreadyExistLogins.Add($"Login already exists in database: {login}");
                }
            }

            // 4. Handle logging of failed accounts

            // List to combine all failed and skipped accounts for the log file.
            var allFailed = new List<string>();
            allFailed.AddRange(failedAccounts);
            allFailed.AddRange(alreadyExistLogins); // Combine all failures for logging

            // Total count of entries that were not successfully added.
            int failedCount = allFailed.Count;

            // Count of accounts skipped due to existing login.
            int skippedCount = alreadyExistLogins.Count;

            // Prepare log path (file writing happens regardless of showing it)
            string logPath = null;
            if (failedCount > 0)
            {
                // Define the full path for the error log file.
                logPath = Path.Combine(FilePaths.LogsDirectory, "addAccountError.log");

                // Write all invalid and duplicate entries to log file.
                File.WriteAllLines(logPath, allFailed);
            }

            // 5. Show final summary
            if (added == 0)
            {
                MessageBox.Show(
                    "No accounts were successfully added.\n" +
                    $"Total failed/skipped: {failedCount}.\n" +
                    (skippedCount > 0 ? $"Of which {skippedCount} logins already exist." : string.Empty),
                    "Import Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
            else
            {
                MessageBox.Show(
                    $"Successfully added {added} accounts out of {total}.\n" +
                    $"Skipped (already exist): {skippedCount}.\n" +
                    $"Invalid format: {failedCount - skippedCount}.\n\n" +
                    (failedCount > 0
                        ? $"Details for {failedCount} failed items saved to addAccountError.log"
                        : string.Empty),
                    "Import Summary",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // Notify parent form and close ONLY on successful import
                AccountsImported?.Invoke(this, EventArgs.Empty);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }

            // Open the log file using the default text editor (notepad.exe).
            if (logPath != null)
            {
                System.Diagnostics.Process.Start("notepad.exe", logPath);
            }
        }
    }
}