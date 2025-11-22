using UnityEngine;
using UnityEngine.EventSystems;
using GOFUS.Map;

namespace GOFUS.Core
{
    /// <summary>
    /// Handles input detection including manual raycasting to bypass UI blocking issues.
    /// This solves the problem where Unity UI Canvas blocks OnMouseDown() events on world objects.
    /// Note: Named MapInputHandler to avoid conflicts with Unity's Input system
    /// </summary>
    public class MapInputHandler : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool enableManualRaycasting = true;
        [SerializeField] private bool debugRaycast = true;
        [SerializeField] private LayerMask clickableLayers = ~0; // All layers by default

        [Header("Debug Info")]
        [SerializeField] private string lastClickResult = "None";
        [SerializeField] private int lastClickedCellId = -1;
        [SerializeField] private bool isOverUI = false;

        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[InputManager] Main Camera not found! Click detection will not work.");
            }
            else
            {
                Debug.Log("[InputManager] Initialized with manual raycasting enabled");
            }
        }

        private void Update()
        {
            if (!enableManualRaycasting || mainCamera == null)
                return;

            // Check for left mouse button click
            if (Input.GetMouseButtonDown(0))
            {
                HandleClick();
            }
        }

        private void HandleClick()
        {
            // Check if clicking over UI
            isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (isOverUI)
            {
                if (debugRaycast)
                {
                    Debug.Log("[InputManager] Click blocked: Pointer is over UI element");
                }
                lastClickResult = "Blocked by UI";
                return;
            }

            // Get mouse position in world space
            Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            if (debugRaycast)
            {
                Debug.Log($"[InputManager] Click detected at screen pos: {Input.mousePosition}, world pos: {mousePos}");
            }

            // Perform 2D raycast
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, clickableLayers);

            if (hit.collider != null)
            {
                if (debugRaycast)
                {
                    Debug.Log($"[InputManager] Raycast hit: {hit.collider.gameObject.name}");
                }

                // Try to get CellClickHandler
                CellClickHandler clickHandler = hit.collider.GetComponent<CellClickHandler>();
                if (clickHandler != null)
                {
                    lastClickedCellId = clickHandler.CellId;
                    lastClickResult = $"Cell {lastClickedCellId}";

                    if (debugRaycast)
                    {
                        Debug.Log($"[InputManager] Triggering click on cell {lastClickedCellId}");
                    }

                    // Trigger the click manually
                    clickHandler.TriggerClick();
                }
                else
                {
                    if (debugRaycast)
                    {
                        Debug.LogWarning($"[InputManager] Hit object has no CellClickHandler: {hit.collider.gameObject.name}");
                    }
                    lastClickResult = "No handler";
                }
            }
            else
            {
                if (debugRaycast)
                {
                    Debug.Log("[InputManager] Raycast hit nothing");
                }
                lastClickResult = "No hit";
            }
        }

        /// <summary>
        /// Draw debug gizmos showing raycast
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!enableManualRaycasting || !debugRaycast)
                return;

            if (mainCamera != null && Input.GetMouseButton(0))
            {
                Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(mousePos, 0.5f);
            }
        }

        /// <summary>
        /// Test raycast at a specific world position (for debugging)
        /// </summary>
        public void TestRaycastAtPosition(Vector2 worldPos)
        {
            Debug.Log($"[InputManager] Testing raycast at {worldPos}");

            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

            if (hit.collider != null)
            {
                Debug.Log($"[InputManager] Test raycast hit: {hit.collider.gameObject.name}");
                CellClickHandler handler = hit.collider.GetComponent<CellClickHandler>();
                if (handler != null)
                {
                    Debug.Log($"[InputManager] Found CellClickHandler for cell {handler.CellId}");
                }
                else
                {
                    Debug.LogWarning($"[InputManager] No CellClickHandler on hit object");
                }
            }
            else
            {
                Debug.LogWarning($"[InputManager] Test raycast hit nothing");
            }
        }

        /// <summary>
        /// Get all colliders at mouse position (for debugging)
        /// </summary>
        [ContextMenu("Debug - Show All Colliders At Mouse")]
        public void DebugShowCollidersAtMouse()
        {
            if (mainCamera == null)
            {
                Debug.LogError("[InputManager] No main camera");
                return;
            }

            Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Debug.Log($"[InputManager] Checking all colliders at {mousePos}");

            Collider2D[] colliders = Physics2D.OverlapPointAll(mousePos);

            if (colliders.Length == 0)
            {
                Debug.LogWarning("[InputManager] No colliders found at mouse position");
            }
            else
            {
                Debug.Log($"[InputManager] Found {colliders.Length} colliders:");
                foreach (var col in colliders)
                {
                    Debug.Log($"  - {col.gameObject.name} (Layer: {LayerMask.LayerToName(col.gameObject.layer)})");
                }
            }
        }
    }
}
