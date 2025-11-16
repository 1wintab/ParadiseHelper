using System;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using OpenCvSharp;
using ParadiseHelper.Tools.WinAPI;
using ParadiseHelper.AI.Control.Mouse;
using ParadiseHelper.AI.Orientation.Map;
using ParadiseHelper.AI.Control.KeyBoard;
using ParadiseHelper.AI.Orientation.Radar;
using ParadiseHelper.AI.Orientation.Map.Navmesh;


namespace ParadiseHelper.AI.Movement
{
    /// <summary>
    /// Manages autonomous player navigation, pathfinding, and anti-stuck logic using Navmesh and radar data.
    /// </summary>
    public class NavigationManager
    {
        // --- Constants ---
        
        /// <summary>The distance threshold for detecting large, sudden coordinate jumps (e.g., fast travel or map errors).</summary>
        private const double POSITION_JUMP_THRESHOLD = 100.0;
       
        /// <summary>The distance to the next Navmesh node considered sufficient for arrival.</summary>
        private const float NAVMESH_ARRIVAL_DISTANCE = 1.0f;
        
        /// <summary>The maximum time allowed to spend moving towards a single <see cref="NextNode"/> before resetting navigation.</summary>
        private static readonly TimeSpan NextNodeTimeout = TimeSpan.FromSeconds(4.0);
        
        /// <summary>Factor for smoothing camera rotation during pathfinding (lower = smoother turn).</summary>
        private const float NAVIGATION_TURN_SMOOTHING_FACTOR = 0.1f;
        
        /// <summary>The distance threshold below which movement is considered insufficient for anti-stuck check.</summary>
        private const double STUCK_DISTANCE_THRESHOLD = 2.0;
        
        /// <summary>The duration the player must be "stuck" (not moving past the distance threshold) before triggering an unstuck maneuver.</summary>
        private static readonly TimeSpan StuckTimeThreshold = TimeSpan.FromSeconds(1.5);

        // --- Fields ---
        
        // Lock object used for thread-safe access to navigation state variables.
        private readonly object _lockObject;

        // --- Navigation State ---

        /// <summary>Gets the currently loaded navigation mesh (<see cref="Navmesh"/>).</summary>
        public Navmesh CurrentNavmesh { get; private set; }
        
        /// <summary>Gets the node closest to the player's current radar position.</summary>
        public Node NearestNavmeshNode { get; private set; }
        
        /// <summary>Gets the previously visited node, used for path reversal.</summary>
        public Node LastNode { get; private set; }

        /// <summary>Gets the node the player is currently considered to be on or close to.</summary>
        public Node CurrentNode { get; private set; }

        /// <summary>Gets the target node the player is currently attempting to reach.</summary>
        public Node NextNode { get; private set; }

        /// <summary>Gets the historical list of nodes traversed.</summary>
        public List<Node> PathHistory { get; } = new List<Node>();

        // Set of node IDs that have already been visited during the current navigation session.
        private readonly HashSet<int> _visitedNodes = new HashSet<int>();

        // Timestamp when the current NextNode was assigned. Used for timeout checking.
        private DateTime _nextNodeAssignedTimestamp = DateTime.MinValue;

        // --- Position Tracking & Anti-Stuck ---
        
        // The last known radar position of the player before the current update cycle.
        private OpenCvSharp.Point? _lastPlayerRadarPosition;

        // The player position recorded for the start of the current anti-stuck check period.
        private OpenCvSharp.Point? _lastStuckCheckPosition;

        // Timestamp of the last detected significant position change.
        private DateTime _lastPositionChangeTimestamp = DateTime.UtcNow;

        // Random number generator for randomized unstuck maneuvers.
        private readonly Random _random = new Random();

        /// <summary>Gets the latest movement decision in a descriptive string format (e.g., "W:ON A:off S:off D:ON").</summary>
        public string LatestMovementDecision { get; private set; } = "Standing Still";

        // Flag to track if the player was in combat in the previous update cycle.
        private bool _wasPreviouslyInCombat = false;

