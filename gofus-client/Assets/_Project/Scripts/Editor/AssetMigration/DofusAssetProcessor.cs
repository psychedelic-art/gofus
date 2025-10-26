using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;

namespace GOFUS.Editor.AssetMigration
{
    /// <summary>
    /// Main asset processor for migrating Dofus/Flash assets to Unity
    /// Handles extraction, conversion, and optimization
    /// </summary>
    public class DofusAssetProcessor : EditorWindow
    {
        #region Properties

        private string sourcePath = "";
        private string outputPath = "Assets/_Project/ImportedAssets";
        private AssetType assetTypeToProcess = AssetType.All;
        private bool createAtlases = true;
        private bool generateAnimations = true;
        private bool optimizeTextures = true;

        private int processedFiles = 0;
        private int totalFiles = 0;
        private string currentOperation = "";
        private float progress = 0f;

        private Vector2 scrollPosition;
        private List<AssetProcessingResult> processingResults = new List<AssetProcessingResult>();

        #endregion

        #region Menu Items

        [MenuItem("GOFUS/Asset Migration/Dofus Asset Processor")]
        public static void ShowWindow()
        {
            var window = GetWindow<DofusAssetProcessor>("Dofus Asset Processor");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        [MenuItem("GOFUS/Asset Migration/Quick Actions/Process Character Sprites")]
        public static void QuickProcessCharacters()
        {
            ProcessAssetType(AssetType.Characters);
        }

        [MenuItem("GOFUS/Asset Migration/Quick Actions/Process Map Tiles")]
        public static void QuickProcessMaps()
        {
            ProcessAssetType(AssetType.Maps);
        }

        [MenuItem("GOFUS/Asset Migration/Quick Actions/Process UI Elements")]
        public static void QuickProcessUI()
        {
            ProcessAssetType(AssetType.UI);
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            DrawHeader();
            DrawSettings();
            DrawProcessingOptions();
            DrawActionButtons();
            DrawProgress();
            DrawResults();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("Dofus Asset Migration Tool", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Convert Flash/SWF assets to Unity format", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.Separator();
        }

        private void DrawSettings()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Source Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            sourcePath = EditorGUILayout.TextField("Source Path:", sourcePath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Dofus Assets Folder", sourcePath, "");
                if (!string.IsNullOrEmpty(path))
                    sourcePath = path;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            outputPath = EditorGUILayout.TextField("Output Path:", outputPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Output Folder", outputPath, "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                {
                    outputPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();

            assetTypeToProcess = (AssetType)EditorGUILayout.EnumPopup("Asset Type:", assetTypeToProcess);
        }

        private void DrawProcessingOptions()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Processing Options", EditorStyles.boldLabel);

            createAtlases = EditorGUILayout.Toggle("Create Sprite Atlases", createAtlases);
            generateAnimations = EditorGUILayout.Toggle("Generate Animations", generateAnimations);
            optimizeTextures = EditorGUILayout.Toggle("Optimize Textures", optimizeTextures);

            if (optimizeTextures)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("Texture optimization will compress images and generate mipmaps where appropriate.", MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !string.IsNullOrEmpty(sourcePath) && Directory.Exists(sourcePath);

            if (GUILayout.Button("Analyze Assets", GUILayout.Height(30)))
            {
                AnalyzeAssets();
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Process Assets", GUILayout.Height(30)))
            {
                ProcessAssets();
            }
            GUI.backgroundColor = Color.white;

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Cache"))
            {
                ClearCache();
            }
            if (GUILayout.Button("Validate Assets"))
            {
                ValidateAssets();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawProgress()
        {
            if (totalFiles > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Progress", EditorStyles.boldLabel);

                EditorGUI.ProgressBar(
                    EditorGUILayout.GetControlRect(GUILayout.Height(20)),
                    progress,
                    $"{currentOperation} ({processedFiles}/{totalFiles})"
                );
            }
        }

        private void DrawResults()
        {
            if (processingResults.Count > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField($"Processing Results ({processingResults.Count})", EditorStyles.boldLabel);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

                foreach (var result in processingResults)
                {
                    EditorGUILayout.BeginHorizontal();

                    GUI.color = result.Success ? Color.green : Color.red;
                    EditorGUILayout.LabelField(result.Success ? "✓" : "✗", GUILayout.Width(20));
                    GUI.color = Color.white;

                    EditorGUILayout.LabelField(result.FileName);
                    EditorGUILayout.LabelField(result.Message, EditorStyles.miniLabel);

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        #endregion

        #region Processing

        private void AnalyzeAssets()
        {
            processingResults.Clear();
            totalFiles = 0;

            if (!Directory.Exists(sourcePath))
            {
                EditorUtility.DisplayDialog("Error", "Source directory does not exist!", "OK");
                return;
            }

            var files = GetFilesToProcess();
            totalFiles = files.Count;

            EditorUtility.DisplayDialog(
                "Analysis Complete",
                $"Found {totalFiles} files to process:\n" +
                $"- PNG/JPG: {files.Count(f => f.EndsWith(".png") || f.EndsWith(".jpg"))}\n" +
                $"- SWF: {files.Count(f => f.EndsWith(".swf"))}\n" +
                $"- Audio: {files.Count(f => f.EndsWith(".mp3") || f.EndsWith(".wav"))}",
                "OK"
            );
        }

        private void ProcessAssets()
        {
            if (!Directory.Exists(sourcePath))
            {
                EditorUtility.DisplayDialog("Error", "Source directory does not exist!", "OK");
                return;
            }

            processingResults.Clear();
            processedFiles = 0;
            progress = 0;

            try
            {
                // Create output directories
                CreateOutputDirectories();

                // Get files to process
                var files = GetFilesToProcess();
                totalFiles = files.Count;

                // Process each file
                foreach (var file in files)
                {
                    ProcessFile(file);
                    processedFiles++;
                    progress = (float)processedFiles / totalFiles;

                    EditorUtility.DisplayProgressBar(
                        "Processing Assets",
                        $"Processing {Path.GetFileName(file)}...",
                        progress
                    );
                }

                // Post-processing
                if (createAtlases)
                {
                    CreateSpriteAtlases();
                }

                if (generateAnimations)
                {
                    GenerateAnimationClips();
                }

                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayDialog(
                    "Success",
                    $"Processed {processedFiles} files successfully!",
                    "OK"
                );
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", $"Processing failed: {e.Message}", "OK");
                Debug.LogError($"Asset processing error: {e}");
            }
        }

        private void ProcessFile(string filePath)
        {
            currentOperation = $"Processing {Path.GetFileName(filePath)}";

            try
            {
                string extension = Path.GetExtension(filePath).ToLower();
                AssetProcessingResult result = new AssetProcessingResult
                {
                    FileName = Path.GetFileName(filePath)
                };

                switch (extension)
                {
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                        ProcessImage(filePath, result);
                        break;

                    case ".swf":
                        ProcessSWF(filePath, result);
                        break;

                    case ".mp3":
                    case ".wav":
                    case ".ogg":
                        ProcessAudio(filePath, result);
                        break;

                    case ".xml":
                    case ".json":
                        ProcessData(filePath, result);
                        break;

                    default:
                        result.Success = false;
                        result.Message = "Unsupported file type";
                        break;
                }

                processingResults.Add(result);
            }
            catch (Exception e)
            {
                processingResults.Add(new AssetProcessingResult
                {
                    FileName = Path.GetFileName(filePath),
                    Success = false,
                    Message = e.Message
                });
            }
        }

        private void ProcessImage(string filePath, AssetProcessingResult result)
        {
            string relativePath = GetRelativeOutputPath(filePath);
            string outputFilePath = Path.Combine(outputPath, relativePath);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

            // Copy file
            File.Copy(filePath, outputFilePath, true);

            // Import and configure texture
            AssetDatabase.ImportAsset(outputFilePath);

            TextureImporter importer = AssetImporter.GetAtPath(outputFilePath) as TextureImporter;
            if (importer != null)
            {
                ConfigureTextureImporter(importer, filePath);
                importer.SaveAndReimport();
            }

            result.Success = true;
            result.Message = "Image imported successfully";
        }

        private void ProcessSWF(string filePath, AssetProcessingResult result)
        {
            // SWF processing requires external tools
            // For now, we'll create a placeholder

            result.Success = false;
            result.Message = "SWF extraction requires JPEXS or similar tool";

            // TODO: Integrate with JPEXS command line
            // string jpexsPath = "path/to/ffdec.jar";
            // string command = $"java -jar {jpexsPath} -export image {outputPath} {filePath}";
        }

        private void ProcessAudio(string filePath, AssetProcessingResult result)
        {
            string relativePath = GetRelativeOutputPath(filePath);
            string outputFilePath = Path.Combine(outputPath, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
            File.Copy(filePath, outputFilePath, true);

            AssetDatabase.ImportAsset(outputFilePath);

            AudioImporter importer = AssetImporter.GetAtPath(outputFilePath) as AudioImporter;
            if (importer != null)
            {
                ConfigureAudioImporter(importer, filePath);
                importer.SaveAndReimport();
            }

            result.Success = true;
            result.Message = "Audio imported successfully";
        }

        private void ProcessData(string filePath, AssetProcessingResult result)
        {
            string relativePath = GetRelativeOutputPath(filePath);
            string outputFilePath = Path.Combine(outputPath, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
            File.Copy(filePath, outputFilePath, true);

            AssetDatabase.ImportAsset(outputFilePath);

            result.Success = true;
            result.Message = "Data file imported";
        }

        #endregion

        #region Texture Configuration

        private void ConfigureTextureImporter(TextureImporter importer, string filePath)
        {
            // Determine texture type based on path
            if (filePath.Contains("character") || filePath.Contains("sprite"))
            {
                ConfigureCharacterSprite(importer);
            }
            else if (filePath.Contains("ui") || filePath.Contains("icon"))
            {
                ConfigureUISprite(importer);
            }
            else if (filePath.Contains("tile") || filePath.Contains("map"))
            {
                ConfigureMapTile(importer);
            }
            else if (filePath.Contains("effect") || filePath.Contains("particle"))
            {
                ConfigureEffectSprite(importer);
            }
            else
            {
                ConfigureDefaultSprite(importer);
            }

            if (optimizeTextures)
            {
                ApplyTextureOptimization(importer);
            }
        }

        private void ConfigureCharacterSprite(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;

            // Set sprite sheet settings
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
        }

        private void ConfigureUISprite(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 1024;
            importer.mipmapEnabled = false;
        }

        private void ConfigureMapTile(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64; // Isometric tile size
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 512;
            importer.mipmapEnabled = false;
        }

        private void ConfigureEffectSprite(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 512;
            importer.alphaIsTransparency = true;
        }

        private void ConfigureDefaultSprite(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Bilinear;
        }

        private void ApplyTextureOptimization(TextureImporter importer)
        {
            var platformSettings = new TextureImporterPlatformSettings
            {
                name = "Standalone",
                overridden = true,
                maxTextureSize = 2048,
                format = TextureImporterFormat.DXT5,
                compressionQuality = 50
            };
            importer.SetPlatformTextureSettings(platformSettings);
        }

        #endregion

        #region Audio Configuration

        private void ConfigureAudioImporter(AudioImporter importer, string filePath)
        {
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;

            if (filePath.Contains("music") || filePath.Contains("bgm"))
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
            }
            else if (filePath.Contains("sfx") || filePath.Contains("sound"))
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
            }
            else
            {
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.5f;
            }

            importer.defaultSampleSettings = settings;
        }

        #endregion

        #region Sprite Atlas Creation

        private void CreateSpriteAtlases()
        {
            currentOperation = "Creating sprite atlases...";

            if (assetTypeToProcess == AssetType.Characters || assetTypeToProcess == AssetType.All)
            {
                CreateCharacterAtlases();
            }

            if (assetTypeToProcess == AssetType.UI || assetTypeToProcess == AssetType.All)
            {
                CreateUIAtlases();
            }

            if (assetTypeToProcess == AssetType.Maps || assetTypeToProcess == AssetType.All)
            {
                CreateMapAtlases();
            }
        }

        private void CreateCharacterAtlases()
        {
            string atlasPath = Path.Combine(outputPath, "Atlases", "CharacterAtlas.spriteatlasv2");

            SpriteAtlas atlas = new SpriteAtlas();

            // Configure atlas settings
            SpriteAtlasPackingSettings packingSettings = new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                padding = 2,
                enableRotation = false,
                enableTightPacking = false
            };
            atlas.SetPackingSettings(packingSettings);

            // Add sprites to atlas
            string spritePath = Path.Combine(outputPath, "Sprites", "Characters");
            if (Directory.Exists(spritePath))
            {
                var sprites = AssetDatabase.FindAssets("t:Sprite", new[] { spritePath });
                foreach (var guid in sprites)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null)
                    {
                        atlas.Add(new UnityEngine.Object[] { sprite });
                    }
                }
            }

            // Save atlas
            AssetDatabase.CreateAsset(atlas, atlasPath);
        }

        private void CreateUIAtlases()
        {
            // Similar to CreateCharacterAtlases but for UI
            string atlasPath = Path.Combine(outputPath, "Atlases", "UIAtlas.spriteatlasv2");
            // ... implement UI atlas creation
        }

        private void CreateMapAtlases()
        {
            // Similar to CreateCharacterAtlases but for maps
            string atlasPath = Path.Combine(outputPath, "Atlases", "MapAtlas.spriteatlasv2");
            // ... implement map atlas creation
        }

        #endregion

        #region Animation Generation

        private void GenerateAnimationClips()
        {
            currentOperation = "Generating animation clips...";

            if (assetTypeToProcess == AssetType.Characters || assetTypeToProcess == AssetType.All)
            {
                GenerateCharacterAnimations();
            }
        }

        private void GenerateCharacterAnimations()
        {
            string spritePath = Path.Combine(outputPath, "Sprites", "Characters");
            if (!Directory.Exists(spritePath))
                return;

            var directories = Directory.GetDirectories(spritePath);
            foreach (var dir in directories)
            {
                string className = Path.GetFileName(dir);
                GenerateClassAnimations(className, dir);
            }
        }

        private void GenerateClassAnimations(string className, string directory)
        {
            // Animation types to generate
            string[] animationTypes = { "idle", "walk", "run", "attack", "cast", "death" };
            string[] directions = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

            foreach (var animType in animationTypes)
            {
                foreach (var direction in directions)
                {
                    GenerateAnimationClip(className, animType, direction, directory);
                }
            }
        }

        private void GenerateAnimationClip(string className, string animationType, string direction, string directory)
        {
            string animName = $"{className}_{animationType}_{direction}";
            string clipPath = Path.Combine(outputPath, "Animations", "Characters", className, $"{animName}.anim");

            // Find sprites for this animation
            string pattern = $"{animationType}_{direction}_*.png";
            var sprites = Directory.GetFiles(directory, pattern)
                .Select(f => AssetDatabase.LoadAssetAtPath<Sprite>(f))
                .Where(s => s != null)
                .OrderBy(s => s.name)
                .ToArray();

            if (sprites.Length == 0)
                return;

            // Create animation clip
            AnimationClip clip = new AnimationClip
            {
                frameRate = 12 // Adjust based on animation type
            };

            // Create sprite animation keyframes
            EditorCurveBinding spriteBinding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] spriteKeyframes = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                spriteKeyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / clip.frameRate,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeyframes);

            // Set animation settings
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = animationType == "idle" || animationType == "walk" || animationType == "run";
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // Save animation clip
            Directory.CreateDirectory(Path.GetDirectoryName(clipPath));
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        #endregion

        #region Helper Methods

        private List<string> GetFilesToProcess()
        {
            var files = new List<string>();
            string[] extensions = { "*.png", "*.jpg", "*.jpeg", "*.swf", "*.mp3", "*.wav", "*.ogg", "*.xml", "*.json" };

            foreach (var ext in extensions)
            {
                files.AddRange(Directory.GetFiles(sourcePath, ext, SearchOption.AllDirectories));
            }

            // Filter by asset type
            if (assetTypeToProcess != AssetType.All)
            {
                files = FilterFilesByType(files, assetTypeToProcess);
            }

            return files;
        }

        private List<string> FilterFilesByType(List<string> files, AssetType type)
        {
            return files.Where(f =>
            {
                string lower = f.ToLower();
                switch (type)
                {
                    case AssetType.Characters:
                        return lower.Contains("character") || lower.Contains("sprite") || lower.Contains("class");
                    case AssetType.Maps:
                        return lower.Contains("map") || lower.Contains("tile") || lower.Contains("background");
                    case AssetType.UI:
                        return lower.Contains("ui") || lower.Contains("interface") || lower.Contains("button") || lower.Contains("icon");
                    case AssetType.Effects:
                        return lower.Contains("effect") || lower.Contains("particle") || lower.Contains("spell");
                    case AssetType.Audio:
                        return lower.EndsWith(".mp3") || lower.EndsWith(".wav") || lower.EndsWith(".ogg");
                    default:
                        return true;
                }
            }).ToList();
        }

        private void CreateOutputDirectories()
        {
            string[] subdirs = {
                "Sprites/Characters",
                "Sprites/Maps",
                "Sprites/UI",
                "Sprites/Effects",
                "Sprites/Monsters",
                "Audio/Music",
                "Audio/SFX",
                "Audio/Ambient",
                "Animations/Characters",
                "Animations/Monsters",
                "Animations/Effects",
                "Atlases",
                "Data",
                "Materials",
                "Prefabs"
            };

            foreach (var subdir in subdirs)
            {
                string path = Path.Combine(outputPath, subdir);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            AssetDatabase.Refresh();
        }

        private string GetRelativeOutputPath(string filePath)
        {
            // Determine output subdirectory based on file content
            string fileName = Path.GetFileName(filePath).ToLower();
            string extension = Path.GetExtension(fileName);

            if (extension == ".mp3" || extension == ".wav" || extension == ".ogg")
            {
                if (fileName.Contains("music") || fileName.Contains("bgm"))
                    return Path.Combine("Audio", "Music", Path.GetFileName(filePath));
                else if (fileName.Contains("ambient"))
                    return Path.Combine("Audio", "Ambient", Path.GetFileName(filePath));
                else
                    return Path.Combine("Audio", "SFX", Path.GetFileName(filePath));
            }
            else if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
            {
                if (fileName.Contains("character") || fileName.Contains("sprite"))
                    return Path.Combine("Sprites", "Characters", Path.GetFileName(filePath));
                else if (fileName.Contains("map") || fileName.Contains("tile"))
                    return Path.Combine("Sprites", "Maps", Path.GetFileName(filePath));
                else if (fileName.Contains("ui") || fileName.Contains("icon"))
                    return Path.Combine("Sprites", "UI", Path.GetFileName(filePath));
                else if (fileName.Contains("effect") || fileName.Contains("particle"))
                    return Path.Combine("Sprites", "Effects", Path.GetFileName(filePath));
                else if (fileName.Contains("monster") || fileName.Contains("mob"))
                    return Path.Combine("Sprites", "Monsters", Path.GetFileName(filePath));
                else
                    return Path.Combine("Sprites", Path.GetFileName(filePath));
            }
            else
            {
                return Path.Combine("Data", Path.GetFileName(filePath));
            }
        }

        private void ClearCache()
        {
            processingResults.Clear();
            processedFiles = 0;
            totalFiles = 0;
            progress = 0;
            currentOperation = "";

            EditorUtility.DisplayDialog("Cache Cleared", "Processing cache has been cleared.", "OK");
        }

        private void ValidateAssets()
        {
            currentOperation = "Validating assets...";
            int missingCount = 0;
            int errorCount = 0;

            // Check for missing textures
            var materials = AssetDatabase.FindAssets("t:Material", new[] { outputPath });
            foreach (var guid in materials)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material != null && material.mainTexture == null)
                {
                    missingCount++;
                    Debug.LogWarning($"Missing texture in material: {path}");
                }
            }

            // Check for broken prefabs
            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { outputPath });
            foreach (var guid in prefabs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null)
                {
                    errorCount++;
                    Debug.LogError($"Broken prefab: {path}");
                }
            }

            EditorUtility.DisplayDialog(
                "Validation Complete",
                $"Found {missingCount} missing textures and {errorCount} errors.",
                "OK"
            );
        }

        private static void ProcessAssetType(AssetType type)
        {
            var window = GetWindow<DofusAssetProcessor>();
            window.assetTypeToProcess = type;
            window.ProcessAssets();
        }

        #endregion
    }

    #region Supporting Classes

    public enum AssetType
    {
        All,
        Characters,
        Maps,
        UI,
        Effects,
        Monsters,
        Audio
    }

    public class AssetProcessingResult
    {
        public string FileName { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    #endregion
}