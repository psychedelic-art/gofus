using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class PackageAutoImporter
{
    static PackageAutoImporter()
    {
        EditorApplication.delayCall += CheckPackages;
    }

    static void CheckPackages()
    {
        Debug.Log("[GOFUS] Checking for required packages...");

        // Check if TextMeshPro is installed
        if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.textmeshpro") == null)
        {
            Debug.LogError("[GOFUS] TextMeshPro not found! Please install via Package Manager.");
        }
        else
        {
            Debug.Log("[GOFUS] TextMeshPro found. Remember to import Essential Resources if prompted!");
        }
    }
}
