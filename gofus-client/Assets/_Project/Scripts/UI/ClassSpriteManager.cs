using System.Collections.Generic;
using UnityEngine;

namespace GOFUS.UI
{
    /// <summary>
    /// Manages loading and caching of character class sprites from Resources
    /// </summary>
    public class ClassSpriteManager : MonoBehaviour
    {
        private static ClassSpriteManager _instance;
        public static ClassSpriteManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("ClassSpriteManager");
                    _instance = go.AddComponent<ClassSpriteManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private Dictionary<int, Sprite> classSprites = new Dictionary<int, Sprite>();
        private Dictionary<int, Sprite> classIcons = new Dictionary<int, Sprite>();
        private bool isLoaded = false;

        // Class ID to Name mapping
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

        // Class descriptions
        private readonly Dictionary<int, string> classDescriptions = new Dictionary<int, string>
        {
            { 1, "Masters of protection and defensive magic" },
            { 2, "Beast masters who summon creatures" },
            { 3, "Treasure hunters with earth magic" },
            { 4, "Deadly assassins with traps" },
            { 5, "Time mages who manipulate AP" },
            { 6, "Gamblers relying on luck" },
            { 7, "Powerful healers and support" },
            { 8, "Fearless melee warriors" },
            { 9, "Expert archers with precision" },
            { 10, "Nature mages commanding plants" },
            { 11, "Berserkers gaining power from pain" },
            { 12, "Drunken brawlers" }
        };

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Load all class sprites from Resources folder
        /// </summary>
        public void LoadClassSprites()
        {
            if (isLoaded)
            {
                Debug.Log("[ClassSpriteManager] Sprites already loaded");
                return;
            }

            Debug.Log("[ClassSpriteManager] Loading class sprites from Resources...");

            // Check if we need to generate placeholders
            bool needsPlaceholders = false;
            if (!GOFUS.Utilities.PlaceholderAssetGenerator.PlaceholdersExist())
            {
                Debug.LogWarning("[ClassSpriteManager] No sprites found in Resources. Generating placeholders...");
                GOFUS.Utilities.PlaceholderAssetGenerator.GenerateAllPlaceholders();
                needsPlaceholders = true;
            }

            int loadedCount = 0;
            int iconCount = 0;
            int placeholderCount = 0;

            // Load sprites for each class
            for (int classId = 1; classId <= 12; classId++)
            {
                string className = GetClassName(classId);

                // Try to load character sprite
                string spritePath = $"Sprites/Classes/Class_{classId:D2}_{className}_Male";
                Sprite sprite = Resources.Load<Sprite>(spritePath);

                if (sprite != null)
                {
                    classSprites[classId] = sprite;
                    loadedCount++;
                    Debug.Log($"[ClassSpriteManager] Loaded sprite for {className} from {spritePath}");
                }
                else
                {
                    // Try alternative path
                    spritePath = $"Sprites/Classes/{className}/idle";
                    sprite = Resources.Load<Sprite>(spritePath);

                    if (sprite != null)
                    {
                        classSprites[classId] = sprite;
                        loadedCount++;
                        Debug.Log($"[ClassSpriteManager] Loaded sprite for {className} from {spritePath}");
                    }
                    else
                    {
                        // Generate placeholder if no sprite found
                        Debug.LogWarning($"[ClassSpriteManager] Could not load sprite for class {classId} ({className}). Using placeholder.");
                        sprite = GOFUS.Utilities.PlaceholderAssetGenerator.GeneratePlaceholderSprite(classId);
                        if (sprite != null)
                        {
                            classSprites[classId] = sprite;
                            placeholderCount++;
                            Debug.Log($"[ClassSpriteManager] Generated placeholder sprite for {className}");
                        }
                    }
                }

                // Try to load class icon
                string iconPath = $"Sprites/Classes/Icons/{className}_Icon";
                Sprite icon = Resources.Load<Sprite>(iconPath);

                if (icon != null)
                {
                    classIcons[classId] = icon;
                    iconCount++;
                    Debug.Log($"[ClassSpriteManager] Loaded icon for {className}");
                }
                else
                {
                    // Generate placeholder icon if not found
                    icon = GOFUS.Utilities.PlaceholderAssetGenerator.GeneratePlaceholderSprite(classId, 32, 32);
                    if (icon != null)
                    {
                        classIcons[classId] = icon;
                        iconCount++;
                        Debug.Log($"[ClassSpriteManager] Generated placeholder icon for {className}");
                    }
                    else if (classSprites.ContainsKey(classId))
                    {
                        // Use main sprite as icon if no icon found
                        classIcons[classId] = classSprites[classId];
                        iconCount++;
                    }
                }
            }

            isLoaded = true;
            Debug.Log($"[ClassSpriteManager] Loading complete. Loaded {loadedCount} sprites, {iconCount} icons, and {placeholderCount} placeholders");

            if (needsPlaceholders)
            {
                Debug.LogWarning("[ClassSpriteManager] Using placeholder sprites. To use real assets, follow the asset extraction guide.");
            }
        }

