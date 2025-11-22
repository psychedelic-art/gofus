using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GOFUS.Combat;
using GOFUS.Map;
using GOFUS.Entities;

namespace GOFUS.UI.Screens
{
    /// <summary>
    /// Complete Game HUD implementation with seamless map transitions
    /// </summary>
    public class GameHUD : UIScreen
    {
        #region Properties

        // Health & Mana
        public int CurrentHealth { get; private set; } = 100;
        public int MaxHealth { get; private set; } = 100;
        public int CurrentMana { get; private set; } = 50;
        public int MaxMana { get; private set; } = 50;
        public float HealthPercentage => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0;
        public float ManaPercentage => MaxMana > 0 ? (float)CurrentMana / MaxMana : 0;

        // Action & Movement Points
        public int CurrentAP { get; private set; } = 6;
        public int MaxAP { get; private set; } = 6;
        public int CurrentMP { get; private set; } = 3;
        public int MaxMP { get; private set; } = 3;

        // Experience
        public int Level { get; private set; } = 1;
        public int CurrentExp { get; private set; }
        public int RequiredExp { get; private set; } = 1000;
        public float ExpPercentage => RequiredExp > 0 ? (float)CurrentExp / RequiredExp : 0;

        // Minimap
        public bool MinimapVisible { get; private set; } = true;
        public Vector2 PlayerPositionOnMinimap { get; private set; }
        public List<MinimapEntity> MinimapEntities { get; private set; } = new List<MinimapEntity>();
        public Rect CurrentMapBounds { get; private set; }

        // Map Transitions
        public int CurrentMapId { get; private set; }
        public int CurrentCellId { get; private set; }
        public int PreloadingMapId { get; private set; }
        public Dictionary<MapEdge, int> AdjacentMaps { get; private set; } = new Dictionary<MapEdge, int>();
        private Vector2 playerVelocity;
        private const float EDGE_DETECTION_DISTANCE = 10f;

        // Map Rendering
        private MapRenderer mapRenderer;
        private GameObject characterSprite;
        private GOFUS.Rendering.CharacterLayerRenderer characterRenderer;
        private CameraController cameraController;
        private GOFUS.Player.PlayerController playerController;

        // Character data for sprite rendering
        private int characterClassId = 1; // Default to Feca
        private bool characterIsMale = true; // Default to male
        private string characterName = "Player";

        // Combat Mode
        public CombatMode? CurrentCombatMode { get; private set; } = null;
        public string CombatModeText { get; private set; } = "Exploration";
        public float ATBProgress { get; private set; }
        public bool IsATBVisible => CurrentCombatMode == CombatMode.RealTime;

        // Quick Actions
        public List<QuickSkill> QuickSkills { get; private set; } = new List<QuickSkill>();
        private Dictionary<int, float> skillCooldowns = new Dictionary<int, float>();

        // Status Effects
        public List<StatusEffect> ActiveBuffs { get; private set; } = new List<StatusEffect>();
        public List<StatusEffect> ActiveDebuffs { get; private set; } = new List<StatusEffect>();

        // Notifications
        public List<HUDNotification> ActiveNotifications { get; private set; } = new List<HUDNotification>();

        // Custom Resources
        public List<ResourceBar> CustomResources { get; private set; } = new List<ResourceBar>();

        #endregion

        #region Events

        public event Action<int> OnMapTransition;
        public event Action<MapEdge> OnApproachingEdge;
        public event Action<int> OnLevelUp;

        #endregion

        #region UI Elements

        private Image healthBar;
        private Image manaBar;
        private Image expBar;
        private TextMeshProUGUI healthText;
        private TextMeshProUGUI manaText;
        private TextMeshProUGUI levelText;
        private TextMeshProUGUI combatModeText;
        private GameObject minimap;
        private Transform buffContainer;
        private Transform debuffContainer;
        private Transform notificationContainer;
        private GameObject atbGauge;
        private Image atbFill;

        #endregion

        #region Initialization

        public override void Initialize()
        {
            base.Initialize();
            CreateHUDElements();
            SetupMinimap();
            SetupQuickActionBar();
            SetupStatusEffectBars();
            SetupMapRenderer();
            SetupCamera();
            SetupInputManager();
        }

