using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using OpenCvSharp.Extensions;
using ParadiseHelper.AI.Core;
using ParadiseHelper.Tools.WinAPI;
using ParadiseHelper.AI.Core.Settings;
using ParadiseHelper.AI.Orientation.Radar;
using ParadiseHelper.AI.ClickerAutoRunGame;
using ParadiseHelper.AI.Orientation.Map.Navmesh;

namespace UI.AI_UI
{
    /// <summary>
    /// A debugging window that displays real-time processed frames, 
    /// detection results, and various AI state information.
    /// </summary>
    public partial class WindowCheckVision : SmartForm
    {
        // --- Fields & Constants ---

        // Handle for the installed low-level keyboard hook.
        private IntPtr _keyboardHookHandle = IntPtr.Zero;

        // Delegate instance to prevent garbage collection by the CLR.
        private NativeMethods.HookProc _hookDelegate;

        // The main AI processing and state management core.
        private AICore _aiCore;

        // Facade for accessing AI state data safely from the UI thread.
        private AIStateUIFacade _stateFacade;

        // Timer responsible for triggering UI redraws (OnPaint).
        private readonly System.Windows.Forms.Timer _renderTimer;

        // Font used for drawing all debug text overlays.
        private readonly Font _font;

        /// <summary>
        /// Pen used for drawing detected objects (e.g., enemy models).
        /// </summary>
        public readonly Pen DetectionBoxPen;

        /// <summary>
        /// Pen used for drawing the player's position on the radar.
        /// </summary>
        public readonly Pen PlayerBoxPen;

        /// <summary>
        /// Pen used for drawing the player's direction arrow.
        /// </summary>
        public readonly Pen DirectionArrowPen;

        /// <summary>
        /// Pen used for drawing the recognized map bounding box.
        /// </summary>
        public readonly Pen MapBoxPen;

        /// <summary>
        /// Pen used for drawing the dead player marker on the radar.
        /// </summary>
        public readonly Pen DeadPlayerBoxPen;

        /// <summary>
        /// Brush for drawing standard navmesh nodes.
        /// </summary>
        public readonly Brush NavmeshNodeBrush;

        /// <summary>
        /// Brush for drawing the navmesh node closest to the player.
        /// </summary>
        public readonly Brush NearestNodeBrush;

        /// <summary>
        /// Pen for drawing the edges (connections) between navmesh nodes.
        /// </summary>
        public readonly Pen NavmeshEdgePen;

        // Pixel size for drawing standard navmesh nodes.
        private readonly int sizeNavmeshNode = 2;

        // Pixel size for drawing the nearest/target navmesh node.
        private readonly int sizeNavmeshNearest = 3;

        // Pixel thickness for navmesh connection lines.
        private readonly int navmeshEdgeThickness = 1;

        /// <summary>
        /// Brush for highlighting the previously visited path node.
        /// </summary>
        public readonly Brush LastNodeBrush;

        /// <summary>
        /// Brush for highlighting the next target node in the planned path.
        /// </summary>
        public readonly Brush NextNodeBrush;

        /// <summary>
        /// Pen for drawing the AI's recorded path history.
        /// </summary>
        public readonly Pen PathHistoryPen;

        // Toggled by F1. Controls drawing of the main frame and detections.
        private volatile bool _isDrawingEnabled = true;

        // Target interval for a fast (~60 FPS) update rate.
        private const int FAST_RENDER_INTERVAL = 16;

        // Target interval for a slow (~20 FPS) update rate to reduce resource usage.
        private const int SLOW_RENDER_INTERVAL = 55;

