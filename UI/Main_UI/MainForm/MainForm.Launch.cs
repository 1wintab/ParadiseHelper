using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using Core;
using Data.Settings.LaunchParameters;
using ParadiseHelper.Managers;
using ParadiseHelper.Core.Enums;
using ParadiseHelper.Validators;
using ParadiseHelper.Tools.WinAPI;
using ParadiseHelper.Managers.Steam;
using ParadiseHelper.Managers.Launch;
using ParadiseHelper.Managers.MaFiles;
using ParadiseHelper.Tools.ProcessTools;
using ParadiseHelper.Managers.AccountQueue;
using ParadiseHelper.SteamLogAccounts.SteamAuth;
using ParadiseHelper.Data.Settings.LaunchParameters;
using Newtonsoft.Json;
using UI.Error_UI;
using System.Media;

namespace ParadiseHelper
{
    // This partial class manages the logic for launching, running, and terminating game processes (Steam, CS2), including handling launch parameters, async execution, and process killing.
    public partial class MainForm : Form
    {
        /// <summary>
        /// Initiates the asynchronous process of launching all accounts currently selected in the queue.
        /// Checks for an active launch session to prevent parallel execution.
        /// </summary>
        private async void StartAccounts()
        {
            // Check if a launch task is already running.
            if (launchTask != null && !launchTask.IsCompleted)
            {
                Log("🚀 Another launch is already active. Wait for it to finish.", Color.Crimson);
                return;
            }

            // Retrieve the list of accounts to be launched.
            var loginsToRun = AccountQueueManager.GetQueue().ToList();
            if (loginsToRun.Count == 0)
            {
                Log("⚠️ No accounts selected for launch. Please select one or more accounts.", Color.Gray);

                MessageBox.Show(
                    "Please select one or more accounts from the list before starting the launch process.",
                    "No Accounts Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            if (!AutoAddtionCfgCS2.AddGlobalCS2Cfg())
            {
                return; // Помилка вже була показана, зупиняємо виконання
            }

            // Apply global configuration settings for CS2 before launching.
            AutoAddtionCfgCS2.AddGlobalCS2Cfg();

            // Load mobile authenticator secrets for all accounts.
            Dictionary<string, string> secretMap = MaFilesManager.LoadSecrets();

            launchTask = RunAccountsAsync(secretMap, loginsToRun);
            await launchTask;
        }

        /// <summary>
        /// Displays the custom scrollable error dialog on the main UI thread, passing the formatted log and raw list.
        /// </summary>
        /// <param name="title">The title for the error dialog window.</param>
        /// <param name="formattedContent">The detailed, formatted log content to display in the scrollable area.</param>
        /// <param name="rawLoginsContent">The raw list of logins intended for clipboard copy.</param>
        private void ShowConsolidatedErrorDialog(string title, string formattedContent, string rawLoginsContent)
        {
            // Check if the current call is required to be marshaled to the UI thread.
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    using (var errorForm = new ErrorDisplayForm(title, formattedContent, rawLoginsContent))
                    {
                        errorForm.ShowDialog(this);
                    }
                });
            }
            else
            {
                using (var errorForm = new ErrorDisplayForm(title, formattedContent, rawLoginsContent))
                {
                    errorForm.ShowDialog(this);
                }
            }
        }

