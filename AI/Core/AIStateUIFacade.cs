using System;
using System.Drawing;
using System.Collections.Generic;
using OpenCvSharp;
using ParadiseHelper.AI.Movement;
using ParadiseHelper.AI.Orientation.Map.Navmesh;
using ParadiseHelper.AI.Orientation.Radar;
using ParadiseHelper.AI.Video.Detect;

namespace ParadiseHelper.AI.Core
{
    /// <summary>
    /// Provides a simplified and thread-safe interface for the User Interface (UI) 
    /// to access and display the current state and real-time data of the AI core.
    /// This pattern hides complex internal logic and ensures data is accessed safely.
    /// </summary>
    public class AIStateUIFacade
    {
        // Global AI state container with core variables.
        private readonly AIState _state;

        // Player-specific state (e.g., health, combat readiness).
        private readonly PlayerState _playerState;

        // Manager handling pathfinding and movement decisions.
        private readonly NavigationManager _navigationManager;

        // Manager for in-game radar processing and data.
        private readonly RadarManager _radarManager;

        // Synchronization object for thread-safe access.
        private readonly object _lockObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="AIStateUIFacade"/> class.
        /// </summary>
        /// <param name="state">The global AI state container.</param>
        /// <param name="navigationManager">The manager responsible for pathfinding and movement.</param>
        /// <param name="radarManager">The manager responsible for radar detection and processing.</param>
        /// <param name="playerState">The state object tracking the player's life and combat readiness.</param>
        /// <param name="lockObject">The synchronization object used for all thread-safe data access.</param>
        public AIStateUIFacade(AIState state, NavigationManager navigationManager, RadarManager radarManager, PlayerState playerState, object lockObject)
        {
            _state = state;
            _navigationManager = navigationManager;
            _radarManager = radarManager;
            _playerState = playerState;
            _lockObject = lockObject;
        }

        // --- Core Data Access (Thread Safe) ---

        /// <summary>
        /// Gets the latest captured game frame for display in the UI (returns a clone).
        /// </summary>
        /// <returns>A thread-safe <see cref="Bitmap"/> copy, or null if no frame is available.</returns>
        public Bitmap GetLatestFrameForUI() { lock (_lockObject) { return _state.LatestFrame?.Clone() as Bitmap; } }

        /// <summary>
        /// Gets the latest list of detected objects from the YOLO model.
        /// </summary>
        /// <returns>The <see cref="List{T}"/> of <see cref="YoloResult"/>.</returns>
        public List<YoloResult> GetLatestResultsForUI() { lock (_lockObject) { return _state.LatestResults; } }

        /// <summary>
        /// Gets the latest processed weapon image for debug display (returns a clone), 
        /// or null if debugging is disabled.
        /// </summary>
        public Mat GetLatestProcessedWeaponImageForUI()
        {
            if (!_state.ShowWeaponDebugImage) return null;
            lock (_lockObject) { return _state.LatestProcessedWeaponImage?.Clone(); }
        }

        /// <summary>
        /// Gets the latest processed radar image for debug display (returns a clone), 
        /// or null if debugging is disabled.
        /// </summary>
        public Mat GetLatestProcessedRadarImageForUI()
        {
            if (!_state.ShowRadarDebugImage) return null;
            lock (_lockObject) { return _state.LatestProcessedRadarImage?.Clone(); }
        }

        /// <summary>
        /// Gets the player's detected location coordinates on the radar.
        /// </summary>
        public OpenCvSharp.Point? GetPlayerLocationOnRadar() { lock (_lockObject) { return _state.PlayerLocationOnRadar; } }

        /// <summary>
        /// Gets the player's detected direction vector on the radar.
        /// </summary>
        public OpenCvSharp.Point? GetPlayerDirectionOnRadar() { lock (_lockObject) { return _state.PlayerDirection; } }

        /// <summary>
        /// Gets the bounding box of the detected map region.
        /// </summary>
        public Rect? GetLatestMapBoundingBox() { lock (_lockObject) { return _state.LatestMapBoundingBox; } }

        /// <summary>
        /// Gets the match value confidence for player detection on the radar (lower is better).
        /// </summary>
        public double GetLatestRadarMatchValue() { lock (_lockObject) { return _state.LatestRadarMatchValue; } }

        /// <summary>
        /// Gets the name of the currently detected map. Returns "Unknown" if the match value is too high.
        /// </summary>
        public string GetLatestMapName() { lock (_lockObject) { return _state.LatestMapMatchValue <= AIState.MapMatchThreshold ? _state.LatestMapName : "Unknown"; } }

        /// <summary>
        /// Gets the match value confidence for the map template matching.
        /// </summary>
        public double GetLatestMapMatchValue() { lock (_lockObject) { return _state.LatestMapMatchValue; } }

