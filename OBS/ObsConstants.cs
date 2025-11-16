using OBSWebsocketDotNet.Types;

namespace ParadiseHelper.OBS
{
    /// <summary>
    /// Provides a centralized repository for constants, default settings, and source identification
    /// used when interacting with OBS Studio via the WebSocket protocol.
    /// </summary>
    public static class ObsConstants
    {
        // OBS Input Kinds

        /// <summary>
        /// OBS Input Kind identifier for Window Capture sources.
        /// </summary>
        public const string InputKind_WindowCapture = "window_capture";

        // OBS Resolutions

        /// <summary>
        /// The standard default width for OBS scenes and video settings (1280 pixels).
        /// </summary>
        public const int StandardWidth = 1280;

        /// <summary>
        /// The standard default height for OBS scenes and video settings (720 pixels).
        /// </summary>
        public const int StandardHeight = 720;

        /// <summary>
        /// Default <see cref="ObsVideoSettings"/> configured for standard 720p (1280x720) resolution at 60 FPS.
        /// </summary>
        public static readonly ObsVideoSettings StandardSettings = new ObsVideoSettings
        {
            BaseWidth = 1280,
            BaseHeight = 720,
            OutputWidth = 1280,
            OutputHeight = 720,
            FpsNumerator = 60,
            FpsDenominator = 1
        };

        /// <summary>
        /// Default <see cref="SceneItemTransformInfo"/> to make a source fill the entire Standard 720p scene.
        /// </summary>
        public static readonly SceneItemTransformInfo DefaultFullWindowTransform = new SceneItemTransformInfo
        {
            // --- POSITION & ALIGNMENT ---
            X = 0.0,
            Y = 0.0,
            Rotation = 0.0,
            Alignnment = 5, // Center-Center

            // --- BOUNDS/STRETCH SETTINGS ---

            // Bounds Type: Stretches the source to the Bounds dimensions.
            BoundsType = SceneItemBoundsType.OBS_BOUNDS_STRETCH,
            BoundsWidth = StandardWidth,
            BoundsHeight = StandardHeight,
            BoundsAlignnment = 0,

            // --- CROP ---
            CropBottom = 0,
            CropLeft = 0,
            CropRight = 0,
            CropTop = 0,
        };

        /// <summary>
        /// The default scene name used for AI-related captures (e.g., "#Special#").
        /// </summary>
        public static string DefaultAISceneName = "#Special#";

        /// <summary>
        /// The default source name used for the AI capture source (e.g., "CS2Capture").
        /// </summary>
        public static string DefaultAISourceName = "CS2Capture";

        /// <summary>
        /// Contains constant identifiers used to find and capture specific application windows.
        /// </summary>
        public class WindowCaptureSources
        {
            /// <summary>
            /// Constants related to capturing the Counter-Strike 2 application window.
            /// </summary>
            public class CS2
            {
                /// <summary>
                /// The expected window title of the Counter-Strike 2 application.
                /// </summary>
                public const string Title = "Counter-Strike 2";

                /// <summary>
                /// The expected window class name (or similar identifier) of the Counter-Strike 2 application.
                /// </summary>
                public const string Class = "SDL_app";

                /// <summary>
                /// The executable name of the Counter-Strike 2 application.
                /// </summary>
                public const string Name = "cs2.exe";

                /// <summary>
                /// A special marker string used in the window title to indicate a managed or special capture state.
                /// </summary>
                public const string SpecialWindowTitleMarker = "#Special#";
            }
        }
    }
}