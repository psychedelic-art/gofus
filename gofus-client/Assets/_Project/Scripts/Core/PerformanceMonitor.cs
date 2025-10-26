using UnityEngine;
using System.Text;

namespace GOFUS.Core
{
    /// <summary>
    /// Real-time performance monitoring for runtime analysis.
    /// Displays FPS, memory usage, draw calls, and custom metrics.
    /// Toggle with F1 key during gameplay.
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool showStats = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private int fontSize = 14;
        [SerializeField] private Color goodColor = Color.green;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color badColor = Color.red;

        [Header("Performance Targets")]
        [SerializeField] private float targetFPS = 60f;
        [SerializeField] private float warningFPS = 45f;
        [SerializeField] private float maxMemoryMB = 800f;
        [SerializeField] private int maxDrawCalls = 200;

        [Header("Current Stats (Read-Only)")]
        [SerializeField] private float currentFPS;
        [SerializeField] private float memoryUsageMB;
        [SerializeField] private int activeGameObjects;
        [SerializeField] private int triangleCount;
        [SerializeField] private int vertexCount;

        private float deltaTime;
        private GUIStyle style;
        private StringBuilder statsBuilder = new StringBuilder();

        // Custom metrics
        private int customMetricCount = 0;
        private System.Collections.Generic.Dictionary<string, float> customMetrics = new System.Collections.Generic.Dictionary<string, float>();

        private void Start()
        {
            InitializeStyle();
        }

        private void InitializeStyle()
        {
            style = new GUIStyle();
            style.fontSize = fontSize;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.UpperLeft;
            style.padding = new RectOffset(10, 10, 10, 10);
        }

        private void Update()
        {
            // Toggle display
            if (Input.GetKeyDown(toggleKey))
            {
                showStats = !showStats;
            }

            // Update FPS
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            currentFPS = 1.0f / deltaTime;

            // Update memory
            memoryUsageMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1024f / 1024f;

            // Update object count
            activeGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
        }

        private void OnGUI()
        {
            if (!showStats) return;

            // Build stats string
            BuildStatsString();

            // Determine color based on FPS
            Color fpsColor = GetFPSColor();
            style.normal.textColor = fpsColor;

            // Calculate rect
            int w = Screen.width;
            int h = Screen.height;
            Rect rect = new Rect(10, 10, 400, 300);

            // Draw background
            GUI.Box(rect, "");

            // Draw text
            GUI.Label(rect, statsBuilder.ToString(), style);
        }

        private void BuildStatsString()
        {
            statsBuilder.Clear();

            // FPS
            statsBuilder.AppendLine($"<b>PERFORMANCE MONITOR</b>");
            statsBuilder.AppendLine($"Press {toggleKey} to toggle");
            statsBuilder.AppendLine();

            statsBuilder.AppendLine($"<b>FRAMERATE</b>");
            statsBuilder.AppendLine($"Current FPS: {Mathf.Ceil(currentFPS)}");
            statsBuilder.AppendLine($"Target FPS: {targetFPS}");
            statsBuilder.AppendLine($"Frame Time: {deltaTime * 1000f:F2}ms");
            statsBuilder.AppendLine();

            // Memory
            statsBuilder.AppendLine($"<b>MEMORY</b>");
            statsBuilder.AppendLine($"Total Allocated: {memoryUsageMB:F1} MB");
            statsBuilder.AppendLine($"Total Reserved: {UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1024f / 1024f:F1} MB");
            statsBuilder.AppendLine($"Mono Heap: {UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong() / 1024f / 1024f:F1} MB");
            statsBuilder.AppendLine($"Mono Used: {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1024f / 1024f:F1} MB");
            statsBuilder.AppendLine();

            // Scene Stats
            statsBuilder.AppendLine($"<b>SCENE</b>");
            statsBuilder.AppendLine($"Active GameObjects: {activeGameObjects}");

            // Rendering stats (only available in editor)
            #if UNITY_EDITOR
            statsBuilder.AppendLine($"Triangles: {UnityEditor.UnityStats.triangles}");
            statsBuilder.AppendLine($"Vertices: {UnityEditor.UnityStats.vertices}");
            statsBuilder.AppendLine($"Batches: {UnityEditor.UnityStats.batches}");
            statsBuilder.AppendLine($"SetPass Calls: {UnityEditor.UnityStats.setPassCalls}");
            #endif
            statsBuilder.AppendLine();

            // Custom metrics
            if (customMetrics.Count > 0)
            {
                statsBuilder.AppendLine($"<b>CUSTOM METRICS</b>");
                foreach (var metric in customMetrics)
                {
                    statsBuilder.AppendLine($"{metric.Key}: {metric.Value:F2}");
                }
                statsBuilder.AppendLine();
            }

            // Pool stats if available
            if (PoolManager.Instance != null)
            {
                statsBuilder.AppendLine($"<b>OBJECT POOLS</b>");
                // This would require exposing pool stats from PoolManager
                statsBuilder.AppendLine($"Active Pools: Available");
            }
        }

        private Color GetFPSColor()
        {
            if (currentFPS >= targetFPS)
                return goodColor;
            else if (currentFPS >= warningFPS)
                return warningColor;
            else
                return badColor;
        }

        /// <summary>
        /// Adds a custom metric to display.
        /// </summary>
        public void AddMetric(string name, float value)
        {
            customMetrics[name] = value;
        }

        /// <summary>
        /// Removes a custom metric.
        /// </summary>
        public void RemoveMetric(string name)
        {
            customMetrics.Remove(name);
        }

        /// <summary>
        /// Gets current performance data.
        /// </summary>
        public PerformanceData GetPerformanceData()
        {
            return new PerformanceData
            {
                fps = currentFPS,
                frameTime = deltaTime * 1000f,
                memoryMB = memoryUsageMB,
                activeObjects = activeGameObjects,
                isPerformanceGood = currentFPS >= targetFPS && memoryUsageMB < maxMemoryMB
            };
        }

        /// <summary>
        /// Logs performance snapshot to console.
        /// </summary>
        public void LogSnapshot()
        {
            Debug.Log($"=== Performance Snapshot ===");
            Debug.Log($"FPS: {currentFPS:F1}");
            Debug.Log($"Memory: {memoryUsageMB:F1} MB");
            Debug.Log($"Objects: {activeGameObjects}");
            Debug.Log($"Frame Time: {deltaTime * 1000f:F2}ms");
        }
    }

    /// <summary>
    /// Performance data structure.
    /// </summary>
    [System.Serializable]
    public struct PerformanceData
    {
        public float fps;
        public float frameTime;
        public float memoryMB;
        public int activeObjects;
        public bool isPerformanceGood;

        public override string ToString()
        {
            return $"FPS: {fps:F1}, Memory: {memoryMB:F1}MB, Objects: {activeObjects}";
        }
    }
}
