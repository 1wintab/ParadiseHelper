using Core;
using System.IO;
using ParadiseHelper.AI.Extinctions;

namespace ParadiseHelper.AI.ClickerAutoRunGame
{
    /// <summary>
    /// Holds and initializes all <see cref="TemplateMatcherBinarization"/> objects used for 
    /// detecting specific UI elements (buttons, labels, maps) in the game's lobby or menu screen.
    /// </summary>
    public class SettingsDetector
    {
        // --- Home Button Template ---

        /// <summary>
        /// Width of the Home Button template in pixels (14).
        /// </summary>
        public const int HOME_BUTTON_WIDTH = 14;

        /// <summary>
        /// Height of the Home Button template in pixels (14).
        /// </summary>
        public const int HOME_BUTTON_HEIGHT = 14;

        /// <summary>
        /// X-coordinate of the top-left corner of the Home Button region (9).
        /// </summary>
        private const int HOME_BUTTON_X = 9;

        /// <summary>
        /// Y-coordinate of the top-left corner of the Home Button region (15).
        /// </summary>
        private const int HOME_BUTTON_Y = 15;

        /// <summary>
        /// The file name of the Home Button template image ("home_button.png").
        /// </summary>
        private const string HOME_FILE_NAME = "home_button.png";

        /// <summary>
        /// The required match threshold (0.0 to 1.0) for the Home Button to be considered found (0.9).
        /// </summary>
        private const double HOME_MATCH_THRESHOLD = 0.9;

        /// <summary>
        /// Gets the template matcher instance for the Home Button, typically located at the top-left of the screen.
        /// </summary>
        public TemplateMatcherBinarization HomeButton { get; } = new TemplateMatcherBinarization(
            HOME_BUTTON_X,
            HOME_BUTTON_Y,
            HOME_BUTTON_WIDTH,
            HOME_BUTTON_HEIGHT,
            Path.Combine(FilePaths.AI.Templates.LobbyDirectory, HOME_FILE_NAME),
            HOME_MATCH_THRESHOLD
        );

        // --- Line Selected Mode Map Template ---

        /// <summary>
        /// Width of the Line Selected Mode Map template in pixels (242).
        /// </summary>
        public const int LINE_MODE_MAP_WIDTH = 242;

        /// <summary>
        /// Height of the Line Selected Mode Map template in pixels (15).
        /// </summary>
        public const int LINE_MODE_MAP_HEIGHT = 15;

        /// <summary>
        /// X-coordinate of the top-left corner of the Line Mode Map region (215).
        /// </summary>
        private const int LINE_MODE_MAP_X = 215;

        /// <summary>
        /// Y-coordinate of the top-left corner of the Line Mode Map region (618).
        /// </summary>
        private const int LINE_MODE_MAP_Y = 618;

        /// <summary>
        /// The file name of the Line Selected Mode Map template image ("line_mode_map.png").
        /// </summary>
        private const string LINE_MODE_FILE_NAME = "line_mode_map.png";

        /// <summary>
        /// The required match threshold (0.0 to 1.0) for the Line Mode Map to be considered found (0.8).
        /// </summary>
        private const double LINE_MODE_MAP_MATCH_THRESHOLD = 0.8;

        /// <summary>
        /// Gets the template matcher instance for the visual indicator that a 'Line Mode' map is selected.
        /// </summary>
        public TemplateMatcherBinarization LineModeMap { get; } = new TemplateMatcherBinarization(
            LINE_MODE_MAP_X,
            LINE_MODE_MAP_Y,
            LINE_MODE_MAP_WIDTH,
            LINE_MODE_MAP_HEIGHT,
            Path.Combine(FilePaths.AI.Templates.LobbyDirectory, LINE_MODE_FILE_NAME),
            LINE_MODE_MAP_MATCH_THRESHOLD
        );

        // --- Start Button Template ---

        /// <summary>
        /// Width of the Start Button template in pixels (200).
        /// </summary>
        public const int START_BUTTON_WIDTH = 200;

        /// <summary>
        /// Height of the Start Button template in pixels (39).
        /// </summary>
        public const int START_BUTTON_HEIGHT = 39;

        /// <summary>
        /// X-coordinate of the top-left corner of the Start Button region (1023).
        /// </summary>
        private const int START_BUTTON_X = 1023;

        /// <summary>
        /// Y-coordinate of the top-left corner of the Start Button region (669).
        /// </summary>
        private const int START_BUTTON_Y = 669;

        /// <summary>
        /// The file name of the Start Button template image ("start_button.png").
        /// </summary>
        private const string START_FILE_NAME = "start_button.png";

