using System.Drawing;
using System.Windows.Forms;
using System.Media;

namespace UI.Error_UI
{
    /// <summary>
    /// A custom dialog form designed to display critical authentication errors
    /// and a detailed list of affected accounts (logins).
    /// </summary>
    public partial class ErrorDisplayForm : Form
    {
        private const int HeaderHeight = 70;       // Height of the top panel (header)
        private readonly string _rawLoginsContent; // Storage for content to be copied to the clipboard

        /// <summary>
        /// Initializes a new instance of the ErrorDisplayForm.
        /// </summary>
        /// <param name="title">The title text for the form.</param>
        /// <param name="content">The detailed log content (RichTextBox text).</param>
        /// <param name="rawLoginsContent">The raw list of logins to be copied to the clipboard.</param>
        public ErrorDisplayForm(string title, string content, string rawLoginsContent)
        {
            _rawLoginsContent = rawLoginsContent;

            // --- Form Setup ---
            this.Text = title;
            this.Size = new Size(515, 340);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // --- UI Constants ---
            
            // Horizontal margin from the form edge
            const int HorizontalMargin = 10;
            
            // Vertical spacing between panels and the RichTextBox (set to 1px for minimal gap)
            const int VerticalSpacing = 1;
            
            // Button dimensions and spacing
            const int ButtonWidth = 88;
            const int ButtonHeight = 25;
            const int ButtonSpacing = 5;
            
            // Height of the bottom panel (footer)
            const int FooterHeight = 45;

            // 1. Setup the Top Panel (Header)
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = HeaderHeight,
                BackColor = SystemColors.Control,
                Padding = new Padding(HorizontalMargin, 0, HorizontalMargin, 0)
            };

            // 1a. Error Icon
            var iconPictureBox = new PictureBox
            {
                Location = new Point(0, 5),
                Size = new Size(48, 48),
                Image = SystemIcons.Error.ToBitmap(),
                SizeMode = PictureBoxSizeMode.CenterImage,
            };

            // 1b. Main explanatory Label
            var mainLabel = new Label
            {
                Location = new Point(60, 10),
                AutoSize = false,
                Size = new Size(this.ClientSize.Width - 2 * HorizontalMargin - 60 - 5, HeaderHeight - 20),
                Text = "Authentication failure: Missing **MaFiles (2FA)** for these accounts.\n" +
                       "Please ensure these required 2FA files are in the designated folder.\n" +
                       "You can open the folder via: [Settings -> MaFiles (2FA) -> Open Folder].",
                Font = new Font(this.Font.FontFamily, 9f, FontStyle.Regular),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.TopLeft
            };

            headerPanel.Controls.Add(iconPictureBox);
            headerPanel.Controls.Add(mainLabel);

            // 2. Setup the Footer Panel and Buttons

            // 2a. OK Button (Right-most)
            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new Size(ButtonWidth, ButtonHeight),
            };
            okButton.Click += (s, e) => this.Close();

            // 2b. Copy Logins Button (Left of OK)
            var copyButton = new Button
            {
                Text = "Copy Logins",
                Size = new Size(ButtonWidth, ButtonHeight),
            };
            copyButton.Click += (s, e) => {
                if (!string.IsNullOrEmpty(_rawLoginsContent))
                {
                    Clipboard.SetText(_rawLoginsContent);
                    Application.DoEvents();

                    SystemSounds.Asterisk.Play();
                    MessageBox.Show("All failed account logins have been copied to the clipboard.", "Copy Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            var footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = FooterHeight,
                BackColor = SystemColors.Control,
                Padding = new Padding(HorizontalMargin, 10, HorizontalMargin, 10)
            };

            // Add the two buttons to the footer panel
            footerPanel.Controls.Add(okButton);
            footerPanel.Controls.Add(copyButton);

            // Calculate and set button positions (fixed positioning relative to the form's width)
            int buttonY = footerPanel.Padding.Top;

            // Calculate X position for OK button (right-aligned)
            int okButtonX = this.ClientSize.Width - HorizontalMargin - okButton.Width;
            
            // Calculate X position for Copy button (left of OK with ButtonSpacing)
            int copyButtonX = okButtonX - ButtonWidth - ButtonSpacing;

            okButton.Location = new Point(okButtonX, buttonY);
            copyButton.Location = new Point(copyButtonX, buttonY);


            // 3. Container for the RichTextBox (Main Content)

            int containerX = HorizontalMargin;
            
            // Calculate Y position based on header height and vertical spacing
            int containerY = HeaderHeight + VerticalSpacing;

            int containerWidth = this.ClientSize.Width - (2 * HorizontalMargin);

            // Calculate the height, accounting for the header, footer, and vertical spacing above/below the container
            int containerHeight = this.ClientSize.Height - HeaderHeight - FooterHeight - (2 * VerticalSpacing);

            var mainContainer = new Panel
            {
                Location = new Point(containerX, containerY),
                Size = new Size(containerWidth, containerHeight),
                BorderStyle = BorderStyle.FixedSingle,
            };

            // 4. Setup the RichTextBox
            var logTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Text = content,
                Font = new Font("Consolas", 9.5f),
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                BorderStyle = BorderStyle.None,
            };

            // Scroll RichTextBox content to the top initially
            if (logTextBox.TextLength > 0)
            {
                logTextBox.SelectionStart = 0;
                logTextBox.ScrollToCaret();
            }

            mainContainer.Controls.Add(logTextBox);

            // Proper order for adding controls to the form (Footer must be added before MainContent to avoid overlap)
            this.Controls.Add(headerPanel);
            this.Controls.Add(footerPanel);
            this.Controls.Add(mainContainer);

            this.AcceptButton = okButton;

            SystemSounds.Hand.Play();
        }
    }
}