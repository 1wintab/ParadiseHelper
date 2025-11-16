using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using ParadiseHelper.AI.Weapon;
using ParadiseHelper.AI.Movement;
using ParadiseHelper.AI.Orientation.Radar;
using ParadiseHelper.AI.ClickerAutoRunGame;
using ParadiseHelper.AI.Core.DetectionLoop;
using ParadiseHelper.AI.Core.Settings.Weapon;
using ParadiseHelper.AI.Core.DetectionLoop.Handlers;
using ParadiseHelper.AI.Control.KeyBoard.Binds.AutoBuy;
using ParadiseHelper.AI.Control.KeyBoard.Binds.AutoDisconnect;
using ParadiseHelper.AI.Control.KeyBoard.Binds.AutoSkipWeapon;
using Core;
using ParadiseHelper.AI.Video.Detect;
using ParadiseHelper.AI.Video.GameVideoSource;

namespace ParadiseHelper.AI.Core
{
    /// <summary>
    /// The main class orchestrating all AI subsystems (vision, aiming, movement, state management).
    /// It manages the lifecycle, initialization, start, stop, and disposal of all core components and handlers.
    /// </summary>
    public class AICore : IDisposable
    {
        // Synchronization object used for thread-safe access to shared state variables.
        private readonly object _lockObject = new object();
        
        // Cancellation token source to signal termination to all background handler tasks.
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        // --- Core Components ---

        // Shared state object holding global AI variables (e.g., detected location, target).
        private readonly AIState _state;

        // State object tracking the player's life, combat, and in-game status.
        private readonly PlayerState _playerState;
        
        // Manager responsible for capturing raw video frames from the game source.
        private readonly MultiResolutionCapture _capture;

        // Component to preprocess captured frames on the GPU for the detection model.
        private readonly GpuPreprocessor _gpuPreprocessor;

        // The deep learning model handler (e.g., YOLO) responsible for object detection.
        private readonly YoloDetector _detector;

        // A queue used to pass detection results to the AimingHandler for processing, ensuring thread safety.
        private readonly BlockingCollection<Tuple<YoloResult, System.Drawing.Size, System.Drawing.Point>> _aimActionQueue;

        // --- Managers ---

        // Manager responsible for pathfinding, tactical decisions, and issuing movement commands.
        private readonly NavigationManager _navigationManager;
        
        // Manager responsible for weapon configuration and control logic.
        private readonly WeaponManager _weaponManager;
        
        // Manager responsible for processing in-game radar and minimap data.
        private readonly RadarManager _radarManager;
        
        // Manager handling game lobby and pre-game screen detection logic.
        private readonly LobbyManager _lobbyManager;

        // --- Handlers ---
        // A list containing all background threads/loops (derived from DetectionLoopBase) that are executed concurrently.
        private readonly List<DetectionLoopBase> _handlers = new List<DetectionLoopBase>();

