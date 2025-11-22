using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using GOFUS.Core;
using GOFUS.Models;
using GOFUS.UI;
using GOFUS.Rendering;

namespace GOFUS.UI.Screens
{
    /// <summary>
    /// Character creation screen for creating new characters
    /// Shows all 12 classes with their descriptions and spells
    /// </summary>
    public class CharacterCreationScreen : UIScreen
    {
        [Header("Class Selection")]
        private RectTransform classGridContainer;
        private List<ClassButton> classButtons;
        private ClassData selectedClass;
        private int selectedClassId = -1;

        [Header("Class Information")]
        private TextMeshProUGUI classNameText;
        private TextMeshProUGUI classDescriptionText;
        private TextMeshProUGUI classRoleText;
        private TextMeshProUGUI classElementText;
        private TextMeshProUGUI classStatsText;
        private RectTransform spellListContainer;
        private Transform characterPreviewContainer;
        private CharacterLayerRenderer currentCharacterRenderer;

        [Header("Character Settings")]
        private TMP_InputField nameInput;
        private Toggle maleToggle;
        private Toggle femaleToggle;
        private bool isMale = true;

        [Header("Buttons")]
        private Button createButton;
        private Button cancelButton;
        private Button randomNameButton;

        [Header("Status")]
        private TextMeshProUGUI statusText;
        private GameObject loadingIndicator;

        [Header("Backend")]
        private string backendUrl = "https://gofus-backend.vercel.app";
        private string localBackendUrl = "http://localhost:3000";
        private bool useLocalBackend = false;
        private string jwtToken;
        private List<ClassData> availableClasses;
        private bool isCreating = false;

        // Events
        public event Action<int> OnCharacterCreated;
        public event Action OnCancelClicked;

        // Class sprite manager
        private ClassSpriteManager classSpriteManager;

        public override void Initialize()
        {
            base.Initialize();

            Debug.Log("[CharacterCreation] Initializing...");

            // Initialize sprite manager
            classSpriteManager = ClassSpriteManager.Instance;
            classSpriteManager.LoadClassSprites();

            // Get JWT token
            jwtToken = PlayerPrefs.GetString("jwt_token", "");
            useLocalBackend = PlayerPrefs.GetInt("use_local_backend", 0) == 1;

            // Initialize UI
            classButtons = new List<ClassButton>();
            availableClasses = new List<ClassData>();

            CreateUI();
            SetupEventHandlers();

            Debug.Log("[CharacterCreation] Initialization complete");
        }

        protected override void OnScreenShown()
        {
            base.OnScreenShown();

            // Load classes when screen is shown (not during initialization)
            // This ensures the GameObject is active so coroutines can run
            if (availableClasses == null || availableClasses.Count == 0)
            {
                LoadAvailableClasses();
            }
        }

        private void OnDestroy()
        {
            // Clean up character preview
            if (currentCharacterRenderer != null)
            {
                Destroy(currentCharacterRenderer.gameObject);
                currentCharacterRenderer = null;
            }

            // Clean up preview container
            if (characterPreviewContainer != null)
            {
                Destroy(characterPreviewContainer.gameObject);
            }
        }

        private void CreateUI()
        {
            // Main container
            GameObject container = new GameObject("CharacterCreationContainer");
            container.transform.SetParent(transform, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Background
            Image bg = container.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);

            // Title
            CreateTitle("Create New Character", container.transform);

            // Left Panel - Class Grid
            GameObject leftPanel = CreatePanel("ClassPanel", container.transform,
                new Vector2(0.02f, 0.1f), new Vector2(0.48f, 0.85f));
            CreateClassGrid(leftPanel.transform);

            // Right Panel - Class Info and Settings
            GameObject rightPanel = CreatePanel("InfoPanel", container.transform,
                new Vector2(0.52f, 0.1f), new Vector2(0.98f, 0.85f));
            CreateClassInfoPanel(rightPanel.transform);
            CreateCharacterSettingsPanel(rightPanel.transform);

            // Bottom - Buttons
            CreateBottomButtons(container.transform);

            // Status bar
            CreateStatusBar(container.transform);
        }

        private void CreateTitle(string text, Transform parent)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);

