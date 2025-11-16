using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ParadiseHelper.Tools.WinAPI
{
    /// <summary>
    /// Provides high-level, static methods for managing windows and processes using the Windows API.
    /// This is the main interface for window control logic within the application.
    /// </summary>
    public static class WindowController
    {
        // The required substring that must be present in the game window's title for identification.
        private const string GameWindowTitleSubstring = "Counter-Strike 2";

        /// <summary>
        /// Attempts to find the primary window handle of the "cs2" process.
        /// It verifies the window's title contains the required substring.
        /// </summary>
        /// <returns>The IntPtr handle of the Counter-Strike 2 game window, or IntPtr.Zero if not found or the title doesn't match.</returns>
        public static IntPtr FindGameWindow()
        {
            // Find the first process named "cs2" that has a main window handle.
            var cs2Process = Process.GetProcessesByName("cs2")
                .FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);

            if (cs2Process == null)
            {
                return IntPtr.Zero; // Process not found
            }

            IntPtr hWnd = cs2Process.MainWindowHandle;
            string currentTitle = GetWindowTitle(hWnd);

            // Check if the window title contains the required substring for confirmation.
            if (currentTitle.Contains(GameWindowTitleSubstring, StringComparison.OrdinalIgnoreCase))
            {
                return hWnd;
            }

            return IntPtr.Zero; // Found process but title doesn't match
        }

        /// <summary>
        /// Retrieves the title text of the specified window handle.
        /// </summary>
        /// <param name="hWnd">The handle to the window.</param>
        /// <returns>The window title as a string, or an empty string if retrieval fails.</returns>
        public static string GetWindowTitle(IntPtr hWnd)
        {
            // Maximum capacity for the window title string buffer.
            const int MAX_CAPACITY = 256;
            
            var titleBuilder = new StringBuilder(MAX_CAPACITY);

            // Call the native method and check if any characters were retrieved.
            if (NativeMethods.GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity) > 0)
            {
                return titleBuilder.ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// Checks if the game window is currently the active foreground window.
        /// If the window is running but not focused, it attempts to restore and focus it using robust API methods.
        /// </summary>
        /// <returns>True if the game window is running and is successfully set as the foreground window, otherwise false.</returns>
        public static bool IsGameWindowActiveAndFocused()
        {
            IntPtr gameWindowHandle = FindGameWindow();
            if (gameWindowHandle == IntPtr.Zero)
            {
                return false; // Game is not running
            }

            // Check if it is already the foreground window.
            if (NativeMethods.GetForegroundWindow() == gameWindowHandle)
            {
                return true; // Already focused
            }

            // --- Window Activation Logic ---
            var placement = new NativeMethods.WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            NativeMethods.GetWindowPlacement(gameWindowHandle, ref placement);

            // Restore if minimized (SW_SHOWMINIMIZED is 2).
            if (placement.showCmd == NativeMethods.SW_SHOWMINIMIZED)
            {
                NativeMethods.ShowWindow(gameWindowHandle, NativeMethods.SW_RESTORE);
                Thread.Sleep(100); // Give the system time to process the restore command.
            }

            // Use AttachThreadInput to reliably set foreground window across different threads.
            uint currentThreadId = NativeMethods.GetCurrentThreadId();
            uint foregroundThreadId = NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), out _);

            NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, true);
            try
            {
                NativeMethods.BringWindowToTop(gameWindowHandle);
                NativeMethods.SetForegroundWindow(gameWindowHandle);
            }
            finally
            {
                // Always detach the thread input connection.
                NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }

            Thread.Sleep(50); // Wait a short time for focus to switch.
            
            return NativeMethods.GetForegroundWindow() == gameWindowHandle; // Verify focus status.
        }

        /// <summary>
        /// Performs a left mouse button click at the specified absolute screen coordinates.
        /// </summary>
        /// <param name="x">The x-coordinate of the screen position.</param>
        /// <param name="y">The y-coordinate of the screen position.</param>
        public static void ClickAtPoint(int x, int y)
        {
            NativeMethods.SetCursorPos(x, y);

            // Simulate mouse down and mouse up for a left click.
            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN | NativeMethods.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        /// <summary>
        /// Performs a left mouse button click at the specified screen Point.
        /// </summary>
        /// <param name="point">The screen coordinates for the click.</param>
        public static void ClickAtPoint(Point point)
        {
            // Set cursor position using the framework's Cursor class for convenience.
            Cursor.Position = point;

            // Simulate mouse down and mouse up for a left click.
            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN | NativeMethods.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        /// <summary>
        /// Sends a simulated Alt+F4 key combination to the specified window handle.
        /// This is often used to close or hide the target window gracefully.
        /// </summary>
        /// <param name="hWnd">The handle to the window to target.</param>
        public static void HideWindowWithAltF4(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;

            // Step 1: Forcefully gain focus for reliable key event delivery.
            if (NativeMethods.IsIconic(hWnd))
            {
                NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
            }

            NativeMethods.BringWindowToTop(hWnd);
            Thread.Sleep(100);

            // Alt-key focus trick: Press Alt, set foreground, release Alt.
            NativeMethods.keybd_event(NativeMethods.VK_MENU, 0, 0, 0); // Alt Down
            
            NativeMethods.SetForegroundWindow(hWnd);
            
            NativeMethods.keybd_event(NativeMethods.VK_MENU, 0, NativeMethods.KEYEVENTF_KEYUP, 0); // Alt Up
            Thread.Sleep(150);
            
            NativeMethods.SetForegroundWindow(hWnd); // Second attempt to ensure focus
            Thread.Sleep(100);

            // Step 2: Send Alt+F4 combination (Down F4, Up F4, Up Alt).
            NativeMethods.keybd_event(NativeMethods.VK_MENU, 0, 0, 0); // Alt Down
            Thread.Sleep(50);
            
            NativeMethods.keybd_event(NativeMethods.VK_F4, 0, 0, 0); // F4 Down
            Thread.Sleep(50);
            
            NativeMethods.keybd_event(NativeMethods.VK_F4, 0, NativeMethods.KEYEVENTF_KEYUP, 0); // F4 Up
            Thread.Sleep(50);
            
            NativeMethods.keybd_event(NativeMethods.VK_MENU, 0, NativeMethods.KEYEVENTF_KEYUP, 0); // Alt Up
        }

        /// <summary>
        /// Iterates through all windows on the desktop to find a window whose title contains the given substring.
        /// </summary>
        /// <param name="titlePart">The substring to search for in window titles.</param>
        /// <returns>The IntPtr handle of the first matching window, or IntPtr.Zero if none is found.</returns>
        public static IntPtr FindWindowByTitle(string titlePart)
        {
            // Start searching from the desktop window's first child.
            IntPtr hWnd = NativeMethods.FindWindow(null, null);

            while (hWnd != IntPtr.Zero)
            {
                var sb = new StringBuilder(256);
                if (NativeMethods.GetWindowText(hWnd, sb, sb.Capacity) > 0)
                {
                    string windowTitle = sb.ToString();
                    // Perform case-insensitive search for the title substring.
                    if (windowTitle.IndexOf(titlePart, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return hWnd;
                    }
                }

                // Move to the next window in the Z-order.
                hWnd = NativeMethods.GetWindow(hWnd, NativeMethods.GW_HWNDNEXT);
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Directly finds a window by its class name and/or window name (exact title match).
        /// </summary>
        /// <param name="className">The class name of the window (can be null).</param>
        /// <param name="windowName">The window name/title (can be null).</param>
        /// <returns>The IntPtr handle of the matching window, or IntPtr.Zero if not found.</returns>
        public static IntPtr FindWindowDirect(string className, string windowName)
        {
            return NativeMethods.FindWindow(className, windowName);
        }

        /// <summary>
        /// Blocks the calling thread until a window with the specified title part is found.
        /// Once found, it restores the window (if minimized) and brings it to the foreground.
        /// </summary>
        /// <param name="titlePart">The title substring to wait for.</param>
        /// <param name="checkInterval">The interval (in milliseconds) between checks.</param>
        /// <returns>The IntPtr handle of the found and activated window.</returns>
        public static IntPtr WaitForWindowByTitle(string titlePart, int checkInterval = 300)
        {
            while (true)
            {
                IntPtr hWnd = FindWindowByTitle(titlePart);
                if (hWnd != IntPtr.Zero)
                {
                    // Check and restore if window is minimized.
                    if (NativeMethods.IsIconic(hWnd))
                        NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);

                    // Bring to foreground.
                    NativeMethods.SetForegroundWindow(hWnd);
                    
                    return hWnd;
                }

                // Pause before checking again.
                Thread.Sleep(checkInterval);
            }
        }

        /// <summary>
        /// Blocks the calling thread until a window with the specified class name is found.
        /// Once found, it restores the window (if minimized) and brings it to the foreground.
        /// </summary>
        /// <param name="className">The class name to wait for.</param>
        /// <param name="checkInterval">The interval (in milliseconds) between checks.</param>
        /// <returns>The IntPtr handle of the found and activated window.</returns>
        public static IntPtr WaitForWindowByClassName(string className, int checkInterval = 300)
        {
            while (true)
            {
                IntPtr hWnd = NativeMethods.FindWindow(className, null);
                if (hWnd != IntPtr.Zero)
                {
                    if (NativeMethods.IsIconic(hWnd))
                    {
                        NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                    }

                    NativeMethods.SetForegroundWindow(hWnd);
                    
                    return hWnd;
                }

                // Pause before checking again.
                Thread.Sleep(checkInterval);
            }
        }

        /// <summary>
        /// Checks if the main "Steam" client window is currently the foreground (active) window.
        /// </summary>
        /// <returns>True if Steam is active, otherwise false.</returns>
        public static bool IsSteamActive()
        {
            IntPtr fg = NativeMethods.GetForegroundWindow();
            // Find the Steam window by its exact title "Steam".
            IntPtr steamHwnd = NativeMethods.FindWindow(null, "Steam");
            return fg == steamHwnd;
        }

        /// <summary>
        /// Ensures the specified window (found by exact title) is brought to the foreground.
        /// It uses the "Alt key trick" for a more robust focus transfer.
        /// </summary>
        /// <param name="title">The exact window title to focus.</param>
        public static void EnsureWindowFocusByTitle(string title)
        {
            // Find window by exact title.
            IntPtr hWnd = NativeMethods.FindWindow(null, title);

            if (hWnd != IntPtr.Zero)
            {
                // Restore if minimized.
                if (NativeMethods.IsIconic(hWnd))
                {
                    NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                }

                NativeMethods.BringWindowToTop(hWnd);
                Thread.Sleep(100);

                // Use the "Alt key trick" to reliably steal focus from other applications.
                NativeMethods.keybd_event(NativeMethods.VK_MENU, 0, 0, 0); // Alt Down
                NativeMethods.SetForegroundWindow(hWnd);
                NativeMethods.keybd_event(NativeMethods.VK_MENU, 0, NativeMethods.KEYEVENTF_KEYUP, 0); // Alt Up
                Thread.Sleep(150);
            }
        }

        /// <summary>
        /// Finds the main window handle of the "cs2" process and checks if its title contains the specified substring.
        /// This is similar to FindGameWindow but uses a custom substring.
        /// </summary>
        /// <param name="titleSubstring">The substring to look for in the window title.</param>
        /// <returns>The IntPtr handle of the matching window, or IntPtr.Zero.</returns>
        public static IntPtr FindWindowByTitleSubstring(string titleSubstring)
        {
            // Find the first process named "cs2" that has a main window handle.
            var cs2Process = Process.GetProcessesByName("cs2")
                .FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);

            if (cs2Process == null)
            {
                return IntPtr.Zero;
            }

            IntPtr hWnd = cs2Process.MainWindowHandle;
            // Use the wrapper method GetWindowTitle to safely retrieve the title.
            string currentTitle = GetWindowTitle(hWnd);

            // Check if the retrieved title contains the required substring.
            if (currentTitle.Contains(titleSubstring, StringComparison.OrdinalIgnoreCase))
            {
                return hWnd;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Retrieves the Parent Process ID (PPID) for a given child process ID (PID)
        /// by iterating through the system process snapshot (using Toolhelp32Snapshot).
        /// </summary>
        /// <param name="id">The Process ID of the child process (e.g., cs2.exe).</param>
        /// <returns>The Parent Process ID (PPID), or 0 if the child process is not found or snapshot creation fails.</returns>
        public static int GetParentProcessId(int id)
        {
            // 1. Take a snapshot of all processes in the system.
            IntPtr hSnapshot = NativeMethods.CreateToolhelp32Snapshot(
                NativeMethods.TH32CS_SNAPPROCESS,
                0 // 0 means all processes
            );

            if (hSnapshot == IntPtr.Zero)
            {
                return 0; // Failed to create snapshot
            }

            // Initialize the structure used for process iteration.
            NativeMethods.PROCESSENTRY32 pe32 = new NativeMethods.PROCESSENTRY32();
            pe32.dwSize = (uint)Marshal.SizeOf(pe32);

            try
            {
                // 2. Iterate through all processes.
                if (NativeMethods.Process32First(hSnapshot, ref pe32))
                {
                    do
                    {
                        // 3. Check if this process entry matches the target child PID.
                        if (pe32.th32ProcessID == id)
                        {
                            // 4. Return the Parent Process ID (PPID).
                            return (int)pe32.th32ParentProcessID;
                        }
                    } while (NativeMethods.Process32Next(hSnapshot, ref pe32));
                }
            }
            finally
            {
                // 5. Clean up the snapshot handle.
                NativeMethods.CloseHandle(hSnapshot);
            }

            return 0; // Not found
        }
    }
}