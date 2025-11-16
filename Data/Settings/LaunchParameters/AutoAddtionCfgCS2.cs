using System; 
using System.IO;
using System.Windows.Forms; 
using Core;
using ParadiseHelper.SteamLogAccounts.SteamEnv;

namespace Data.Settings.LaunchParameters
{
    /// <summary>
    /// Utility class responsible for automatically copying a custom configuration file 
    /// into the global Counter-Strike 2 (CS2) configuration directory.
    /// This class is static and cannot be instantiated.
    /// </summary>
    public static class AutoAddtionCfgCS2
    {
        /// <summary>
        /// The fixed file name for the custom configuration file being copied.
        /// </summary>
        public const string AI_CONFIG_FILENAME = "custom_v1.cfg";

        /// <summary>
        /// The local source directory where the AI configuration file is stored within the application.
        /// </summary>
        private static readonly string _sourceDirectory = FilePaths.AI.Settings.CS2ConfigsDirectory;

        /// <summary>
        /// Copies the <c>custom_v1.cfg</c> file from the local application directory 
        /// to the Counter-Strike 2 global configuration folder. Overwrites if the file already exists.
        /// This method now handles path resolution and provides user-friendly error messages.
        /// </summary>
        /// <returns>True if the file was copied successfully, otherwise false.</returns>
        public static bool AddGlobalCS2Cfg()
        {
            try
            {
                // Get game config path here to ensure all application settings (e.g., Steam path) are loaded first.
                string gameCFGDirectory = SteamUserDataManager.GetCS2GlobalCfgPath();

                // Construct the full path to the source configuration file.
                string sourcePath = Path.Combine(_sourceDirectory, AI_CONFIG_FILENAME);

                // Construct the full path for the destination file within the game's configuration folder.
                string destinationPath = Path.Combine(gameCFGDirectory, AI_CONFIG_FILENAME);

                // Check if the source file exists before attempting to copy.
                if (!File.Exists(sourcePath))
                {
                    // Throw a specific exception if the source file is missing.
                    throw new FileNotFoundException(
                        $"Cfg file {AI_CONFIG_FILENAME} not found in default location: {_sourceDirectory}. " +
                        "Please ensure the file is present in the application's configuration folder.");
                }

                // Copy the file, using 'true' to allow overwriting of any existing file.
                File.Copy(sourcePath, destinationPath, true);

                // If we got here, it was successful.
                return true;
            }
            catch (InvalidOperationException ex) // Catches "Cannot determine Steam installation path."
            {
                // Display a user-friendly error if the Steam path is not set.
                MessageBox.Show(
                    $"Failed to add custom CFG.\n\n" +
                    $"The Steam installation path was not found.\n" +
                    $"Please set the path in Settings -> Path Manager.\n\n" +
                    $"Error: {ex.Message}",
                    "Steam Path Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return false;
            }
            catch (FileNotFoundException ex) // Catches the missing source file
            {
                MessageBox.Show(
                    $"Failed to find the source configuration file.\n\n" +
                    $"Details: {ex.Message}",
                    "Missing File Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return false;
            }
            catch (IOException ex) // Catches file copy errors (e.g., permissions)
            {
                // Re-throw with a more user-friendly error message, suggesting running as administrator.
                MessageBox.Show(
                    $"Failed to copy {AI_CONFIG_FILENAME} to the game config directory.\n" +
                    $"Try running the application as an administrator.\n\n" +
                    $"Error: {ex.Message}",
                    "File Copy Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
            }
            catch (Exception ex) // Catches any other unexpected errors
            {
                MessageBox.Show(
                   $"An unexpected error occurred while adding the global CFG.\n\n" +
                   $"Error: {ex.Message}",
                   "Unexpected Error",
                   MessageBoxButtons.OK,
                   MessageBoxIcon.Error
                );
                return false;
            }
        }
    }
}