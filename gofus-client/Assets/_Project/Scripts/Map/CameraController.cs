using UnityEngine;

namespace GOFUS.Map
{
    /// <summary>
    /// Controls camera movement, panning, zooming and following for isometric map view
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool followTarget = true;
        [SerializeField] private float followSmoothing = 5f;

        [Header("Zoom Settings")]
        [SerializeField] private float minZoom = 20f;
        [SerializeField] private float maxZoom = 50f;
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float mouseScrollZoomSpeed = 5f;
        [SerializeField] private float currentZoom = 35f;

        [Header("Pan Settings")]
        [SerializeField] private bool allowPanning = true;
        [SerializeField] private float panSpeed = 20f;
        [SerializeField] private float dragPanSpeed = 0.1f;
        [SerializeField] private bool enableEdgePan = false;
        [SerializeField] private float edgePanBorderSize = 10f;

        [Header("Bounds")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private Vector2 minBounds = new Vector2(-50f, -20f);
        [SerializeField] private Vector2 maxBounds = new Vector2(50f, 50f);

        [Header("Drag")]
        [SerializeField] private bool allowDrag = true;
        [SerializeField] private KeyCode dragKey = KeyCode.Mouse2; // Middle mouse button
        [SerializeField] private KeyCode altDragKey = KeyCode.Mouse1; // Right mouse button

        private Camera cam;
        private Vector3 lastMousePosition;
        private Vector3 dragOrigin;
        private bool isDragging;
        private Vector3 targetPosition;

        // Properties
        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public bool FollowTarget
        {
            get => followTarget;
            set => followTarget = value;
        }

        public float CurrentZoom
        {
            get => currentZoom;
            set
            {
                currentZoom = Mathf.Clamp(value, minZoom, maxZoom);
                if (cam != null && cam.orthographic)
                {
                    cam.orthographicSize = currentZoom;
                }
            }
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                Debug.LogError("[CameraController] No Camera component found!");
                return;
            }

            // Set initial zoom
            if (cam.orthographic)
            {
                currentZoom = cam.orthographicSize;
            }

            targetPosition = transform.position;
        }

        private void LateUpdate()
        {
            HandleZoom();
            HandlePan();
            HandleDrag();
            HandleFollow();
            ApplyBounds();
        }