        // --- Constructor ---

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowCheckVision"/> form.
        /// Sets up controls, initializes drawing resources, and positions the window.
        /// </summary>
        public WindowCheckVision()
        {
            _font = new Font("Consolas", 8, FontStyle.Bold);

            // --- Detection Pen Initialization ---
            DetectionBoxPen = new Pen(Color.Cyan, 2);
            PlayerBoxPen = new Pen(Color.Green, 1);
            DirectionArrowPen = new Pen(Color.White, 1);
            MapBoxPen = new Pen(Color.Yellow, 1);
            DeadPlayerBoxPen = new Pen(Color.Red, 1);

            // --- Navmesh Pen/Brush Initialization ---
            NavmeshNodeBrush = Brushes.Red;
            NearestNodeBrush = Brushes.Green;
            NavmeshEdgePen = new Pen(Color.White, navmeshEdgeThickness);

            // --- Path Pen/Brush Initialization ---
            LastNodeBrush = Brushes.Orange;
            NextNodeBrush = Brushes.LawnGreen;
            PathHistoryPen = new Pen(Color.GreenYellow, navmeshEdgeThickness);

            Text = "Real-Time Detection";
            Size = new System.Drawing.Size(720, 520);
            Width = 720;
            Height = 520;
            DoubleBuffered = true; // Reduces flickering during redraws.
            BackColor = Color.Black;

            // --- StartPosition Configuration ---
            this.StartPosition = FormStartPosition.Manual;
            int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
            int formX = screenWidth - this.Width;
            int formY = 0;
            this.Location = new Point(formX, formY);

            // Configure and initialize the rendering timer.
            _renderTimer = new System.Windows.Forms.Timer { Interval = FAST_RENDER_INTERVAL };
            _renderTimer.Tick += (s, args) => Invalidate(); // Force redraw on every tick.

            this.TopMost = true; // Keep the form on top of other windows.

            // Initialize the delegate for the keyboard hook callback.
            _hookDelegate = new NativeMethods.HookProc(KeyboardHookCallback);
        }

        // --- Form Lifecycle Overrides ---

        /// <summary>
        /// Overrides the form's OnLoad event to initialize the AI core and start services.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnLoad(EventArgs e)
        {
            // This call was not present in the original code, but is good practice
            // to ensure the form window is created before trying to set its position.
            base.OnLoad(e);

            // Use the centralized NativeMethods class to set window position
            NativeMethods.SetWindowPos(
                this.Handle,
                NativeMethods.HWND_TOPMOST, // Use constant from NativeMethods
                this.Location.X,
                this.Location.Y,
                this.Width,
                this.Height,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE); // Use constants


            try
            {
                // Initialize and start the main AI processing core.
                _aiCore = new AICore();
                _stateFacade = _aiCore.StateFacade;
                _aiCore.Start();
                _renderTimer.Start();
                SetHook(); // Activate the keyboard hook.
            }
            catch (Exception ex)
            {
                // This catch block handles initialization failures (e.g., OBS not found).
                // It's triggered from AIForm's launch process.
                MessageBox.Show(
                                    "AI Bot Initialization Error:\n" +
                                    "Failed to connect to OBS or Virtual Camera. " +
                                    "Please ensure OBS is running and configured according to the guide.\n\n" +
                                    $"Details: {ex.Message}",
                                    "Error",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error
                  );
                this.Close();
            }
        }

        /// <summary>
        /// Overrides the form's OnPaint event to draw the main video feed and all debug overlays.
        /// </summary>
        /// <param name="e">Paint event arguments, provides the Graphics object.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            // This top-level try-catch prevents the application from crashing if a runtime
            // error occurs *after* initialization.
            try
            {
                base.OnPaint(e);
                if (_aiCore == null || _aiCore.StateFacade == null) return;

                // Only draw the main frame and detection boxes if drawing is enabled (F1 toggle).
                if (_isDrawingEnabled)
                {
                    // Get and display the latest captured/processed game frame.
                    using (var latestFrame = _aiCore.StateFacade.GetLatestFrameForUI())
                    {
                        if (latestFrame != null)
                        {
                            // Stretch the frame to fit the client area.
                            e.Graphics.DrawImage(latestFrame, this.ClientRectangle);
                        }
                    }

                    // Draw detection boxes on top of the frame.
                    var latestResults = _aiCore.StateFacade.GetLatestResultsForUI();

                    if (latestResults != null)
                    {
                        foreach (var r in latestResults)
                        {
                            // Scale the detection coordinates from model size to window size.
                            var box = ScaleBoxToWindow(r.Box);
                            var roundedBox = Rectangle.Round(box);
                            if (roundedBox.Width > 0 && roundedBox.Height > 0)
                            {
                                var color = r.Label.Contains("head") ?
                                    Brushes.Red : Brushes.Orange;

                                e.Graphics.DrawRectangle(DetectionBoxPen, roundedBox);
                                e.Graphics.DrawString($"{r.Label} {r.Confidence:P0}", _font, color, box.Location);
                            }
                        }
                    }
                    DebugInfo(e);
                }
                else
                {
                    // Still show debug info even if the main frame drawing is disabled.
                    DebugInfo(e);
                }
            }
            catch (Exception ex)
            {
                // --- Catch block for runtime rendering errors ---
                _renderTimer?.Stop(); // Stop trying to re-render
                _aiCore?.Stop(); // Stop the AI core

                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"AI Bot OnPaint crash: {ex.Message}");
                
