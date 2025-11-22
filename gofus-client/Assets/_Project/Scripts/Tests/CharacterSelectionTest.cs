using UnityEngine;
using GOFUS.UI;
using GOFUS.UI.Screens;
using GOFUS.Utilities;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GOFUS.Tests
{
    /// <summary>
    /// Test script for verifying character selection screen functionality
    /// </summary>
    public class CharacterSelectionTest : MonoBehaviour
    {
        private CharacterSelectionScreen characterScreen;
        private bool testCompleted = false;
        private int testsPassed = 0;
        private int testsTotal = 0;

        /// <summary>
        /// Run all character selection tests
        /// </summary>
        public IEnumerator RunTests()
        {
            Debug.Log("========================================");
            Debug.Log("Character Selection Screen Tests");
            Debug.Log("========================================");

            // Test 1: Placeholder Asset Generation
            yield return TestPlaceholderGeneration();

            // Test 2: ClassSpriteManager Loading
            yield return TestClassSpriteManager();

            // Test 3: Character Selection Screen Initialization
            yield return TestCharacterScreenInit();

            // Test 4: Mock Data Loading
            yield return TestMockDataLoading();

            // Test 5: Character Selection Interaction
            yield return TestCharacterSelection();

            // Test Summary
            PrintTestSummary();
            testCompleted = true;
        }

        private IEnumerator TestPlaceholderGeneration()
        {
            testsTotal++;
            Debug.Log("\n[TEST 1] Testing Placeholder Asset Generation...");

            try
            {
                // Generate placeholders if they don't exist
                if (!PlaceholderAssetGenerator.PlaceholdersExist())
                {
                    PlaceholderAssetGenerator.GenerateAllPlaceholders();
                    Debug.Log("✓ Placeholder sprites generated successfully");
                }
                else
                {
                    Debug.Log("✓ Placeholder sprites already exist");
                }

                // Test individual sprite generation
                Sprite testSprite = PlaceholderAssetGenerator.GeneratePlaceholderSprite(1, 64, 64);
                if (testSprite != null && testSprite.texture != null)
                {
                    Debug.Log("✓ Individual sprite generation works");
                    testsPassed++;
                }
                else
                {
                    Debug.LogError("✗ Failed to generate individual sprite");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Placeholder generation failed: {e.Message}");
            }

            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator TestClassSpriteManager()
        {
            testsTotal++;
            Debug.Log("\n[TEST 2] Testing ClassSpriteManager...");

            try
            {
                // Get or create ClassSpriteManager instance
                ClassSpriteManager spriteManager = ClassSpriteManager.Instance;

                // Load sprites
                spriteManager.LoadClassSprites();
                Debug.Log($"✓ ClassSpriteManager loaded {spriteManager.LoadedSpriteCount} sprites");

                // Test sprite retrieval
                Sprite fecaSprite = spriteManager.GetClassSprite(1);
                Sprite sramIcon = spriteManager.GetClassIcon(4);

                if (fecaSprite != null && sramIcon != null)
                {
                    Debug.Log("✓ Sprite retrieval works correctly");
                    testsPassed++;
                }
                else
                {
                    Debug.LogError("✗ Failed to retrieve sprites");
                }

                // Test class name lookup
                string className = spriteManager.GetClassName(7);
                if (className == "Eniripsa")
                {
                    Debug.Log("✓ Class name lookup works");
                }
                else
                {
                    Debug.LogError($"✗ Class name lookup failed. Expected 'Eniripsa', got '{className}'");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ ClassSpriteManager test failed: {e.Message}");
            }

            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator TestCharacterScreenInit()
        {
            testsTotal++;
            Debug.Log("\n[TEST 3] Testing Character Selection Screen Initialization...");

            try
            {
                // Find or create character selection screen
                GameObject screenObj = GameObject.Find("CharacterSelectionScreen");
                if (screenObj == null)
                {
                    screenObj = new GameObject("CharacterSelectionScreen");
                    characterScreen = screenObj.AddComponent<CharacterSelectionScreen>();
                }
                else
                {
                    characterScreen = screenObj.GetComponent<CharacterSelectionScreen>();
                }

                // Force use of mock data for testing
                PlayerPrefs.SetInt("use_mock_characters", 1);
                PlayerPrefs.Save();

                // Initialize the screen
                characterScreen.Initialize();
                Debug.Log("✓ Character selection screen initialized");

                // Check if UI elements were created
                if (characterScreen.CharacterSlots != null && characterScreen.CharacterSlots.Count == CharacterSelectionScreen.MAX_CHARACTERS)
                {
                    Debug.Log($"✓ Created {characterScreen.CharacterSlots.Count} character slots");
                    testsPassed++;
                }
                else
                {
                    Debug.LogError("✗ Character slots not properly created");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Screen initialization failed: {e.Message}");
            }

            yield return new WaitForSeconds(1f);
        }

        private IEnumerator TestMockDataLoading()
        {
            testsTotal++;
            Debug.Log("\n[TEST 4] Testing Mock Data Loading...");

            bool skipTest = false;

            try
            {
                if (characterScreen == null)
                {
                    Debug.LogError("✗ Character screen not initialized");
                    skipTest = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Mock data loading failed: {e.Message}");
                skipTest = true;
            }

            if (skipTest)
            {
                yield return new WaitForSeconds(0.5f);
                yield break;
            }

            // Wait for mock data to load
            yield return new WaitForSeconds(2f);

            try
            {
                // Check if characters were loaded
                int charCount = characterScreen.CharacterCount;
                if (charCount > 0)
                {
                    Debug.Log($"✓ Loaded {charCount} mock characters");
                    testsPassed++;

                    // Verify character data
                    for (int i = 0; i < charCount && i < characterScreen.CharacterSlots.Count; i++)
                    {
                        var slot = characterScreen.CharacterSlots[i];
                        if (slot != null && slot.HasCharacter)
                        {
                            Debug.Log($"  - Slot {i}: {slot.CharacterName} (Level {slot.Level})");
                        }
                    }
                }
                else
                {
                    Debug.LogError("✗ No mock characters loaded");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Mock data loading test exception: {e.Message}");
            }

            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator TestCharacterSelection()
        {
            testsTotal++;
            Debug.Log("\n[TEST 5] Testing Character Selection Interaction...");

            bool skipTest = false;

            try
            {
                if (characterScreen == null || characterScreen.CharacterCount == 0)
                {
                    Debug.LogError("✗ Cannot test selection - no screen or characters");
                    skipTest = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Character selection test failed: {e.Message}");
                skipTest = true;
            }

            if (skipTest)
            {
                yield return new WaitForSeconds(0.5f);
                yield break;
            }

            try
            {
                // Test selecting first character
                characterScreen.SelectCharacter(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Character selection failed: {e.Message}");
            }

            yield return new WaitForSeconds(0.5f);

            try
            {
                if (characterScreen.SelectedSlotIndex == 0 && characterScreen.CanPlay)
                {
                    Debug.Log("✓ Character selection works");
                    Debug.Log($"  Selected character ID: {characterScreen.SelectedCharacterId}");
                    testsPassed++;
                }
                else
                {
                    Debug.LogError("✗ Character selection failed");
                }

                // Test play button state
                if (characterScreen.CanPlay)
                {
                    Debug.Log("✓ Play button enabled correctly");
                }
                else
                {
                    Debug.LogError("✗ Play button not enabled");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Character selection verification failed: {e.Message}");
            }

            yield return new WaitForSeconds(0.5f);
        }

        private void PrintTestSummary()
        {
            Debug.Log("\n========================================");
            Debug.Log("Test Summary");
            Debug.Log("========================================");
            Debug.Log($"Tests Passed: {testsPassed}/{testsTotal}");

            if (testsPassed == testsTotal)
            {
                Debug.Log("✓✓✓ ALL TESTS PASSED ✓✓✓");
                Debug.Log("\nCharacter Selection Screen is working correctly!");
                Debug.Log("The screen will now display mock characters with placeholder sprites.");
            }
            else
            {
                Debug.LogWarning($"⚠ Some tests failed ({testsTotal - testsPassed} failures)");
                Debug.LogWarning("Please check the error messages above for details.");
            }

            Debug.Log("\n========================================");
            Debug.Log("Next Steps:");
            Debug.Log("1. Install JPEXS FFDec and Dofus client");
            Debug.Log("2. Run extract_priority_assets.bat to extract real assets");
            Debug.Log("3. Import extracted assets into Unity");
            Debug.Log("4. Test with real character sprites");
            Debug.Log("========================================");

            // Reset mock data preference
            PlayerPrefs.SetInt("use_mock_characters", 0);
            PlayerPrefs.Save();
        }

#if UNITY_EDITOR
        [MenuItem("GOFUS/Tests/Test Character Selection")]
        public static void RunTestMenuItem()
        {
            GameObject testObj = new GameObject("CharacterSelectionTest");
            CharacterSelectionTest test = testObj.AddComponent<CharacterSelectionTest>();
            test.StartCoroutine(test.RunTests());
        }

        [MenuItem("GOFUS/Tests/Enable Mock Characters")]
        public static void EnableMockCharacters()
        {
            PlayerPrefs.SetInt("use_mock_characters", 1);
            PlayerPrefs.Save();
            Debug.Log("Mock characters enabled. Restart the character selection screen to see test data.");
        }

        [MenuItem("GOFUS/Tests/Disable Mock Characters")]
        public static void DisableMockCharacters()
        {
            PlayerPrefs.SetInt("use_mock_characters", 0);
            PlayerPrefs.Save();
            Debug.Log("Mock characters disabled. The screen will now attempt to load from backend.");
        }
#endif
    }
}