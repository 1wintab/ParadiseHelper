using System;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ParadiseHelper.Tools.WinAPI
{
    /// <summary>
    /// This internal class holds all P/Invoke declarations, structures, and constants
    /// required for interacting with the native Windows API (WinAPI).
    /// It acts as a single source of truth for all low-level platform interactions.
    /// </summary>
    public static class NativeMethods
    {
        #region Delegates

        /// <summary>
        /// Delegate for the keyboard hook procedure.
        /// </summary>
        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        #endregion // Delegates

        #region Structures

        /// <summary>
        /// Defines the coordinates of a rectangle.
        /// Used for window positions.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>
        /// Contains information about the placement of a window on the screen.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public RECT rcNormalPosition;
        }

        /// <summary>
        /// Contains information about a process in a system snapshot.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        /// <summary>
        /// Structure representing information about a simulated mouse event.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        /// <summary>
        /// Structure containing information about a simulated input event.
        /// Used by the SendInput function.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            /// <summary>
            /// The type of the input event.
            /// </summary>
            public int type;

            /// <summary>
            /// The structure that holds the event details.
            /// Note: This is a union in C++, but we only use MOUSEINPUT here.
            /// For keyboard, a different structure would be needed.
            /// </summary>
            public MOUSEINPUT mi;
        }

        #endregion // Structures

        #region Constants

        // --- Window Position Constants (for SetWindowPos) ---

        /// <summary>
        /// Handle for placing the window above all non-topmost windows.
        /// </summary>
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        /// <summary>
        /// Retains the current position (ignores X and Y parameters).
        /// </summary>
        public const uint SWP_NOMOVE = 0x0002;

        /// <summary>
        /// Retains the current size (ignores cx and cy parameters).
        /// </summary>
        public const uint SWP_NOSIZE = 0x0001;

        // --- Constants for SendMessage (WM_VSCROLL) ---

        /// <summary>
        /// Vertical scroll notification message.
        /// </summary>
        public const uint WM_VSCROLL = 0x0115;

        /// <summary>
        /// Scroll command to move the scroll bar to the bottom position.
        /// </summary>
        public const int SB_BOTTOM = 7;

        // --- Keyboard Hook Constants ---

        /// <summary>
        /// Constant for a low-level keyboard hook.
        /// </summary>
        public const int WH_KEYBOARD_LL = 13;

        /// <summary>
        /// Constant for a key down event.
        /// </summary>
        public const int WM_KEYDOWN = 0x0100;

        // --- Process Snapshot Constants ---

        /// <summary>
        /// Used with CreateToolhelp32Snapshot to include all processes in the snapshot.
        /// </summary>
        public const uint TH32CS_SNAPPROCESS = 0x00000002;

        // --- Window Show Command Constants (for ShowWindow) ---

        /// <summary>
        /// Activates and displays the window. If the window is minimized or maximized, it is restored to its original size and position.
        /// </summary>
        public const int SW_RESTORE = 9;

        /// <summary>
        /// Activates the window and displays it as a minimized window.
        /// </summary>
        public const int SW_SHOWMINIMIZED = 2;

        // --- Window Enumeration Constants (for GetWindow) ---

        /// <summary>
        /// Retrieves the next window in the Z-order.
        /// </summary>
        public const uint GW_HWNDNEXT = 2;

        // --- Mouse Input Constants ---

        /// <summary>
        /// Input type constant for mouse events.
        /// </summary>
        public const int INPUT_MOUSE = 0;

        /// <summary>
        /// Flag to specify mouse movement.
        /// </summary>
        public const uint MOUSEEVENTF_MOVE = 0x0001;

        /// <summary>
        /// Specifies a left button down.
        /// </summary>
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;

        /// <summary>
        /// Specifies a left button up.
        /// </summary>
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;

        /// <summary>
        /// Flag to specify coordinates are absolute (scaled to 0-65535).
        /// </summary>
        public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        // --- Keyboard Input Constants (for keybd_event) ---

        /// <summary>
        /// Virtual-key code for the ALT key.
        /// </summary>
        public const byte VK_MENU = 0x12;

        /// <summary>
        /// Virtual-key code for the F4 key.
        /// </summary>
        public const byte VK_F4 = 0x73;

        /// <summary>
        /// If specified, the key is being released. If not specified, the key is being pressed.
        /// </summary>
        public const uint KEYEVENTF_KEYUP = 0x0002;

        #endregion // Constants

        #region user32.dll Imports (Windowing)

        /// <summary>
        /// Hides the text input caret (cursor) from a specified window's edit control.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern bool HideCaret(IntPtr hWnd);

        /// <summary>
        /// Sends the specified message to a window or windows.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Changes the position, size, and Z-order of a child, pop-up, or top-level window.
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        /// <summary>
        /// Retrieves a handle to the top-level window whose class name and window name match the specified strings.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// Copies the text of the specified window's title bar (if it has one) into a buffer.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// Changes the text of the specified window's title bar (if it has one).
        /// </summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetWindowText(IntPtr hWnd, string lpString);

        /// <summary>
        /// Retrieves the dimensions of the bounding rectangle of the specified window.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// Retrieves the handle to a window that has the relationship (Z-Order or owner) to the specified window.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        /// <summary>
        /// Retrieves a handle to the foreground window (the window with which the user is currently working).
        /// </summary>
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Brings the specified window to the foreground and activates it.
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Brings the specified window to the top of the Z order.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        /// <summary>
        /// Sets the specified window's show state.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="nCmdShow">Controls how the window is to be shown.</param>
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// Determines whether the specified window is minimized (iconic).
        /// </summary>
        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        /// <summary>
        /// Retrieves the identifier of the thread that created the specified window and, optionally, the identifier of the process that created the window.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// Attaches or detaches the input processing mechanism of one thread to that of another thread.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        /// <summary>
        /// Retrieves the show state and the restored, minimized, and maximized positions of the specified window.
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        #endregion // user32.dll Imports (Windowing)

        #region user32.dll Imports (Hooks)

        /// <summary>
        /// Installs an application-defined hook procedure into a hook chain.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        /// <summary>
        /// Removes a hook procedure installed in a hook chain by SetWindowsHookEx.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /// <summary>
        /// Passes the hook information to the next hook procedure in the current hook chain.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        #endregion // user32.dll Imports (Hooks)

        #region user32.dll Imports (Input)

        /// <summary>
        /// Synthesizes mouse motions and clicks.
        /// (Overload for single input struct)
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

        /// <summary>
        /// Synthesizes mouse motions and clicks.
        /// (Overload for an array of input structs)
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        /// <summary>
        /// Converts client-area coordinates to absolute screen coordinates.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref System.Drawing.Point lpPoint);

        /// <summary>
        /// Sets the cursor to the specified screen coordinates.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        /// <summary>
        /// Synthesizes mouse motions and clicks.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        /// <summary>
        /// Synthesizes a keystroke.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        #endregion // user32.dll Imports (Input)

        #region kernel32.dll Imports

        /// <summary>
        /// Retrieves a module handle for the specified module.
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// Retrieves the thread identifier of the calling thread.
        /// </summary>
        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        /// <summary>
        /// Takes a snapshot of the specified processes, as well as the heaps, modules, and threads used by these processes.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        /// <summary>
        /// Retrieves information about the first process encountered in a system snapshot.
        /// </summary>
        [DllImport("kernel32.dll")]
        public static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        /// <summary>
        /// Retrieves information about the next process recorded in a system snapshot.
        /// </summary>
        [DllImport("kernel32.dll")]
        public static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        #endregion // kernel32.dll Imports
    }
}