                // Show a user-friendly error
                MessageBox.Show(
                    "A runtime error occurred in the AI Bot.\n" +
                    "This can happen if OBS or the virtual camera was closed or disconnected.\n\n" +
                    "The AI Bot window will now close.",
                    "AI Bot Runtime Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                this.Close(); // Safely close this form.
            }
        }

        /// <summary>
        /// Overrides the form's OnFormClosing event to ensure all resources are properly disposed.
        /// </summary>
        /// <param name="e">Form closing event arguments.</param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 1. Негайно зупиняємо всі джерела подій, щоб запобігти гонитві.

            // Зупиняємо рендерінг, щоб OnPaint більше не викликався.
            _renderTimer?.Stop();

            // Видаляємо хук клавіатури, щоб KeyboardHookCallback більше не викликався.
            UnHook();

            // 2. Сигналізуємо AI-ядру про зупинку (якщо воно має фонові потоки).
            _aiCore?.Stop();

            // 3. Тепер, коли всі активності зупинені, безпечно звільняємо ресурси.

            // Звільняємо AI-ядро та його фасад.
            (_aiCore as IDisposable)?.Dispose();
            _aiCore = null; // Допомагаємо збирачу сміття
            _stateFacade = null;

            // Звільняємо таймер.
            _renderTimer?.Dispose();

            // Звільняємо всі GDI+ ресурси (пензлі, шрифти).
            _font?.Dispose();
            DirectionArrowPen?.Dispose();
            PlayerBoxPen?.Dispose();
            MapBoxPen?.Dispose();
            DetectionBoxPen?.Dispose();
            DeadPlayerBoxPen?.Dispose();
            PathHistoryPen?.Dispose();
            NavmeshEdgePen?.Dispose();

            // 4. Обнуляємо делегат (про всяк випадок).
            _hookDelegate = null;

