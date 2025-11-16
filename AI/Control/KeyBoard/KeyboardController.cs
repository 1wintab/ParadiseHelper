using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace ParadiseHelper.AI.Control.KeyBoard
{
    /// <summary>
    /// Static class for simulating keyboard input using the native Windows API (<c>user32.dll</c>) 
    /// and maintaining a thread-safe, cached record of which keys are currently held down.
    /// </summary>
    public static class KeyboardController
    {
        // P/Invoke declaration for the native Windows keybd_event function.
        // This function is used to synthesize a keystroke.
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

        // Flag indicating a key release event.
        private const uint KEYEVENTF_KEYUP = 0x0002;

        /// <summary>
        /// Thread-safe dictionary to track the current pressed state of managed keys.
        /// Keys mapped to <c>true</c> are considered pressed.
        /// </summary>
        private static readonly ConcurrentDictionary<Keys, bool> _keyStates = new ConcurrentDictionary<Keys, bool>();

        /// <summary>
        /// Path for logging key press/release failures due to exceptions.
        /// </summary>
        private static readonly string LogFilePath = "debug.log";

        /// <summary>
        /// Checks if a specific key is currently tracked as being pressed.
        /// </summary>
        /// <param name="key">The <see cref="Keys"/> enum value to check (e.g., <c>Keys.F6</c>).</param>
        /// <returns><c>true</c> if the key is pressed and tracked; otherwise, <c>false</c>.</returns>
        public static bool IsKeyPressed(Keys key)
        {
            return _keyStates.TryGetValue(key, out bool isPressed) && isPressed;
        }

        /// <summary>
        /// Simulates pressing a key down using the Windows API.
        /// </summary>
        /// <remarks>
        /// If the key is already tracked as pressed, no action is taken. Any API failure is logged.
        /// </remarks>
        /// <param name="key">The key to press down.</param>
        public static void PressKey(Keys key)
        {
            // Do not press the key if it is already tracked as pressed
            if (_keyStates.TryGetValue(key, out bool isPressed) && isPressed) return;

            try
            {
                // Call the native function to simulate key down (dwFlags = 0)
                keybd_event((byte)key, 0, 0, nuint.Zero);
                _keyStates[key] = true; // Update state cache
            }
            catch (Exception ex)
            {
                // Log failure to press the key
                File.AppendAllText(
                    LogFilePath,
                    $"{DateTime.Now:HH:mm:ss.fff} - [Keyboard] FAILED to press key: {key}. " +
                    $"Exception: {ex.Message}{Environment.NewLine}"
                );
            }
        }

        /// <summary>
        /// Simulates releasing a key using the Windows API.
        /// </summary>
        /// <remarks>
        /// Only releases the key if it is currently tracked as pressed. Any API failure is logged.
        /// </remarks>
        /// <param name="key">The key to release.</param>
        public static void ReleaseKey(Keys key)
        {
            // Only release if currently tracked as pressed
            if (!_keyStates.TryGetValue(key, out bool isPressed) || !isPressed)
                return;

            try
            {
                // Call the native function to simulate key release (dwFlags = KEYEVENTF_KEYUP)
                keybd_event((byte)key, 0, KEYEVENTF_KEYUP, nuint.Zero);
                _keyStates[key] = false; // Update state cache
            }
            catch (Exception ex)
            {
                // Log failure to release the key
                File.AppendAllText(
                    LogFilePath,
                    $"{DateTime.Now:HH:mm:ss.fff} - [Keyboard] FAILED to release key: {key}. " +
                    $"Exception: {ex.Message}{Environment.NewLine}"
                );
            }
        }

        /// <summary>
        /// Forces the release of all keys currently tracked as being pressed.
        /// This is useful for stopping auto-run functions in case of application exit or error.
        /// </summary>
        public static void ReleaseAllKeys()
        {
            // Using ToList() is important here to avoid modifying the collection while iterating it.
            foreach (var key in _keyStates.Keys.ToList())
            {
                if (_keyStates.TryGetValue(key, out bool isPressed) && isPressed)
                {
                    // This calls the ReleaseKey method, which safely updates the state.
                    ReleaseKey(key);
                }
            }
        }
    }
}