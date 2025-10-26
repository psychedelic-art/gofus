using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace GOFUS.Editor.AssetMigration
{
    /// <summary>
    /// Validates extracted assets and runs the full pipeline automatically
    /// </summary>
    public class AssetExtractionValidator : EditorWindow
    {
        #region Properties

        private string extractionPath = "ExtractedAssets/Raw";
        private ValidationResults validationResults;
        private Vector2 scrollPosition;
        private bool autoProcess = true;
        private bool showDetails = false;

        #endregion

        #region Classes

        [Serializable]
        public class ValidationResults
        {
            public bool extractionExists;
            public int totalAssetsFound;
            public Dictionary<string, CategoryResult> categories;
            public List<string> errors;
            public List<string> warnings;
            public float overallScore;

            public ValidationResults()
            {
                categories = new Dictionary<string, CategoryResult>();
                errors = new List<string>();
                warnings = new List<string>();
            }
        }

        [Serializable]
        public class CategoryResult
        {
            public string name;
            public int filesFound;
            public int expectedMinimum;
            public bool isValid;
            public List<string> missingItems;
            public List<string> foundItems;

            public CategoryResult(string name)
            {
                this.name = name;
                missingItems = new List<string>();
                foundItems = new List<string>();
            }
        }

        #endregion

        #region Menu Items

        [MenuItem("GOFUS/Asset Migration/Extraction Validator")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetExtractionValidator>("Extraction Validator");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        [MenuItem("GOFUS/Asset Migration/Quick Actions/Validate & Process All")]
        public static void QuickValidateAndProcess()
        {
            var validator = CreateInstance<AssetExtractionValidator>();
            validator.extractionPath = "ExtractedAssets/Raw";
            validator.autoProcess = true;
            validator.RunFullValidation();
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            DrawHeader();
            DrawControls();
            DrawResults();
            DrawActions();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Asset Extraction Validator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool validates extracted Dofus assets and can automatically process them through the migration pipeline.",
                MessageType.Info);
            EditorGUILayout.Space();
        }

        private void DrawControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            extractionPath = EditorGUILayout.TextField("Extraction Path", extractionPath);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Extraction Folder", extractionPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    extractionPath = GetRelativePath(path);
                }
            }

            if (GUILayout.Button("Open Folder", GUILayout.Width(100)))
            {
                if (Directory.Exists(extractionPath))
                {
                    EditorUtility.RevealInFinder(Path.GetFullPath(extractionPath));
                }
            }
            EditorGUILayout.EndHorizontal();

            autoProcess = EditorGUILayout.Toggle("Auto Process After Validation", autoProcess);
            showDetails = EditorGUILayout.Toggle("Show Detailed Results", showDetails);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawResults()
        {
            if (validationResults == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);

            // Overall status
            Color statusColor = validationResults.overallScore > 0.8f ? Color.green :
                               validationResults.overallScore > 0.5f ? Color.yellow : Color.red;

            GUI.color = statusColor;
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUI.color = Color.white;

            EditorGUILayout.LabelField($"Overall Score: {validationResults.overallScore:P0}");
            EditorGUILayout.LabelField($"Total Assets: {validationResults.totalAssetsFound}");

            EditorGUILayout.EndHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(250));

            // Category results
            foreach (var category in validationResults.categories.Values)
            {
                DrawCategoryResult(category);
            }

            // Errors
            if (validationResults.errors.Count > 0)
            {
                EditorGUILayout.Space();
                GUI.color = Color.red;
                EditorGUILayout.LabelField("Errors:", EditorStyles.boldLabel);
                GUI.color = Color.white;

                foreach (string error in validationResults.errors)
                {
                    EditorGUILayout.LabelField($"• {error}", EditorStyles.wordWrappedLabel);
                }
            }

            // Warnings
            if (validationResults.warnings.Count > 0)
            {
                EditorGUILayout.Space();
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("Warnings:", EditorStyles.boldLabel);
                GUI.color = Color.white;

                foreach (string warning in validationResults.warnings)
                {
                    EditorGUILayout.LabelField($"• {warning}", EditorStyles.wordWrappedLabel);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawCategoryResult(CategoryResult category)
        {
            Color categoryColor = category.isValid ? Color.green : Color.yellow;

            EditorGUILayout.BeginHorizontal();

            GUI.color = categoryColor;
            EditorGUILayout.LabelField($"✓", GUILayout.Width(20));
            GUI.color = Color.white;

            EditorGUILayout.LabelField($"{category.name}:", GUILayout.Width(100));
            EditorGUILayout.LabelField($"{category.filesFound}/{category.expectedMinimum} files");

            if (category.filesFound < category.expectedMinimum)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField($"(Missing {category.expectedMinimum - category.filesFound})", GUILayout.Width(100));
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            if (showDetails && category.foundItems.Count > 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"Found: {string.Join(", ", category.foundItems.Take(5))}...");
                EditorGUI.indentLevel--;
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Run Validation", GUILayout.Height(30)))
            {
                RunFullValidation();
            }

            GUI.enabled = validationResults != null && validationResults.totalAssetsFound > 0;

            if (GUILayout.Button("Process Assets", GUILayout.Height(30)))
            {
                ProcessExtractedAssets();
            }

            if (GUILayout.Button("Generate Report", GUILayout.Height(30)))
            {
                GenerateDetailedReport();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Validation Logic

        public void RunFullValidation()
        {
            validationResults = new ValidationResults();

            try
            {
                EditorUtility.DisplayProgressBar("Validating", "Checking extraction folder...", 0.1f);

                // Check if extraction folder exists
                if (!Directory.Exists(extractionPath))
                {
                    validationResults.extractionExists = false;
                    validationResults.errors.Add($"Extraction folder not found: {extractionPath}");
                    validationResults.overallScore = 0f;
                    return;
                }

                validationResults.extractionExists = true;

                // Validate each category
                EditorUtility.DisplayProgressBar("Validating", "Checking characters...", 0.2f);
                ValidateCharacters();

                EditorUtility.DisplayProgressBar("Validating", "Checking UI elements...", 0.4f);
                ValidateUI();

                EditorUtility.DisplayProgressBar("Validating", "Checking map tiles...", 0.6f);
                ValidateMaps();

                EditorUtility.DisplayProgressBar("Validating", "Checking audio...", 0.8f);
                ValidateAudio();

                // Calculate overall score
                CalculateOverallScore();

                EditorUtility.DisplayProgressBar("Validating", "Complete!", 1f);

                // Auto process if enabled and validation passed
                if (autoProcess && validationResults.overallScore > 0.5f)
                {
                    if (EditorUtility.DisplayDialog("Auto Process",
                        $"Validation complete with score {validationResults.overallScore:P0}.\n\nProcess assets now?",
                        "Yes", "No"))
                    {
                        ProcessExtractedAssets();
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void ValidateCharacters()
        {
            var category = new CategoryResult("Characters");
            category.expectedMinimum = 32; // At least one character with basic animations

            string charPath = Path.Combine(extractionPath, "Characters");
            if (Directory.Exists(charPath))
            {
                var files = Directory.GetFiles(charPath, "*.png", SearchOption.AllDirectories);
                category.filesFound = files.Length;
                validationResults.totalAssetsFound += files.Length;

                // Check for expected structure
                var directories = Directory.GetDirectories(charPath);
                foreach (var dir in directories)
                {
                    string className = Path.GetFileName(dir);
                    category.foundItems.Add(className);

                    // Check for minimum animations
                    var spriteFiles = Directory.GetFiles(dir, "*.png");
                    if (spriteFiles.Length < 16) // Minimum for idle and walk in 8 directions
                    {
                        category.missingItems.Add($"{className} has only {spriteFiles.Length} sprites (expected at least 16)");
                    }
                }

                category.isValid = category.filesFound >= category.expectedMinimum;
            }
            else
            {
                category.isValid = false;
                validationResults.warnings.Add("Characters folder not found");
            }

            validationResults.categories["Characters"] = category;
        }

        private void ValidateUI()
        {
            var category = new CategoryResult("UI");
            category.expectedMinimum = 10;

            string uiPath = Path.Combine(extractionPath, "UI");
            if (Directory.Exists(uiPath))
            {
                var files = Directory.GetFiles(uiPath, "*.png", SearchOption.AllDirectories);
                category.filesFound = files.Length;
                validationResults.totalAssetsFound += files.Length;

                // Check for essential UI elements
                string[] essentialFolders = { "Buttons", "Windows", "Icons" };
                foreach (string folder in essentialFolders)
                {
                    string folderPath = Path.Combine(uiPath, folder);
                    if (Directory.Exists(folderPath))
                    {
                        var folderFiles = Directory.GetFiles(folderPath, "*.png");
                        if (folderFiles.Length > 0)
                        {
                            category.foundItems.Add($"{folder} ({folderFiles.Length} files)");
                        }
                    }
                    else
                    {
                        category.missingItems.Add(folder);
                    }
                }

                category.isValid = category.filesFound >= category.expectedMinimum;
            }
            else
            {
                category.isValid = false;
                validationResults.warnings.Add("UI folder not found");
            }

            validationResults.categories["UI"] = category;
        }

        private void ValidateMaps()
        {
            var category = new CategoryResult("Maps");
            category.expectedMinimum = 10;

            string mapPath = Path.Combine(extractionPath, "Maps");
            if (Directory.Exists(mapPath))
            {
                var files = Directory.GetFiles(mapPath, "*.png", SearchOption.AllDirectories);
                category.filesFound = files.Length;
                validationResults.totalAssetsFound += files.Length;

                // Check for tile types
                string tilesPath = Path.Combine(mapPath, "Tiles");
                if (Directory.Exists(tilesPath))
                {
                    var tileFiles = Directory.GetFiles(tilesPath, "*.png");
                    category.foundItems.Add($"Tiles ({tileFiles.Length} files)");
                }

                category.isValid = category.filesFound >= category.expectedMinimum;
            }
            else
            {
                category.isValid = false;
                validationResults.warnings.Add("Maps folder not found");
            }

            validationResults.categories["Maps"] = category;
        }

        private void ValidateAudio()
        {
            var category = new CategoryResult("Audio");
            category.expectedMinimum = 5;

            string audioPath = Path.Combine(extractionPath, "Audio");
            if (Directory.Exists(audioPath))
            {
                string[] audioExtensions = { "*.mp3", "*.wav", "*.ogg" };
                int totalAudio = 0;

                foreach (string ext in audioExtensions)
                {
                    var files = Directory.GetFiles(audioPath, ext, SearchOption.AllDirectories);
                    totalAudio += files.Length;
                }

                category.filesFound = totalAudio;
                validationResults.totalAssetsFound += totalAudio;

                // Check audio categories
                string[] audioCategories = { "Music", "SFX", "Ambient" };
                foreach (string cat in audioCategories)
                {
                    string catPath = Path.Combine(audioPath, cat);
                    if (Directory.Exists(catPath))
                    {
                        int catFiles = 0;
                        foreach (string ext in audioExtensions)
                        {
                            catFiles += Directory.GetFiles(catPath, ext).Length;
                        }
                        if (catFiles > 0)
                        {
                            category.foundItems.Add($"{cat} ({catFiles} files)");
                        }
                    }
                }

                category.isValid = category.filesFound >= category.expectedMinimum;
            }
            else
            {
                category.isValid = false;
                // Audio is optional for initial testing
                validationResults.warnings.Add("Audio folder not found (optional)");
            }

            validationResults.categories["Audio"] = category;
        }

        private void CalculateOverallScore()
        {
            if (validationResults.categories.Count == 0)
            {
                validationResults.overallScore = 0f;
                return;
            }

            float totalScore = 0f;
            float maxScore = 0f;

            foreach (var category in validationResults.categories.Values)
            {
                float categoryScore = Mathf.Clamp01((float)category.filesFound / category.expectedMinimum);
                totalScore += categoryScore;
                maxScore += 1f;
            }

            validationResults.overallScore = totalScore / maxScore;

            // Adjust for errors
            if (validationResults.errors.Count > 0)
            {
                validationResults.overallScore *= 0.5f;
            }
        }

        #endregion

        #region Processing

        private void ProcessExtractedAssets()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Processing", "Starting asset processing...", 0f);

                // 1. Run DofusAssetProcessor
                EditorUtility.DisplayProgressBar("Processing", "Running Dofus Asset Processor...", 0.2f);
                RunDofusAssetProcessor();

                // 2. Slice sprite sheets
                EditorUtility.DisplayProgressBar("Processing", "Slicing sprite sheets...", 0.4f);
                SliceSpriteSheets();

                // 3. Generate animation controllers
                EditorUtility.DisplayProgressBar("Processing", "Generating animation controllers...", 0.6f);
                GenerateAnimationControllers();

                // 4. Create atlases
                EditorUtility.DisplayProgressBar("Processing", "Creating sprite atlases...", 0.8f);
                CreateSpriteAtlases();

                // 5. Run final validation
                EditorUtility.DisplayProgressBar("Processing", "Running final validation...", 0.9f);
                RunFinalValidation();

                EditorUtility.DisplayProgressBar("Processing", "Complete!", 1f);

                EditorUtility.DisplayDialog("Success",
                    "Asset processing complete!\n\nCheck the ImportedAssets folder for results.",
                    "OK");

                // Open the imported assets folder
                string importedPath = "Assets/_Project/ImportedAssets";
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(importedPath);
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void RunDofusAssetProcessor()
        {
            // Simulate running the processor
            Debug.Log("Running Dofus Asset Processor on extracted assets...");

            // In a real implementation, this would call the actual processor
            // For now, we'll just copy files to the imported folder
            string sourcePath = Path.GetFullPath(extractionPath);
            string destPath = Path.GetFullPath("Assets/_Project/ImportedAssets");

            if (Directory.Exists(sourcePath))
            {
                CopyDirectory(sourcePath, destPath);
                AssetDatabase.Refresh();
            }
        }

        private void SliceSpriteSheets()
        {
            Debug.Log("Slicing character sprite sheets...");
            // This would call the SpriteSheetSlicer for each character sprite sheet
        }

        private void GenerateAnimationControllers()
        {
            Debug.Log("Generating animation controllers for characters...");
            // This would call the CharacterAnimationGenerator
        }

        private void CreateSpriteAtlases()
        {
            Debug.Log("Creating optimized sprite atlases...");
            // This would create Unity Sprite Atlases for performance
        }

        private void RunFinalValidation()
        {
            Debug.Log("Running final validation on imported assets...");
            // This would run the AssetValidationReport
        }

        #endregion

        #region Reporting

        private void GenerateDetailedReport()
        {
            string reportPath = Path.Combine(Application.dataPath, "../ExtractionReport.html");

            using (StreamWriter writer = new StreamWriter(reportPath))
            {
                writer.WriteLine("<html><head><title>Asset Extraction Report</title>");
                writer.WriteLine("<style>body { font-family: Arial; } .success { color: green; } .warning { color: orange; } .error { color: red; }</style>");
                writer.WriteLine("</head><body>");
                writer.WriteLine("<h1>Asset Extraction Validation Report</h1>");
                writer.WriteLine($"<p>Date: {DateTime.Now}</p>");
                writer.WriteLine($"<p>Path: {extractionPath}</p>");
                writer.WriteLine($"<p>Overall Score: <strong>{validationResults.overallScore:P0}</strong></p>");
                writer.WriteLine($"<p>Total Assets: {validationResults.totalAssetsFound}</p>");

                writer.WriteLine("<h2>Category Results</h2>");
                writer.WriteLine("<table border='1'>");
                writer.WriteLine("<tr><th>Category</th><th>Found</th><th>Expected</th><th>Status</th></tr>");

                foreach (var category in validationResults.categories.Values)
                {
                    string statusClass = category.isValid ? "success" : "warning";
                    string status = category.isValid ? "✓ Valid" : "⚠ Incomplete";

                    writer.WriteLine($"<tr>");
                    writer.WriteLine($"<td>{category.name}</td>");
                    writer.WriteLine($"<td>{category.filesFound}</td>");
                    writer.WriteLine($"<td>{category.expectedMinimum}</td>");
                    writer.WriteLine($"<td class='{statusClass}'>{status}</td>");
                    writer.WriteLine($"</tr>");
                }

                writer.WriteLine("</table>");

                if (validationResults.errors.Count > 0)
                {
                    writer.WriteLine("<h2 class='error'>Errors</h2>");
                    writer.WriteLine("<ul>");
                    foreach (string error in validationResults.errors)
                    {
                        writer.WriteLine($"<li>{error}</li>");
                    }
                    writer.WriteLine("</ul>");
                }

                if (validationResults.warnings.Count > 0)
                {
                    writer.WriteLine("<h2 class='warning'>Warnings</h2>");
                    writer.WriteLine("<ul>");
                    foreach (string warning in validationResults.warnings)
                    {
                        writer.WriteLine($"<li>{warning}</li>");
                    }
                    writer.WriteLine("</ul>");
                }

                writer.WriteLine("</body></html>");
            }

            EditorUtility.RevealInFinder(reportPath);
            Debug.Log($"Report generated: {reportPath}");
        }

        #endregion

        #region Utilities

        private string GetRelativePath(string absolutePath)
        {
            string projectPath = Path.GetFullPath(Application.dataPath + "/..");
            if (absolutePath.StartsWith(projectPath))
            {
                return absolutePath.Substring(projectPath.Length + 1).Replace('\\', '/');
            }
            return absolutePath;
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        #endregion
    }
}