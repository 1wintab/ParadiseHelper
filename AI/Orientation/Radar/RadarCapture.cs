using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using OpenCvSharp;

namespace ParadiseHelper.AI.Orientation.Radar
{
    /// <summary>
    /// Static class responsible for processing the radar section of the game screen
    /// and performing template matching to find player icons.
    /// </summary>
    public static class RadarCapture
    {
        // Constants defining the fixed coordinates and size of the radar area in the main frame.
        public const int RADAR_X = 23;
        public const int RADAR_Y = 23;
        public const int RADAR_WIDTH = 214;
        public const int RADAR_HEIGHT = 214;

        // Dictionaries to hold active player icon templates (Mat objects).
        private static Dictionary<string, Mat> _playerIconTemplates = new Dictionary<string, Mat>();
       
        // Dictionaries to hold dead player icon templates (Mat objects).
        private static Dictionary<string, Mat> _deadPlayerIconTemplates = new Dictionary<string, Mat>();

        // Flag indicating whether all templates have been successfully loaded.
        private static bool _isTemplatesLoaded = false;
        
        // Stored width of the loaded templates.
        private static int _templateWidth;
        
        // Stored height of the loaded templates.
        private static int _templateHeight;

        /// <summary>
        /// Initializes the <see cref="RadarCapture"/> class by loading all necessary player icon templates from disk.
        /// If templates are already loaded, this method returns immediately.
        /// </summary>
        /// <param name="ctTemplatePath">File path for the Counter-Terrorist (CT) active player icon template.</param>
        /// <param name="tTemplatePath">File path for the Terrorist (T) active player icon template.</param>
        /// <param name="ctDeadTemplatePath">File path for the CT dead player icon template.</param>
        /// <param name="tDeadTemplatePath">File path for the T dead player icon template.</param>
        public static void Initialize(string ctTemplatePath, string tTemplatePath, string ctDeadTemplatePath, string tDeadTemplatePath)
        {
            if (_isTemplatesLoaded) return;

            // Perform cleanup to ensure no previous Mats are left unmanaged before loading new ones.
            Cleanup();

            try
            {
                // Load templates for active players
                LoadPlayerTemplate("ct_mark", ctTemplatePath, _playerIconTemplates);
                LoadPlayerTemplate("t_mark", tTemplatePath, _playerIconTemplates);

                // Load templates for dead players
                LoadPlayerTemplate("ct_dead_mark", ctDeadTemplatePath, _deadPlayerIconTemplates);
                LoadPlayerTemplate("t_dead_mark", tDeadTemplatePath, _deadPlayerIconTemplates);

                if (_playerIconTemplates.Count > 0)
                {
                    _isTemplatesLoaded = true;

                    // Store the dimensions of the first template for convenience
                    var firstPlayerTemplate = _playerIconTemplates.Values.First();

                    _templateWidth = firstPlayerTemplate.Width;
                    _templateHeight = firstPlayerTemplate.Height;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing RadarCapture: {ex.Message}");
                // Clean up any partially loaded resources in case of an exception.
                Cleanup();
            }
        }

        /// <summary>
        /// Releases all managed <see cref="Mat"/> resources held by the static template dictionaries.
        /// This should be called when the application shuts down or templates are reloaded.
        /// </summary>
        public static void Cleanup()
        {
            foreach (var template in _playerIconTemplates.Values)
            {
                template.Dispose();
            }
            _playerIconTemplates.Clear();

            foreach (var template in _deadPlayerIconTemplates.Values)
            {
                template.Dispose();
            }
            _deadPlayerIconTemplates.Clear();

            _isTemplatesLoaded = false;
        }

        /// <summary>
        /// Gets the width of the loaded player icon templates.
        /// </summary>
        /// <returns>The template width in pixels.</returns>
        public static int GetTemplateWidth() => _templateWidth;

        /// <summary>
        /// Gets the height of the loaded player icon templates.
        /// </summary>
        /// <returns>The template height in pixels.</returns>
        public static int GetTemplateHeight() => _templateHeight;

        /// <summary>
        /// Crops the radar area from the main game frame, applies a circular mask, and searches for the player icon using template matching.
        /// </summary>
        /// <param name="frame">The full screen capture Mat object.</param>
        /// <returns>A <see cref="RadarDetectionResult"/> containing the processed circular radar image, player location, and match confidence.</returns>
        public static RadarDetectionResult ProcessRadarAndFindPlayer(Mat frame)
        {
            // Input validation: check for null/empty frame or if radar area is outside the frame boundaries.
            if (frame == null || frame.Empty() ||
                RADAR_X + RADAR_WIDTH > frame.Width ||
                RADAR_Y + RADAR_HEIGHT > frame.Height)
            {
                return new RadarDetectionResult
                {
                    ProcessedImage = null,
                    PlayerLocation = null,
                    MatchValue = 1.0
                };
            }
            try
            {
                // 1. Crop the square radar area from the main frame using a Rect.
                using (Mat radarSquare = new Mat(frame, new Rect(RADAR_X, RADAR_Y, RADAR_WIDTH, RADAR_HEIGHT)))
                {
                    // 2. Create a circular mask to isolate the circular radar content.
                    using (Mat mask = new Mat(RADAR_HEIGHT, RADAR_WIDTH, MatType.CV_8UC1, new Scalar(0)))
                    {
                        int centerX = RADAR_WIDTH / 2;
                        int centerY = RADAR_HEIGHT / 2;

                        // Radius slightly smaller than the half-width/height to avoid corner artifacts.
                        int radius = Math.Min(centerX, centerY) - 2;

                        // Draw a filled white circle on the black mask.
                        Cv2.Circle(mask, new Point(centerX, centerY), radius, new Scalar(255), -1);

                        // 3. Apply the mask to get the circular radar image.
                        using (Mat circularRadar = new Mat())
                        {
                            // Copy the radarSquare to circularRadar, applying the mask.
                            radarSquare.CopyTo(circularRadar, mask);

                            // 4. Find the player icon within the circular radar area.
                            var playerResult = FindPlayerIcon(circularRadar);

                            return new RadarDetectionResult
                            {
                                // Clone the processed image before the 'using' block exits to return a valid Mat.
                                ProcessedImage = circularRadar.Clone(),
                                PlayerLocation = playerResult.Item1,
                                MatchValue = playerResult.Item2
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing radar frame: {ex.Message}");         
                return new RadarDetectionResult { ProcessedImage = null, PlayerLocation = null, MatchValue = 1.0 };
            }
        }

        /// <summary>
        /// Determines the direction vector of the player by finding the brightest point on the icon image.
        /// </summary>
        /// <param name="playerIconImage">A cropped image of the player icon (e.g., 5x5 pixels).</param>
        /// <returns>A <see cref="Point"/> representing the displacement from the icon center, or null if the input is invalid.</returns>
        public static Point? DeterminePlayerDirection(Mat playerIconImage)
        {
            if (playerIconImage == null || playerIconImage.Empty()) return null;

            using (var grayImage = new Mat())
            {
                // Convert to grayscale for simpler brightness analysis.
                Cv2.CvtColor(playerIconImage, grayImage, ColorConversionCodes.BGR2GRAY);

                // Find the location of the minimum and maximum pixel intensity.
                Cv2.MinMaxLoc(grayImage, out double minVal, out double maxVal, out Point minLoc, out Point maxLoc);

                int centerX = grayImage.Width / 2;
                int centerY = grayImage.Height / 2;

                // The direction vector is the displacement of the brightest spot (maxLoc) from the center.
                Point directionVector = new Point(maxLoc.X - centerX, maxLoc.Y - centerY);

                return directionVector;
            }
        }

        // Helper function to load a player template from a file path and store a clone in the dictionary.
        private static void LoadPlayerTemplate(string name, string path, Dictionary<string, Mat> templateDictionary)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"Player icon template not found at {path}");
                return;
            }

            // Load the template using 'using' for automatic Dispose of the temporary Mat.
            using Mat template = Cv2.ImRead(path, ImreadModes.Unchanged);

            if (template.Empty())
            {
                Console.WriteLine($"Failed to load player icon template: {path}");
                return;
            }

            // Logic for conversion and storage.
            if (template.Channels() == 4)
            {
                // Convert BGRA (with alpha channel) to BGR before storing.
                Mat bgrTemplate = new Mat();
                Cv2.CvtColor(template, bgrTemplate, ColorConversionCodes.BGRA2BGR);
                templateDictionary[name] = bgrTemplate;
            }
            else
            {
                // Simply clone the existing Mat for storage.
                templateDictionary[name] = template.Clone();
            }
        }

        // Performs template matching for active player icons within the radar frame.
        private static Tuple<Point?, double> FindPlayerIcon(Mat radarFrame)
        {
            if (!_isTemplatesLoaded || radarFrame == null || radarFrame.Empty())
            {
                return new Tuple<Point?, double>(null, 1.0);
            }

            Point? bestLocation = null;
            double minVal = 1.0;

            // Iterate through active player templates to find the best match.
            foreach (var template in _playerIconTemplates.Values)
            {
                using (var result = new Mat())
                {
                    // MatchTemplate using Squared Difference Normalized (SqDiffNormed).
                    Cv2.MatchTemplate(radarFrame, template, result, TemplateMatchModes.SqDiffNormed);
                    Cv2.MinMaxLoc(result, out double currentMinVal, out double maxVal, out Point minLoc, out Point maxLoc);

                    // SqDiffNormed: a lower value indicates a better match (closer to 0).
                    if (currentMinVal < minVal)
                    {
                        minVal = currentMinVal;
                        bestLocation = minLoc;
                    }
                }
            }
            return new Tuple<Point?, double>(bestLocation, minVal);
        }

        /// <summary>
        /// Detects the presence and location of a dead player icon within the radar frame.
        /// </summary>
        /// <param name="radarFrame">The circular radar image Mat object.</param>
        /// <returns>A <see cref="Tuple{T1, T2}"/> containing the best matching location (<see cref="Point"/>) and the match confidence value (double).</returns>
        public static Tuple<Point?, double> DetectDeath(Mat radarFrame)
        {
            if (!_isTemplatesLoaded || radarFrame == null || radarFrame.Empty())
            {
                return new Tuple<Point?, double>(null, 1.0);
            }

            double minVal = 1.0;
            Point? bestLocation = null;

            // Iterate through dead player templates to find the best match.
            foreach (var template in _deadPlayerIconTemplates.Values)
            {
                using (var result = new Mat())
                {
                    // MatchTemplate using Squared Difference Normalized (SqDiffNormed).
                    Cv2.MatchTemplate(radarFrame, template, result, TemplateMatchModes.SqDiffNormed);
                    Cv2.MinMaxLoc(result, out double currentMinVal, out _, out Point minLoc, out _);

                    // SqDiffNormed: a lower value indicates a better match (closer to 0).
                    if (currentMinVal < minVal)
                    {
                        minVal = currentMinVal;
                        bestLocation = minLoc;
                    }
                }
            }

            return new Tuple<Point?, double>(bestLocation, minVal);
        }
    }
}