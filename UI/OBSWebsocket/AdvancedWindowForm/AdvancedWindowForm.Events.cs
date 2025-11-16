using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Types.Events;
using ParadiseHelper.OBS;

namespace UI.OBSWebsocket
{
    // This partial class contains all event handlers (OBS events and UI control clicks/events).
    public partial class AdvancedWindowForm
    {
        // --- Core Form Initialization ---

        /// <summary>
        /// Sets up initial event handlers specific to the main form elements (e.g., TreeView).
        /// </summary>
        private void SetupEventHandlers()
        {
            this.tvScenes.NodeMouseDoubleClick += this.tvScenes_NodeMouseDoubleClick;
        }

        /// <summary>
        /// Initializes OBS-related UI components and event subscriptions for the main form.
        /// </summary>
        private void InitObsUiManagement()
        {
            // Subscribe to OBS connection and virtual cam status changes
            ObsController.Instance.ConnectionStatusChanged += OBSConnectionStatusChanged;
            ObsController.Instance.VirtualCamStatusChanged += OnVirtualCamStateChanged;

            // Subscribe to OBS scene structure changes for automatic TreeView and status updates
            if (ObsController.Instance.Client != null)
            {
                var obsClient = ObsController.Instance.Client;
                obsClient.SceneListChanged += OnSceneStructureChanged;
                obsClient.SceneItemCreated += OnSceneStructureChanged;
                obsClient.SceneItemRemoved += OnSceneStructureChanged;
                obsClient.SceneNameChanged += OnSceneStructureChanged;
                obsClient.InputNameChanged += OnSceneStructureChanged;
                obsClient.CurrentProgramSceneChanged += OnCurrentProgramSceneChanged;
            }

            // Attempt to load data immediately if already connected
            if (ObsController.Instance.IsConnected)
            {
                // Fire and Forget: Load scenes without awaiting to keep UI responsive
                _ = LoadScenesToTreeViewAsync();
                UpdateVirtualCamStatusInitial();
            }
        }

        // --- Form Events ---

        /// <summary>
        /// Handles the Click event for the exit picture box to close the form.
        /// </summary>
        private void exit_pictureBox_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // --- OBS Event Handlers (Primary) ---

        /// <summary>
        /// Handles the OBS connection status changing.
        /// Triggers scene loading and virtual camera status updates upon connection.
        /// </summary>
        private async void OBSConnectionStatusChanged(object sender, EventArgs e)
        {
            if (ObsController.Instance.IsConnected)
            {
                await LoadScenesToTreeViewAsync();
                UpdateVirtualCamStatusInitial();
            }
            else
            {
                lblVirtualCamStatus.Text = "VirtualCam (Disconnected)";
            }
        }

        /// <summary>
        /// Handles the OBS event for virtual camera state changes (e.g., started, stopped).
        /// </summary>
        private void OnVirtualCamStateChanged(object sender, VirtualcamStateChangedEventArgs args)
        {
            string stateText;
            Color stateColor;

            // Determine the VirtualCam state and corresponding label color
            switch (args.OutputState.State)
            {
                case OutputState.OBS_WEBSOCKET_OUTPUT_STARTED:
                    stateText = "VirtualCam Started";
                    stateColor = Color.SeaGreen;
                    break;

                case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED:
                    stateText = "VirtualCam Stopped";
                    stateColor = Color.IndianRed;
                    break;

                case OutputState.OBS_WEBSOCKET_OUTPUT_STARTING:
                    stateText = "VirtualCam starting...";
                    stateColor = Color.Gray;
                    break;

                case OutputState.OBS_WEBSOCKET_OUTPUT_STOPPING:
                    stateText = "VirtualCam stopping...";
                    stateColor = Color.Gray;
                    break;

                default:
                    stateText = "VirtualCam (Unknown)";
                    stateColor = Color.Gray;
                    break;
            }

            // Update the label safely from the UI thread
            BeginInvoke((MethodInvoker)(() =>
            {
                lblVirtualCamStatus.Text = stateText;
                lblVirtualCamStatus.ForeColor = stateColor;
            }));
        }

