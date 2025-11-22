using System;
using System.Collections.Generic;
using UnityEngine;
using GOFUS.Core;
using GOFUS.Rendering;

namespace GOFUS.Player
{
    /// <summary>
    /// Handles player character animations for 8-directional isometric movement
    /// Supports idle, walk, run, attack, and spell casting animations
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float animationSpeed = 1f;
        [SerializeField] private bool useSpritesheetAnimation = true;
        [SerializeField] private float frameRate = 12f;

        [Header("Current State")]
        [SerializeField] private PlayerDirection currentDirection = PlayerDirection.South;
        [SerializeField] private AnimationState currentState = AnimationState.Idle;
        [SerializeField] private bool isMoving = false;
        [SerializeField] private bool isAttacking = false;
        [SerializeField] private bool isCasting = false;

        [Header("Sprite References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Dictionary<string, Sprite[]> animationSprites;

        // Integration with CharacterLayerRenderer
        private CharacterLayerRenderer characterRenderer;

        // Animation timing
        private float animationTimer;
        private int currentFrameIndex;
        private string currentAnimationKey;

        // Events
        public event Action OnAttackComplete;
        public event Action OnCastComplete;
        public event Action OnDeathComplete;

        public enum AnimationState
        {
            Idle,
            Walk,
            Run,
            Attack,
            Cast,
            Death,
            Hit,
            Victory
        }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                }
            }

            // Get CharacterLayerRenderer for sprite animation
            characterRenderer = GetComponent<CharacterLayerRenderer>();
            if (characterRenderer == null)
            {
                characterRenderer = GetComponentInChildren<CharacterLayerRenderer>();
            }

            if (characterRenderer != null)
            {
                Debug.Log("[PlayerAnimator] CharacterLayerRenderer found!");
            }
            else
            {
                Debug.LogWarning("[PlayerAnimator] CharacterLayerRenderer not found! Sprite animations won't work.");
            }

