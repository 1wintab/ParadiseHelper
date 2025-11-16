using System;
using OpenCvSharp;

namespace ParadiseHelper.AI.Orientation.Radar
{
    /// <summary>
    /// A data structure to hold the results of radar processing, including the processed image
    /// and the detected location of an icon (e.g., the player). It implements IDisposable to manage
    /// the unmanaged memory of the underlying OpenCvSharp Mat object.
    /// </summary>
    public class RadarDetectionResult : IDisposable
    {
        /// <summary>
        /// Gets or sets the processed, circular radar image (Mat object).
        /// This Mat object holds unmanaged memory and must be disposed.
        /// </summary>
        public Mat ProcessedImage { get; set; }

        /// <summary>
        /// Gets or sets the detected location of the player icon within the radar image (relative coordinates).
        /// This is a nullable Point structure.
        /// </summary>
        public Point? PlayerLocation { get; set; }

        /// <summary>
        /// Gets or sets the match value from the template matching process. 
        /// (SqDiffNormed mode is typically used, where a lower value is better).
        /// </summary>
        public double MatchValue { get; set; }

        /// <summary>
        /// Releases all unmanaged resources used by the <see cref="ProcessedImage"/> Mat object.
        /// </summary>
        public void Dispose()
        {
            // Dispose of the OpenCvSharp Mat object if it exists and has not been disposed already.
            ProcessedImage?.Dispose();

            // Set the reference to null to help the garbage collector clean up.
            ProcessedImage = null;
        }
    }
}