using Core;
using System;
using System.IO;
using System.Windows.Forms;

namespace ParadiseHelper.Core
{
    /// <summary>
    /// Provides centralized, application-wide management for log files.
    /// Handles log directory initialization, ensures log files exist, and provides methods
    /// for logging application lifecycle events (launch, finish) and errors.
    /// </summary>
    public static class LogManager
    {
        // --- Configuration Constants ---

        // File name for recording application startup events.
        private const string LaunchLogFileName = "launch.log";
        
        // File name for recording application successful shutdown events.
        private const string FinishedLogFileName = "finished.log";
        
        // File name for recording all application exceptions and errors.
        private const string ErrorLogFileName = "errors.txt";

        // Static constructor: Initializes the log directory and ensures all required log files are created.
        static LogManager()
        {
            // Create the log directory if it does not already exist.
            if (!Directory.Exists(FilePaths.LogsDirectory))
            {
                Directory.CreateDirectory(FilePaths.LogsDirectory);
            }

            // Ensure all specific log files are present.
            EnsureLogFilesExist();
        }

        // Ensures all necessary log files (launch, finished, errors) are created within the LogsDirectory if they do not exist.
        private static void EnsureLogFilesExist()
        {
            string launchLogPath = Path.Combine(FilePaths.LogsDirectory, LaunchLogFileName);
            string finishedLogPath = Path.Combine(FilePaths.LogsDirectory, FinishedLogFileName);
            string errorLogPath = Path.Combine(FilePaths.LogsDirectory, ErrorLogFileName);

            // Check and create the launch log file, immediately disposing the file stream to prevent locking the file.
            if (!File.Exists(launchLogPath))
            {
                File.Create(launchLogPath).Dispose();
            }

            // Check and create the finished log file.
            if (!File.Exists(finishedLogPath))
            {
                File.Create(finishedLogPath).Dispose();
            }

            // Check and create the error log file.
            if (!File.Exists(errorLogPath))
            {
                File.Create(errorLogPath).Dispose();
            }
        }

        /// <summary>
        /// Logs the application startup time and event to the launch log file.
        /// </summary>
        public static void LogLaunch()
        {
            try
            {
                string logFilePath = Path.Combine(FilePaths.LogsDirectory, LaunchLogFileName);
                string formattedMessage = $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} | Application started.{Environment.NewLine}";

                // Append the timestamped 'Application started' message to the launch log.
                File.AppendAllText(logFilePath, formattedMessage);
            }
            catch (Exception ex)
            {
                // Fallback mechanism: Show a message box if file system access fails during logging.
                MessageBox.Show(
                    $"Failed to write to launch log file: {ex.Message}",
                    "Logging Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Logs the application successful shutdown time and event to the finished log file.
        /// </summary>
        public static void LogFinished()
        {
            try
            {
                string logFilePath = Path.Combine(FilePaths.LogsDirectory, FinishedLogFileName);
                string formattedMessage = $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} | Application finished successfully.{Environment.NewLine}";

                // Append the timestamped 'Application finished successfully' message to the finished log.
                File.AppendAllText(logFilePath, formattedMessage);
            }
            catch (Exception ex)
            {
                // Fallback mechanism: Show a message box if file system access fails during logging.
                MessageBox.Show(
                    $"Failed to write to finished log file: {ex.Message}",
                    "Logging Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Logs a specific error message along with a timestamp to the error log file.
        /// </summary>
        /// <param name="message">The detailed error message to be recorded.</param>
        public static void LogError(string message)
        {
            try
            {
                string logFilePath = Path.Combine(FilePaths.LogsDirectory, ErrorLogFileName);
                string formattedMessage = $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} | {message}{Environment.NewLine}";

                // Append the timestamped error message to the error log.
                File.AppendAllText(logFilePath, formattedMessage);
            }
            catch (Exception ex)
            {
                // Fallback mechanism: Show a message box if file system access fails during logging.
                MessageBox.Show(
                    $"Failed to write to error log file: {ex.Message}",
                    "Logging Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}