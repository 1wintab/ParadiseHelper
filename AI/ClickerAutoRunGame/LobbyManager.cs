using System;
using System.Threading;
using OpenCvSharp;
using ParadiseHelper.AI.Core;
using ParadiseHelper.AI.Control.Mouse;
using ParadiseHelper.AI.Extinctions;
using ParadiseHelper.Tools.WinAPI;
using ParadiseHelper.AI.Video.GameVideoSource;

namespace ParadiseHelper.AI.ClickerAutoRunGame
{
    /// <summary>
    /// Manages automated interactions with the game's lobby (menu) screens, 
    /// including initiating game searches, confirming ready checks, and handling disconnections.
    /// </summary>
    public class LobbyManager
    {
        // The shared state object containing the current status of the AI, game flags, and match values.
        private readonly AIState _state;

        // Detector responsible for template matching various UI elements (buttons, labels) in the lobby screen.
        private readonly SettingsDetector _settingsDetector;
        
        // Synchronization object used to ensure thread-safe access when updating shared state objects, like debug images.
        private readonly object _lockObject;
        
        // Mechanism for capturing the game window frame, used to check the current game state.
        private readonly MultiResolutionCapture _capture;

        /// <summary>
        /// Initializes a new instance of the <see cref="LobbyManager"/> class.
        /// </summary>
        /// <param name="state">The shared state object containing application and AI status.</param>
        /// <param name="lockObject">The synchronization object used for thread-safe UI updates (e.g., debug images).</param>
        /// <param name="capture">The video capture mechanism for retrieving the latest game frame.</param>
        public LobbyManager(AIState state, object lockObject, MultiResolutionCapture capture)
        {
            _state = state;
            _settingsDetector = new SettingsDetector();
            _lockObject = lockObject;
            _capture = capture;
        }

        /// <summary>
        /// Core method for running template matching detection on a frame and updating the corresponding state properties.
        /// </summary>
        /// <param name="currentFrame">The current video frame to analyze.</param>
        /// <param name="detector">The specific template matcher instance to use (e.g., HomeButton, StartButton).</param>
        /// <param name="setMatchValue">Action to update the match confidence score in the <see cref="AIState"/>.</param>
        /// <param name="getProcessedImage">Function to retrieve the previous processed debug image for disposal.</param>
        /// <param name="setProcessedImage">Action to set the newly processed debug image in the <see cref="AIState"/>.</param>
        /// <returns>True if the detection confidence meets the required threshold; otherwise, false.</returns>
        private bool ProcessDetection(
            Mat currentFrame,
            TemplateMatcherBinarization detector,
            Action<double> setMatchValue,
            Func<Mat> getProcessedImage,
            Action<Mat> setProcessedImage)
        {
            if (currentFrame == null)
            {
                return false;
            }

            // Run the template matching algorithm
            var result = detector.DetectTarget(currentFrame);

            // Use the lock object to ensure thread safety when updating shared state objects (AIState)
            lock (_lockObject)
            {
                setMatchValue(result.MatchValue);

                // Get the old image via the delegate and dispose of it to prevent memory leaks
                getProcessedImage()?.Dispose();

                // Check debug flag and if a processed image was generated
                if (_state.ShowLobbyButtonDebugImage && result.ProcessedImage != null)
                {
                    // Clone the image and set the new image via delegate
                    setProcessedImage(result.ProcessedImage.Clone());
                }
                else
                {
                    setProcessedImage(null);
                }
            }

            // Dispose of the original result image, as a clone (if needed) was already saved to state
            result.ProcessedImage?.Dispose();

            // Return true if the match confidence is high enough
            return result.MatchValue >= detector.MatchThreshold;
        }

        /// <summary>
        /// Checks if the Home/Main Lobby button is visible, indicating the user is on the main menu.
        /// </summary>
        /// <param name="currentFrame">The current video frame.</param>
        /// <returns>True if the Home button is detected.</returns>
        public bool IsInLobby(Mat currentFrame) =>
            ProcessDetection(currentFrame, _settingsDetector.HomeButton,
                v => _state.LatestLobbyMatchValue = v,
                () => _state.LatestProcessedLobbyImage,
                img => _state.LatestProcessedLobbyImage = img);

        /// <summary>
        /// Checks for the presence of the Line Mode Map selector, typically present when selecting a game.
        /// </summary>
        /// <param name="currentFrame">The current video frame.</param>
        /// <returns>True if the Line Mode Map selector is detected.</returns>
        public bool IsExistsLineModeMap(Mat currentFrame) =>
            ProcessDetection(currentFrame, _settingsDetector.LineModeMap,
                v => _state.LatestLineModeMapMatchValue = v,
                () => _state.LatestProcessedLineModeMapImage,
                img => _state.LatestProcessedLineModeMapImage = img);

