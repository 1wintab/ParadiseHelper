using System.Threading;
using ParadiseHelper.AI.Control.KeyBoard.Binds.AutoBuy;
using ParadiseHelper.AI.Orientation.Radar;
using ParadiseHelper.Tools.WinAPI;
using ParadiseHelper.Tools;

namespace ParadiseHelper.AI.Core.DetectionLoop.Handlers
{
    /// <summary>
    /// Handler responsible for automatically purchasing weapons at the start of a round or upon respawn.
    /// It manages the buying process safely by checking map context and temporarily
    /// disabling the Aimbot feature to prevent conflicts during keyboard input simulation.
    /// </summary>
    public class AutoBuyHandler : DetectionLoopBase
    {
        // The global AI state container, used to read flags like ShouldBuyWeapon and manage Aimbot state.
        private readonly AIState _state;
        
        // The current state of the player, checked to ensure buying doesn't happen when the player is dead.
        private readonly PlayerState _playerState;
        
        // The utility class responsible for simulating keyboard inputs to perform the actual purchase.
        private readonly AutoBuyWeapons _autoBuyWeapons;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoBuyHandler"/> class.
        /// </summary>
        /// <param name="state">The global AI state container.</param>
        /// <param name="playerState">The current state of the player.</param>
        /// <param name="autoBuyWeapons">The utility class for performing keyboard bind actions for buying.</param>
        public AutoBuyHandler(AIState state, PlayerState playerState, AutoBuyWeapons autoBuyWeapons)
        {
            _state = state;
            _playerState = playerState;
            _autoBuyWeapons = autoBuyWeapons;
        }

        /// <summary>
        /// The main execution loop for the handler, running until cancellation is requested.
        /// </summary>
        /// <param name="token">The cancellation token to stop the loop.</param>
        protected override void Loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Check if the state machine has requested a weapon purchase.
                if (_state.ShouldBuyWeapon)
                {
                    // Check if this is the first time entering the buy process since respawn.
                    // This determines if the Aimbot state needs to be saved.
                    bool isInitialBuy = !_state.AimbotStateBeforeBuy.HasValue;

                    try
                    {
                        if (isInitialBuy)
                        {
                            // Save the current Aimbot state before disabling it for safety during input simulation.
                            _state.AimbotStateBeforeBuy = _state.AimbotEnabled;
                        }

                        // Temporarily disable Aimbot to prevent conflicts during keyboard input.
                        _state.AimbotEnabled = false;

                        // Check map detection confidence (MatchValue <= 0.2 means the map is known with high confidence).
                        bool isMapKnown = (_state.LatestMapMatchValue <= 0.2 ? _state.LatestMapName : "Unknown") != "Unknown";

                        // Check if player location is within the bounds of the detected map image.
                        bool isPlayerOnMap = IsPlayerOnMapWithBoxMargin();

                        if (isMapKnown && isPlayerOnMap)
                        {
                            // Wait for the defined time period to allow the buy phase to begin in-game.
                            Thread.Sleep(AIState.TimeForBuy);

                            if (token.IsCancellationRequested) break;

                            // Final check before executing purchase command.
                            if (_state.ShouldBuyWeapon && !_playerState.IsPlayerDead)
                            {
                                // Check if the game window is active, as key presses require focus.
                                if (WindowController.IsGameWindowActiveAndFocused())
                                {
                                    _autoBuyWeapons.PerformAutoBuy();
                                }
                            }
                        }
                    }
                    finally
                    {
                        // Logic to restore the Aimbot state and finalize the purchase sequence.
                        if (isInitialBuy)
                        {
                            // If it was an initial buy, restore Aimbot to the default enabled state after purchase.
                            _state.AimbotEnabled = true;
                        }
                        else if (_state.AimbotStateBeforeBuy.HasValue)
                        {
                            // Restore Aimbot to the state saved earlier (if it wasn't the initial buy).
                            _state.AimbotEnabled = _state.AimbotStateBeforeBuy.Value;
                        }

                        // Clear the saved state and reset the buy flag, regardless of success.
                        _state.AimbotStateBeforeBuy = null;
                        _state.ShouldBuyWeapon = false;
                    }
                }
                // Short sleep interval when not actively buying to save CPU resources.
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Checks if the player's radar location is within the bounds of the detected map template with a margin.
        /// This helps ensure buying only happens when the player is loaded into the map and positioned correctly.
        /// </summary>
        /// <returns>True if the player's location is within the map bounds (with margin), otherwise false.</returns>
        private bool IsPlayerOnMapWithBoxMargin()
        {
            var playerLocation = _state.PlayerLocationOnRadar;
            var mapBoundingBox = _state.LatestMapBoundingBox;

            if (!playerLocation.HasValue || !mapBoundingBox.HasValue)
            {
                // Cannot perform check if either player location or map bounds are unknown.
                return false;
            }

            // Using placeholder methods for getting template dimensions, as the actual implementation 
            // of RadarCapture is not available here. These values define the margin.
            int templateWidth = RadarCapture.GetTemplateWidth();
            int templateHeight = RadarCapture.GetTemplateHeight();

            // Define margins based on template size to allow player to be slightly outside the map bounds.
            // Using a margin equal to the template size gives a generous buffer.
            int leftMargin = templateWidth;
            int topMargin = templateHeight;
            int rightMargin = 0;
            int bottomMargin = 0;

            // Check boundaries
            bool isInLeftBound = playerLocation.Value.X >= mapBoundingBox.Value.X - leftMargin;
            bool isInTopBound = playerLocation.Value.Y >= mapBoundingBox.Value.Y - topMargin;
            bool isInRightBound = playerLocation.Value.X <= mapBoundingBox.Value.X + mapBoundingBox.Value.Width + rightMargin;
            bool isInBottomBound = playerLocation.Value.Y <= mapBoundingBox.Value.Y + mapBoundingBox.Value.Height + bottomMargin;

            return isInLeftBound
                && isInTopBound
                && isInRightBound
                && isInBottomBound;
        }
    }
}