using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace GOFUS.Editor.AssetMigration
{
    /// <summary>
    /// Asset validation and reporting system for Phase 7
    /// Tracks migration progress and identifies missing assets
    /// </summary>
    public class AssetValidationReport : EditorWindow
    {
        #region Properties

        private AssetReport currentReport;
        private Vector2 scrollPosition;
        private string reportPath = "Assets/_Project/ImportedAssets";
        private bool autoScan = true;
        private ReportView currentView = ReportView.Summary;

        // Filter options
        private bool showCharacters = true;
        private bool showMaps = true;
        private bool showUI = true;
        private bool showEffects = true;
        private bool showAudio = true;
        private bool showOnlyMissing = false;

        // Expected asset counts
        private readonly Dictionary<AssetCategory, int> expectedCounts = new Dictionary<AssetCategory, int>
        {
            { AssetCategory.Characters, 18 * 2 * 8 * 8 }, // 18 classes × 2 genders × 8 directions × 8 animations
            { AssetCategory.Maps, 500 },
            { AssetCategory.UI, 200 },
            { AssetCategory.Effects, 100 },
            { AssetCategory.Audio, 50 },
            { AssetCategory.Monsters, 200 }
        };

        #endregion

        #region Window

        [MenuItem("GOFUS/Asset Migration/Validation Report")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetValidationReport>("Asset Validation");
            window.minSize = new Vector2(600, 500);
            window.Show();
            window.GenerateReport();
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            DrawHeader();
            DrawToolbar();

            if (currentReport == null)
            {
                EditorGUILayout.HelpBox("Click 'Generate Report' to scan assets.", MessageType.Info);
                return;
            }

            switch (currentView)
            {
                case ReportView.Summary:
                    DrawSummaryView();
                    break;
                case ReportView.Details:
                    DrawDetailsView();
                    break;
                case ReportView.Missing:
                    DrawMissingAssetsView();
                    break;
                case ReportView.Progress:
                    DrawProgressView();
                    break;
            }
        }

        private void DrawHeader()
        {
            GUILayout.Label("Asset Migration Validation", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Report Date: {currentReport?.GenerationTime ?? "No report"}", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.Separator();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Generate Report", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                GenerateReport();
            }

            GUILayout.Space(10);

            // View tabs
            if (GUILayout.Toggle(currentView == ReportView.Summary, "Summary", EditorStyles.toolbarButton))
                currentView = ReportView.Summary;

            if (GUILayout.Toggle(currentView == ReportView.Details, "Details", EditorStyles.toolbarButton))
                currentView = ReportView.Details;

            if (GUILayout.Toggle(currentView == ReportView.Missing, "Missing", EditorStyles.toolbarButton))
                currentView = ReportView.Missing;

            if (GUILayout.Toggle(currentView == ReportView.Progress, "Progress", EditorStyles.toolbarButton))
                currentView = ReportView.Progress;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Export", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                ExportReport();
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Views

        private void DrawSummaryView()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Asset Migration Summary", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Overall progress
            DrawProgressBar("Overall Progress", currentReport.OverallProgress);

            EditorGUILayout.Space(10);

            // Category summaries
            foreach (var category in currentReport.Categories)
            {
                DrawCategorySummary(category);
            }

            // Statistics
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Total Assets: {currentReport.TotalAssets}");
            EditorGUILayout.LabelField($"Valid Assets: {currentReport.ValidAssets}");
            EditorGUILayout.LabelField($"Missing Textures: {currentReport.MissingTextures}");
            EditorGUILayout.LabelField($"Broken References: {currentReport.BrokenReferences}");
            EditorGUILayout.LabelField($"Unused Assets: {currentReport.UnusedAssets}");
            EditorGUILayout.LabelField($"Total File Size: {FormatFileSize(currentReport.TotalFileSize)}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        private void DrawDetailsView()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Detailed Asset List", EditorStyles.boldLabel);

            // Filters
            EditorGUILayout.BeginHorizontal();
            showCharacters = GUILayout.Toggle(showCharacters, "Characters", EditorStyles.miniButton);
            showMaps = GUILayout.Toggle(showMaps, "Maps", EditorStyles.miniButton);
            showUI = GUILayout.Toggle(showUI, "UI", EditorStyles.miniButton);
            showEffects = GUILayout.Toggle(showEffects, "Effects", EditorStyles.miniButton);
            showAudio = GUILayout.Toggle(showAudio, "Audio", EditorStyles.miniButton);
            GUILayout.FlexibleSpace();
            showOnlyMissing = GUILayout.Toggle(showOnlyMissing, "Only Missing", EditorStyles.miniButton);
            EditorGUILayout.EndHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var category in currentReport.Categories)
            {
                if (!ShouldShowCategory(category.Type)) continue;

                EditorGUILayout.LabelField(category.Name, EditorStyles.boldLabel);

                foreach (var asset in category.Assets)
                {
                    if (showOnlyMissing && asset.IsValid) continue;

                    DrawAssetEntry(asset);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawMissingAssetsView()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Missing Assets", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var missingAssets = GetMissingAssetsList();

            if (missingAssets.Count == 0)
            {
                EditorGUILayout.HelpBox("No missing assets detected!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"Found {missingAssets.Count} missing assets", MessageType.Warning);

                foreach (var missing in missingAssets)
                {
                    EditorGUILayout.BeginHorizontal();

                    GUI.color = GetSeverityColor(missing.Severity);
                    EditorGUILayout.LabelField(missing.AssetName, GUILayout.Width(200));
                    GUI.color = Color.white;

                    EditorGUILayout.LabelField(missing.Category.ToString(), GUILayout.Width(100));
                    EditorGUILayout.LabelField(missing.Reason, EditorStyles.miniLabel);

                    if (GUILayout.Button("Fix", GUILayout.Width(40)))
                    {
                        AttemptAutoFix(missing);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawProgressView()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Migration Progress", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Overall timeline
            DrawTimeline();

            EditorGUILayout.Space(20);

            // Category progress
            foreach (var category in currentReport.Categories)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField(category.Name, EditorStyles.boldLabel);
                DrawProgressBar($"Progress", category.Progress);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Assets: {category.Assets.Count} / {GetExpectedCount(category.Type)}");
                EditorGUILayout.LabelField($"Valid: {category.ValidCount}");
                EditorGUILayout.LabelField($"Missing: {category.MissingCount}");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            // Recommendations
            DrawRecommendations();

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Report Generation

        private void GenerateReport()
        {
            currentReport = new AssetReport
            {
                GenerationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            EditorUtility.DisplayProgressBar("Generating Report", "Scanning assets...", 0);

            try
            {
                // Scan each category
                ScanCharacterAssets();
                ScanMapAssets();
                ScanUIAssets();
                ScanEffectAssets();
                ScanAudioAssets();
                ScanMonsterAssets();

                // Calculate statistics
                CalculateStatistics();

                EditorUtility.DisplayDialog(
                    "Report Generated",
                    $"Scanned {currentReport.TotalAssets} assets.\n" +
                    $"Valid: {currentReport.ValidAssets}\n" +
                    $"Issues: {currentReport.MissingTextures + currentReport.BrokenReferences}",
                    "OK"
                );
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void ScanCharacterAssets()
        {
            var category = new AssetCategoryReport
            {
                Name = "Characters",
                Type = AssetCategory.Characters
            };

            string path = Path.Combine(reportPath, "Sprites", "Characters");
            if (Directory.Exists(path))
            {
                // Scan for character sprites
                var sprites = AssetDatabase.FindAssets("t:Sprite", new[] { path });
                foreach (var guid in sprites)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = ValidateAsset(assetPath, ValidationAssetType.Sprite);
                    category.Assets.Add(asset);
                }

                // Scan for animations
                var animations = AssetDatabase.FindAssets("t:AnimationClip", new[] { path.Replace("Sprites", "Animations") });
                foreach (var guid in animations)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = ValidateAsset(assetPath, ValidationAssetType.Animation);
                    category.Assets.Add(asset);
                }
            }

            category.CalculateProgress(GetExpectedCount(AssetCategory.Characters));
            currentReport.Categories.Add(category);
        }

        private void ScanMapAssets()
        {
            var category = new AssetCategoryReport
            {
                Name = "Maps",
                Type = AssetCategory.Maps
            };

            string path = Path.Combine(reportPath, "Sprites", "Maps");
            if (Directory.Exists(path))
            {
                var tiles = AssetDatabase.FindAssets("t:Sprite", new[] { path });
                foreach (var guid in tiles)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = ValidateAsset(assetPath, ValidationAssetType.Sprite);
                    category.Assets.Add(asset);
                }
            }

            category.CalculateProgress(GetExpectedCount(AssetCategory.Maps));
            currentReport.Categories.Add(category);
        }

        private void ScanUIAssets()
        {
            var category = new AssetCategoryReport
            {
                Name = "UI Elements",
                Type = AssetCategory.UI
            };

            string path = Path.Combine(reportPath, "Sprites", "UI");
            if (Directory.Exists(path))
            {
                var uiElements = AssetDatabase.FindAssets("t:Sprite", new[] { path });
                foreach (var guid in uiElements)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = ValidateAsset(assetPath, ValidationAssetType.Sprite);
                    category.Assets.Add(asset);
                }
            }

            category.CalculateProgress(GetExpectedCount(AssetCategory.UI));
            currentReport.Categories.Add(category);
        }

        private void ScanEffectAssets()
        {
            var category = new AssetCategoryReport
            {
                Name = "Effects",
                Type = AssetCategory.Effects
            };

            string path = Path.Combine(reportPath, "Sprites", "Effects");
            if (Directory.Exists(path))
            {
                var effects = AssetDatabase.FindAssets("t:Sprite t:Material t:Prefab", new[] { path });
                foreach (var guid in effects)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = ValidateAsset(assetPath, ValidationAssetType.Effect);
                    category.Assets.Add(asset);
                }
            }

            category.CalculateProgress(GetExpectedCount(AssetCategory.Effects));
            currentReport.Categories.Add(category);
        }

        private void ScanAudioAssets()
        {
            var category = new AssetCategoryReport
            {
                Name = "Audio",
                Type = AssetCategory.Audio
            };

            string path = Path.Combine(reportPath, "Audio");
            if (Directory.Exists(path))
            {
                var audioClips = AssetDatabase.FindAssets("t:AudioClip", new[] { path });
                foreach (var guid in audioClips)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = ValidateAsset(assetPath, ValidationAssetType.Audio);
                    category.Assets.Add(asset);
                }
            }

            category.CalculateProgress(GetExpectedCount(AssetCategory.Audio));
            currentReport.Categories.Add(category);
        }

        private void ScanMonsterAssets()
        {
            var category = new AssetCategoryReport
            {
                Name = "Monsters",
                Type = AssetCategory.Monsters
            };

            string path = Path.Combine(reportPath, "Sprites", "Monsters");
            if (Directory.Exists(path))
            {
                var monsters = AssetDatabase.FindAssets("t:Sprite", new[] { path });
                foreach (var guid in monsters)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = ValidateAsset(assetPath, ValidationAssetType.Sprite);
                    category.Assets.Add(asset);
                }
            }

            category.CalculateProgress(GetExpectedCount(AssetCategory.Monsters));
            currentReport.Categories.Add(category);
        }

        private AssetValidationEntry ValidateAsset(string path, ValidationAssetType type)
        {
            var entry = new AssetValidationEntry
            {
                Path = path,
                Name = Path.GetFileNameWithoutExtension(path),
                Type = type,
                IsValid = true
            };

            // Check if file exists
            if (!File.Exists(path))
            {
                entry.IsValid = false;
                entry.Issues.Add("File not found");
                return entry;
            }

            // Get file info
            FileInfo fileInfo = new FileInfo(path);
            entry.FileSize = fileInfo.Length;

            // Type-specific validation
            switch (type)
            {
                case ValidationAssetType.Sprite:
                    ValidateSprite(entry);
                    break;
                case ValidationAssetType.Animation:
                    ValidateAnimation(entry);
                    break;
                case ValidationAssetType.Audio:
                    ValidateAudio(entry);
                    break;
                case ValidationAssetType.Effect:
                    ValidateEffect(entry);
                    break;
            }

            return entry;
        }

        private void ValidateSprite(AssetValidationEntry entry)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entry.Path);
            if (sprite == null)
            {
                entry.IsValid = false;
                entry.Issues.Add("Failed to load sprite");
                return;
            }

            // Check texture
            if (sprite.texture == null)
            {
                entry.IsValid = false;
                entry.Issues.Add("Missing texture");
            }

            // Check size
            if (sprite.texture != null && (sprite.texture.width > 4096 || sprite.texture.height > 4096))
            {
                entry.Issues.Add("Texture too large (>4096px)");
            }
        }

        private void ValidateAnimation(AssetValidationEntry entry)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(entry.Path);
            if (clip == null)
            {
                entry.IsValid = false;
                entry.Issues.Add("Failed to load animation");
                return;
            }

            if (clip.length == 0)
            {
                entry.Issues.Add("Empty animation clip");
            }

            if (clip.frameRate < 12)
            {
                entry.Issues.Add("Low framerate (<12 fps)");
            }
        }

        private void ValidateAudio(AssetValidationEntry entry)
        {
            var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(entry.Path);
            if (audioClip == null)
            {
                entry.IsValid = false;
                entry.Issues.Add("Failed to load audio");
                return;
            }

            if (audioClip.length > 60)
            {
                entry.Issues.Add("Long audio clip (>60s)");
            }
        }

        private void ValidateEffect(AssetValidationEntry entry)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(entry.Path);
            if (asset == null)
            {
                entry.IsValid = false;
                entry.Issues.Add("Failed to load effect");
            }
        }

        private void CalculateStatistics()
        {
            currentReport.TotalAssets = currentReport.Categories.Sum(c => c.Assets.Count);
            currentReport.ValidAssets = currentReport.Categories.Sum(c => c.ValidCount);
            currentReport.MissingTextures = currentReport.Categories.Sum(c =>
                c.Assets.Count(a => a.Issues.Contains("Missing texture")));
            currentReport.BrokenReferences = currentReport.Categories.Sum(c =>
                c.Assets.Count(a => a.Issues.Contains("Failed to load")));
            currentReport.TotalFileSize = currentReport.Categories.Sum(c =>
                c.Assets.Sum(a => a.FileSize));

            // Calculate overall progress
            int totalExpected = expectedCounts.Values.Sum();
            currentReport.OverallProgress = (float)currentReport.ValidAssets / totalExpected;
        }

        #endregion

        #region Helper Methods

        private void DrawCategorySummary(AssetCategoryReport category)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(category.Name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{category.Assets.Count} assets", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            DrawProgressBar("", category.Progress);

            EditorGUILayout.BeginHorizontal();
            GUI.color = Color.green;
            EditorGUILayout.LabelField($"✓ Valid: {category.ValidCount}", EditorStyles.miniLabel);
            GUI.color = Color.red;
            EditorGUILayout.LabelField($"✗ Issues: {category.MissingCount}", EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawProgressBar(string label, float progress)
        {
            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.LabelField(label);
            }

            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
            EditorGUI.ProgressBar(rect, progress, $"{(progress * 100):F1}%");
        }

        private void DrawAssetEntry(AssetValidationEntry asset)
        {
            EditorGUILayout.BeginHorizontal();

            GUI.color = asset.IsValid ? Color.green : Color.red;
            EditorGUILayout.LabelField(asset.IsValid ? "✓" : "✗", GUILayout.Width(20));
            GUI.color = Color.white;

            EditorGUILayout.LabelField(asset.Name, GUILayout.Width(200));
            EditorGUILayout.LabelField(asset.Type.ToString(), GUILayout.Width(80));
            EditorGUILayout.LabelField(FormatFileSize(asset.FileSize), GUILayout.Width(80));

            if (asset.Issues.Count > 0)
            {
                EditorGUILayout.LabelField(string.Join(", ", asset.Issues), EditorStyles.miniLabel);
            }

            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.Path);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTimeline()
        {
            EditorGUILayout.LabelField("Migration Timeline", EditorStyles.boldLabel);

            string[] phases = {
                "Setup Tools",
                "Character Assets",
                "Map Assets",
                "UI Elements",
                "Effects & Audio",
                "Optimization",
                "Validation"
            };

            float[] progress = { 1.0f, 0.8f, 0.6f, 0.4f, 0.3f, 0.1f, 0.0f };

            for (int i = 0; i < phases.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(phases[i], GUILayout.Width(120));
                DrawProgressBar("", progress[i]);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawRecommendations()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Recommendations", EditorStyles.boldLabel);

            var recommendations = GenerateRecommendations();

            if (recommendations.Count == 0)
            {
                EditorGUILayout.HelpBox("No recommendations at this time.", MessageType.Info);
            }
            else
            {
                foreach (var rec in recommendations)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("•", GUILayout.Width(15));
                    EditorGUILayout.LabelField(rec, EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private List<string> GenerateRecommendations()
        {
            var recommendations = new List<string>();

            if (currentReport == null) return recommendations;

            // Check character assets
            var charCategory = currentReport.Categories.FirstOrDefault(c => c.Type == AssetCategory.Characters);
            if (charCategory != null && charCategory.Progress < 0.5f)
            {
                recommendations.Add("Priority: Extract and process character sprites. These are essential for gameplay.");
            }

            // Check for missing UI
            var uiCategory = currentReport.Categories.FirstOrDefault(c => c.Type == AssetCategory.UI);
            if (uiCategory != null && uiCategory.Progress < 0.3f)
            {
                recommendations.Add("UI elements are critically low. Import buttons, icons, and window frames.");
            }

            // Check file sizes
            if (currentReport.TotalFileSize > 500 * 1024 * 1024) // 500MB
            {
                recommendations.Add("Consider creating sprite atlases to reduce texture memory usage.");
            }

            // Check for broken references
            if (currentReport.BrokenReferences > 10)
            {
                recommendations.Add($"Found {currentReport.BrokenReferences} broken references. Run 'Fix Missing References' tool.");
            }

            return recommendations;
        }

        private bool ShouldShowCategory(AssetCategory type)
        {
            switch (type)
            {
                case AssetCategory.Characters: return showCharacters;
                case AssetCategory.Maps: return showMaps;
                case AssetCategory.UI: return showUI;
                case AssetCategory.Effects: return showEffects;
                case AssetCategory.Audio: return showAudio;
                default: return true;
            }
        }

        private List<MissingAsset> GetMissingAssetsList()
        {
            var missingAssets = new List<MissingAsset>();

            // Check for essential missing assets
            string[] essentialCharacters = { "Feca", "Osamodas", "Enutrof", "Sram", "Xelor" };
            foreach (var charClass in essentialCharacters)
            {
                bool found = currentReport.Categories
                    .Where(c => c.Type == AssetCategory.Characters)
                    .SelectMany(c => c.Assets)
                    .Any(a => a.Name.Contains(charClass));

                if (!found)
                {
                    missingAssets.Add(new MissingAsset
                    {
                        AssetName = $"{charClass} sprites",
                        Category = AssetCategory.Characters,
                        Reason = "Character class not found",
                        Severity = MissingSeverity.Critical
                    });
                }
            }

            // Add other missing assets from validation
            foreach (var category in currentReport.Categories)
            {
                foreach (var asset in category.Assets.Where(a => !a.IsValid))
                {
                    missingAssets.Add(new MissingAsset
                    {
                        AssetName = asset.Name,
                        Category = category.Type,
                        Reason = string.Join(", ", asset.Issues),
                        Severity = DetermineSeverity(asset)
                    });
                }
            }

            return missingAssets;
        }

        private MissingSeverity DetermineSeverity(AssetValidationEntry asset)
        {
            if (asset.Issues.Contains("Failed to load"))
                return MissingSeverity.Critical;
            if (asset.Issues.Contains("Missing texture"))
                return MissingSeverity.High;
            if (asset.Issues.Contains("Too large"))
                return MissingSeverity.Medium;
            return MissingSeverity.Low;
        }

        private Color GetSeverityColor(MissingSeverity severity)
        {
            switch (severity)
            {
                case MissingSeverity.Critical: return Color.red;
                case MissingSeverity.High: return new Color(1, 0.5f, 0);
                case MissingSeverity.Medium: return Color.yellow;
                case MissingSeverity.Low: return Color.white;
                default: return Color.white;
            }
        }

        private void AttemptAutoFix(MissingAsset missing)
        {
            EditorUtility.DisplayDialog(
                "Auto Fix",
                $"Attempting to fix: {missing.AssetName}\n\n" +
                "This feature would:\n" +
                "1. Search for alternative sources\n" +
                "2. Generate placeholder if needed\n" +
                "3. Update references",
                "OK"
            );

            // TODO: Implement auto-fix logic
        }

        private int GetExpectedCount(AssetCategory category)
        {
            return expectedCounts.ContainsKey(category) ? expectedCounts[category] : 100;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:F2} {sizes[order]}";
        }

        private void ExportReport()
        {
            if (currentReport == null)
            {
                EditorUtility.DisplayDialog("Error", "No report to export!", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel(
                "Export Report",
                "Assets",
                $"AssetReport_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                "json"
            );

            if (!string.IsNullOrEmpty(path))
            {
                string json = JsonUtility.ToJson(currentReport, true);
                File.WriteAllText(path, json);
                EditorUtility.DisplayDialog("Success", "Report exported successfully!", "OK");
            }
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class AssetReport
    {
        public string GenerationTime;
        public List<AssetCategoryReport> Categories = new List<AssetCategoryReport>();
        public int TotalAssets;
        public int ValidAssets;
        public int MissingTextures;
        public int BrokenReferences;
        public int UnusedAssets;
        public long TotalFileSize;
        public float OverallProgress;
    }

    [Serializable]
    public class AssetCategoryReport
    {
        public string Name;
        public AssetCategory Type;
        public List<AssetValidationEntry> Assets = new List<AssetValidationEntry>();
        public float Progress;
        public int ValidCount => Assets.Count(a => a.IsValid);
        public int MissingCount => Assets.Count(a => !a.IsValid);

        public void CalculateProgress(int expectedCount)
        {
            Progress = expectedCount > 0 ? (float)ValidCount / expectedCount : 0;
        }
    }

    [Serializable]
    public class AssetValidationEntry
    {
        public string Path;
        public string Name;
        public ValidationAssetType Type;
        public bool IsValid;
        public long FileSize;
        public List<string> Issues = new List<string>();
    }

    public class MissingAsset
    {
        public string AssetName;
        public AssetCategory Category;
        public string Reason;
        public MissingSeverity Severity;
    }

    public enum AssetCategory
    {
        Characters,
        Maps,
        UI,
        Effects,
        Audio,
        Monsters
    }

    public enum ValidationAssetType
    {
        Sprite,
        Animation,
        Audio,
        Effect,
        Prefab,
        Material
    }

    public enum ReportView
    {
        Summary,
        Details,
        Missing,
        Progress
    }

    public enum MissingSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    #endregion
}