            animationSprites = new Dictionary<string, Sprite[]>();
            LoadAnimationSprites();
        }

        private void Start()
        {
            // Set initial animation
            UpdateAnimation();
        }

        private void Update()
        {
            if (useSpritesheetAnimation && animationSprites.Count > 0)
            {
                UpdateSpritesheetAnimation();
            }

            // CharacterLayerRenderer handles sprite updates automatically
        }

        private void LoadAnimationSprites()
        {
            // In production, these would be loaded from Resources or AddressableAssets
            // For now, we'll create placeholder logic

            // Example sprite loading pattern:
            // animationSprites["idle_south"] = Resources.LoadAll<Sprite>("Sprites/Player/Idle/South");
            // animationSprites["walk_south"] = Resources.LoadAll<Sprite>("Sprites/Player/Walk/South");

            Debug.Log("Animation sprites would be loaded here from extracted Flash assets");
        }

        public void SetDirection(PlayerDirection direction)
        {
            if (currentDirection != direction)
            {
                currentDirection = direction;
                Debug.Log($"[PlayerAnimator] SetDirection({direction})");
                UpdateAnimation();
            }
        }

        public void SetMoving(bool moving)
        {
            if (isMoving != moving)
            {
                isMoving = moving;
                currentState = moving ? AnimationState.Walk : AnimationState.Idle;
                Debug.Log($"[PlayerAnimator] SetMoving({moving}) - State changed to {currentState}");
                UpdateAnimation();
            }
        }

        public void SetRunning(bool running)
        {
            if (running && isMoving)
            {
                currentState = AnimationState.Run;
                UpdateAnimation();
            }
        }

        public void PlayAttack(Action onComplete = null)
        {
            if (isAttacking || currentState == AnimationState.Death)
                return;

            isAttacking = true;
            currentState = AnimationState.Attack;
            UpdateAnimation();

            // Use animation event or timer to trigger completion
            float attackDuration = GetAnimationDuration(AnimationState.Attack);
            Invoke(nameof(CompleteAttack), attackDuration);

            if (onComplete != null)
            {
                OnAttackComplete += onComplete;
            }
        }

        private void CompleteAttack()
        {
            isAttacking = false;
            currentState = isMoving ? AnimationState.Walk : AnimationState.Idle;
            UpdateAnimation();

            OnAttackComplete?.Invoke();
            OnAttackComplete = null; // Clear one-time callbacks
        }

        public void PlayCast(float castTime, Action onComplete = null)
        {
            if (isCasting || currentState == AnimationState.Death)
                return;

            isCasting = true;
            currentState = AnimationState.Cast;
            UpdateAnimation();

            // Cast animation loops until cast is complete
            Invoke(nameof(CompleteCast), castTime);

            if (onComplete != null)
            {
                OnCastComplete += onComplete;
            }
        }

        private void CompleteCast()
        {
            isCasting = false;
            currentState = isMoving ? AnimationState.Walk : AnimationState.Idle;
            UpdateAnimation();

            OnCastComplete?.Invoke();
            OnCastComplete = null;
        }

        public void PlayHit()
        {
            if (currentState == AnimationState.Death)
                return;

            // Brief hit animation
            StartCoroutine(PlayTemporaryAnimation(AnimationState.Hit, 0.3f));
        }

        public void PlayDeath()
        {
            currentState = AnimationState.Death;
            UpdateAnimation();

            float deathDuration = GetAnimationDuration(AnimationState.Death);
            Invoke(nameof(CompleteDeathAnimation), deathDuration);
        }

        private void CompleteDeathAnimation()
        {
            OnDeathComplete?.Invoke();
        }

        public void PlayVictory()
        {
            if (currentState == AnimationState.Death)
                return;

            currentState = AnimationState.Victory;
            UpdateAnimation();
        }

        public void ResetAnimation()
        {
            isMoving = false;
            isAttacking = false;
            isCasting = false;
            currentState = AnimationState.Idle;
            UpdateAnimation();
        }

        private void UpdateAnimation()
        {
            // Build animation key based on state and direction
            string animKey = BuildAnimationKey(currentState, currentDirection);

            if (animKey != currentAnimationKey)
            {
                currentAnimationKey = animKey;
                currentFrameIndex = 0;
                animationTimer = 0;
                LoadAnimationFrames(animKey);
            }

            // Apply animation to CharacterLayerRenderer
            if (characterRenderer != null)
            {
                string characterAnimName = ConvertToCharacterAnimation(currentState, currentDirection);
                characterRenderer.SetAnimation(characterAnimName);
                Debug.Log($"[PlayerAnimator] Set CharacterLayerRenderer animation: {characterAnimName}");
            }
            else
            {
                Debug.Log($"[PlayerAnimator] Animation updated: state={currentState}, direction={currentDirection} (no CharacterLayerRenderer)");
            }
        }

        private string BuildAnimationKey(AnimationState state, PlayerDirection direction)
        {
            string stateStr = state.ToString().ToLower();
            string dirStr = GetDirectionString(direction);
            return $"{stateStr}_{dirStr}";
        }

        private string GetDirectionString(PlayerDirection direction)
        {
            switch (direction)
            {
                case PlayerDirection.North: return "north";
                case PlayerDirection.NorthEast: return "northeast";
                case PlayerDirection.East: return "east";
                case PlayerDirection.SouthEast: return "southeast";
                case PlayerDirection.South: return "south";
                case PlayerDirection.SouthWest: return "southwest";
                case PlayerDirection.West: return "west";
                case PlayerDirection.NorthWest: return "northwest";
                default: return "south";
            }
        }

        /// <summary>
        /// Convert PlayerAnimator state to CharacterLayerRenderer animation name
        /// Format: {state}{direction} where state is static/walk/run and direction is S/N/E/W/SE/SW/NE/NW
        /// </summary>
        private string ConvertToCharacterAnimation(AnimationState state, PlayerDirection direction)
        {
            // Map animation state to prefix
            string statePrefix;
            switch (state)
            {
                case AnimationState.Idle:
                    statePrefix = "static";
                    break;
                case AnimationState.Walk:
                    statePrefix = "walk";
                    break;
                case AnimationState.Run:
                    statePrefix = "run";
                    break;
                case AnimationState.Attack:
                case AnimationState.Cast:
                case AnimationState.Death:
                case AnimationState.Hit:
                case AnimationState.Victory:
                    // For now, default to static for unsupported states
                    statePrefix = "static";
                    break;
                default:
                    statePrefix = "static";
                    break;
            }

            // Map direction to suffix
            string directionSuffix;
            switch (direction)
            {
                case PlayerDirection.North:
                    directionSuffix = "N";
                    break;
                case PlayerDirection.NorthEast:
                    directionSuffix = "NE";
                    break;
                case PlayerDirection.East:
                    directionSuffix = "E";
                    break;
                case PlayerDirection.SouthEast:
                    directionSuffix = "SE";
                    break;
                case PlayerDirection.South:
                    directionSuffix = "S";
                    break;
                case PlayerDirection.SouthWest:
                    directionSuffix = "SW";
                    break;
                case PlayerDirection.West:
                    directionSuffix = "W";
                    break;
                case PlayerDirection.NorthWest:
                    directionSuffix = "NW";
                    break;
                default:
                    directionSuffix = "S";
                    break;
            }

            return statePrefix + directionSuffix;
        }

        /// <summary>
        /// Apply visual feedback to show direction and movement state (temporary until sprites are loaded)
        /// </summary>
        private void ApplyVisualFeedback()
        {
            if (spriteRenderer == null)
                return;

            // Color-code by direction for visual feedback
            Color directionColor = GetDirectionColor(currentDirection);

            // Brighten/darken based on movement state
            float intensity = isMoving ? 1.0f : 0.7f;
            spriteRenderer.color = directionColor * intensity;

            // Flip sprite for left-facing directions
            bool shouldFlipX = (currentDirection == PlayerDirection.West ||
                               currentDirection == PlayerDirection.NorthWest ||
                               currentDirection == PlayerDirection.SouthWest);
            spriteRenderer.flipX = shouldFlipX;

            Debug.Log($"[PlayerAnimator] Visual feedback: direction={currentDirection}, color={directionColor}, moving={isMoving}, flipX={shouldFlipX}");
        }

        /// <summary>
        /// Get color for direction (temporary visual indicator)
        /// </summary>
        private Color GetDirectionColor(PlayerDirection direction)
        {
            switch (direction)
            {
                case PlayerDirection.North:      return new Color(1.0f, 0.3f, 0.3f); // Red
                case PlayerDirection.NorthEast:  return new Color(1.0f, 0.7f, 0.3f); // Orange
                case PlayerDirection.East:       return new Color(1.0f, 1.0f, 0.3f); // Yellow
                case PlayerDirection.SouthEast:  return new Color(0.3f, 1.0f, 0.3f); // Green
                case PlayerDirection.South:      return new Color(0.3f, 0.7f, 1.0f); // Blue
                case PlayerDirection.SouthWest:  return new Color(0.7f, 0.3f, 1.0f); // Purple
                case PlayerDirection.West:       return new Color(1.0f, 0.3f, 0.7f); // Pink
                case PlayerDirection.NorthWest:  return new Color(0.3f, 1.0f, 1.0f); // Cyan
                default:                         return Color.white;
            }
        }

        private void LoadAnimationFrames(string animationKey)
        {
            if (animationSprites.ContainsKey(animationKey))
            {
                // Frames are already loaded
                return;
            }

            // In production, load the appropriate sprite frames
            // For testing, we'll use placeholder logic
            Debug.Log($"Loading animation: {animationKey}");
        }

        private void UpdateSpritesheetAnimation()
        {
            if (string.IsNullOrEmpty(currentAnimationKey))
                return;

            if (!animationSprites.TryGetValue(currentAnimationKey, out Sprite[] frames))
                return;

            if (frames == null || frames.Length == 0)
                return;

            // Update animation timer
            animationTimer += Time.deltaTime * animationSpeed;

            // Calculate current frame
            float frameDuration = 1f / frameRate;
            if (animationTimer >= frameDuration)
            {
                animationTimer -= frameDuration;
                currentFrameIndex++;

                // Loop or stop based on animation type
                if (ShouldLoopAnimation(currentState))
                {
                    currentFrameIndex %= frames.Length;
                }
                else if (currentFrameIndex >= frames.Length)
                {
                    currentFrameIndex = frames.Length - 1;
                }

                // Update sprite
                if (spriteRenderer != null && currentFrameIndex < frames.Length)
                {
                    spriteRenderer.sprite = frames[currentFrameIndex];
                }
            }
        }

        private bool ShouldLoopAnimation(AnimationState state)
        {
            switch (state)
            {
                case AnimationState.Idle:
                case AnimationState.Walk:
                case AnimationState.Run:
                case AnimationState.Cast:
                    return true;
                case AnimationState.Attack:
                case AnimationState.Death:
                case AnimationState.Hit:
                case AnimationState.Victory:
                    return false;
                default:
                    return true;
            }
        }

        private float GetAnimationDuration(AnimationState state)
        {
            // Default durations for different animations
            switch (state)
            {
                case AnimationState.Attack:
                    return 0.5f;
                case AnimationState.Death:
                    return 1.0f;
                case AnimationState.Hit:
                    return 0.3f;
                case AnimationState.Victory:
                    return 2.0f;
                default:
                    return 1.0f;
            }
        }

        private System.Collections.IEnumerator PlayTemporaryAnimation(AnimationState tempState, float duration)
        {
            var previousState = currentState;
            currentState = tempState;
            UpdateAnimation();

            yield return new WaitForSeconds(duration);

            if (currentState == tempState) // Only revert if still in temp state
            {
                currentState = previousState;
                UpdateAnimation();
            }
        }

        public void SetAnimationSpeed(float speed)
        {
            animationSpeed = Mathf.Clamp(speed, 0.1f, 3f);
        }

        public void SetFrameRate(float fps)
        {
            frameRate = Mathf.Clamp(fps, 1f, 60f);
        }

        // Helper method for visual debugging
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
                return;

            // Draw current direction
            Vector3 directionVector = GetWorldDirection(currentDirection);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, directionVector * 2f);

            // Show current animation state
            Vector3 textPos = transform.position + Vector3.up * 1.5f;
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(textPos, $"State: {currentState}\nDir: {currentDirection}");
            #endif
        }

        private Vector3 GetWorldDirection(PlayerDirection direction)
        {
            switch (direction)
            {
                case PlayerDirection.North: return new Vector3(0, 1, 0);
                case PlayerDirection.NorthEast: return new Vector3(1, 1, 0).normalized;
                case PlayerDirection.East: return new Vector3(1, 0, 0);
                case PlayerDirection.SouthEast: return new Vector3(1, -1, 0).normalized;
                case PlayerDirection.South: return new Vector3(0, -1, 0);
                case PlayerDirection.SouthWest: return new Vector3(-1, -1, 0).normalized;
                case PlayerDirection.West: return new Vector3(-1, 0, 0);
                case PlayerDirection.NorthWest: return new Vector3(-1, 1, 0).normalized;
                default: return Vector3.down;
            }
        }
    }
}