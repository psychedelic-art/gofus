using System.Collections;
using UnityEngine;
using GOFUS.Map;
using GOFUS.Player;
using GOFUS.UI.Screens;

namespace GOFUS.Tests
{
    /// <summary>
    /// TDD Test for Map Click and Character Movement
    /// This test verifies that clicking on a map cell causes the character to move to that cell.
    ///
    /// Usage: Attach this to a GameObject in the scene and click "Run Test" button in Inspector
    /// </summary>
    public class MapClickMovementTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool autoRunOnStart = false;
        [SerializeField] private int testCellId = 250; // Cell to click on
        [SerializeField] private float timeoutSeconds = 10f;

        [Header("Test Results")]
        [SerializeField] private string testStatus = "Not Started";
        [SerializeField] private string lastError = "";
        [SerializeField] private float testDuration = 0f;

        [Header("References (Auto-found)")]
        [SerializeField] private GameHUD gameHUD;
        [SerializeField] private MapRenderer mapRenderer;
        [SerializeField] private PlayerController playerController;

        private bool testRunning = false;
        private int testsPassed = 0;
        private int testsFailed = 0;

        private void Start()
        {
            if (autoRunOnStart)
            {
                StartCoroutine(RunAllTests());
            }
        }

        /// <summary>
        /// Run all tests in sequence
        /// </summary>
        public IEnumerator RunAllTests()
        {
            if (testRunning)
            {
                Debug.LogWarning("[MapClickMovementTest] Test already running!");
                yield break;
            }

            testRunning = true;
            testsPassed = 0;
            testsFailed = 0;
            float startTime = Time.time;

            Debug.Log("========================================");
            Debug.Log("[MapClickMovementTest] Starting TDD Test Suite");
            Debug.Log("========================================");

            // Test 1: Find required components
            yield return StartCoroutine(Test_FindComponents());

            // Test 2: Verify map is loaded
            yield return StartCoroutine(Test_MapLoaded());

            // Test 3: Verify cells have colliders
            yield return StartCoroutine(Test_CellsHaveColliders());

            // Test 4: Verify PlayerController is initialized
            yield return StartCoroutine(Test_PlayerControllerInitialized());

            // Test 5: Simulate cell click
            yield return StartCoroutine(Test_SimulateCellClick());

            // Test 6: Verify character moved
            yield return StartCoroutine(Test_VerifyMovement());

            testDuration = Time.time - startTime;

            Debug.Log("========================================");
            Debug.Log($"[MapClickMovementTest] Test Suite Complete!");
            Debug.Log($"[MapClickMovementTest] Passed: {testsPassed}, Failed: {testsFailed}");
            Debug.Log($"[MapClickMovementTest] Duration: {testDuration:F2}s");
            Debug.Log("========================================");

            testStatus = testsFailed == 0 ? "✅ ALL TESTS PASSED" : $"❌ {testsFailed} TESTS FAILED";
            testRunning = false;
        }

        /// <summary>
        /// Test 1: Find all required components in the scene
        /// </summary>
        private IEnumerator Test_FindComponents()
        {
            Debug.Log("[TEST 1] Finding required components...");

            // Find GameHUD
            gameHUD = FindObjectOfType<GameHUD>();
            if (gameHUD == null)
            {
                LogTestFailure("GameHUD not found in scene");
                yield break;
            }

            // Find MapRenderer
            mapRenderer = FindObjectOfType<MapRenderer>();
            if (mapRenderer == null)
            {
                LogTestFailure("MapRenderer not found in scene");
                yield break;
            }

            // Find PlayerController (might not exist yet if character not created)
            playerController = FindObjectOfType<PlayerController>();
            // Note: PlayerController might be null at this stage, that's ok

            LogTestSuccess("All required components found");
            yield return null;
        }

        /// <summary>
        /// Test 2: Verify map is loaded with cells
        /// </summary>
        private IEnumerator Test_MapLoaded()
        {
            Debug.Log("[TEST 2] Verifying map is loaded...");

            if (mapRenderer == null)
            {
                LogTestFailure("MapRenderer is null");
                yield break;
            }

            if (!mapRenderer.IsInitialized)
            {
                LogTestFailure("MapRenderer is not initialized");
                yield break;
            }

            if (mapRenderer.Grid == null)
            {
                LogTestFailure("Map grid is null");
                yield break;
            }

            if (mapRenderer.Grid.Cells == null || mapRenderer.Grid.Cells.Length == 0)
            {
                LogTestFailure("Map has no cells");
                yield break;
            }

            Debug.Log($"[TEST 2] Map loaded with {mapRenderer.Grid.Cells.Length} cells");
            LogTestSuccess("Map is loaded correctly");
            yield return null;
        }

        /// <summary>
        /// Test 3: Verify cells have colliders for click detection
        /// </summary>
        private IEnumerator Test_CellsHaveColliders()
        {
            Debug.Log("[TEST 3] Checking if cells have colliders...");

            // Find all CellClickHandlers in the scene
            CellClickHandler[] handlers = FindObjectsOfType<CellClickHandler>();

            if (handlers.Length == 0)
            {
                LogTestFailure("No CellClickHandlers found! Cells were not created.");
                yield break;
            }

            Debug.Log($"[TEST 3] Found {handlers.Length} CellClickHandlers");

            // Check if they have colliders
            int cellsWithColliders = 0;
            int cellsWithoutColliders = 0;

            foreach (var handler in handlers)
            {
                Collider2D collider = handler.GetComponent<Collider2D>();
                if (collider != null && collider.enabled)
                {
                    cellsWithColliders++;
                }
                else
                {
                    cellsWithoutColliders++;
                }
            }

            Debug.Log($"[TEST 3] Cells with colliders: {cellsWithColliders}");
            Debug.Log($"[TEST 3] Cells without colliders: {cellsWithoutColliders}");

            if (cellsWithoutColliders > 0)
            {
                LogTestFailure($"{cellsWithoutColliders} cells are missing colliders!");
                yield break;
            }

            LogTestSuccess("All cells have colliders");
            yield return null;
        }

