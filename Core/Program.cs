using System;
using System.IO;
using System.Windows.Forms;
using Core;
using ParadiseHelper.Core;
using ParadiseHelper.Data.Settings;

namespace ParadiseHelper
{
    /// <summary>
    /// The main entry point class for the Windows Forms application.
    /// Handles application initialization, settings loading, directory setup, and unhandled exception logging.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// Configures application settings, initializes logging, runs the main form, and handles unhandled crashes.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Log the application startup event.
            LogManager.LogLaunch();

            // --- Standard Windows Forms Initialization ---
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Set the application to ignore DPI scaling, ensuring consistent UI size regardless of system settings.
            Application.SetHighDpiMode(HighDpiMode.DpiUnaware);

            // Ensure all necessary application directories exist before proceeding with file operations.
            EnsureAppDirectories();

            // Load application settings from the configuration files.
            SettingsManager.Load();

            try
            {
                // Initialize the main application window.
                var mainForm = new MainForm();

                // Attach an event handler to log a successful application closure when the main form is closed.
                mainForm.FormClosed += (sender, e) => LogManager.LogFinished();

                // Start the main application message loop.
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                // --- Unhandled Crash Handling ---

                // Log the critical error using the standard error logger.
                LogManager.LogError($"Application crashed. Exception: {ex.Message}");

                // Ensure the Logs directory exists (redundant but safe for emergencies). 
                Directory.CreateDirectory(FilePaths.LogsDirectory);

                // Define the path for the dedicated crash log file.
                string logPath = Path.Combine(FilePaths.LogsDirectory, "crash.log");
                
                // Format the detailed crash message including the full exception stack trace.
                string msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Unhandled exception:\n{ex}\n";

                // Write the full exception details to the dedicated crash log file. 
                File.WriteAllText(logPath, msg);

                // Notify the user about the crash and the saved log file location.
                MessageBox.Show(
                    "Oops! Something went wrong during startup.\n\n" +
                    "A crash.log has been saved in the Logs folder.\n" +
                    "The file will now be opened.",
                    "Application Crash",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                // Automatically open the crash log file using the system's default text editor (Notepad).
                System.Diagnostics.Process.Start("notepad.exe", logPath);
            }
        }

        /// <summary>
        /// Creates all essential directories required for the application's operation, 
        /// including data, logs, configuration, and AI model storage folders, if they do not already exist.
        /// </summary>
        private static void EnsureAppDirectories()
        {
            string[] folders = {
                FilePaths.Standard.MaFilesDirectory,
                FilePaths.LogsDirectory,
                FilePaths.Standard.Settings.ConfigDirectory,
                FilePaths.AI.ModelsDirectory
            };

            // Iterate through the required folders and create each one if it does not exist.
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
        }
    }
}