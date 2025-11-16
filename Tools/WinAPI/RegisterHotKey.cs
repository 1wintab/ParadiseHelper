using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ParadiseHelper.Tools
{
    // Class to register and handle a global hotkey using the Windows API.
    // Must be used with a Form's WndProc method to process the WM_HOTKEY message.
    public class RegisterHotKeyHandler : IDisposable
    {
        // P/Invoke: Registers a system-wide hotkey.
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(nint hWnd, int id, int fsModifiers, int vk);

        // P/Invoke: Unregisters a system-wide hotkey.
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(nint hWnd, int id);

        // Constant for the Windows message received when a registered hotkey is pressed.
        private const int WM_HOTKEY = 0x0312;

        // Handle (hWnd) of the window registering the hotkey (often the main Form).
        private readonly nint _handle;

        // Unique ID assigned to this hotkey registration.
        private readonly int _hotkeyId;

        // The method to execute when the hotkey is pressed.
        private readonly Action _onHotKey; 

        public RegisterHotKeyHandler(nint handle, Keys key, KeyModifiers modifiers, Action onHotKey)
        {
            _handle = handle;
            
            // Generate a unique hotkey ID (using hash code truncated to 16 bits).
            // NOTE: While GetHashCode is easy, in a production app, a more robust ID generation might be needed
            // to prevent potential conflicts, such as using Interlocked.Increment on a static counter.
            _hotkeyId = GetHashCode() & 0xFFFF;
            
            _onHotKey = onHotKey;

            // Attempt to register the hotkey with Windows.
            bool success = RegisterHotKey(_handle, _hotkeyId, (int)modifiers, (int)key);
            if (!success)
            {
                // Throw exception if registration fails (e.g., hotkey already in use).
                // Alternatively, you could log the failure and return gracefully:
                // Debug.WriteLine($"Failed to register hotkey {modifiers} + {key}. Error code: {Marshal.GetLastWin32Error()}");
                throw new InvalidOperationException($"Unable to register hotkey: {modifiers} + {key}. It may already be in use.");
            }
        }

        // Must be called inside the Form's WndProc override to listen for the hotkey message.
        public void ProcessHotKey(ref Message m)
        {
            // Check if the message is a hotkey message and if the ID matches this instance's hotkey.
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == _hotkeyId)
            {
                _onHotKey?.Invoke(); // Execute the registered action.
            }
        }

        // Implementation of IDisposable: clean up the hotkey registration when disposed.
        public void Dispose()
        {
            UnregisterHotKey(_handle, _hotkeyId);
            
            // Suppress finalization, as cleanup has been done.
            GC.SuppressFinalize(this);
        }

        // Flags enumeration for key modifiers (Alt, Control, Shift, Win).
        [Flags]
        public enum KeyModifiers
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            Win = 8
        }
    }
}