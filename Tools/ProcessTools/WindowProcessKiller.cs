using System;
using System.Diagnostics;

namespace ParadiseHelper.Tools.ProcessTools
{
    /// <summary>
    /// Provides utility methods for forcefully terminating running processes by name on the operating system.
    /// </summary>
    public static class WindowProcessKiller
    {
        /// <summary>
        /// Attempts to forcefully terminate all running instances of a process with the specified name.
        /// This method uses the internal <see cref="Process.Kill()"/> method for a direct termination request.
        /// </summary>
        /// <param name="processName">The name of the process image (e.g., "chrome" or "notepad.exe").</param>
        public static void KillAllByName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                return;
            }

            // Standardize the process name by removing the ".exe" suffix if present,
            // as Process.GetProcessesByName expects the image name without the extension.
            if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                processName = processName.Substring(0, processName.Length - 4);
            }

            // Retrieve all running processes matching the name.
            Process[] processes = Process.GetProcessesByName(processName);

            if (processes.Length == 0)
            {
                return; // No instances found, exit early.
            }

            // Iterate through and kill each process instance found.
            foreach (var process in processes)
            {
                try
                {
                    // The Kill() method sends a forceful termination request.
                    process.Kill();
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Catches security/access denied errors (e.g., trying to kill a system process).
                    // We catch and ignore to proceed with other processes.
                }
                catch (InvalidOperationException)
                {
                    // Catches cases where the process has already exited since the time GetProcessesByName was called.
                    // We catch and ignore this exception.
                }
                finally
                {
                    // Dispose of the Process object to free up resources.
                    process.Dispose();
                }
            }
        }
    }
}