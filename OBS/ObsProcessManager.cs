using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using ParadiseHelper.Data.Settings;

public static class ObsProcessManager
{
    /// <summary>
    /// Checks if the OBS process (obs64.exe or obs32.exe) is currently running.
    /// </summary>
    public static bool IsObsProcessRunning()
    {
        return Process.GetProcessesByName("obs64").Length > 0 ||
               Process.GetProcessesByName("obs32").Length > 0;
    }

    /// <summary>
    /// Launches OBS Studio silently (hidden window, minimized to tray).
    /// Steam Overlay is disabled to prevent injection issues.
    /// </summary>
    public static async Task<bool> StartObsIfNotRunningAsync()
    {
        // Skip if already running
        if (IsObsProcessRunning())
            return true;

        var obsConfig = SettingsManager.GetApp("OBS");
        if (obsConfig == null || string.IsNullOrEmpty(obsConfig.Path))
            return false;

        string fullPathToExe = obsConfig.Path;
        if (!File.Exists(fullPathToExe))
            return false;

        string obsDirectoryPath = Path.GetDirectoryName(fullPathToExe)!;
        const int MaxAttempts = 2;

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                // Silent launch configuration
                var startInfo = new ProcessStartInfo
                {
                    FileName = fullPathToExe,
                    Arguments = "--startminimized --disable-updater",
                    WorkingDirectory = obsDirectoryPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Minimized
                };

                // Disable Steam overlay
                startInfo.EnvironmentVariables["SteamNoOverlay"] = "1";

                // Start without UI focus
                startInfo.EnvironmentVariables["OBS_DISABLE_UI_RENDERING"] = "1";
                startInfo.EnvironmentVariables["OBS_STARTUP_NO_ACTIVATE"] = "1";

                // Start OBS silently
                Process proc = Process.Start(startInfo);
                if (proc == null)
                    return false;

                await Task.Delay(1200);

                if (IsObsProcessRunning())
                    return true;

                if (attempt < MaxAttempts)
                    await Task.Delay(2000);
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}