        /// <summary>
        /// Executes the launch sequence for a list of accounts concurrently, handling authentication and process management.
        /// </summary>
        /// <param name="secretMap">Dictionary mapping login names to MA file secrets for 2FA.</param>
        /// <param name="logins">The list of account logins to process.</param>
        private async Task RunAccountsAsync(Dictionary<string, string> secretMap, List<string> logins)
        {
            // List to collect all errors related to missing MaFiles (2FA secrets).
            var maFileErrors = new List<string>();

            // Creates a variable to collect all pre-launch configuration error messages.
            string configErrorMessages = "";

            // 1. Steam check
            if (!ExeValidator.IsExecutableValid("Steam"))
            {
                configErrorMessages += "❌ Steam executable was not found.\n";
                Log($"❌ Steam executable path is invalid.", Color.Red);
            }

            // 2. CS2 check
            if (!ExeValidator.IsExecutableValid("CS2"))
            {
                configErrorMessages += "❌ Counter-Strike 2 executable (cs2.exe) was not found.\n";
                Log($"❌ CS2 executable path is invalid.", Color.Red);
            }

            // 3. Determine launch mode.
            bool useAiMode = isStartWithAICFG_checkbox.Checked;

            // 4. OBS check (only if AI mode is enabled)
            if (useAiMode && !ExeValidator.IsExecutableValid("OBS"))
            {
                configErrorMessages += "❌ OBS executable was not found. (AI Mode Required).\n";
                Log($"❌ OBS executable path is invalid (AI Mode Required).", Color.Red);
            }

            // --- Overall check and message display ---
            if (!string.IsNullOrEmpty(configErrorMessages))
            {
                MessageBox.Show(
                    $"Launch cannot proceed due to the following errors:\n\n{configErrorMessages}\n" +
                    $"Please update your path settings: Settings -> Path Manager.",
                    "Launch Configuration Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                Log($"❌ Launch stopped: Please check configuration paths.", Color.Red);

                return;
            }

            // Set the launch mode enum accordingly.
            var launchMode = useAiMode ? LaunchMode.AICore : LaunchMode.Default;

            // Get the necessary launch parameters from configuration files for Steam and CS2.
            string steamParams = GetLaunchParamsForMode("steam", launchMode);
            string cs2Params = GetLaunchParamsForMode("cs2", launchMode);

            // Suspend layout updates on the account panel to improve performance and prevent flickering.
            accountPanel.SuspendLayout();

            // Iterate through each selected account and attempt to launch it.
            foreach (string login in logins)
            {
                // Retrieve the account's password from the database.
                string password = DatabaseHelper.GetPasswordForLogin(login);
                Log($"🚀 Launching account: {login}", Color.DarkViolet);

                // Update UI status to 'Running' (yellow indicator).
                SetAccountIndicatorStatus(login, AccountStatus.Running, Color.Gold);
                AccountStatusTracker.MarkAsRunning(login);

                // Register a cancellation token for this specific launch process.
                CancellationToken token = LaunchControl.Register(login);

                bool success = false;
                try
                {
                    // Execute the main authentication and launch logic on a separate thread (Task.Run).
                    success = await Task.Run(() =>
                    {
                        // Core logic to authenticate Steam, launch the game, and manage processes.
                        return AuthSteam.Run(login, password, secretMap, token, steamParams, cs2Params, launchMode, useAiMode);
                    }, token);
                }
                catch (InvalidOperationException ex)
                {
                    success = false;

                    // CHECK FOR THE SPECIFIC MAFILE ERROR TAG from AuthSteam.ValidateAccountAndApplyPresets
                    if (ex.Message.StartsWith("CRITICAL_MAFILE_MISSING:"))
                    {
                        // Collect the login for later consolidated reporting.
                        maFileErrors.Add(login);

                        Log($"❌ Critical launch failed for {login}: MaFile missing. Consolidated for later report.", Color.Crimson);
                    }
                    else
                    {
                        // Handle other InvalidOperationExceptions immediately.
                        MessageBox.Show(
                            ex.Message,
                            "Initialization Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        // Log the error, stripping newlines for cleaner log output.
                        string logMessage = $"❌ Launch failed for {login}: {ex.Message.Split('\n')[0].Trim()}";
                        Log(logMessage, Color.Crimson);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Handle cancellation if the user stopped the launch process.
                    success = false;

                    Log($"⛔ Launch cancelled for {login}", Color.Crimson);
                }
                finally
                {
                    // Cleanup: Remove the cancellation token and mark the launch as finished regardless of outcome.
                    LaunchControl.Remove(login);
                    AccountStatusTracker.MarkAsFinished(login);

                    // Important: Remove the account from the launch queue to prevent accidental re-launch.
                    AccountQueueManager.Remove(login);

                    if (!success)
                    {
                        // If launch failed or was cancelled, reset status to Idle (red indicator).
                        SetAccountIndicatorStatus(login, AccountStatus.Idle, Color.Crimson);
                    }
                }
            }
            // Resumes the layout logic of the control after the suspension period.
            // This ensures all status changes (like color updates) are rendered simultaneously.
            accountPanel.ResumeLayout(true);

            // --- CONSOLIDATED ERROR REPORTING ---
            // Check if there are any missing MaFile errors to report.
            if (maFileErrors.Any())
            {
                // Create a raw, newline-separated string of affected logins for clipboard copying.
                string rawLoginsList = string.Join(Environment.NewLine, maFileErrors.Select(login => login));

                // Define the English title for the error dialog.
                string errorTitle = $"Authentication Failed: Missing MaFiles for {maFileErrors.Count} Accounts";

                // Format the list for display in the RichTextBox, adding sequential numbering.
                string formattedList = string.Join(Environment.NewLine,
                    maFileErrors.Select((login, index) => $"{index + 1}. {login}"));

                // Assemble the complete content for the scrollable error dialog.
                // This content includes header separators and a footer tip.
                string scrollableContent =
                    $"-------------------------------------------------------------------\n" +
                    $"           Account Logins With Missing maFiles ({maFileErrors.Count} Total)\n" +
                    $"-------------------------------------------------------------------\n" +
                    $"{formattedList}                                                                 \n" +
                    $"-------------------------------------------------------------------\n\n" +
                    $"Tip: Select text and press Ctrl+C to copy the full list of logins.";

                // Display the custom scrollable error dialog to the user.
                ShowConsolidatedErrorDialog(errorTitle, scrollableContent, rawLoginsList);
            }

            // Final log entry indicating the completion of the processing cycle.
            Log($"✅ All accounts have been processed.", Color.DarkSlateBlue);
        }

        /// <summary>
        /// Retrieves the launch parameters (command-line arguments) for a specified application and launch mode.
        /// </summary>
        /// <param name="app">The application name (e.g., "steam" or "cs2").</param>
        /// <param name="mode">The desired launch mode (e.g., Default, AICore).</param>
        /// <returns>A string containing the launch parameters, or an empty string if not found.</returns>
        private string GetLaunchParamsForMode(string app, LaunchMode mode)
        {
            // Find the configuration definition for the selected mode.
            var modeDefinition = ModeManager.AvailableModes.FirstOrDefault(m => m.Mode == mode);
            if (modeDefinition == null) return "";

            string fileName;
            if (app == "cs2")
            {
                fileName = modeDefinition.Cs2ConfigFile;
            }
            else if (app == "steam")
            {
                fileName = modeDefinition.SteamConfigFile;
            }
            else 
            {
                // Return empty string for unknown application requests.
                return ""; 
            }

            // Construct the full path to the configuration file.
            string filePath = Path.Combine(FilePaths.Standard.Settings.ParamsFoldersDirectory, fileName);

            // Check if the file exists and return its content.
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath).Trim();
            }

            // File not found.
            return ""; 
        }

        /// <summary>
        /// Forcefully terminates all processes matching the given names globally across the system.
        /// This method resets all launch status and queues upon successful termination.
        /// </summary>
        /// <param name="processNames">An array of process names (e.g., "steam.exe", "cs2.exe").</param>
        /// <returns>True if any process was killed, otherwise false.</returns>
        private bool KillProcesses(string[] processNames)
        {
            bool anyKilled = false;

            // Iterate through the list of process names to kill.
            foreach (var processName in processNames)
            {
                // Find currently running processes by name (ignoring the .exe extension).
                var matches = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName));

                if (matches.Length > 0)
                {
                    // Use the utility to forcefully kill all matching processes.
                    WindowProcessKiller.KillAllByName(processName);
                    anyKilled = true;
                }
            }

