using System;
using System.Drawing;
using System.Collections.Generic;
using OpenCvSharp;
using ParadiseHelper.AI.Video.Detect;

namespace ParadiseHelper.AI.Core
{
    /// <summary>
    /// Stores all shared state for the AI application.
    /// It acts as the single source of truth for various concurrent threads
    /// related to detection, timing, control, and UI status.
    /// </summary>
    public class AIState
    {
        // --- Timestamps & Cooldowns ---

        /// <summary>
        /// Gets or sets the last <see cref="DateTime"/> when a major control action (e.g., aiming, firing) was taken.
        /// Used for cooldown management.
        /// </summary>
        public DateTime LastActionTimestamp { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Gets or sets the last <see cref="DateTime"/> a player target was successfully detected.
        /// Used for tracking player presence and engagement timeout.
        /// </summary>
        public DateTime LastTimePlayerSeen { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Defines the maximum time allowed without a target before navigation state should be reset.
        /// </summary>
        public static readonly TimeSpan NavigationResetTimeout = TimeSpan.FromSeconds(4.5);

        // --- Hotkey State Flags ---

        // Flag indicating if the main activation key (e.g., Aimbot enable/disable) was pressed in the last check cycle.
        public bool wasActivationKeyPressed = false;

        // Flag indicating if the debug toggle key for general visuals was pressed.
        public bool wasToggleDebugKeyPressed = false;

        // Flag indicating if the debug toggle key for radar processing visuals was pressed.
        public bool wasToggleRadarKeyPressed = false;

        // Flag indicating if the debug toggle key for lobby detection visuals was pressed.
        public bool wasToggleLobbyKeyPressed = false;

        // --- UI & Debug Flags ---

        /// <summary>
        /// Gets or sets a value indicating whether the main aimbot function is currently active.
        /// </summary>
        public bool AimbotEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the processed weapon image should be displayed for debugging.
        /// </summary>
        public bool ShowWeaponDebugImage { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the processed lobby button images should be displayed for debugging.
        /// </summary>
        public bool ShowLobbyButtonDebugImage { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the processed radar image should be displayed for debugging.
        /// </summary>
        public bool ShowRadarDebugImage { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the full statistical information should be displayed in the UI.
        /// </summary>
        public bool ShowFullInfo { get; set; } = true;

        // --- Yolo Detection State ---

        /// <summary>
        /// Gets or sets the latest raw frame/image captured from the screen.
        /// </summary>
        public Bitmap LatestFrame { get; set; }

        /// <summary>
        /// Gets or sets the list of detection results from the latest YOLO inference run.
        /// </summary>
        public List<YoloResult> LatestResults { get; set; }

        /// <summary>
        /// Gets or sets the current frame rate (Frames Per Second) of the detection process.
        /// </summary>
        public float DetectionFps { get; set; }

        // --- Weapon State ---

        /// <summary>
        /// Gets or sets the name of the weapon currently detected by template matching (e.g., "AK-47").
        /// Defaults to "Unknown" if no match is found.
        /// </summary>
        public string LatestWeaponName { get; set; } = "Unknown";

        /// <summary>
        /// Gets or sets the match certainty value (0.0 to 1.0) for the latest weapon detection.
        /// </summary>
        public double LatestWeaponMatchValue { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets the processed <see cref="Mat"/> (OpenCV image) of the weapon icon for debugging display.
        /// </summary>
        public Mat LatestProcessedWeaponImage { get; set; }

        /// <summary>
        /// List of weapons considered 'extra' or non-primary, often for specific handling logic.
        /// </summary>
        public static readonly List<string> ExtraWeapons = new List<string> { "Glock-18", "USP-S", "P2000", "Knife" };

        /// <summary>
        /// Gets or sets the timestamp of the last observed respawn event.
        /// </summary>
        public DateTime LastRespawnTimestamp { get; set; } = DateTime.MinValue;

        // --- Auto-Buy State ---

        /// <summary>
        /// Gets or sets a flag indicating whether the auto-buy process should be initiated.
        /// </summary>
        public bool ShouldBuyWeapon { get; set; } = false;

        /// <summary>
        /// Gets or sets a flag indicating if the first-round auto-buy has been completed.
        /// </summary>
        public bool IsFirstBuyDone { get; set; } = false;

        /// <summary>
        /// Gets or sets the state of the Aimbot before the auto-buy process started, allowing restoration afterward.
        /// Null indicates no state saved.
        /// </summary>
        public bool? AimbotStateBeforeBuy { get; set; } = null;

        // The time (in milliseconds) allocated for the simulated weapon buy process.
        public static readonly int TimeForBuy = 500;

        // List of preferred primary weapons the AI should attempt to buy.
        public static List<string> desiredWeapons = new List<string> { "AK-47", "M4A1-S" };

        // --- Radar State ---

        /// <summary>
        /// Gets or sets the processed <see cref="Mat"/> (OpenCV image) of the radar area for display.
        /// </summary>
        public Mat LatestProcessedRadarImage { get; set; }

        /// <summary>
        /// Gets or sets the match certainty value (0.0 to 1.0) for the latest radar detection.
        /// </summary>
        public double LatestRadarMatchValue { get; set; }

        /// <summary>
        /// Gets or sets the current player's location as a coordinate on the radar image.
        /// Null if location could not be determined.
        /// </summary>
        public OpenCvSharp.Point? PlayerLocationOnRadar { get; set; }

        /// <summary>
        /// Gets or sets the current direction vector or orientation of the player on the radar.
        /// Null if direction could not be determined.
        /// </summary>
        public OpenCvSharp.Point? PlayerDirection { get; set; }

        // --- Map State ---

        /// <summary>
        /// Gets or sets the name of the map currently loaded in the game (e.g., "Dust2").
        /// Defaults to "Unknown".
        /// </summary>
        public string LatestMapName { get; set; } = "Unknown";

        /// <summary>
        /// Gets or sets the match certainty value (0.0 to 1.0) for the latest map identification.
        /// </summary>
        public double LatestMapMatchValue { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the bounding box <see cref="Rect"/> of the detected map image on screen.
        /// Null if map location is unknown.
        /// </summary>
        public Rect? LatestMapBoundingBox { get; set; }

        /// <summary>
        /// The threshold below which a map match is considered unreliable.
        /// </summary>
        public static readonly double MapMatchThreshold = 0.3;

        /// <summary>
        /// List of maps where the AI functionality should be disabled or altered.
        /// </summary>
        public static readonly List<string> forbiddenMaps = new List<string> { "Vertigo_Topside", "Vertigo_Underside" };

        // --- Lobby State & Auto-Reconnect ---

        // The interval (in milliseconds) at which lobby elements are scanned.
        public static readonly int LobbyDetectionIntervalMs = 2000;

        /// <summary>
        /// Gets or sets a flag indicating whether the game is currently in a "searching for match" state.
        /// </summary>
        public bool IsSearchingGame { get; set; } = false;

        /// <summary>
        /// Gets or sets a flag indicating whether the "Match is Ready" button is currently visible.
        /// </summary>
        public bool IsMatchIsReady { get; set; } = false;

        /// <summary>
        /// Gets or sets a flag indicating if the application is currently operating within the game lobby menu.
        /// </summary>
        public bool IsInLobby { get; set; } = false;

        /// <summary>
        /// Gets or sets the processed <see cref="Mat"/> of the general lobby area for display.
        /// </summary>
        public Mat LatestProcessedLobbyImage { get; set; }

        /// <summary>
        /// Gets or sets the match certainty value (0.0 to 1.0) for the latest general lobby detection.
        /// </summary>
        public double LatestLobbyMatchValue { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the processed <see cref="Mat"/> of the map selection image when in Line Mode.
        /// </summary>
        public Mat LatestProcessedLineModeMapImage { get; set; }

        /// <summary>
        /// Gets or sets the match certainty value (0.0 to 1.0) for the Line Mode Map image detection.
        /// </summary>
        public double LatestLineModeMapMatchValue { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the processed <see cref="Mat"/> of the 'Start' button image.
        /// </summary>
        public Mat LatestProcessedStartButtonImage { get; set; }

        /// <summary>
        /// Gets or sets the match certainty value (0.0 to 1.0) for the 'Start' button detection.
        /// </summary>
        public double LatestStartButtonMatchValue { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the processed <see cref="Mat"/> of the 'Cancel' button image.
        /// </summary>
        public Mat LatestProcessedCancelButtonImage { get; set; }

        /// <summary>
        /// Gets or sets the match certainty value (0.0 to 1.0) for the 'Cancel' button detection.
        /// </summary>
        public double LatestCancelButtonMatchValue { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the processed <see cref="Mat"/> of the disconnection error label image.
        /// </summary>
        public Mat LatestProcessedDisconnectionLabelImage { get; set; }

        /// <summary>
        /// Gets or sets the match certainty value (0.0 to 1.0) for the disconnection label detection.
        /// </summary>
        public double LatestDisconnectionLabelMatchValue { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the processed <see cref="Mat"/> of the 'Match is Ready' label image.
        /// </summary>
        public Mat LatestProcessedMatchIsReadyLabelImage { get; set; }

        /// <summary>
        /// Gets or sets the match certainty value (0.0 to 1.0) for the 'Match is Ready' label detection.
        /// </summary>
        public double LatestMatchIsReadyLabelMatchValue { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the processed <see cref="Mat"/> of the 'Team Selection Terrorist/Counter-Terrorist' image.
        /// </summary>
        public Mat LatestProcessedTeamSelectionTTImage { get; set; }

        /// <summary>
        /// Gets or sets the match certainty value (0.0 to 1.0) for the Team Selection image detection.
        /// </summary>
        public double LatestTeamSelectionTTMatchValue { get; set; } = 1.0;
    }
}