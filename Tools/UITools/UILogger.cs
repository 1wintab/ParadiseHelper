using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ParadiseHelper.Tools.WinAPI;

namespace Tools.UITools
{
    /// <summary>
    /// Manages UI logging for a specific RichTextBox control.
    /// Encapsulates formatting, file writing, thread safety, and placeholder state.
    /// </summary>
    public class UILogger
    {
        private readonly RichTextBox _logControl;
        private readonly Font _logCustomFont;
        private readonly string _sessionLogFilePath;
        private readonly string _logPlaceholderText;
        private bool _isLogPlaceholderActive;

        /// <summary>
        /// Initializes a new instance of the UILogger.
        /// </summary>
        /// <param name="logControl">The RichTextBox control to write logs to.</param>
        /// <param name="logFont">The custom font to use for log entries.</param>
        /// <param name="sessionLogFilePath">The full path to the session log file.</param>
        /// <param name="logPlaceholderText">The text to display when the log is empty.</param>
        public UILogger(RichTextBox logControl, Font logFont, string sessionLogFilePath, string logPlaceholderText)
        {
            _logControl = logControl;
            _logCustomFont = logFont;
            _sessionLogFilePath = sessionLogFilePath;
            _logPlaceholderText = logPlaceholderText;
        }

        // --- Public API Methods ---

        /// <summary>
        /// Writes a time-stamped message to the RichTextBox, handling cross-thread calls.
        /// </summary>
        /// <param name="message">The main text message to be logged.</param>
        /// <param name="color">Optional color for the message text.</param>
        /// <param name="footer">Optional second line of text (footer) for the log entry.</param>
        public void Log(string message, Color? color = null, string footer = null)
        {
            // 1. Cross-Thread Access Check (C# WinForms/WPF Standard)
            // If the method is called from a thread other than the UI thread,
            // it invokes itself on the correct thread and exits the current call.
            if (_logControl.InvokeRequired)
            {
                _logControl.Invoke(new Action(() => Log(message, color, footer)));
                return;
            }

            // 2. Placeholder Handling
            // If a temporary placeholder text is active in the log box, clear it.
            if (_isLogPlaceholderActive)
            {
                ClearPlaceholder();
            }

            // 3. Preparation: Timestamps and Separators
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string timeStampText = $"[{timestamp}]";
            string spacer = " ";

            // Check if the call is intended to be a simple separator line (empty message and empty footer).
            bool isSeparator = string.IsNullOrWhiteSpace(message) && string.IsNullOrEmpty(footer);

            // Write the raw content to a session file/storage (if applicable, this method is external).
            WriteToSessionLog(message, timeStampText, spacer, footer, isSeparator);

            // 4. Formatting Setup
            // Ensure the new entry starts on a new line, but only if the log box is not empty.
            if (_logControl.TextLength > 0)
            {
                _logControl.AppendText(Environment.NewLine);
            }

            // Set cursor position to the end of the text before appending.
            _logControl.SelectionStart = _logControl.TextLength;
            _logControl.SelectionLength = 0;

            // 5. Separator Logic (If only a timestamp/separator is needed)
            if (isSeparator)
            {
                _logControl.SelectionFont = _logCustomFont;
                _logControl.SelectionColor = Color.Black;
                _logControl.AppendText(timeStampText); // Only append the timestamp
                FinalizeLogUpdate(); // Scroll to caret and refresh/update UI
                return;
            }

            // 6. Main Log Entry Rendering
            Color messageColor = color ?? _logControl.ForeColor;
            Color timeStampColor = Color.Black;

            // A. Render Timestamp (e.g., [10:00:00] - always black)
            _logControl.SelectionFont = _logCustomFont;
            _logControl.SelectionColor = timeStampColor;
            _logControl.AppendText(timeStampText);

            // B. Render Main Message (e.g., [10:00:00] Log Message)
            _logControl.SelectionFont = _logCustomFont;
            _logControl.SelectionColor = messageColor;
            _logControl.AppendText(spacer + message);

            // 7. Footer Rendering (If optional footer text is provided)
            if (!string.IsNullOrEmpty(footer))
            {
                _logControl.AppendText(Environment.NewLine); // Start new line for the footer

                // C. Re-render Timestamp for the footer line (e.g., [10:00:00])
                _logControl.SelectionFont = _logCustomFont;
                _logControl.SelectionColor = timeStampColor;
                _logControl.AppendText(timeStampText);

                // D. Render Footer text (e.g., [10:00:00] Secondary/Details Text)
                _logControl.SelectionFont = _logCustomFont;
                _logControl.SelectionColor = Color.Black;
                _logControl.AppendText(spacer + footer);
            }

            // 8. Finalize
            // Scroll the RichTextBox to the bottom/caret position and apply changes.
            FinalizeLogUpdate();
        }