        /// <summary>
        /// Gets the name of the currently equipped weapon.
        /// </summary>
        public string GetLatestWeaponName() { lock (_lockObject) { return _state.LatestWeaponName; } }

        /// <summary>
        /// Gets the match value confidence for the weapon template matching.
        /// </summary>
        public double GetLatestWeaponMatchValue() { lock (_lockObject) { return _state.LatestWeaponMatchValue; } }

        // --- Navigation Data Access (Thread Safe) ---

        /// <summary>
        /// Gets the current navigation mesh being used for pathfinding.
        /// </summary>
        public Navmesh GetCurrentNavmeshForUI() { lock (_lockObject) { return _navigationManager.CurrentNavmesh; } }

        /// <summary>
        /// Gets the nearest navigation node to the player's current position.
        /// </summary>
        public Node GetNearestNavmeshNodeForUI() { lock (_lockObject) { return _navigationManager.NearestNavmeshNode; } }

        /// <summary>
        /// Gets the last navigation node the player was at.
        /// </summary>
        public Node GetLastNodeForUI() { lock (_lockObject) { return _navigationManager.LastNode; } }

        /// <summary>
        /// Gets the current navigation node the player is aiming for.
        /// </summary>
        public Node GetCurrentNodeForUI() { lock (_lockObject) { return _navigationManager.CurrentNode; } }

        /// <summary>
        /// Gets the next navigation node in the current path.
        /// </summary>
        public Node GetNextNodeForUI() { lock (_lockObject) { return _navigationManager.NextNode; } }

        /// <summary>
        /// Gets a thread-safe copy of the navigation path history.
        /// </summary>
        public List<Node> GetPathHistoryForUI() { lock (_lockObject) { return new List<Node>(_navigationManager.PathHistory); } }

        // --- Lobby Data Access (Thread Safe) ---

        /// <summary>
        /// Gets the latest processed lobby screen image for debug display (returns a clone).
        /// </summary>
        public Mat GetLatestProcessedLobbyImage() { lock (_lockObject) { return _state.LatestProcessedLobbyImage?.Clone(); } }

        /// <summary>
        /// Gets the latest processed image showing the map in line mode (returns a clone).
        /// </summary>
        public Mat GetLatestProcessedLineModeMapImage() { lock (_lockObject) { return _state.LatestProcessedLineModeMapImage?.Clone(); } }

        /// <summary>
        /// Gets the latest processed image for the disconnection label (returns a clone).
        /// </summary>
        public Mat GetLatestProcessedDisconnectionLabelImage() { lock (_lockObject) { return _state.LatestProcessedDisconnectionLabelImage?.Clone(); } }

        /// <summary>
        /// Gets the latest processed image for the "Match is Ready" label (returns a clone).
        /// </summary>
        public Mat GetLatestProcessedMatchIsReadyLabelImage() { lock (_lockObject) { return _state.LatestProcessedMatchIsReadyLabelImage?.Clone(); } }

        /// <summary>
        /// Gets the latest processed image for the team selection text (returns a clone).
        /// </summary>
        public Mat GetLatestProcessedTeamSelectionTTImage() { lock (_lockObject) { return _state.LatestProcessedTeamSelectionTTImage?.Clone(); } }

        /// <summary>
        /// Gets the latest detected "Start" or "Cancel" button image for display (returns a clone).
        /// </summary>
        public Mat GetLatestProcessedActionButtonImage()
        {
            lock (_lockObject)
            {
                if (_state.LatestProcessedCancelButtonImage != null)
                {
                    return _state.LatestProcessedCancelButtonImage.Clone();
                }
                if (_state.LatestProcessedStartButtonImage != null)
                {
                    return _state.LatestProcessedStartButtonImage.Clone();
                }
                return null;
            }
        }

        // --- Derived & Utility Methods ---

        /// <summary>
        /// Toggles the visibility of the detailed debug information panel in the UI.
        /// </summary>
        public void ToggleFullInfo() { _state.ShowFullInfo = !_state.ShowFullInfo; }

        /// <summary>
        /// Checks if the detailed debug information panel is currently visible.
        /// </summary>
        public bool IsFullInfoVisible() { return _state.ShowFullInfo; }

        /// <summary>
        /// Checks if the lobby button debug image is currently set to be shown.
        /// </summary>
        public bool GetShowLobbyButtonDebugImage() { return _state.ShowLobbyButtonDebugImage; }

        /// <summary>
        /// Checks the current state of the lobby debug image visibility flag (thread-safe).
        /// </summary>
        public bool IsLobbyDebugOn() { lock (_lockObject) { return _state.ShowLobbyButtonDebugImage; } }

        /// <summary>
        /// Indicates if the hotkey for toggling lobby handling was recently pressed.
        /// </summary>
        public bool WasToggleLobbyKeyPressed { get { return _state.wasToggleLobbyKeyPressed; } }

