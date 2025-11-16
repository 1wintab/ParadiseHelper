using System;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using OBSWebsocketDotNet.Types;
using ParadiseHelper.Core.Enums;
using ParadiseHelper.OBS;

namespace ParadiseHelper
{
    // This partial class contains the status update timer, real-time process monitoring logic, and all methods related to OBS integration, including Websocket connection and scene configuration.
    public partial class MainForm : Form
    {
        // --- Timer and Status Update Logic ---

        /// <summary>
        /// Timer event handler responsible for continuously checking the status of the OBS process 
        /// and all launched game accounts, updating the UI indicators accordingly.
        /// </summary>
        private void UpdateAccountStatuses_Tick(object sender, EventArgs e)
        {
            // --- OBS Process Check ---

            // Check if the OBS process is currently running.
            bool obsIsCurrentlyRunning = ObsProcessManager.IsObsProcessRunning();

            // Handle OBS process shutdown event.
            if (_isObsProcessRunning && !obsIsCurrentlyRunning)
            {
                // The OBS process has just been closed.
                _isObsProcessRunning = false;
                Log("❌ OBS process was closed. Deactivating OBS features.", Color.IndianRed);
                OnActiveAIAccountChanged?.Invoke(null);

                // Iterate through account panels to uncheck the active action box if any.
                foreach (Panel row in accountPanel.Controls.OfType<Panel>())
                {
                    var cb = row.Controls.Find("actionCheckBox", false).FirstOrDefault() as CheckBox;

                    if (cb != null && cb.Checked)
                    {
                        cb.Checked = false;
                        break;
                    }
                }
            }
            // Handle OBS process startup event.
            else if (!_isObsProcessRunning && obsIsCurrentlyRunning)
            {
                // The OBS process has just been detected.
                _isObsProcessRunning = true;
                Log("ℹ️ OBS process has been detected.", Color.Blue);
            }
            // --- End OBS Check ---

            // Retrieve all currently running game processes for efficient lookup.
            var gameProcesses = System.Diagnostics.Process.GetProcessesByName(GameProcessName).ToList();

            // Iterate through each account row in the UI.
            foreach (Panel row in accountPanel.Controls.OfType<Panel>())
            {
                // Attempt to find the account login label.
                Label label = row.Controls.OfType<Label>().FirstOrDefault();
                if (label == null) continue;

                // Account login/username.
                string login = label.Text;

                // UI controls for status indication and action.
                Panel statusIndicator = row.Controls.Find("statusIndicator", false).FirstOrDefault() as Panel;
                CheckBox actionCheckBox = row.Controls.Find("actionCheckBox", false).FirstOrDefault() as CheckBox;

                if (statusIndicator == null || actionCheckBox == null) continue;

                // Get the current color tag of the indicator to check for the "launching" state (Color.Gold).
                var currentIndicatorColor = (Color)(statusIndicator.Tag ?? Color.Crimson);
                
                // Determine the actual visual status (Red, Green, Purple) based on the running process window.
                var newVisualStatus = GetAccountWindowState(login, gameProcesses);
                
                // Get the internal account status tag (Idle, Running, Queued, etc.).
                var currentTagStatus = (AccountStatus)row.Tag;

                // Skip processing if the account is Queued (selected but not started).
                // If a user selects an account (Queued), the timer must NOT reset this selection.
                // We skip the update for this row until its status officially changes (e.g., to "Starting").
                if (currentTagStatus == AccountStatus.Queued)
                {
                    // Skip update to keep the row visually marked as 'selected/queued'.
                    continue;
                }

                // 2. Prevent a flicker from Gold ('Launching') back to Red ('Stopped').
                // If the status check detects no process (Red), but the visual indicator is currently Gold (meaning 'launching'),
                // we skip this update cycle to prevent the indicator from being reset to Red prematurely.
                // The launch process itself will handle failures and set the status to Red if necessary.
                if (newVisualStatus == AccountVisualStatus.Red && currentIndicatorColor == Color.Gold)
                {
                    // Launch is in progress; prevent premature visual status change.
                    continue;
                }

                // --- Cache and Status Change Check ---
                // Skip UI update if the status has not changed since the last check.
                if (_lastAccountStatuses.TryGetValue(login, out var lastStatus) && lastStatus == newVisualStatus)
                {
                    if (newVisualStatus == AccountVisualStatus.Red && currentTagStatus == AccountStatus.Idle)
                        continue;
                    if (newVisualStatus != AccountVisualStatus.Red && currentTagStatus == AccountStatus.Running)
                        continue;
                }

                // Update the status cache.
                _lastAccountStatuses[login] = newVisualStatus;

                // --- UI Update (only runs if status changed) ---
                // Use BeginInvoke to safely update UI elements from the timer thread.
                this.BeginInvoke(new Action(() =>
                {
                    switch (newVisualStatus)
                    {
                        case AccountVisualStatus.Red:
                            // The game process is not running or window not found.
                            statusIndicator.Tag = Color.Crimson;
                            actionCheckBox.Checked = false;
                            actionCheckBox.Enabled = false;
                            row.Tag = AccountStatus.Idle;
                            row.BackColor = accountPanel.BackColor;
                            break;
                        case AccountVisualStatus.Green:
                            // The game process is running, but not marked for special action.
                            statusIndicator.Tag = Color.ForestGreen;
                            actionCheckBox.Checked = false;
                            actionCheckBox.Enabled = false;
                            row.Tag = AccountStatus.Running;
                            row.BackColor = accountPanel.BackColor;
                            break;
                        case AccountVisualStatus.Purple:
                            // The game process is running and marked for special action (OBS integration).
                            statusIndicator.Tag = Color.MediumPurple;
                            actionCheckBox.Enabled = true;
                            row.Tag = AccountStatus.Running;
                            row.BackColor = accountPanel.BackColor;
                            break;
                    }
                    // Force a redraw of the status indicator panel to update the color.
                    statusIndicator.Invalidate();
                }));
            }
        }

