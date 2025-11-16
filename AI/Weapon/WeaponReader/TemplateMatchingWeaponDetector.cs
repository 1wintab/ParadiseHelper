using System;
using System.IO;
using System.Collections.Generic;
using OpenCvSharp;
using Core;
using ParadiseHelper.AI.Extinctions;
using ParadiseHelper.AI.ClickerAutoRunGame;

namespace ParadiseHelper.AI.Weapon.WeaponReader
{
    /// <summary>
    /// Static class responsible for detecting the current equipped weapon using OpenCV Template Matching 
    /// against a set of pre-loaded template images.
    /// </summary>
    public static class TemplateMatchingWeaponDetector
    {
        // A list to hold the pre-loaded template matcher objects for all known weapons.
        private static List<TemplateMatcherBinarization> _weaponTemplates = new List<TemplateMatcherBinarization>();

        // Static constructor. Initializes the class by loading all templates on first access.
        static TemplateMatchingWeaponDetector()
        {
            LoadTemplates();
        }

        /// <summary>
        /// Loads all weapon template images from the designated directory into <see cref="TemplateMatcherBinarization"/> 
        /// objects and stores them for later matching.
        /// </summary>
        private static void LoadTemplates()
        {
            // Check if the directory where templates are stored exists.
            if (!Directory.Exists(FilePaths.AI.Templates.WeaponDirectory))
            {
                Console.WriteLine($"Error: Template directory not found at path: {FilePaths.AI.Templates.WeaponDirectory}");
                return;
            }

            var templateFiles = Directory.GetFiles(FilePaths.AI.Templates.WeaponDirectory, "*.png");

            // Iterate over all found template files (e.g., .png) in the directory.
            foreach (var templateFile in templateFiles)
            {
                // Create an instance of the specific matcher class for each template file, 
                // using pre-defined coordinate and threshold settings.
                var matcher = new TemplateMatcherBinarization(
                    SettingsDetector.WEAPON_NAME_X,
                    SettingsDetector.WEAPON_NAME_Y,
                    SettingsDetector.WEAPON_NAME_WIDTH,
                    SettingsDetector.WEAPON_NAME_HEIGHT,
                    templateFile,
                    SettingsDetector.MATCH_THRESHOLD
                );

                // Add the initialized matcher instance to the static list.
                _weaponTemplates.Add(matcher);
            }
        }

        /// <summary>
        /// Attempts to detect the equipped weapon in the given screenshot frame by comparing it against 
        /// all loaded templates using template matching.
        /// </summary>
        /// <param name="frame">The <see cref="Mat"/> object representing the current game frame (screenshot).</param>
        /// <returns>A tuple containing the detected weapon name, the highest match score, and the binarized image area used for matching.</returns>
        public static (string WeaponName, double MatchValue, Mat ProcessedImage) DetectWeapon(Mat frame)
        {
            // Handle null frame or missing templates.
            if (frame == null || _weaponTemplates.Count == 0)
            {
                Console.WriteLine("Error: Input frame or weapon templates are missing.");
                return ("Unknown", 0.0, null);
            }

            // Variable to track the best match score found across all templates.
            double maxMatch = 0.0;
            
            // Variable to store the name of the template with the highest match.
            string detectedName = "Unknown";
            
            // Stores the processed area for the best match, must be disposed later.
            Mat processedImage = null;

            // Iterate through every pre-loaded weapon matcher object.
            foreach (var weaponMatcher in _weaponTemplates)
            {
                // Execute the template matching detection for the current weapon.
                var (name, matchValue, processedArea) = weaponMatcher.DetectTarget(frame);

                if (matchValue > maxMatch)
                {
                    maxMatch = matchValue;
                    detectedName = name;

                    // Dispose the previous best image to prevent a memory leak before assigning the new one.
                    processedImage?.Dispose();
                    processedImage = processedArea;
                }
                else
                {
                    // If the match is worse, dispose the current processed area immediately to free resources.
                    processedArea?.Dispose();
                }
            }

            // Check if the best match meets the minimum required threshold.
            if (maxMatch >= SettingsDetector.MATCH_THRESHOLD)
            {
                // Return the best result if it meets the minimum threshold.
                return (detectedName, maxMatch, processedImage);
            }
            else
            {
                // Return "Unknown" if the best match is below the required threshold.
                // processedImage will contain the processed image from the best *attempt*, even if it failed the threshold.
                return ("Unknown", maxMatch, processedImage);
            }
        }
    }
}