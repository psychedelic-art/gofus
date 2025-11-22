using UnityEditor;
using UnityEngine;

namespace GOFUS.Editor
{
    /// <summary>
    /// Editor utility to quickly switch between local and production backend
    /// </summary>
    public class SwitchToLocalBackend
    {
        [MenuItem("GOFUS/Backend/Use Local Backend (localhost:3000)")]
        public static void UseLocalBackend()
        {
            PlayerPrefs.SetInt("use_local_backend", 1);
            PlayerPrefs.Save();
            Debug.Log("[GOFUS] Switched to LOCAL backend (http://localhost:3000)");
            Debug.Log("[GOFUS] Please restart the game or reload LoginScreen for changes to take effect");
        }

        [MenuItem("GOFUS/Backend/Use Production Backend (Vercel)")]
        public static void UseProductionBackend()
        {
            PlayerPrefs.SetInt("use_local_backend", 0);
            PlayerPrefs.Save();
            Debug.Log("[GOFUS] Switched to PRODUCTION backend (https://gofus-backend.vercel.app)");
            Debug.Log("[GOFUS] Please restart the game or reload LoginScreen for changes to take effect");
        }

        [MenuItem("GOFUS/Backend/Check Current Backend")]
        public static void CheckCurrentBackend()
        {
            int useLocal = PlayerPrefs.GetInt("use_local_backend", 0);
            string backend = useLocal == 1 ? "LOCAL (http://localhost:3000)" : "PRODUCTION (https://gofus-backend.vercel.app)";
            Debug.Log($"[GOFUS] Current backend: {backend}");
        }
    }
}
