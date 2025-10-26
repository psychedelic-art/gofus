using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
// using UnityEditor.U2D.Sprites; // This namespace may not be available in all Unity versions

namespace GOFUS.Editor.AssetMigration
{
    /// <summary>
    /// Automatic sprite sheet slicer for Dofus character sprites
    /// Handles 8-directional sprites with multiple animation frames
    /// </summary>
    public class SpriteSheetSlicer : EditorWindow
    {
        #region Properties

        private Texture2D selectedTexture;
        private int rows = 8; // 8 directions
        private int columns = 8; // frames per animation
        private bool autoDetect = true;
        private int spriteWidth = 64;
        private int spriteHeight = 64;
        private Vector2 pivot = new Vector2(0.5f, 0.5f);
        private SpriteAlignment alignment = SpriteAlignment.Center;

        private AnimationType animationType = AnimationType.Character;
        private NamingConvention namingConvention = NamingConvention.DirectionFirst;

        private List<SpriteMetaData> generatedSprites = new List<SpriteMetaData>();
        private Vector2 scrollPosition;

        #endregion

        #region Window

        [MenuItem("GOFUS/Asset Migration/Sprite Sheet Slicer")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpriteSheetSlicer>("Sprite Sheet Slicer");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            DrawHeader();
            DrawTextureSelection();

            if (selectedTexture != null)
            {
                DrawSlicingOptions();
                DrawNamingOptions();
                DrawPreview();
                DrawActionButtons();
            }

            DrawResults();
        }

        private void DrawHeader()
        {
            GUILayout.Label("Sprite Sheet Slicer", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Automatically slice Dofus sprite sheets", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.Separator();
        }

        private void DrawTextureSelection()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Texture Selection", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            selectedTexture = (Texture2D)EditorGUILayout.ObjectField(
                "Sprite Sheet:",
                selectedTexture,
                typeof(Texture2D),
                false
            );

            if (EditorGUI.EndChangeCheck() && selectedTexture != null && autoDetect)
            {
                AutoDetectGrid();
            }
        }

        private void DrawSlicingOptions()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Slicing Options", EditorStyles.boldLabel);

            autoDetect = EditorGUILayout.Toggle("Auto Detect Grid", autoDetect);

            EditorGUI.BeginDisabledGroup(autoDetect);
            EditorGUI.indentLevel++;