        /// <summary>
        /// The required match threshold (0.0 to 1.0) for the Start Button to be considered found (0.8).
        /// </summary>
        private const double START_MATCH_THRESHOLD = 0.8;

        /// <summary>
        /// Gets the template matcher instance for the Start Button, used to begin a match.
        /// </summary>
        public TemplateMatcherBinarization StartButton { get; } = new TemplateMatcherBinarization(
            START_BUTTON_X,
            START_BUTTON_Y,
            START_BUTTON_WIDTH,
            START_BUTTON_HEIGHT,
            Path.Combine(FilePaths.AI.Templates.LobbyDirectory, START_FILE_NAME),
            START_MATCH_THRESHOLD
        );

        // --- Cancel Button Template ---

        /// <summary>
        /// Width of the Cancel Button template in pixels (200).
        /// </summary>
        public const int CANCEL_BUTTON_WIDTH = 200;

        /// <summary>
        /// Height of the Cancel Button template in pixels (39).
        /// </summary>
        public const int CANCEL_BUTTON_HEIGHT = 39;

        /// <summary>
        /// X-coordinate of the top-left corner of the Cancel Button region (1023).
        /// </summary>
        private const int CANCEL_BUTTON_X = 1023;

        /// <summary>
        /// Y-coordinate of the top-left corner of the Cancel Button region (669).
        /// </summary>
        private const int CANCEL_BUTTON_Y = 669;

        /// <summary>
        /// The file name of the Cancel Button template image ("cancel_button.png").
        /// </summary>
        private const string CANCEL_FILE_NAME = "cancel_button.png";

        /// <summary>
        /// The required match threshold (0.0 to 1.0) for the Cancel Button to be considered found (0.8).
        /// </summary>
        private const double CANCEL_MATCH_THRESHOLD = 0.8;

        /// <summary>
        /// Gets the template matcher instance for the Cancel Button, which typically appears while waiting in a queue.
        /// </summary>
        public TemplateMatcherBinarization CancelButton { get; } = new TemplateMatcherBinarization(
            CANCEL_BUTTON_X,
            CANCEL_BUTTON_Y,
            CANCEL_BUTTON_WIDTH,
            CANCEL_BUTTON_HEIGHT,
            Path.Combine(FilePaths.AI.Templates.LobbyDirectory, CANCEL_FILE_NAME),
            CANCEL_MATCH_THRESHOLD
        );

        // --- Disconnected Label Template ---

        /// <summary>
        /// Width of the Disconnection Label template in pixels (111).
        /// </summary>
        public const int DISCONNECTION_LABEL_WIDTH = 111;

        /// <summary>
        /// Height of the Disconnection Label template in pixels (22).
        /// </summary>
        public const int DISCONNECTION_LABEL_HEIGHT = 22;

        /// <summary>
        /// X-coordinate of the top-left corner of the Disconnection Label region (485).
        /// </summary>
        private const int DISCONNECTION_LABEL_X = 485;

        /// <summary>
        /// Y-coordinate of the top-left corner of the Disconnection Label region (298).
        /// </summary>
        private const int DISCONNECTION_LABEL_Y = 298;

        /// <summary>
        /// The file name of the Disconnection Label template image ("disconnection_label.png").
        /// </summary>
        private const string DISCONNECTION_LABEL_FILE_NAME = "disconnection_label.png";

        /// <summary>
        /// The required match threshold (0.0 to 1.0) for the Disconnection Label to be considered found (0.8).
        /// </summary>
        private const double DISCONNECTION_LABEL_MATCH_THRESHOLD = 0.8;

        /// <summary>
        /// Gets the template matcher instance for the 'Disconnected' label, indicating a connection failure.
        /// </summary>
        public TemplateMatcherBinarization DisconnectionLabel { get; } = new TemplateMatcherBinarization(
            DISCONNECTION_LABEL_X,
            DISCONNECTION_LABEL_Y,
            DISCONNECTION_LABEL_WIDTH,
            DISCONNECTION_LABEL_HEIGHT,
            Path.Combine(FilePaths.AI.Templates.LobbyDirectory, DISCONNECTION_LABEL_FILE_NAME),
            DISCONNECTION_LABEL_MATCH_THRESHOLD
        );

        // --- Match Is Ready Label Template ---

        /// <summary>
        /// Width of the 'Match Is Ready' Label template in pixels (262).
        /// </summary>
        public const int MATCH_IS_READY_LABEL_WIDTH = 262;