        /// <summary>
        /// Test 4: Verify PlayerController exists and is initialized
        /// </summary>
        private IEnumerator Test_PlayerControllerInitialized()
        {
            Debug.Log("[TEST 4] Checking PlayerController initialization...");

            // Try to find PlayerController again (it might have been created after character spawn)
            playerController = FindObjectOfType<PlayerController>();

            if (playerController == null)
            {
                LogTestFailure("PlayerController not found! Character might not be spawned yet.");
                yield break;
            }

            Debug.Log($"[TEST 4] PlayerController found on GameObject: {playerController.gameObject.name}");
            Debug.Log($"[TEST 4] Current cell: {playerController.CurrentCellId}");
            Debug.Log($"[TEST 4] Is moving: {playerController.IsMoving}");

            LogTestSuccess("PlayerController is initialized");
            yield return null;
        }

        /// <summary>
        /// Test 5: Simulate clicking on a cell
        /// </summary>
        private IEnumerator Test_SimulateCellClick()
        {
            Debug.Log($"[TEST 5] Simulating click on cell {testCellId}...");

            if (mapRenderer == null)
            {
                LogTestFailure("MapRenderer is null");
                yield break;
            }

            // Find the CellClickHandler for the target cell
            CellClickHandler[] handlers = FindObjectsOfType<CellClickHandler>();
            CellClickHandler targetHandler = null;

            foreach (var handler in handlers)
            {
                if (handler.CellId == testCellId)
                {
                    targetHandler = handler;
                    break;
                }
            }

            if (targetHandler == null)
            {
                LogTestFailure($"Could not find CellClickHandler for cell {testCellId}");
                yield break;
            }

            Debug.Log($"[TEST 5] Found CellClickHandler for cell {testCellId}");

            // Manually trigger the click (bypass OnMouseDown which might be blocked by UI)
            Debug.Log($"[TEST 5] Triggering click event...");
            targetHandler.TriggerClick();

            Debug.Log($"[TEST 5] Click event triggered, waiting for response...");
            yield return new WaitForSeconds(0.5f); // Give time for event to propagate

            LogTestSuccess($"Cell {testCellId} click simulated");
        }

        /// <summary>
        /// Test 6: Verify character actually moved to the target cell
        /// </summary>
        private IEnumerator Test_VerifyMovement()
        {
            Debug.Log("[TEST 6] Verifying character movement...");

            if (playerController == null)
            {
                LogTestFailure("PlayerController is null");
                yield break;
            }

            int initialCell = playerController.CurrentCellId;
            Debug.Log($"[TEST 6] Initial cell: {initialCell}");
            Debug.Log($"[TEST 6] Target cell: {testCellId}");

            if (initialCell == testCellId)
            {
                Debug.LogWarning("[TEST 6] Character is already at target cell, choosing different cell");
                testCellId = initialCell + 5; // Try a different cell
            }

            // Wait for movement to complete (with timeout)
            float startTime = Time.time;
            bool movementStarted = false;

            while (Time.time - startTime < timeoutSeconds)
            {
                if (playerController.IsMoving)
                {
                    movementStarted = true;
                    Debug.Log("[TEST 6] Movement started!");
                    break;
                }
                yield return new WaitForSeconds(0.1f);
            }

            if (!movementStarted)
            {
                LogTestFailure("Character did not start moving within timeout");
                Debug.LogError("[TEST 6] ❌ CLICK NOT DETECTED! This is the core issue.");
                Debug.LogError("[TEST 6] Possible causes:");
                Debug.LogError("[TEST 6]   1. UI is blocking clicks (most likely)");
                Debug.LogError("[TEST 6]   2. Colliders not set up correctly");
                Debug.LogError("[TEST 6]   3. Camera raycast not working");
                Debug.LogError("[TEST 6]   4. Event subscription failed");
                yield break;
            }

            // Wait for movement to complete
            while (playerController.IsMoving && Time.time - startTime < timeoutSeconds)
            {
                yield return new WaitForSeconds(0.1f);
            }

            if (playerController.IsMoving)
            {
                LogTestFailure("Movement did not complete within timeout");
                yield break;
            }

            int finalCell = playerController.CurrentCellId;
            Debug.Log($"[TEST 6] Final cell: {finalCell}");

            if (finalCell == testCellId)
            {
                LogTestSuccess($"Character successfully moved from cell {initialCell} to {finalCell}");
            }
            else
            {
                LogTestFailure($"Character moved to wrong cell. Expected: {testCellId}, Got: {finalCell}");
            }
        }

        private void LogTestSuccess(string message)
        {
            testsPassed++;
            Debug.Log($"✅ {message}");
        }

        private void LogTestFailure(string message)
        {
            testsFailed++;
            lastError = message;
            Debug.LogError($"❌ {message}");
        }

        // Inspector button to run tests
        [ContextMenu("Run All Tests")]
        public void RunTestsFromInspector()
        {
            StartCoroutine(RunAllTests());
        }
    }
}
