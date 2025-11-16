using OpenCvSharp;

namespace ParadiseHelper.AI.Orientation.Map
{
    /// <summary>
    /// Data class to store the result of the map detection process (template matching).
    /// </summary>
    public class MapDetectionResult
    {
        /// <summary>
        /// Gets or sets the name of the map template that was matched best (e.g., "Dust2", "Mirage").
        /// </summary>
        public string MapName { get; set; }

        /// <summary>
        /// Gets or sets the match value from the template matching (SqDiffNormed: lower is better).
        /// </summary>
        public double MatchValue { get; set; }

        /// <summary>
        /// Gets or sets the bounding box (location and size) of the best match in the radar image.
        /// </summary>
        public Rect BoundingBox { get; set; }
    }
}