        /// <summary>
        /// Resets the background color and internal status tag for all account rows to the default Idle state.
        /// </summary>
        private void ResetAllAccountStatuses()
        {
            // Get the default background color for the account panel.
            Color defaultIdleColor = accountPanel.BackColor;

            // Iterate over all panel controls in the account container.
            foreach (Panel row in accountPanel.Controls.OfType<Panel>())
            {
                // Check if the Tag holds a valid AccountStatus before attempting a reset.
                if (row.Tag is AccountStatus)
                {
                    row.BackColor = defaultIdleColor;
                    
                    // Reset the internal status tag to Idle.
                    row.Tag = AccountStatus.Idle;
                }
            }
        }

        /// <summary>
        /// Determines the visual status (Red, Green, or Purple) of a specific account based on its running game process window title.
        /// </summary>
        /// <param name="login">The login string used to identify the game window.</param>
        /// <param name="gameProcesses">A list of all currently running game processes.</param>
        /// <returns>The calculated visual status of the account.</returns>
        private AccountVisualStatus GetAccountWindowState(string login, List<Process> gameProcesses)
        {
            // Find the game process whose main window title contains the account login.
            var process = gameProcesses.FirstOrDefault(p =>
            {
                try
                {
                    // Check if the window title exists and contains the login string.
                    return !string.IsNullOrEmpty(p.MainWindowTitle) && p.MainWindowTitle.Contains(login);
                }
                catch
                {
                    // Suppress potential "Process already exited" exceptions.
                    return false;
                }
            });

            if (process == null)
            {
                return AccountVisualStatus.Red; // Process not found for this login.
            }

            // Process found, check for the special marker in the window title to determine if it's "Purple" (OBS integrated) or "Green" (running normally).
            return process.MainWindowTitle.Contains(ObsConstants.WindowCaptureSources.CS2.SpecialWindowTitleMarker)
                ? AccountVisualStatus.Purple
                : AccountVisualStatus.Green;
        }

        // --- OBS Configuration Logic ---

