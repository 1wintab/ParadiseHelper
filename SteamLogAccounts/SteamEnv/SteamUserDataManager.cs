using System;
using System.IO;
using ParadiseHelper.Data.Settings;

namespace ParadiseHelper.SteamLogAccounts.SteamEnv
{
    /// <summary>
    /// Static utility class dedicated to resolving specific file system paths within the local Steam client installation,
    /// primarily focused on the 'userdata' directory structure for individual accounts.
    /// </summary>
    public static class SteamUserDataManager
    {
        /// <summary>
        /// Retrieves the base installation directory path for the Steam client.
        /// </summary>
        /// <returns>The full directory path where Steam is installed (e.g., "C:\Program Files (x86)\Steam").</returns>
        /// <exception cref="InvalidOperationException">Thrown if the Steam installation path cannot be determined from application settings.</exception>
        public static string GetSteamPath()
        {
            // Retrieve the configuration object for the Steam application, which holds the full executable path.
            AppConfig steamApp = SettingsManager.GetApp("Steam");
            
            // The full path to the Steam executable (e.g., "C:\Steam\steam.exe").
            string fullExePath = steamApp?.Path;

            if (string.IsNullOrWhiteSpace(fullExePath))
                throw new InvalidOperationException("Cannot determine Steam installation path.");

            // Extract the parent directory path from the full executable path.
            // This yields the base Steam directory (e.g., "C:\Steam").
            string steamDirectory = Path.GetDirectoryName(fullExePath);

            if (string.IsNullOrWhiteSpace(steamDirectory))
                throw new InvalidOperationException("Invalid format for Steam path.");

            return steamDirectory;
        }

        /// <summary>
        /// Generates the base path to a specific Steam account's 'userdata' folder.
        /// The structure is typically: [SteamPath]\userdata\[AccountID].
        /// </summary>
        /// <param name="accountId">The 32-bit Account ID of the user.</param>
        /// <returns>The full path to the user's base 'userdata' directory.</returns>
        public static string GetUserdataPath(uint accountId)
        {
            // Combine the base Steam directory with the fixed "userdata" folder name and the account ID.
            return Path.Combine(GetSteamPath(), "userdata", accountId.ToString());
        }

        /// <summary>
        /// Generates the path to the Counter-Strike 2 (CS2) configuration folder for a specific account.
        /// The structure is: [UserdataPath]\730\local\cfg (where 730 is the AppID for CS:GO/CS2).
        /// </summary>
        /// <param name="accountId">The 32-bit Account ID of the user.</param>
        /// <returns>The full path to the user-specific CS2 configuration directory.</returns>
        public static string GetCS2CfgPath(uint accountId)
        {
            // CS2's AppID is 730. The path is nested within the user's userdata folder.
            return Path.Combine(GetUserdataPath(accountId), "730", "local", "cfg");
        }

        /// <summary>
        /// Generates the path to the global (non-account-specific) Counter-Strike 2 configuration folder.
        /// This folder typically contains default configurations and maps.
        /// </summary>
        /// <returns>The full path to the global CS2 'cfg' directory.</returns>
        public static string GetCS2GlobalCfgPath()
        {
            // Build the global path: [SteamPath]\steamapps\common\Counter-Strike Global Offensive\game\csgo\cfg
            string cs2CfgGlobalPath = Path.Combine(
                GetSteamPath(),
                "steamapps",
                "common",
                "Counter-Strike Global Offensive",
                "game",
                "csgo",
                "cfg"
            );

            return cs2CfgGlobalPath;
        }
    }
}