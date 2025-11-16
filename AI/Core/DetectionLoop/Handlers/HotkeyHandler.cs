using System;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ParadiseHelper.AI.Core.DetectionLoop.Handlers
{
    /// <summary>
    /// Handler responsible for continuously monitoring and managing hotkey inputs to toggle various AI features and debug visualization states.
    /// This runs in a dedicated thread loop inherited from <see cref="DetectionLoopBase"/>.
    /// </summary>
    public class HotkeyHandler : DetectionLoopBase
    {
        /// <summary>
        /// Imports the Windows API function to check the asynchronous state (pressed or not) of a specified virtual key.
        /// </summary>
        /// <param name="vKey">The virtual key code.</param>
        /// <returns>A short value representing the key state.</returns>
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(Keys vKey);

        // Constant mask used to check if the most significant bit is set (indicating the key is currently down).
        private const int KeyIsDownMask = 0x8000;

        // The shared AI state object used to read and toggle feature flags (e.g., AimbotEnabled, ShowDebugImage).
        private readonly AIState _state;

        // --- Hotkey Definitions ---

        /// <summary>
        /// Gets or sets the key used to toggle the main Aimbot functionality. Defaults to <see cref="Keys.XButton2"/>.
        /// </summary>
        public Keys AimBotDebugKey { get; set; } = Keys.XButton2;

        /// <summary>
        /// Gets or sets the key used to toggle the visibility of the Weapon Debug visualization. Defaults to <see cref="Keys.PageDown"/>.
        /// </summary>
        public Keys WeaponDebugKey { get; set; } = Keys.PageDown;

        /// <summary>
        /// Gets or sets the key used to toggle the visibility of the Radar Debug visualization. Defaults to <see cref="Keys.PageUp"/>.
        /// </summary>
        public Keys RadarDebugKey { get; set; } = Keys.PageUp;

        /// <summary>
        /// Gets or sets the key used to toggle the visibility of the Lobby Button Debug visualization. Defaults to <see cref="Keys.Home"/>.
        /// </summary>
        public Keys LobbyDebugKey { get; set; } = Keys.Home;

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyHandler"/> class.
        /// </summary>
        /// <param name="state">The shared AI state object used to read and toggle feature flags.</param>
        public HotkeyHandler(AIState state)
        {
            _state = state;
        }

        /// <summary>
        /// The main execution loop that continuously polls the system for hotkey presses.
        /// </summary>
        /// <param name="token">A token to observe for cancellation requests for clean thread shutdown.</param>
        protected override void Loop(CancellationToken token)
        {
            // The loop runs until the cancellation token is requested.
            while (!token.IsCancellationRequested)
            {
                // --- Aimbot Toggle Logic ---
                bool isActivationKeyPressed = (GetAsyncKeyState(AimBotDebugKey) & KeyIsDownMask) != 0;

                // Toggle Aimbot state only on the key down event (when !_state.was... is false).
                if (isActivationKeyPressed && !_state.wasActivationKeyPressed)
                {
                    _state.AimbotEnabled = !_state.AimbotEnabled;
                    _state.LastActionTimestamp = DateTime.UtcNow;
                }
                
                // Update the tracking flag to detect the key release in the next iteration.
                _state.wasActivationKeyPressed = isActivationKeyPressed;

                // --- Weapon Debug Toggle Logic ---
                bool isToggleDebugKeyPressed = (GetAsyncKeyState(WeaponDebugKey) & KeyIsDownMask) != 0;
                if (isToggleDebugKeyPressed && !_state.wasToggleDebugKeyPressed)
                {
                    _state.ShowWeaponDebugImage = !_state.ShowWeaponDebugImage;
                }
                _state.wasToggleDebugKeyPressed = isToggleDebugKeyPressed;

                // --- Radar Debug Toggle Logic ---
                bool isToggleRadarKeyPressed = (GetAsyncKeyState(RadarDebugKey) & KeyIsDownMask) != 0;
                if (isToggleRadarKeyPressed && !_state.wasToggleRadarKeyPressed)
                {
                    _state.ShowRadarDebugImage = !_state.ShowRadarDebugImage;
                }
                _state.wasToggleRadarKeyPressed = isToggleRadarKeyPressed;

                // --- Lobby Debug Toggle Logic ---
                bool isToggleLobbyKeyPressed = (GetAsyncKeyState(LobbyDebugKey) & KeyIsDownMask) != 0;
                if (isToggleLobbyKeyPressed && !_state.wasToggleLobbyKeyPressed)
                {
                    _state.ShowLobbyButtonDebugImage = !_state.ShowLobbyButtonDebugImage;
                }
                _state.wasToggleLobbyKeyPressed = isToggleLobbyKeyPressed;

                // Short, necessary delay to prevent high CPU usage from continuous polling and stabilize key detection.
                Thread.Sleep(10);
            }
        }
    }
}