using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using OpenCvSharp;

namespace ParadiseHelper.AI.Orientation.Map
{
    /// <summary>
    /// Static class responsible for map identification using template matching.
    /// </summary>
    public static class MapDetection
    {
        // Dictionary to store map template images (Mat objects), keyed by map name.
        private static Dictionary<string, Mat> _mapTemplates = new Dictionary<string, Mat>();
        
        // Flag indicating whether map templates have been successfully loaded.
        private static bool _isTemplatesLoaded = false;

        /// <summary>
        /// Initializes the MapDetection by loading all map template images from the specified folder.
        /// </summary>
        /// <param name="templatesFolderPath">The file path to the folder containing map templates (PNG files).</param>
        public static void Initialize(string templatesFolderPath)
        {
            // Prevent re-initialization if templates are already loaded.
            if (_isTemplatesLoaded) return;

            _mapTemplates.Clear();

            if (!Directory.Exists(templatesFolderPath))
            {
                // Log an error if the templates folder is missing.
                Console.WriteLine($"Error: Templates folder not found at: {templatesFolderPath}");
                
                return;
            }

            // Get all PNG files in the templates folder.
            var files = Directory.GetFiles(templatesFolderPath, "*.png");
            foreach (var file in files)
            {
                string mapName = Path.GetFileNameWithoutExtension(file);

                // Load the template image in grayscale mode for efficient matching.
                Mat template = Cv2.ImRead(file, ImreadModes.Grayscale);

                if (template.Empty())
                {
                    // Log a warning if a template file failed to load.
                    Console.WriteLine($"Warning: Failed to load map template from: {file}");
                    
                    continue;
                }

                // Store the loaded template Mat object.
                _mapTemplates.Add(mapName, template);
            }

            // Set the flag if any templates were successfully loaded.
            if (_mapTemplates.Any()) _isTemplatesLoaded = true;
        }

        /// <summary>
        /// Attempts to detect the current map in the provided radar image using template matching.
        /// </summary>
        /// <param name="radarImage">The input image (usually BGR) containing the radar area to search.</param>
        /// <returns>A <see cref="MapDetectionResult"/> containing the best match information.</returns>
        public static MapDetectionResult DetectMap(Mat radarImage)
        {
            // Check for necessary prerequisites and valid input image.
            if (!_isTemplatesLoaded || radarImage == null || radarImage.Empty())
            {
                // Return a default "Unknown" result if templates are missing or input is invalid.
                return new MapDetectionResult
                {
                    MapName = "Unknown",
                    MatchValue = 1.0, // 1.0 indicates the worst possible match for TemplateMatchModes.SqDiffNormed.
                    BoundingBox = new Rect()
                };
            }

            // Use 'using' block for grayRadar Mat to ensure its unmanaged memory is released promptly.
            using (Mat grayRadar = new Mat())
            {
                // Convert the input BGR image to grayscale.
                Cv2.CvtColor(radarImage, grayRadar, ColorConversionCodes.BGR2GRAY);

                // Initialize best match variables. 1.0 is the worst score for SqDiffNormed.
                double bestMatchValue = 1.0;
                string bestMatchName = "Unknown";
                Rect bestMatchBoundingBox = new Rect();

                // Iterate through all loaded map templates to find the best match.
                foreach (var kvp in _mapTemplates)
                {
                    string mapName = kvp.Key;
                    Mat template = kvp.Value;

                    // Skip matching if the template is larger than the search image (Template Matching constraint).
                    if (grayRadar.Width < template.Width || grayRadar.Height < template.Height)
                    {
                        continue;
                    }

                    // Use 'using' block for the result Mat to ensure its unmanaged memory is released promptly.
                    using (Mat result = new Mat())
                    {
                        // Perform template matching using Squared Difference Normalized (TM_SQDIFF_NORMED).
                        // Lower value (closer to 0) means a better match.
                        Cv2.MatchTemplate(grayRadar, template, result, TemplateMatchModes.SqDiffNormed);

                        // Find the minimum and maximum match values and their locations.
                        Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out Point minLoc, out Point maxLoc);

                        // Check if the current minimum value is better (lower) than the global best match.
                        if (minVal < bestMatchValue)
                        {
                            bestMatchValue = minVal;
                            bestMatchName = mapName;

                            // Calculate the bounding box based on the top-left corner of the best match.
                            bestMatchBoundingBox = new Rect(minLoc.X, minLoc.Y, template.Width, template.Height);
                        }
                    }
                }

                // Return the best detection result found across all templates.
                return new MapDetectionResult
                {
                    MapName = bestMatchName,
                    MatchValue = bestMatchValue,
                    BoundingBox = bestMatchBoundingBox
                };
            }
        }
    }
}