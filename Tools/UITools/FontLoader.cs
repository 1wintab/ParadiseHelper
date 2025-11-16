using System;
using System.IO;
using System.Drawing;
using System.Drawing.Text;
using System.Collections.Generic;
using Core;

namespace ParadiseHelper.Tools.UITools
{
    /// <summary>
    /// Static utility class responsible for loading custom font files from disk
    /// and providing cached Font objects for use throughout the application.
    /// This prevents repeated file loading and resource leaks.
    /// </summary>
    public static class FontLoader
    {
        // Private collection for VAG_World.ttf font family.
        private static PrivateFontCollection _vagFontCollection;
        
        // Private collection for VAG_Rounded.ttf font family.
        private static PrivateFontCollection _vagRoundedFont;
        
        // Private collection for BIPs.ttf font family.
        private static PrivateFontCollection _vagBIPsFont;

        // Cache storage for all generated Font objects, indexed by a unique key (fileName|size|style).
        private static readonly Dictionary<string, Font> _fontCache = new Dictionary<string, Font>();

        /// <summary>
        /// Core method to load a font file, utilizing caching to prevent redundant file operations.
        /// If the font collection is not yet loaded, it reads the font data from disk.
        /// </summary>
        /// <param name="fileName">The name of the font file (e.g., "VAG_World.ttf").</param>
        /// <param name="collection">A reference to the static <see cref="PrivateFontCollection"/> that holds the font data.</param>
        /// <param name="size">The desired font size in points.</param>
        /// <param name="style">The desired font style (<see cref="FontStyle"/>).</param>
        /// <returns>A cached or newly created <see cref="Font"/> object.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the specified font file is missing on disk.</exception>
        private static Font LoadFont(string fileName, ref PrivateFontCollection collection, float size, FontStyle style)
        {
            // Create a unique key from file name, size, and style.
            string key = $"{fileName}|{size}|{style}";

            // 1. Check if the Font object is already in the cache.
            Font cached;
            if (_fontCache.TryGetValue(key, out cached))
            {
                return cached;
            }

            // 2. If the font collection has not been initialized, load the font file from disk.
            if (collection == null)
            {
                collection = new PrivateFontCollection();

                string fontPath = Path.Combine(FilePaths.Standard.FontsDirectory, fileName);

                if (!File.Exists(fontPath))
                {
                    // Throws an exception if the font file is missing, stopping execution.
                    throw new FileNotFoundException($"Font file not found at path: {fontPath}", fontPath);
                }

                // Load the font data into the private collection.
                collection.AddFontFile(fontPath);
            }

            // 3. Create the Font object from the loaded collection, save it to the cache, and return it.
            // collection.Families[0] is used because private collections typically contain one family per AddFontFile call.
            Font font = new Font(collection.Families[0], size, style);
            _fontCache[key] = font;

            return font;
        }

        /// <summary>
        /// Gets the VAG World font with the specified size and style.
        /// </summary>
        /// <param name="size">The desired font size.</param>
        /// <param name="style">The desired font style (defaults to <see cref="FontStyle.Regular"/>).</param>
        /// <returns>The VAG World <see cref="Font"/> object.</returns>
        public static Font VAGWorld(float size, FontStyle style = FontStyle.Regular)
        {
            return LoadFont("VAG_World.ttf", ref _vagFontCollection, size, style);
        }

        /// <summary>
        /// Gets the VAG Rounded Bold font with the specified size.
        /// This file likely contains only the Bold variant.
        /// </summary>
        /// <param name="size">The desired font size.</param>
        /// <param name="style">The desired font style (defaults to <see cref="FontStyle.Bold"/>).</param>
        /// <returns>The VAG Rounded Bold <see cref="Font"/> object.</returns>
        public static Font VAGRoundedBold(float size, FontStyle style = FontStyle.Bold)
        {
            return LoadFont("VAG_Rounded.ttf", ref _vagRoundedFont, size, style);
        }

        /// <summary>
        /// Gets the BIPs font with the specified size.
        /// </summary>
        /// <param name="size">The desired font size.</param>
        /// <param name="style">The desired font style (defaults to <see cref="FontStyle.Bold"/>).</param>
        /// <returns>The BIPs <see cref="Font"/> object.</returns>
        public static Font BIPs(float size, FontStyle style = FontStyle.Bold)
        {
            return LoadFont("BIPs.ttf", ref _vagBIPsFont, size, style);
        }
    }
}