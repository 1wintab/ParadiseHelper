using OpenCvSharp;
using ParadiseHelper.AI.Weapon.WeaponReader;

namespace ParadiseHelper.AI.Weapon
{
    /// <summary>
    /// Manages the current weapon state, using template matching results and applying 
    /// smoothing/cooldown logic to ensure stability.
    /// </summary>
    public class WeaponManager
    {
        /// <summary>
        /// The number of consecutive 'Unknown' frames required before the weapon state 
        /// is officially reset to "Unknown", preventing rapid flickering.
        /// </summary>
        private const int WEAPON_CHANGE_COOLDOWN_FRAMES = 30;

        // Counter for consecutive frames where no weapon was successfully matched.
        private int _unknownCount = 0;

        // Holds the raw name from the latest successful detection (not smoothed).
        private string _lastDetectedWeaponName = "Unknown"; 

        /// <summary>
        /// The officially recognized weapon name, smoothed by the cooldown logic.
        /// </summary>
        public string LatestWeaponName { get; private set; } = "Unknown";

        /// <summary>
        /// The confidence score of the latest successful template match.
        /// </summary>
        public double LatestWeaponMatchValue { get; private set; } = 0.0;

        /// <summary>
        /// Detects the weapon in the given frame, updates the internal state, and applies smoothing logic.
        /// </summary>
        /// <param name="frame">The input video frame containing the weapon area.</param>
        /// <param name="showWeaponDebugImage">If true, the processed image is returned; otherwise, it is disposed.</param>
        /// <returns>The processed image area (OpenCvSharp Mat) if debug is enabled, or null.</returns>
        public Mat DetectWeapon(Mat frame, bool showWeaponDebugImage)
        {
            // Run the template matching algorithm on the frame.
            var weaponDetectionResult = TemplateMatchingWeaponDetector.DetectWeapon(frame);

            if (weaponDetectionResult.WeaponName == "Unknown")
            {
                // Increment the counter if no match was found above the confidence threshold.
                _unknownCount++;

                // If the cooldown threshold is reached, officially reset the weapon state to 'Unknown'.
                if (_unknownCount >= WEAPON_CHANGE_COOLDOWN_FRAMES)
                {
                    _lastDetectedWeaponName = "Unknown";
                    LatestWeaponName = "Unknown";
                }
            }
            else // A weapon was successfully matched above the detection threshold.
            {
                // Update the raw detection name.
                _lastDetectedWeaponName = weaponDetectionResult.WeaponName;

                // A successful detection immediately resets the unknown counter.
                _unknownCount = 0;

                // Immediately update the official weapon name. Smoothing only applies when transitioning TO 'Unknown'.
                LatestWeaponName = weaponDetectionResult.WeaponName;
            }

            // Always update the latest match confidence value.
            LatestWeaponMatchValue = weaponDetectionResult.MatchValue;

            // Handle the lifetime of the processed image based on the debug flag.
            if (showWeaponDebugImage)
            {
                // Return the Mat to the caller for display (caller is responsible for disposal).
                return weaponDetectionResult.ProcessedImage;
            }
            else
            {
                // Dispose of the Mat immediately if debugging is off to prevent memory leaks.
                weaponDetectionResult.ProcessedImage?.Dispose();
                return null;
            }
        }
    }
}