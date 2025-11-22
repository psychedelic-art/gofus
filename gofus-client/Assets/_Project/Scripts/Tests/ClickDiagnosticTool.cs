using UnityEngine;
using UnityEngine.EventSystems;
using GOFUS.Map;

namespace GOFUS.Tests
{
    /// <summary>
    /// Diagnostic tool to identify why cell clicks aren't working.
    /// Attach this to any GameObject in the scene to run diagnostics.
    /// </summary>
    public class ClickDiagnosticTool : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runDiagnosticsOnStart = true;
        [SerializeField] private bool logEveryFrame = false;

        [Header("Diagnostic Results")]
        [SerializeField] private string cameraStatus = "Not checked";
        [SerializeField] private string eventSystemStatus = "Not checked";
        [SerializeField] private string cellColliderStatus = "Not checked";
        [SerializeField] private string uiBlockingStatus = "Not checked";

        private Camera mainCamera;
        private EventSystem eventSystem;

        private void Start()
        {
            if (runDiagnosticsOnStart)
            {
                RunFullDiagnostics();
            }
        }

        private void Update()
        {
            if (logEveryFrame && Input.GetMouseButtonDown(0))
            {
                CheckClickAtMousePosition();
            }
        }

        [ContextMenu("Run Full Diagnostics")]
        public void RunFullDiagnostics()
        {
            Debug.Log("========================================");
            Debug.Log("[ClickDiagnostic] Running Full Diagnostics");
            Debug.Log("========================================");

            CheckCamera();
            CheckEventSystem();
            CheckCellColliders();
            CheckUIBlocking();
            CheckLayerMasks();

            Debug.Log("========================================");
            Debug.Log("[ClickDiagnostic] Diagnostics Complete");
            Debug.Log("========================================");
        }

        private void CheckCamera()
        {
            Debug.Log("\n[CHECK 1] Camera Configuration:");

            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                cameraStatus = "❌ MISSING";
                Debug.LogError("  ❌ Main Camera not found!");
                Debug.LogError("  → Solution: Ensure your camera has the 'MainCamera' tag");
                return;
            }

            Debug.Log($"  ✅ Camera found: {mainCamera.name}");
            Debug.Log($"  - Orthographic: {mainCamera.orthographic}");
            Debug.Log($"  - Orthographic Size: {mainCamera.orthographicSize}");
            Debug.Log($"  - Position: {mainCamera.transform.position}");
            Debug.Log($"  - Culling Mask: {mainCamera.cullingMask}");

