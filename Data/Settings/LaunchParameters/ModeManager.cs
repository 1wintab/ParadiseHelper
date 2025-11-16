using System.IO;
using System.Drawing;
using System.Collections.Generic;
using Core;
using Data.Settings.LaunchParameters;

namespace ParadiseHelper.Data.Settings.LaunchParameters
{
    /// <summary>
    /// Manages the collection of available launch modes and tracks the currently selected mode.
    /// </summary>
    public static class ModeManager
    {
        private static readonly List<ModeDefinition> _availableModes;

        /// <summary>
        /// Gets or sets the zero-based index of the currently selected launch mode. Defaults to 0 (Default mode).
        /// </summary>
        public static int CurrentModeIndex { get; set; } = 0;

        /// <summary>
        /// Gets the <see cref="ModeDefinition"/> object for the currently active mode.
        /// </summary>
        public static ModeDefinition CurrentMode => _availableModes[CurrentModeIndex];

        /// <summary>
        /// Gets a read-only list of all defined launch modes.
        /// </summary>
        public static IReadOnlyList<ModeDefinition> AvailableModes => _availableModes;

        /// <summary>
        /// Static constructor to initialize the list of available modes.
        /// </summary>
        static ModeManager()
        {
            _availableModes = new List<ModeDefinition>
            {
                new ModeDefinition
                {
                    Mode = LaunchMode.Default,
                    Name = "Default mode",
                    Description = "Launches the game with standard, user-defined settings.",
                    Icon = Image.FromFile(Path.Combine(FilePaths.Standard.Images.LaunchParametersDirectory, "default_icon.png")),
                    Cs2ConfigFile = "default_cs2_launch.txt",
                    SteamConfigFile = "default_steam_launch.txt",
                    ExtraParamsFile = "default_extra_params.txt"
                },
                new ModeDefinition
                {
                    Mode = LaunchMode.AICore,
                    Name = "AI Mode CS2",
                    Description = "Launches CS2 with optimized settings and configurations for AI bot operation.",
                    Icon = Image.FromFile(Path.Combine(FilePaths.Standard.Images.LaunchParametersDirectory, "ai_core_icon.png")),
                    Cs2ConfigFile = "ai_cs2_launch.txt",
                    SteamConfigFile = "ai_steam_launch.txt",
                    ExtraParamsFile = "ai_extra_params.txt"
                }
            };
        }

        /// <summary>
        /// Cycles the <see cref="CurrentModeIndex"/> forward to the next available mode.
        /// If the current mode is the last in the list, it wraps around to the first mode.
        /// </summary>
        public static void CycleToNextMode()
        {
            CurrentModeIndex = (CurrentModeIndex + 1) % _availableModes.Count;
        }

        /// <summary>
        /// Cycles the <see cref="CurrentModeIndex"/> backward to the previous available mode.
        /// If the current mode is the first in the list, it wraps around to the last mode.
        /// </summary>
        public static void CycleToPreviousMode()
        {
            if (CurrentModeIndex == 0)
            {
                CurrentModeIndex = _availableModes.Count - 1;
            }
            else
            {
                CurrentModeIndex--;
            }
        }
    }
}