            // Cleanup and status reset if processes were successfully terminated.
            if (anyKilled)
            {
                // Cancel all active launch tasks/tokens.
                LaunchControl.CancelAll();

                // Clear process registries for all accounts.
                SteamProcessRegistry.ClearAll();
                CS2ProcessRegistry.ClearAll();

                // Retrieve and clear all accounts remaining in the launch queue.
                var queuedLogins = AccountQueueManager.GetQueue().ToList();
                foreach (var login in queuedLogins)
                {
                    AccountQueueManager.Remove(login);
                }

                // Reset the UI status for all account panels to Idle.
                foreach (Panel row in accountPanel.Controls.OfType<Panel>())
                {
                    var lbl = row.Controls.OfType<Label>().FirstOrDefault();
                    if (lbl != null)
                    {
                        string login = lbl.Text;
                        row.BackColor = accountPanel.BackColor;
                        SetAccountIndicatorStatus(login, AccountStatus.Idle, Color.Crimson);
                    }
                }

                Log("⛔ Processes forcibly terminated: " + string.Join(", ", processNames), Color.Crimson);
                Log("✅ Launch cancelled. All accounts and the queue were reset.", Color.DarkSlateBlue);
                
                launchTask = null;
            }

            return anyKilled;
        }

        /// <summary>
        /// Forcefully terminates a specific process using its Process ID (PID) via the taskkill command.
        /// This method is used for maximum termination reliability on Windows.
        /// </summary>
        /// <param name="pid">The Process ID of the process to kill.</param>
        private void KillPid(int pid)
        {
            try
            {
                // Launch a command prompt process to execute the taskkill command.
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    // /C executes the command and then terminates. /F forces termination.
                    Arguments = $"/C taskkill /F /PID {pid}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch
            {
                // Log if the command execution itself fails.
                Log($"❌ Failed to kill process {pid}", Color.Crimson);
            }
        }

        /// <summary>
        /// Forcefully terminates all associated Steam and CS2 processes for a single account.
        /// It prioritizes finding the Steam process via the CS2 parent PID (PPID) for reliability.
        /// </summary>
        /// <param name="login">The login name of the account whose processes should be killed.</param>
        private void KillProcessesForAccount(string login)
        {
            // Cancel any pending or ongoing launch task for this specific account.
            LaunchControl.Cancel(login);

            bool cs2Killed = false;
            
            // Stores the PID of the Steam process (parent) found through CS2.
            int steamToKillPid = 0;

            // --- CS2 Process Termination Logic ---

            // 1. Attempt to kill CS2 via the PID stored in the process registry.
            if (CS2ProcessRegistry.TryGetProcessId(login, out int cs2Pid))
            {
                // CRITICAL STEP: Get Steam PID BEFORE killing CS2! (CS2's parent is Steam).
                steamToKillPid = WindowController.GetParentProcessId(cs2Pid);

                KillPid(cs2Pid);
                CS2ProcessRegistry.Unregister(login);
                Log($"🎯 CS2 killed for {login} (PID {cs2Pid})\n", Color.Crimson);
                cs2Killed = true;
            }
            else
            {
                // 2. Fallback: Search for CS2 processes by window title containing the login name.
                foreach (var proc in System.Diagnostics.Process.GetProcessesByName("cs2"))
                {
                    try
                    {
                        // Ensure the process is active and its main window title contains the login.
                        if (!proc.HasExited && proc.MainWindowTitle.Contains(login))
                        {
                            // Get Steam PID BEFORE killing CS2!
                            steamToKillPid = WindowController.GetParentProcessId(proc.Id);

                            proc.Kill();
                            Log($"🎯 CS2 killed by window title for {login} (PID {proc.Id})", Color.Crimson);
                            cs2Killed = true;
                            
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"⚠️ Failed to kill fallback CS2 for {login}: {ex.Message}", Color.Crimson);
                    }
                }
            }

            if (!cs2Killed)
            {
                Log($"⚠️ CS2 process for {login} not found or already closed.", Color.Gray);
            }

            // --- Steam Process Termination Logic ---
            bool steamKilled = false;
            
            // The final PID determined to be the target Steam process.
            int targetSteamPid = 0;

            // A. Use PPID found from CS2 (Most reliable source if CS2 was running).
            if (steamToKillPid > 0)
            {
                targetSteamPid = steamToKillPid;
                Log($"ℹ️ Steam PID identified via CS2 parent process: {targetSteamPid}", Color.Blue);
            }          
            // B. Fallback: Try the registry PID (for cases where CS2 closed first, or PPID logic failed).
            else if (SteamProcessRegistry.TryGetProcessId(login, out int registrySteamPid))
            {
                targetSteamPid = registrySteamPid;
                Log($"ℹ️ Steam PID identified via Registry: {targetSteamPid}", Color.Blue);
            }

            if (targetSteamPid > 0)
            {
                try
                {
                    // Get the process object by the identified PID.
                    var steamProc = System.Diagnostics.Process.GetProcessById(targetSteamPid);

                    // Safety check: ensure we are killing a process named 'steam'.
                    if (steamProc.ProcessName.ToLower() == "steam")
                    {
                        KillPid(targetSteamPid);
                        
                        // Unregister the PID, as the process is now terminated.
                        SteamProcessRegistry.Unregister(login);
                       
                        Log($"⛔ Steam killed for {login} (PID {targetSteamPid})", Color.Crimson);     
                        steamKilled = true;
                    }
                    else
                    {
                        Log($"⚠️ Found parent process (PID {targetSteamPid}) was not 'steam.exe'. Aborting Steam kill.", Color.Crimson);
                    }
                }
                catch (Exception ex)
                {
                    Log($"⚠️ Could not kill Steam (PID {targetSteamPid}): {ex.Message}", Color.Crimson);
                }
            }

            if (steamKilled)
            {
                // Kill SteamWebHelper only if the main Steam process was successfully killed.
                foreach (var proc in System.Diagnostics.Process.GetProcessesByName("steamwebhelper"))
                {
                    // Note: This kills all SteamWebHelper instances, which is acceptable 
                    // since a single Steam process runs them.
                    try
                    {
                        proc.Kill();
                        Log($"🌐 SteamWebHelper [{proc.Id}] killed (alongside Steam)", Color.Crimson);
                    }
                    catch { /* Continue on failure to kill auxiliary process */ }
                }
            }
            else
            {
                Log($"⚠️ Steam process for {login} not found and was not killed.", Color.Gray);
            }

            // --- UI Update Logic ---
            AccountStatusTracker.MarkAsFinished(login);

            // Find the corresponding UI panel and update its status.
            foreach (Panel row in accountPanel.Controls.OfType<Panel>())
            {
                var lbl = row.Controls.OfType<Label>().FirstOrDefault();
                if (lbl != null && lbl.Text == login)
                {
                    // Reset status tag and indicator.
                    row.Tag = AccountStatus.Idle;
                    SetAccountIndicatorStatus(login, AccountStatus.Idle, Color.Crimson);
                    break;
                }
            }
        }
    }
}