        /// <summary>
        /// Handles OBS events indicating a change in scene structure (add, remove, rename).
        /// Triggers an asynchronous reload of the scene TreeView and cleans up fields.
        /// </summary>
        private void OnSceneStructureChanged(object sender, EventArgs e)
        {
            if (this.IsHandleCreated)
            {
                this.BeginInvoke((MethodInvoker)(() =>
                {
                    // Fire and forget reload
                    _ = LoadScenesAndCleanupFieldsAsync();
                }));
            }
        }

        /// <summary>
        /// Handles the OBS event for the active program scene changing.
        /// Updates the current scene/source text fields and selects the new scene in the TreeView.
        /// </summary>
        private void OnCurrentProgramSceneChanged(object sender, ProgramSceneChangedEventArgs args)
        {
            string newSceneName = args.SceneName;

            if (this.IsHandleCreated)
            {
                this.BeginInvoke((MethodInvoker)(() =>
                {
                    // 1. Update the current scene field
                    tbSceneName.Text = newSceneName;

                    // 2. Clear the source field, as the selection may not exist in the new scene
                    tbSourceName.Text = string.Empty;

                    // 3. Automatically select the new scene node in the TreeView
                    SelectTreeViewNode(newSceneName);
                }));
            }
        }

        // --- VirtualCam Button Click Handlers ---

        private async void btnVirtualCamStart_Click(object sender, EventArgs e)
        {
            await ObsController.Instance.StartVirtualCamAsync();
        }

        private async void btnVirtualCamStop_Click(object sender, EventArgs e)
        {
            await ObsController.Instance.StopVirtualCamAsync();
        }

        private async void btnVirtualCamToggle_Click(object sender, EventArgs e)
        {
            await ObsController.Instance.ToggleVirtualCamAsync();
        }

        // --- Scene Management Button Click Handlers ---

