using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Communication;
using OBSWebsocketDotNet.Types.Events;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace ParadiseHelper.OBS
{
    /// <summary>
    /// Controller class for managing OBS Studio via the OBS-Websocket API.
    /// Implemented as a Singleton to ensure a single, consistent connection.
    /// </summary>
    public class ObsController
    {
        #region 0. Singleton, Initialization, and Properties

        // Singleton Instance
        private static readonly ObsController instance = new ObsController();

        /// <summary>
        /// Gets the singleton instance of the ObsController.
        /// </summary>
        public static ObsController Instance => instance;

        /// <summary>
        /// Gets the raw OBSWebsocket client instance for advanced operations.
        /// </summary>
        public OBSWebsocket Client => _obswebsocket;

        // Fields

        // The core OBS-Websocket client instance
        private OBSWebsocket _obswebsocket;

        // Cached server URL (e.g., "ws://127.0.0.1:4455")
        private string _serverURL;

        // Cached server password
        private string _serverPassword; 

        // Cache for source 'kind' (type) lookups to avoid frequent API calls
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _sourceKindCache =
            new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

        // Events
        /// <summary>
        /// Fires when the connection state to the OBS-Websocket server changes (Connected or Disconnected).
        /// </summary>
        public event EventHandler ConnectionStatusChanged;

        /// <summary>
        /// Fires when the OBS Virtual Camera state changes (e.g., started, stopped).
        /// </summary>
        public event EventHandler<VirtualcamStateChangedEventArgs> VirtualCamStatusChanged;

        // Constructor
        private ObsController()
        {
            _obswebsocket = new OBSWebsocket();
            _obswebsocket.Connected += OnObsConnected;
            _obswebsocket.Disconnected += OnObsDisconnected;
            _obswebsocket.VirtualcamStateChanged += OnVirtualCamStateChanged;
        }

        // --- Event Handlers ---

        // Forward the internal OBS-Websocket event to our public event
        private void OnVirtualCamStateChanged(object sender, VirtualcamStateChangedEventArgs e)
        {
            VirtualCamStatusChanged?.Invoke(this, e);
        }

        // Forward the internal Connected event
        private void OnObsConnected(object sender, EventArgs e) => ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);

        // Forward the internal Disconnected event
        private void OnObsDisconnected(object sender, ObsDisconnectionInfo e) => ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);

        // Public Properties
        /// <summary>
        /// Gets a value indicating whether the controller has been initialized with server parameters.
        /// </summary>
        public bool IsConfigured => !string.IsNullOrEmpty(_serverURL);

        /// <summary>
        /// Gets a value indicating whether the controller is currently connected to OBS-Websocket.
        /// </summary>
        public bool IsConnected => _obswebsocket.IsConnected;

        // --- Connection Logic ---

        /// <summary>
        /// Initializes the controller with new connection parameters.
        /// Disconnects if already connected.
        /// </summary>
        /// <param name="parameters">The connection parameters (IP, Port, Password).</param>
        public void Initialize(OBSConnectionParams parameters)
        {
            if (_obswebsocket.IsConnected)
            {
                _obswebsocket.Disconnect();
            }

            if (string.IsNullOrEmpty(parameters.Ip) || parameters.Port == 0) return;

            _serverURL = $"ws://{parameters.Ip}:{parameters.Port}";
            _serverPassword = parameters.Password;

            Connect();
        }

        /// <summary>
        /// Attempts to connect to the OBS-Websocket server using the stored configuration.
        /// </summary>
        public void Connect()
        {
            if (IsConfigured && !_obswebsocket.IsConnected)
            {
                try
                {
                    _obswebsocket.ConnectAsync(_serverURL, _serverPassword);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"OBS connection attempt failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Disconnects from the OBS-Websocket server if connected.
        /// </summary>
        public void Disconnect()
        {
            if (_obswebsocket.IsConnected)
            {
                _obswebsocket.Disconnect();
            }
        }

        /// <summary>
        /// Asynchronously waits for the connection to OBS-Websocket, repeatedly attempting to connect
        /// until the timeout is reached.
        /// This is crucial for handling OBS process startup and
        /// internal server initialization time.
        /// </summary>
        /// <param name="timeoutMs">The maximum time to wait for a connection in milliseconds.</param>
        /// <param name="retryDelayMs">The delay between connection attempts in milliseconds (e.g., 200ms for aggressive check).</param>
        /// <returns>
        /// <see langword="true"/> if the connection was already established or successfully established before the timeout;
        /// <see langword="false"/> if the connection parameters are invalid or the timeout was reached.
        /// </returns>
        public async Task<bool> WaitForConnectionAsync(int timeoutMs, int retryDelayMs = 200)
        {
            // Immediately check if the controller is already connected (e.g., OBS was running before the action).
            if (_obswebsocket.IsConnected) return true;

            // Ensure connection parameters (URL/Password) were set by the Initialize method.
            if (string.IsNullOrEmpty(_serverURL)) return false;

            int totalTime = 0;
            while (totalTime < timeoutMs)
            {
                try
                {
                    // Attempt connection using the parameters set during initialization.
                    Connect();

                    if (_obswebsocket.IsConnected)
                    {
                        return true; // Successful connection achieved.
                    }
                }
                catch
                {
                    // Ignore connection errors;
                    // they are expected while OBS is starting up and the server is not ready.
                }

                await Task.Delay(retryDelayMs); // Pause between retry attempts.
                totalTime += retryDelayMs;
            }

            return false; // Timeout reached without successful connection.
        }

        #endregion // 0. Singleton, Initialization, and Properties

        // -------------------------------------------------------

        #region 1. Scene Management Methods

        /// <summary>
        /// Creates a new scene in OBS if it doesn't already exists.
        /// </summary>
        /// <param name="sceneName">Name of the scene to create.</param>
        public async Task CreateSceneAsync(string sceneName)
        {
            if (await IsSceneExistAsync(sceneName)) return;

            try
            {
                await Task.Run(() => _obswebsocket.CreateScene(sceneName));
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (CreateSceneAsync): {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes an existing scene from OBS.
        /// </summary>
        /// <param name="sceneName">Name of the scene to delete.</param>
        public async Task DeleteSceneAsync(string sceneName)
        {
            if (!await IsSceneExistAsync(sceneName)) return;

            try
            {
                await Task.Run(() => _obswebsocket.RemoveScene(sceneName));
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (DeleteSceneAsync): {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if scene with the specified name exists in OBS.
        /// </summary>
        /// <param name="sceneName">Name of scene to check.</param>
        /// <returns>True if the scene exists; otherwise False.</returns>
        public async Task<bool> IsSceneExistAsync(string sceneName)
        {
            try
            {
                var sceneListResponse = await Task.Run(() => _obswebsocket.GetSceneList());
                return sceneListResponse.Scenes.Any(s => s.Name.Equals(sceneName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex) when (ex is OperationCanceledException ||
                                       ex is AggregateException && ex.InnerException is OperationCanceledException ||
                                       ex is NullReferenceException)
            {
                Debug.WriteLine($"OBS Error (IsSceneExistAsync): {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OBS Error (IsSceneExistAsync): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Renames an existing scene in OBS.
        /// </summary>
        /// <param name="sceneName">Current name of the scene.</param>
        /// <param name="newSceneName">The new name for the scene.</param>
        public async Task SetSceneNameAsync(string sceneName, string newSceneName)
        {
            if (!await IsSceneExistAsync(sceneName)) return;

            try
            {
                await Task.Run(() => _obswebsocket.SetSceneName(sceneName, newSceneName));
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (SetSceneNameAsync): {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves a list of all scenes in the current OBS profile.
        /// </summary>
        /// <returns>A list containing basic information for each scene.</returns>
        public async Task<List<SceneBasicInfo>> GetScenesAsync()
        {
            try
            {
                var response = await Task.Run(() => _obswebsocket.GetSceneList());
                return response.Scenes.ToList();
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (GetScenesAsync): {ex.Message}");
                return new List<SceneBasicInfo>(); // Return empty list on error
            }
        }

        /// <summary>
        /// Gets the name of the currently active program scene.
        /// </summary>
        /// <returns>The name of the current program scene.</returns>
        public async Task<string> GetCurrentProgramSceneAsync()
        {
            try
            {
                return await Task.Run(() => _obswebsocket.GetCurrentProgramScene());
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (GetCurrentProgramSceneAsync): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets the currently active program scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene to switch to.</param>
        public async Task SetCurrentProgramSceneAsync(string sceneName)
        {
            try
            {
                await Task.Run(() => _obswebsocket.SetCurrentProgramScene(sceneName));
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (SetCurrentProgramSceneAsync): {ex.Message}");
            }
        }

        #endregion // 1. Scene Management Methods

        // ------------------------------------------------------

        #region 2. Window Capture Control

        /// <summary>
        /// Creates a scene item (source instance) for a Window Capture input on a specified scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene to add the item to.</param>
        /// <param name="sourceName">The name of the Window Capture source/input.</param>
        /// <returns>The unique Item ID of the newly created scene item, or -1 if it already exists.</returns>
        public async Task<int> CreateWindowCaptureAsync(string sceneName, string sourceName)
        {
            // Call the new helper method with the specific Input Kind.
            return await CreateAndAddInputToSceneAsync(
                sceneName,
                sourceName,
                ObsConstants.InputKind_WindowCapture
            );
        }

        /// <summary>
        /// Permanently deletes a specific Window Capture source item from a scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene containing the item.</param>
        /// <param name="sourceName">The name of the Window Capture source to delete.</param>
        public async Task DeleteWindowCaptureAsync(string sceneName, string sourceName)
        {
            int sceneItemId = await GetWindowCaptureItemIdAsync(sceneName, sourceName);
            if (sceneItemId == -1) return;

            try
            {
                await Task.Run(() => _obswebsocket.RemoveSceneItem(sceneName, sceneItemId));
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (DeleteWindowCaptureAsync): {ex.Message}");
            }
        }

        /// <summary>
        /// Renames an existing OBS Input/Source (not the scene item).
        /// </summary>
        /// <param name="sourceName">Current name of the source.</param>
        /// <param name="newSourceName">The new name for the source.</param>
        public async Task SetWindowCaptureNameAsync(string sourceName, string newSourceName)
        {
            if (!await IsSourceWindowCaptureAsync(sourceName)) return;

            try
            {
                await Task.Run(() => _obswebsocket.SetInputName(sourceName, newSourceName));
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (SetWindowCaptureNameAsync): {ex.Message}");
            }
        }

        // --- VISIBILITY METHODS ---

        /// <summary>
        /// Sets the visibility (enabled/disabled state) of a source item on a scene.
        /// This is equivalent to clicking the 'eye' icon in OBS.
        /// </summary>
        /// <param name="sceneName">The name of the scene containing the item.</param>
        /// <param name="sourceName">The name of the source/input.</param>
        /// <param name="isVisible">True to show (enable), False to hide (disable).</param>
        public async Task SetSourceVisibilityAsync(string sceneName, string sourceName, bool isVisible)
        {
            int sceneItemId = await GetSceneItemIdBySourceNameAsync(sceneName, sourceName);
            if (sceneItemId == -1) return;

            try
            {
                await Task.Run(() => _obswebsocket.SetSceneItemEnabled(sceneName, sceneItemId, isVisible));
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (SetSourceVisibilityAsync): {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to quickly show a source item on a scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene.</param>
        /// <param name="sourceName">The name of the source/input.</param>
        public Task ShowSourceAsync(string sceneName, string sourceName)
        {
            return SetSourceVisibilityAsync(sceneName, sourceName, true);
        }

        /// <summary>
        /// Hides ALL sources on the specified scene and then shows only the one with the matching sourceName, 
        /// provided it is a Window Capture type.
        /// Ideal for switching focus between capture windows.
        /// </summary>
        /// <param name="sceneName">The name of the scene.</param>
        /// <param name="sourceName">The name of the Window Capture source/input to show.</param>
        public async Task ShowOnlyWindowCaptureAsync(string sceneName, string sourceName)
        {
            // 1. Hide all elements on the scene first.
            await HideAllSourcesAsync(sceneName); // This method now has its own safety checks

            // 2. Find the unique Item ID for the specific Window Capture instance.
            // NOTE: This relies on GetWindowCaptureItemIdAsync which is filtered by type AND name.
            int sceneItemId = await GetWindowCaptureItemIdAsync(sceneName, sourceName);

            // 3. Show the target element if found.
            if (sceneItemId != -1)
            {
                try
                {
                    await Task.Run(() => _obswebsocket.SetSceneItemEnabled(sceneName, sceneItemId, true));
                }
                catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
                {
                    Debug.WriteLine($"OBS Error (ShowOnlyWindowCaptureAsync): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Creates a new Input (global source) in OBS and adds it as a scene item to the specified scene.
        /// This resolves the issue where a simple CreateSceneItem fails if the global input doesn't exist.
        /// </summary>
        /// <param name="sceneName">The scene where the item will be placed.</param>
        /// <param name="sourceName">The desired name for the new source.</param>
        /// <param name="inputKind">The type of source, e.g., "window_capture".</param>
        /// <returns>The Scene Item ID of the newly created item, or -1 if the item already exists on the scene.</returns>
        public async Task<int> CreateAndAddInputToSceneAsync(string sceneName, string sourceName, string inputKind)
        {
            // 1. Check if the scene item already exists on this specific scene.
            int existingItemId = await GetSceneItemIdBySourceNameAsync(sceneName, sourceName);
            if (existingItemId != -1) return -1;

            // 2. Try to create the global Input (Source) AND add it to the scene.
            try
            {
                // The 'CreateInput' method (in v5+) creates the input and adds it to the scene,
                // returning the 'int' SceneItemId directly.
                var sceneItemId = await Task.Run(() => _obswebsocket.CreateInput(
                  sceneName,
                  sourceName,
                  inputKind,
                  inputSettings: null,
                  sceneItemEnabled: true)
                );

                // If successful, the item is already added. Return the new ID.
                return sceneItemId;
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (CreateAndAddInputToSceneAsync - CreateInput): {ex.Message}");
                return -1; // Failed to create
            }
            catch (Exception ex) when (ex.Message.Contains("already exists"))
            {
                // This error means the *global* Input already exists.
                // We now just need to add that existing input to the scene.
                try
                {
                    return await Task.Run(() => _obswebsocket.CreateSceneItem(
                            sceneName: sceneName,
                            sourceName: sourceName,
                            sceneItemEnabled: true
                        ));
                }
                catch (Exception ex2) when (ex2 is NullReferenceException || ex2 is OperationCanceledException)
                {
                    Debug.WriteLine($"OBS Error (CreateAndAddInputToSceneAsync - CreateSceneItem): {ex2.Message}");
                    return -1; // Failed to add existing item
                }
            }
            catch
            {
                // Re-throw any other unexpected errors.
                throw;
            }
        }

        #endregion // 2. Window Capture Control

        // ------------------------------------------------------

        #region 3. Internal Getters and Helpers

        /// <summary>
        /// Retrieves a list of all scene items (sources) present on the specified scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene to query.</param>
        /// <returns>A list of details for all sources on the scene.</returns>
        public async Task<List<SceneItemDetails>> GetSourcesAsync(string sceneName)
        {
            try
            {
                var response = await Task.Run(()
                    => _obswebsocket.GetSceneItemList(sceneName));
                return response;
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (GetSourcesAsync): {ex.Message}");
                return new List<SceneItemDetails>(); // Return empty list on error
            }
        }

        /// <summary>
        /// Retrieves the unique numeric Item ID for a specific source item on a scene (regardless of its kind).
        /// This method uses the SourceName, so be cautious if multiple sources on the scene share the same name.
        /// </summary>
        /// <param name="sceneName">The name of the scene.</param>
        /// <param name="sourceName">The name of the source/input.</param>
        /// <returns>The Scene Item ID, or -1 if the item is not found.</returns>
        private async Task<int> GetSceneItemIdBySourceNameAsync(string sceneName, string sourceName)
        {
            // Retrieve the list of ALL scene items
            var list = await GetSourcesAsync(sceneName);

            // Find the first item whose SourceName matches the requested name
            var item = list.FirstOrDefault(i => i.SourceName.Equals(sourceName, StringComparison.OrdinalIgnoreCase));
            return item?.ItemId ?? -1;
        }

        /// <summary>
        /// Hides all scene items on the specified scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene to clear.</param>
        public async Task HideAllSourcesAsync(string sceneName)
        {
            // Retrieve the list of ALL scene items
            List<SceneItemDetails> allSources = await GetSourcesAsync(sceneName);

            // This method is now safe
            if (!allSources.Any()) return;

            // Create a list of asynchronous tasks to hide each item in parallel
            var hideTasks = allSources.Select(item => Task.Run(() =>
            {
                try
                {
                    _obswebsocket.SetSceneItemEnabled(sceneName, item.ItemId, false);
                }
                catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
                {
                    // Log error but continue trying to hide other items
                    Debug.WriteLine($"OBS Error (HideAllSourcesAsync - Item {item.SourceName}): {ex.Message}");
                }
            })).ToList();

            // Wait for all hiding operations to complete
            await Task.WhenAll(hideTasks);
        }

        /// <summary>
        /// Helper method to quickly hide a source item on a scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene.</param>
        /// <param name="sourceName">The name of the source/input.</param>
        public Task HideSourceAsync(string sceneName, string sourceName)
        {
            return SetSourceVisibilityAsync(sceneName, sourceName, false);
        }

        /// <summary>
        /// Checks if a named source is specifically a 'Window Capture' type.
        /// This method uses a cache for high performance.
        /// </summary>
        public async Task<bool> IsSourceWindowCaptureAsync(string sourceName)
        {
            // 1. Check the cache first
            if (_sourceKindCache.TryGetValue(sourceName, out string kind))
            {
                return kind.Equals(
                    ObsConstants.InputKind_WindowCapture,
                    StringComparison.OrdinalIgnoreCase
                );
            }

            // 2. If not in cache, fetch from OBS
            var inputSettings = await GetInputSettingsAsync(sourceName);
            if (inputSettings?.InputKind == null)
            {
                return false; // Source might not exist or settings failed
            }

            // 3. Store the result in the cache for next time
            _sourceKindCache.TryAdd(sourceName, inputSettings.InputKind);

            // 4. Return the result
            return inputSettings.InputKind.Equals(
                ObsConstants.InputKind_WindowCapture,
                StringComparison.OrdinalIgnoreCase
            );
        }

        /// <summary>
        /// Checks if a specific Window Capture source item already exists on a given scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene to check.</param>
        /// <param name="sourceName">The name of the Window Capture source.</param>
        /// <returns>True if the item exists on the scene; otherwise False.</returns>
        public async Task<bool> IsWindowCaptureExistAsync(string sceneName, string sourceName)
        {
            var sceneWindowCaptureList = await GetWindowCaptureListAsync(sceneName);
            return sceneWindowCaptureList.Any(s => s.SourceName.Equals(sourceName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Retrieves a filtered list of all Window Capture type items on a scene.
        /// This involves multiple asynchronous checks to filter the sources.
        /// </summary>
        /// <param name="sceneName">The name of the scene to check.</param>
        /// <returns>A list of SceneItemDetails for all Window Capture sources on the scene.</returns>
        public async Task<List<SceneItemDetails>> GetWindowCaptureListAsync(string sceneName)
        {
            // 1. Retrieve the list of all scene items.
            var sceneItemList = await GetSourcesAsync(sceneName);

            // 2. Create a collection of Tasks for parallel source type checking.
            var typeCheckTasks = sceneItemList.Select(async item =>
            {
                // Asynchronously check if the source is a WindowCapture.
                bool isWindowCapture = await IsSourceWindowCaptureAsync(item.SourceName);

                // Return an anonymous object containing the item and the check result.
                return new { Item = item, IsWindowCapture = isWindowCapture };

            }).ToList();

            // 3. Wait for ALL type checks to complete in parallel.
            var results = await Task.WhenAll(typeCheckTasks);

            // 4. Filter the results to include only WindowCapture sources.
            List<SceneItemDetails> windowCaptureList = results
                .Where(r => r.IsWindowCapture)
                .Select(r => r.Item)
                .ToList();

            return windowCaptureList;
        }

        /// <summary>
        /// Retrieves the unique numeric Item ID for a specific Window Capture source item on a scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene.</param>
        /// <param name="sourceName">The name of the source/input.</param>
        /// <returns>The Scene Item ID, or -1 if the item is not found.</returns>
        private async Task<int> GetWindowCaptureItemIdAsync(string sceneName, string sourceName)
        {
            var list = await GetWindowCaptureListAsync(sceneName);
            var item = list.FirstOrDefault(i => i.SourceName.Equals(sourceName, StringComparison.OrdinalIgnoreCase));

            return item?.ItemId ?? -1;
        }

        /// <summary>
        /// Retrieves the current visibility state (enabled/disabled) of a scene item.
        /// </summary>
        /// <param name="sceneName">The name of the scene containing the item.</param>
        /// <param name="sourceName">The name of the source/input.</param>
        /// <returns>True if the item is visible (enabled); False if hidden (disabled).</returns>
        public async Task<bool> IsSourceVisibleAsync(string sceneName, string sourceName)
        {
            // First, we need the unique Item ID for the source within the scene.
            int sceneItemId = await GetSceneItemIdBySourceNameAsync(sceneName, sourceName);

            if (sceneItemId == -1)
            {
                // Source not found on the scene
                return false;
            }

            try
            {
                return await Task.Run(() => _obswebsocket.GetSceneItemEnabled(sceneName, sceneItemId));
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (IsSourceVisibleAsync): {ex.Message}");
                return false; // Assume not visible if check fails
            }
        }

        #endregion // 3. Internal Getters and Helpers 

        // ------------------------------------------------------

        #region 4. Virtual Camera  

        /// <summary>
        /// Starts Virtual Camera in OBS.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async Task StartVirtualCamAsync()
        {
            try
            {
                await Task.Run(() => _obswebsocket.StartVirtualCam());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OBS error: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops Virtual Camera in OBS.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async Task StopVirtualCamAsync()
        {
            try
            {
                await Task.Run(() => _obswebsocket.StopVirtualCam());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OBS error: {ex.Message}");
            }
        }

        /// <summary>
        /// Toggles the Virtual Camera on or off (switch its state).
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async Task ToggleVirtualCamAsync()
        {
            try
            {
                await Task.Run(() => _obswebsocket.ToggleVirtualCam());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OBS error: {ex.Message}");
            }
        }

        /// <summary>
        /// Asynchronously checks the current status of the OBS Virtual Camera.
        /// </summary>
        /// <returns>True if the Virtual Cam is active (streaming), otherwise false.</returns>
        public async Task<bool> IsVirtualCamStartedAsync()
        {
            // Check connection status before sending any request to prevent timeouts on dead links.
            if (!_obswebsocket.IsConnected)
            {
                Debug.WriteLine("OBS Warning (IsVirtualCamStartedAsync): Not connected to OBS. Returning false.");
                return false;
            }

            try
            {
                // Wrap the synchronous OBS client call in Task.Run to execute it off the UI thread.
                // The OBS client (OBSWebsocketDotNet) uses synchronous methods for network I/O, 
                // which can block, so Task.Run is necessary.
                var status = await Task.Run(() => _obswebsocket.GetVirtualCamStatus());

                // Log successful status check
                Debug.WriteLine($"OBS Status: Virtual Cam IsActive = {status.IsActive}");

                return status.IsActive;
            }
            // Catch specific exceptions indicating timeout or request cancellation.
            catch (OperationCanceledException)
            {
                // This catches direct cancellation of the Task.Run.
                Debug.WriteLine("OBS Error (IsVirtualCamStartedAsync): Request was canceled (timeout).");
                
                return false;
            }
            catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
            {
                // This catches the TaskCanceledException wrapped inside an AggregateException, 
                // which is what happens when a Task.Run is awaited and the underlying operation fails due to cancellation/timeout.
                Debug.WriteLine($"OBS Error (IsVirtualCamStartedAsync): Task failed due to inner cancellation (timeout): {ex.InnerException.Message}");
                
                return false;
            }
            catch (Exception ex)
            {
                // Catch all other unexpected exceptions (like JSON errors, connection errors, etc.).
                Debug.WriteLine($"OBS Error (IsVirtualCamStartedAsync): Unexpected error during status check: {ex.Message}");
                
                return false;
            }
        }

        #endregion  4. Virtual Camera

        // ------------------------------------------------------

        #region 5. Settings

        // --- Core OBS Websocket Settings Methods ---

        /// <summary>
        /// Retrieves the current video settings from the OBS instance asynchronously.
        /// </summary>
        /// <returns>A Task that yields the current <see cref="ObsVideoSettings"/>.</returns>
        public async Task<ObsVideoSettings> GetVideoSettingsAsync()
        {
            try
            {
                return await Task.Run(() => _obswebsocket.GetVideoSettings());
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (GetVideoSettingsAsync): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets the video resolution and FPS settings in OBS using the OBS WebSockets client.
        /// </summary>
        /// <param name="obsVideoSettings">The settings to apply.
        /// If null, standard settings are used.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task SetVideoSettingsAsync(ObsVideoSettings obsVideoSettings = null)
        {
            // Fall back to default settings if none are provided.
            if (obsVideoSettings == null)
            {
                obsVideoSettings = ObsConstants.StandardSettings;
            }

            try
            {
                await Task.Run(() => _obswebsocket.SetVideoSettings(obsVideoSettings));
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (SetVideoSettingsAsync): {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the current video settings in OBS match
        /// the standard settings defined in ObsConstants.
        /// </summary>
        /// <returns>True if the settings already match, otherwise False.</returns>
        public async Task<bool> AreStandardVideoSettingsAppliedAsync()
        {
            try
            {
                // 1. Get the current settings from OBS
                var currentSettings = await GetVideoSettingsAsync(); // This method is now safe

                // 2. Get our desired standard settings
                var standardSettings = ObsConstants.StandardSettings;
                if (currentSettings == null) return false;

                // 3. Compare only the fields defined in our constant
                return currentSettings.BaseWidth == standardSettings.BaseWidth &&
                       currentSettings.BaseHeight == standardSettings.BaseHeight &&
                       currentSettings.OutputWidth == standardSettings.OutputWidth &&
                       currentSettings.OutputHeight == standardSettings.OutputHeight &&
                       currentSettings.FpsNumerator == standardSettings.FpsNumerator &&
                       currentSettings.FpsDenominator == standardSettings.FpsDenominator;
            }
            catch (Exception ex) // Catch potential NullRef if GetVideoSettingsAsync wasn't safe
            {
                // If we fail to get settings, it's safer to assume they do not match, to trigger a settings update.
                Debug.WriteLine($"OBS Error (AreStandardVideoSettingsAppliedAsync): {ex.Message}");
                
                return false;
            }
        }

        /// <summary>
        /// Stops the virtual camera if active, applies the specified (or standard default) video settings,
        /// and restores the camera state if it was running.
        /// This handles the OBS limitation
        /// where video settings cannot be changed while the Virtual Cam is active.
        /// </summary>
        /// <param name="obsVideoSettings">Optional settings to apply.
        /// If null, standard settings are used.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task<bool> ApplyStandardVideoSettingsAsync(ObsVideoSettings obsVideoSettings = null)
        {
            // If specific settings aren't provided (meaning we want the default)
            // AND the standard settings are ALREADY applied...
            if (obsVideoSettings == null && await AreStandardVideoSettingsAppliedAsync())
            {
                // Skip settings application.
                return false;
            }

            // 1. SAVE STATE: Check if the Virtual Camera is currently running.
            bool isVirtualCameraStarted = await IsVirtualCamStartedAsync();

            // 2. STOP (if necessary)
            if (isVirtualCameraStarted)
            {
                // Stop the Virtual Camera to unlock video settings.
                await StopVirtualCamAsync();
            }

            // 3. Delay to ensure OBS has fully processed the stop command.
            await Task.Delay(300);

            // 4. Apply the provided settings (or default if null).
            await SetVideoSettingsAsync(obsVideoSettings);

            // 5. Restart the Virtual Camera if it was running initially.
            if (isVirtualCameraStarted)
            {
                await StartVirtualCamAsync();
            }

            return true;
        }

        // --- Input Settings Methods ---

        /// <summary>
        /// Retrieves the settings for a specified input source in OBS.
        /// </summary>
        /// <param name="sourceName">The name of the source (Input) whose settings are to be retrieved.</param>
        /// <returns>The <see cref="InputSettings"/> object containing the generic settings dictionary.</returns>
        public async Task<InputSettings> GetInputSettingsAsync(string sourceName)
        {
            try
            {
                // OBSWebsocketDotNet is synchronous, so we wrap it in Task.Run for async flow.
                return await Task.Run(() => _obswebsocket.GetInputSettings(sourceName));
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (GetInputSettingsAsync): {ex.Message}");
                
                return null;
            }
        }

        /// <summary>
        /// Sets the settings of an input by providing the source name, a dictionary of settings, and overlay flag.
        /// THIS IS THE PRIMARY METHOD for setting custom configurations.
        /// </summary>
        /// <param name="sourceName">The name of the input to change.</param>
        /// <param name="settings">A dictionary of settings (Dictionary<string, object>) to apply.</param>
        /// <param name="overlay">True to apply the settings on top of existing ones, False to reset the input before applying.</param>
        public async Task SetInputSettingsAsync(string sourceName, Dictionary<string, object> settings, bool overlay = true)
        {
            // 1. Serialize the Dictionary<string, object> into a JSON string.
            var settingsJson = Newtonsoft.Json.JsonConvert.SerializeObject(settings);

            // 2. Deserialize the JSON string directly into a JObject.
            var jObjectSettings = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(settingsJson);

            try
            {
                // 3. Call the OBSWebsocket method, which now accepts the JObject.
                await Task.Run(() => _obswebsocket.SetInputSettings(sourceName, jObjectSettings, overlay));
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (SetInputSettingsAsync): {ex.Message}");
            }
        }

        // --- Window Capture Settings Helpers (The specific methods for our task) ---

        /// <summary>
        /// Asynchronously retrieves the specific settings for a Window Capture source,
        /// converting the generic settings dictionary received from OBS to our strongly typed 
        /// <see cref="WindowCaptureSettings"/> object.
        /// </summary>
        /// <param name="sourceName">The name of the Window Capture source.</param>
        /// <returns>The strongly typed <see cref="WindowCaptureSettings"/> object.</returns>
        public async Task<WindowCaptureSettings> GetWindowCaptureSettingsAsync(string sourceName)
        {
            // 1. Get the generic OBS settings (InputSettings)
            var obsSettings = await GetInputSettingsAsync(sourceName);

            // 2. Serialize the Dictionary<string, object> to a JSON string
            var settingsJson = Newtonsoft.Json.JsonConvert.SerializeObject(obsSettings.Settings);

            // 3. Deserialize the JSON string into our specific WindowCaptureSettings class
            return Newtonsoft.Json.JsonConvert.DeserializeObject<WindowCaptureSettings>(settingsJson);
        }

        /// <summary>
        /// Gets the raw 'window' identifier string from a Window Capture source.
        /// </summary>
        /// <param name="sourceName">The name of the Window Capture source.</param>
        /// <returns>The window identifier string (e.g., "Title:Class:Executable").</returns>
        public async Task<string> GetWindowCaptureTitleAsync(string sourceName)
        {
            WindowCaptureSettings settingsJson = await GetWindowCaptureSettingsAsync(sourceName);
            string WindowIdentifier = settingsJson.WindowIdentifier;

            return WindowIdentifier;
        }

        /// <summary>
        /// Asynchronously sets the settings for a Window Capture source.
        /// This converts our strongly typed <see cref="WindowCaptureSettings"/> object into the 
        /// generic dictionary required by OBS and then calls the primary SetInputSettings method.
        /// </summary>
        /// <param name="sourceName">The name of the Window Capture source.</param>
        /// <param name="settings">The <see cref="WindowCaptureSettings"/> object with the new parameters.</param>
        /// <param name="overlay">True to merge settings, False to reset before applying.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async Task SetWindowCaptureSettingsAsync(string sourceName, WindowCaptureSettings settings, bool overlay = true)
        {
            // 1. Serialize our WindowCaptureSettings object to a JSON string
            var settingsJson = Newtonsoft.Json.JsonConvert.SerializeObject(settings);

            // 2. Deserialize the JSON string back into the Dictionary<string, object>
            var settingsDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(settingsJson);

            // 3. Send the generic settings dictionary to OBS using the primary method
            await SetInputSettingsAsync(sourceName, settingsDictionary, overlay);
        }

        /// <summary>
        /// Sets the target window for a Window Capture source by changing only the "window" setting.
        /// This is a simplified helper for the most common operation.
        /// </summary>
        /// <param name="sourceName">The name of the Window Capture source.</param>
        /// <param name="windowHandleOrName">The new 'window' string (e.g., [window title]:[class name]:[executable]).</param>
        /// <param name="overlay">True to merge settings (default).</param>
        private Task SetWindowHandleAsync(string sourceName, string windowHandleOrName, bool overlay = true)
        {
            // We create a minimal dictionary containing ONLY the key we want to change ("window").
            var settingsUpdate = new Dictionary<string, object>
            {
                { "window", windowHandleOrName }
            };

            // We call the primary SetInputSettings method, using overlay=true to only update "window".
            return SetInputSettingsAsync(sourceName, settingsUpdate, overlay);
        }

        /// <summary>
        /// Helper overload: Sets the target window for a Window Capture source by providing three components,
        /// which are then formatted into the required OBS "window" string format: "Title:Class:Executable.exe".
        /// </summary>
        /// <param name="sourceName">The name of the Window Capture source.</param>
        /// <param name="windowTitle">The title of the window (e.g., "ferma2021_akk42 [Counter-Strike 2]").</param>
        /// <param name="windowClass">The window class (e.g., "SDL_app").</param>
        /// <param name="executableName">The executable name (e.g., "cs2.exe").</param>
        /// <param name="overlay">True to merge settings (default).</param>
        public Task SetWindowHandleAsync(string sourceName, string windowTitle, string windowClass, string executableName, bool overlay = true)
        {
            // 1. Format the full string in the "Title:Class:Executable" format.
            string formattedWindowString = $"{windowTitle}:{windowClass}:{executableName}";

            // 2. Call the base SetWindowHandleAsync method to apply the settings.
            return SetWindowHandleAsync(sourceName, formattedWindowString, overlay);
        }

        #endregion 5. Settings

        // ------------------------------------------------------

        #region 6. Scene Item Transform Control

        /// <summary>
        /// Asynchronously retrieves the transform information (position, size, rotation, cropping) 
        /// for a source item on the specified scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene containing the item.</param>
        /// <param name="sourceName">The name of the source.</param>
        /// <returns>The SceneItemTransformInfo object, or null if the source item is not found.</returns>
        public async Task<SceneItemTransformInfo> GetSourceTransformAsync(string sceneName, string sourceName)
        {
            // Get Scene Item ID
            int sceneItemId = await GetSceneItemIdBySourceNameAsync(sceneName, sourceName);
            if (sceneItemId == -1)
            {
                return null;
            }

            try
            {
                // OBS-Websocket's GetSceneItemTransform is synchronous, so we wrap it in Task.Run.
                return await Task.Run(() => _obswebsocket.GetSceneItemTransform(sceneName, sceneItemId));
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (GetSourceTransformAsync): {ex.Message}");
                
                return null;
            }
        }

        /// <summary>
        /// Asynchronously sets the transform information for a scene item (source) on a scene.
        /// This method is the core utility for applying ANY transform changes.
        /// </summary>
        /// <param name="sceneName">The name of the scene containing the item.</param>
        /// <param name="sourceName">The name of the source (Input).</param>
        /// <param name="transform">The new SceneItemTransformInfo object with the desired changes.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public async Task SetSourceTransformAsync(string sceneName, string sourceName, SceneItemTransformInfo transform)
        {
            // 1. Get the Item ID
            int sceneItemId = await GetSceneItemIdBySourceNameAsync(sceneName, sourceName);
            if (sceneItemId == -1)
            {
                // Don't throw, just log and exit
                Debug.WriteLine($"Source '{sourceName}' not found on scene '{sceneName}'. Cannot set transform.");
                
                return;
            }

            try
            {
                // 2. Set the new transform.
                await Task.Run(() => _obswebsocket.SetSceneItemTransform(sceneName, sceneItemId, transform));
            }
            catch (Exception ex) when (ex is NullReferenceException || ex is OperationCanceledException)
            {
                Debug.WriteLine($"OBS Error (SetSourceTransformAsync): {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the current transform of a source item matches
        /// the standard (DefaultFullWindowTransform).
        /// </summary>
        /// <param name="sceneName">The name of the scene.</param>
        /// <param name="sourceName">The name of the source item.</param>
        /// <returns>True if the transform already matches, otherwise False.</returns>
        private async Task<bool> IsDefaultTransformAppliedAsync(string sceneName, string sourceName)
        {
            try
            {
                // 1. Get the current transform
                var currentTransform = await GetSourceTransformAsync(sceneName, sourceName); // This method is now safe

                // 2. Get the desired default transform
                var defaultTransform = ObsConstants.DefaultFullWindowTransform;

                // 3. Define a small tolerance (epsilon) for comparing double values
                const double epsilon = 0.001;
                if (currentTransform == null) return false;

                // 4. Compare all fields defined in the constant.
                // Note the use of Math.Abs for 'double' comparison!
                // Also, the constant in the user's code has a typo 'Alignnment',
                // we must use the same typo for a correct comparison.
                return Math.Abs(currentTransform.X - defaultTransform.X) < epsilon &&
                       Math.Abs(currentTransform.Y - defaultTransform.Y) < epsilon &&
                       Math.Abs(currentTransform.Rotation - defaultTransform.Rotation) < epsilon &&
                       currentTransform.Alignnment == defaultTransform.Alignnment &&
                       currentTransform.BoundsType == defaultTransform.BoundsType &&
                       // BoundsWidth/Height might also be doubles, compare with tolerance
                       Math.Abs(currentTransform.BoundsWidth - defaultTransform.BoundsWidth) < epsilon &&
                       Math.Abs(currentTransform.BoundsHeight - defaultTransform.BoundsHeight) < epsilon &&
                       currentTransform.CropBottom == defaultTransform.CropBottom &&
                       currentTransform.CropLeft == defaultTransform.CropLeft &&
                       currentTransform.CropRight == defaultTransform.CropRight &&
                       currentTransform.CropTop == defaultTransform.CropTop;
            }
            catch (Exception ex) // Catch potential NullRef if GetSourceTransformAsync wasn't safe
            {
                // If the source isn't found or an error occurs, return false to trigger the transform update.
                Debug.WriteLine($"OBS Error (IsDefaultTransformAppliedAsync): {ex.Message}");
                
                return false;
            }
        }

        /// <summary>
        /// Asynchronously applies the predefined 'DefaultFullWindowTransform' constants, 
        /// effectively stretching the source to fit the entire defined working area (1280x720).
        /// </summary>
        /// <param name="sceneName">The name of the scene containing the item.</param>
        /// <param name="sourceName">The name of the source (Input) to be stretched.</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        public async Task<bool> StretchSourceToFullAreaAsync(string sceneName, string sourceName)
        {
            // Check if the default transform is ALREADY applied.
            if (await IsDefaultTransformAppliedAsync(sceneName, sourceName))
            {
                return false;
            }

            // 1. Get the default transform object (which is configured for stretching).
            SceneItemTransformInfo defaultTransform = ObsConstants.DefaultFullWindowTransform;

            // 2. Apply it using the core setter method.
            await SetSourceTransformAsync(sceneName, sourceName, defaultTransform);

            return true;
        }

        #endregion // 6. Scene Item Transform Control
    }
}