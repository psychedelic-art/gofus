using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Collections.Generic;
using System.Linq;

namespace GOFUS.Editor
{
    /// <summary>
    /// Automatically handles package imports and TMP setup on project load
    /// </summary>
    [InitializeOnLoad]
    public class AutoPackageImporter : EditorWindow
    {
        private static ListRequest listRequest;
        private static AddRequest addRequest;
        private static bool hasCheckedPackages = false;

        private static readonly Dictionary<string, string> RequiredPackages = new Dictionary<string, string>
        {
            { "com.unity.textmeshpro", "3.0.9" },
            { "com.unity.ugui", "2.0.0" },
            { "com.unity.2d.sprite", "1.0.0" },
            { "com.unity.2d.tilemap", "1.0.0" },
            { "com.unity.inputsystem", "1.14.2" },
            { "com.unity.nuget.newtonsoft-json", "3.2.1" }
        };

        static AutoPackageImporter()
        {
            // Check packages after a small delay to let Unity initialize
            EditorApplication.delayCall += CheckPackagesOnce;
        }

        private static void CheckPackagesOnce()
        {
            if (!hasCheckedPackages)
            {
                hasCheckedPackages = true;
                CheckInstalledPackages();
            }
        }

        private static void CheckInstalledPackages()
        {
            Debug.Log("[GOFUS] Checking for required packages...");
            listRequest = Client.List();
            EditorApplication.update += Progress;
        }

        private static void Progress()
        {
            if (listRequest != null && listRequest.IsCompleted)
            {
                if (listRequest.Status == StatusCode.Success)
                {
                    var installedPackages = listRequest.Result.ToDictionary(p => p.name, p => p.version);
                    bool allPackagesInstalled = true;

                    foreach (var required in RequiredPackages)
                    {
                        if (!installedPackages.ContainsKey(required.Key))
                        {
                            Debug.LogWarning($"[GOFUS] Missing package: {required.Key}");
                            allPackagesInstalled = false;
                        }
                        else
                        {
                            Debug.Log($"[GOFUS] ✓ Found package: {required.Key} v{installedPackages[required.Key]}");
                        }
                    }

                    if (allPackagesInstalled)
                    {
                        Debug.Log("[GOFUS] All required packages are installed!");
                        CheckTMPResources();
                    }
                    else
                    {
                        ShowPackageWarning();
                    }
                }
                else if (listRequest.Status >= StatusCode.Failure)
                {
                    Debug.LogError($"[GOFUS] Failed to list packages: {listRequest.Error.message}");
                }

                EditorApplication.update -= Progress;
                listRequest = null;
            }
        }

        private static void CheckTMPResources()
        {
            // Check if TMP Essential Resources are imported
            string[] tmpEssentialCheck = AssetDatabase.FindAssets("TMP Settings");

            if (tmpEssentialCheck.Length == 0)
            {
                Debug.LogWarning("[GOFUS] TextMeshPro Essential Resources not found!");

                // Show dialog to import TMP resources
                if (EditorUtility.DisplayDialog("Import TMP Essential Resources",
                    "TextMeshPro Essential Resources are required for this project.\n\n" +
                    "Would you like to import them now?",
                    "Import", "Later"))
                {
                    // Try to import TMP essentials
                    ImportTMPEssentials();
                }
            }
            else
            {
                Debug.Log("[GOFUS] ✓ TextMeshPro Essential Resources found!");
                ConfigureProjectSettings();
            }
        }

        private static void ImportTMPEssentials()
        {
            Debug.Log("[GOFUS] Attempting to import TMP Essential Resources...");

            // This will trigger the TMP import window
            var tmpImporterType = System.Type.GetType("TMPro.EditorUtilities.TMP_PackageResourceImporter, Unity.TextMeshPro.Editor");
            if (tmpImporterType != null)
            {
                var importMethod = tmpImporterType.GetMethod("ImportResources", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                if (importMethod != null)
                {
                    importMethod.Invoke(null, null);
                    Debug.Log("[GOFUS] TMP Resource importer window should now be open.");
                }
            }
            else
            {
                Debug.LogWarning("[GOFUS] Could not find TMP importer. Please go to Window > TextMeshPro > Import TMP Essential Resources");
            }
        }

        private static void ShowPackageWarning()
        {
            EditorUtility.DisplayDialog("Missing Packages",
                "Some required packages are missing.\n\n" +
                "Please open Package Manager (Window > Package Manager) and install:\n" +
                "• TextMeshPro\n" +
                "• 2D Sprite\n" +
                "• 2D Tilemap\n" +
                "• Input System\n" +
                "• Newtonsoft Json",
                "OK");
        }

        private static void ConfigureProjectSettings()
        {
            // Set 2D mode as default
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;

            // Set scene view to 2D
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.in2DMode = true;
                sceneView.Repaint();
            }

            // Configure quality settings for 2D
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 0;

            Debug.Log("[GOFUS] ✓ Project configured for 2D development!");

            // Mark setup as complete
            EditorPrefs.SetBool("GOFUS_Setup_Complete", true);

            ShowSuccessMessage();
        }

        private static void ShowSuccessMessage()
        {
            EditorUtility.DisplayDialog("Setup Complete!",
                "GOFUS Unity project is ready!\n\n" +
                "✓ All packages installed\n" +
                "✓ TextMeshPro configured\n" +
                "✓ 2D mode enabled\n" +
                "✓ Project settings optimized\n\n" +
                "You can now:\n" +
                "• Open MainScene from Assets/_Project/Scenes/\n" +
                "• Press Play to test\n" +
                "• Use GOFUS menu for additional tools",
                "Great!");
        }

        [MenuItem("GOFUS/Setup/Verify Package Installation")]
        public static void ManualCheck()
        {
            hasCheckedPackages = false;
            CheckPackagesOnce();
        }

        [MenuItem("GOFUS/Setup/Import TMP Resources")]
        public static void ManualTMPImport()
        {
            ImportTMPEssentials();
        }

        [MenuItem("GOFUS/Setup/Reset Setup Status")]
        public static void ResetSetup()
        {
            EditorPrefs.DeleteKey("GOFUS_Setup_Complete");
            hasCheckedPackages = false;
            Debug.Log("[GOFUS] Setup status reset. Will check packages on next compile.");
        }
    }
}