using System;
using System.IO;

namespace Core
{
    /// <summary>
    /// The central entry point for all application file and directory paths.
    /// This structure uses nested static classes to logically reflect the folder hierarchy on disk.
    /// </summary>
    public static class FilePaths
    {
        // Gets the base directory where the application executable resides.
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;

        // The root directory for all general application data.
        private static readonly string DataDir = Path.Combine(BaseDir, "Data");
        
        // The root directory for all general image assets.
        private static readonly string ImagesDir = Path.Combine(DataDir, "Images");

        // The root directory for all AI-related files and data.
        private static readonly string AIDir = Path.Combine(BaseDir, "AI");

        // === Global Utility Paths ===

        // Directory for storing application log files (located under BaseDir).
        public static readonly string LogsDirectory = Path.Combine(BaseDir, "Logs");
        
        // Directory for the ParserMaFile tool (located under BaseDir/Tools).
        public static readonly string ParserMaFileDirectory = Path.Combine(BaseDir, "Tools", "ParserMaFile");

        // --- Standard Application Data (BaseDir/Data) ---
        /// <summary>
        /// Contains paths for standard, non-AI application data located in the 'Data' folder.
        /// </summary>
        public static class Standard
        {
            // Directory for Steam's mobile authenticator files (maFiles).
            public static readonly string MaFilesDirectory = Path.Combine(DataDir, "Steam", "maFiles");
            
            // Directory for custom font files.
            public static readonly string FontsDirectory = Path.Combine(DataDir, "Fonts");
            
            // Directory for application database files.
            public static readonly string DataBaseDirectory = Path.Combine(DataDir, "Database");

            /// <summary>
            /// Paths related to configuration and application settings.
            /// </summary>
            public static class Settings
            {
                // Root directory for all application settings.
                private static readonly string SettingsDir = Path.Combine(DataDir, "Settings");
                
                // Directory for main configuration files.
                public static readonly string ConfigDirectory = Path.Combine(SettingsDir, "Config");
                
                // Directory storing custom launch parameters for applications.
                public static readonly string ParamsFoldersDirectory = Path.Combine(SettingsDir, "Launchparams");
                
                // Directory for game-specific configuration presets.
                public static readonly string GamePresetsDirectory = Path.Combine(SettingsDir, "GamePresets");
            }

            /// <summary>
            /// Paths related to standard image resources.
            /// </summary>
            public static class Images
            {
                // Directory for images used specifically in the Launch Parameters form.
                public static readonly string LaunchParametersDirectory = Path.Combine(ImagesDir, "LaunchParametersForm");
            }

            /// <summary>
            /// Paths related to OBS Websocket configuration data.
            /// </summary>
            public static class OBS
            {
                // Directory containing OBS Websocket configuration files.
                public static readonly string OBSConfigDirectory = Path.Combine(DataDir, "OBS Websocket");
            }

            /// <summary>
            /// Paths for general application template files.
            /// </summary>
            public static class Templates
            {
                // Root directory for all general templates.
                private static readonly string TemplatesDir = Path.Combine(DataDir, "Templates");
                
                // Directory for Steam-related template files.
                public static readonly string SteamDirectory = Path.Combine(TemplatesDir, "Steam");
            }
        }

        // --- AI Module Data (BaseDir/AI) ---
        /// <summary>
        /// Contains paths for data and configuration specific to the AI module.
        /// </summary>
        public static class AI
        {
            // Root data directory for the AI module.
            private static readonly string AIDataDir = Path.Combine(AIDir, "Data");
            
            // Directory for AI configuration settings.
            private static readonly string AISettingsDir = Path.Combine(AIDataDir, "Settings");
            
            // Directory for AI template images used for detection.
            private static readonly string AITemplatesDir = Path.Combine(AIDataDir, "Templates");

            // Primary AI data directories.
            
            // Directory for navigation mesh data (e.g., for pathfinding).
            public static readonly string NavmeshDirectory = Path.Combine(AIDataDir, "Navmesh");
            
            // Directory for machine learning models.
            public static readonly string ModelsDirectory = Path.Combine(AIDataDir, "Models");

            /// <summary>
            /// Paths for AI-specific configuration settings.
            /// </summary>
            public static class Settings
            {
                // Directory for weapon-related configuration and settings.
                public static readonly string WeaponDirectory = Path.Combine(AISettingsDir, "Weapon");
                
                // Directory for Counter-Strike 2 configuration files used by the AI.
                public static readonly string CS2ConfigsDirectory = Path.Combine(AISettingsDir, "CS2Configs");
            }

            /// <summary>
            /// Paths for template images used by the AI module for visual detection (e.g., object recognition).
            /// </summary>
            public static class Templates
            {
                // Directory for templates used to detect the game lobby.
                public static readonly string LobbyDirectory = Path.Combine(AITemplatesDir, "Lobby");
                
                // Directory for map-related templates or data.
                public static readonly string MapsDirectory = Path.Combine(AITemplatesDir, "Maps");
                
                // Directory for weapon detection templates.
                public static readonly string WeaponDirectory = Path.Combine(AITemplatesDir, "Weapon");
            }
        }
    }
}