        /// <summary>
        /// Checks for the presence of the 'Start' button, indicating a game search can be initiated.
        /// </summary>
        /// <param name="currentFrame">The current video frame.</param>
        /// <returns>True if the Start button is detected.</returns>
        public bool IsExistsStartButton(Mat currentFrame) =>
            ProcessDetection(currentFrame, _settingsDetector.StartButton,
                v => _state.LatestStartButtonMatchValue = v,
                () => _state.LatestProcessedStartButtonImage,
                img => _state.LatestProcessedStartButtonImage = img);

        /// <summary>
        /// Checks for the presence of the 'Cancel' button, indicating a game search is currently in progress.
        /// </summary>
        /// <param name="currentFrame">The current video frame.</param>
        /// <returns>True if the Cancel button is detected.</returns>
        public bool IsExistsCancelButton(Mat currentFrame) =>
            ProcessDetection(currentFrame, _settingsDetector.CancelButton,
                v => _state.LatestCancelButtonMatchValue = v,
                () => _state.LatestProcessedCancelButtonImage,
                img => _state.LatestProcessedCancelButtonImage = img);

        /// <summary>
        /// Checks for the 'Disconnection' label or message, indicating the game needs to be restarted or reconnected.
        /// </summary>
        /// <param name="currentFrame">The current video frame.</param>
        /// <returns>True if the Disconnection label is detected.</returns>
        public bool IsExistsDisconnectionLabel(Mat currentFrame) =>
            ProcessDetection(currentFrame, _settingsDetector.DisconnectionLabel,
                v => _state.LatestDisconnectionLabelMatchValue = v,
                () => _state.LatestProcessedDisconnectionLabelImage,
                img => _state.LatestProcessedDisconnectionLabelImage = img);

        /// <summary>
        /// Checks for the 'Match is Ready' confirmation prompt.
        /// </summary>
        /// <param name="currentFrame">The current video frame.</param>
        /// <returns>True if the Match Ready prompt is detected.</returns>
        public bool IsMatchIsReady(Mat currentFrame) =>
            ProcessDetection(currentFrame, _settingsDetector.MatchIsReadyLabel,
                v => _state.LatestMatchIsReadyLabelMatchValue = v,
                () => _state.LatestProcessedMatchIsReadyLabelImage,
                img => _state.LatestProcessedMatchIsReadyLabelImage = img);

        /// <summary>
        /// Checks for the Team Selection screen (e.g., Terrorist/Counter-Terrorist selection).
        /// </summary>
        /// <param name="currentFrame">The current video frame.</param>
        /// <returns>True if the Team Selection screen is detected.</returns>
        public bool IsTeamSelectionTT(Mat currentFrame) =>
            ProcessDetection(currentFrame, _settingsDetector.TeamSelectionTT,
                v => _state.LatestTeamSelectionTTMatchValue = v,
                () => _state.LatestProcessedTeamSelectionTTImage,
                img => _state.LatestProcessedTeamSelectionTTImage = img);

