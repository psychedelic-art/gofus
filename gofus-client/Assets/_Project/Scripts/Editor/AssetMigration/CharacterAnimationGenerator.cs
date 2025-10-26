using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace GOFUS.Editor.AssetMigration
{
    /// <summary>
    /// Generates complete animation controllers for Dofus characters
    /// Creates state machines with 8-directional movement and combat animations
    /// </summary>
    public class CharacterAnimationGenerator : EditorWindow
    {
        #region Properties

        private string characterName = "";
        private DofusClass characterClass = DofusClass.Feca;
        private GameObject characterPrefab;
        private List<AnimationClip> animationClips = new List<AnimationClip>();
        private AnimatorController generatedController;

        private bool createIdleStates = true;
        private bool createMovementStates = true;
        private bool createCombatStates = true;
        private bool createEmoteStates = false;
        private bool use8Directions = true;
        private bool useBlendTrees = true;

        private Vector2 scrollPosition;
        private List<string> generationLog = new List<string>();

        #endregion

        #region Window

        [MenuItem("GOFUS/Asset Migration/Character Animation Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<CharacterAnimationGenerator>("Animation Generator");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            DrawHeader();
            DrawCharacterSettings();
            DrawAnimationSettings();
            DrawClipSelection();
            DrawActionButtons();
            DrawGenerationLog();
        }

        private void DrawHeader()
        {
            GUILayout.Label("Character Animation Generator", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Create Dofus-style animation controllers", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.Separator();
        }

        private void DrawCharacterSettings()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Character Settings", EditorStyles.boldLabel);

            characterName = EditorGUILayout.TextField("Character Name:", characterName);
            characterClass = (DofusClass)EditorGUILayout.EnumPopup("Class:", characterClass);
            characterPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Character Prefab:",
                characterPrefab,
                typeof(GameObject),
                false
            );
        }

        private void DrawAnimationSettings()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);

            createIdleStates = EditorGUILayout.Toggle("Create Idle States", createIdleStates);
            createMovementStates = EditorGUILayout.Toggle("Create Movement States", createMovementStates);
            createCombatStates = EditorGUILayout.Toggle("Create Combat States", createCombatStates);
            createEmoteStates = EditorGUILayout.Toggle("Create Emote States", createEmoteStates);

            EditorGUILayout.Space(5);

            use8Directions = EditorGUILayout.Toggle("8-Directional Movement", use8Directions);
            useBlendTrees = EditorGUILayout.Toggle("Use Blend Trees", useBlendTrees);

            if (useBlendTrees)
            {
                EditorGUILayout.HelpBox(
                    "Blend trees will create smooth transitions between directional animations.",
                    MessageType.Info
                );
            }
        }

        private void DrawClipSelection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Animation Clips", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Auto-Find Clips"))
            {
                AutoFindAnimationClips();
            }

            if (GUILayout.Button("Add Clip"))
            {
                animationClips.Add(null);
            }

            if (GUILayout.Button("Clear All"))
            {
                animationClips.Clear();
            }

            EditorGUILayout.EndHorizontal();

            if (animationClips.Count > 0)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

                for (int i = 0; i < animationClips.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    animationClips[i] = (AnimationClip)EditorGUILayout.ObjectField(
                        animationClips[i],
                        typeof(AnimationClip),
                        false
                    );

                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        animationClips.RemoveAt(i);
                        i--;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.LabelField($"Total Clips: {animationClips.Count}");
            }
            else
            {
                EditorGUILayout.HelpBox("No animation clips loaded. Click 'Auto-Find Clips' to search for animations.", MessageType.Info);
            }
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.Space(10);

            GUI.backgroundColor = Color.green;
            GUI.enabled = !string.IsNullOrEmpty(characterName) && animationClips.Count > 0;

            if (GUILayout.Button("Generate Controller", GUILayout.Height(40)))
            {
                GenerateAnimatorController();
            }

            GUI.enabled = true;
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = generatedController != null;

            if (GUILayout.Button("Apply to Prefab"))
            {
                ApplyControllerToPrefab();
            }

            if (GUILayout.Button("Save Controller"))
            {
                SaveController();
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGenerationLog()
        {
            if (generationLog.Count > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Generation Log", EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                foreach (string log in generationLog)
                {
                    EditorGUILayout.LabelField(log, EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Clear Log"))
                {
                    generationLog.Clear();
                }
            }
        }

        #endregion

        #region Controller Generation

        private void GenerateAnimatorController()
        {
            generationLog.Clear();
            AddLog("Starting controller generation...");

            try
            {
                // Create new controller
                generatedController = AnimatorController.CreateAnimatorControllerAtPath(
                    $"Assets/_Project/ImportedAssets/Animations/Controllers/{characterName}_Controller.controller"
                );

                AddLog($"Created controller: {generatedController.name}");

                // Add parameters
                AddParameters();

                // Create layers
                CreateBaseLayer();

                if (createCombatStates)
                {
                    CreateCombatLayer();
                }

                if (createEmoteStates)
                {
                    CreateEmoteLayer();
                }

                // Save controller
                EditorUtility.SetDirty(generatedController);
                AssetDatabase.SaveAssets();

                AddLog("Controller generation complete!");

                EditorUtility.DisplayDialog(
                    "Success",
                    $"Animation controller generated successfully!\n\n" +
                    $"States created: {generatedController.layers[0].stateMachine.states.Length}\n" +
                    $"Parameters: {generatedController.parameters.Length}",
                    "OK"
                );
            }
            catch (Exception e)
            {
                AddLog($"Error: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate controller: {e.Message}", "OK");
            }
        }

        private void AddParameters()
        {
            AddLog("Adding parameters...");

            // Movement parameters
            generatedController.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            generatedController.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            generatedController.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            generatedController.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

            // Direction parameter (0-7 for 8 directions)
            generatedController.AddParameter("Direction", AnimatorControllerParameterType.Int);

            // Combat parameters
            if (createCombatStates)
            {
                generatedController.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
                generatedController.AddParameter("Cast", AnimatorControllerParameterType.Trigger);
                generatedController.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
                generatedController.AddParameter("Death", AnimatorControllerParameterType.Trigger);
                generatedController.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
                generatedController.AddParameter("InCombat", AnimatorControllerParameterType.Bool);
            }

            // Emote parameters
            if (createEmoteStates)
            {
                generatedController.AddParameter("EmoteId", AnimatorControllerParameterType.Int);
                generatedController.AddParameter("TriggerEmote", AnimatorControllerParameterType.Trigger);
            }

            AddLog($"Added {generatedController.parameters.Length} parameters");
        }

        private void CreateBaseLayer()
        {
            AddLog("Creating base layer...");

            var baseLayer = generatedController.layers[0];
            var stateMachine = baseLayer.stateMachine;

            // Create states
            AnimatorState idleState = null;
            AnimatorState moveState = null;

            if (createIdleStates)
            {
                if (useBlendTrees && use8Directions)
                {
                    idleState = CreateDirectionalBlendTree(stateMachine, "Idle", "idle");
                }
                else
                {
                    idleState = CreateSimpleState(stateMachine, "Idle", GetClipByName("idle"));
                }
                stateMachine.defaultState = idleState;
            }

            if (createMovementStates)
            {
                if (useBlendTrees && use8Directions)
                {
                    moveState = CreateDirectionalBlendTree(stateMachine, "Move", "walk");
                }
                else
                {
                    moveState = CreateSimpleState(stateMachine, "Move", GetClipByName("walk"));
                }
            }

            // Create transitions
            if (idleState != null && moveState != null)
            {
                // Idle to Move
                var toMove = idleState.AddTransition(moveState);
                toMove.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
                toMove.duration = 0.1f;
                toMove.hasExitTime = false;

                // Move to Idle
                var toIdle = moveState.AddTransition(idleState);
                toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
                toIdle.duration = 0.1f;
                toIdle.hasExitTime = false;
            }

            AddLog($"Base layer created with {stateMachine.states.Length} states");
        }

        private void CreateCombatLayer()
        {
            AddLog("Creating combat layer...");

            generatedController.AddLayer("Combat");
            var combatLayer = generatedController.layers[generatedController.layers.Length - 1];
            combatLayer.defaultWeight = 1;
            var stateMachine = combatLayer.stateMachine;

            // Create states
            var emptyState = stateMachine.AddState("Empty");
            stateMachine.defaultState = emptyState;

            if (use8Directions)
            {
                CreateDirectionalCombatStates(stateMachine);
            }
            else
            {
                CreateSimpleCombatStates(stateMachine);
            }

            AddLog($"Combat layer created with {stateMachine.states.Length} states");
        }

        private void CreateEmoteLayer()
        {
            AddLog("Creating emote layer...");

            generatedController.AddLayer("Emotes");
            var emoteLayer = generatedController.layers[generatedController.layers.Length - 1];
            emoteLayer.defaultWeight = 1;
            var stateMachine = emoteLayer.stateMachine;

            // Create empty state
            var emptyState = stateMachine.AddState("Empty");
            stateMachine.defaultState = emptyState;

            // Find emote clips
            var emoteClips = animationClips.Where(c => c != null && c.name.Contains("emote")).ToList();

            foreach (var clip in emoteClips)
            {
                var state = stateMachine.AddState(clip.name);
                state.motion = clip;

                // Transition from empty
                var fromEmpty = emptyState.AddTransition(state);
                fromEmpty.AddCondition(AnimatorConditionMode.If, 0, "TriggerEmote");
                fromEmpty.hasExitTime = false;

                // Transition back to empty
                var toEmpty = state.AddTransition(emptyState);
                toEmpty.hasExitTime = true;
                toEmpty.exitTime = 0.9f;
            }

            AddLog($"Emote layer created with {emoteClips.Count} emotes");
        }

        #endregion

        #region Blend Tree Creation

        private AnimatorState CreateDirectionalBlendTree(AnimatorStateMachine stateMachine, string name, string animationType)
        {
            var state = stateMachine.AddState(name);
            var blendTree = new BlendTree();

            string blendTreePath = AssetDatabase.GenerateUniqueAssetPath(
                $"Assets/_Project/ImportedAssets/Animations/BlendTrees/{characterName}_{name}.asset"
            );

            blendTree.name = name;
            blendTree.blendType = BlendTreeType.SimpleDirectional2D;
            blendTree.blendParameter = "MoveX";
            blendTree.blendParameterY = "MoveY";

            // Find clips for each direction
            string[] directions = { "S", "SW", "W", "NW", "N", "NE", "E", "SE" };
            Vector2[] positions = {
                new Vector2(0, -1),   // S
                new Vector2(-1, -1),  // SW
                new Vector2(-1, 0),   // W
                new Vector2(-1, 1),   // NW
                new Vector2(0, 1),    // N
                new Vector2(1, 1),    // NE
                new Vector2(1, 0),    // E
                new Vector2(1, -1)    // SE
            };

            for (int i = 0; i < directions.Length; i++)
            {
                var clip = GetClipByNameAndDirection(animationType, directions[i]);
                if (clip != null)
                {
                    blendTree.AddChild(clip, positions[i]);
                    AddLog($"Added {animationType}_{directions[i]} to blend tree");
                }
            }

            state.motion = blendTree;

            // Save blend tree as asset
            AssetDatabase.CreateAsset(blendTree, blendTreePath);

            return state;
        }

        private void CreateDirectionalCombatStates(AnimatorStateMachine stateMachine)
        {
            string[] combatTypes = { "attack", "cast", "hit", "death" };
            string[] directions = use8Directions ?
                new string[] { "S", "SW", "W", "NW", "N", "NE", "E", "SE" } :
                new string[] { "S", "W", "N", "E" };

            foreach (string combatType in combatTypes)
            {
                var combatState = CreateDirectionalBlendTree(stateMachine, combatType.First().ToString().ToUpper() + combatType.Substring(1), combatType);

                // Add transition from empty
                var fromEmpty = stateMachine.defaultState.AddTransition(combatState);

                switch (combatType)
                {
                    case "attack":
                        fromEmpty.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                        break;
                    case "cast":
                        fromEmpty.AddCondition(AnimatorConditionMode.If, 0, "Cast");
                        break;
                    case "hit":
                        fromEmpty.AddCondition(AnimatorConditionMode.If, 0, "Hit");
                        break;
                    case "death":
                        fromEmpty.AddCondition(AnimatorConditionMode.If, 0, "Death");
                        break;
                }

                fromEmpty.hasExitTime = false;
                fromEmpty.duration = 0.05f;

                // Return to empty
                if (combatType != "death")
                {
                    var toEmpty = combatState.AddTransition(stateMachine.defaultState);
                    toEmpty.hasExitTime = true;
                    toEmpty.exitTime = 0.9f;
                    toEmpty.duration = 0.1f;
                }
            }
        }

        private void CreateSimpleCombatStates(AnimatorStateMachine stateMachine)
        {
            string[] combatTypes = { "attack", "cast", "hit", "death" };

            foreach (string combatType in combatTypes)
            {
                var clip = GetClipByName(combatType);
                if (clip != null)
                {
                    var state = stateMachine.AddState(combatType.First().ToString().ToUpper() + combatType.Substring(1));
                    state.motion = clip;

                    // Transition logic similar to directional version
                }
            }
        }

        #endregion

        #region Helper Methods

        private AnimatorState CreateSimpleState(AnimatorStateMachine stateMachine, string name, AnimationClip clip)
        {
            var state = stateMachine.AddState(name);
            if (clip != null)
            {
                state.motion = clip;
            }
            return state;
        }

        private void AutoFindAnimationClips()
        {
            animationClips.Clear();

            if (string.IsNullOrEmpty(characterName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a character name first!", "OK");
                return;
            }

            // Search for animation clips
            string[] searchFolders = {
                "Assets/_Project/ImportedAssets/Animations",
                "Assets/Animations",
                "Assets/Resources/Animations"
            };

            var guids = AssetDatabase.FindAssets($"t:AnimationClip {characterName}", searchFolders);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                {
                    animationClips.Add(clip);
                }
            }

            if (animationClips.Count == 0)
            {
                // Try broader search
                guids = AssetDatabase.FindAssets("t:AnimationClip", searchFolders);
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (clip != null && (clip.name.Contains(characterName) || clip.name.Contains(characterClass.ToString())))
                    {
                        animationClips.Add(clip);
                    }
                }
            }

            AddLog($"Found {animationClips.Count} animation clips");
        }

        private AnimationClip GetClipByName(string name)
        {
            return animationClips.FirstOrDefault(c => c != null && c.name.ToLower().Contains(name.ToLower()));
        }

        private AnimationClip GetClipByNameAndDirection(string name, string direction)
        {
            return animationClips.FirstOrDefault(c =>
                c != null &&
                c.name.ToLower().Contains(name.ToLower()) &&
                c.name.Contains(direction)
            );
        }

        private void ApplyControllerToPrefab()
        {
            if (characterPrefab == null || generatedController == null)
            {
                EditorUtility.DisplayDialog("Error", "No prefab or controller to apply!", "OK");
                return;
            }

            var animator = characterPrefab.GetComponent<Animator>();
            if (animator == null)
            {
                animator = characterPrefab.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = generatedController;

            EditorUtility.SetDirty(characterPrefab);
            AssetDatabase.SaveAssets();

            AddLog($"Applied controller to {characterPrefab.name}");
        }

        private void SaveController()
        {
            if (generatedController == null)
            {
                EditorUtility.DisplayDialog("Error", "No controller to save!", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Animator Controller",
                $"{characterName}_Controller",
                "controller",
                "Save the animator controller"
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(generatedController), path);
                AssetDatabase.SaveAssets();
                AddLog($"Saved controller to {path}");
            }
        }

        private void AddLog(string message)
        {
            generationLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            Debug.Log($"[Animation Generator] {message}");
        }

        #endregion
    }

    #region Enums

    public enum DofusClass
    {
        Feca,
        Osamodas,
        Enutrof,
        Sram,
        Xelor,
        Ecaflip,
        Eniripsa,
        Iop,
        Cra,
        Sadida,
        Sacrier,
        Pandawa,
        Roublard,
        Zobal,
        Steamer,
        Eliotrope,
        Huppermage,
        Ouginak
    }

    #endregion
}