        // Name of the map currently being navigated. Used to detect map changes.
        private string _navigatingMapName = "Unknown";
        // A set of nodes identified as dead ends for the current map.
        private HashSet<int> _deadEndNodeIds = new HashSet<int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationManager"/> class.
        /// </summary>
        /// <param name="lockObject">The synchronization object used for thread safety.</param>
        public NavigationManager(object lockObject)
        {
            _lockObject = lockObject;
        }

        // --- Primary update method (async) ---
        /// <summary>
        /// The main loop update method for autonomous movement. It handles state checking, anti-stuck logic, and navigation decisions.
        /// </summary>
        /// <param name="isPlayerVisible">True if the player's icon is visible on the radar.</param>
        /// <param name="isInCombat">True if the player is currently engaged in combat.</param>
        /// <param name="shouldBuyWeapon">True if the bot's logic dictates it should currently be performing a weapon purchase.</param>
        /// <param name="playerLocation">The player's current coordinates on the radar image.</param>
        /// <param name="playerDirectionDegrees">The player's current camera orientation in degrees (0-360).</param>
        /// <param name="currentMapName">The name of the current map (for map change detection).</param>
        /// <param name="currentMapBox">The bounding box of the radar map image.</param>
        /// <param name="navmesh">The current Navmesh data for the map.</param>
        public async Task UpdateMovement(
            bool isPlayerVisible,
            bool isInCombat,
            bool shouldBuyWeapon,
            OpenCvSharp.Point? playerLocation,
            double playerDirectionDegrees,
            string currentMapName,
            Rect? currentMapBox,
            Navmesh navmesh)
        {
            // Check if combat just ended and update the node timeout timestamp to allow time for the next move.
            if (_wasPreviouslyInCombat && !isInCombat && NextNode != null)
            {
                _nextNodeAssignedTimestamp = DateTime.UtcNow;
            }
            _wasPreviouslyInCombat = isInCombat;

            // Check for map change and update dead-end nodes if a new map is detected.
            if (navmesh != null && currentMapName != _navigatingMapName)
            {
                _navigatingMapName = currentMapName;
                // Recalculate dead-end nodes for the new map's navmesh.
                _deadEndNodeIds = NavmeshCore.FindDeadEndNodeIds(navmesh);

                ResetNavigationState("Map changed");
            }

            // If player is not visible, stop all movement and clear position data.
            if (!isPlayerVisible)
            {
                StopMovement();
                _lastPlayerRadarPosition = null;
                return;
            }

            // Check for sudden coordinate "jump" (e.g., from teleportation or map transition).
            if (_lastPlayerRadarPosition.HasValue && playerLocation.HasValue)
            {
                double distance = CalculateDistance(_lastPlayerRadarPosition.Value, playerLocation.Value);

                if (distance > POSITION_JUMP_THRESHOLD)
                {
                    ResetNavigationState("Large position jump detected");

                    _lastPlayerRadarPosition = null;

                    return;
                }
            }

            // Anti-stuck logic: check if the player is stuck and perform a recovery maneuver.
            if (await CheckAndPerformUnstuck(playerLocation)) return;

            // Timeout check for reaching the NextNode: If too much time has passed, reset navigation.
            if (NextNode != null && (DateTime.UtcNow - _nextNodeAssignedTimestamp) > NextNodeTimeout)
            {
                ResetNavigationState("NextNode timeout");
                return;
            }

            // Store the last position for the next update cycle's jump detection.
            _lastPlayerRadarPosition = playerLocation;

            // Update the state of the navmesh relative to the player's position.
            if (navmesh != null && currentMapBox.HasValue)
            {
                UpdateNavmeshState(playerLocation, currentMapBox.Value, navmesh, _deadEndNodeIds);
            }

            // Decision on movement: Navigate only if not in combat, not buying weapons, and a path exists.
            bool canNavigateToNextNode = !isInCombat && !shouldBuyWeapon && NextNode != null;

            if (canNavigateToNextNode)
            {
                NavigateToNextNode(playerLocation, playerDirectionDegrees, currentMapBox);
            }
            else
            {
                StopMovement();
            }
        }