        private void HandleZoom()
        {
            if (!cam || !cam.orthographic) return;

            // Mouse scroll wheel zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                currentZoom -= scroll * mouseScrollZoomSpeed;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            }

            // Keyboard zoom (Q/E or PageUp/PageDown)
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.PageUp))
            {
                currentZoom -= zoomSpeed * Time.deltaTime;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            }
            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.PageDown))
            {
                currentZoom += zoomSpeed * Time.deltaTime;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            }

            // Smoothly apply zoom
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, currentZoom, Time.deltaTime * 10f);
        }

        private void HandlePan()
        {
            if (!allowPanning) return;

            Vector3 panMovement = Vector3.zero;

            // WASD keyboard panning
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                panMovement += Vector3.up;
                followTarget = false; // Disable follow when manually panning
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                panMovement += Vector3.down;
                followTarget = false;
            }
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                panMovement += Vector3.left;
                followTarget = false;
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                panMovement += Vector3.right;
                followTarget = false;
            }

            // Edge panning (mouse near screen edges)
            if (enableEdgePan)
            {
                Vector3 mousePos = Input.mousePosition;

                if (mousePos.x < edgePanBorderSize)
                {
                    panMovement += Vector3.left;
                    followTarget = false;
                }
                else if (mousePos.x > Screen.width - edgePanBorderSize)
                {
                    panMovement += Vector3.right;
                    followTarget = false;
                }

                if (mousePos.y < edgePanBorderSize)
                {
                    panMovement += Vector3.down;
                    followTarget = false;
                }
                else if (mousePos.y > Screen.height - edgePanBorderSize)
                {
                    panMovement += Vector3.up;
                    followTarget = false;
                }
            }

            if (panMovement != Vector3.zero)
            {
                targetPosition += panMovement.normalized * panSpeed * Time.deltaTime;
            }
        }

        private void HandleDrag()
        {
            if (!allowDrag) return;

            // Start dragging
            if (Input.GetKeyDown(dragKey) || Input.GetKeyDown(altDragKey))
            {
                isDragging = true;
                dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
                followTarget = false; // Disable follow when dragging
            }

            // Stop dragging
            if (Input.GetKeyUp(dragKey) || Input.GetKeyUp(altDragKey))
            {
                isDragging = false;
            }

            // Perform drag
            if (isDragging)
            {
                Vector3 currentMouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector3 difference = dragOrigin - currentMouseWorld;

                // Only move in X and Y (keep Z constant for 2D camera)
                difference.z = 0;

                targetPosition += difference * dragPanSpeed;
            }
        }

        private void HandleFollow()
        {
            if (followTarget && target != null)
            {
                // Smooth follow the target
                Vector3 targetPos = target.position;
                targetPos.z = transform.position.z; // Maintain camera depth

                targetPosition = Vector3.Lerp(targetPosition, targetPos, followSmoothing * Time.deltaTime);
            }

            // Apply target position
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
        }

        private void ApplyBounds()
        {
            if (!useBounds) return;

            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
            pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
            transform.position = pos;
            targetPosition = pos;
        }

        /// <summary>
        /// Focus camera on a specific cell
        /// </summary>
        public void FocusOnCell(int cellId, bool immediate = false)
        {
            Vector3 cellWorldPos = IsometricHelper.CellIdToWorldPosition(cellId);
            cellWorldPos.z = transform.position.z;

            if (immediate)
            {
                transform.position = cellWorldPos;
                targetPosition = cellWorldPos;
            }
            else
            {
                targetPosition = cellWorldPos;
            }

            followTarget = false;
        }

        /// <summary>
        /// Focus camera on a world position
        /// </summary>
        public void FocusOn(Vector3 position, bool immediate = false)
        {
            position.z = transform.position.z;

            if (immediate)
            {
                transform.position = position;
                targetPosition = position;
            }
            else
            {
                targetPosition = position;
            }

            followTarget = false;
        }

        /// <summary>
        /// Set camera bounds based on map size
        /// </summary>
        public void SetBounds(Vector2 min, Vector2 max)
        {
            minBounds = min;
            maxBounds = max;
            useBounds = true;
        }

        /// <summary>
        /// Enable/disable camera bounds
        /// </summary>
        public void SetUseBounds(bool enabled)
        {
            useBounds = enabled;
        }

        /// <summary>
        /// Reset camera to follow target
        /// </summary>
        public void ResetToTarget()
        {
            if (target != null)
            {
                followTarget = true;
                targetPosition = target.position;
                targetPosition.z = transform.position.z;
            }
        }

        /// <summary>
        /// Set zoom level instantly
        /// </summary>
        public void SetZoom(float zoom, bool immediate = false)
        {
            currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);

            if (immediate && cam != null && cam.orthographic)
            {
                cam.orthographicSize = currentZoom;
            }
        }

        /// <summary>
        /// Enable/disable follow mode
        /// </summary>
        public void SetFollowMode(bool enabled)
        {
            followTarget = enabled;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (useBounds)
            {
                // Draw bounds
                Gizmos.color = Color.yellow;
                Vector3 bottomLeft = new Vector3(minBounds.x, minBounds.y, 0);
                Vector3 bottomRight = new Vector3(maxBounds.x, minBounds.y, 0);
                Vector3 topRight = new Vector3(maxBounds.x, maxBounds.y, 0);
                Vector3 topLeft = new Vector3(minBounds.x, maxBounds.y, 0);

                Gizmos.DrawLine(bottomLeft, bottomRight);
                Gizmos.DrawLine(bottomRight, topRight);
                Gizmos.DrawLine(topRight, topLeft);
                Gizmos.DrawLine(topLeft, bottomLeft);
            }

            if (target != null)
            {
                // Draw line to target
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
#endif
    }
}