        /// <summary>
        /// Determines if the player's icon has been reliably found on the radar.
        /// </summary>
        public bool IsPlayerFoundOnRadar() { lock (_lockObject) { return _state.LatestRadarMatchValue >= 0.0 && _state.LatestRadarMatchValue <= 0.47; } }

        /// <summary>
        /// Checks if the player's detected location is within the bounds of the detected map image, 
        /// including a margin based on the radar template size.
        /// </summary>
        public bool IsPlayerOnMapWithBoxMargin()
        {
            var playerLocation = GetPlayerLocationOnRadar();
            var mapBoundingBox = GetLatestMapBoundingBox();

            if (!playerLocation.HasValue || !mapBoundingBox.HasValue)
            {
                return false;
            }

            // Note: Requires access to RadarCapture (assuming it's a static class or accessible utility).
            // These margins ensure the player icon is fully contained within the relevant map area.
            int templateWidth = RadarCapture.GetTemplateWidth();
            int templateHeight = RadarCapture.GetTemplateHeight();

            int leftMargin = templateWidth;
            int topMargin = templateHeight;
            int rightMargin = 0;
            int bottomMargin = 0;

            bool isInLeftBound = playerLocation.Value.X >= mapBoundingBox.Value.X - leftMargin;
            bool isInTopBound = playerLocation.Value.Y >= mapBoundingBox.Value.Y - topMargin;
            bool isInRightBound = playerLocation.Value.X <= mapBoundingBox.Value.X + mapBoundingBox.Value.Width + rightMargin;
            bool isInBottomBound = playerLocation.Value.Y <= mapBoundingBox.Value.Y + mapBoundingBox.Value.Height + bottomMargin;

            return isInLeftBound && isInTopBound && isInRightBound && isInBottomBound;
        }

        /// <summary>
        /// Calculates the player's current direction in degrees (0-360) based on the radar direction vector.
        /// </summary>
        /// <returns>The direction in degrees, or -1 if the direction is unknown.</returns>
        public double GetPlayerDirectionInDegrees()
        {
            lock (_lockObject)
            {
                if (_state.PlayerDirection.HasValue)
                {
                    // Calculate angle using atan2(x, -y) to convert screen coordinates (Y-down) to standard compass degrees.
                    double angleInDegrees = Math.Atan2(_state.PlayerDirection.Value.X, -_state.PlayerDirection.Value.Y) * 180 / Math.PI;
                    
                    if (angleInDegrees < 0)
                    {
                        angleInDegrees += 360;
                    }
                    
                    return angleInDegrees;
                }
                return -1;
            }
        }

        /// <summary>
        /// Gets the location of a detected "death" icon on the radar, indicating a player died nearby.
        /// </summary>
        public OpenCvSharp.Point? GetDeadPlayerLocationOnRadar()
        {
            lock (_lockObject)
            {
                if (_radarManager.IsDeathDetected())
                {
                    return _radarManager.DeadPlayerLocationOnRadar;
                }
                return null;
            }
        }

        // --- UI Text Formatting Methods ---

        /// <summary>
        /// Formats the current detection FPS value as a string for display.
        /// </summary>
        public string GetFpsText() { return $"Detection FPS: {_state.DetectionFps:F0}"; }

        /// <summary>
        /// Formats the player's status on the radar (Found/Not Found) as a string.
        /// </summary>
        public string GetPlayerStatusText()
        {
            bool isPlayerFound = IsPlayerOnMapWithBoxMargin();
            bool isMapDetected = _state.LatestMapMatchValue < AIState.MapMatchThreshold && _state.LatestMapName != "Unknown";
            
            return $"Player Status on Radar: {(isPlayerFound && isMapDetected ? "Yes" : "No")}";
        }

        /// <summary>
        /// Formats the player's relative location on the map as a coordinate string.
        /// </summary>
        public string GetPlayerLocationText()
        {
            var playerLocation = GetPlayerLocationOnRadar();
            var mapBoundingBox = GetLatestMapBoundingBox();

            if (playerLocation.HasValue && mapBoundingBox.HasValue)
            {
                // Calculate position relative to the top-left corner of the map bounding box
                int relativeX = playerLocation.Value.X - mapBoundingBox.Value.X;
                int relativeY = playerLocation.Value.Y - mapBoundingBox.Value.Y;
                
                return $"Player Location: [{relativeX}; {relativeY}]";
            }
           
            return "Player Location: Unknown";
        }

        /// <summary>
        /// Formats the player's direction in degrees as a string.
        /// </summary>
        public string GetPlayerDirectionText()
        {
            var playerDirection = GetPlayerDirectionInDegrees();
            return $"Player Direction: {playerDirection:F2}";
        }

        /// <summary>
        /// Formats the radar match value as a string.
        /// </summary>
        public string GetRadarMatchValueText() { return $"Radar Match Value: {GetLatestRadarMatchValue():F2}"; }