        /// <summary>
        /// Stops all movement and resets the internal navigation state variables (current node, path history, etc.).
        /// </summary>
        /// <param name="reason">The reason for the navigation reset (for logging/debugging).</param>
        public void ResetNavigationState(string reason)
        {
            StopMovement();

            lock (_lockObject)
            {
                CurrentNavmesh = null;
                NearestNavmeshNode = null;
                LastNode = null;
                CurrentNode = null;
                NextNode = null;

                PathHistory.Clear();
                _visitedNodes.Clear();
            }
        }

        /// <summary>
        /// Releases all currently pressed movement keys (W, A, S, D) and updates the movement decision status.
        /// </summary>
        public void StopMovement()
        {
            KeyboardController.ReleaseAllKeys();
            LatestMovementDecision = "Standing Still";
        }

        /// <summary>
        /// Finds the nearest navmesh node and updates the internal state (<see cref="CurrentNode"/> and <see cref="NextNode"/>).
        /// </summary>
        private void UpdateNavmeshState(OpenCvSharp.Point? playerLocation, Rect currentMapBox, Navmesh navmesh, HashSet<int> deadEndNodeIds)
        {
            // Retrieve radar template dimensions to calculate the player's center.
            int templateWidth = RadarCapture.GetTemplateWidth();
            int templateHeight = RadarCapture.GetTemplateHeight();

            // Calculate player's absolute center position on the radar image.
            var playerCenterOnRadar = new System.Drawing.Point(
                playerLocation.Value.X + templateWidth / 2,
                playerLocation.Value.Y + templateHeight / 2
            );

            // Calculate player's position relative to the map bounding box (Navmesh coordinate system).
            int relativeX = playerCenterOnRadar.X - currentMapBox.X;
            int relativeY = playerCenterOnRadar.Y - currentMapBox.Y;

            // Convert to a PointF structure for use with Navmesh logic.
            PointF playerCoords = new PointF(relativeX, relativeY);

            var mapDetectionResult = new MapDetectionResult { BoundingBox = currentMapBox };
            var nearestNode = NavmeshCore.FindNearestNode(playerCoords, navmesh, mapDetectionResult);

            lock (_lockObject)
            {
                CurrentNavmesh = navmesh;
                NearestNavmeshNode = nearestNode;

                // Step 1: Initialize the current node state or check for arrival at the NextNode.
                if (CurrentNode == null && nearestNode != null)
                {
                    // Initial setup: set CurrentNode, clear history, and start tracking visited nodes.
                    CurrentNode = nearestNode;
                    PathHistory.Clear();
                    _visitedNodes.Clear();
                    PathHistory.Add(CurrentNode);
                    _visitedNodes.Add(CurrentNode.ID);
                }
                else if (NextNode != null)
                {
                    // Check if the player has arrived at the NextNode.
                    float distanceToNextNode = CalculateDistance(NextNode, playerCoords);
                    if (distanceToNextNode < NAVMESH_ARRIVAL_DISTANCE)
                    {
                        // Transition to the new node.
                        LastNode = CurrentNode;
                        CurrentNode = NextNode;
                        NextNode = null;
                        // Small pause to allow game state to stabilize after node arrival.
                        Thread.Sleep(50);
                    }
                }

                // Step 2: If the current target is null, decide on the next node to move towards.
                if (CurrentNode != null && NextNode == null)
                {
                    DecideNextNode(navmesh, deadEndNodeIds, playerCoords);
                }
            }
        }

        /// <summary>
        /// Determines the next node in the path based on current location, connectivity, and history.
        /// </summary>
        private void DecideNextNode(Navmesh navmesh, HashSet<int> deadEndNodeIds, PointF playerCoords)
        {
            // Find all nodes connected to the CurrentNode.
            var connectedNodeIDs = navmesh.Edges
                .Where(e => e.Node1ID == CurrentNode.ID || e.Node2ID == CurrentNode.ID)
                .Select(e => e.Node1ID == CurrentNode.ID ? e.Node2ID : e.Node1ID)
                .ToHashSet();

            var connectedNodes = navmesh.Nodes
                .Where(n => connectedNodeIDs.Contains(n.ID))
                .ToList();

            // Filter out the node we just came from and known dead ends.
            var validForwardCandidates = connectedNodes
                .Where(n => (LastNode == null || n.ID != LastNode.ID) && !deadEndNodeIds.Contains(n.ID))
                .ToList();

            if (validForwardCandidates.Any())
            {
                // Select the next node using a priority sort:
                // 1. Prioritize unvisited nodes (OrderBy ensures unvisited nodes come first).
                // 2. Break ties by choosing the node closest to the player's current position (to avoid backtracking).
                NextNode = validForwardCandidates
                    .OrderBy(n => _visitedNodes.Contains(n.ID))
                    .ThenBy(n => CalculateDistance(n, playerCoords))
                    .FirstOrDefault();
            }
            else
            {
                // If no forward path is found, retreat to the previous node (if available).
                NextNode = LastNode;
            }

            if (NextNode != null)
            {
                _nextNodeAssignedTimestamp = DateTime.UtcNow;

                PathHistory.Add(NextNode);

                // Mark node as visited when assigned as NextNode to discourage immediate re-visiting.
                _visitedNodes.Add(NextNode.ID);
            }
        }

