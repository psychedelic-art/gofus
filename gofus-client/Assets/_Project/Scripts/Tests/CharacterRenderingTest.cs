using UnityEngine;
using GOFUS.UI;
using GOFUS.Rendering;

namespace GOFUS.Tests
{
    /// <summary>
    /// Test script for character rendering system
    /// Attach this to a GameObject in a test scene to verify character rendering works
    /// </summary>
    public class CharacterRenderingTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool createOnStart = true;
        [SerializeField] private int testClassId = 1; // Feca by default
        [SerializeField] private bool isMale = true;
        [SerializeField] private Vector3 spawnPosition = Vector3.zero;

        [Header("Test All Classes")]
        [SerializeField] private bool testAllClasses = false;
        [SerializeField] private float spacing = 2f;

        [Header("Runtime Controls")]
        [SerializeField] private bool showDebugInfo = true;

        private CharacterLayerRenderer currentCharacter;
        private string currentAnimState = "static"; // Current animation state: static, walk, or run

        private void Start()
        {
            if (createOnStart)
            {
                if (testAllClasses)
                {
                    TestAllCharacterClasses();
                }
                else
                {
                    TestSingleCharacter();
                }
            }
        }

        /// <summary>
        /// Test rendering a single character class
        /// </summary>
        public void TestSingleCharacter()
        {
            Debug.Log($"[CharacterRenderingTest] Testing character class {testClassId} ({(isMale ? "Male" : "Female")})");

            // Create character using ClassSpriteManager extension
            currentCharacter = ClassSpriteManager.Instance.CreateCharacterRenderer(
                testClassId,
                isMale,
                null,
                spawnPosition
            );

            if (currentCharacter != null)
            {
                Debug.Log($"[CharacterRenderingTest] Successfully created character renderer");
                Debug.Log($"[CharacterRenderingTest] Character has {currentCharacter.LayerCount} layers");
            }
            else
            {
                Debug.LogError($"[CharacterRenderingTest] Failed to create character renderer");
            }
        }

        /// <summary>
        /// Test rendering all 12 character classes
        /// </summary>
        public void TestAllCharacterClasses()
        {
            Debug.Log("[CharacterRenderingTest] Testing all character classes");

            int rows = 3;
            int cols = 4;
            int classIndex = 0;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int classId = classIndex + 1;
                    if (classId > 12) break;

                    Vector3 position = new Vector3(
                        col * spacing,
                        -row * spacing,
                        0
                    );

                    CharacterLayerRenderer character = ClassSpriteManager.Instance.CreateCharacterRenderer(
                        classId,
                        isMale,
                        transform,
                        position
                    );

                    if (character != null)
                    {
                        string className = ClassSpriteManager.Instance.GetClassName(classId);
                        character.gameObject.name = $"TestCharacter_{className}";
                        Debug.Log($"[CharacterRenderingTest] Created {className} at position {position}");
                    }

                    classIndex++;
                }
            }

            Debug.Log($"[CharacterRenderingTest] Created {classIndex} test characters");
        }

        /// <summary>
        /// Change the current character's animation
        /// </summary>
        public void ChangeAnimation(string animationName)
        {
            if (currentCharacter != null)
            {
                currentCharacter.SetAnimation(animationName);
                Debug.Log($"[CharacterRenderingTest] Changed animation to: {animationName}");
            }
            else
            {
                Debug.LogWarning("[CharacterRenderingTest] No character to animate");
            }
        }

        /// <summary>
        /// Change the current character's class
        /// </summary>
        public void ChangeClass(int newClassId)
        {
            if (currentCharacter != null)
            {
                currentCharacter.SetClass(newClassId, isMale);
                string className = ClassSpriteManager.Instance.GetClassName(newClassId);
                Debug.Log($"[CharacterRenderingTest] Changed class to: {className}");
            }
            else
            {
                Debug.LogWarning("[CharacterRenderingTest] No character to change");
            }
        }

        /// <summary>
        /// Clear all test characters
        /// </summary>
        public void ClearAllCharacters()
        {
            // Destroy all child objects (test characters)
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            if (currentCharacter != null && currentCharacter.transform.parent != transform)
            {
                Destroy(currentCharacter.gameObject);
            }

            currentCharacter = null;
            Debug.Log("[CharacterRenderingTest] Cleared all test characters");
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 320, 550));
            GUILayout.Label("Character Rendering Test");
            GUILayout.Space(10);

            if (GUILayout.Button("Test Single Character"))
            {
                ClearAllCharacters();
                TestSingleCharacter();
            }

            if (GUILayout.Button("Test All Classes"))
            {
                ClearAllCharacters();
                TestAllCharacterClasses();
            }

            if (GUILayout.Button("Clear All"))
            {
                ClearAllCharacters();
            }

            GUILayout.Space(10);
            GUILayout.Label("Animation State:");
            
            // Animation state selector
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Static", currentAnimState == "static" ? GUI.skin.box : GUI.skin.button))
            {
                currentAnimState = "static";
            }
            if (GUILayout.Button("Walk", currentAnimState == "walk" ? GUI.skin.box : GUI.skin.button))
            {
                currentAnimState = "walk";
            }
            if (GUILayout.Button("Run", currentAnimState == "run" ? GUI.skin.box : GUI.skin.button))
            {
                currentAnimState = "run";
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Direction (Compass):");
            
            // Direction grid - arranged like a compass
            // Row 1: NW  N  NE
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("NW", GUILayout.Width(60)))
            {
                ChangeAnimation(currentAnimState + "NW");
            }
            if (GUILayout.Button("N", GUILayout.Width(60)))
            {
                ChangeAnimation(currentAnimState + "N");
            }
            if (GUILayout.Button("NE", GUILayout.Width(60)))
            {
                ChangeAnimation(currentAnimState + "NE");
            }
            GUILayout.EndHorizontal();

            // Row 2: W  •  E
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("W", GUILayout.Width(60)))
            {
                ChangeAnimation(currentAnimState + "W");
            }
            GUILayout.Label("•", GUILayout.Width(60)); // Center marker
            if (GUILayout.Button("E", GUILayout.Width(60)))
            {
                ChangeAnimation(currentAnimState + "E");
            }
            GUILayout.EndHorizontal();

            // Row 3: SW  S  SE
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("SW", GUILayout.Width(60)))
            {
                ChangeAnimation(currentAnimState + "SW");
            }
            if (GUILayout.Button("S", GUILayout.Width(60)))
            {
                ChangeAnimation(currentAnimState + "S");
            }
            if (GUILayout.Button("SE", GUILayout.Width(60)))
            {
                ChangeAnimation(currentAnimState + "SE");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label($"Current Class: {testClassId}");

            testClassId = (int)GUILayout.HorizontalSlider(testClassId, 1, 12);

            if (GUILayout.Button($"Change to Class {testClassId}"))
            {
                ChangeClass(testClassId);
            }

            GUILayout.Space(10);
            if (currentCharacter != null)
            {
                GUILayout.Label($"Active Character:");
                GUILayout.Label($"- Class: {ClassSpriteManager.Instance.GetClassName(currentCharacter.ClassId)}");
                GUILayout.Label($"- Gender: {(currentCharacter.IsMale ? "Male" : "Female")}");
                GUILayout.Label($"- Layers: {currentCharacter.LayerCount}");
                GUILayout.Label($"- Animation: {currentCharacter.CurrentAnimation}");
            }

            GUILayout.EndArea();
        }
    }
}
