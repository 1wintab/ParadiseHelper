using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.IO.Compression;
using System.Collections.Generic;
using Newtonsoft.Json;
using Common.Helpers.Tools.MemoryTools;
using ParadiseHelper.Tools.UITools;
using ParadiseHelper.Managers.MaFiles;
using ParadiseHelper;
using Tools.UITools;

namespace UI.Main_UI
{
    /// <summary>
    /// Represents the main form for selecting, processing, and archiving Steam maFile data.
    /// Inherits from SmartForm to utilize custom UI capabilities.
    /// </summary>
    public partial class ParcerForm : SmartForm
    {
        // Logger instance responsible for displaying messages in the RichTextBox.
        private UILogger _logger;

        // Custom font to be used for the log output to maintain visual consistency.
        private Font _logCustomFont = FontLoader.BIPs(9);

        // Constant string defining the placeholder text for the log window.
        private const string LogPlaceholderText = "Here will appear your selected maFile names...";

        // List to store the full paths of the maFile archives selected by the user.
        private List<string> selectedMaFiles = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ParcerForm"/> class.
        /// </summary>
        public ParcerForm()
        {
            InitializeComponent();

            ApplyFont();
            ApplyVisualStyle();

            InitSessionLog();
        }

        /// <summary>
        /// Handles actions performed when the form is closed, including UI effects and memory cleanup.
        /// </summary>
        /// <param name="e">The event data for the form closing.</param>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            // Applies a brief fade-in effect before the form fully closes.
            UIEffects.ApplyFadeIn(this);

            // Ensures proper release of form resources to prevent memory leaks.
            MemoryHelper.EnsureFormRelease(this);
        }

        // --- UI Configuration Methods ---

        /// <summary>
        /// Applies the custom VAG World font to all relevant labels and controls on the form.
        /// </summary>
        private void ApplyFont()
        {
            label1.Font = FontLoader.VAGWorld(24);
            label2.Font = FontLoader.VAGWorld(20);
            Create_Button.Font = FontLoader.VAGWorld(20);
            richTextBox1.Font = _logCustomFont;
        }

        /// <summary>
        /// Applies custom visual effects, such as fading, and rounded corners to the form's panels and controls.
        /// </summary>
        private void ApplyVisualStyle()
        {
            // Applies a fade-out effect to the form upon initial display.
            UIEffects.ApplyFadeOut(this);

            // Applies rounded corners to the main content panels.
            UIHelper.ApplyRoundedFrame(panel2, 9);
            UIHelper.ApplyRoundedFrame(panel1, 9);
        }

        /// <summary>
        /// Initializes the <see cref="UILogger"/> instance and sets up the log RichTextBox.
        /// </summary>
        private void InitSessionLog()
        {
            _logger = new UILogger(
                richTextBox1,
                _logCustomFont,
                null,
                LogPlaceholderText
            );

            // Displays the initial placeholder text in the log box.
            _logger.ShowPlaceholder();

            // Attaches focus event handlers to manage the placeholder text visibility.
            richTextBox1.GotFocus += richTextBox1_GotFocus;
            richTextBox1.LostFocus += richTextBox1_LostFocus;
        }

        // --- Log Focus Handlers ---

        /// <summary>
        /// Handles the RichTextBox receiving focus, hiding the placeholder if currently visible.
        /// </summary>
        private void richTextBox1_GotFocus(object sender, EventArgs e)
        {
            _logger.HandleGotFocus();
        }

        /// <summary>
        /// Handles the RichTextBox losing focus, showing the placeholder if the log is currently empty.
        /// </summary>
        private void richTextBox1_LostFocus(object sender, EventArgs e)
        {
            _logger.HandleLostFocus();
        }

        // --- Event Handlers ---

        /// <summary>
        /// Event handler for the SelectMaFile button click. Opens a dialog for multi-file selection
        /// and updates the log with the names of the selected files.
        /// </summary>
        private void SelectMaFile_Button_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                // Configures the dialog to filter for .maFile and allow multiple selections.
                ofd.Filter = "maFile (*.maFile)|*.maFile";
                ofd.Multiselect = true;
                ofd.Title = "Select one or more .maFile files";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    // Stores the paths of the selected files.
                    selectedMaFiles = ofd.FileNames.ToList();

                    // Clears the placeholder before writing new log entries.
                    _logger.HandleGotFocus();

                    richTextBox1.Clear();

                    // Logs the name of each selected maFile.
                    foreach (var file in selectedMaFiles)
                    {
                        _logger.Log($"📄 {Path.GetFileName(file)}", Color.Black);
                    }
                    // Logs the total count of files selected.
                    _logger.Log($"🧾 Files selected: {selectedMaFiles.Count}", Color.Black);