        /// <summary>
        /// Calculates the required angle to the next node and simulates the corresponding camera and key presses.
        /// </summary>
        private void NavigateToNextNode(OpenCvSharp.Point? playerLocationOnRadar, double currentPlayerAngle, Rect? currentMapBox)
        {
            if (NextNode == null || currentPlayerAngle < 0 || !playerLocationOnRadar.HasValue || !currentMapBox.HasValue)
            {
                StopMovement();

                return;
            }

            // Get radar template dimensions.
            int templateWidth = RadarCapture.GetTemplateWidth();
            int templateHeight = RadarCapture.GetTemplateHeight();

            // Calculate player's coordinates relative to the map (Navmesh space).
            PointF realPlayerCoords = new PointF(
                playerLocationOnRadar.Value.X + templateWidth / 2 - currentMapBox.Value.X,
                playerLocationOnRadar.Value.Y + templateHeight / 2 - currentMapBox.Value.Y
            );

            // Get target node coordinates.
            PointF nextNodeLocation = new PointF((float)NextNode.X, (float)NextNode.Y);
            // Calculate the required rotation angle.
            double angleToNextNode = CalculateAngle(realPlayerCoords, nextNodeLocation);

            SimulateMovement(angleToNextNode, currentPlayerAngle);
        }

        /// <summary>
        /// Performs a counter-strafe maneuver (pressing the inverse movement key briefly) to stop movement quickly.
        /// </summary>
        public void PerformCounterStrafe()
        {
            // Ensure the game window is active to send inputs.
            if (!WindowController.IsGameWindowActiveAndFocused()) return;

            // Check which keys are currently pressed.
            bool isMovingForward = KeyboardController.IsKeyPressed(Keys.W);
            bool isMovingBack = KeyboardController.IsKeyPressed(Keys.S);
            bool isMovingLeft = KeyboardController.IsKeyPressed(Keys.A);
            bool isMovingRight = KeyboardController.IsKeyPressed(Keys.D);

            // Do nothing if no movement keys are currently active.
            if (!isMovingForward && !isMovingBack && !isMovingLeft && !isMovingRight) return;

            // Release all keys before initiating the counter-strafe.
            KeyboardController.ReleaseAllKeys();

            // Determine counter keys (W -> S, A -> D, etc.)
            List<Keys> counterKeys = new List<Keys>();
            if (isMovingForward) counterKeys.Add(Keys.S);
            if (isMovingBack) counterKeys.Add(Keys.W);
            if (isMovingLeft) counterKeys.Add(Keys.D);
            if (isMovingRight) counterKeys.Add(Keys.A);

            // Press the counter keys momentarily.
            foreach (var key in counterKeys) KeyboardController.PressKey(key);

            // Hold keys for a brief duration.
            Thread.Sleep(50);

            // Release the counter keys.
            foreach (var key in counterKeys) KeyboardController.ReleaseKey(key);
        }