        private void SetupInputManager()
        {
            // Add MapInputHandler to handle clicks that might be blocked by UI
            var inputHandler = FindObjectOfType<GOFUS.Core.MapInputHandler>();
            if (inputHandler == null)
            {
                GameObject inputObj = new GameObject("MapInputHandler");
                inputHandler = inputObj.AddComponent<GOFUS.Core.MapInputHandler>();
                Debug.Log("[GameHUD] MapInputHandler created to handle manual click detection");
            }
            else
            {
                Debug.Log("[GameHUD] MapInputHandler already exists in scene");
            }
        }

        private void CreateHUDElements()
        {
            // Create main HUD container
            GameObject hudContainer = new GameObject("HUD_Container");
            hudContainer.transform.SetParent(transform, false);

            // Add RectTransform and anchor to top-left
            RectTransform hudRect = hudContainer.AddComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0, 1); // Top-left anchor
            hudRect.anchorMax = new Vector2(0, 1); // Top-left anchor
            hudRect.pivot = new Vector2(0, 1); // Top-left pivot
            hudRect.anchoredPosition = Vector2.zero; // Position at anchor
            hudRect.sizeDelta = new Vector2(300, 200); // Size for container

            // Health Bar
            CreateHealthBar(hudContainer.transform);

            // Mana Bar
            CreateManaBar(hudContainer.transform);

            // Experience Bar
            CreateExpBar(hudContainer.transform);

            // Combat Mode Indicator
            CreateCombatIndicator(hudContainer.transform);

            // AP/MP Display
            CreateAPMPDisplay(hudContainer.transform);
        }

        private void CreateHealthBar(Transform parent)
        {
            GameObject healthObj = CreateBar("HealthBar", parent, new Vector2(10, -10), Color.red);
            healthBar = healthObj.transform.Find("Fill").GetComponent<Image>();
            healthText = CreateBarText(healthObj.transform, "100/100");
        }

        private void CreateManaBar(Transform parent)
        {
            GameObject manaObj = CreateBar("ManaBar", parent, new Vector2(10, -40), Color.blue);
            manaBar = manaObj.transform.Find("Fill").GetComponent<Image>();
            manaText = CreateBarText(manaObj.transform, "50/50");
        }

        private void CreateExpBar(Transform parent)
        {
            GameObject expObj = CreateBar("ExpBar", parent, new Vector2(10, -70), Color.yellow);
            expBar = expObj.transform.Find("Fill").GetComponent<Image>();
            expBar.fillAmount = 0;
        }

        private GameObject CreateBar(string name, Transform parent, Vector2 position, Color color)
        {
            GameObject barObj = new GameObject(name);
            barObj.transform.SetParent(parent, false);

            RectTransform rect = barObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(200, 20);

            // Background
            Image bg = barObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barObj.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;

            Image fill = fillObj.AddComponent<Image>();
            fill.color = color;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;

            return barObj;
        }