            RectTransform rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.92f);
            rect.anchorMax = new Vector2(0.8f, 0.98f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = text;
            titleText.fontSize = 42;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            titleText.fontStyle = FontStyles.Bold;
        }

        private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.18f, 0.9f);  // Lightened for better contrast

            return panel;
        }

        private void CreateClassGrid(Transform parent)
        {
            // Title
            GameObject gridTitle = new GameObject("GridTitle");
            gridTitle.transform.SetParent(parent, false);

            RectTransform titleRect = gridTitle.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.92f);
            titleRect.anchorMax = new Vector2(0.9f, 0.98f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            TextMeshProUGUI titleText = gridTitle.AddComponent<TextMeshProUGUI>();
            titleText.text = "Choose Your Class";
            titleText.fontSize = 28;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.9f, 0.8f, 0.3f);

            // Grid container
            GameObject gridObj = new GameObject("ClassGrid");
            gridObj.transform.SetParent(parent, false);

            RectTransform gridRect = gridObj.AddComponent<RectTransform>();
            classGridContainer = gridRect;
            gridRect.anchorMin = new Vector2(0.05f, 0.05f);
            gridRect.anchorMax = new Vector2(0.95f, 0.9f);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;

            // Grid layout - 4 columns x 3 rows for 12 classes
            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(100, 120);
            grid.spacing = new Vector2(15, 15);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            // Create placeholder class buttons (will be populated when classes load)
            for (int i = 1; i <= 12; i++)
            {
                CreateClassButton(i);
            }

            // Layout will update automatically
        }

        private void CreateClassButton(int classId)
        {
            GameObject buttonObj = new GameObject($"ClassButton_{classId}");
            buttonObj.transform.SetParent(classGridContainer, false);

            // Add RectTransform for proper layout in GridLayoutGroup
            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();

            // Add LayoutElement to ensure proper sizing in GridLayoutGroup
            UnityEngine.UI.LayoutElement layoutElement = buttonObj.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.preferredWidth = 100;
            layoutElement.preferredHeight = 120;
            layoutElement.minWidth = 100;
            layoutElement.minHeight = 120;

            ClassButton classBtn = buttonObj.AddComponent<ClassButton>();
            classBtn.Initialize(classId);
            classBtn.OnClicked += OnClassButtonClicked;

            classButtons.Add(classBtn);
        }

        private void CreateClassInfoPanel(Transform parent)
        {
            // Class name
            classNameText = CreateText("ClassName", parent,
                new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.92f),
                "Select a Class", 32, TextAlignmentOptions.Center);
            classNameText.fontStyle = FontStyles.Bold;
            classNameText.color = new Color(1f, 0.9f, 0.4f);

            // Character preview container (world space for CharacterLayerRenderer)
            GameObject previewContainerObj = new GameObject("CharacterPreviewContainer");
            characterPreviewContainer = previewContainerObj.transform;

            // Position the container in world space
            // This will be where the character sprite appears
            // Position is set when character is created based on screen position

            // Role and Element
            classRoleText = CreateText("ClassRole", parent,
                new Vector2(0.05f, 0.6f), new Vector2(0.45f, 0.65f),
                "Role: -", 16, TextAlignmentOptions.Left);

            classElementText = CreateText("ClassElement", parent,
                new Vector2(0.55f, 0.6f), new Vector2(0.95f, 0.65f),
                "Element: -", 16, TextAlignmentOptions.Right);

            // Description
            classDescriptionText = CreateText("ClassDescription", parent,
                new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.6f),
                "Class description will appear here...", 14, TextAlignmentOptions.TopLeft);
            classDescriptionText.color = new Color(0.8f, 0.8f, 0.8f);

            // Stats per level
            GameObject statsTitle = new GameObject("StatsTitle");
            statsTitle.transform.SetParent(parent, false);

            RectTransform statsTitleRect = statsTitle.AddComponent<RectTransform>();
            statsTitleRect.anchorMin = new Vector2(0.05f, 0.38f);
            statsTitleRect.anchorMax = new Vector2(0.95f, 0.42f);
            statsTitleRect.offsetMin = Vector2.zero;
            statsTitleRect.offsetMax = Vector2.zero;

            TextMeshProUGUI statsTitleText = statsTitle.AddComponent<TextMeshProUGUI>();
            statsTitleText.text = "Stats Per Level:";
            statsTitleText.fontSize = 16;
            statsTitleText.alignment = TextAlignmentOptions.Left;
            statsTitleText.fontStyle = FontStyles.Bold;

            classStatsText = CreateText("ClassStats", parent,
                new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.38f),
                "Vit: - | Wis: - | Str: - | Int: - | Cha: - | Agi: -", 14, TextAlignmentOptions.Left);

            // Starting Spells section
            GameObject spellsTitle = new GameObject("SpellsTitle");
            spellsTitle.transform.SetParent(parent, false);

            RectTransform spellsTitleRect = spellsTitle.AddComponent<RectTransform>();
            spellsTitleRect.anchorMin = new Vector2(0.05f, 0.25f);
            spellsTitleRect.anchorMax = new Vector2(0.95f, 0.3f);
            spellsTitleRect.offsetMin = Vector2.zero;
            spellsTitleRect.offsetMax = Vector2.zero;

            TextMeshProUGUI spellsTitleText = spellsTitle.AddComponent<TextMeshProUGUI>();
            spellsTitleText.text = "Starting Spells:";
            spellsTitleText.fontSize = 16;
            spellsTitleText.alignment = TextAlignmentOptions.Left;
            spellsTitleText.fontStyle = FontStyles.Bold;

            // Spell list container with scroll
            GameObject spellScrollObj = new GameObject("SpellScroll");
            spellScrollObj.transform.SetParent(parent, false);

            RectTransform scrollRect = spellScrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.05f, 0.05f);
            scrollRect.anchorMax = new Vector2(0.95f, 0.25f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            ScrollRect scroll = spellScrollObj.AddComponent<ScrollRect>();
            scroll.vertical = true;
            scroll.horizontal = false;

            // Spell content
            GameObject spellContent = new GameObject("SpellContent");
            spellContent.transform.SetParent(spellScrollObj.transform, false);

            RectTransform contentRect = spellContent.AddComponent<RectTransform>();
            spellListContainer = contentRect;
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 300);

            VerticalLayoutGroup layout = spellContent.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = spellContent.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;
        }

        private void CreateCharacterSettingsPanel(Transform parent)
        {
            // Separator line
            GameObject separator = new GameObject("Separator");
            separator.transform.SetParent(parent, false);

            RectTransform sepRect = separator.AddComponent<RectTransform>();
            sepRect.anchorMin = new Vector2(0.05f, 0.29f);
            sepRect.anchorMax = new Vector2(0.95f, 0.295f);
            sepRect.offsetMin = Vector2.zero;
            sepRect.offsetMax = Vector2.zero;

            Image sepImage = separator.AddComponent<Image>();
            sepImage.color = new Color(0.3f, 0.3f, 0.3f);
        }

        private void CreateBottomButtons(Transform parent)
        {
            // Character Name Input
            GameObject namePanel = new GameObject("NamePanel");
            namePanel.transform.SetParent(parent, false);

            RectTransform namePanelRect = namePanel.AddComponent<RectTransform>();
            namePanelRect.anchorMin = new Vector2(0.35f, 0.03f);
            namePanelRect.anchorMax = new Vector2(0.65f, 0.08f);
            namePanelRect.offsetMin = Vector2.zero;
            namePanelRect.offsetMax = Vector2.zero;

            // Name label
            GameObject nameLabel = new GameObject("NameLabel");
            nameLabel.transform.SetParent(namePanel.transform, false);

            RectTransform labelRect = nameLabel.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(0.25f, 1);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI labelText = nameLabel.AddComponent<TextMeshProUGUI>();
            labelText.text = "Name:";
            labelText.fontSize = 18;
            labelText.alignment = TextAlignmentOptions.MidlineRight;

            // Name input field
            GameObject inputObj = new GameObject("NameInput");
            inputObj.transform.SetParent(namePanel.transform, false);

            RectTransform inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.27f, 0);
            inputRect.anchorMax = new Vector2(0.73f, 1);
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;

            Image inputBg = inputObj.AddComponent<Image>();
            inputBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            nameInput = inputObj.AddComponent<TMP_InputField>();
            nameInput.characterLimit = 20;
            nameInput.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;

            GameObject placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(inputObj.transform, false);
            RectTransform placeholderRect = placeholder.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10, 0);
            placeholderRect.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Enter character name...";
            placeholderText.fontSize = 16;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 16;
            inputText.color = Color.white;
            inputText.alignment = TextAlignmentOptions.MidlineLeft;

            nameInput.placeholder = placeholderText;
            nameInput.textComponent = inputText;
            nameInput.textViewport = textRect;

            // Random name button
            randomNameButton = CreateButton("Random", namePanel.transform,
                new Vector2(0.75f, 0), new Vector2(1f, 1f));
            randomNameButton.onClick.AddListener(GenerateRandomName);

            // Gender selection
            GameObject genderPanel = new GameObject("GenderPanel");
            genderPanel.transform.SetParent(parent, false);

            RectTransform genderRect = genderPanel.AddComponent<RectTransform>();
            genderRect.anchorMin = new Vector2(0.7f, 0.03f);
            genderRect.anchorMax = new Vector2(0.9f, 0.08f);
            genderRect.offsetMin = Vector2.zero;
            genderRect.offsetMax = Vector2.zero;

            // Male toggle
            GameObject maleObj = new GameObject("MaleToggle");
            maleObj.transform.SetParent(genderPanel.transform, false);

            RectTransform maleRect = maleObj.AddComponent<RectTransform>();
            maleRect.anchorMin = new Vector2(0, 0);
            maleRect.anchorMax = new Vector2(0.45f, 1);
            maleRect.offsetMin = Vector2.zero;
            maleRect.offsetMax = Vector2.zero;

            maleToggle = CreateToggle(maleObj, "Male", true);

            // Female toggle
            GameObject femaleObj = new GameObject("FemaleToggle");
            femaleObj.transform.SetParent(genderPanel.transform, false);

            RectTransform femaleRect = femaleObj.AddComponent<RectTransform>();
            femaleRect.anchorMin = new Vector2(0.55f, 0);
            femaleRect.anchorMax = new Vector2(1f, 1);
            femaleRect.offsetMin = Vector2.zero;
            femaleRect.offsetMax = Vector2.zero;

            femaleToggle = CreateToggle(femaleObj, "Female", false);

            // Toggle group for gender selection
            ToggleGroup genderGroup = genderPanel.AddComponent<ToggleGroup>();
            maleToggle.group = genderGroup;
            femaleToggle.group = genderGroup;

            // Create and Cancel buttons
            createButton = CreateButton("Create Character", parent,
                new Vector2(0.05f, 0.03f), new Vector2(0.25f, 0.08f));
            createButton.onClick.AddListener(CreateCharacter);
            createButton.interactable = false; // Disabled until class selected

            cancelButton = CreateButton("Cancel", parent,
                new Vector2(0.92f, 0.03f), new Vector2(0.98f, 0.08f));
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }

        private Toggle CreateToggle(GameObject parent, string label, bool isOn)
        {
            Toggle toggle = parent.AddComponent<Toggle>();

            // Checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(parent.transform, false);

            RectTransform checkRect = checkmark.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.2f);
            checkRect.anchorMax = new Vector2(0.3f, 0.8f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;

            Image checkImage = checkmark.AddComponent<Image>();
            checkImage.color = Color.green;

            toggle.graphic = checkImage;
            toggle.isOn = isOn;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(parent.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.35f, 0);
            labelRect.anchorMax = new Vector2(0.9f, 1);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 16;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;

            return toggle;
        }

        private void CreateStatusBar(Transform parent)
        {
            GameObject statusBar = new GameObject("StatusBar");
            statusBar.transform.SetParent(parent, false);

            RectTransform statusRect = statusBar.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.2f, 0);
            statusRect.anchorMax = new Vector2(0.8f, 0.03f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            statusText = statusBar.AddComponent<TextMeshProUGUI>();
            statusText.text = "";
            statusText.fontSize = 14;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = Color.yellow;
        }

        private TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, string text, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.alignment = alignment;
            tmpText.color = Color.white;

            return tmpText;
        }

        private Button CreateButton(string text, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObj = new GameObject(text + "Button");
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = buttonObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = bg;

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontSize = 18;
            buttonText.color = Color.white;

            return button;
        }

        private void SetupEventHandlers()
        {
            if (maleToggle != null)
                maleToggle.onValueChanged.AddListener(OnGenderChanged);

            if (nameInput != null)
                nameInput.onValueChanged.AddListener(OnNameChanged);
        }

        private void LoadAvailableClasses()
        {
            StartCoroutine(LoadClassesCoroutine());
        }

        private IEnumerator LoadClassesCoroutine()
        {
            SetStatus("Loading available classes...", Color.white);

            string baseUrl = useLocalBackend ? localBackendUrl : backendUrl;
            string url = $"{baseUrl}/api/classes";

            Debug.Log($"[CharacterCreation] Loading classes from: {url}");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = request.downloadHandler.text;
                        Debug.Log($"[CharacterCreation] Classes response: {json}");

                        ClassListResponse response = JsonUtility.FromJson<ClassListResponse>(json);

                        if (response != null && response.classes != null && response.classes.Length > 0)
                        {
                            availableClasses = response.classes.ToList();

                            // Log loaded class IDs for debugging
                            string classIds = string.Join(", ", availableClasses.Select(c => $"{c.id}:{c.name}"));
                            Debug.Log($"[CharacterCreation] Successfully loaded {availableClasses.Count} classes from backend: {classIds}");

                            UpdateClassButtons();
                            SetStatus($"Loaded {availableClasses.Count} classes", Color.green);
                        }
                        else
                        {
                            Debug.LogWarning("[CharacterCreation] Backend returned empty classes array. Using mock data.");
                            LoadMockClasses();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[CharacterCreation] Failed to parse classes: {e.Message}");
                        LoadMockClasses();
                    }
                }
                else
                {
                    Debug.LogError($"[CharacterCreation] Failed to load classes: {request.error}");
                    LoadMockClasses();
                }
            }
        }

        private void LoadMockClasses()
        {
            SetStatus("Using mock class data (backend unavailable)", Color.yellow);
            Debug.LogWarning("[CharacterCreation] Loading mock class data. Classes and spells shown are placeholders.");

            // Create mock classes for testing with basic stats and spells
            availableClasses = new List<ClassData>
            {
                CreateMockClass(1, "Feca", "Masters of protection and defensive magic", 50, 10, 10, 10, 10, 10),
                CreateMockClass(2, "Osamodas", "Beast masters who summon creatures", 40, 15, 10, 15, 10, 10),
                CreateMockClass(3, "Enutrof", "Treasure hunters with earth magic", 45, 15, 10, 10, 15, 15),
                CreateMockClass(4, "Sram", "Deadly assassins with traps", 40, 10, 15, 10, 10, 25),
                CreateMockClass(5, "Xelor", "Time mages who manipulate AP", 40, 15, 10, 20, 10, 15),
                CreateMockClass(6, "Ecaflip", "Gamblers relying on luck", 45, 12, 15, 12, 18, 15),
                CreateMockClass(7, "Eniripsa", "Powerful healers and support", 45, 20, 10, 15, 10, 10),
                CreateMockClass(8, "Iop", "Fearless melee warriors", 50, 10, 25, 10, 10, 10),
                CreateMockClass(9, "Cra", "Expert archers with precision", 40, 12, 10, 10, 10, 28),
                CreateMockClass(10, "Sadida", "Nature mages commanding plants", 45, 15, 10, 18, 12, 12),
                CreateMockClass(11, "Sacrieur", "Berserkers gaining power from pain", 55, 10, 20, 10, 10, 10),
                CreateMockClass(12, "Pandawa", "Drunken brawlers", 50, 12, 18, 10, 12, 12)
            };

            UpdateClassButtons();
        }

        private ClassData CreateMockClass(int id, string name, string description, int vit, int wis, int str, int intel, int cha, int agi)
        {
            return new ClassData
            {
                id = id,
                name = name,
                description = description,
                statsPerLevel = new StatsPerLevel
                {
                    vitality = vit,
                    wisdom = wis,
                    strength = str,
                    intelligence = intel,
                    chance = cha,
                    agility = agi
                },
                startingSpells = new List<SpellData>
                {
                    new SpellData
                    {
                        id = id * 100 + 1,
                        name = $"{name} Strike",
                        description = $"Basic attack for {name} class",
                        apCost = 3,
                        range = "1-3",
                        effects = new List<SpellEffect>
                        {
                            new SpellEffect { type = "damage", element = "neutral", min = 10, max = 15 }
                        }
                    },
                    new SpellData
                    {
                        id = id * 100 + 2,
                        name = $"{name} Shield",
                        description = $"Defensive ability for {name} class",
                        apCost = 2,
                        range = "0",
                        effects = new List<SpellEffect>
                        {
                            new SpellEffect { type = "shield", value = 20 }
                        }
                    },
                    new SpellData
                    {
                        id = id * 100 + 3,
                        name = $"{name} Power",
                        description = $"Special ability for {name} class",
                        apCost = 4,
                        range = "1-5",
                        effects = new List<SpellEffect>
                        {
                            new SpellEffect { type = "damage", element = "fire", min = 15, max = 25 }
                        }
                    }
                }
            };
        }

        private void UpdateClassButtons()
        {
            Debug.Log($"[CharacterCreation] UpdateClassButtons called. classButtons count: {classButtons.Count}, availableClasses count: {availableClasses.Count}");

            if (classButtons == null || classButtons.Count == 0)
            {
                Debug.LogError("[CharacterCreation] classButtons list is null or empty!");
                return;
            }

            if (availableClasses == null || availableClasses.Count == 0)
            {
                Debug.LogError("[CharacterCreation] availableClasses list is null or empty!");
                return;
            }

            int updatedCount = 0;
            foreach (var button in classButtons)
            {
                Debug.Log($"[CharacterCreation] Processing button with ClassId: {button.ClassId}");
                var classData = availableClasses.FirstOrDefault(c => c.id == button.ClassId);
                if (classData != null)
                {
                    Debug.Log($"[CharacterCreation] Found class data for ID {button.ClassId}: {classData.name}");
                    button.SetClassData(classData, classSpriteManager);
                    updatedCount++;
                }
                else
                {
                    Debug.LogWarning($"[CharacterCreation] No class data found for button with ClassId: {button.ClassId}");
                }
            }

            Debug.Log($"[CharacterCreation] UpdateClassButtons complete. Updated {updatedCount} buttons.");

            // Layout will update automatically on next frame
            // Forcing immediate rebuild can cause crashes when called during JSON parsing
        }

        private void OnClassButtonClicked(int classId)
        {
            Debug.Log($"[CharacterCreation] Class {classId} selected");

            // Deselect previous
            foreach (var button in classButtons)
            {
                button.SetSelected(button.ClassId == classId);
            }

            selectedClassId = classId;
            selectedClass = availableClasses.FirstOrDefault(c => c.id == classId);

            if (selectedClass != null)
            {
                DisplayClassInfo(selectedClass);
                UpdateCreateButtonState();
            }
        }

        private void DisplayClassInfo(ClassData classData)
        {
            classNameText.text = classData.name;
            classDescriptionText.text = classData.description;
            classRoleText.text = $"Role: {classData.GetRoleDescription()}";
            classElementText.text = $"Element: {classData.GetElementFocus()}";

            if (classData.statsPerLevel != null)
            {
                var stats = classData.statsPerLevel;
                classStatsText.text = $"Vit: {stats.vitality} | Wis: {stats.wisdom} | Str: {stats.strength} | Int: {stats.intelligence} | Cha: {stats.chance} | Agi: {stats.agility}";
            }

            // Update character preview with layered renderer
            UpdateCharacterPreview(classData.id, isMale);

            // Display starting spells
            DisplaySpells(classData.startingSpells);
        }

        private void UpdateCharacterPreview(int classId, bool male)
        {
            // Destroy existing character preview
            if (currentCharacterRenderer != null)
            {
                Destroy(currentCharacterRenderer.gameObject);
                currentCharacterRenderer = null;
            }

            // Create new character renderer using the sprite manager
            if (classSpriteManager != null)
            {
                // Position in world space where it will be visible
                // This position should be adjusted based on your camera setup
                Vector3 previewPosition = new Vector3(8f, 3f, 0f);

                currentCharacterRenderer = classSpriteManager.CreateCharacterRenderer(
                    classId,
                    male,
                    characterPreviewContainer,
                    previewPosition
                );

                if (currentCharacterRenderer != null)
                {
                    Debug.Log($"[CharacterCreation] Created character preview for class {classId}");
                }
                else
                {
                    Debug.LogWarning($"[CharacterCreation] Failed to create character preview for class {classId}");
                }
            }
        }

        private void DisplaySpells(List<SpellData> spells)
        {
            // Clear existing spell displays
            if (spellListContainer != null)
            {
                foreach (Transform child in spellListContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            if (spellListContainer == null)
            {
                Debug.LogWarning("[CharacterCreation] Spell list container is null, cannot display spells");
                return;
            }

            if (spells == null || spells.Count == 0)
            {
                // Show placeholder
                GameObject placeholder = new GameObject("NoSpells");
                placeholder.transform.SetParent(spellListContainer, false);

                TextMeshProUGUI text = placeholder.AddComponent<TextMeshProUGUI>();
                text.text = "No starting spells available";
                text.fontSize = 14;
                text.color = new Color(0.5f, 0.5f, 0.5f);
                return;
            }

            // Create spell entries
            foreach (var spell in spells)
            {
                CreateSpellEntry(spell);
            }
        }

        private void CreateSpellEntry(SpellData spell)
        {
            if (spellListContainer == null)
            {
                Debug.LogWarning("[CharacterCreation] Spell list container is null, cannot create spell entry");
                return;
            }

            GameObject spellObj = new GameObject($"Spell_{spell.name}");
            spellObj.transform.SetParent(spellListContainer, false);

            RectTransform rect = spellObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 60);

            // Background
            Image bg = spellObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);

            // Spell name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(spellObj.transform, false);

            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.02f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.9f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = spell.name;
            nameText.fontSize = 14;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = spell.GetElementColor();

            // AP Cost
            GameObject apObj = new GameObject("AP");
            apObj.transform.SetParent(spellObj.transform, false);

            RectTransform apRect = apObj.AddComponent<RectTransform>();
            apRect.anchorMin = new Vector2(0.5f, 0.5f);
            apRect.anchorMax = new Vector2(0.7f, 0.9f);
            apRect.offsetMin = Vector2.zero;
            apRect.offsetMax = Vector2.zero;

            TextMeshProUGUI apText = apObj.AddComponent<TextMeshProUGUI>();
            apText.text = spell.GetFormattedAPCost();
            apText.fontSize = 12;
            apText.color = new Color(0.3f, 0.7f, 1f);

            // Range
            GameObject rangeObj = new GameObject("Range");
            rangeObj.transform.SetParent(spellObj.transform, false);

            RectTransform rangeRect = rangeObj.AddComponent<RectTransform>();
            rangeRect.anchorMin = new Vector2(0.7f, 0.5f);
            rangeRect.anchorMax = new Vector2(0.98f, 0.9f);
            rangeRect.offsetMin = Vector2.zero;
            rangeRect.offsetMax = Vector2.zero;

            TextMeshProUGUI rangeText = rangeObj.AddComponent<TextMeshProUGUI>();
            rangeText.text = spell.GetFormattedRange();
            rangeText.fontSize = 12;
            rangeText.color = new Color(0.7f, 0.7f, 0.7f);

            // Description
            GameObject descObj = new GameObject("Description");
            descObj.transform.SetParent(spellObj.transform, false);

            RectTransform descRect = descObj.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.02f, 0.1f);
            descRect.anchorMax = new Vector2(0.98f, 0.5f);
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;

            TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
            descText.text = spell.description;
            descText.fontSize = 11;
            descText.color = new Color(0.6f, 0.6f, 0.6f);
        }

        private void OnGenderChanged(bool isMaleSelected)
        {
            isMale = isMaleSelected;
            Debug.Log($"[CharacterCreation] Gender changed to: {(isMale ? "Male" : "Female")}");

            // Update character preview if a class is selected
            if (selectedClassId > 0)
            {
                UpdateCharacterPreview(selectedClassId, isMale);
            }
        }

        private void OnNameChanged(string name)
        {
            UpdateCreateButtonState();
        }

        private void UpdateCreateButtonState()
        {
            bool canCreate = selectedClassId > 0 &&
                            !string.IsNullOrEmpty(nameInput.text) &&
                            nameInput.text.Length >= 2;

            createButton.interactable = canCreate;

            if (!canCreate)
            {
                if (selectedClassId <= 0)
                    SetStatus("Select a class to continue", Color.yellow);
                else if (string.IsNullOrEmpty(nameInput.text))
                    SetStatus("Enter a character name", Color.yellow);
                else if (nameInput.text.Length < 2)
                    SetStatus("Name must be at least 2 characters", Color.yellow);
            }
            else
            {
                SetStatus("Ready to create character", Color.green);
            }
        }

        private void GenerateRandomName()
        {
            string[] prefixes = { "Brave", "Swift", "Dark", "Light", "Fire", "Ice", "Storm", "Shadow", "Moon", "Sun" };
            string[] suffixes = { "blade", "heart", "soul", "strike", "walker", "dancer", "keeper", "seeker", "hunter", "guard" };

            string randomName = prefixes[UnityEngine.Random.Range(0, prefixes.Length)] +
                               suffixes[UnityEngine.Random.Range(0, suffixes.Length)];

            nameInput.text = randomName;
        }

        private void CreateCharacter()
        {
            if (isCreating || selectedClassId <= 0 || string.IsNullOrEmpty(nameInput.text))
                return;

            StartCoroutine(CreateCharacterCoroutine());
        }

        private IEnumerator CreateCharacterCoroutine()
        {
            isCreating = true;
            createButton.interactable = false;
            SetStatus("Creating character...", Color.white);

            string baseUrl = useLocalBackend ? localBackendUrl : backendUrl;
            string url = $"{baseUrl}/api/characters";

            CharacterCreationRequest requestData = new CharacterCreationRequest
            {
                name = nameInput.text,
                classId = selectedClassId,
                sex = isMale
            };

            string jsonData = JsonUtility.ToJson(requestData);

            Debug.Log($"[CharacterCreation] Creating character: {jsonData}");

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    SetStatus("Character created successfully!", Color.green);
                    Debug.Log($"[CharacterCreation] Character created: {request.downloadHandler.text}");

                    // Parse response to get character ID if needed
                    try
                    {
                        var response = JsonUtility.FromJson<CharacterCreationResponse>(request.downloadHandler.text);
                        OnCharacterCreated?.Invoke(response.character.id);
                    }
                    catch
                    {
                        OnCharacterCreated?.Invoke(0);
                    }

                    // Return to character selection
                    yield return new WaitForSeconds(1f);
                    UIManager.Instance.ShowScreen(ScreenType.CharacterSelection);
                }
                else
                {
                    string error = request.error;
                    if (request.responseCode == 409)
                        error = "Character name already exists";
                    else if (request.responseCode == 403)
                        error = "Maximum character limit reached";

                    SetStatus($"Failed: {error}", Color.red);
                    Debug.LogError($"[CharacterCreation] Failed to create character: {error}");
                    createButton.interactable = true;
                }
            }

            isCreating = false;
        }

        private void OnCancelButtonClicked()
        {
            OnCancelClicked?.Invoke();
            UIManager.Instance.ShowScreen(ScreenType.CharacterSelection);
        }

        private void SetStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
        }

        [Serializable]
        private class CharacterCreationRequest
        {
            public string name;
            public int classId;
            public bool sex;
        }

        [Serializable]
        private class CharacterCreationResponse
        {
            public CreatedCharacter character;
        }

        [Serializable]
        private class CreatedCharacter
        {
            public int id;
            public string name;
            public int classId;
            public bool sex;
        }
    }

    /// <summary>
    /// Individual class button in the selection grid
    /// </summary>
    public class ClassButton : MonoBehaviour
    {
        private int classId;
        private ClassData classData;
        private bool isSelected;

        private Image background;
        private Image icon;
        private TextMeshProUGUI nameText;
        private Image selectionBorder;
        private Button button;

        public int ClassId => classId;
        public event Action<int> OnClicked;

        public void Initialize(int id)
        {
            classId = id;
            CreateUI();
        }

        private void CreateUI()
        {
            // Background
            background = gameObject.AddComponent<Image>();
            background.color = new Color(0.4f, 0.4f, 0.45f, 0.95f);  // Much brighter for visibility

            // Add permanent outline for better visibility
            Outline buttonOutline = gameObject.AddComponent<Outline>();
            buttonOutline.effectColor = new Color(0.7f, 0.7f, 0.7f, 1f);  // Light gray outline
            buttonOutline.effectDistance = new Vector2(2, -2);

            // Button
            button = gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => OnClicked?.Invoke(classId));

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.3f);
            iconRect.anchorMax = new Vector2(0.9f, 0.8f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            icon = iconObj.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.color = new Color(0.8f, 0.8f, 0.8f);  // Brighter icon for better visibility

            // Name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(transform, false);

            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(1, 0.3f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = $"Class {classId}";
            nameText.fontSize = 14;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;

            // Selection border
            GameObject borderObj = new GameObject("SelectionBorder");
            borderObj.transform.SetParent(transform, false);

            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-2, -2);
            borderRect.offsetMax = new Vector2(2, 2);

            selectionBorder = borderObj.AddComponent<Image>();
            selectionBorder.color = Color.yellow;
            selectionBorder.raycastTarget = false;

            Outline outline = borderObj.AddComponent<Outline>();
            outline.effectColor = Color.yellow;
            outline.effectDistance = new Vector2(2, 2);

            selectionBorder.gameObject.SetActive(false);
        }

        public void SetClassData(ClassData data, ClassSpriteManager spriteManager)
        {
            Debug.Log($"[ClassButton] SetClassData called for classId {classId}. Data is null: {data == null}");

            classData = data;

            if (data != null)
            {
                Debug.Log($"[ClassButton] Setting class data: ID={data.id}, Name={data.name}");

                nameText.text = data.name;
                background.color = data.GetClassColor() * 0.8f;  // Increased from 0.5f for better visibility

                Debug.Log($"[ClassButton] Name text updated to: {nameText.text}, Background color: {background.color}");

                if (spriteManager != null)
                {
                    Sprite classSprite = spriteManager.GetClassIcon(data.id);
                    if (classSprite != null)
                    {
                        icon.sprite = classSprite;
                        icon.color = Color.white;
                        Debug.Log($"[ClassButton] Icon sprite set successfully for {data.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ClassButton] No icon sprite found for class {data.id} ({data.name})");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ClassButton] SpriteManager is null for class {data.id}");
                }
            }
            else
            {
                Debug.LogError($"[ClassButton] SetClassData called with null data for button {classId}!");
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            selectionBorder.gameObject.SetActive(selected);

            if (selected && classData != null)
            {
                background.color = classData.GetClassColor() * 1.0f;  // Full brightness when selected
            }
            else if (classData != null)
            {
                background.color = classData.GetClassColor() * 0.8f;  // Bright when not selected
            }
        }
    }
}