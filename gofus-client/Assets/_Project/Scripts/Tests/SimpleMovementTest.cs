using UnityEngine;
using GOFUS.Map;
using GOFUS.Player;

namespace GOFUS.Tests
{
    /// <summary>
    /// Simple movement test without coroutines
    /// </summary>
    public class SimpleMovementTest : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int targetCellId = 250;

        private PlayerController playerController;
        private MapRenderer mapRenderer;
        private bool testRun = false;

        private void Update()
        {
            // Press T to run test
            if (Input.GetKeyDown(KeyCode.T) && !testRun)
            {
                RunSimpleTest();
            }
        }

        [ContextMenu("Run Simple Test")]
        public void RunSimpleTest()
        {
            Debug.Log("=== SIMPLE MOVEMENT TEST ===");

            // Find components
            playerController = FindObjectOfType<PlayerController>();
            mapRenderer = FindObjectOfType<MapRenderer>();

            if (playerController == null)
            {
                Debug.LogError("PlayerController not found!");
                return;
            }

            if (mapRenderer == null)
            {
                Debug.LogError("MapRenderer not found!");
                return;
            }

            Debug.Log($"Current position: {playerController.CurrentCellId}");
            Debug.Log($"Target position: {targetCellId}");

            // Find target cell handler
            CellClickHandler[] handlers = FindObjectsOfType<CellClickHandler>();
            Debug.Log($"Found {handlers.Length} cell handlers");

            CellClickHandler targetHandler = null;
            foreach (var handler in handlers)
            {
                if (handler.CellId == targetCellId)
                {
                    targetHandler = handler;
                    break;
                }
            }

            if (targetHandler != null)
            {
                Debug.Log($"Triggering click on cell {targetCellId}");
                targetHandler.TriggerClick();
                testRun = true;
            }
            else
            {
                Debug.LogError($"Could not find handler for cell {targetCellId}");
            }
        }
    }
}