        /// <summary>
        /// Handles the Click event for the 'Create Scene' button.
        /// </summary>
        private async void btnCreateScene_Click(object sender, EventArgs e)
        {
            string newSceneName = tbNewSceneName.Text.Trim();
            if (string.IsNullOrEmpty(newSceneName))
            {
                MessageBox.Show(
                    "Please enter a name for the new scene.",
                    "Input Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            try
            {
                await ObsController.Instance.CreateSceneAsync(newSceneName);

                MessageBox.Show(
                    $"Scene '{newSceneName}' successfully created.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // Refresh the TreeView and clear input fields
                await LoadScenesToTreeViewAsync();
                tbSceneName.Text = string.Empty;
                tbSourceName.Text = string.Empty;
                tbNewSceneName.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to create scene: {ex.Message}",
                    "OBS Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Handles the Click event for the 'Delete Scene' button.
        /// </summary>
        private async void btnDeleteScene_Click(object sender, EventArgs e)
        {
            string sceneToDelete = tbSceneName.Text.Trim();
            if (string.IsNullOrEmpty(sceneToDelete))
            {
                MessageBox.Show(
                    "Please enter the scene name to delete.",
                    "Input Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            // Confirm deletion with the user
            var dialogResult = MessageBox.Show(
                $"Are you sure you want to delete scene '{sceneToDelete}'? " +
                $"This action is irreversible!",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (dialogResult != DialogResult.Yes) return;

            try
            {
                await ObsController.Instance.DeleteSceneAsync(sceneToDelete);

                MessageBox.Show(
                    $"Scene '{sceneToDelete}' successfully deleted.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // Refresh the TreeView and clear selection
                await LoadScenesToTreeViewAsync();
                tbSceneName.Text = string.Empty;
                tbSourceName.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to delete scene: {ex.Message}",
                    "OBS Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Handles the Click event for the 'Rename Scene' button.
        /// </summary>
        private async void btnRenameScene_Click(object sender, EventArgs e)
        {
            string oldSceneName = tbSceneName.Text.Trim();
            string newSceneName = tbNewSceneName.Text.Trim();

            if (string.IsNullOrEmpty(oldSceneName) || string.IsNullOrEmpty(newSceneName))
            {
                MessageBox.Show(
                    "Please enter both the current and new scene names.",
                    "Input Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            try
            {
                await ObsController.Instance.SetSceneNameAsync(oldSceneName, newSceneName);

                MessageBox.Show(
                    $"Scene '{oldSceneName}' successfully renamed to '{newSceneName}'.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // Refresh the TreeView and update selection
                await LoadScenesToTreeViewAsync();
                tbSceneName.Text = newSceneName;
                tbSourceName.Text = string.Empty;
                tbNewSceneName.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to rename scene: {ex.Message}",
                    "OBS Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // --- Source Management Button Click Handlers ---

        /// <summary>
        /// Handles the Click event for the 'Create Source' button (Window Capture).
        /// </summary>
        private async void btnCreateSource_Click(object sender, EventArgs e)
        {
            string sceneName = tbSceneName.Text.Trim();
            string sourceName = tbNewSourceName.Text.Trim();

            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(sourceName))
            {
                MessageBox.Show(
                    "Please enter the Scene Name and Source Name (Window Capture).",
                    "Input Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            try
            {
                // This adds a 'Window Capture' source item to the scene
                int itemId = await ObsController.Instance.CreateWindowCaptureAsync(sceneName, sourceName);

                if (itemId != -1)
                {
                    MessageBox.Show(
                        $"Source '{sourceName}' (Window Capture) successfully added to scene '{sceneName}'.",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        $"Source '{sourceName}' already exists on scene '{sceneName}'.",
                        "Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }

                // Refresh the TreeView and clear input
                await LoadScenesToTreeViewAsync();
                tbSourceName.Text = string.Empty;
                tbNewSourceName.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to create source: {ex.Message}",
                    "OBS Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Handles the Click event for the 'Delete Source' button.
        /// This removes the scene item, not the global source input.
        /// </summary>
        private async void btnDeleteSource_Click(object sender, EventArgs e)
        {
            string sceneName = tbSceneName.Text.Trim();
            string sourceName = tbSourceName.Text.Trim();

            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(sourceName))
            {
                MessageBox.Show(
                    "Please enter the Scene Name and Source Name to delete.",
                    "Input Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            // Confirm deletion with the user
            var dialogResult = MessageBox.Show(
                $"Are you sure you want to delete source '{sourceName}' from scene '{sceneName}'? " +
                $"This removes the scene item.",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (dialogResult != DialogResult.Yes) return;

            try
            {
                // Deletes the scene item (instance of the source on the scene)
                await ObsController.Instance.DeleteWindowCaptureAsync(sceneName, sourceName);

                MessageBox.Show(
                    $"Source '{sourceName}' successfully removed from scene '{sceneName}'.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // Refresh the TreeView and clear selection
                await LoadScenesToTreeViewAsync();
                tbSourceName.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to delete source: {ex.Message}",
                    "OBS Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Handles the Click event for the 'Rename Source' button.
        /// This renames the global OBS Input.
        /// </summary>
        private async void btnRenameSource_Click(object sender, EventArgs e)
        {
            string oldSourceName = tbSourceName.Text.Trim();
            string newSourceName = tbNewSourceName.Text.Trim();

            if (string.IsNullOrEmpty(oldSourceName) || string.IsNullOrEmpty(newSourceName))
            {
                MessageBox.Show(
                    "Please enter both the current and new source names.",
                    "Input Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            try
            {
                // Renames the OBS Input itself
                await ObsController.Instance.SetWindowCaptureNameAsync(oldSourceName, newSourceName);

                MessageBox.Show(
                    $"Source '{oldSourceName}' successfully renamed to '{newSourceName}'.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // Refresh the TreeView and update selection
                await LoadScenesToTreeViewAsync();
                tbSourceName.Text = newSourceName;
                tbNewSourceName.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to rename source: {ex.Message}",
                    "OBS Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // --- Target Window Change Button Click Handler ---

        /// <summary>
        /// Handles the Click event for the 'Change Target Window' button.
        /// Updates the 'window' setting for the selected source.
        /// </summary>
        private async void btnNewTargetWindowChange_Click(object sender, EventArgs e)
        {
            // 1. Get input data
            string sceneName = tbSceneName.Text.Trim();
            string sourceName = tbSourceName.Text.Trim();
            string targetTitle = tbNewTargetWindowTitle.Text.Trim();
            string targetClass = tbNewTargetWindowClass.Text.Trim();
            string targetName = tbNewTargetWindowName.Text.Trim(); // Executable Name

            // 2. Validate essential data
            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(sourceName))
            {
                MessageBox.Show(
                    "Please select a **Scene** and a **Source** (Window Capture) from the list.",
                    "Input Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            if (string.IsNullOrEmpty(targetTitle) && string.IsNullOrEmpty(targetClass) && string.IsNullOrEmpty(targetName))
            {
                MessageBox.Show(
                    "Please enter at least the **Target Window Title**, **Class**, or **Executable**.",
                    "Input Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            // 3. Change settings via ObsController
            try
            {
                // This method updates the "window" setting for the specified source
                await ObsController.Instance.SetWindowHandleAsync(
                    sourceName: ObsConstants.DefaultAISourceName,
                    windowTitle: targetTitle,
                    windowClass: ObsConstants.WindowCaptureSources.CS2.Class,
                    executableName: ObsConstants.WindowCaptureSources.CS2.Name);

                MessageBox.Show(
                    $"Target window for source '{sourceName}' successfully updated.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // Clear the target input fields
                tbNewTargetWindowTitle.Text = string.Empty;
                tbNewTargetWindowClass.Text = string.Empty;
                tbNewTargetWindowName.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to change target window: {ex.Message}",
                    "OBS Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                Debug.WriteLine($"OBS error (Change Target Window): {ex.Message}");
            }
        }

        // --- Apply Settings Button Click Handlers ---

        /// <summary>
        /// Handles the Click event for the 'Apply Transform Settings' button.
        /// Stretches the selected source to the full area.
        /// </summary>
        private async void btnApplyTransformSettings_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbSceneName.Text) || string.IsNullOrWhiteSpace(tbSourceName.Text))
            {
                MessageBox.Show(
                    "Please enter both the Scene Name and the Source Name.",
                    "Input Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            try
            {
                // Apply the stretch transformation
                bool transformWasApplied = await ObsController.Instance.StretchSourceToFullAreaAsync(tbSceneName.Text, tbSourceName.Text);

                if (transformWasApplied)
                {
                    MessageBox.Show(
                        "Transformation successfully applied! The source has been stretched.",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error applying OBS transformation: {ex.Message}",
                    "OBS Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Handles the Click event for the 'Apply Default OBS Settings' button.
        /// Applies standard video settings to OBS.
        /// </summary>
        private async void btnApplyDefaultObsSettings_Click(object sender, EventArgs e)
        {
            try
            {
                // Apply standard video settings
                bool settingsWereApplied = await ObsController.Instance.ApplyStandardVideoSettingsAsync();

                if (settingsWereApplied)
                {
                    MessageBox.Show(
                        "VideoSettings successfully applied!",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error setting OBS video setting: {ex.Message}",
                    "OBS Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // --- TreeView Events ---

        /// <summary>
        /// Handles the AfterSelect event for the scene TreeView.
        /// Updates text fields and attempts to switch the current OBS scene if a scene node is selected.
        /// </summary>
        private async void tvScenes_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;

            // --- Auto-fill Logic ---
            if (e.Node.Level == 0) // Scene selected
            {
                tbSceneName.Text = e.Node.Text;
                tbSourceName.Text = string.Empty;
            }
            else if (e.Node.Level == 1) // Source selected
            {
                tbSceneName.Text = e.Node.Parent?.Text;
                tbSourceName.Text = e.Node.Text;
            }

            // Redirect focus back to the TreeView
            tvScenes.Focus();

            // --- OBS Command Logic ---
            // Only switch scenes if a scene node (Level 0) is selected
            if (e.Node.Level == 0)
            {
                try
                {
                    await ObsController.Instance.SetCurrentProgramSceneAsync(e.Node.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to set OBS scene: {ex.Message}",
                        "OBS Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        /// <summary>
        /// Handles the NodeMouseDoubleClick event for the scene TreeView.
        /// Toggles the visibility of a source if a source node (Level 1) is double-clicked.
        /// </summary>
        private async void tvScenes_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Level == 1) // Source node
            {
                string sceneName = e.Node.Parent?.Text;
                string sourceName = e.Node.Text;

                if (string.IsNullOrEmpty(sceneName)) return;

                try
                {
                    await ToggleSourceVisibilityAsync(sceneName, sourceName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to toggle source visibility: {ex.Message}",
                        "OBS Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }
    }
}