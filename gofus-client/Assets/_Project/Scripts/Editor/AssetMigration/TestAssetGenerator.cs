using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace GOFUS.Editor.AssetMigration
{
    /// <summary>
    /// Generates test assets to validate the asset pipeline
    /// Creates sample sprites that simulate extracted Dofus assets
    /// </summary>
    public class TestAssetGenerator : EditorWindow
    {
        private string outputPath = "ExtractedAssets/Raw";
        private bool generateCharacters = true;
        private bool generateUI = true;
        private bool generateMaps = true;

        [MenuItem("GOFUS/Asset Migration/Generate Test Assets")]
        public static void ShowWindow()
        {
            var window = GetWindow<TestAssetGenerator>("Test Asset Generator");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Test Asset Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This will generate sample assets to test the extraction pipeline", MessageType.Info);

            EditorGUILayout.Space();

            outputPath = EditorGUILayout.TextField("Output Path", outputPath);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Asset Types to Generate:", EditorStyles.boldLabel);
            generateCharacters = EditorGUILayout.Toggle("Character Sprites", generateCharacters);
            generateUI = EditorGUILayout.Toggle("UI Elements", generateUI);
            generateMaps = EditorGUILayout.Toggle("Map Tiles", generateMaps);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Test Assets", GUILayout.Height(30)))
            {
                GenerateTestAssets();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Open Extraction Folder"))
            {
                string fullPath = Path.GetFullPath(outputPath);
                if (Directory.Exists(fullPath))
                {
                    EditorUtility.RevealInFinder(fullPath);
                }
            }
        }

        private void GenerateTestAssets()
        {
            try
            {
                int totalAssets = 0;

                if (generateCharacters)
                {
                    totalAssets += GenerateCharacterAssets();
                }

                if (generateUI)
                {
                    totalAssets += GenerateUIAssets();
                }

                if (generateMaps)
                {
                    totalAssets += GenerateMapAssets();
                }

                EditorUtility.DisplayDialog("Success",
                    $"Generated {totalAssets} test assets successfully!\n\nPath: {outputPath}",
                    "OK");

                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Failed to generate test assets: {e.Message}",
                    "OK");
            }
        }

        private int GenerateCharacterAssets()
        {
            string charPath = Path.Combine(outputPath, "Characters/Feca");
            Directory.CreateDirectory(charPath);

            int count = 0;
            string[] directions = { "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest" };
            string[] animations = { "idle", "walk", "run", "attack" };

            foreach (string anim in animations)
            {
                foreach (string dir in directions)
                {
                    // Create a simple 64x64 test sprite
                    var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                    Color baseColor = GetColorForAnimation(anim);

                    // Fill with base color
                    for (int x = 0; x < 64; x++)
                    {
                        for (int y = 0; y < 64; y++)
                        {
                            texture.SetPixel(x, y, baseColor);
                        }
                    }

                    // Add direction indicator
                    DrawDirectionArrow(texture, dir);

                    // Add animation type text
                    DrawAnimationType(texture, anim);

                    texture.Apply();

                    // Save as PNG
                    string fileName = $"{anim}_{dir}_0.png";
                    string filePath = Path.Combine(charPath, fileName);
                    File.WriteAllBytes(filePath, texture.EncodeToPNG());
                    DestroyImmediate(texture);

                    count++;
                }
            }

            Debug.Log($"Generated {count} character test sprites in {charPath}");
            return count;
        }

        private int GenerateUIAssets()
        {
            string uiPath = Path.Combine(outputPath, "UI");
            Directory.CreateDirectory(Path.Combine(uiPath, "Buttons"));
            Directory.CreateDirectory(Path.Combine(uiPath, "Windows"));
            Directory.CreateDirectory(Path.Combine(uiPath, "Icons"));

            int count = 0;

            // Generate button states
            string[] buttonStates = { "normal", "hover", "pressed", "disabled" };
            foreach (string state in buttonStates)
            {
                var texture = CreateButtonTexture(state);
                string filePath = Path.Combine(uiPath, "Buttons", $"btn_{state}.png");
                File.WriteAllBytes(filePath, texture.EncodeToPNG());
                DestroyImmediate(texture);
                count++;
            }

            // Generate window frame
            var windowTexture = CreateWindowFrame();
            File.WriteAllBytes(Path.Combine(uiPath, "Windows", "window_frame.png"), windowTexture.EncodeToPNG());
            DestroyImmediate(windowTexture);
            count++;

            // Generate sample icons
            string[] iconTypes = { "sword", "shield", "potion", "spell", "coin" };
            foreach (string icon in iconTypes)
            {
                var texture = CreateIcon(icon);
                string filePath = Path.Combine(uiPath, "Icons", $"icon_{icon}.png");
                File.WriteAllBytes(filePath, texture.EncodeToPNG());
                DestroyImmediate(texture);
                count++;
            }

            Debug.Log($"Generated {count} UI test assets");
            return count;
        }

        private int GenerateMapAssets()
        {
            string mapPath = Path.Combine(outputPath, "Maps/Tiles");
            Directory.CreateDirectory(mapPath);

            int count = 0;

            // Generate tile types
            string[] tileTypes = { "grass", "stone", "sand", "water", "dirt" };
            foreach (string type in tileTypes)
            {
                for (int variant = 1; variant <= 3; variant++)
                {
                    var texture = CreateTileTexture(type, variant);
                    string filePath = Path.Combine(mapPath, $"{type}_{variant:D2}.png");
                    File.WriteAllBytes(filePath, texture.EncodeToPNG());
                    DestroyImmediate(texture);
                    count++;
                }
            }

            Debug.Log($"Generated {count} map tile test assets");
            return count;
        }

        private Color GetColorForAnimation(string animation)
        {
            switch (animation)
            {
                case "idle": return new Color(0.5f, 0.5f, 0.8f, 1f);
                case "walk": return new Color(0.5f, 0.8f, 0.5f, 1f);
                case "run": return new Color(0.8f, 0.8f, 0.5f, 1f);
                case "attack": return new Color(0.8f, 0.5f, 0.5f, 1f);
                default: return Color.gray;
            }
        }

        private void DrawDirectionArrow(Texture2D texture, string direction)
        {
            int centerX = 32;
            int centerY = 32;
            Color arrowColor = Color.white;

            // Draw arrow based on direction
            switch (direction)
            {
                case "north":
                    DrawLine(texture, centerX, centerY, centerX, centerY + 15, arrowColor);
                    DrawLine(texture, centerX, centerY + 15, centerX - 5, centerY + 10, arrowColor);
                    DrawLine(texture, centerX, centerY + 15, centerX + 5, centerY + 10, arrowColor);
                    break;
                case "east":
                    DrawLine(texture, centerX, centerY, centerX + 15, centerY, arrowColor);
                    DrawLine(texture, centerX + 15, centerY, centerX + 10, centerY - 5, arrowColor);
                    DrawLine(texture, centerX + 15, centerY, centerX + 10, centerY + 5, arrowColor);
                    break;
                // Add more directions as needed
            }
        }

        private void DrawAnimationType(Texture2D texture, string animation)
        {
            // Draw simple text indicator (simplified - just a marker)
            Color textColor = Color.black;
            int startX = 5;
            int startY = 5;

            // Draw a simple rectangle as text placeholder
            for (int x = startX; x < startX + animation.Length * 5; x++)
            {
                for (int y = startY; y < startY + 8; y++)
                {
                    if (x < texture.width && y < texture.height)
                    {
                        texture.SetPixel(x, y, textColor);
                    }
                }
            }
        }

        private void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x0 >= 0 && x0 < texture.width && y0 >= 0 && y0 < texture.height)
                {
                    texture.SetPixel(x0, y0, color);
                }

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private Texture2D CreateButtonTexture(string state)
        {
            var texture = new Texture2D(128, 48, TextureFormat.RGBA32, false);

            Color bgColor = state switch
            {
                "normal" => new Color(0.7f, 0.7f, 0.7f, 1f),
                "hover" => new Color(0.8f, 0.8f, 0.8f, 1f),
                "pressed" => new Color(0.6f, 0.6f, 0.6f, 1f),
                "disabled" => new Color(0.5f, 0.5f, 0.5f, 0.5f),
                _ => Color.gray
            };

            // Fill background
            for (int x = 0; x < 128; x++)
            {
                for (int y = 0; y < 48; y++)
                {
                    texture.SetPixel(x, y, bgColor);
                }
            }

            // Add border
            Color borderColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            for (int x = 0; x < 128; x++)
            {
                texture.SetPixel(x, 0, borderColor);
                texture.SetPixel(x, 47, borderColor);
            }
            for (int y = 0; y < 48; y++)
            {
                texture.SetPixel(0, y, borderColor);
                texture.SetPixel(127, y, borderColor);
            }

            texture.Apply();
            return texture;
        }

        private Texture2D CreateWindowFrame()
        {
            var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);

            // Fill with transparent background
            for (int x = 0; x < 256; x++)
            {
                for (int y = 0; y < 256; y++)
                {
                    texture.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }

            // Draw frame
            Color frameColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            int thickness = 8;

            for (int x = 0; x < 256; x++)
            {
                for (int t = 0; t < thickness; t++)
                {
                    texture.SetPixel(x, t, frameColor);
                    texture.SetPixel(x, 255 - t, frameColor);
                }
            }

            for (int y = 0; y < 256; y++)
            {
                for (int t = 0; t < thickness; t++)
                {
                    texture.SetPixel(t, y, frameColor);
                    texture.SetPixel(255 - t, y, frameColor);
                }
            }

            texture.Apply();
            return texture;
        }

        private Texture2D CreateIcon(string iconType)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);

            // Simple colored squares for different icon types
            Color iconColor = iconType switch
            {
                "sword" => new Color(0.7f, 0.7f, 0.8f, 1f),
                "shield" => new Color(0.8f, 0.7f, 0.5f, 1f),
                "potion" => new Color(0.5f, 0.8f, 0.5f, 1f),
                "spell" => new Color(0.8f, 0.5f, 0.8f, 1f),
                "coin" => new Color(0.9f, 0.8f, 0.3f, 1f),
                _ => Color.gray
            };

            // Fill with icon color
            for (int x = 4; x < 28; x++)
            {
                for (int y = 4; y < 28; y++)
                {
                    texture.SetPixel(x, y, iconColor);
                }
            }

            // Add simple border
            Color borderColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            for (int x = 4; x < 28; x++)
            {
                texture.SetPixel(x, 4, borderColor);
                texture.SetPixel(x, 27, borderColor);
            }
            for (int y = 4; y < 28; y++)
            {
                texture.SetPixel(4, y, borderColor);
                texture.SetPixel(27, y, borderColor);
            }

            texture.Apply();
            return texture;
        }

        private Texture2D CreateTileTexture(string tileType, int variant)
        {
            var texture = new Texture2D(64, 32, TextureFormat.RGBA32, false); // Isometric tile size

            Color baseColor = tileType switch
            {
                "grass" => new Color(0.3f, 0.6f, 0.3f, 1f),
                "stone" => new Color(0.5f, 0.5f, 0.5f, 1f),
                "sand" => new Color(0.8f, 0.7f, 0.5f, 1f),
                "water" => new Color(0.3f, 0.5f, 0.7f, 1f),
                "dirt" => new Color(0.5f, 0.4f, 0.3f, 1f),
                _ => Color.gray
            };

            // Add variant variation
            float variantOffset = variant * 0.05f;
            baseColor.r = Mathf.Clamp01(baseColor.r + variantOffset);
            baseColor.g = Mathf.Clamp01(baseColor.g + variantOffset);
            baseColor.b = Mathf.Clamp01(baseColor.b + variantOffset);

            // Fill with base color
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    texture.SetPixel(x, y, baseColor);
                }
            }

            // Add simple pattern based on variant
            Color patternColor = new Color(baseColor.r * 0.8f, baseColor.g * 0.8f, baseColor.b * 0.8f, 1f);
            UnityEngine.Random.InitState(variant);
            for (int i = 0; i < 10 + variant * 5; i++)
            {
                int x = UnityEngine.Random.Range(5, 59);
                int y = UnityEngine.Random.Range(5, 27);
                texture.SetPixel(x, y, patternColor);
                texture.SetPixel(x + 1, y, patternColor);
                texture.SetPixel(x, y + 1, patternColor);
            }

            texture.Apply();
            return texture;
        }
    }
}