using UnityEngine;
using System.Collections.Generic;
using GOFUS.Core;

namespace GOFUS.Rendering
{
    /// <summary>
    /// Level of Detail system for 2D sprites in MMORPG context.
    /// Reduces rendering complexity based on camera distance.
    /// Target: 30-50% performance improvement in crowded areas (50+ players).
    /// </summary>
    public class LODManager2D : Singleton<LODManager2D>
    {
        [System.Serializable]
        public class LODLevel
        {
            [Tooltip("Distance from camera at which this LOD applies")]
            public float distance = 10f;

            [Tooltip("Scale multiplier for sprite")]
            [Range(0.1f, 1f)] public float spriteScale = 1f;

            [Tooltip("Animation frame skip (0 = full fps, 1 = half fps, 2 = third fps)")]
            [Range(0, 4)] public int animationFrameSkip = 0;

            [Tooltip("Disable animations entirely")]
            public bool disableAnimations = false;

            [Tooltip("Disable particle effects")]
            public bool disableParticles = false;

            [Tooltip("Cull object completely")]
            public bool cullObject = false;
        }

        [Header("LOD Configuration")]
        [SerializeField] private LODLevel[] lodLevels = new LODLevel[]
        {
            new LODLevel { distance = 15f, spriteScale = 1f, animationFrameSkip = 0 },    // Full quality
            new LODLevel { distance = 25f, spriteScale = 0.9f, animationFrameSkip = 1 },  // Slight reduction
            new LODLevel { distance = 40f, spriteScale = 0.7f, animationFrameSkip = 2 },  // Medium quality
            new LODLevel { distance = 60f, spriteScale = 0.5f, disableAnimations = true, disableParticles = true }, // Low quality
            new LODLevel { distance = 100f, cullObject = true }                            // Culled
        };

        [Header("Settings")]
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private bool enableLOD = true;

        [Header("Statistics")]
        [SerializeField] private int registeredObjects;
        [SerializeField] private int culledObjects;
        [SerializeField] private int lowDetailObjects;
        [SerializeField] private int fullDetailObjects;

        private List<LODObject2D> registeredLODObjects = new List<LODObject2D>();
        private float lastUpdateTime;

        protected override void Awake()
        {
            base.Awake();
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (!enableLOD) return;

            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateAllLODs();
                lastUpdateTime = Time.time;
            }
        }

        private void UpdateAllLODs()
        {
            if (mainCamera == null) return;

            Vector3 cameraPos = mainCamera.transform.position;

            culledObjects = 0;
            lowDetailObjects = 0;
            fullDetailObjects = 0;

            foreach (var obj in registeredLODObjects)
            {
                if (obj == null || !obj.enabled) continue;

                float distance = Vector2.Distance(new Vector2(cameraPos.x, cameraPos.y),
                                                  new Vector2(obj.transform.position.x, obj.transform.position.y));
                LODLevel level = GetLODLevel(distance);
                obj.ApplyLOD(level);

                // Track stats
                if (level.cullObject)
                    culledObjects++;
                else if (level.animationFrameSkip > 1 || level.disableAnimations)
                    lowDetailObjects++;
                else
                    fullDetailObjects++;
            }
        }

        private LODLevel GetLODLevel(float distance)
        {
            foreach (var level in lodLevels)
            {
                if (distance < level.distance)
                {
                    return level;
                }
            }
            return lodLevels[lodLevels.Length - 1];
        }

        /// <summary>
        /// Registers an object for LOD management.
        /// </summary>
        public void Register(LODObject2D obj)
        {
            if (!registeredLODObjects.Contains(obj))
            {
                registeredLODObjects.Add(obj);
                registeredObjects = registeredLODObjects.Count;
            }
        }

        /// <summary>
        /// Unregisters an object from LOD management.
        /// </summary>
        public void Unregister(LODObject2D obj)
        {
            registeredLODObjects.Remove(obj);
            registeredObjects = registeredLODObjects.Count;
        }

        /// <summary>
        /// Sets how often LOD updates occur.
        /// Lower values = more responsive but higher CPU usage.
        /// </summary>
        public void SetUpdateInterval(float interval)
        {
            updateInterval = Mathf.Max(0.1f, interval);
        }