        // --- Facade for UI ---
        /// <summary>
        /// Gets the facade object used by the User Interface (UI) to safely access and display 
        /// the current status and internal state of the AI core.
        /// </summary>
        public AIStateUIFacade StateFacade { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AICore"/> class, setting up all core components,
        /// managers, and detection handlers.
        /// </summary>
        public AICore()
        {
            // --- Core Component Initialization ---
            _state = new AIState();
            _playerState = new PlayerState();
            _capture = new MultiResolutionCapture();
            _gpuPreprocessor = new GpuPreprocessor();
            _detector = new YoloDetector(_gpuPreprocessor);
            _aimActionQueue = new BlockingCollection<Tuple<YoloResult, System.Drawing.Size, System.Drawing.Point>>();

            // --- Manager Initialization ---
            _navigationManager = new NavigationManager(_lockObject);
            _weaponManager = new WeaponManager();
            _radarManager = new RadarManager();
            _lobbyManager = new LobbyManager(_state, _lockObject, _capture);

            // --- UI Facade Creation ---
            StateFacade = new AIStateUIFacade(_state, _navigationManager, _radarManager, _playerState, _lockObject);

            // --- Handler Creation and Initialization  ---
            var autoBuyWeapons = new AutoBuyWeapons();
            var autoDisconnect = new AutoDisconnect();
            var autoSkipWeapon = new AutoSkipWeapon();

            // Add all continuous loop handlers to the list for unified management
            _handlers.Add(new HotkeyHandler(_state));
            _handlers.Add(new YoloDetectionHandler(_capture, _detector, _aimActionQueue, _state, _playerState, _lockObject));
            _handlers.Add(new WeaponDetectionHandler(_capture, _weaponManager, _state, autoSkipWeapon, _lockObject));
            _handlers.Add(new RadarDetectionHandler(_capture, _radarManager, _navigationManager, _state, _playerState, _lockObject));
            _handlers.Add(new MapDetectionHandler(_capture, _playerState, _state, autoDisconnect, _lockObject));
            _handlers.Add(new LobbyDetectionHandler(_capture, _lobbyManager));
            _handlers.Add(new AimingHandler(_aimActionQueue, _navigationManager, _state));
            _handlers.Add(new AutoBuyHandler(_state, _playerState, autoBuyWeapons));
            _handlers.Add(new PlayerStateManagementHandler(_playerState, _state));

            // Load specific weapon settings from the configuration file
            LoadWeaponSettings();
        }

        /// <summary>
        /// Loads the weapon configuration settings from a JSON file.
        /// </summary>
        private void LoadWeaponSettings()
        {
            string settingsPath = Path.Combine(FilePaths.AI.Settings.WeaponDirectory, "weapon_settings.json");

            // Assuming WeaponSettingsManager.LoadSettings handles deserialization and application of settings.
            var settings = WeaponSettingsManager.LoadSettings(settingsPath);
        }

        /// <summary>
        /// Starts all detection and processing handlers, initiating all background worker tasks.
        /// </summary>
        public void Start()
        {
            // Start each continuous loop handler using the shared cancellation token.
            foreach (var handler in _handlers)
            {
                handler.Start(_cts.Token);
            }
        }

        /// <summary>
        /// Stops the entire AI core by requesting cancellation, waiting for all background tasks 
        /// to terminate, and initiating the disposal of resources.
        /// </summary>
        public void Stop()
        {
            // 1. Send cancellation signal to all running tasks/threads
            _cts.Cancel();

            // 2. Collect tasks to wait for their termination
            var stopTasks = _handlers.Select(h => h.GetStopTask()).Where(t => t != null).ToArray();

            try
            {
                // 3. Wait for ALL tasks to finish, with a timeout (5 seconds).
                Task.WaitAll(stopTasks, 5000);
            }
            catch (AggregateException ex)
            {
                // Check if all inner exceptions are the expected TaskCanceledException
                if (!ex.InnerExceptions.All(e => e is TaskCanceledException))
                {
                    // If there are unexpected errors, re-throw them
                    throw;
                }
                // else: Expected cancellation, so we ignore the exception.
            }
            catch (Exception)
            {
                // General error handling during shutdown.
            }
            finally
            {
                // 4. Critical cleanup actions that must execute regardless of exceptions
                _navigationManager.StopMovement();
                _aimActionQueue.CompleteAdding();

                // Release all unmanaged resources. This is key to prevent AccessViolation on restart.
                Dispose(true);
            }
        }

        /// <summary>
        /// Public implementation of the IDisposable pattern. 
        /// Ensures all managed and unmanaged resources are correctly released.
        /// </summary>
        public void Dispose()
        {
            // Calls the main disposal logic
            Dispose(true);

            // Suppress finalization to prevent the garbage collector from running the finalizer
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Centralized resource disposal logic (standard IDisposable pattern).
        /// </summary>
        /// <param name="disposing">True if called from the Dispose method; false if called from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose of managed resources that hold unmanaged resources (like image frames or detection models)
                // Null-coalescing operator '?' ensures we don't call Dispose on a null object.
                _aimActionQueue?.Dispose();

                _detector?.Dispose();
                _gpuPreprocessor?.Dispose();
                _capture?.Dispose();

                _cts?.Dispose();
            }
        }
    }
}