        /// <summary>
        /// Simulates player movement (camera turn and key presses) based on target angle.
        /// </summary>
        /// <param name="targetAngle">The desired heading in degrees (0-360).</param>
        /// <param name="currentAngle">The player's current camera heading in degrees (0-360).</param>
        private void SimulateMovement(double targetAngle, double currentAngle)
        {
            // --- 1. Guard Clauses ---
           
            // Stop if the game window isn't focused.
            if (!WindowController.IsGameWindowActiveAndFocused())
            {
                StopMovement();
                return;
            }
           
            // Ignore if current angle is invalid.
            if (currentAngle < 0) return;

            // --- 2. Angular Logic (Normalization) ---

            // Calculate the shortest angle difference between the current and target angles (-180 to 180).
            double angleDifference = targetAngle - currentAngle;
            
            while (angleDifference <= -180) angleDifference += 360;
            while (angleDifference > 180) angleDifference -= 360;

            // --- 3. Camera Turn ---

            const double turnThresholdDegrees = 1.5;

            // Only turn the camera if the difference is greater than the turn threshold.
            if (Math.Abs(angleDifference) > turnThresholdDegrees)
            {
                // Smoothly adjust the camera's horizontal view using the smoothing factor.
                MouseAimer.TurnCameraHorizontally(angleDifference * NAVIGATION_TURN_SMOOTHING_FACTOR);
            }

            // --- 4. WASD Decision ---
            // Determine the optimal combination of WASD keys to press based on the angular difference.
            var (pressW, pressS, pressA, pressD) = GetMovementDecision(angleDifference);

            // --- 5. Record and Execute Movement ---
            // Format the movement decision for external monitoring/logging.
            string decision =
                $"W:{(pressW ? "ON" : "off")} " +
                $"A:{(pressA ? "ON" : "off")} " +
                $"S:{(pressS ? "ON" : "off")} " +
                $"D:{(pressD ? "ON" : "off")}";

            LatestMovementDecision = decision;

            // Execute key presses/releases based on the decision.
            if (pressW) KeyboardController.PressKey(Keys.W); else KeyboardController.ReleaseKey(Keys.W);
            if (pressS) KeyboardController.PressKey(Keys.S); else KeyboardController.ReleaseKey(Keys.S);
            if (pressA) KeyboardController.PressKey(Keys.A); else KeyboardController.ReleaseKey(Keys.A);
            if (pressD) KeyboardController.PressKey(Keys.D); else KeyboardController.ReleaseKey(Keys.D);
        }

        /// <summary>
        /// Determines which WASD keys to press based on the angular difference to the target.
        /// </summary>
        /// <param name="angleDifference">The signed angular difference in degrees (-180 to 180).</param>
        /// <returns>A tuple indicating the state (press/release) for W, S, A, and D keys.</returns>
        private (bool w, bool s, bool a, bool d) GetMovementDecision(double angleDifference)
        {
            bool pressW = false, pressS = false, pressA = false, pressD = false;

            // Simplified movement logic based on angular ranges to favor diagonal movement when off-center.
            
            // Moving primarily forward (W)
            if (angleDifference > -15 && angleDifference <= 15) { pressW = true; }
            
            // Forward-Right (W+D)
            else if (angleDifference > 15 && angleDifference <= 70) { pressW = true; pressD = true; }
            
            // Pure Right (D)
            else if (angleDifference > 70 && angleDifference <= 110) { pressD = true; }
            
            // Backward-Right (S+D)
            else if (angleDifference > 110 && angleDifference <= 165) { pressS = true; pressD = true; }
            
            // Pure Backward (S)
            else if (angleDifference > 165 || angleDifference <= -165) { pressS = true; }
            
            // Backward-Left (S+A)
            else if (angleDifference > -165 && angleDifference <= -110) { pressS = true; pressA = true; }
            
            // Pure Left (A)
            
            else if (angleDifference > -110 && angleDifference <= -70) { pressA = true; }
            
            // Forward-Left (W+A)
            else if (angleDifference > -70 && angleDifference <= -15) { pressW = true; pressA = true; }

            return (pressW, pressS, pressA, pressD);
        }

        /// <summary>
        /// Calculates the angle in degrees (0-360) from a starting point (p1) to an end point (p2).
        /// </summary>
        /// <param name="p1">The starting point (player coordinates).</param>
        /// <param name="p2">The end point (target node coordinates).</param>
        /// <returns>The angle in degrees from p1 to p2.</returns>
        private double CalculateAngle(PointF p1, PointF p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;

            // Note: The Y-axis inversion (-dy) is common for radar/screen coordinates where Y increases downwards.
            double angleRad = Math.Atan2(dx, -dy);
            double angleDeg = angleRad * 180.0 / Math.PI;

            // Convert angle from -180/180 range to 0-360 range.
            if (angleDeg < 0) angleDeg += 360;

            return angleDeg;
        }