        /// <summary>
        /// Height of the 'Match Is Ready' Label template in pixels (40).
        /// </summary>
        public const int MATCH_IS_READY_LABEL_HEIGHT = 40;

        /// <summary>
        /// X-coordinate of the top-left corner of the 'Match Is Ready' Label region (508).
        /// </summary>
        private const int MATCH_IS_READY_LABEL_X = 508;

        /// <summary>
        /// Y-coordinate of the top-left corner of the 'Match Is Ready' Label region (238).
        /// </summary>
        private const int MATCH_IS_READY_LABEL_Y = 238;

        /// <summary>
        /// The file name of the 'Match Is Ready' Label template image ("match_is_ready_label.png").
        /// </summary>
        private const string MATCH_IS_READY_LABEL_FILE_NAME = "match_is_ready_label.png";

        /// <summary>
        /// The required match threshold (0.0 to 1.0) for the 'Match Is Ready' Label to be considered found (0.8).
        /// </summary>
        private const double MATCH_IS_READY_LABEL_MATCH_THRESHOLD = 0.8;

        /// <summary>
        /// Gets the template matcher instance for the 'Match Is Ready' notification label.
        /// </summary>
        public TemplateMatcherBinarization MatchIsReadyLabel { get; } = new TemplateMatcherBinarization(
                MATCH_IS_READY_LABEL_X,
                MATCH_IS_READY_LABEL_Y,
                MATCH_IS_READY_LABEL_WIDTH,
                MATCH_IS_READY_LABEL_HEIGHT,
                Path.Combine(FilePaths.AI.Templates.LobbyDirectory, MATCH_IS_READY_LABEL_FILE_NAME),
                MATCH_IS_READY_LABEL_MATCH_THRESHOLD
        );

        // --- Team Selection TT Template ---

        /// <summary>
        /// Width of the Team Selection 'TT' (Terrorist/Team) Icon template in pixels (55).
        /// </summary>
        public const int TEAM_SELECTION_TT_WIDTH = 55;

        /// <summary>
        /// Height of the Team Selection 'TT' (Terrorist/Team) Icon template in pixels (55).
        /// </summary>
        public const int TEAM_SELECTION_TT_HEIGHT = 55;

        /// <summary>
        /// X-coordinate of the top-left corner of the Team Selection 'TT' Icon region (346).
        /// </summary>
        private const int TEAM_SELECTION_TT_X = 346;

        /// <summary>
        /// Y-coordinate of the top-left corner of the Team Selection 'TT' Icon region (27).
        /// </summary>
        private const int TEAM_SELECTION_TT_Y = 27;

        /// <summary>
        /// The file name of the Team Selection 'TT' Icon template image ("team_selection_tt.png").
        /// </summary>
        private const string TEAM_SELECTION_TT_FILE_NAME = "team_selection_tt.png";

        /// <summary>
        /// The required match threshold (0.0 to 1.0) for the Team Selection 'TT' Icon to be considered found (0.8).
        /// </summary>
        private const double TEAM_SELECTION_TT_MATCH_THRESHOLD = 0.8;

        /// <summary>
        /// Gets the template matcher instance for the 'TT' icon in the team selection menu.
        /// </summary>
        public TemplateMatcherBinarization TeamSelectionTT { get; } = new TemplateMatcherBinarization(
                TEAM_SELECTION_TT_X,
                TEAM_SELECTION_TT_Y,
                TEAM_SELECTION_TT_WIDTH,
                TEAM_SELECTION_TT_HEIGHT,
                Path.Combine(FilePaths.AI.Templates.LobbyDirectory, TEAM_SELECTION_TT_FILE_NAME),
                TEAM_SELECTION_TT_MATCH_THRESHOLD
        );

        // --- WeaponReader ---

        /// <summary>
        /// X-coordinate of the region of interest for the Weapon Reader (1195).
        /// </summary>
        public const int WEAPON_NAME_X = 1195;

        /// <summary>
        /// Y-coordinate of the region of interest for the Weapon Reader (540).
        /// </summary>
        public const int WEAPON_NAME_Y = 540;

        /// <summary>
        /// Width of the region of interest for the Weapon Reader (69).
        /// </summary>
        public const int WEAPON_NAME_WIDTH = 69;

        /// <summary>
        /// Height of the region of interest for the Weapon Reader (180).
        /// </summary>
        public const int WEAPON_NAME_HEIGHT = 180;

        /// <summary>
        /// Generic match threshold, likely used for sub-templates within the WeaponReader logic (0.9).
        /// </summary>
        public const double MATCH_THRESHOLD = 0.9;
    }
}