            if (!mainCamera.orthographic)
            {
                Debug.LogWarning("  ⚠️ Camera is not orthographic! For 2D games, it should be.");
                cameraStatus = "⚠️ Not Orthographic";
            }
            else
            {
                cameraStatus = "✅ OK";
            }
        }

        private void CheckEventSystem()
        {
            Debug.Log("\n[CHECK 2] Event System:");

            eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogWarning("  ⚠️ No EventSystem found in scene");
                Debug.Log("  → This might cause UI events to not work properly");
                Debug.Log("  → Solution: Add an EventSystem GameObject (GameObject > UI > Event System)");
                eventSystemStatus = "⚠️ Missing";
            }
            else
            {
                Debug.Log($"  ✅ EventSystem found: {eventSystem.name}");
                Debug.Log($"  - Enabled: {eventSystem.enabled}");
                eventSystemStatus = "✅ OK";
            }
        }

        private void CheckCellColliders()
        {
            Debug.Log("\n[CHECK 3] Cell Colliders:");

            CellClickHandler[] handlers = FindObjectsOfType<CellClickHandler>();

            if (handlers.Length == 0)
            {
                cellColliderStatus = "❌ NO CELLS";
                Debug.LogError("  ❌ No CellClickHandlers found!");
                Debug.LogError("  → Cells haven't been created yet");
                Debug.LogError("  → Solution: Ensure MapRenderer has loaded and created cells");
                return;
            }

            Debug.Log($"  ✅ Found {handlers.Length} CellClickHandlers");

            int withColliders = 0;
            int withoutColliders = 0;
            int activeColliders = 0;
            int inactiveColliders = 0;

            foreach (var handler in handlers)
            {
                Collider2D col = handler.GetComponent<Collider2D>();
                if (col != null)
                {
                    withColliders++;
                    if (col.enabled && handler.gameObject.activeInHierarchy)
                    {
                        activeColliders++;
                    }
                    else
                    {
                        inactiveColliders++;
                    }
                }
                else
                {
                    withoutColliders++;
                }
            }

            Debug.Log($"  - Cells with colliders: {withColliders}");
            Debug.Log($"  - Cells without colliders: {withoutColliders}");
            Debug.Log($"  - Active colliders: {activeColliders}");
            Debug.Log($"  - Inactive colliders: {inactiveColliders}");

            if (withoutColliders > 0)
            {
                Debug.LogError($"  ❌ {withoutColliders} cells are missing colliders!");
                cellColliderStatus = "❌ Missing Colliders";
            }
            else if (activeColliders == 0)
            {
                Debug.LogError("  ❌ All colliders are inactive!");
                cellColliderStatus = "❌ Inactive";
            }
            else
            {
                Debug.Log("  ✅ All cells have active colliders");
                cellColliderStatus = "✅ OK";
            }

            // Check a sample cell's properties
            if (handlers.Length > 0)
            {
                var sampleHandler = handlers[0];
                Debug.Log($"\n  Sample cell properties (Cell {sampleHandler.CellId}):");
                Debug.Log($"    - GameObject: {sampleHandler.gameObject.name}");
                Debug.Log($"    - Active: {sampleHandler.gameObject.activeInHierarchy}");
                Debug.Log($"    - Layer: {LayerMask.LayerToName(sampleHandler.gameObject.layer)}");
                Debug.Log($"    - Position: {sampleHandler.transform.position}");

                var col = sampleHandler.GetComponent<Collider2D>();
                if (col != null)
                {
                    Debug.Log($"    - Collider Type: {col.GetType().Name}");
                    Debug.Log($"    - Collider Enabled: {col.enabled}");
                    Debug.Log($"    - Is Trigger: {col.isTrigger}");
                }
            }
        }

        private void CheckUIBlocking()
        {
            Debug.Log("\n[CHECK 4] UI Blocking:");

            Canvas[] canvases = FindObjectsOfType<Canvas>();

            if (canvases.Length == 0)
            {
                Debug.Log("  ✅ No canvases found, UI blocking not possible");
                uiBlockingStatus = "✅ No UI";
                return;
            }

            Debug.Log($"  Found {canvases.Length} Canvas(es):");

            bool hasBlockingCanvas = false;

            foreach (var canvas in canvases)
            {
                Debug.Log($"\n  Canvas: {canvas.name}");
                Debug.Log($"    - Render Mode: {canvas.renderMode}");
                Debug.Log($"    - Sort Order: {canvas.sortingOrder}");
                Debug.Log($"    - Enabled: {canvas.enabled}");

                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    Debug.LogWarning("    ⚠️ THIS CANVAS IS IN SCREEN SPACE - OVERLAY MODE");
                    Debug.LogWarning("    → This mode renders on top and BLOCKS clicks to world objects!");
                    Debug.LogWarning("    → Solution 1: Change to 'Screen Space - Camera' mode");
                    Debug.LogWarning("    → Solution 2: Set 'Raycast Target' to FALSE on UI elements");
                    Debug.LogWarning("    → Solution 3: Use manual input detection (InputManager)");
                    hasBlockingCanvas = true;
                }

                // Check for GraphicRaycaster
                var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (raycaster != null)
                {
                    Debug.Log($"    - GraphicRaycaster: {raycaster.enabled}");
                }
            }

            if (hasBlockingCanvas)
            {
                uiBlockingStatus = "❌ UI BLOCKING";
                Debug.LogError("\n  ❌ UI IS LIKELY BLOCKING CLICKS!");
                Debug.LogError("  → This is the most common reason clicks don't work");
            }
            else
            {
                uiBlockingStatus = "✅ OK";
                Debug.Log("\n  ✅ UI configuration looks OK");
            }
        }

        private void CheckLayerMasks()
        {
            Debug.Log("\n[CHECK 5] Layer Masks:");

            if (mainCamera == null)
            {
                Debug.LogWarning("  ⚠️ Cannot check layer masks without camera");
                return;
            }

            CellClickHandler[] handlers = FindObjectsOfType<CellClickHandler>();
            if (handlers.Length == 0)
            {
                Debug.LogWarning("  ⚠️ No cells to check layers");
                return;
            }

            var sampleCell = handlers[0];
            int cellLayer = sampleCell.gameObject.layer;
            string layerName = LayerMask.LayerToName(cellLayer);

            Debug.Log($"  Cells are on layer: {cellLayer} ({layerName})");

            bool cameraCanSeeLayer = (mainCamera.cullingMask & (1 << cellLayer)) != 0;

            if (cameraCanSeeLayer)
            {
                Debug.Log($"  ✅ Camera can see layer '{layerName}'");
            }
            else
            {
                Debug.LogError($"  ❌ Camera CANNOT see layer '{layerName}'!");
                Debug.LogError("  → Solution: Add this layer to Camera's Culling Mask");
            }
        }

        [ContextMenu("Check Click At Mouse Position")]
        public void CheckClickAtMousePosition()
        {
            if (mainCamera == null)
            {
                Debug.LogError("[ClickDiagnostic] No camera to test with");
                return;
            }

            Vector2 mouseScreenPos = Input.mousePosition;
            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);

            Debug.Log("\n========================================");
            Debug.Log("[ClickDiagnostic] Testing Click At Mouse Position");
            Debug.Log("========================================");
            Debug.Log($"Screen Position: {mouseScreenPos}");
            Debug.Log($"World Position: {mouseWorldPos}");

            // Check if over UI
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            Debug.Log($"Over UI: {overUI}");

            if (overUI)
            {
                Debug.LogWarning("⚠️ MOUSE IS OVER UI! This will block clicks.");
            }

            // Perform raycast
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

            if (hit.collider != null)
            {
                Debug.Log($"✅ Raycast HIT: {hit.collider.gameObject.name}");
                Debug.Log($"   - Position: {hit.point}");
                Debug.Log($"   - Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

                CellClickHandler handler = hit.collider.GetComponent<CellClickHandler>();
                if (handler != null)
                {
                    Debug.Log($"   ✅ Has CellClickHandler for cell {handler.CellId}");
                }
                else
                {
                    Debug.LogWarning("   ⚠️ No CellClickHandler on this object");
                }
            }
            else
            {
                Debug.LogWarning("❌ Raycast hit NOTHING");
            }

            // Check all colliders at point
            Collider2D[] allColliders = Physics2D.OverlapPointAll(mouseWorldPos);
            Debug.Log($"\nAll colliders at point: {allColliders.Length}");
            foreach (var col in allColliders)
            {
                Debug.Log($"  - {col.gameObject.name}");
            }

            Debug.Log("========================================\n");
        }

        /// <summary>
        /// Continuously monitor clicks for debugging
        /// </summary>
        [ContextMenu("Enable Continuous Click Monitoring")]
        public void EnableContinuousMonitoring()
        {
            logEveryFrame = true;
            Debug.Log("[ClickDiagnostic] Continuous click monitoring ENABLED");
            Debug.Log("[ClickDiagnostic] Click anywhere and see diagnostics in console");
        }

        [ContextMenu("Disable Continuous Click Monitoring")]
        public void DisableContinuousMonitoring()
        {
            logEveryFrame = false;
            Debug.Log("[ClickDiagnostic] Continuous click monitoring DISABLED");
        }
    }
}
