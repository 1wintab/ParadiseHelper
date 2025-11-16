using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ParadiseHelper.Core
{
    /// <summary>
    /// Utility class for managing and launching external processes based on configuration read from a JSON file.
    /// </summary>
    public static class ProcessManager
    {
        /// <summary>
        /// Launches an external executable defined by a path stored within a JSON configuration file.
        /// The JSON structure is expected to be: { "Apps": { "appKey": { "Path": "C:\\path\\to\\app.exe" } } }.
        /// </summary>
        /// <param name="jsonPath">The full file path to the JSON configuration file.</param>
        /// <param name="appKey">The key within the JSON config (e.g., "GameClient") that specifies the application details.</param>
        /// <param name="launchArgs">Optional command-line arguments to pass to the executable.</param>
        public static void LaunchFromJson(string jsonPath, string appKey, string launchArgs = "")
        {
            // 1. Initial file existence check.
            if (!File.Exists(jsonPath))
            {
                MessageBox.Show("Config file not found:\n" + jsonPath, "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Read and deserialize the JSON configuration file using dynamic for flexible structure access.
                string json = File.ReadAllText(jsonPath);
                dynamic config = JsonConvert.DeserializeObject(json);

                // 2. Configuration structure check (e.g., config.Apps.AppKey.Path).
                if (config?.Apps?[appKey]?["Path"] == null)
                {
                    MessageBox.Show(
                        $"No configuration found for \"{appKey}\" in JSON file.\n\nPlease check the config structure.",
                        "Missing Configuration",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                // Extract the executable path.
                string exePath = config.Apps[appKey]["Path"]?.ToString();

                // 3. Executable file existence check.
                if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                {
                    MessageBox.Show(
                        $"File not found:\n{exePath}\n\nPlease check the path in the settings or configuration file.",
                        "Executable Missing",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                // 4. Launch the process using ProcessStartInfo.
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = launchArgs,  
                    UseShellExecute = true 
                };

                Process.Start(startInfo);
            }
            // 5. General error handling (e.g., JSON parsing failure, I/O errors, launch failure).
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to launch:\n{ex.Message}",
                    "Process Launch Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}