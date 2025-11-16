using System;
using OpenCvSharp;
using ParadiseHelper.AI.Core.Settings;

namespace ParadiseHelper.AI.Video.GameVideoSource
{
    /// <summary>
    /// Handles necessary preprocessing steps (resizing, color conversion) for video frames 
    /// before they are passed to the neural network detector.
    /// </summary>
    /// <remarks>
    /// This implementation uses OpenCvSharp for image manipulation.
    /// </remarks>
    public class GpuPreprocessor : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GpuPreprocessor"/> class.
        /// </summary>
        public GpuPreprocessor() { }

        /// <summary>
        /// Processes the input frame to meet the model's input requirements (resize and color conversion).
        /// </summary>
        /// <param name="input">The raw input frame (OpenCvSharp Mat).</param>
        /// <returns>A new Mat containing the processed frame in RGB format, or null if the input is invalid.</returns>
        public Mat ProcessFrame(Mat input)
        {
            if (input == null || input.Empty())
            {
                return null;
            }

            // Reference to the Mat that holds the resized or original data.
            Mat processedMat = input;

            // Check if resizing is necessary (e.g., if model expects a specific size like 512x512).
            if (input.Width != ModelSettings.Width || input.Height != ModelSettings.Height)
            {
                // Create a new Mat for the resized image.
                processedMat = new Mat(ModelSettings.Height, ModelSettings.Width, MatType.CV_8UC3);

                // Perform the resize operation using linear interpolation.
                Cv2.Resize(input, processedMat, new Size(ModelSettings.Width, ModelSettings.Height), 0, 0, InterpolationFlags.Linear);
            }

            // Convert the image from BGR (OpenCV default) to RGB (common neural network model input).
            var rgbMat = new Mat();
            Cv2.CvtColor(processedMat, rgbMat, ColorConversionCodes.BGR2RGB);

            // Dispose of the intermediate resized Mat if a new one was created.
            // This ensures we only dispose of Mats created within this method, not the input Mat passed by the caller.
            if (processedMat != input)
            {
                processedMat.Dispose();
            }

            // The calling code (e.g., YoloDetector.Detect) is responsible for disposing of the returned rgbMat.
            return rgbMat;
        }

        /// <summary>
        /// Disposes of any resources held by the preprocessor.
        /// </summary>
        /// <remarks>
        /// The class contains no unmanaged resources besides the temporary Mats created and disposed 
        /// within <see cref="ProcessFrame"/>, so this method is intentionally empty.
        /// </remarks>
        public void Dispose() { }
    }
}