            // 5. Викликаємо базовий метод.
            base.OnFormClosing(e);
        }

        // --- Private Helper Methods ---

        /// <summary>
        /// Sets up the low-level keyboard hook to capture global key presses.
        /// </summary>
        private void SetHook()
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                // Call the function from our centralized NativeMethods class
                _keyboardHookHandle = NativeMethods.SetWindowsHookEx(
                    NativeMethods.WH_KEYBOARD_LL, // Use constant from NativeMethods
                    _hookDelegate,
                    NativeMethods.GetModuleHandle(curModule.ModuleName),
                    0);
            }
        }

        /// <summary>
        /// Removes the low-level keyboard hook.
        /// </summary>
        private void UnHook()
        {
            // Call the function from our centralized NativeMethods class
            NativeMethods.UnhookWindowsHookEx(_keyboardHookHandle);
        }

        /// <summary>
        /// Callback method executed when a key event is detected by the global hook.
        /// </summary>
        /// <param name="nCode">A code the hook procedure uses to determine how to process the message.</param>
        /// <param name="wParam">The identifier of the keyboard message (e.g., WM_KEYDOWN).</param>
        /// <param name="lParam">A pointer to a KBDLLHOOKSTRUCT structure.</param>
        /// <returns>A handle to the next hook procedure in the chain.</returns>
        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // Use constant from NativeMethods
            if (nCode >= 0 && wParam == (IntPtr)NativeMethods.WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                // Use BeginInvoke to safely update UI elements from the non-UI hook thread.
                if (key == Keys.Pause)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        // Toggle the visibility of the detailed debug information panel.
                        _aiCore?.StateFacade.ToggleFullInfo();
                        this.Invalidate();
                    });
                }
                else if (key == Keys.F1)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        // Toggle rendering of the main image and object boxes.
                        _isDrawingEnabled = !_isDrawingEnabled;
                        this.Invalidate();
                    });
                }
                else if (key == Keys.F2)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        // Toggle between fast (60 FPS) and slow (20 FPS) rendering intervals.
                        if (_renderTimer.Interval == FAST_RENDER_INTERVAL)
                        {
                            _renderTimer.Interval = SLOW_RENDER_INTERVAL;
                            this.Text = "Real-Time Detection (Low FPS)";
                        }
                        else
                        {
                            _renderTimer.Interval = FAST_RENDER_INTERVAL;
                            this.Text = "Real-Time Detection (High FPS)";
                        }
                    });
                }
            }
            // Pass the control to the next hook in the chain.
            return NativeMethods.CallNextHookEx(_keyboardHookHandle, nCode, wParam, lParam);
        }

        /// <summary>
        /// Draws all supplementary debug information, including mini-maps, 
        /// weapon images, and navigational data.
        /// </summary>
        /// <param name="e">Paint event arguments.</param>
        private void DebugInfo(PaintEventArgs e)
        {
            // Display the textual debug info panel (top-left).
            DrawTextInfo(e.Graphics);

            // Display the processed radar image (mini-map).
            using (var processedRadarImage = _aiCore.StateFacade.GetLatestProcessedRadarImageForUI())
            {
                if (processedRadarImage != null
                    && processedRadarImage.Width > 0
                    && processedRadarImage.Height > 0)
                {
                    // Convert OpenCvSharp.Mat to System.Drawing.Bitmap for GDI+ rendering.
                    using var processedBitmap = processedRadarImage.ToBitmap();

                    int radarWidth = processedBitmap.Width;
                    int radarHeight = processedBitmap.Height;

                    // Calculate position in the top-right corner.
                    int radarX = this.ClientSize.Width - radarWidth - 5;
                    int radarY = 5;

                    if (radarX >= 0 && radarY >= 0 && radarWidth > 0 && radarHeight > 0)
                    {
                        e.Graphics.DrawImage(processedBitmap, new Point(radarX, radarY));
                    }

                    // Get and draw supplementary radar data (map box, player location, directions).
                    var playerLocation = _aiCore.StateFacade.GetPlayerLocationOnRadar();
                    var mapBoundingBox = _aiCore.StateFacade.GetLatestMapBoundingBox();
                    var deadPlayerLocation = _aiCore.StateFacade.GetDeadPlayerLocationOnRadar();

                    // Draw Dead Player Marker
                    if (deadPlayerLocation.HasValue && _aiCore.StateFacade.GetLatestMapName() != "Unknown")
                    {
                        int templateWidth = RadarCapture.GetTemplateWidth();
                        int templateHeight = RadarCapture.GetTemplateHeight();
                        int deadX = deadPlayerLocation.Value.X + radarX;
                        int deadY = deadPlayerLocation.Value.Y + radarY;
                        e.Graphics.DrawRectangle(DeadPlayerBoxPen, deadX, deadY, templateWidth, templateHeight);
                    }

                    // Draw elements relative to the map bounding box
                    if (mapBoundingBox.HasValue && _aiCore.StateFacade.GetLatestMapName() != "Unknown")
                    {
                        // Draw the bounding box for the recognized map area on the radar.
                        e.Graphics.DrawRectangle(
                            MapBoxPen,
                            new Rectangle(
                                mapBoundingBox.Value.X + radarX,
                                mapBoundingBox.Value.Y + radarY,
                                mapBoundingBox.Value.Width,
                                mapBoundingBox.Value.Height)
                        );

                        // Draw Player detection
                        if (_aiCore.StateFacade.IsPlayerFoundOnRadar() && playerLocation.HasValue)
                        {
                            // Draw the player detection box.
                            int templateWidth = RadarCapture.GetTemplateWidth();
                            int templateHeight = RadarCapture.GetTemplateHeight();
                            int x = playerLocation.Value.X + radarX;
                            int y = playerLocation.Value.Y + radarY;
                            e.Graphics.DrawRectangle(PlayerBoxPen, x, y, templateWidth, templateHeight);

                            // Draw the player's direction arrow.
                            var playerDirection = _aiCore.StateFacade.GetPlayerDirectionOnRadar();
                            if (playerDirection.HasValue)
                            {
                                int centerX = x + templateWidth / 2;
                                int centerY = y + templateHeight / 2;

                                double angleRadians = Math.Atan2(playerDirection.Value.X, -playerDirection.Value.Y);
                                double arrowLength = templateWidth * 2.5;
                                int endX = centerX + (int)(arrowLength * Math.Sin(angleRadians));
                                int endY = centerY - (int)(arrowLength * Math.Cos(angleRadians));
                                e.Graphics.DrawLine(DirectionArrowPen, centerX, centerY, endX, endY);
                            }
                        }

                        // Draw Navmesh and Pathfinding Data
                        var navmesh = _aiCore.StateFacade.GetCurrentNavmeshForUI();
                        if (navmesh != null && navmesh.Nodes.Any())
                        {
                            var lastNode = _aiCore.StateFacade.GetLastNodeForUI();
                            var nextNode = _aiCore.StateFacade.GetNextNodeForUI();
                            var pathHistory = _aiCore.StateFacade.GetPathHistoryForUI();

                            // Draw all navmesh edges (connections between nodes).
                            foreach (var edge in navmesh.Edges)
                            {
                                Node node1 = navmesh.Nodes.FirstOrDefault(n => n.ID == edge.Node1ID);
                                Node node2 = navmesh.Nodes.FirstOrDefault(n => n.ID == edge.Node2ID);

                                if (node1 != null && node2 != null)
                                {
                                    // Scale coordinates to the radar window position.
                                    int x1 = (int)(mapBoundingBox.Value.X + radarX + node1.X);
                                    int y1 = (int)(mapBoundingBox.Value.Y + radarY + node1.Y);
                                    int x2 = (int)(mapBoundingBox.Value.X + radarX + node2.X);
                                    int y2 = (int)(mapBoundingBox.Value.Y + radarY + node2.Y);
                                    e.Graphics.DrawLine(NavmeshEdgePen, x1, y1, x2, y2);
                                }
                            }

                            // Draw the path history (the path the AI has followed)
                            if (pathHistory.Count > 1)
                            {
                                for (int i = 1; i < pathHistory.Count; i++)
                                {
                                    int x1 = (int)(mapBoundingBox.Value.X + radarX + pathHistory[i - 1].X);
                                    int y1 = (int)(mapBoundingBox.Value.Y + radarY + pathHistory[i - 1].Y);
                                    int x2 = (int)(mapBoundingBox.Value.X + radarX + pathHistory[i].X);
                                    int y2 = (int)(mapBoundingBox.Value.Y + radarY + pathHistory[i].Y);

                                    e.Graphics.DrawLine(PathHistoryPen, x1, y1, x2, y2);
                                }
                            }

                            // Draw all navmesh nodes (points).
                            foreach (var node in navmesh.Nodes)
                            {
                                int nodeScreenX = (int)(mapBoundingBox.Value.X + radarX + node.X);
                                int nodeScreenY = (int)(mapBoundingBox.Value.Y + radarY + node.Y);

                                e.Graphics.FillRectangle(
                                    NavmeshNodeBrush,
                                    nodeScreenX - sizeNavmeshNode / 2,
                                    nodeScreenY - sizeNavmeshNode / 2,
                                    sizeNavmeshNode,
                                    sizeNavmeshNode
                                );
                            }

                            // Highlight the last visited node.
                            if (lastNode != null)
                            {
                                int lastNodeX = (int)(mapBoundingBox.Value.X + radarX + lastNode.X);
                                int lastNodeY = (int)(mapBoundingBox.Value.Y + radarY + lastNode.Y);

                                e.Graphics.FillRectangle(
                                    LastNodeBrush,
                                    lastNodeX - sizeNavmeshNearest / 2,
                                    lastNodeY - sizeNavmeshNearest / 2,
                                    sizeNavmeshNearest,
                                    sizeNavmeshNearest
                                );
                            }

                            // Highlight the next target node.
                            if (nextNode != null)
                            {
                                int nextNodeX = (int)(mapBoundingBox.Value.X + radarX + nextNode.X);
                                int nextNodeY = (int)(mapBoundingBox.Value.Y + radarY + nextNode.Y);

                                e.Graphics.FillRectangle(
                                    NextNodeBrush,
                                    nextNodeX - sizeNavmeshNearest / 2,
                                    nextNodeY - sizeNavmeshNearest / 2,
                                    sizeNavmeshNearest,
                                    sizeNavmeshNearest
                                );
                            }
                        }
                    }
                }
            }

            // Display the processed weapon detection image below the radar.
            using (var processedWeaponImage = _aiCore.StateFacade.GetLatestProcessedWeaponImageForUI())
            {
                if (processedWeaponImage != null)
                {
                    using var processedBitmap = processedWeaponImage.ToBitmap();
                    int weaponX = this.ClientSize.Width - processedBitmap.Width - 5;
                    int weaponY = RadarCapture.RADAR_HEIGHT + 10;
                    e.Graphics.DrawImage(
                        processedBitmap,
                        new Point(weaponX, weaponY)
                    );
                }
            }

            // --- Display Debug Images for Game State Detection (Lobby/Buttons) ---

            // Display processed Lobby/Home Button image.
            using (var processedLobbyImage = _aiCore.StateFacade.GetLatestProcessedLobbyImage())
            {
                if (processedLobbyImage != null
                    && _aiCore.StateFacade.GetShowLobbyButtonDebugImage()
                    && !processedLobbyImage.IsDisposed)
                {
                    using (var lobbyBitmap = processedLobbyImage.ToBitmap())
                    {
                        int lobbyX = this.ClientSize.Width - lobbyBitmap.Width - 5;
                        int lobbyY = RadarCapture.RADAR_HEIGHT + 10 +
                                     SettingsDetector.WEAPON_NAME_HEIGHT + 10;
                        e.Graphics.DrawImage(
                            lobbyBitmap,
                            new Point(lobbyX, lobbyY)
                        );
                    }
                }
            }

            // Display processed Line Mode Map image.
            using (var processedLineModeMapImage = _aiCore.StateFacade.GetLatestProcessedLineModeMapImage())
            {
                if (processedLineModeMapImage != null
                    && _aiCore.StateFacade.GetShowLobbyButtonDebugImage()
                    && !processedLineModeMapImage.IsDisposed)
                {
                    using (var lineModeMapBitmap = processedLineModeMapImage.ToBitmap())
                    {
                        int lineModeMapX = this.ClientSize.Width - lineModeMapBitmap.Width - 5;
                        int lineModeMapY = RadarCapture.RADAR_HEIGHT + 10 +
                                           SettingsDetector.WEAPON_NAME_HEIGHT + 10 +
                                           SettingsDetector.HOME_BUTTON_HEIGHT + 10;
                        e.Graphics.DrawImage(
                            lineModeMapBitmap,
                            new Point(lineModeMapX, lineModeMapY)
                        );
                    }
                }
            }

            // Display processed Action Button image (e.g., Start/Cancel).
            using (var processedActionButtonImage = _aiCore.StateFacade.GetLatestProcessedActionButtonImage())
            {
                if (processedActionButtonImage != null
                    && _aiCore.StateFacade.GetShowLobbyButtonDebugImage()
                    && !processedActionButtonImage.IsDisposed)
                {
                    using (var actionButtonBitmap = processedActionButtonImage.ToBitmap())
                    {
                        int buttonX = this.ClientSize.Width - actionButtonBitmap.Width - 5;
                        int buttonY = RadarCapture.RADAR_HEIGHT + 10 +
                                      SettingsDetector.WEAPON_NAME_HEIGHT + 10 +
                                      SettingsDetector.HOME_BUTTON_HEIGHT + 10 +
                                      SettingsDetector.LINE_MODE_MAP_HEIGHT + 10;
                        e.Graphics.DrawImage(
                            actionButtonBitmap,
                            new Point(buttonX, buttonY)
                        );
                    }
                }
            }

            // Display processed Disconnection Label image.
            using (var processedDisconnectionLabelImage = _aiCore.StateFacade.GetLatestProcessedDisconnectionLabelImage())
            {
                if (processedDisconnectionLabelImage != null
                    && _aiCore.StateFacade.GetShowLobbyButtonDebugImage()
                    && !processedDisconnectionLabelImage.IsDisposed)
                {
                    using (var disconnectionLabelBitmap = processedDisconnectionLabelImage.ToBitmap())
                    {
                        int disconnectionLabelX = this.ClientSize.Width - disconnectionLabelBitmap.Width - 5;
                        int disconnectionLabelY = RadarCapture.RADAR_HEIGHT + 10 +
                                                  SettingsDetector.WEAPON_NAME_HEIGHT + 10 +
                                                  SettingsDetector.HOME_BUTTON_HEIGHT + 10 +
                                                  SettingsDetector.LINE_MODE_MAP_HEIGHT + 10 +
                                                  SettingsDetector.START_BUTTON_HEIGHT + 10;
                        e.Graphics.DrawImage(
                            disconnectionLabelBitmap,
                            new Point(disconnectionLabelX, disconnectionLabelY)
                        );
                    }
                }
            }

            // Display processed Match Is Ready Label image.
            using (var processedMatchIsReadyLabelImage = _aiCore.StateFacade.GetLatestProcessedMatchIsReadyLabelImage())
            {
                if (processedMatchIsReadyLabelImage != null
                    && _aiCore.StateFacade.GetShowLobbyButtonDebugImage()
                    && !processedMatchIsReadyLabelImage.IsDisposed)
                {
                    using (var disconnectionLabelBitmap = processedMatchIsReadyLabelImage.ToBitmap())
                    {
                        int matchIsReadyLabelX = this.ClientSize.Width - disconnectionLabelBitmap.Width - 5;
                        int matchIsReadyLabelY = RadarCapture.RADAR_HEIGHT + 10 +
                                                  SettingsDetector.WEAPON_NAME_HEIGHT + 10 +
                                                  SettingsDetector.HOME_BUTTON_HEIGHT + 10 +
                                                  SettingsDetector.LINE_MODE_MAP_HEIGHT + 10 +
                                                  SettingsDetector.START_BUTTON_HEIGHT + 10 +
                                                  SettingsDetector.DISCONNECTION_LABEL_HEIGHT + 10;
                        e.Graphics.DrawImage(
                            disconnectionLabelBitmap,
                            new Point(matchIsReadyLabelX, matchIsReadyLabelY)
                        );
                    }
                }
            }

            // Display processed Team Selection (TT) image.
            using (var processedTeamSelectionTTImage = _aiCore.StateFacade.GetLatestProcessedTeamSelectionTTImage())
            {
                if (processedTeamSelectionTTImage != null
                    && _aiCore.StateFacade.GetShowLobbyButtonDebugImage()
                    && !processedTeamSelectionTTImage.IsDisposed)
                {
                    using (var disconnectionLabelBitmap = processedTeamSelectionTTImage.ToBitmap())
                    {
                        int teamSelectionTT_X = this.ClientSize.Width - disconnectionLabelBitmap.Width - 5;
                        int teamSelectionTT_Y = RadarCapture.RADAR_HEIGHT + 10 +
                                                  SettingsDetector.WEAPON_NAME_HEIGHT + 10 +
                                                  SettingsDetector.HOME_BUTTON_HEIGHT + 10 +
                                                  SettingsDetector.LINE_MODE_MAP_HEIGHT + 10 +
                                                  SettingsDetector.START_BUTTON_HEIGHT + 10 +
                                                  SettingsDetector.DISCONNECTION_LABEL_HEIGHT + 10 +
                                                  SettingsDetector.MATCH_IS_READY_LABEL_HEIGHT + 10;
                        e.Graphics.DrawImage(
                            disconnectionLabelBitmap,
                            new Point(teamSelectionTT_X, teamSelectionTT_Y)
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Scales the coordinates of a detection box from the original 
        /// AI model's input size to the current window size.
        /// </summary>
        /// <param name="box">The detection box in model coordinates.</param>
        /// <returns>A new RectangleF scaled to fit the window's ClientSize.</returns>
        private RectangleF ScaleBoxToWindow(RectangleF box)
        {
            var scaleX = (float)this.ClientSize.Width / ModelSettings.Width;
            var scaleY = (float)this.ClientSize.Height / ModelSettings.Height;

            return new RectangleF(
                box.X * scaleX,
                box.Y * scaleY,
                box.Width * scaleX,
                box.Height * scaleY
            );
        }

        /// <summary>
        /// Draws the real-time textual debug information panel in the top-left corner.
        /// </summary>
        /// <param name="g">The Graphics object to draw on.</param>
        private void DrawTextInfo(Graphics g)
        {
            int yOffset = 10;
            const int lineHeight = 15;
            const int panelWidth = 220;

            // Get the required height dynamically to cover all visible text lines.
            int panelHeight = _aiCore.StateFacade.GetTextPanelHeight();

            // Draw a semi-transparent black background rectangle for the text panel.
            using (var semiTransparentBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 0)))
            {
                g.FillRectangle(
                    semiTransparentBrush,
                    new RectangleF(5, 5, panelWidth, panelHeight)
                );
            }

            // Draw FPS information (always visible).
            g.DrawString(_aiCore.StateFacade.GetFpsText(), _font, Brushes.Cyan, new PointF(10, yOffset));
            yOffset += lineHeight;

            // Draw Keyboard shortcut (always visible).
            g.DrawString("Press Ctrl + B to turn off", _font, Brushes.Yellow, new PointF(10, yOffset));
            yOffset += lineHeight;

            // Draw Keyboard shortcut (always visible).
            g.DrawString("Press F1 to hide image (more FPS)", _font, Brushes.Green, new PointF(10, yOffset));
            yOffset += lineHeight;

            // Draw detailed debug information only if the full info flag is set (toggled by Pause key).
            if (_aiCore.StateFacade.IsFullInfoVisible())
            {
                yOffset += lineHeight;
                g.DrawString(_aiCore.StateFacade.GetPlayerStatusText(), _font, Brushes.White, new PointF(10, yOffset));

                // Draw player coordinates and direction if the player is recognized on the map.
                if (_aiCore.StateFacade.IsPlayerOnMapWithBoxMargin())
                {
                    yOffset += lineHeight;
                    g.DrawString(_aiCore.StateFacade.GetPlayerLocationText(), _font, Brushes.White, new PointF(10, yOffset));
                    yOffset += lineHeight;
                    g.DrawString(_aiCore.StateFacade.GetPlayerDirectionText(), _font, Brushes.White, new PointF(10, yOffset));
                }

                // --- Radar Debug Info ---
                yOffset += lineHeight;
                yOffset += lineHeight;
                g.DrawString(_aiCore.StateFacade.GetRadarMatchValueText(), _font, Brushes.White, new PointF(10, yOffset));
                yOffset += lineHeight;
                g.DrawString(_aiCore.StateFacade.GetShowRadarDebugText(), _font, Brushes.White, new PointF(10, yOffset));
                yOffset += lineHeight;
                g.DrawString(_aiCore.StateFacade.GetDeathMatchValueText(), _font, Brushes.OrangeRed, new PointF(10, yOffset));

                // --- Map/Navmesh Debug Info ---
                yOffset += lineHeight;
                yOffset += lineHeight;
                g.DrawString(_aiCore.StateFacade.GetCurrentMapText(), _font, Brushes.White, new PointF(10, yOffset));
                yOffset += lineHeight;
                g.DrawString(_aiCore.StateFacade.GetMapMatchValueText(), _font, Brushes.White, new PointF(10, yOffset));

                // --- General AI State and Weapon Info ---
                yOffset += lineHeight;
                yOffset += lineHeight;
                g.DrawString(_aiCore.StateFacade.GetMovementDecisionText(), _font, Brushes.LawnGreen, new PointF(10, yOffset));
                yOffset += lineHeight;
                g.DrawString(_aiCore.StateFacade.GetAimbotStatusText(), _font, Brushes.White, new PointF(10, yOffset));
                yOffset += lineHeight;
                g.DrawString(_aiCore.StateFacade.GetCurrentWeaponText(), _font, Brushes.White, new PointF(10, yOffset));
                yOffset += lineHeight;
                g.DrawString(_aiCore.StateFacade.GetWeaponMatchValueText(), _font, Brushes.White, new PointF(10, yOffset));
                yOffset += lineHeight;
                g.DrawString(_aiCore.StateFacade.GetShowWeaponDebugText(), _font, Brushes.White, new PointF(10, yOffset));
                yOffset += lineHeight;

                // --- Lobby/Button Detection Debug Info ---
                if (_aiCore.StateFacade.IsLobbyDebugOn())
                {
                    yOffset += lineHeight;
                    g.DrawString(_aiCore.StateFacade.GetUIShowLobbyButtonDebugImage(), _font, Brushes.White, new PointF(10, yOffset));
                    yOffset += lineHeight;
                    g.DrawString(_aiCore.StateFacade.GetUILobbyMatchValueText(), _font, Brushes.White, new PointF(10, yOffset));
                    yOffset += lineHeight;
                    g.DrawString(_aiCore.StateFacade.GetIsInLobbyText(), _font, Brushes.White, new PointF(10, yOffset));
                    yOffset += lineHeight;
                    g.DrawString(_aiCore.StateFacade.GetLatestStartButtonMatchValue(), _font, Brushes.White, new PointF(10, yOffset));
                    yOffset += lineHeight;
                    g.DrawString(_aiCore.StateFacade.GetLatestCancelButtonMatchValue(), _font, Brushes.White, new PointF(10, yOffset));
                }
            }
        }
    }
}