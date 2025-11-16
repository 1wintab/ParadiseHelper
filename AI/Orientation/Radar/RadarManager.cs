using System.Linq;
using System.Collections.Generic;
using OpenCvSharp;

namespace ParadiseHelper.AI.Orientation.Radar
{
    /// <summary>
    /// Manages the state and processing logic for player location and movement direction 
    /// using data extracted from the in-game radar image.
    /// </summary>
    public class RadarManager
    {
        // Threshold for determining a successful detection of a 'dead player' icon. 
        // This is the maximum acceptable SqDiffNormed match value (lower is better).
        private const double DEATH_DETECTION_THRESHOLD = 0.44;

        // Defines the number of recent frames to use for smoothing the player's detected direction vector.
        // A window size of 1 means no actual smoothing, just the current frame's direction.
        private const int SmoothingWindowSize = 1;

        // History buffer for storing recent player direction vectors, used for calculating a smoothed average.
        private readonly List<Point> _directionHistory = new List<Point>();

        /// <summary>
        /// Gets the latest template matching score for the active player icon detection.
        /// (SqDiffNormed: lower is better).
        /// </summary>
        public double LatestRadarMatchValue { get; private set; }

        /// <summary>
        /// Gets the detected location of the active player icon on the processed radar image.
        /// Null if the player icon was not found.
        /// </summary>
        public Point? PlayerLocationOnRadar { get; private set; }

        /// <summary>
        /// Gets the smoothed and averaged direction vector of the player, derived from the icon's rotation.
        /// Null if the player icon or direction could not be determined.
        /// </summary>
        public Point? PlayerDirection { get; private set; }

        /// <summary>
        /// Gets the latest template matching score for the dead player icon detection.
        /// (SqDiffNormed: lower is better).
        /// </summary>
        public double LatestDeathMatchValue { get; private set; }

        /// <summary>
        /// Gets the detected location of the dead player icon on the processed radar image.
        /// Null if the dead player icon was not found.
        /// </summary>
        public Point? DeadPlayerLocationOnRadar { get; private set; }

        /// <summary>
        /// Checks if a dead player icon has been successfully detected based on the 
        /// <see cref="LatestDeathMatchValue"/> and the predefined threshold.
        /// </summary>
        /// <returns>True if the match value is less than or equal to the threshold, indicating a detected death.</returns>
        public bool IsDeathDetected()
        {
            // Check if a detection occurred (score is within 0.0 and the set threshold).
            return LatestDeathMatchValue >= 0.0 && LatestDeathMatchValue <= DEATH_DETECTION_THRESHOLD;
        }

        /// <summary>
        /// Processes the raw video frame to extract radar data, find the player icon, 
        /// determine their direction, and check for a death icon.
        /// </summary>
        /// <param name="frame">The raw input video frame (Mat BGR).</param>
        /// <returns>The processed and cropped radar image (Mat). The caller is responsible for disposing of this Mat object.</returns>
        public Mat ProcessRadar(Mat frame)
        {
            // 1. Detect the active player icon and crop the radar image from the full frame.
            var result = RadarCapture.ProcessRadarAndFindPlayer(frame);

            // 2. Check for the dead player icon (only if the initial radar image was successfully processed).
            if (result.ProcessedImage != null && !result.ProcessedImage.Empty())
            {
                // RadarCapture.DetectDeath returns a tuple/ValueTuple of (Point?, double).
                var deathResult = RadarCapture.DetectDeath(result.ProcessedImage);
                LatestDeathMatchValue = deathResult.Item2;
                DeadPlayerLocationOnRadar = deathResult.Item1;
            }

            Point? currentDirection = null;

            // 3. If the active player was found, attempt to determine their direction.
            if (result?.PlayerLocation.HasValue == true)
            {
                // Define the rectangular area around the detected player icon.
                var pRect = new Rect(
                    result.PlayerLocation.Value,
                    new Size(RadarCapture.GetTemplateWidth(),
                    RadarCapture.GetTemplateHeight())
                );

                // Safety check to ensure the bounding box does not exceed the image boundaries.
                if (pRect.Right <= result.ProcessedImage.Width + 1 && pRect.Bottom <= result.ProcessedImage.Height + 1)
                {
                    // Crop the player icon area for detailed direction analysis.
                    using (var playerIconMat = new Mat(result.ProcessedImage, pRect))
                    {
                        // Determine direction using image analysis specific to the player icon's rotation.
                        currentDirection = RadarCapture.DeterminePlayerDirection(playerIconMat);
                    }
                }
            }

            // 4. Apply smoothing to the player direction using a history buffer.
            if (currentDirection.HasValue)
            {
                _directionHistory.Add(currentDirection.Value);

                // Maintain the defined window size by removing the oldest entry.
                if (_directionHistory.Count > SmoothingWindowSize)
                    _directionHistory.RemoveAt(0);

                // Calculate the average (smoothed) direction vector.
                long sumX = _directionHistory.Sum(d => (long)d.X);
                long sumY = _directionHistory.Sum(d => (long)d.Y);

                PlayerDirection = new Point((int)(sumX / _directionHistory.Count), (int)(sumY / _directionHistory.Count));
            }
            else
            {
                // Clear history and direction if no valid direction was detected this frame.
                _directionHistory.Clear();
                PlayerDirection = null;
            }

            // 5. Update state properties with the latest information.
            if (result != null)
            {
                PlayerLocationOnRadar = result.PlayerLocation;
                LatestRadarMatchValue = result.MatchValue;
            }

            // 6. Return the processed radar image. The caller MUST dispose of this Mat.
            return result?.ProcessedImage;
        }
    }
}