        /// <summary>
        /// Logs a decorative empty line with only a timestamp.
        /// </summary>
        public void LogSeparator()
        {
            Log("");
        }

        /// <summary>
        /// Clears the log control and the physical session log file.
        /// </summary>
        public void DeleteLog()
        {
            _logControl.Clear();
            if (!string.IsNullOrEmpty(_sessionLogFilePath) && File.Exists(_sessionLogFilePath))
            {
                try
                {
                    File.WriteAllText(_sessionLogFilePath, string.Empty);
                    ShowPlaceholder();
                }
                catch (Exception ex)
                {
                    Log($"Error clearing session log file: {ex.Message}", Color.Red);
                }
            }
            else
            {
                ShowPlaceholder();
            }
        }

        /// <summary>
        /// Opens the session log file in the default system application.
        /// </summary>
        public void OpenSessionLog()
        {
            if (!string.IsNullOrEmpty(_sessionLogFilePath) && File.Exists(_sessionLogFilePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(_sessionLogFilePath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Log($"Error opening session log file: {ex.Message}", Color.Red);
                }
            }
            else
            {
                Log("Cannot open log file: file not found or path is invalid.", Color.Gray);
            }
        }

        /// <summary>
        /// Opens a SaveFileDialog to save the current log content.
        /// </summary>
        public void SaveLog()
        {
            string logContent = _logControl.Text.Trim();
            string contentToSave = logContent;
            if (logContent.StartsWith(_logPlaceholderText, StringComparison.OrdinalIgnoreCase))
            {
                contentToSave = logContent.Replace(_logPlaceholderText, "").Trim();
            }
            if (string.IsNullOrWhiteSpace(contentToSave))
            {
                MessageBox.Show("The log is empty...", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.FileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(saveFileDialog.FileName, contentToSave);
                        
                        MessageBox.Show(
                            $"Log successfully saved...", 
                            "Save Complete", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Information
                        );
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Failed to save log: {ex.Message}", 
                            "Error", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Error
                        );
                    }
                }
            }
        }

        // --- Placeholder Handlers ---

        /// <summary>
        /// Handles the GotFocus event for the log control.
        /// </summary>
        public void HandleGotFocus()
        {
            if (_isLogPlaceholderActive)
            {
                ClearPlaceholder();
            }
        }

        /// <summary>
        /// Handles the LostFocus event for the log control.
        /// </summary>
        public void HandleLostFocus()
        {
            if (_logControl.TextLength == 0)
            {
                ShowPlaceholder();
            }
        }

        // --- Private Helpers ---

        /// <summary>
        /// Clears the placeholder text and resets formatting.
        /// </summary>
        private void ClearPlaceholder()
        {
            _logControl.Clear();
            _logControl.SelectionColor = _logControl.ForeColor;
            _logControl.Font = _logCustomFont;
            _isLogPlaceholderActive = false;
        }

        /// <summary>
        /// Displays the placeholder text with gray formatting.
        /// </summary>
        // FIXED: Made public to be called by MainForm.ConfigureLogRichTextBox
        public void ShowPlaceholder()
        {
            _logControl.Clear();
            _logControl.SelectionFont = _logCustomFont;
            _logControl.SelectionColor = Color.Gray;
            _logControl.AppendText(_logPlaceholderText);
            _isLogPlaceholderActive = true;
        }

        /// <summary>
        /// Forces the scrollbar to the bottom.
        /// </summary>
        private void FinalizeLogUpdate()
        {
            _logControl.SelectionColor = _logControl.ForeColor;
            _logControl.SelectionFont = _logControl.Font;
            
            NativeMethods.SendMessage(_logControl.Handle, NativeMethods.WM_VSCROLL, (IntPtr)NativeMethods.SB_BOTTOM, IntPtr.Zero);
        }

        /// <summary>
        /// Writes the log entry to the session file asynchronously.
        /// </summary>
        private void WriteToSessionLog(string message, string timeStampText, string spacer, string footer, bool isSeparator)
        {
            string logEntry = timeStampText;
            if (!isSeparator)
            {
                logEntry += spacer + message;
                if (!string.IsNullOrEmpty(footer))
                {
                    logEntry += Environment.NewLine + timeStampText + spacer + footer;
                }
            }
            if (!string.IsNullOrEmpty(_sessionLogFilePath))
            {
                Task.Run(() =>
                {
                    try
                    {
                        File.AppendAllText(_sessionLogFilePath, logEntry + Environment.NewLine);
                    }
                    catch (Exception)
                    {
                        // Suppress file write errors
                    }
                });
            }
        }
    }
}