        /// <summary>
        /// Forces immediate LOD update for all objects.
        /// </summary>
        public void ForceUpdate()
        {
            UpdateAllLODs();
        }

        /// <summary>
        /// Gets current LOD statistics.
        /// </summary>
        public LODStats GetStats()
        {
            return new LODStats
            {
                totalObjects = registeredObjects,
                culledObjects = culledObjects,
                lowDetailObjects = lowDetailObjects,
                fullDetailObjects = fullDetailObjects
            };
        }
    }

    /// <summary>
    /// Component to attach to GameObjects that should use LOD.
    /// Automatically registers/unregisters with LODManager2D.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class LODObject2D : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private ParticleSystem[] particles;

        [Header("LOD Settings")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private bool preserveAspectRatio = true;

        [Header("Current State (Read-Only)")]
        [SerializeField] private float currentDistance;
        [SerializeField] private int currentLODLevel;
        [SerializeField] private bool isCulled;

        private Vector3 originalScale;
        private int frameSkipCounter = 0;
        private bool wasAnimatorEnabled;

        private void Awake()
        {
            InitializeComponents();
            originalScale = transform.localScale;
        }

        private void InitializeComponents()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (animator == null)
                animator = GetComponent<Animator>();

            if (particles == null || particles.Length == 0)
                particles = GetComponentsInChildren<ParticleSystem>();

            if (animator != null)
                wasAnimatorEnabled = animator.enabled;
        }

        private void OnEnable()
        {
            if (enableLOD)
            {
                LODManager2D.Instance?.Register(this);
            }
        }

        private void OnDisable()
        {
            LODManager2D.Instance?.Unregister(this);
        }

        /// <summary>
        /// Applies LOD level to this object.
        /// Called by LODManager2D.
        /// </summary>
        public void ApplyLOD(LODManager2D.LODLevel level)
        {
            if (!enableLOD) return;

            // Culling
            if (level.cullObject)
            {
                isCulled = true;
                if (spriteRenderer != null)
                    spriteRenderer.enabled = false;
                if (animator != null)
                    animator.enabled = false;
                DisableParticles();
                return;
            }
            else
            {
                isCulled = false;
                if (spriteRenderer != null)
                    spriteRenderer.enabled = true;
            }

            // Scale adjustment
            if (preserveAspectRatio)
            {
                transform.localScale = originalScale * level.spriteScale;
            }

            // Animation control
            if (animator != null)
            {
                if (level.disableAnimations)
                {
                    animator.enabled = false;
                }
                else
                {
                    animator.enabled = wasAnimatorEnabled;

                    // Frame skip implementation
                    if (level.animationFrameSkip > 0)
                    {
                        frameSkipCounter++;
                        if (frameSkipCounter % (level.animationFrameSkip + 1) == 0)
                        {
                            animator.Update(Time.deltaTime * (level.animationFrameSkip + 1));
                            frameSkipCounter = 0;
                        }
                    }
                }
            }

            // Particle control
            if (level.disableParticles)
            {
                DisableParticles();
            }
            else
            {
                EnableParticles();
            }
        }

        private void DisableParticles()
        {
            if (particles != null)
            {
                foreach (var ps in particles)
                {
                    if (ps != null && ps.isPlaying)
                    {
                        ps.Stop();
                    }
                }
            }
        }

        private void EnableParticles()
        {
            if (particles != null)
            {
                foreach (var ps in particles)
                {
                    if (ps != null && !ps.isPlaying)
                    {
                        ps.Play();
                    }
                }
            }
        }

        /// <summary>
        /// Updates distance to camera (for debugging).
        /// </summary>
        public void UpdateDistance(float distance)
        {
            currentDistance = distance;
        }

        private void OnValidate()
        {
            InitializeComponents();
        }
    }

    /// <summary>
    /// LOD statistics data.
    /// </summary>
    [System.Serializable]
    public struct LODStats
    {
        public int totalObjects;
        public int culledObjects;
        public int lowDetailObjects;
        public int fullDetailObjects;

        public override string ToString()
        {
            return $"Total: {totalObjects}, Culled: {culledObjects}, Low: {lowDetailObjects}, Full: {fullDetailObjects}";
        }
    }
}
