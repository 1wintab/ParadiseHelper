using System;
using System.IO;
using Core;

namespace SteamLogAccounts.SteamAuth
{
    /// <summary>
    /// A static class containing application-wide constants, file paths, and configuration values, 
    /// primarily related to Steam authentication, image recognition, and process control.
    /// </summary>
    public static class AuthConstants
    {
        // --- Private Static Readonly Fields for Launch Parameters (Loaded once at startup) ---

        // Stores the Steam client launch parameters read from 'steam_launch.txt'.
        private static readonly string _steamLaunchParams;
        
        // Stores the CS2 launch parameters read from 'cs2_launch.txt'.
        private static readonly string _cs2LaunchParams;
        
        // Stores the combined launch parameters.
        private static readonly string _fullLaunchParams;

        // --- Static Constructor: Initialize Launch Parameters Once ---

        /// <summary>
        /// Initializes the <see cref="AuthConstants"/> class, ensuring that file-based
        /// configuration values like launch parameters are loaded only one time.
        /// </summary>
        static AuthConstants()
        {
            // Load Steam Launch Parameters
            string steamPath = Path.Combine(FilePaths.Standard.Settings.ParamsFoldersDirectory, "steam_launch.txt");
            _steamLaunchParams = TryReadFile(steamPath);

            // Load CS2 Launch Parameters
            string cs2Path = Path.Combine(FilePaths.Standard.Settings.ParamsFoldersDirectory, "cs2_launch.txt");
            _cs2LaunchParams = TryReadFile(cs2Path);

            // Combine both sets of parameters
            _fullLaunchParams = $"{_steamLaunchParams} {_cs2LaunchParams}".Trim();
        }

        /// <summary>
        /// Safely reads the content of a file, returning an empty string if the file is missing or an error occurs.
        /// </summary>
        /// <param name="path">The full path to the file.</param>
        /// <returns>The content of the file, or an empty string.</returns>
        private static string TryReadFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return File.ReadAllText(path).Trim();
                }
                // Log the missing file instead of letting ReadAllText throw an exception
                Console.WriteLine($"[AuthConstants WARNING] Configuration file not found: {path}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthConstants ERROR] Failed to read file {path}. Error: {ex.Message}");
                return string.Empty;
            }
        }

        // --- Template Paths for Image Recognition ---

        /// <summary>Path to the image template for the login field.</summary>
        public static string LoginTemplatePath => Path.Combine(FilePaths.Standard.Templates.SteamDirectory, "login_field.png");

        /// <summary>Path to the image template for the password field.</summary>
        public static string PasswordTemplatePath => Path.Combine(FilePaths.Standard.Templates.SteamDirectory, "password_field.png");

        /// <summary>Path to the image template for the 'Add another account' button.</summary>
        public static string AddAnotherAccountTemplatePath => Path.Combine(FilePaths.Standard.Templates.SteamDirectory, "addAnotherAccount_button.png");

        /// <summary>Path to the image template for the 'Sign In' button.</summary>
        public static string SignInButtonTemplatePath => Path.Combine(FilePaths.Standard.Templates.SteamDirectory, "sign_in_button.png");

        /// <summary>Path to the image template for the Steam Guard 2FA code field.</summary>
        public static string SteamGuardCodeTemplatePath => Path.Combine(FilePaths.Standard.Templates.SteamDirectory, "2FA.png");

        /// <summary>Path to the image template for a generic retries error message.</summary>
        public static string RetriesErrorTemplatePath => Path.Combine(FilePaths.Standard.Templates.SteamDirectory, "retries_error.png");

        /// <summary>Path to the image template for the 'Play Anyway' button.</summary>
        public static string PlayAnywayTemplatePath => Path.Combine(FilePaths.Standard.Templates.SteamDirectory, "playAnyway_button.png");

        /// <summary>Path to the image template for the 'Continue Play' button.</summary>
        public static string ContinuePlayTemplatePath => Path.Combine(FilePaths.Standard.Templates.SteamDirectory, "continuePlay_button.png");

        /// <summary>Path to the image template for the Cloud Conflict warning label.</summary>
        public static string CloudConflictTemplatePath => Path.Combine(FilePaths.Standard.Templates.SteamDirectory, "CloudConflict_label.png");

        /// <summary>Path to the image template for the Cloud Save selection image.</summary>
        public static string SelectClouldSaveTemplatePath => Path.Combine(FilePaths.Standard.Templates.SteamDirectory, "SelectClouldSave_image.png");

        // --- Thresholds and Configuration ---

        /// <summary>
        /// Standard image matching confidence threshold (e.g., 0.97).
        /// </summary>
        public const double Threshold = 0.97;

        /// <summary>
        /// Lower confidence threshold specifically for the "Add another account" button (e.g., 0.85).
        /// </summary>
        public const double ThresholdForPlus = 0.85;

        /// <summary>
        /// Maximum attempts for certain operations before failing.
        /// </summary>
        public const int MaxRetries = 3;

        /// <summary>
        /// Maximum time allowed for the entire login process (50 seconds).
        /// </summary>
        public static readonly TimeSpan LoginTimeout = TimeSpan.FromSeconds(50);

        /// <summary>
        /// Number of 100ms intervals to wait for the CS2 window (150 * 100ms = 15 seconds).
        /// </summary>
        public const int Cs2WaitIterations = 150;

        // --- Configuration and Launch Parameters ---

        /// <summary>
        /// Path to the main application configuration file defining application locations.
        /// </summary>
        public static string LauncherConfigJsonPath =>
            Path.Combine(FilePaths.Standard.Settings.ConfigDirectory, "AppLocations.json");

        /// <summary>
        /// Steam client launch parameters read from 'steam_launch.txt'. Loaded once at startup.
        /// </summary>
        public static string SteamLaunchParams => _steamLaunchParams;

        /// <summary>
        /// Counter-Strike 2 (CS2) launch parameters read from 'cs2_launch.txt'. Loaded once at startup.
        /// </summary>
        public static string Cs2LaunchParams => _cs2LaunchParams;

        /// <summary>
        /// Combines both Steam and CS2 launch parameters. Loaded once at startup.
        /// </summary>
        public static string FullLaunchParams => _fullLaunchParams;
    }
}