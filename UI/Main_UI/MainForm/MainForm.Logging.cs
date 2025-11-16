using System.Drawing;
using System.Windows.Forms;

namespace ParadiseHelper
{
    // This partial class now proxies all logging calls to the dedicated UILogger instance.
    public partial class MainForm : Form
    {
        /// <summary>
        /// Writes a time-stamped message to the log control.
        /// Proxies the call to the UILogger instance.
        /// </summary>
        public void Log(string message, Color? color = null, string footer = null)
        {
            _logger.Log(message, color, footer);
        }

        /// <summary>
        /// Helper method to log a decorative separator.
        /// Proxies the call to the UILogger instance.
        /// </summary>
        public void LogSeparator()
        {
            _logger.LogSeparator();
        }

        /// <summary>
        /// Clears the RichTextBox log and the session file.
        /// Proxies the call to the UILogger instance.
        /// </summary>
        private void DeleteLog()
        {
            _logger.DeleteLog();
        }

        /// <summary>
        /// Attempts to open the session log file.
        /// Proxies the call to the UILogger instance.
        /// </summary>
        private void OpenSessionLog()
        {
            _logger.OpenSessionLog();
        }

        /// <summary>
        /// Opens a SaveFileDialog to save the current log content.
        /// Proxies the call to the UILogger instance.
        /// </summary>
        private void SaveLog()
        {
            _logger.SaveLog();
        }

        /// <summary>
        /// Displays the placeholder text.
        /// Proxies the call to the UILogger instance.
        /// </summary>
        private void ShowPlaceholder()
        {
            // This is called by ConfigureLogRichTextBox to set the initial gray text.
            _logger.ShowPlaceholder();
        }
    }
}