        /// <summary>
        /// Calculates the Euclidean distance between a Navmesh Node and a PointF structure.
        /// </summary>
        /// <param name="node">The Navmesh Node.</param>
        /// <param name="playerCoords">The player's coordinates (PointF).</param>
        /// <returns>The distance as a float.</returns>
        private float CalculateDistance(Node node, PointF playerCoords)
        {
            return (float)Math.Sqrt(
                Math.Pow(node.X - playerCoords.X, 2) +
                Math.Pow(node.Y - playerCoords.Y, 2)
            );
        }

        /// <summary>
        /// Calculates the Euclidean distance between two OpenCvSharp.Point structures (used for radar positions).
        /// </summary>
        /// <param name="p1">The first point.</param>
        /// <param name="p2">The second point.</param>
        /// <returns>The distance as a double.</returns>
        private double CalculateDistance(OpenCvSharp.Point p1, OpenCvSharp.Point p2)
        {
            return Math.Sqrt(
                Math.Pow(p1.X - p2.X, 2) +
                Math.Pow(p1.Y - p2.Y, 2)
            );
        }

        /// <summary>
        /// Checks if the player is stuck and performs an unstuck maneuver if necessary.
        /// </summary>
        /// <param name="playerLocation">The player's current radar position.</param>
        /// <returns>True if an unstuck maneuver was performed (and further movement should be halted for this cycle), otherwise false.</returns>
        private async Task<bool> CheckAndPerformUnstuck(OpenCvSharp.Point? playerLocation)
        {
            // Initialize or return if position data is missing.
            if (!_lastStuckCheckPosition.HasValue || !playerLocation.HasValue)
            {
                UpdateStuckCheckData(playerLocation);
                return false;
            }

            var lastPos = _lastStuckCheckPosition.Value;
            var currentPos = playerLocation.Value;

            double distanceMoved = CalculateDistance(lastPos, currentPos);

            if (distanceMoved < STUCK_DISTANCE_THRESHOLD)
            {
                // The player has not moved enough since the last check position.
                if (DateTime.UtcNow - _lastPositionChangeTimestamp > StuckTimeThreshold)
                {
                    // If stuck for too long, trigger the recovery maneuver.
                    await PerformUnstuckManeuver();

                    // Reset anti-stuck state after recovery.
                    _lastPositionChangeTimestamp = DateTime.UtcNow;
                    _lastStuckCheckPosition = null;

                    return true;
                }
                // If stuck but not for long, continue monitoring; do not update the stuck check data.
            }
            else
            {
                // The player is moving sufficiently; update the anti-stuck reference point.
                UpdateStuckCheckData(playerLocation);
            }

            return false;
        }

        /// <summary>
        /// Updates the timestamp and position used for the anti-stuck mechanism.
        /// </summary>
        /// <param name="newLocation">The player's new radar position.</param>
        private void UpdateStuckCheckData(OpenCvSharp.Point? newLocation)
        {
            if (newLocation.HasValue)
            {
                _lastPositionChangeTimestamp = DateTime.UtcNow;
                _lastStuckCheckPosition = newLocation;
            }
        }

        /// <summary>
        /// Executes a brief, randomized backward and sideways movement to dislodge the player.
        /// </summary>
        private async Task PerformUnstuckManeuver()
        {
            StopMovement();

            // Allow time for the game to process key releases.
            await Task.Delay(50);

            // Choose a random side (A or D) for strafing.
            Keys sideKey = _random.Next(0, 2) == 0 ? Keys.A : Keys.D;

            // Move backward (S) and sideways simultaneously.
            KeyboardController.PressKey(Keys.S);
            KeyboardController.PressKey(sideKey);

            // Hold for the duration of the maneuver.
            await Task.Delay(600);

            // Stop movement immediately afterwards.
            StopMovement();
        }
    }
}