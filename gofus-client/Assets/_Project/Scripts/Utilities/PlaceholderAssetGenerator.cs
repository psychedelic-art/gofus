using UnityEngine;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GOFUS.Utilities
{
    /// <summary>
    /// Generates placeholder sprites for character classes when real assets are not available
    /// </summary>
    public class PlaceholderAssetGenerator : MonoBehaviour
    {
        private static readonly Dictionary<int, Color> classColors = new Dictionary<int, Color>
        {
            { 1, new Color(0.6f, 0.4f, 0.2f) }, // Feca - Brown
            { 2, new Color(0.2f, 0.6f, 0.3f) }, // Osamodas - Green
            { 3, new Color(0.8f, 0.7f, 0.3f) }, // Enutrof - Gold
            { 4, new Color(0.4f, 0.2f, 0.5f) }, // Sram - Purple
            { 5, new Color(0.3f, 0.4f, 0.7f) }, // Xelor - Blue
            { 6, new Color(0.4f, 0.6f, 0.3f) }, // Ecaflip - Light Green
            { 7, new Color(0.7f, 0.3f, 0.5f) }, // Eniripsa - Pink
            { 8, new Color(0.8f, 0.3f, 0.2f) }, // Iop - Red
            { 9, new Color(0.3f, 0.6f, 0.7f) }, // Cra - Cyan
            { 10, new Color(0.3f, 0.5f, 0.3f) }, // Sadida - Dark Green
            { 11, new Color(0.5f, 0.2f, 0.2f) }, // Sacrieur - Dark Red
            { 12, new Color(0.5f, 0.5f, 0.5f) }  // Pandawa - Gray
        };

        private static readonly Dictionary<int, string> classNames = new Dictionary<int, string>
        {
            { 1, "Feca" },
            { 2, "Osamodas" },
            { 3, "Enutrof" },
            { 4, "Sram" },
            { 5, "Xelor" },
            { 6, "Ecaflip" },
            { 7, "Eniripsa" },
            { 8, "Iop" },
            { 9, "Cra" },
            { 10, "Sadida" },
            { 11, "Sacrieur" },
            { 12, "Pandawa" }
        };

        /// <summary>
        /// Generate a placeholder sprite for a character class
        /// </summary>
        public static Sprite GeneratePlaceholderSprite(int classId, int width = 64, int height = 64)
        {
            // Create texture
            Texture2D texture = new Texture2D(width, height);

            // Get class color
            Color classColor = classColors.ContainsKey(classId) ? classColors[classId] : Color.gray;

            // Fill with base color
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = classColor;
            }

            // Add simple character silhouette
            DrawCharacterSilhouette(pixels, width, height, Color.white);

            // Add class number in corner
            DrawClassNumber(pixels, width, height, classId, Color.white);

            // Apply pixels to texture
            texture.SetPixels(pixels);
            texture.Apply();

            // Create sprite
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = $"Placeholder_{GetClassName(classId)}";

            return sprite;
        }

        /// <summary>
        /// Draw a simple character silhouette
        /// </summary>
        private static void DrawCharacterSilhouette(Color[] pixels, int width, int height, Color color)
        {
            // Draw simple stick figure
            int centerX = width / 2;
            int centerY = height / 2;

            // Head (circle)
            int headRadius = width / 8;
            int headY = centerY + height / 4;
            DrawCircle(pixels, width, height, centerX, headY, headRadius, color);

            // Body (vertical line)
            for (int y = centerY - height / 8; y < headY - headRadius; y++)
            {
                if (y >= 0 && y < height)
                {
                    int index = y * width + centerX;
                    if (index >= 0 && index < pixels.Length)
                        pixels[index] = color;
                }
            }

            // Arms (horizontal line)
            int armY = centerY + height / 8;
            for (int x = centerX - width / 6; x <= centerX + width / 6; x++)
            {
                if (x >= 0 && x < width && armY >= 0 && armY < height)
                {
                    int index = armY * width + x;
                    if (index >= 0 && index < pixels.Length)
                        pixels[index] = color;
                }
            }

            // Legs (two diagonal lines)
            for (int i = 0; i < height / 6; i++)
            {
                int legY = centerY - height / 8 - i;
                if (legY >= 0 && legY < height)
                {
                    // Left leg
                    int leftX = centerX - i / 2;
                    if (leftX >= 0 && leftX < width)
                    {
                        int index = legY * width + leftX;
                        if (index >= 0 && index < pixels.Length)
                            pixels[index] = color;
                    }

                    // Right leg
                    int rightX = centerX + i / 2;
                    if (rightX >= 0 && rightX < width)
                    {
                        int index = legY * width + rightX;
                        if (index >= 0 && index < pixels.Length)
                            pixels[index] = color;
                    }
                }
            }
        }

        /// <summary>
        /// Draw a circle
        /// </summary>
        private static void DrawCircle(Color[] pixels, int width, int height, int centerX, int centerY, int radius, Color color)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                        if (distance <= radius)
                        {
                            int index = y * width + x;
                            if (index >= 0 && index < pixels.Length)
                                pixels[index] = color;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draw class number in corner
        /// </summary>
        private static void DrawClassNumber(Color[] pixels, int width, int height, int classId, Color color)
        {
            // Simple number display in top-left corner
            // This is a simplified representation - in production, use proper font rendering
            int startX = 4;
            int startY = height - 12;
            int digitSize = 3;

            string number = classId.ToString();
            foreach (char digit in number)
            {
                DrawDigit(pixels, width, height, startX, startY, digit, color, digitSize);
                startX += digitSize + 2;
            }
        }

        /// <summary>
        /// Draw a single digit
        /// </summary>
        private static void DrawDigit(Color[] pixels, int width, int height, int x, int y, char digit, Color color, int size)
        {
            // Simplified digit drawing - just fill a small square with the color
            // In production, use proper font texture
            for (int dy = 0; dy < size * 2; dy++)
            {
                for (int dx = 0; dx < size; dx++)
                {
                    int px = x + dx;
                    int py = y + dy;
                    if (px >= 0 && px < width && py >= 0 && py < height)
                    {
                        int index = py * width + px;
                        if (index >= 0 && index < pixels.Length)
                            pixels[index] = color;
                    }
                }
            }
        }

        /// <summary>
        /// Get class name from ID
        /// </summary>
        public static string GetClassName(int classId)
        {
            return classNames.ContainsKey(classId) ? classNames[classId] : "Unknown";
        }

        /// <summary>
        /// Generate all placeholder sprites and save to Resources
        /// </summary>
        public static void GenerateAllPlaceholders()
        {
            Debug.Log("[PlaceholderAssetGenerator] Generating placeholder sprites for all classes...");

            string basePath = Application.dataPath + "/_Project/Resources/Sprites/Classes";

            // Create directories if they don't exist
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
                Debug.Log($"[PlaceholderAssetGenerator] Created directory: {basePath}");
            }

            // Generate placeholders for each class
            foreach (var kvp in classNames)
            {
                int classId = kvp.Key;
                string className = kvp.Value;

                // Create class directory
                string classPath = Path.Combine(basePath, className);
                if (!Directory.Exists(classPath))
                {
                    Directory.CreateDirectory(classPath);
                }

                // Generate sprite
                Sprite sprite = GeneratePlaceholderSprite(classId, 64, 64);

                // Save texture as PNG
                if (sprite != null && sprite.texture != null)
                {
                    byte[] pngData = sprite.texture.EncodeToPNG();
                    string filePath = Path.Combine(classPath, "idle.png");
                    File.WriteAllBytes(filePath, pngData);
                    Debug.Log($"[PlaceholderAssetGenerator] Generated placeholder for {className} at {filePath}");
                }
            }

            // Also create Icons folder with placeholders
            string iconsPath = Path.Combine(basePath, "Icons");
            if (!Directory.Exists(iconsPath))
            {
                Directory.CreateDirectory(iconsPath);
            }

            foreach (var kvp in classNames)
            {
                int classId = kvp.Key;
                string className = kvp.Value;

                // Generate icon (smaller size)
                Sprite icon = GeneratePlaceholderSprite(classId, 32, 32);

                if (icon != null && icon.texture != null)
                {
                    byte[] pngData = icon.texture.EncodeToPNG();
                    string filePath = Path.Combine(iconsPath, $"{className}_Icon.png");
                    File.WriteAllBytes(filePath, pngData);
                    Debug.Log($"[PlaceholderAssetGenerator] Generated icon for {className}");
                }
            }

            Debug.Log("[PlaceholderAssetGenerator] Placeholder generation complete!");

#if UNITY_EDITOR
            // Refresh asset database in Unity Editor
            AssetDatabase.Refresh();
            Debug.Log("[PlaceholderAssetGenerator] Unity asset database refreshed");
#endif
        }

        /// <summary>
        /// Check if placeholders exist
        /// </summary>
        public static bool PlaceholdersExist()
        {
            string basePath = Application.dataPath + "/_Project/Resources/Sprites/Classes";

            if (!Directory.Exists(basePath))
                return false;

            // Check if at least one class has sprites
            foreach (var className in classNames.Values)
            {
                string classPath = Path.Combine(basePath, className);
                string idleSpritePath = Path.Combine(classPath, "idle.png");

                if (File.Exists(idleSpritePath))
                    return true;
            }

            return false;
        }

#if UNITY_EDITOR
        [MenuItem("GOFUS/Asset Tools/Generate Placeholder Sprites")]
        public static void GeneratePlaceholdersMenuItem()
        {
            GenerateAllPlaceholders();
            EditorUtility.DisplayDialog("Placeholder Generation",
                "Placeholder sprites have been generated successfully!", "OK");
        }

        [MenuItem("GOFUS/Asset Tools/Clear Placeholder Sprites")]
        public static void ClearPlaceholdersMenuItem()
        {
            string basePath = Application.dataPath + "/_Project/Resources/Sprites/Classes";

            if (Directory.Exists(basePath))
            {
                Directory.Delete(basePath, true);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Clear Placeholders",
                    "Placeholder sprites have been cleared.", "OK");
            }
        }
#endif
    }
}