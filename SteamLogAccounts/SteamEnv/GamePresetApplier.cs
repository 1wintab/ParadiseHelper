using System.IO;
using System.Linq;
using System.Windows.Forms;
using Core;

namespace ParadiseHelper.SteamLogAccounts.SteamEnv
{
    /// <summary>
    /// Static class responsible for copying predefined game configuration files (presets)
    /// into the specific Counter-Strike 2 (CS2) user data directory for a given Steam account.
    /// </summary>
    public static class GamePresetApplier
    {
        /// <summary>
        /// Attempts to copy configuration files (.cfg or .txt) from the 'game_presets' folder
        /// to the CS2 user config path tied to the provided Steam Account ID.
        /// </summary>
        /// <param name="accountId">The 32-bit Steam Account ID (e.g., from the SteamID3 structure) identifying the user's CS2 configuration folder.</param>
        /// <returns>True if presets were successfully copied; otherwise, false if no presets were found or the presets folder was missing.</returns>
        public static bool ApplyGamePresets(uint accountId)
        {
            // The directory where user-defined preset files are stored.
            string presetsSourceDirectory = FilePaths.Standard.Settings.GamePresetsDirectory;

            // 1. Ensure the presets directory exists.
            if (!Directory.Exists(presetsSourceDirectory))
            {
                // Create the directory if missing and inform the user.
                Directory.CreateDirectory(presetsSourceDirectory);
                MessageBox.Show(
                    $"Presets folder was not found. \n" +
                    $"It has now been created: {presetsSourceDirectory}\n" +
                    "Please place your .cfg or .txt files inside this folder.",
                    "Presets Folder Created",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // Stop operation since there are no presets to apply yet.
                return false;
            }

            // 2. Get the destination path for CS2 configuration files (user-specific).
            
            // This path should look like '.../steamapps/common/Counter-Strike Global Offensive/csgo/cfg/{accountID}/'.
            string targetDir = SteamUserDataManager.GetCS2CfgPath(accountId);
            
            // Ensure the target directory for the specific account exists.
            Directory.CreateDirectory(targetDir);

            // 3. Get relevant files (.cfg or .txt) from the root of the presets directory.
            var presetFiles = Directory.GetFiles(presetsSourceDirectory, "*.*", SearchOption.TopDirectoryOnly)
                                       // Filter for files with .txt or .cfg extensions.
                                       .Where(f => f.EndsWith(".txt") || f.EndsWith(".cfg"))
                                       .ToList();

            // Check if any preset files were actually found.
            if (!presetFiles.Any())
            {
                MessageBox.Show(
                    "No .cfg or .txt files were found inside the 'game_presets' folder.\n\n" +
                    "Game launch has been cancelled.",
                    "No Presets Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                // Stop operation if no files are found.
                return false;
            }

            // 4. Apply each file by copying it to the target directory, overwriting existing files.
            foreach (string filePath in presetFiles)
            {
                // Extract the file name to use for the destination path.
                string fileName = Path.GetFileName(filePath);
                
                // Construct the full destination path.
                string destPath = Path.Combine(targetDir, fileName);
                
                // Copy the file, overwriting the destination if it already exists.
                File.Copy(filePath, destPath, overwrite: true);
            }

            // Presets successfully applied to the target CS2 configuration folder.
            return true;
        }
    }
}