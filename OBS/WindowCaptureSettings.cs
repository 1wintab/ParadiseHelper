using Newtonsoft.Json;

namespace ParadiseHelper.OBS
{
    /// <summary>
    /// Represents the specific settings structure for an OBS 'window_capture' input type.
    /// This provides strong typing for managing window capture parameters.
    /// </summary>
    public class WindowCaptureSettings
    {
        /// <summary>
        /// Whether to capture the client area of the window (excluding border/title bar).
        /// Corresponds to "client_area" in OBS settings. (Default: True)
        /// </summary>
        [JsonProperty("client_area")]
        public bool ClientArea { get; set; } = true;

        /// <summary>
        /// Whether to capture the mouse cursor.
        /// Corresponds to "cursor" in OBS settings. (Default: False)
        /// </summary>
        [JsonProperty("cursor")]
        public bool CaptureCursor { get; set; } = false;

        /// <summary>
        /// Forces the use of Standard Dynamic Range (SDR) color.
        /// Corresponds to "force_sdr" in OBS settings. (Default: False)
        /// </summary>
        [JsonProperty("force_sdr")]
        public bool ForceSDR { get; set; } = false;

        /// <summary>
        /// The capture method (0 = Automatic, 1 = BitBlt, 2 = Windows Graphics Capture, etc.).
        /// Corresponds to "method" in OBS settings. (Default: 0 - Automatic)
        /// </summary>
        [JsonProperty("method")]
        public int Method { get; set; } = 0;

        /// <summary>
        /// The priority of the window match search:
        /// (0 = Title must match, 1 = Title contains, 2 = Title matches, otherwise executable matches, etc.).
        /// Corresponds to "priority" in OBS settings. (Default: 0)
        /// </summary>
        [JsonProperty("priority")]
        public int Priority { get; set; } = 0;

        /// <summary>
        /// The specific window title or identifier string to capture.
        /// Corresponds to "window" in OBS settings. (Default: Empty)
        /// </summary>
        [JsonProperty("window")]
        public string WindowIdentifier { get; set; } = string.Empty;

        /// <summary>
        /// Gets a default instance of <see cref="WindowCaptureSettings"/> using the most common and reliable settings.
        /// </summary>
        public static WindowCaptureSettings Default => new WindowCaptureSettings();
    }
}