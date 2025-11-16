using System.IO;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;

// NOTE: This class assumes the containing assembly (ParadiseHelper) has a reference to
// ScreenCapturer from the parent namespace, and Emgu.CV is correctly linked.

namespace ParadiseHelper.SteamAuth
{
    /// <summary>
    /// Utility class for image recognition using Template Matching provided by the Emgu CV library (a .NET wrapper for OpenCV).
    /// </summary>
    internal static class TemplateMatcher
    {
        /// <summary>
        /// Captures the entire desktop screen and checks if a specified template image
        /// is visible on the screen above a given confidence threshold.
        /// </summary>
        /// <param name="templatePath">The file path to the template image (e.g., a button or icon) to search for.</param>
        /// <param name="threshold">The minimum confidence score (0.0 to 1.0) required to consider the template as found.</param>
        /// <returns><c>true</c> if the template is found with a confidence greater than or equal to the threshold; otherwise, <c>false</c>.</returns>
        public static bool IsTemplateVisible(string templatePath, double threshold)
        {
            // Use 'using' statements to ensure proper disposal of the Bitmap objects.
            using (var screen = ScreenCapturer.CaptureDesktop())
            {
                // Ensure the template file exists before attempting to load it.
                if (!File.Exists(templatePath))
                {
                    // In a production environment, you might throw an exception or log an error here.
                    return false;
                }

                using (var template = new Bitmap(templatePath))
                {
                    // Attempt to find the best match location in memory.
                    var match = FindMatchInMemory(screen, template, threshold);

                    // If a Point is returned (not null), a match was found.
                    return match.HasValue;
                }
            }
        }

        /// <summary>
        /// Core function: Finds the best location of the template image within the screen image
        /// using OpenCV's template matching algorithm (TM_CCOEFF_NORMED).
        /// </summary>
        /// <param name="screenBmp">The <see cref="Bitmap"/> of the area to search within (the "haystack").</param>
        /// <param name="templateBmp">The <see cref="Bitmap"/> of the image to find (the "needle").</param>
        /// <param name="threshold">The minimum confidence score (0.0 to 1.0) required for a match.</param>
        /// <returns>A nullable <see cref="Point"/> representing the top-left coordinate of the best match
        /// if the score meets the threshold; otherwise, <c>null</c>.</returns>
        public static Point? FindMatchInMemory(Bitmap screenBmp, Bitmap templateBmp, double threshold)
        {
            // Mat objects are OpenCV's core image containers. They must be disposed of.
            using (var result = new Mat())
            using (var screen = new Mat())
            using (var template = new Mat())
            {
                // --- 1. Convert System.Drawing.Bitmap to Emgu CV Mat ---

                // Bitmaps must be converted to a byte array (e.g., PNG format) before Emgu CV can decode them into Mat objects.
                using (MemoryStream msScreen = new MemoryStream())
                using (MemoryStream msTemplate = new MemoryStream())
                {
                    screenBmp.Save(msScreen, System.Drawing.Imaging.ImageFormat.Png);
                    templateBmp.Save(msTemplate, System.Drawing.Imaging.ImageFormat.Png);

                    byte[] screenBytes = msScreen.ToArray();
                    byte[] templateBytes = msTemplate.ToArray();

                    // Imdecode converts the image byte array into a Mat structure, loading it in color mode.
                    CvInvoke.Imdecode(screenBytes, ImreadModes.Color, screen);
                    CvInvoke.Imdecode(templateBytes, ImreadModes.Color, template);
                }

                // --- 2. Perform Template Matching ---

                // MatchTemplate performs the sliding window search.
                // TM_CCOEFF_NORMED (Normalized Cross-Correlation) is generally robust,
                // producing values from -1.0 (perfect mismatch) to 1.0 (perfect match).
                CvInvoke.MatchTemplate(screen, template, result, TemplateMatchingType.CcoeffNormed);

                // --- 3. Analyze the Results ---

                // MinMax finds the minimum and maximum values (scores) in the result matrix
                // and their respective locations (where the best and worst matches occurred).
                result.MinMax(out double[] minVals, out double[] maxVals,
                              out Point[] minLocs, out Point[] maxLocs);

                // For TM_CCOEFF_NORMED, the maximum value (maxVals[0]) represents the best match score.
                double bestMatchScore = maxVals[0];
                Point matchLocation = maxLocs[0];

                // Check if the highest score meets the required confidence threshold.
                if (bestMatchScore >= threshold)
                {
                    // Return the top-left coordinate of the best match found.
                    return matchLocation;
                }
            }

            // If the code reaches here, no match was found with sufficient confidence.
            return null;
        }
    }
}