        private TextMeshProUGUI CreateBarText(Transform parent, string text)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return tmp;
        }

        private void CreateCombatIndicator(Transform parent)
        {
            GameObject indicatorObj = new GameObject("CombatIndicator");
            indicatorObj.transform.SetParent(parent, false);

            RectTransform rect = indicatorObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -10);
            rect.sizeDelta = new Vector2(150, 30);

            combatModeText = indicatorObj.AddComponent<TextMeshProUGUI>();
            combatModeText.text = "Exploration";
            combatModeText.fontSize = 18;
            combatModeText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateAPMPDisplay(Transform parent)
        {
            GameObject apmpObj = new GameObject("APMP_Display");
            apmpObj.transform.SetParent(parent, false);

            RectTransform rect = apmpObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(220, -10);
            rect.sizeDelta = new Vector2(100, 50);

            TextMeshProUGUI apmpText = apmpObj.AddComponent<TextMeshProUGUI>();
            apmpText.text = $"AP: {CurrentAP}/{MaxAP}\nMP: {CurrentMP}/{MaxMP}";
            apmpText.fontSize = 14;
            apmpText.color = Color.white;
        }

        #endregion

        #region Minimap

        private void SetupMinimap()
        {
            minimap = new GameObject("Minimap");
            minimap.transform.SetParent(transform, false);

            RectTransform rect = minimap.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-10, -10);
            rect.sizeDelta = new Vector2(200, 200);

            Image bg = minimap.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            // Add border
            Outline outline = minimap.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2, 2);
        }

        public void UpdateMinimapPosition(Vector2 position)
        {
            PlayerPositionOnMinimap = position;
            CheckMapEdgeProximity(position);
            CheckMapTransition();
        }

        public void UpdateMinimapEntities(List<MinimapEntity> entities)
        {
            MinimapEntities = new List<MinimapEntity>(entities);
        }

        public void ToggleMinimap()
        {
            MinimapVisible = !MinimapVisible;
            if (minimap != null)
                minimap.SetActive(MinimapVisible);
        }

        public void SetCurrentMapBounds(Rect bounds)
        {
            CurrentMapBounds = bounds;
        }

        #endregion

        #region Map Rendering

        private void SetupMapRenderer()
        {
            // Create MapRenderer GameObject in WORLD SPACE (not as UI child!)
            GameObject mapRendererObj = new GameObject("MapRenderer");
            // DON'T parent to UI transform - keep it in world space root
            // This allows SpriteRenderers to work properly
            mapRendererObj.transform.position = Vector3.zero;
            mapRendererObj.transform.localScale = Vector3.one;

            // Add MapRenderer component
            mapRenderer = mapRendererObj.AddComponent<MapRenderer>();

            // Store reference but don't parent it
            // MapRenderer will create world space sprites that Camera.main will render

            Debug.Log("[GameHUD] MapRenderer setup complete in World Space");
        }

        private void SetupCamera()
        {
            // Check if Main Camera exists, otherwise create one
            Camera mapCamera = Camera.main;
            GameObject cameraObj;

            if (mapCamera == null)
            {
                // Create camera GameObject in WORLD SPACE
                cameraObj = new GameObject("Main Camera");
                cameraObj.tag = "MainCamera";
                // DON'T parent to UI - keep in world space

                // Add Camera component
                mapCamera = cameraObj.AddComponent<Camera>();
            }
            else
            {
                cameraObj = mapCamera.gameObject;
            }

            // Configure camera for isometric/2D view
            mapCamera.orthographic = true;
            // Cells are now 200x100 (instead of 86x43), so we need a much larger camera view
            // Increased from 15f to 35f to accommodate the larger sprites
            mapCamera.orthographicSize = 35f; // Much larger to see all cells
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = new Color(0.2f, 0.3f, 0.4f, 1f);

            // Position camera to center on the isometric map
            // For a 14x20 isometric grid with cell dimensions 200x100 pixels at 50 PPU
            // The map center is roughly at the middle of all cells
            // Calculate center based on grid dimensions
            float centerX = 0; // Isometric grid centers around 0 on X
            float centerY = (IsometricHelper.GRID_HEIGHT / 2) * IsometricHelper.CELL_HALF_HEIGHT;
            mapCamera.transform.position = new Vector3(centerX, centerY, -10);
            mapCamera.transform.rotation = Quaternion.identity; // No rotation for 2D

            // Set depth to render before UI
            mapCamera.depth = -1;

            // Add CameraController for pan/zoom/drag
            cameraController = cameraObj.GetComponent<CameraController>();
            if (cameraController == null)
            {
                cameraController = cameraObj.AddComponent<CameraController>();
            }

            // Configure camera controller
            cameraController.CurrentZoom = 35f;
            cameraController.FollowTarget = false; // Start without following, will enable after character spawns

            // Set bounds based on map size (approximate for 14x20 grid)
            // X range: approximately -50 to 50
            // Y range: approximately -20 to 50
            cameraController.SetBounds(new Vector2(-50f, -20f), new Vector2(50f, 50f));

            Debug.Log("[GameHUD] Camera setup complete with CameraController");
        }

        /// <summary>
        /// Set character data (classId, gender, name) - should be called before SetCharacterCell
        /// </summary>
        public void SetCharacterData(int classId, bool isMale, string name = "Player")
        {
            characterClassId = classId;
            characterIsMale = isMale;
            characterName = name;
            
            Debug.Log($"[GameHUD] Character data set: Class {classId}, {(isMale ? "Male" : "Female")}, Name: {name}");
            
            // If character already exists, update it
            if (characterRenderer != null)
            {
                characterRenderer.SetClass(classId, isMale);
            }
        }
        
        /// <summary>
        /// Position character at the given cell ID and create character sprite if needed
        /// </summary>
        public void SetCharacterCell(int cellId)
        {
            CurrentCellId = cellId;

            // Create character renderer if it doesn't exist
            if (characterRenderer == null)
            {
                Debug.Log($"[GameHUD] Creating character renderer for class {characterClassId}");

                // Use ClassSpriteManager to create proper character renderer (like CharacterRenderingTest does)
                characterRenderer = GOFUS.UI.ClassSpriteManager.Instance.CreateCharacterRenderer(
                    characterClassId,
                    characterIsMale,
                    mapRenderer != null ? mapRenderer.transform : null, // Parent to MapRenderer
                    Vector3.zero // Initial position
                );

                if (characterRenderer != null)
                {
                    characterSprite = characterRenderer.gameObject;
                    characterSprite.name = $"Character_{characterName}";

                    // Set sorting order to render above map (map uses 0 to -40 range, so 100 is well above)
                    characterRenderer.SetSortingOrder(100);

                    // Scale character to be VERY visible (10x larger than default)
                    // User requested much larger sprite for better visibility
                    characterSprite.transform.localScale = Vector3.one * 10f;

                    Debug.Log($"[GameHUD] Character renderer created successfully with {characterRenderer.LayerCount} layers (scale: 10x)");

                    // Add PlayerController for movement
                    playerController = characterSprite.AddComponent<GOFUS.Player.PlayerController>();
                    playerController.Initialize(mapRenderer);

                    Debug.Log("[GameHUD] PlayerController added and initialized");
                }
                else
                {
                    Debug.LogError("[GameHUD] Failed to create character renderer! ClassSpriteManager returned null");
                    // Fallback to placeholder
                    CreatePlaceholderCharacter();
                }
            }

            // Position character at the cell
            if (characterSprite != null)
            {
                Vector3 worldPos = IsometricHelper.CellIdToWorldPosition(cellId);
                characterSprite.transform.position = worldPos; // CharacterLayerRenderer handles sprite offset internally

                // Update PlayerController position if it exists
                if (playerController != null)
                {
                    playerController.SetPosition(cellId);
                }

                Debug.Log($"[GameHUD] Character positioned at cell {cellId} -> world position {worldPos}");

                // Set camera to follow character
                if (cameraController != null)
                {
                    cameraController.Target = characterSprite.transform;
                    cameraController.FollowTarget = true;
                    cameraController.FocusOn(worldPos, immediate: true); // Start focused on character

                    Debug.Log("[GameHUD] Camera set to follow character");
                }
            }
            else
            {
                Debug.LogWarning("[GameHUD] No character sprite to position!");
            }
        }
        
        /// <summary>
        /// Fallback: Create a simple placeholder if ClassSpriteManager fails
        /// </summary>
        private void CreatePlaceholderCharacter()
        {
            characterSprite = new GameObject("CharacterPlaceholder");

            if (mapRenderer != null)
            {
                characterSprite.transform.SetParent(mapRenderer.transform, false);
            }

            // Scale placeholder to match character size (10x for visibility)
            characterSprite.transform.localScale = Vector3.one * 10f;

            // Create a simple visible sprite
            SpriteRenderer renderer = characterSprite.AddComponent<SpriteRenderer>();
            renderer.color = Color.cyan;
            renderer.sortingOrder = 100;
            
            // Create a larger, more visible diamond shape
            Texture2D tex = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % 64;
                int y = i / 64;
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32));
                pixels[i] = distance < 24 ? new Color(1f, 0f, 1f, 1f) : Color.clear; // Bright magenta
            }
            tex.SetPixels(pixels);
            tex.Apply();
            
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 16f); // Lower PPU for larger size
            renderer.sprite = sprite;
            
            Debug.LogWarning("[GameHUD] Using placeholder character sprite");
        }

        #endregion

        #region Seamless Map Transitions

        public void SetCurrentMapId(int mapId)
        {
            CurrentMapId = mapId;
        }
        
        /// <summary>
        /// Load map and position character - convenience method called from CharacterSelectionScreen
        /// </summary>
        public void LoadMap(int mapId, int cellId)
        {
            Debug.Log($"[GameHUD] LoadMap called: mapId={mapId}, cellId={cellId}");
            
            // Set the map ID
            SetCurrentMapId(mapId);
            
            // Load the map via MapRenderer
            if (mapRenderer != null)
            {
                mapRenderer.LoadMapFromServer(mapId);
            }
            else
            {
                Debug.LogWarning("[GameHUD] MapRenderer is null! Map won't load.");
            }
            
            // Position character at the cell
            SetCharacterCell(cellId);
        }

        public void SetAdjacentMaps(int topMapId = -1, int bottomMapId = -1,
            int leftMapId = -1, int rightMapId = -1,
            int topLeftMapId = -1, int topRightMapId = -1,
            int bottomLeftMapId = -1, int bottomRightMapId = -1)
        {
            AdjacentMaps.Clear();

            if (topMapId != -1) AdjacentMaps[MapEdge.Top] = topMapId;
            if (bottomMapId != -1) AdjacentMaps[MapEdge.Bottom] = bottomMapId;
            if (leftMapId != -1) AdjacentMaps[MapEdge.Left] = leftMapId;
            if (rightMapId != -1) AdjacentMaps[MapEdge.Right] = rightMapId;
            if (topLeftMapId != -1) AdjacentMaps[MapEdge.TopLeft] = topLeftMapId;
            if (topRightMapId != -1) AdjacentMaps[MapEdge.TopRight] = topRightMapId;
            if (bottomLeftMapId != -1) AdjacentMaps[MapEdge.BottomLeft] = bottomLeftMapId;
            if (bottomRightMapId != -1) AdjacentMaps[MapEdge.BottomRight] = bottomRightMapId;
        }

        public bool IsNearMapEdge(MapEdge edge)
        {
            float x = PlayerPositionOnMinimap.x;
            float y = PlayerPositionOnMinimap.y;

            switch (edge)
            {
                case MapEdge.Right:
                    return x > CurrentMapBounds.width - EDGE_DETECTION_DISTANCE;
                case MapEdge.Left:
                    return x < EDGE_DETECTION_DISTANCE;
                case MapEdge.Top:
                    return y > CurrentMapBounds.height - EDGE_DETECTION_DISTANCE;
                case MapEdge.Bottom:
                    return y < EDGE_DETECTION_DISTANCE;
                default:
                    return false;
            }
        }

        private void CheckMapEdgeProximity(Vector2 position)
        {
            foreach (MapEdge edge in Enum.GetValues(typeof(MapEdge)))
            {
                if (edge == MapEdge.None) continue;

                if (IsNearMapEdge(edge) && AdjacentMaps.ContainsKey(edge))
                {
                    PreloadingMapId = AdjacentMaps[edge];
                    OnApproachingEdge?.Invoke(edge);
                }
            }
        }

        public void CheckMapTransition()
        {
            float x = PlayerPositionOnMinimap.x;
            float y = PlayerPositionOnMinimap.y;

            // Check for edge crossing
            if (x >= CurrentMapBounds.width - 1 && AdjacentMaps.ContainsKey(MapEdge.Right))
            {
                TriggerMapTransition(AdjacentMaps[MapEdge.Right]);
            }
            else if (x <= 1 && AdjacentMaps.ContainsKey(MapEdge.Left))
            {
                TriggerMapTransition(AdjacentMaps[MapEdge.Left]);
            }
            else if (y >= CurrentMapBounds.height - 1 && AdjacentMaps.ContainsKey(MapEdge.Top))
            {
                TriggerMapTransition(AdjacentMaps[MapEdge.Top]);
            }
            else if (y <= 1 && AdjacentMaps.ContainsKey(MapEdge.Bottom))
            {
                TriggerMapTransition(AdjacentMaps[MapEdge.Bottom]);
            }
        }

        private void TriggerMapTransition(int targetMapId)
        {
            CurrentMapId = targetMapId;
            OnMapTransition?.Invoke(targetMapId);
        }

        public bool IsMapPreloading(int mapId)
        {
            return PreloadingMapId == mapId;
        }

        public void SetPlayerVelocity(Vector2 velocity)
        {
            playerVelocity = velocity;
        }

        public Vector2 GetPlayerVelocity()
        {
            return playerVelocity;
        }

        #endregion

        #region Health & Mana

        public void UpdateHealth(int current, int max)
        {
            CurrentHealth = Mathf.Clamp(current, 0, max);
            MaxHealth = Mathf.Max(1, max);

            if (healthBar != null)
                healthBar.fillAmount = HealthPercentage;

            if (healthText != null)
                healthText.text = $"{CurrentHealth}/{MaxHealth}";
        }

        public void UpdateMana(int current, int max)
        {
            CurrentMana = Mathf.Clamp(current, 0, max);
            MaxMana = Mathf.Max(0, max);

            if (manaBar != null)
                manaBar.fillAmount = ManaPercentage;

            if (manaText != null)
                manaText.text = $"{CurrentMana}/{MaxMana}";
        }

        #endregion

        #region Experience & Level

        public void UpdateLevel(int level)
        {
            int oldLevel = Level;
            Level = level;

            if (levelText != null)
                levelText.text = $"Level {level}";

            if (level > oldLevel)
                OnLevelUp?.Invoke(level);
        }

        public void UpdateExperience(int current, int needed)
        {
            CurrentExp = current;
            RequiredExp = needed;

            if (expBar != null)
                expBar.fillAmount = ExpPercentage;
        }

        #endregion

        #region Action Points

        public void UpdateActionPoints(int current, int max)
        {
            CurrentAP = current;
            MaxAP = max;
        }

        public void UpdateMovementPoints(int current, int max)
        {
            CurrentMP = current;
            MaxMP = max;
        }

        #endregion

        #region Combat Mode

        public void SetCombatMode(CombatMode mode)
        {
            CurrentCombatMode = mode;

            switch (mode)
            {
                case CombatMode.TurnBased:
                    CombatModeText = "Turn-Based";
                    break;
                case CombatMode.RealTime:
                    CombatModeText = "Real-Time";
                    break;
                default:
                    CombatModeText = "Exploration";
                    break;
            }

            if (combatModeText != null)
                combatModeText.text = CombatModeText;

            // Show/Hide ATB gauge
            if (atbGauge != null)
                atbGauge.SetActive(mode == CombatMode.RealTime);
        }

        public void UpdateATBGauge(float progress)
        {
            ATBProgress = Mathf.Clamp01(progress);
            if (atbFill != null)
                atbFill.fillAmount = ATBProgress;
        }

        #endregion

        #region Quick Action Bar

        private void SetupQuickActionBar()
        {
            GameObject quickBar = new GameObject("QuickActionBar");
            quickBar.transform.SetParent(transform, false);

            RectTransform rect = quickBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 20);
            rect.sizeDelta = new Vector2(500, 60);

            // Create slots
            for (int i = 0; i < 10; i++)
            {
                CreateQuickSlot(quickBar.transform, i);
            }
        }

        private void CreateQuickSlot(Transform parent, int index)
        {
            GameObject slot = new GameObject($"Slot_{index + 1}");
            slot.transform.SetParent(parent, false);

            RectTransform rect = slot.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(index * 50, 0);
            rect.sizeDelta = new Vector2(45, 45);

            Image bg = slot.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        }

        public void SetQuickSkills(List<QuickSkill> skills)
        {
            QuickSkills = new List<QuickSkill>(skills);
        }

        public void SetSkillCooldown(int skillId, float cooldown)
        {
            skillCooldowns[skillId] = cooldown;
        }

        public float GetSkillCooldown(int skillId)
        {
            return skillCooldowns.ContainsKey(skillId) ? skillCooldowns[skillId] : 0;
        }

        public bool IsSkillOnCooldown(int skillId)
        {
            return GetSkillCooldown(skillId) > 0;
        }

        public void UpdateCooldowns(float deltaTime)
        {
            List<int> toRemove = new List<int>();

            foreach (var kvp in skillCooldowns)
            {
                skillCooldowns[kvp.Key] -= deltaTime;
                if (skillCooldowns[kvp.Key] <= 0)
                    toRemove.Add(kvp.Key);
            }

            foreach (int key in toRemove)
                skillCooldowns.Remove(key);
        }

        #endregion

        #region Status Effects

        private void SetupStatusEffectBars()
        {
            // Buff container
            buffContainer = CreateEffectContainer("BuffContainer", new Vector2(10, -100));

            // Debuff container
            debuffContainer = CreateEffectContainer("DebuffContainer", new Vector2(10, -130));
        }

        private Transform CreateEffectContainer(string name, Vector2 position)
        {
            GameObject container = new GameObject(name);
            container.transform.SetParent(transform, false);

            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300, 25);

            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;

            return container.transform;
        }

        public void UpdateBuffs(List<StatusEffect> buffs)
        {
            ActiveBuffs = new List<StatusEffect>(buffs);
            RefreshStatusDisplay(buffContainer, buffs);
        }

        public void UpdateDebuffs(List<StatusEffect> debuffs)
        {
            ActiveDebuffs = new List<StatusEffect>(debuffs);
            RefreshStatusDisplay(debuffContainer, debuffs);
        }

        private void RefreshStatusDisplay(Transform container, List<StatusEffect> effects)
        {
            // Clear existing
            foreach (Transform child in container)
                Destroy(child.gameObject);

            // Add new
            foreach (var effect in effects)
            {
                GameObject effectObj = new GameObject(effect.Name);
                effectObj.transform.SetParent(container, false);

                RectTransform rect = effectObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(25, 25);

                Image icon = effectObj.AddComponent<Image>();
                icon.color = Color.white;
            }
        }

        #endregion

        #region Notifications

        public void ShowNotification(string message, NotificationType type, float duration = 3f)
        {
            var notification = new HUDNotification
            {
                Message = message,
                Type = type,
                TimeRemaining = duration
            };

            ActiveNotifications.Add(notification);
            CreateNotificationUI(notification);
        }

        private void CreateNotificationUI(HUDNotification notification)
        {
            if (notificationContainer == null)
            {
                GameObject container = new GameObject("NotificationContainer");
                container.transform.SetParent(transform, false);

                RectTransform rect = container.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.7f);
                rect.anchorMax = new Vector2(0.5f, 0.7f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(400, 200);

                VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 5;

                notificationContainer = container.transform;
            }

            GameObject notifObj = new GameObject("Notification");
            notifObj.transform.SetParent(notificationContainer, false);

            RectTransform notifRect = notifObj.AddComponent<RectTransform>();
            notifRect.sizeDelta = new Vector2(400, 30);

            TextMeshProUGUI text = notifObj.AddComponent<TextMeshProUGUI>();
            text.text = notification.Message;
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.Center;

            // Color based on type
            switch (notification.Type)
            {
                case NotificationType.Success:
                    text.color = Color.green;
                    break;
                case NotificationType.Warning:
                    text.color = Color.yellow;
                    break;
                case NotificationType.Error:
                    text.color = Color.red;
                    break;
                default:
                    text.color = Color.white;
                    break;
            }
        }

        public void UpdateNotifications(float deltaTime)
        {
            for (int i = ActiveNotifications.Count - 1; i >= 0; i--)
            {
                ActiveNotifications[i].TimeRemaining -= deltaTime;
                if (ActiveNotifications[i].TimeRemaining <= 0)
                {
                    ActiveNotifications.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Custom Resources

        public void AddResourceBar(string name, int current, int max, Color color)
        {
            var resource = new ResourceBar
            {
                Name = name,
                Current = current,
                Max = max,
                Color = color,
                Percentage = max > 0 ? (float)current / max : 0
            };

            CustomResources.Add(resource);
        }

        public void UpdateResource(string name, int current)
        {
            var resource = CustomResources.Find(r => r.Name == name);
            if (resource != null)
            {
                resource.Current = current;
                resource.Percentage = resource.Max > 0 ? (float)current / resource.Max : 0;
            }
        }

        public ResourceBar GetResource(string name)
        {
            return CustomResources.Find(r => r.Name == name);
        }

        #endregion

        #region Update

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                ToggleMinimap();
            }

            // Update cooldowns
            UpdateCooldowns(Time.deltaTime);

            // Update notifications
            UpdateNotifications(Time.deltaTime);

            // Check for seamless transitions continuously
            if (CurrentMapBounds.width > 0 && CurrentMapBounds.height > 0)
            {
                CheckMapTransition();
            }
        }

        #endregion
    }

    #region Supporting Classes

    public class MinimapEntity
    {
        public int Id;
        public Vector2 Position;
        public GOFUS.Entities.EntityType Type;
    }

    // Using EntityType from GOFUS.Entities namespace
    // Using MapEdge from GOFUS.Map namespace
    // Using CombatMode from GOFUS.Combat namespace

    public class QuickSkill
    {
        public int SlotId;
        public int SkillId;
        public KeyCode Keybind;
    }

    public class StatusEffect
    {
        public int Id;
        public string Name;
        public float Duration;
        public string Icon;
    }

    public enum NotificationType
    {
        Info, Success, Warning, Error
    }

    public class HUDNotification
    {
        public string Message;
        public NotificationType Type;
        public float TimeRemaining;
    }

    public class ResourceBar
    {
        public string Name;
        public int Current;
        public int Max;
        public float Percentage;
        public Color Color;
    }

    #endregion
}