        /// <summary>
        /// Get sprite for a specific class ID
        /// </summary>
        public Sprite GetClassSprite(int classId)
        {
            if (!isLoaded)
            {
                LoadClassSprites();
            }

            if (classSprites.TryGetValue(classId, out Sprite sprite))
            {
                return sprite;
            }

            Debug.LogWarning($"[ClassSpriteManager] No sprite found for class ID {classId}");

            // Return a default sprite if available
            Sprite defaultSprite = Resources.Load<Sprite>("Sprites/UI/default_character");
            return defaultSprite;
        }

        /// <summary>
        /// Get icon for a specific class ID
        /// </summary>
        public Sprite GetClassIcon(int classId)
        {
            if (!isLoaded)
            {
                LoadClassSprites();
            }

            if (classIcons.TryGetValue(classId, out Sprite icon))
            {
                return icon;
            }

            // Fallback to main sprite
            return GetClassSprite(classId);
        }

        /// <summary>
        /// Get class name from ID
        /// </summary>
        public string GetClassName(int classId)
        {
            if (classNames.TryGetValue(classId, out string name))
            {
                return name;
            }
            return "Unknown";
        }

        /// <summary>
        /// Get class description from ID
        /// </summary>
        public string GetClassDescription(int classId)
        {
            if (classDescriptions.TryGetValue(classId, out string desc))
            {
                return desc;
            }
            return "No description available";
        }

        /// <summary>
        /// Get class ID from name
        /// </summary>
        public int GetClassIdFromName(string className)
        {
            foreach (var kvp in classNames)
            {
                if (kvp.Value.Equals(className, System.StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Key;
                }
            }
            return 0; // Invalid ID
        }

        /// <summary>
        /// Check if sprites are loaded
        /// </summary>
        public bool IsLoaded => isLoaded;

        /// <summary>
        /// Get total number of loaded sprites
        /// </summary>
        public int LoadedSpriteCount => classSprites.Count;

        /// <summary>
        /// Get total number of loaded icons
        /// </summary>
        public int LoadedIconCount => classIcons.Count;

        /// <summary>
        /// Clear all loaded sprites (for memory management)
        /// </summary>
        public void ClearSprites()
        {
            classSprites.Clear();
            classIcons.Clear();
            isLoaded = false;
            Debug.Log("[ClassSpriteManager] Cleared all sprites");
        }

        /// <summary>
        /// Get all available class IDs
        /// </summary>
        public List<int> GetAllClassIds()
        {
            return new List<int>(classNames.Keys);
        }

        /// <summary>
        /// Get all class names
        /// </summary>
        public List<string> GetAllClassNames()
        {
            return new List<string>(classNames.Values);
        }
    }
}