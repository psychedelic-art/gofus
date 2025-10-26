using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace GOFUS.Editor
{
    public class SceneSetupWizard : EditorWindow
    {
        [MenuItem("GOFUS/Setup/Create 2D Scene")]
        public static void ShowWindow()
        {
            GetWindow<SceneSetupWizard>("Scene Setup Wizard");
        }

        private void OnGUI()
        {
            GUILayout.Label("GOFUS 2D Scene Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Create New 2D Scene", GUILayout.Height(40)))
            {
                CreateNew2DScene();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Fix Current Scene for 2D", GUILayout.Height(30)))
            {
                FixCurrentSceneFor2D();
            }
        }

        private void CreateNew2DScene()
        {
            // Create new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

            // Create Camera
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

            // Create Grid for Tilemaps
            GameObject gridGO = new GameObject("Grid");
            Grid grid = gridGO.AddComponent<Grid>();
            grid.cellSize = new Vector3(1, 1, 0);

            // Create Tilemap layers
            CreateTilemapLayer(gridGO, "Ground", 0);
            CreateTilemapLayer(gridGO, "Obstacles", 1);
            CreateTilemapLayer(gridGO, "Decorations", 2);

            // Create Game Systems
            GameObject systems = new GameObject("_Systems");

            // Add EventSystem for UI
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(systems.transform);
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Create UI Canvas
            GameObject canvasGO = new GameObject("UI Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create Directional Light for 2D
            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0);

            // Set 2D mode in scene view
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.in2DMode = true;
                sceneView.Repaint();
            }

            Debug.Log("✅ 2D Scene created successfully!");

            // Save scene
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Scene", "New2DScene", "unity",
                "Save the new 2D scene");

            if (!string.IsNullOrEmpty(path))
            {
                EditorSceneManager.SaveScene(newScene, path);
                Debug.Log($"Scene saved to: {path}");
            }
        }

        private GameObject CreateTilemapLayer(GameObject parent, string name, int sortingOrder)
        {
            GameObject tilemapGO = new GameObject(name);
            tilemapGO.transform.SetParent(parent.transform);

            Tilemap tilemap = tilemapGO.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapGO.AddComponent<TilemapRenderer>();

            renderer.sortingOrder = sortingOrder;

            if (name == "Obstacles")
            {
                tilemapGO.AddComponent<TilemapCollider2D>();
                tilemapGO.layer = LayerMask.NameToLayer("Default");
            }

            return tilemapGO;
        }

        private void FixCurrentSceneFor2D()
        {
            // Find and fix camera
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 5.4f;
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
                EditorUtility.SetDirty(mainCamera);
            }

            // Set scene view to 2D
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.in2DMode = true;
                sceneView.Repaint();
            }

            Debug.Log("✅ Current scene configured for 2D!");
        }
    }
}