        /// <summary>
        /// Executes the special OBS configuration sequence for the given account login, 
        /// including starting OBS, connecting via Websocket, and applying scene/source settings.
        /// </summary>
        /// <param name="login">The account login to be linked to the OBS source.</param>
        /// <returns>True if the special action and configuration were successful; otherwise, false.</returns>
        private async Task<bool> HandleSpecialAction(string login)
        {
            // Load necessary OBS connection parameters from settings.
            var obsConnectionParams = OBSConfigManager.Load();

            // Validate connection parameters.
            // Check: IP must be set AND Port must be a valid, non-zero value.
            if (string.IsNullOrEmpty(obsConnectionParams.Ip) || obsConnectionParams.Port == 0)
            {
                Log("❌ ERROR: OBS Connection parameters are not set. Cannot proceed.", Color.Red);

                // Added: MessageBox to notify the user immediately with troubleshooting steps.
                MessageBox.Show(
                    "OBS connection parameters (IP and Port) are missing or invalid in the settings.\n\n" +
                    "Please go to Settings -> OBS Connection and save valid IP and Port.\n\n" +
                    "IMPORTANT: Also, ensure that:\n" +
                    "1. In OBS (Settings -> WebSocket), the 'Enable WebSocket server' checkbox is checked.\n" +
                    "2. If you set a password in OBS settings, it must be provided here.\n" +
                    "3. OBS is running.",
                    "OBS Configuration Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return false;
            }

            // Initialize the OBS controller with the loaded connection parameters.
            ObsController.Instance.Initialize(obsConnectionParams);

            // Attempt to start the OBS process if it's not already running.
            bool obsProcessStarted = await ObsProcessManager.StartObsIfNotRunningAsync();
            if (obsProcessStarted)
            {
                Log($"▶️ Special action activated for: {login}. OBS process started.", Color.MediumPurple);
                Log("⏳ Waiting for OBS-Websocket connection... (15 seconds)", Color.Blue);

                // Define the connection timeout constant in milliseconds.
                const int ConnectionTimeoutMs = 15000;
                
                // Wait for the Websocket connection to establish.
                bool connected = await ObsController.Instance.WaitForConnectionAsync(ConnectionTimeoutMs);

                if (connected)
                {
                    Log($"✅ OBS is ready. Commands can be sent.", Color.Green);

                    // Execute OBS setup steps sequentially.
                    await CreatingSceneAsync();
                    await CreatingSourceAsync();
                    await EnsureDefaultSourceIsSolelyVisibleAsync();
                    await VirtualCamTurnOffAsync();
                    await StandardSettingsAsync();
                    await StretchingSourceToFullAreaAsync();
                    await SetRightTitleForSourceAsync(login);
                    await VirtualCamTurnOnAsync();

                    Log($"⚙️ OBS settings successfully applied.", Color.Blue);
                    Log($"✨ AI Bot is ready for use.", Color.Black);

                    return true; // Success
                }
                else
                {
                    // Connection failed due to timeout.
                    Log($"❌ ERROR: Failed to connect to OBS within {ConnectionTimeoutMs / 1000} seconds. Check OBS settings (Websocket).", Color.Red);
                    return false;
                }
            }
            else
            {
                // Failed to start OBS process.
                Log($"❌ ERROR: Failed to start the OBS process! Check the path in settings.", Color.Red);
                return false;
            }
        }

        /// <summary>
        /// Ensures the default AI scene exists in OBS and sets it as the current program scene.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CreatingSceneAsync()
        {
            // Check if the default scene already exists.
            bool isSceneExists = await ObsController.Instance.IsSceneExistAsync(ObsConstants.DefaultAISceneName);
            if (!isSceneExists)
            {
                Log($"❌ Scene {ObsConstants.DefaultAISceneName} doesn't exist, creating it now.", Color.Red);
                await ObsController.Instance.CreateSceneAsync(ObsConstants.DefaultAISceneName);
                Log($"✅ Scene {ObsConstants.DefaultAISceneName} has been created!", Color.Green);
            }
            else
            {
                Log($"✅ Scene {ObsConstants.DefaultAISceneName} exists, continuing configuration.", Color.Green);
            }

            // Check if the default scene is currently selected.
            bool isSceneSelected = await ObsController.Instance.GetCurrentProgramSceneAsync() == ObsConstants.DefaultAISceneName;
            if (!isSceneSelected)
            {
                // Set the default scene as the active program scene.
                await ObsController.Instance.SetCurrentProgramSceneAsync(ObsConstants.DefaultAISceneName);
                Log($"✅ Scene {ObsConstants.DefaultAISceneName} has been selected!", Color.Green);
            }
        }

        /// <summary>
        /// Ensures the default Window Capture source exists in the default AI scene.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CreatingSourceAsync()
        {
            // Check if the window capture source exists in the default scene.
            bool isSourceExists = await ObsController.Instance.IsWindowCaptureExistAsync(ObsConstants.DefaultAISceneName, ObsConstants.DefaultAISourceName);
            if (!isSourceExists)
            {
                Log($"❌ Source {ObsConstants.DefaultAISourceName} doesn't exist, creating it now.", Color.Red);
                
                // Create a new Window Capture source.
                await ObsController.Instance.CreateWindowCaptureAsync(ObsConstants.DefaultAISceneName, ObsConstants.DefaultAISourceName);
                Log($"✅ Source {ObsConstants.DefaultAISourceName} has been created", Color.Green);
            }
            else
            {
                Log($"✅ Source {ObsConstants.DefaultAISourceName} exists, continuing configuration.", Color.Green);
            }
        }

        /// <summary>
        /// Ensures that the default Window Capture source is the only visible item in the scene.
        /// Hides all other sources if multiple exist.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task EnsureDefaultSourceIsSolelyVisibleAsync()
        {
            // Get a list of all scene items (sources) in the default scene.
            List<SceneItemDetails> ListOfSources = await ObsController.Instance.GetSourcesAsync(ObsConstants.DefaultAISceneName);
            
            // Check if only one source exists.
            bool isOnlyOneSourceInScene = ListOfSources.Count == 1;

            // Flag to determine if the default source needs to be explicitly shown.
            bool needsDefaultSourceShown = true;

            if (isOnlyOneSourceInScene)
            {
                // If only one source exists, check if it's already visible.
                bool isVisible = await ObsController.Instance.IsSourceVisibleAsync(ObsConstants.DefaultAISceneName, ObsConstants.DefaultAISourceName);
                if (isVisible)
                {
                    needsDefaultSourceShown = false;
                }
            }
            else
            {
                // If multiple sources exist, hide all of them first.
                await ObsController.Instance.HideAllSourcesAsync(ObsConstants.DefaultAISceneName);
            }

            // If the default source is not visible (or if all were just hidden), set its visibility to true.
            if (needsDefaultSourceShown)
            {
                await ObsController.Instance.SetSourceVisibilityAsync(
                    sceneName: ObsConstants.DefaultAISceneName,
                    sourceName: ObsConstants.DefaultAISourceName,
                    isVisible: true);
            }
        }

        /// <summary>
        /// Stops the OBS Virtual Camera if it is currently running to safely apply new video settings.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task VirtualCamTurnOffAsync()
        {
            // Check the current status of the Virtual Camera.
            bool IsVirtualCamStarted = await ObsController.Instance.IsVirtualCamStartedAsync();
            
            if (IsVirtualCamStarted)
            {
                Log($"⏳ Stopping Virtual Camera for applying settings.", Color.Orange);
                await ObsController.Instance.StopVirtualCamAsync();
                Log($"✅ Stopped Virtual Camera.", Color.Green);
            }
        }

        /// <summary>
        /// Applies standard video configuration settings (e.g., resolution, FPS) to OBS.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task StandardSettingsAsync()
        {
            // Attempt to apply standard video settings.
            bool isApplyStandardVideoSettings = await ObsController.Instance.ApplyStandardVideoSettingsAsync();
            if (!isApplyStandardVideoSettings)
            {
                Log($"✅ Standard Video Settings successful applied!", Color.Green);
            }
        }

        /// <summary>
        /// Stretches the default source item to cover the entire canvas area within the scene.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task StretchingSourceToFullAreaAsync()
        {
            // Apply the transform to stretch the source.
            bool transformWasApplied = await ObsController.Instance.StretchSourceToFullAreaAsync(ObsConstants.DefaultAISceneName, ObsConstants.DefaultAISourceName);  
            if (!transformWasApplied)
            {
                Log($"✅ Transform for source {ObsConstants.DefaultAISourceName} successful applied!", Color.Green);
            }
        }

        /// <summary>
        /// Sets the specific window title for the Window Capture source, linking it to the target game instance.
        /// </summary>
        /// <param name="login">The account login to be included in the window title.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SetRightTitleForSourceAsync(string login)
        {
            // Construct the desired window title using the login and the required marker.
            string desiredTitle = $"{login} {ObsConstants.WindowCaptureSources.CS2.SpecialWindowTitleMarker} [Counter-Strike 2]";
            
            // Construct the full window identification string for OBS.
            string desiredWindowString = $"{desiredTitle}:{ObsConstants.WindowCaptureSources.CS2.Class}:{ObsConstants.WindowCaptureSources.CS2.Name}";

            // Get the current window identification string from the OBS source.
            string currentWindowString = await ObsController.Instance.GetWindowCaptureTitleAsync(ObsConstants.DefaultAISourceName);

            // Check if the source's window setting needs to be updated.
            bool isUpdateNeeded = !currentWindowString.Equals(desiredWindowString, StringComparison.Ordinal);
            if (isUpdateNeeded)
            {
                // Apply the new window handle settings to the OBS source.
                await ObsController.Instance.SetWindowHandleAsync(
                    sourceName: ObsConstants.DefaultAISourceName,
                    windowTitle: desiredTitle,
                    windowClass: ObsConstants.WindowCaptureSources.CS2.Class,
                    executableName: ObsConstants.WindowCaptureSources.CS2.Name);
            }
        }

        /// <summary>
        /// Starts the OBS Virtual Camera if it is currently stopped.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task VirtualCamTurnOnAsync()
        {
            // Check the current status of the Virtual Camera.
            bool IsVirtualCamStarted = await ObsController.Instance.IsVirtualCamStartedAsync();
            if (!IsVirtualCamStarted)
            {
                Log($"⏳ Starting Virtual Camera for applying settings.", Color.Orange);
                await ObsController.Instance.StartVirtualCamAsync();
                Log($"✅ Started Virtual Camera.", Color.Green);
            }
        }

        /// <summary>
        /// Universal method to update the account status and visual indicator in the UI.
        /// </summary>
        /// <param name="login">The account login corresponding to the row to be updated.</param>
        /// <param name="status">The new internal account status (e.g., Idle, Running, Queued).</param>
        /// <param name="indicatorColor">The color to set for the status indicator panel.</param>
        private void SetAccountIndicatorStatus(string login, AccountStatus status, Color indicatorColor)
        {
            // Check if the control's handle has been created before calling BeginInvoke.
            if (!this.IsHandleCreated)
            {
                return;
            }

            // Use BeginInvoke to safely update UI elements from a non-UI thread.
            this.BeginInvoke(new Action(() =>
            {
                // Find the required row (Panel) by matching the login label text.
                var row = accountPanel.Controls.OfType<Panel>()
                    .FirstOrDefault(p => p.Controls.OfType<Label>().Any(lbl => lbl.Text == login));

                if (row == null) return;

                // 1. Set the new internal status tag on the row.
                row.Tag = status;

                // 2. Find the status indicator circle panel.
                var statusIndicator = row.Controls.Find("statusIndicator", false).FirstOrDefault() as Panel;
                if (statusIndicator != null)
                {
                    // 3. Set the indicator color (stored in the Tag).
                    statusIndicator.Tag = indicatorColor;
                    
                    // 4. Force a redraw of the indicator panel.
                    statusIndicator.Invalidate();
                }

                // 5. Find and update the action CheckBox.
                var checkBox = row.Controls.Find("actionCheckBox", false).FirstOrDefault() as CheckBox;

                if (checkBox != null)
                {
                    // If the new color is NOT Purple (Special Action state), disable and uncheck the checkbox.
                    if (indicatorColor != Color.MediumPurple)
                    {
                        checkBox.Checked = false; // Key line: Reset the flag.
                        checkBox.Enabled = false; // Prevent immediate re-enablement.
                    }
                    else
                    {
                        // If the status is Purple, the checkbox should be enabled for interaction.
                        checkBox.Enabled = true;
                    }
                }

                // 6. Reset the background color IF the new status is NOT "Queued".
                if (status != AccountStatus.Queued)
                {
                    row.BackColor = accountPanel.BackColor;
                }
            }));
        }
    }
}