        /// <summary>
        /// Formats the radar debug image visibility status as a string.
        /// </summary>
        public string GetShowRadarDebugText() { return $"Show Radar Debug: {_state.ShowRadarDebugImage}"; }

        /// <summary>
        /// Formats the current map name as a string.
        /// </summary>
        public string GetCurrentMapText() { return $"Current Map: {GetLatestMapName()}"; }

        /// <summary>
        /// Formats the map match value as a string.
        /// </summary>
        public string GetMapMatchValueText() { return $"Map Match Value: {GetLatestMapMatchValue():F2}"; }

        /// <summary>
        /// Formats the current weapon name as a string.
        /// </summary>
        public string GetCurrentWeaponText() { return $"Current Weapon: {GetLatestWeaponName()}"; }

        /// <summary>
        /// Formats the weapon match value as a string.
        /// </summary>
        public string GetWeaponMatchValueText() { return $"Weapon Match Value: {GetLatestWeaponMatchValue():F2}"; }

        /// <summary>
        /// Formats the weapon debug image visibility status as a string.
        /// </summary>
        public string GetShowWeaponDebugText() { return $"Show Weapon Debug: {_state.ShowWeaponDebugImage}"; }

        /// <summary>
        /// Formats the latest movement decision from the navigation manager as a string.
        /// </summary>
        public string GetMovementDecisionText() { return $"Move: {_navigationManager.LatestMovementDecision}"; }

        /// <summary>
        /// Formats the death detection match value as a string.
        /// </summary>
        public string GetDeathMatchValueText()
        {
            if (_radarManager.IsDeathDetected() && _state.LatestMapName != "Unknown")
            {
                return $"Death Match: {_radarManager.LatestDeathMatchValue:F2}";
            }
            else
            {
                return $"Death Match: 1.00";
            }
        }

        /// <summary>
        /// Formats the overall Aimbot status (Off, Active, or Waiting) as a string.
        /// </summary>
        public string GetAimbotStatusText()
        {
            if (!_state.AimbotEnabled)
            {
                return "Aimbot Status: Off";
            }

            if (_playerState.IsGameReadyForAimbot)
            {
                return "Aimbot Status: On (Active)";
            }
            else
            {
                return "Aimbot Status: On (Waiting)";
            }
        }

        /// <summary>
        /// Formats the lobby button debug image visibility status as a string.
        /// </summary>
        public string GetUIShowLobbyButtonDebugImage() { lock (_lockObject) { return $"Show Lobby Button Debug: {(_state.ShowLobbyButtonDebugImage ? "On" : "Off")}"; } }

        /// <summary>
        /// Formats the absolute value of the lobby match value as a string.
        /// </summary>
        public string GetUILobbyMatchValueText()
        {
            double displayValue = Math.Abs(_state.LatestLobbyMatchValue);
            
            return $"Lobby Match Value: {displayValue:F2}";
        }

        /// <summary>
        /// Formats the current lobby status (Yes/No) as a string.
        /// </summary>
        public string GetIsInLobbyText()
        {
            lock (_lockObject) { return $"In Lobby: {(_state.IsInLobby ? "Yes" : "No")}"; }
        }

        /// <summary>
        /// Formats the match value for the 'Start' button detection as a string.
        /// </summary>
        public string GetLatestStartButtonMatchValue() { lock (_lockObject) { return $"Start Button Found: {_state.LatestStartButtonMatchValue:F2}"; } }

        /// <summary>
        /// Formats the match value for the 'Cancel' button detection as a string.
        /// </summary>
        public string GetLatestCancelButtonMatchValue() { lock (_lockObject) { return $"Cancel Button Found: {_state.LatestCancelButtonMatchValue:F2}"; } }

        /// <summary>
        /// Calculates the required height of the text panel in the UI based on 
        /// the number of lines needed, factoring in the 'ShowFullInfo' state.
        /// </summary>
        /// <returns>The calculated height in pixels.</returns>
        public int GetTextPanelHeight()
        {
            const int lineHeight = 15;
            int lineCount = 3; // Default minimum lines

            if (_state.ShowFullInfo)
            {
                lineCount++; // FPS
                if (IsPlayerOnMapWithBoxMargin())
                {
                    lineCount += 2; // Status, Location
                }
                lineCount += 2; // Direction, Radar Match
                lineCount += 2; // Map Name, Map Match
                lineCount++; // Weapon Name
                lineCount += 2; // Weapon Match, Weapon Debug
                lineCount += 2; // Movement Decision, Aimbot Status
                lineCount += 2; // Death Match, Death Match
                lineCount += 4; // Lobby Status, Lobby Match, Start/Cancel Buttons
                lineCount++; // Lobby Debug toggle
            }
            return lineCount * lineHeight + 10;
        }
    }
}