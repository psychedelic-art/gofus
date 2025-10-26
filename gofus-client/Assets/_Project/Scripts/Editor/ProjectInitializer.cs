using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace GOFUS.Editor
{
    /// <summary>
    /// Initializes the project on first run
    /// </summary>
    public class ProjectInitializer
    {
        private const string MAIN_SCENE_PATH = "Assets/_Project/Scenes/MainScene.unity";
        private const string SCENES_PATH = "Assets/_Project/Scenes";

        [MenuItem("GOFUS/Project/Initialize Project", priority = 1)]
        public static void InitializeProject()
        {
            Debug.Log("[GOFUS] Initializing project...");

            // Create folder structure
            CreateProjectFolders();

            // Create or update main scene
            SetupMainScene();

            // Configure build settings
            ConfigureBuildSettings();

            // Set graphics settings
            ConfigureGraphicsSettings();

            Debug.Log("[GOFUS] Project initialization complete!");
        }

        private static void CreateProjectFolders()
        {
            string[] folders = new[]
            {
                "Assets/_Project",
                "Assets/_Project/Scenes",
                "Assets/_Project/Prefabs",
                "Assets/_Project/Prefabs/Characters",
                "Assets/_Project/Prefabs/UI",
                "Assets/_Project/Prefabs/Environment",
                "Assets/_Project/Materials",
                "Assets/_Project/Textures",
                "Assets/_Project/Audio",
                "Assets/_Project/Audio/Music",
                "Assets/_Project/Audio/SFX",
                "Assets/_Project/Resources",
                "Assets/_Project/ImportedAssets",
                "Assets/_Project/ImportedAssets/Characters",
                "Assets/_Project/ImportedAssets/UI",
                "Assets/_Project/ImportedAssets/Environment"
            };

            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                    string newFolder = Path.GetFileName(folder);
                    AssetDatabase.CreateFolder(parent, newFolder);
                    Debug.Log($"[GOFUS] Created folder: {folder}");
                }
            }
        }

        private static void SetupMainScene()
        {
            Scene currentScene = SceneManager.GetActiveScene();

            // Check if we need to create a new scene
            if (!File.Exists(MAIN_SCENE_PATH))
            {
                // Create new scene
                Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

                // Add essential GameObjects
                CreateSceneStructure();

                // Save the scene
                if (!AssetDatabase.IsValidFolder(SCENES_PATH))
                {
                    AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
                }

                EditorSceneManager.SaveScene(newScene, MAIN_SCENE_PATH);
                Debug.Log($"[GOFUS] Created MainScene at {MAIN_SCENE_PATH}");
            }
            else
            {
                // Open existing main scene
                EditorSceneManager.OpenScene(MAIN_SCENE_PATH);
                Debug.Log($"[GOFUS] Opened existing MainScene");
            }
        }

        private static void CreateSceneStructure()
        {
            // Create Main Camera (2D setup)
            GameObject cameraGO = new GameObject("Main Camera");
            Camera camera = cameraGO.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            camera.nearClipPlane = -10f;
            camera.farClipPlane = 10f;
            cameraGO.tag = "MainCamera";
            cameraGO.AddComponent<AudioListener>();

            // Create Game Systems container
            GameObject systems = new GameObject("_Systems");
            systems.transform.position = Vector3.zero;

            // Create Managers container
            GameObject managers = new GameObject("_Managers");
            managers.transform.SetParent(systems.transform);

            // Add GameManager
            GameObject gameManager = new GameObject("GameManager");
            gameManager.transform.SetParent(managers.transform);

            // Create Environment container
            GameObject environment = new GameObject("_Environment");

            // Create Grid for tilemaps
            GameObject grid = new GameObject("Grid");
            grid.transform.SetParent(environment.transform);
            var gridComponent = grid.AddComponent<Grid>();
            gridComponent.cellSize = new Vector3(1, 1, 0);

            // Create UI container
            GameObject uiRoot = new GameObject("_UI");

            // Create EventSystem for UI
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(uiRoot.transform);
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Create Canvas
            GameObject canvasGO = new GameObject("Canvas");
            canvasGO.transform.SetParent(uiRoot.transform);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create Directional Light (minimal for 2D)
            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.5f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0);
            light.enabled = false; // Disabled by default for 2D

            Debug.Log("[GOFUS] Scene structure created");
        }

        private static void ConfigureBuildSettings()
        {
            // Get all scenes
            string[] scenePaths = new[] { MAIN_SCENE_PATH };

            // Convert to EditorBuildSettingsScene array
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[scenePaths.Length];
            for (int i = 0; i < scenePaths.Length; i++)
            {
                scenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);
            }

            // Set the build scenes
            EditorBuildSettings.scenes = scenes;

            Debug.Log("[GOFUS] Build settings configured");
        }

        private static void ConfigureGraphicsSettings()
        {
            // Set 2D as default
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;

            // Configure quality settings for 2D
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 0;

            // Set target frame rate
            Application.targetFrameRate = 60;

            Debug.Log("[GOFUS] Graphics settings configured for 2D");
        }

        [MenuItem("GOFUS/Project/Open Main Scene", priority = 2)]
        public static void OpenMainScene()
        {
            if (File.Exists(MAIN_SCENE_PATH))
            {
                EditorSceneManager.OpenScene(MAIN_SCENE_PATH);

                // Set scene view to 2D
                SceneView sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    sceneView.in2DMode = true;
                    sceneView.Repaint();
                }
            }
            else
            {
                Debug.LogError($"[GOFUS] MainScene not found at {MAIN_SCENE_PATH}. Run Initialize Project first.");
            }
        }

        [MenuItem("GOFUS/Project/Create Test Assets", priority = 3)]
        public static void CreateTestAssets()
        {
            string testPath = "Assets/_Project/Tests";
            if (!AssetDatabase.IsValidFolder(testPath))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Tests");
            }

            // Create a simple test sprite
            Texture2D testTexture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            testTexture.SetPixels(pixels);
            testTexture.Apply();

            byte[] bytes = testTexture.EncodeToPNG();
            string spritePath = $"{testPath}/TestSprite.png";
            File.WriteAllBytes(spritePath, bytes);
            AssetDatabase.ImportAsset(spritePath);

            // Set import settings for sprite
            TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32;
                importer.SaveAndReimport();
            }

            Debug.Log($"[GOFUS] Test assets created in {testPath}");
        }
    }
}