        /// <summary>
        /// Orchestrates the process of checking the game state and performing necessary actions (clicking, starting search, handling disconnects).
        /// </summary>
        /// <param name="currentFrame">The current video frame.</param>
        public void ProcessLobbyStateAndActions(Mat currentFrame)
        {
            // Exit early if the game window is not active/focused or the frame is missing.
            if (!WindowController.IsGameWindowActiveAndFocused() || currentFrame == null) return;

            // --- PRIORITY STATE CHECKS ---

            // Priority #1: Are we already loaded into the map (Team Selection Screen)?
            if (IsTeamSelectionTT(currentFrame))
            {
                // Simply click whenever this screen is seen until we move past it
                var gameWindowHandle = WindowController.FindWindowByTitleSubstring("Counter-Strike 2");

                // Assuming a generic click location to dismiss the screen or select a team.
                MouseClicker.ClickRelativeToWindow(gameWindowHandle, new System.Drawing.Point(10, 10));

                Thread.Sleep(500); // Give the game time to react

                // Reset states so that after the game ends, a new search can begin, preventing a loop
                _state.IsSearchingGame = false;
                _state.IsMatchIsReady = false;

                return;
            }

            // Priority #2: Has a match been found and is awaiting confirmation?
            if (IsMatchIsReady(currentFrame))
            {
                _state.IsSearchingGame = false;
                _state.IsMatchIsReady = true; // Set the flag

                // NOTE: The click action to accept the match ready prompt is missing here, 
                // but the state is correctly set for external logic to handle it.

                return;
            }

            // Priority #3: Have we been disconnected or failed to connect?
            if (IsExistsDisconnectionLabel(currentFrame))
            {
                var gameWindowHandle = WindowController.FindWindowByTitleSubstring("Counter-Strike 2");

                // Click on the assumed location of the 'OK' or 'Reconnect' button on the disconnection prompt (775, 405)
                MouseClicker.ClickRelativeToWindow(gameWindowHandle, new System.Drawing.Point(775, 405));

                Thread.Sleep(500);

                _state.IsSearchingGame = false;
                _state.IsMatchIsReady = false; // Reset all states

                return;
            }

            // Check if we are in the main lobby. If not, exit and avoid unnecessary checks.
            if (!IsInLobby(currentFrame))
            {
                _state.IsInLobby = false;
                _state.IsSearchingGame = false;

                return;
            }

            // If the code reached this point, we are DEFINITELY in the lobby.
            _state.IsInLobby = true;

            // --- GAME SEARCH LOGIC ---

            // If we are waiting for the match to load, but not yet on the map, do nothing
            if (_state.IsMatchIsReady) return;

            // Check current status using detection methods
            bool isSearching = IsExistsCancelButton(currentFrame);
            bool isInLobby = IsInLobby(currentFrame);

            _state.IsSearchingGame = isSearching;

            // If we are already in the process of searching, do nothing
            if (isSearching) return;

            // If we are in the lobby and not searching for a game, start a new search
            if (isInLobby) StartNewGameSearch();
        }

        /// <summary>
        /// Executes the sequence of clicks required to navigate the game menu and start a new Deathmatch game search.
        /// </summary>
        private void StartNewGameSearch()
        {
            var gameWindowHandle = WindowController.FindWindowByTitleSubstring("Counter-Strike 2");
            
            if (gameWindowHandle == IntPtr.Zero) return;

            // State is updated to 'true' only AFTER the final click, to avoid race conditions.
            _state.IsSearchingGame = false;

            // Sequence of clicks to select mode and start (using hardcoded relative coordinates)

            // 1. Click on the 'Home' icon/picture to ensure we are on the main screen
            MouseClicker.ClickRelativeToWindow(gameWindowHandle, new System.Drawing.Point(16, 22));
            Thread.Sleep(500);

            // 2. Click on the 'PLAY' button/text
            MouseClicker.ClickRelativeToWindow(gameWindowHandle, new System.Drawing.Point(660, 22));
            Thread.Sleep(500);

            // 3. Click on the 'MATCHMAKING' tab
            MouseClicker.ClickRelativeToWindow(gameWindowHandle, new System.Drawing.Point(550, 58));
            Thread.Sleep(500);

            // 4. Click on the specific game mode, e.g., 'DEATHMATCH'
            MouseClicker.ClickRelativeToWindow(gameWindowHandle, new System.Drawing.Point(630, 90));
            Thread.Sleep(500);

            // 5. Click on the Map Selection/Defusal Group Alpha area (DUST, MIRAGE, INFERNO, VERTIGO)
            MouseClicker.ClickRelativeToWindow(gameWindowHandle, new System.Drawing.Point(360, 400));
            Thread.Sleep(500);

            // After the clicks, take a fresh screenshot to check the resulting state.
            using (var frameAfterClicks = _capture.FrameDistributor.GetLatestFrame())
            {
                // Check 1: Did the clicks result in an active search? (Cancel button is visible)
                if (frameAfterClicks != null && IsExistsCancelButton(frameAfterClicks))
                {
                    Console.WriteLine("Search is already in progress. Skipping final Start button click.");

                    _state.IsSearchingGame = true; // Correct the state

                    return; // Exit as the search is now running
                }

                // Check 2: If no search is running, check if the Start button is visible (meaning we are ready to launch).
                // Note: The multiple IsExistsStartButton checks are redundant, optimizing this is recommended later.
                if (frameAfterClicks != null && IsExistsStartButton(frameAfterClicks)
                    && IsExistsLineModeMap(frameAfterClicks))
                {
                    // 6. Final click on the 'Start' button (1130, 690)
                    MouseClicker.ClickRelativeToWindow(gameWindowHandle, new System.Drawing.Point(1130, 690));
                    _state.IsSearchingGame = true;

                    // Wait longer to allow the game to transition to the 'searching' state reliably.
                    Thread.Sleep(2000);
                }
            }
        }
    }
}