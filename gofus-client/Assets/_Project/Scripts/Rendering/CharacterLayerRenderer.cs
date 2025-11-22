using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace GOFUS.Rendering
{
    /// <summary>
    /// Handles rendering of Dofus characters using a layered sprite composition system.
    /// Characters are built from multiple sprite layers (shapes) that are combined.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterLayerRenderer : MonoBehaviour
    {
        [Header("Character Configuration")]
        [SerializeField] private int classId = 1; // Feca by default
        [SerializeField] private bool isMale = true;
        [SerializeField] private string currentAnimation = "staticS"; // Default static south

        [Header("Layer Settings")]
        [SerializeField] private int sortingOrder = 0;
        [SerializeField] private string sortingLayerName = "Characters";

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Layer management
        private List<SpriteRenderer> spriteLayers = new List<SpriteRenderer>();
        private Dictionary<string, List<Sprite>> animationSprites = new Dictionary<string, List<Sprite>>();
        
        // Direction mapping and mirroring
        private bool currentShouldFlip = false; // Should current animation be flipped horizontally

        // Class ID to sprite ID mapping (male sprites)
        private readonly Dictionary<int, int> classSpriteIds = new Dictionary<int, int>
        {
            { 1, 10 },   // Feca
            { 2, 20 },   // Osamodas
            { 3, 30 },   // Enutrof
            { 4, 40 },   // Sram
            { 5, 50 },   // Xelor
            { 6, 60 },   // Ecaflip
            { 7, 70 },   // Eniripsa
            { 8, 80 },   // Iop
            { 9, 90 },   // Cra
            { 10, 100 }, // Sadida
            { 11, 110 }, // Sacrieur
            { 12, 120 }  // Pandawa
        };

        // Class names
        private readonly Dictionary<int, string> classNames = new Dictionary<int, string>
        {
            { 1, "Feca" },
            { 2, "Osamodas" },
            { 3, "Enutrof" },
            { 4, "Sram" },
            { 5, "Xelor" },
            { 6, "Ecaflip" },
            { 7, "Eniripsa" },
            { 8, "Iop" },
            { 9, "Cra" },
            { 10, "Sadida" },
            { 11, "Sacrieur" },
            { 12, "Pandawa" }
        };

        private void Start()
        {
            InitializeCharacter();
        }

        /// <summary>
        /// Initialize the character by loading all sprite layers
        /// </summary>
        public void InitializeCharacter()
        {
            if (showDebugInfo)
                Debug.Log($"[CharacterLayerRenderer] Initializing character - Class: {GetClassName()}, Male: {isMale}");

            LoadCharacterSprites();
            SetupSpriteLayers();
        }

        /// <summary>
        /// Load all sprite layers for the current character class
        /// </summary>
        private void LoadCharacterSprites()
        {
            string className = GetClassName();
            string basePath = $"Sprites/Classes/{className}/sprites";

            if (showDebugInfo)
                Debug.Log($"[CharacterLayerRenderer] Loading sprites from: {basePath} for animation: {currentAnimation}");

            List<Sprite> animSprites = new List<Sprite>();

            // Parse animation into state and direction (e.g., "walkN" → "walk" + "N")
            string animState = "";
            string unityDirection = "";
            
            // Extract state and direction from currentAnimation
            if (currentAnimation.StartsWith("static"))
            {
                animState = "static";
                unityDirection = currentAnimation.Substring(6); // Remove "static"
            }
            else if (currentAnimation.StartsWith("walk"))
            {
                animState = "walk";
                unityDirection = currentAnimation.Substring(4); // Remove "walk"
            }
            else if (currentAnimation.StartsWith("run"))
            {
                animState = "run";
                unityDirection = currentAnimation.Substring(3); // Remove "run"
            }
            else
            {
                // Fallback for unknown animation types
                animState = currentAnimation;
                unityDirection = "S";
            }

            // Map Unity direction to Dofus direction
            var (dofusDirection, shouldFlip) = MapUnityDirectionToDofus(unityDirection);
            currentShouldFlip = shouldFlip;
            
            // Build Dofus animation name (e.g., "walkS", "staticR")
            string dofusAnimation = animState + dofusDirection;
            
            if (showDebugInfo)
                Debug.Log($"[CharacterLayerRenderer] Mapped {currentAnimation} → {dofusAnimation} (flip: {shouldFlip})");

            // Build file system path to find animation folders
            string assetsPath = Application.dataPath;
            string fullSpritePath = Path.Combine(assetsPath, "_Project", "Resources", "Sprites", "Classes", className, "sprites");

            // Check if the sprites directory exists
            if (Directory.Exists(fullSpritePath))
            {
                // Find all folders matching the animation pattern
                string[] allFolders = Directory.GetDirectories(fullSpritePath);
                int foldersFound = 0;

                foreach (string folder in allFolders)
                {
                    string folderName = Path.GetFileName(folder);

                    // Check if folder name ends with the Dofus animation name (e.g., "DefineSprite_59_walkS")
                    if (folderName.EndsWith("_" + dofusAnimation))
                    {
                        // Build Resources path (relative to Resources folder)
                        string resourcePath = $"Sprites/Classes/{className}/sprites/{folderName}";

                        if (showDebugInfo)
                            Debug.Log($"[CharacterLayerRenderer] Loading from folder: {resourcePath}");

                        // Load all sprites from this specific animation folder
                        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);

                        if (sprites != null && sprites.Length > 0)
                        {
                            animSprites.AddRange(sprites);
                            foldersFound++;

                            if (showDebugInfo)
                                Debug.Log($"[CharacterLayerRenderer] Loaded {sprites.Length} sprite(s) from {folderName}");
                        }
                    }
                }

                if (showDebugInfo)
                    Debug.Log($"[CharacterLayerRenderer] Found {foldersFound} folders matching animation '{currentAnimation}'");
            }
            else
            {
                Debug.LogWarning($"[CharacterLayerRenderer] Directory does not exist: {fullSpritePath}");
            }

            // Fallback: load generic sprites if no animation-specific sprites found
            if (animSprites.Count == 0)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"[CharacterLayerRenderer] No animation-specific sprites found for: {currentAnimation}");
                    Debug.LogWarning($"[CharacterLayerRenderer] Loading first 10 generic sprites as fallback");
                }

                // Load all sprites and take first 10 as fallback
                Sprite[] allSprites = Resources.LoadAll<Sprite>(basePath);
                if (allSprites != null && allSprites.Length > 0)
                {
                    animSprites = allSprites.Take(10).ToList();
                }
                else
                {
                    Debug.LogError($"[CharacterLayerRenderer] No sprites found at all in {basePath}");
                }
            }

            // Store the loaded sprites
            animationSprites[currentAnimation] = animSprites;

            if (showDebugInfo)
                Debug.Log($"[CharacterLayerRenderer] Total sprites loaded for animation '{currentAnimation}': {animSprites.Count}");
        }

        /// <summary>
        /// Setup individual sprite renderer layers for composition
        /// </summary>
        private void SetupSpriteLayers()
        {
            // Clear existing layers - use DestroyImmediate to prevent overlap
            foreach (var layer in spriteLayers)
            {
                if (layer != null && layer.gameObject != this.gameObject)
                {
                    DestroyImmediate(layer.gameObject); // Destroy immediately
                }
            }
            spriteLayers.Clear();

            // Get sprites for current animation
            if (!animationSprites.ContainsKey(currentAnimation))
            {
                Debug.LogWarning($"[CharacterLayerRenderer] No sprites loaded for animation: {currentAnimation}");
                return;
            }

            List<Sprite> sprites = animationSprites[currentAnimation];

            if (showDebugInfo)
                Debug.Log($"[CharacterLayerRenderer] Setting up {sprites.Count} sprite layers");

            // CRITICAL: Sprites have CENTER pivot (0.5, 0.5), so we need to offset them DOWN
            // Find the maximum sprite height to calculate the offset
            float maxSpriteHeight = 0f;
            foreach (var sprite in sprites)
            {
                if (sprite != null)
                {
                    float height = sprite.bounds.size.y;
                    if (height > maxSpriteHeight)
                        maxSpriteHeight = height;
                }
            }
            
            // Offset ALL layers down by half the max height
            // This positions the sprite BOTTOM (feet) at the parent GameObject position
            Vector3 layerOffset = new Vector3(0, -maxSpriteHeight * 0.5f, 0);
            
            if (showDebugInfo)
                Debug.Log($"[CharacterLayerRenderer] Max sprite height: {maxSpriteHeight}, layer offset: {layerOffset.y}");
            
            // Create sprite layers - ALL at the SAME offset
            for (int i = 0; i < sprites.Count; i++)
            {
                GameObject layerObj = new GameObject($"Layer_{i}");
                layerObj.transform.SetParent(transform);
                layerObj.transform.localPosition = layerOffset; // Offset to position feet at parent
                layerObj.transform.localScale = Vector3.one;

                SpriteRenderer sr = layerObj.AddComponent<SpriteRenderer>();
                sr.sprite = sprites[i];
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = sortingOrder + i; // Each layer stacks on top
                sr.flipX = currentShouldFlip; // Apply horizontal flip for mirrored directions

                spriteLayers.Add(sr);
                
                // Debug first layer
                if (showDebugInfo && i == 0 && sprites[i] != null)
                {
                    Sprite sprite = sprites[i];
                    Vector2 pivot = sprite.pivot;
                    Rect rect = sprite.rect;
                    Vector2 pivotNormalized = new Vector2(pivot.x / rect.width, pivot.y / rect.height);
                    
                    Debug.Log($"[CharacterLayerRenderer] First sprite info:");
                    Debug.Log($"  - Size: {rect.width}x{rect.height}");
                    Debug.Log($"  - Pivot (normalized): {pivotNormalized}");
                    Debug.Log($"  - Bounds height: {sprite.bounds.size.y}");
                    Debug.Log($"  - Layer offset applied: {layerOffset.y}");
                    Debug.Log($"  - Total layers: {sprites.Count}");
                }
            }

            if (showDebugInfo)
                Debug.Log($"[CharacterLayerRenderer] Created {spriteLayers.Count} sprite layers");
        }

        /// <summary>
        /// Maps Unity 8-direction naming (N, NE, E, SE, S, SW, W, NW) to Dofus 5-direction naming (F, R, L, S, B)
        /// Returns the Dofus direction suffix and whether the sprite should be flipped horizontally
        /// </summary>
        /// <param name="unityDirection">Unity direction suffix (N, NE, E, SE, S, SW, W, NW)</param>
        /// <returns>Tuple of (dofusDirection, shouldFlip)</returns>
        private (string dofusDirection, bool shouldFlip) MapUnityDirectionToDofus(string unityDirection)
        {
            // Dofus directions (based on actual sprite testing with Xelor):
            // F = Front of character visible (toward camera) = Unity S
            // B = Back of character visible (away from camera) = Unity N
            // R = Right side diagonal = Unity SE
            // L = Left side diagonal = Unity NW
            // S = Side view (lateral) = Unity E/W (with flip for W)

            switch (unityDirection)
            {
                case "N":   // North (up, away from camera) → Back visible
                    return ("B", false);

                case "NE":  // Northeast → Left diagonal flipped (symmetrical to NW)
                    return ("L", true);

                case "E":   // East (right) → Side view (lateral right)
                    return ("S", false);

                case "SE":  // Southeast → Right diagonal lower
                    return ("R", false);

                case "S":   // South (down, toward camera) → Front visible
                    return ("F", false);

                case "SW":  // Southwest → Right diagonal flipped (symmetrical to SE)
                    return ("R", true);

                case "W":   // West (left) → Side view flipped (lateral left)
                    return ("S", true);

                case "NW":  // Northwest → Left diagonal upper
                    return ("L", false);

                default:
                    Debug.LogWarning($"[CharacterLayerRenderer] Unknown Unity direction: {unityDirection}, defaulting to S");
                    return ("S", false);
            }
        }

        /// <summary>
        /// Change the character's animation
        /// </summary>
        public void SetAnimation(string animationName)
        {
            if (currentAnimation == animationName)
                return;

            currentAnimation = animationName;

            if (showDebugInfo)
                Debug.Log($"[CharacterLayerRenderer] Changing animation to: {animationName}");

            LoadCharacterSprites();
            SetupSpriteLayers();
        }

        /// <summary>
        /// Set the character class
        /// </summary>
        public void SetClass(int newClassId, bool male = true)
        {
            if (classId == newClassId && isMale == male)
                return;

            classId = newClassId;
            isMale = male;

            if (showDebugInfo)
                Debug.Log($"[CharacterLayerRenderer] Changing class to: {GetClassName()} ({(male ? "Male" : "Female")})");

            // Reload character
            animationSprites.Clear();
            InitializeCharacter();
        }

        // Note: Animation is handled by loading different sprite sets for each animation state
        // No Update() method needed since we're swapping entire sprite compositions, not animating frames

        /// <summary>
        /// Get the class name for the current class ID
        /// </summary>
        private string GetClassName()
        {
            if (classNames.TryGetValue(classId, out string name))
                return name;
            return "Feca"; // Default
        }

        /// <summary>
        /// Get the sprite ID for the current class and gender
        /// </summary>
        private int GetSpriteId()
        {
            if (!classSpriteIds.TryGetValue(classId, out int spriteId))
                spriteId = 10; // Default to Feca

            // Female sprites are +1 (e.g., 10 = male Feca, 11 = female Feca)
            if (!isMale)
                spriteId += 1;

            return spriteId;
        }

        /// <summary>
        /// Set the sorting order for all layers
        /// </summary>
        public void SetSortingOrder(int order)
        {
            sortingOrder = order;
            for (int i = 0; i < spriteLayers.Count; i++)
            {
                if (spriteLayers[i] != null)
                    spriteLayers[i].sortingOrder = sortingOrder + i;
            }
        }

        /// <summary>
        /// Set the sorting layer for all layers
        /// </summary>
        public void SetSortingLayer(string layerName)
        {
            sortingLayerName = layerName;
            foreach (var layer in spriteLayers)
            {
                if (layer != null)
                    layer.sortingLayerName = layerName;
            }
        }

        /// <summary>
        /// Get current class ID
        /// </summary>
        public int ClassId => classId;

        /// <summary>
        /// Get current gender
        /// </summary>
        public bool IsMale => isMale;

        /// <summary>
        /// Get current animation name
        /// </summary>
        public string CurrentAnimation => currentAnimation;

        /// <summary>
        /// Get total number of layers
        /// </summary>
        public int LayerCount => spriteLayers.Count;

        /// <summary>
        /// Cleanup
        /// </summary>
        private void OnDestroy()
        {
            foreach (var layer in spriteLayers)
            {
                if (layer != null && layer.gameObject != this.gameObject)
                    Destroy(layer.gameObject);
            }
            spriteLayers.Clear();
            animationSprites.Clear();
        }
    }
}
