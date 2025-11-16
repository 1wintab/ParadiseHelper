using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using ParadiseHelper.OBS;

namespace UI.OBSWebsocket
{
    // This partial class contains the core asynchronous logic for data fetching and scene management.
    public partial class AdvancedWindowForm
    {
        /// <summary>
        /// Fetches and displays the initial status of the OBS Virtual Camera.
        /// </summary>
        private async void UpdateVirtualCamStatusInitial()
        {
            if (!ObsController.Instance.IsConnected)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    lblVirtualCamStatus.Text = "VirtualCam (OBS Disconnected)";
                    lblVirtualCamStatus.ForeColor = Color.Gray;
                });
                return;
            }

            try
            {
                bool isStarted = await ObsController.Instance.IsVirtualCamStartedAsync();

                BeginInvoke((MethodInvoker)delegate
                {
                    lblVirtualCamStatus.Text = isStarted ? "VirtualCam Started" : "VirtualCam Stopped";
                    lblVirtualCamStatus.ForeColor = isStarted ? Color.SeaGreen : Color.IndianRed;
                });
            }
            catch
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    lblVirtualCamStatus.Text = "VirtualCam (Status Unknown)";
                    lblVirtualCamStatus.ForeColor = Color.Gray;
                });
            }
        }

        /// <summary>
        /// Selects a node in the TreeView based on the provided scene name.
        /// </summary>
        /// <param name="sceneName">The name of the scene to select.</param>
        private void SelectTreeViewNode(string sceneName)
        {
            foreach (TreeNode node in tvScenes.Nodes)
            {
                if (node.Text.Equals(sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    tvScenes.SelectedNode = node;
                    node.EnsureVisible();
                    return;
                }
            }
        }

        /// <summary>
        /// Asynchronously loads all scenes and their sources from OBS into the TreeView.
        /// </summary>
        public async Task LoadScenesToTreeViewAsync()
        {
            if (!ObsController.Instance.IsConnected)
            {
                // Display disconnected status if OBS is not available
                tvScenes.Nodes.Clear();
                tvScenes.Nodes.Add("Not connected to OBS.");
                return;
            }

            try
            {
                // Fetch scene list from OBS
                var scenes = await ObsController.Instance.GetScenesAsync();
                tvScenes.Nodes.Clear();

                foreach (var scene in scenes)
                {
                    var node = new TreeNode(scene.Name);

                    // Fetch sources for the current scene
                    var sceneSources = await ObsController.Instance.GetSourcesAsync(scene.Name);

                    foreach (var item in sceneSources)
                    {
                        node.Nodes.Add(item.SourceName);
                    }
                    tvScenes.Nodes.Add(node);
                }

                tvScenes.ExpandAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading scenes: {ex.Message}",
                    "OBS Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Reloads the scene TreeView and checks if the previously selected source still exists after a structure change.
        /// Clears the source name text field if the source was removed.
        /// </summary>
        private async Task LoadScenesAndCleanupFieldsAsync()
        {
            // Store the current selection before reloading
            string currentSceneName = tbSceneName.Text.Trim();
            string currentSourceName = tbSourceName.Text.Trim();

            // 1. Reload the TreeView
            await LoadScenesToTreeViewAsync();

            // 2. Cleanup logic (if a source was selected before reload)
            if (!string.IsNullOrEmpty(currentSourceName))
            {
                TreeNode sceneNode = null;
                bool sourceExists = false;

                // A. Find the parent scene node
                foreach (TreeNode node in tvScenes.Nodes)
                {
                    if (node.Text.Equals(currentSceneName, StringComparison.OrdinalIgnoreCase))
                    {
                        sceneNode = node;
                        break;
                    }
                }

                // B. Check if the source exists within that scene
                if (sceneNode != null)
                {
                    foreach (TreeNode sourceNode in sceneNode.Nodes)
                    {
                        if (sourceNode.Text.Equals(currentSourceName, StringComparison.OrdinalIgnoreCase))
                        {
                            sourceExists = true;
                            break;
                        }
                    }
                }

                // C. If the source was deleted, clear the source text box
                if (!sourceExists)
                {
                    this.tbSourceName.Text = string.Empty;
                }
            }
        }

        /// <summary>
        /// Toggles the visibility state of a specific source within a scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene containing the source.</param>
        /// <param name="sourceName">The name of the source to toggle.</param>
        private async Task ToggleSourceVisibilityAsync(string sceneName, string sourceName)
        {
            // 1. Get the current visibility state
            bool isVisible = await ObsController.Instance.IsSourceVisibleAsync(sceneName, sourceName);

            // 2. Set the opposite status
            await ObsController.Instance.SetSourceVisibilityAsync(sceneName, sourceName, !isVisible);
        }
    }
}