                    // Scrolls the log to the bottom to show the newest entries.
                    richTextBox1.SelectionStart = richTextBox1.Text.Length;
                    richTextBox1.ScrollToCaret();
                }

                // If no files were selected (e.g., user clicked 'Cancel'), restore the placeholder.
                if (selectedMaFiles.Count == 0)
                {
                    _logger.HandleLostFocus();
                }
            }
        }

        /// <summary>
        /// Event handler for the Create button click. Processes the selected maFiles by extracting
        /// minimal required data, reformatting the JSON, and creating a ZIP archive.
        /// </summary>
        private void Create_Button_Click(object sender, EventArgs e)
        {
            // Ensures files are selected before starting the processing.
            if (selectedMaFiles.Count == 0)
            {
                MessageBox.Show("Please select maFiles before saving.", "Missing maFiles", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Prepares the log box for output during the processing phase.
            _logger.HandleGotFocus();
            richTextBox1.Clear();

            using (var sfd = new SaveFileDialog())
            {
                // Configures the Save File Dialog for the ZIP archive output.
                sfd.Filter = "ZIP archive|*.zip";
                sfd.FileName = $"mafiles_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

                if (sfd.ShowDialog() != DialogResult.OK) return;

                // Path to the final ZIP archive.
                string archivePath = sfd.FileName;

                // Creates a unique, temporary working directory.
                string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDir);

                // Counter for successfully processed files.
                int count = 0;

                // Iterates through each selected maFile for processing.
                foreach (var path in selectedMaFiles)
                {
                    try
                    {
                        // 1. Read and deserialize the full maFile content.
                        string json = File.ReadAllText(path);
                        var full = JsonConvert.DeserializeObject<MaFileModel>(json);

                        // Safely extract SteamID, defaulting to 0 if the Session object is null.
                        ulong steamId = full.Session?.SteamID ?? 0;

                        // Creates an anonymous object containing only the necessary fields (shared_secret, account_name)
                        // while preserving the nested Session structure with SteamID.
                        var minimal = new
                        {
                            full.shared_secret,
                            full.account_name,
                            Session = new
                            {
                                SteamID = steamId
                            }
                        };

                        // 3. Serialize the minimal data into a compact (non-indented) JSON string.
                        string outputJson = JsonConvert.SerializeObject(minimal, Formatting.None);

                        // 4. Determines the output file name, prioritizing SteamID.
                        string safeName = (steamId > 0)
                            ? steamId.ToString()
                            : Path.GetFileNameWithoutExtension(Path.GetFileName(path));

                        // Fallback naming in case SteamID and original name are unavailable.
                        if (string.IsNullOrWhiteSpace(safeName))
                            safeName = $"mafile_{count + 1}";

                        // 5. Writes the new minimal maFile to the temporary directory.
                        string outPath = Path.Combine(tempDir, $"{safeName}.maFile");
                        File.WriteAllText(outPath, outputJson);
                        count++;

                        _logger.Log($"✅ Saved: {safeName}.maFile", Color.Black);
                    }
                    catch (Exception ex)
                    {
                        // Displays an error message for the specific file that failed.
                        MessageBox.Show($"⚠️ Failed to process {path}:\n{ex.Message}");
                    }
                }

                // --- Archive Creation ---
                try
                {
                    // Deletes the target archive if it already exists.
                    if (File.Exists(archivePath))
                        File.Delete(archivePath);

                    // Creates the ZIP archive from the contents of the temporary directory.
                    ZipFile.CreateFromDirectory(tempDir, archivePath, CompressionLevel.Optimal, false);
                    _logger.Log($"📦 ZIP archive saved: {archivePath}", Color.Black);
                }
                catch (Exception ex)
                {
                    // Displays an error message if ZIP creation fails.
                    MessageBox.Show($"❌ Failed to create archive:\n{ex.Message}");
                }
                finally
                {
                    // Ensures the temporary directory is cleaned up after processing.
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }

                _logger.Log($"✅ Files processed: {count}, archive saved.\n", Color.Black);

                // --- Open Containing Folder ---

                try
                {
                    // Opens the folder where the ZIP file was saved and selects the file.
                    using (System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{archivePath}\""))
                    {
                        // Process resources are released automatically upon using 'using'.
                    }
                }
                catch (Exception ex)
                {
                    // Handles the scenario where the archive was created but the explorer failed to open.
                    MessageBox.Show(
                        $"Archive created, but failed to open explorer:\n" +
                        $"{ex.Message}",
                        "Operation complete with errors",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }

                // Restores the log's placeholder state if the log is now empty.
                _logger.HandleLostFocus();
            }
        }
    }
}