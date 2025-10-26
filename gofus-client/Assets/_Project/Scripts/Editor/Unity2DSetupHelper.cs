using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

namespace GOFUS.Editor
{
    public class Unity2DSetupHelper : EditorWindow
    {
        [MenuItem("GOFUS/Setup/Configure for 2D Mode")]
        public static void ShowWindow()
        {
            GetWindow<Unity2DSetupHelper>("2D Setup Helper");
        }

        private void OnGUI()
        {
            GUILayout.Label("GOFUS 2D Configuration Helper", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox("This will configure Unity for optimal 2D MMORPG development", MessageType.Info);
            GUILayout.Space(10);

            if (GUILayout.Button("Configure Project for 2D", GUILayout.Height(30)))
            {
                ConfigureFor2D();
            }

            GUILayout.Space(10);
            GUILayout.Label("Individual Settings:", EditorStyles.boldLabel);

            if (GUILayout.Button("Set Camera to Orthographic"))
            {
                SetCameraToOrthographic();
            }

            if (GUILayout.Button("Configure 2D Physics"))
            {
                Configure2DPhysics();
            }

            if (GUILayout.Button("Set Sprite Import Settings"))
            {
                SetSpriteDefaults();
            }

            if (GUILayout.Button("Configure Quality Settings"))
            {
                ConfigureQualityFor2D();
            }
        }

        public void ConfigureFor2D()
        {
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;

            SetCameraToOrthographic();
            Configure2DPhysics();
            SetSpriteDefaults();
            ConfigureQualityFor2D();

            // Set scene view to 2D
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.in2DMode = true;
                sceneView.Repaint();
            }

            Debug.Log("✅ Unity configured for 2D mode successfully!");
            EditorUtility.DisplayDialog("2D Setup Complete",
                "Unity has been configured for 2D MMORPG development!\n\n" +
                "✓ Camera set to Orthographic\n" +
                "✓ 2D Physics configured\n" +
                "✓ Sprite defaults set\n" +
                "✓ Quality optimized for 2D", "OK");
        }

        private void SetCameraToOrthographic()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject[] cameras = GameObject.FindGameObjectsWithTag("MainCamera");
                if (cameras.Length > 0)
                    mainCamera = cameras[0].GetComponent<Camera>();
            }

            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 5.4f; // Good for 1080p
                mainCamera.nearClipPlane = -10f;
                mainCamera.farClipPlane = 10f;

                // Set clear flags for 2D
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = new Color(0.2f, 0.2f, 0.2f);

                EditorUtility.SetDirty(mainCamera);
                Debug.Log("✅ Camera set to Orthographic mode");
            }
            else
            {
                Debug.LogWarning("No Main Camera found in scene!");
            }
        }

        private void Configure2DPhysics()
        {
            // Configure 2D Physics settings
            Physics2D.gravity = Vector2.zero; // No gravity for top-down MMORPG

            // Set collision matrix for 2D
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    if (i != j)
                    {
                        Physics2D.IgnoreLayerCollision(i, j, false);
                    }
                }
            }

            Debug.Log("✅ 2D Physics configured");
        }

        private void SetSpriteDefaults()
        {
            // Set default sprite import settings
            TextureImporterSettings settings = new TextureImporterSettings();
            settings.spriteMode = 1; // Single sprite
            settings.spritePivot = new Vector2(0.5f, 0.5f); // Center pivot
            settings.spritePixelsPerUnit = 100;

            EditorSettings.spritePackerMode = SpritePackerMode.BuildTimeOnlyAtlas;

            Debug.Log("✅ Sprite import defaults configured");
        }

        private void ConfigureQualityFor2D()
        {
            // Disable unnecessary 3D features
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowCascades = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;

            // Optimize for 2D
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 0; // No AA for pixel art

            Debug.Log("✅ Quality settings optimized for 2D");
        }
    }

    // Auto-configure on first import
    [InitializeOnLoad]
    public class Unity2DAutoSetup
    {
        static Unity2DAutoSetup()
        {
            EditorApplication.delayCall += CheckAndConfigure;
        }

        private static void CheckAndConfigure()
        {
            if (!EditorPrefs.GetBool("GOFUS_2D_Configured", false))
            {
                if (EditorUtility.DisplayDialog("Configure for 2D?",
                    "GOFUS is a 2D MMORPG. Would you like to configure Unity for 2D mode?",
                    "Yes", "No"))
                {
                    Unity2DSetupHelper helper = new Unity2DSetupHelper();
                    helper.ConfigureFor2D();
                    EditorPrefs.SetBool("GOFUS_2D_Configured", true);
                }
            }
        }
    }
}