using System.IO;
using OpenCvSharp;

namespace ParadiseHelper.AI.Extinctions
{
    /// <summary>
    /// A utility class for template matching that uses binarization (thresholding)
    /// on both the template and the target frame area for detection.
    /// This is particularly useful for finding UI elements with high contrast.
    /// </summary>
    public class TemplateMatcherBinarization
    {
        /// <summary>
        /// Gets the X-coordinate of the top-left corner of the area to scan on the frame.
        /// </summary>
        public int TargetX { get; private set; }

        /// <summary>
        /// Gets the Y-coordinate of the top-left corner of the area to scan on the frame.
        /// </summary>
        public int TargetY { get; private set; }

        /// <summary>
        /// Gets the width of the target area to scan.
        /// </summary>
        public int TargetWidth { get; private set; }

        /// <summary>
        /// Gets the height of the target area to scan.
        /// </summary>
        public int TargetHeight { get; private set; }

        /// <summary>
        /// Gets the processed (binarized) image template used for matching.
        /// </summary>
        public Mat TemplateFile { get; private set; }

        /// <summary>
        /// Gets the name derived from the template file path (used for identification).
        /// </summary>
        public string TemplateName { get; private set; }

        /// <summary>
        /// Gets the minimum match value (0.0 to 1.0) required to consider a detection successful.
        /// </summary>
        public double MatchThreshold { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateMatcherBinarization"/> class.
        /// Loads and processes the template file.
        /// </summary>
        /// <param name="buttonX">The X-coordinate of the scan area.</param>
        /// <param name="buttonY">The Y-coordinate of the scan area.</param>
        /// <param name="buttonWidth">The width of the scan area.</param>
        /// <param name="buttonHeight">The height of the scan area.</param>
        /// <param name="templatePath">The file path to the template image.</param>
        /// <param name="button_MatchThreshold">The minimum correlation required for a match.</param>
        public TemplateMatcherBinarization(int buttonX, int buttonY, int buttonWidth, int buttonHeight, string templatePath, double button_MatchThreshold)
        {
            TargetX = buttonX;
            TargetY = buttonY;
            TargetWidth = buttonWidth;
            TargetHeight = buttonHeight;
            MatchThreshold = button_MatchThreshold;
            LoadTemplate(templatePath);
        }

        /// <summary>
        /// Loads the template image from the specified path, converts it to grayscale,
        /// and applies Otsu's binarization for high-contrast matching.
        /// </summary>
        /// <param name="templatePath">The file path to the template image.</param>
        private void LoadTemplate(string templatePath)
        {
            if (!File.Exists(templatePath)) return;

            // Load the template as grayscale
            using var template = Cv2.ImRead(templatePath, ImreadModes.Grayscale);

            if (template != null && !template.Empty())
            {
                using var binaryTemplate = new Mat();
                // Apply Otsu's thresholding to the template for binarization
                Cv2.Threshold(template, binaryTemplate, 128, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                // Clone the processed image to the member field and store the name
                TemplateFile = binaryTemplate.Clone();
                TemplateName = Path.GetFileNameWithoutExtension(templatePath);
            }
        }

        /// <summary>
        /// Attempts to detect the target template within the defined area of the input frame.
        /// Applies adaptive thresholding and inversion to the target area before matching.
        /// </summary>
        /// <param name="frame">The full source image (e.g., a screen capture) as an OpenCV Mat.</param>
        /// <returns>A tuple containing the detected name ("Unknown" if below threshold), the match value, and the processed target image for debugging.</returns>
        public (string TargetName, double MatchValue, Mat ProcessedImage) DetectTarget(Mat frame)
        {
            // Check for invalid input or missing template
            if (frame == null || TemplateFile == null || TemplateFile.Empty())
            {
                return ("Unknown", 0.0, null);
            }

            // Check if the target area is outside the bounds of the current frame
            if (TargetX + TargetWidth > frame.Width || TargetY + TargetHeight > frame.Height)
            {
                return ("Unknown", 0.0, null);
            }

            // 1. Crop the target area from the full frame
            using var targetArea = new Mat(frame, new Rect(TargetX, TargetY, TargetWidth, TargetHeight));

            // 2. Convert to Grayscale
            using var grayTargetArea = new Mat();
            // Assuming the input 'frame' is BGR (typical for screen capture)
            Cv2.CvtColor(targetArea, grayTargetArea, ColorConversionCodes.BGR2GRAY);

            // 3. Apply Adaptive Thresholding (Gaussian C) to handle variable lighting/backgrounds
            using var processedTargetArea = new Mat();
            Cv2.AdaptiveThreshold(grayTargetArea, processedTargetArea, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);

            // Invert the image (black becomes white, white becomes black) to match potential template inversion
            Cv2.BitwiseNot(processedTargetArea, processedTargetArea);

            // 4. Perform Template Matching using the normalized correlation method
            using var result = new Mat();
            Cv2.MatchTemplate(processedTargetArea, TemplateFile, result, TemplateMatchModes.CCorrNormed);

            // Find the maximum match value across the result map
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);

            // 5. Determine result based on threshold
            string templateName = maxVal >= MatchThreshold ? TemplateName : "Unknown";

            // Return the name, match value, and a clone of the processed image for debugging/UI
            // A clone is returned so the calling method is responsible for its disposal.
            return (templateName, maxVal, processedTargetArea.Clone());
        }
    }
}