            rows = EditorGUILayout.IntField("Rows (Directions):", rows);
            columns = EditorGUILayout.IntField("Columns (Frames):", columns);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sprite Size:", GUILayout.Width(80));
            spriteWidth = EditorGUILayout.IntField(spriteWidth, GUILayout.Width(50));
            EditorGUILayout.LabelField("x", GUILayout.Width(15));
            spriteHeight = EditorGUILayout.IntField(spriteHeight, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();

            alignment = (SpriteAlignment)EditorGUILayout.EnumPopup("Pivot:", alignment);

            if (alignment == SpriteAlignment.Custom)
            {
                EditorGUI.indentLevel++;
                pivot = EditorGUILayout.Vector2Field("Custom Pivot:", pivot);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawNamingOptions()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Naming Options", EditorStyles.boldLabel);

            animationType = (AnimationType)EditorGUILayout.EnumPopup("Animation Type:", animationType);
            namingConvention = (NamingConvention)EditorGUILayout.EnumPopup("Naming Convention:", namingConvention);

            EditorGUILayout.HelpBox(GetNamingExample(), MessageType.Info);
        }

        private void DrawPreview()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            if (selectedTexture != null)
            {
                float aspectRatio = (float)selectedTexture.width / selectedTexture.height;
                float previewHeight = 200;
                float previewWidth = previewHeight * aspectRatio;

                Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);

                // Draw texture
                GUI.DrawTexture(previewRect, selectedTexture);

                // Draw grid overlay
                DrawGridOverlay(previewRect);

                EditorGUILayout.LabelField($"Texture Size: {selectedTexture.width}x{selectedTexture.height}");
                EditorGUILayout.LabelField($"Sprites: {rows * columns} ({rows} rows x {columns} columns)");
                EditorGUILayout.LabelField($"Sprite Size: {spriteWidth}x{spriteHeight}");
            }
        }

        private void DrawGridOverlay(Rect rect)
        {
            Handles.BeginGUI();
            Color originalColor = Handles.color;
            Handles.color = new Color(1, 1, 0, 0.5f);

            float cellWidth = rect.width / columns;
            float cellHeight = rect.height / rows;

            // Draw vertical lines
            for (int i = 1; i < columns; i++)
            {
                float x = rect.x + i * cellWidth;
                Handles.DrawLine(
                    new Vector3(x, rect.y, 0),
                    new Vector3(x, rect.y + rect.height, 0)
                );
            }

            // Draw horizontal lines
            for (int i = 1; i < rows; i++)
            {
                float y = rect.y + i * cellHeight;
                Handles.DrawLine(
                    new Vector3(rect.x, y, 0),
                    new Vector3(rect.x + rect.width, y, 0)
                );
            }

            Handles.color = originalColor;
            Handles.EndGUI();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Preview Slicing", GUILayout.Height(30)))
            {
                PreviewSlicing();
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Apply Slicing", GUILayout.Height(30)))
            {
                ApplySlicing();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset"))
            {
                ResetSettings();
            }

            if (GUILayout.Button("Auto Detect"))
            {
                AutoDetectGrid();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawResults()
        {
            if (generatedSprites.Count > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField($"Generated Sprites ({generatedSprites.Count})", EditorStyles.boldLabel);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));

                for (int i = 0; i < Mathf.Min(generatedSprites.Count, 20); i++)
                {
                    var sprite = generatedSprites[i];
                    EditorGUILayout.LabelField($"{sprite.name} - ({sprite.rect.x}, {sprite.rect.y}, {sprite.rect.width}, {sprite.rect.height})");
                }

                if (generatedSprites.Count > 20)
                {
                    EditorGUILayout.LabelField($"... and {generatedSprites.Count - 20} more");
                }

                EditorGUILayout.EndScrollView();
            }
        }

        #endregion

        #region Slicing Logic

        private void PreviewSlicing()
        {
            if (selectedTexture == null) return;

            generatedSprites = GenerateSprites();

            EditorUtility.DisplayDialog(
                "Preview",
                $"Will generate {generatedSprites.Count} sprites.\n" +
                $"Grid: {rows} x {columns}\n" +
                $"Sprite size: {spriteWidth} x {spriteHeight}",
                "OK"
            );
        }

        private void ApplySlicing()
        {
            if (selectedTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a texture first!", "OK");
                return;
            }

            try
            {
                string path = AssetDatabase.GetAssetPath(selectedTexture);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null)
                {
                    EditorUtility.DisplayDialog("Error", "Could not get texture importer!", "OK");
                    return;
                }

                // Configure importer
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                // Generate and apply sprites
                generatedSprites = GenerateSprites();
                importer.spritesheet = generatedSprites.ToArray();

                // Apply changes
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                EditorUtility.DisplayDialog(
                    "Success",
                    $"Successfully sliced texture into {generatedSprites.Count} sprites!",
                    "OK"
                );

                // Create animation clips if character type
                if (animationType == AnimationType.Character)
                {
                    if (EditorUtility.DisplayDialog(
                        "Create Animations?",
                        "Would you like to create animation clips for these sprites?",
                        "Yes",
                        "No"
                    ))
                    {
                        CreateAnimationClips(path);
                    }
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to slice texture: {e.Message}", "OK");
                Debug.LogError(e);
            }
        }

        private List<SpriteMetaData> GenerateSprites()
        {
            var sprites = new List<SpriteMetaData>();

            if (autoDetect)
            {
                AutoDetectGrid();
            }

            string baseName = selectedTexture.name;
            string[] directions = GetDirectionNames();
            string[] animations = GetAnimationNames();

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    SpriteMetaData sprite = new SpriteMetaData
                    {
                        rect = new Rect(
                            col * spriteWidth,
                            selectedTexture.height - (row + 1) * spriteHeight,
                            spriteWidth,
                            spriteHeight
                        ),
                        alignment = (int)alignment,
                        pivot = GetPivotPoint(),
                        border = Vector4.zero
                    };

                    // Generate name based on convention
                    sprite.name = GenerateSpriteName(baseName, row, col, directions, animations);

                    sprites.Add(sprite);
                }
            }

            return sprites;
        }

        private string GenerateSpriteName(string baseName, int row, int col, string[] directions, string[] animations)
        {
            string direction = row < directions.Length ? directions[row] : $"dir_{row}";
            string frame = col.ToString("D2");

            switch (namingConvention)
            {
                case NamingConvention.DirectionFirst:
                    return $"{baseName}_{direction}_{frame}";

                case NamingConvention.FrameFirst:
                    return $"{baseName}_{frame}_{direction}";

                case NamingConvention.TypeDirectionFrame:
                    string animName = GetAnimationTypeString();
                    return $"{baseName}_{animName}_{direction}_{frame}";

                case NamingConvention.IndexOnly:
                    return $"{baseName}_{row * columns + col:D3}";

                default:
                    return $"{baseName}_{row}_{col}";
            }
        }

        private void AutoDetectGrid()
        {
            if (selectedTexture == null) return;

            // Try to detect common sprite sheet patterns
            int width = selectedTexture.width;
            int height = selectedTexture.height;

            // Common Dofus character sprite dimensions
            int[] commonSizes = { 64, 96, 128, 256 };

            foreach (int size in commonSizes)
            {
                if (width % size == 0 && height % size == 0)
                {
                    columns = width / size;
                    rows = height / size;
                    spriteWidth = size;
                    spriteHeight = size;
                    break;
                }
            }

            // Fallback detection
            if (columns == 0 || rows == 0)
            {
                // Try 8x8 grid (common for character sprites)
                if (width % 8 == 0 && height % 8 == 0)
                {
                    spriteWidth = width / 8;
                    spriteHeight = height / 8;
                    columns = 8;
                    rows = 8;
                }
                else
                {
                    // Default to reasonable values
                    columns = 8;
                    rows = Mathf.CeilToInt((float)height / (width / 8));
                    spriteWidth = width / columns;
                    spriteHeight = height / rows;
                }
            }
        }

        #endregion

        #region Animation Creation

        private void CreateAnimationClips(string texturePath)
        {
            string[] directions = GetDirectionNames();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .ToList();

            if (sprites.Count == 0)
            {
                Debug.LogWarning("No sprites found to create animations");
                return;
            }

            string basePath = texturePath.Replace(selectedTexture.name, "").TrimEnd('/');
            string animPath = basePath + "/Animations/" + selectedTexture.name;

            // Ensure directory exists
            System.IO.Directory.CreateDirectory(animPath);

            // Group sprites by direction
            for (int dirIndex = 0; dirIndex < directions.Length && dirIndex < rows; dirIndex++)
            {
                string direction = directions[dirIndex];
                var directionSprites = new List<Sprite>();

                for (int frame = 0; frame < columns; frame++)
                {
                    int spriteIndex = dirIndex * columns + frame;
                    if (spriteIndex < sprites.Count)
                    {
                        directionSprites.Add(sprites[spriteIndex]);
                    }
                }

                if (directionSprites.Count > 0)
                {
                    CreateAnimationClip(directionSprites, $"{animPath}/{selectedTexture.name}_{direction}.anim");
                }
            }

            AssetDatabase.Refresh();
        }

        private void CreateAnimationClip(List<Sprite> sprites, string path)
        {
            AnimationClip clip = new AnimationClip
            {
                frameRate = 12 // Standard for Dofus animations
            };

            // Create sprite animation keyframes
            EditorCurveBinding spriteBinding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
            for (int i = 0; i < sprites.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / clip.frameRate,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

            // Set loop settings
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // Save clip
            AssetDatabase.CreateAsset(clip, path);
        }

        #endregion

        #region Helper Methods

        private string[] GetDirectionNames()
        {
            return new string[] { "S", "SW", "W", "NW", "N", "NE", "E", "SE" };
        }

        private string[] GetAnimationNames()
        {
            switch (animationType)
            {
                case AnimationType.Character:
                    return new string[] { "idle", "walk", "run", "attack", "cast", "hit", "death", "emote" };
                case AnimationType.Monster:
                    return new string[] { "idle", "move", "attack", "special", "hit", "death" };
                case AnimationType.Effect:
                    return new string[] { "start", "loop", "end" };
                default:
                    return new string[] { "anim" };
            }
        }

        private string GetAnimationTypeString()
        {
            switch (animationType)
            {
                case AnimationType.Character:
                    return "char";
                case AnimationType.Monster:
                    return "mob";
                case AnimationType.Effect:
                    return "fx";
                case AnimationType.UI:
                    return "ui";
                default:
                    return "sprite";
            }
        }

        private Vector2 GetPivotPoint()
        {
            switch (alignment)
            {
                case SpriteAlignment.Center:
                    return new Vector2(0.5f, 0.5f);
                case SpriteAlignment.TopLeft:
                    return new Vector2(0, 1);
                case SpriteAlignment.TopCenter:
                    return new Vector2(0.5f, 1);
                case SpriteAlignment.TopRight:
                    return new Vector2(1, 1);
                case SpriteAlignment.LeftCenter:
                    return new Vector2(0, 0.5f);
                case SpriteAlignment.RightCenter:
                    return new Vector2(1, 0.5f);
                case SpriteAlignment.BottomLeft:
                    return new Vector2(0, 0);
                case SpriteAlignment.BottomCenter:
                    return new Vector2(0.5f, 0);
                case SpriteAlignment.BottomRight:
                    return new Vector2(1, 0);
                case SpriteAlignment.Custom:
                    return pivot;
                default:
                    return new Vector2(0.5f, 0.5f);
            }
        }

        private string GetNamingExample()
        {
            string example = "Example: ";
            switch (namingConvention)
            {
                case NamingConvention.DirectionFirst:
                    return example + "character_S_00, character_S_01, character_SW_00...";
                case NamingConvention.FrameFirst:
                    return example + "character_00_S, character_01_S, character_00_SW...";
                case NamingConvention.TypeDirectionFrame:
                    return example + "character_char_S_00, character_char_S_01...";
                case NamingConvention.IndexOnly:
                    return example + "character_000, character_001, character_002...";
                default:
                    return example + "character_0_0, character_0_1...";
            }
        }

        private void ResetSettings()
        {
            rows = 8;
            columns = 8;
            spriteWidth = 64;
            spriteHeight = 64;
            autoDetect = true;
            alignment = SpriteAlignment.Center;
            pivot = new Vector2(0.5f, 0.5f);
            animationType = AnimationType.Character;
            namingConvention = NamingConvention.DirectionFirst;
            generatedSprites.Clear();
        }

        #endregion
    }

    #region Enums

    public enum AnimationType
    {
        Character,
        Monster,
        Effect,
        UI,
        Other
    }

    public enum NamingConvention
    {
        DirectionFirst,     // sprite_S_00
        FrameFirst,         // sprite_00_S
        TypeDirectionFrame, // sprite_char_S_00
        IndexOnly          // sprite_000
    }

    #endregion
}