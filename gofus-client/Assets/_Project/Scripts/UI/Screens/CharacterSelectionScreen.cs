using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using GOFUS.Core;
using GOFUS.UI;

namespace GOFUS.UI.Screens
{
    /// <summary>
    /// Character selection screen for choosing or creating characters
    /// </summary>
    public class CharacterSelectionScreen : UIScreen
    {
        public const int MAX_CHARACTERS = 5;

        [Header("Character Display")]
        [SerializeField] private RectTransform characterSlotsContainer;
        [SerializeField] private GameObject characterSlotPrefab;
        private List<CharacterSlot> characterSlots;
        private List<CharacterData> loadedCharacters;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button createButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button backButton;

        [Header("Sorting and Filtering")]
        [SerializeField] private TMP_Dropdown sortDropdown;
        [SerializeField] private TMP_Dropdown classFilterDropdown;
        private string currentClassFilter = "All";
        private SortMode currentSortMode = SortMode.Level;

        [Header("Selection State")]
        private int selectedSlotIndex = -1;
        private int selectedCharacterId = -1;

        [Header("Info Display")]
        [SerializeField] private TextMeshProUGUI selectedCharacterInfo;
        [SerializeField] private Image selectedCharacterPreview;

        [Header("Backend Integration")]
        private string backendUrl = "https://gofus-backend.vercel.app";
        private string localBackendUrl = "http://localhost:3000";
        private bool useLocalBackend = false;
        private bool useMockData = false; // Force mock data for testing
        private string jwtToken;
        private bool isLoading = false;
        private int retryCount = 0;
        private const int MAX_RETRIES = 3;
        private TextMeshProUGUI statusText;
        private ClassSpriteManager classSpriteManager;

        // Properties
        public int MaxCharacterSlots => MAX_CHARACTERS;
        public List<CharacterSlot> CharacterSlots => characterSlots;
        public int CharacterCount => loadedCharacters?.Count ?? 0;
        public int SelectedSlotIndex => selectedSlotIndex;
        public int SelectedCharacterId => selectedCharacterId;
        public bool CanPlay => selectedCharacterId > 0;
        public bool CanCreateNew => CharacterCount < MAX_CHARACTERS;
        public int AvailableSlots => MAX_CHARACTERS - CharacterCount;
        public bool ShowEmptySlots => true;

        // Events
        public event Action<int> OnCharacterSelected;
        public event Action<int> OnPlayCharacter;
        public event Action OnCreateCharacter;
        public event Func<int, bool> OnDeleteConfirmationRequested;
        public event Action<int> OnCharacterDeleted;
        public event Action OnRefreshRequested;
        public event Action OnLogoutConfirmed;

        private enum SortMode
        {
            Level,
            LastPlayed,
            Name,
            Class
        }

        public override void Initialize()
        {
            base.Initialize();

            Debug.Log("[CharacterSelection] Initializing...");

            // Initialize ClassSpriteManager
            classSpriteManager = ClassSpriteManager.Instance;
            classSpriteManager.LoadClassSprites();
            Debug.Log($"[CharacterSelection] ClassSpriteManager initialized with {classSpriteManager.LoadedSpriteCount} sprites");

            // Get JWT token from PlayerPrefs (saved during login)
            jwtToken = PlayerPrefs.GetString("jwt_token", "");
            Debug.Log($"[CharacterSelection] JWT Token: {(string.IsNullOrEmpty(jwtToken) ? "MISSING" : $"Found ({jwtToken.Length} chars)")}");

            // Check if we should use local backend
            useLocalBackend = PlayerPrefs.GetInt("use_local_backend", 0) == 1;
            Debug.Log($"[CharacterSelection] Using {(useLocalBackend ? "LOCAL" : "LIVE")} backend");

            characterSlots = new List<CharacterSlot>();
            loadedCharacters = new List<CharacterData>();

            try
            {
                CreateUI();
                Debug.Log("[CharacterSelection] UI Created successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CharacterSelection] Error creating UI: {e.Message}\n{e.StackTrace}");
                SetStatus("Failed to create UI", Color.red);
            }

            SetupEventHandlers();

            // Load characters from backend
            LoadCharactersFromBackend();

            Debug.Log("[CharacterSelection] Initialization complete");
        }

        private void CreateUI()
        {
            // Create main container
            GameObject container = new GameObject("CharacterSelectionContainer");
            container.transform.SetParent(transform, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Title
            CreateTitle("Select Character", container.transform);

            // Character slots container
            CreateCharacterSlotsContainer(container.transform);

            // Control buttons
            CreateControlButtons(container.transform);

            // Sorting/Filtering controls
            CreateSortingControls(container.transform);

            // Character info panel
            CreateInfoPanel(container.transform);
        }

        private void CreateTitle(string text, Transform parent)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);

            RectTransform rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.3f, 0.9f);
            rect.anchorMax = new Vector2(0.7f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = text;
            titleText.fontSize = 48;  // Increased from 32
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
        }

        private void CreateCharacterSlotsContainer(Transform parent)
        {
            GameObject container = new GameObject("CharacterSlots");
            container.transform.SetParent(parent, false);

            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.3f);
            rect.anchorMax = new Vector2(0.9f, 0.85f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Store the RectTransform reference
            characterSlotsContainer = rect;

            // Add grid layout
            GridLayoutGroup grid = container.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(300, 120);
            grid.spacing = new Vector2(20, 20);
            grid.padding = new RectOffset(20, 20, 20, 20);
            grid.childAlignment = TextAnchor.UpperCenter;

            // Create initial slots
            for (int i = 0; i < MAX_CHARACTERS; i++)
            {
                CreateCharacterSlot(i);
            }

            // Force layout rebuild to ensure slots are visible immediately
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(characterSlotsContainer);
        }

        private void CreateCharacterSlot(int index)
        {
            GameObject slotObj = new GameObject($"CharacterSlot_{index}");
            slotObj.transform.SetParent(characterSlotsContainer, false);

            // Add RectTransform for proper layout in GridLayoutGroup
            RectTransform rectTransform = slotObj.AddComponent<RectTransform>();

            // CRITICAL: Set localScale to Vector3.one to prevent scaling issues
            rectTransform.localScale = Vector3.one;

            // Add LayoutElement to ensure proper sizing in GridLayoutGroup
            UnityEngine.UI.LayoutElement layoutElement = slotObj.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.preferredWidth = 300;
            layoutElement.preferredHeight = 120;
            layoutElement.minWidth = 300;
            layoutElement.minHeight = 120;

            CharacterSlot slot = slotObj.AddComponent<CharacterSlot>();
            slot.Initialize(index);
            slot.OnSlotClicked += OnSlotClicked;

            characterSlots.Add(slot);
        }

        private void CreateControlButtons(Transform parent)
        {
            // Play button
            playButton = CreateButton("Play", parent,
                new Vector2(0.4f, 0.15f), new Vector2(0.6f, 0.25f));
            playButton.onClick.AddListener(PlaySelectedCharacter);
            playButton.interactable = false;

            // Create button
            createButton = CreateButton("Create New", parent,
                new Vector2(0.2f, 0.15f), new Vector2(0.35f, 0.25f));
            createButton.onClick.AddListener(OnCreateCharacterClicked);

            // Delete button
            deleteButton = CreateButton("Delete", parent,
                new Vector2(0.65f, 0.15f), new Vector2(0.8f, 0.25f));
            deleteButton.onClick.AddListener(DeleteSelectedCharacter);
            deleteButton.interactable = false;

            // Refresh button
            refreshButton = CreateButton("Refresh", parent,
                new Vector2(0.85f, 0.85f), new Vector2(0.95f, 0.9f));
            refreshButton.onClick.AddListener(RequestRefresh);

            // Back/Logout button
            backButton = CreateButton("Logout", parent,
                new Vector2(0.05f, 0.05f), new Vector2(0.15f, 0.1f));
            backButton.onClick.AddListener(OnLogoutClicked);
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

            // Add text
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
            buttonText.fontSize = 20;  // Increased from 16
            buttonText.color = Color.white;

            return button;
        }

        private void CreateSortingControls(Transform parent)
        {
            // Sort dropdown
            GameObject sortObj = new GameObject("SortDropdown");
            sortObj.transform.SetParent(parent, false);

            RectTransform sortRect = sortObj.AddComponent<RectTransform>();
            sortRect.anchorMin = new Vector2(0.05f, 0.85f);
            sortRect.anchorMax = new Vector2(0.2f, 0.9f);
            sortRect.offsetMin = Vector2.zero;
            sortRect.offsetMax = Vector2.zero;

            sortDropdown = sortObj.AddComponent<TMP_Dropdown>();
            sortDropdown.options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("Sort by Level"),
                new TMP_Dropdown.OptionData("Sort by Last Played"),
                new TMP_Dropdown.OptionData("Sort by Name"),
                new TMP_Dropdown.OptionData("Sort by Class")
            };
            sortDropdown.onValueChanged.AddListener(OnSortModeChanged);

            // Class filter dropdown
            GameObject filterObj = new GameObject("ClassFilter");
            filterObj.transform.SetParent(parent, false);

            RectTransform filterRect = filterObj.AddComponent<RectTransform>();
            filterRect.anchorMin = new Vector2(0.25f, 0.85f);
            filterRect.anchorMax = new Vector2(0.4f, 0.9f);
            filterRect.offsetMin = Vector2.zero;
            filterRect.offsetMax = Vector2.zero;

            classFilterDropdown = filterObj.AddComponent<TMP_Dropdown>();
            UpdateClassFilterOptions();
            classFilterDropdown.onValueChanged.AddListener(OnClassFilterChanged);
        }

        private void CreateInfoPanel(Transform parent)
        {
            GameObject panel = new GameObject("InfoPanel");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.25f);
            rect.anchorMax = new Vector2(0.3f, 0.8f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Character info text
            GameObject infoObj = new GameObject("CharacterInfo");
            infoObj.transform.SetParent(panel.transform, false);

            RectTransform infoRect = infoObj.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.1f, 0.1f);
            infoRect.anchorMax = new Vector2(0.9f, 0.9f);
            infoRect.offsetMin = Vector2.zero;
            infoRect.offsetMax = Vector2.zero;

            selectedCharacterInfo = infoObj.AddComponent<TextMeshProUGUI>();
            selectedCharacterInfo.fontSize = 18;  // Increased from 14
            selectedCharacterInfo.color = Color.white;
            selectedCharacterInfo.alignment = TextAlignmentOptions.TopLeft;
            selectedCharacterInfo.text = "Select a character to view details";

            // Status text
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(parent, false);

            RectTransform statusRect = statusObj.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.2f, 0.02f);
            statusRect.anchorMax = new Vector2(0.8f, 0.08f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            statusText = statusObj.AddComponent<TextMeshProUGUI>();
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.fontSize = 18;  // Increased from 14
            statusText.color = Color.yellow;
            statusText.text = "";
        }

        private void SetupEventHandlers()
        {
            // Button handlers are set up in CreateControlButtons
        }

        // ========== BACKEND API INTEGRATION ==========

        private void LoadCharactersFromBackend()
        {
            // Check if we should use mock data for testing
            useMockData = PlayerPrefs.GetInt("use_mock_characters", 0) == 1;

            if (useMockData)
            {
                Debug.Log("[CharacterSelection] Using mock data (forced by settings)");
                SetStatus("Loading test characters...", Color.yellow);
                LoadMockCharacters();
                return;
            }

            if (string.IsNullOrEmpty(jwtToken))
            {
                SetStatus("No authentication token found - loading test characters", Color.yellow);
                Debug.LogError("[CharacterSelection] No JWT token found in PlayerPrefs");
                LoadMockCharacters();
                return;
            }

            retryCount = 0;
            StartCoroutine(LoadCharactersCoroutine());
        }

        private IEnumerator LoadCharactersCoroutine()
        {
            isLoading = true;

            // Determine which backend to use
            string baseUrl = useLocalBackend ? localBackendUrl : backendUrl;
            string url = $"{baseUrl}/api/characters";

            SetStatus($"Connecting to {(useLocalBackend ? "local" : "live")} backend...", Color.white);
            Debug.Log($"[CharacterSelection] Attempting to load characters from: {url}");
            Debug.Log($"[CharacterSelection] Using JWT token: {jwtToken.Substring(0, Math.Min(20, jwtToken.Length))}...");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                // Set headers
                request.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
                request.SetRequestHeader("Content-Type", "application/json");

                // Set timeout
                request.timeout = 10; // 10 seconds timeout

                SetStatus("Fetching character data...", Color.white);

                // Send request
                yield return request.SendWebRequest();

                Debug.Log($"[CharacterSelection] Request completed. Result: {request.result}");
                Debug.Log($"[CharacterSelection] Response Code: {request.responseCode}");

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = request.downloadHandler.text;
                        Debug.Log($"[CharacterSelection] Raw response: {json}");

                        SetStatus("Parsing character data...", Color.white);

                        // Backend returns: { "characters": [...] }
                        // Parse it directly without wrapping
                        CharacterListResponse response = JsonUtility.FromJson<CharacterListResponse>(json);

                        if (response != null && response.characters != null && response.characters.Length > 0)
                        {
                            Debug.Log($"[CharacterSelection] Successfully parsed {response.characters.Length} characters");
                            LoadCharacters(ConvertToCharacterDataList(response.characters));
                            SetStatus($"Loaded {response.characters.Length} character(s)", Color.green);
                        }
                        else
                        {
                            Debug.LogWarning("[CharacterSelection] Response parsed but no characters found");
                            LoadCharacters(new List<CharacterData>());
                            SetStatus("No characters found - create your first character!", Color.yellow);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[CharacterSelection] Failed to parse response: {e.Message}\n{e.StackTrace}");
                        Debug.LogError($"[CharacterSelection] Raw response was: {request.downloadHandler.text}");

                        HandleLoadError($"Failed to parse server response: {e.Message}", request.responseCode);
                    }
                }
                else
                {
                    string errorMessage = $"{request.error} (Code: {request.responseCode})";

                    if (request.result == UnityWebRequest.Result.ConnectionError)
                    {
                        errorMessage = "Cannot connect to server - check your internet connection";
                    }
                    else if (request.result == UnityWebRequest.Result.ProtocolError)
                    {
                        switch (request.responseCode)
                        {
                            case 401:
                                errorMessage = "Authentication failed - please login again";
                                break;
                            case 403:
                                errorMessage = "Access denied - invalid permissions";
                                break;
                            case 404:
                                errorMessage = "API endpoint not found - server may be down";
                                break;
                            case 500:
                                errorMessage = "Server error - please try again later";
                                break;
                        }
                    }

                    HandleLoadError(errorMessage, request.responseCode);
                }
            }

            isLoading = false;
        }

        private void HandleLoadError(string error, long responseCode)
        {
            Debug.LogError($"[CharacterSelection] Load error: {error} (Code: {responseCode})");

            if (retryCount < MAX_RETRIES)
            {
                retryCount++;
                SetStatus($"Failed to load characters. Retrying ({retryCount}/{MAX_RETRIES})...", Color.yellow);
                Debug.Log($"[CharacterSelection] Retrying load attempt {retryCount}/{MAX_RETRIES}");
                StartCoroutine(RetryLoadAfterDelay(2f)); // Wait 2 seconds before retry
            }
            else
            {
                SetStatus($"Backend unavailable. Loading test characters...", Color.yellow);

                // Show retry button
                if (refreshButton != null)
                {
                    refreshButton.GetComponentInChildren<TextMeshProUGUI>().text = "Retry";
                    refreshButton.GetComponent<Image>().color = Color.yellow;
                }

                // Load mock characters for testing
                Debug.Log("[CharacterSelection] Loading mock characters for testing");
                LoadMockCharacters();
            }
        }

        /// <summary>
        /// Generate mock character data for testing when backend is unavailable
        /// </summary>
        private void LoadMockCharacters()
        {
            Debug.Log("[CharacterSelection] Generating mock characters for testing...");

            List<CharacterData> mockCharacters = new List<CharacterData>();

            // Create 4 test characters with different classes
            // Using map 7411 (center map) with different starting cells
            mockCharacters.Add(new CharacterData
            {
                Id = 1001,
                Name = "TestFeca",
                Level = 50,
                ClassId = 1,
                Class = "Feca",
                ClassDescription = "Masters of protection and defensive magic",
                Gender = "Male",
                LastPlayed = "Today",
                Experience = 250000,
                MapId = 7411,
                CellId = 140, // Center of map (14x20 grid, cell ~140 is roughly center)
                Kamas = 10000
            });

            mockCharacters.Add(new CharacterData
            {
                Id = 1002,
                Name = "TestSram",
                Level = 30,
                ClassId = 4,
                Class = "Sram",
                ClassDescription = "Deadly assassins with traps",
                Gender = "Female",
                LastPlayed = "Yesterday",
                Experience = 90000,
                MapId = 7411,
                CellId = 155,
                Kamas = 5000
            });

            mockCharacters.Add(new CharacterData
            {
                Id = 1003,
                Name = "TestEni",
                Level = 25,
                ClassId = 7,
                Class = "Eniripsa",
                ClassDescription = "Powerful healers and support",
                Gender = "Female",
                LastPlayed = "2 days ago",
                Experience = 62500,
                MapId = 7411,
                CellId = 170,
                Kamas = 3000
            });

            mockCharacters.Add(new CharacterData
            {
                Id = 1004,
                Name = "TestIop",
                Level = 40,
                ClassId = 8,
                Class = "Iop",
                ClassDescription = "Fearless melee warriors",
                Gender = "Male",
                LastPlayed = "3 days ago",
                Experience = 160000,
                MapId = 7411,
                CellId = 125,
                Kamas = 7500
            });

            // Load the mock characters
            LoadCharacters(mockCharacters);
            SetStatus("Loaded 4 test characters (Backend unavailable)", Color.yellow);
            Debug.Log("[CharacterSelection] Mock characters loaded successfully");
        }

        private IEnumerator RetryLoadAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartCoroutine(LoadCharactersCoroutine());
        }

        private List<CharacterData> ConvertToCharacterDataList(BackendCharacter[] backendChars)
        {
            List<CharacterData> charList = new List<CharacterData>();

            foreach (var bc in backendChars)
            {
                // Use className from backend if available, otherwise use ClassSpriteManager
                string className = !string.IsNullOrEmpty(bc.className)
                    ? bc.className
                    : classSpriteManager.GetClassName(bc.classId);

                // Use classDescription from backend if available, otherwise use ClassSpriteManager
                string classDesc = !string.IsNullOrEmpty(bc.classDescription)
                    ? bc.classDescription
                    : classSpriteManager.GetClassDescription(bc.classId);

                charList.Add(new CharacterData
                {
                    Id = bc.id,
                    Name = bc.name,
                    Level = bc.level,
                    ClassId = bc.classId,
                    Class = className,
                    ClassDescription = classDesc,
                    Gender = bc.sex ? "Male" : "Female",
                    LastPlayed = "Today", // TODO: Calculate from timestamp
                    Experience = bc.experience,
                    MapId = bc.mapId,
                    CellId = bc.cellId,
                    Kamas = bc.kamas
                });

                Debug.Log($"[CharacterSelection] Converted character: {bc.name} (Lvl {bc.level} {className})");
            }

            return charList;
        }

        private void SetStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
        }

        // ========== BACKEND DATA CLASSES ==========

        [Serializable]
        private class BackendCharacter
        {
            public int id;
            public string name;
            public int level;
            public int classId;
            public string className;        // Enhanced API field
            public string classDescription; // Enhanced API field
            public bool sex;
            public int mapId;
            public int cellId;
            public int experience;
            public int kamas;
        }

        [Serializable]
        private class CharacterListResponse
        {
            public BackendCharacter[] characters;
        }

        // ========== ORIGINAL CHARACTER MANAGEMENT ==========

        public void LoadCharacters(List<CharacterData> characters)
        {
            loadedCharacters = characters ?? new List<CharacterData>();
            RefreshDisplay();
        }

        public void UpdateFromServer(List<CharacterData> characters)
        {
            LoadCharacters(characters);
        }

        private void RefreshDisplay()
        {
            // Clear all slots first
            foreach (var slot in characterSlots)
            {
                slot.Clear();
            }

            // Apply current filter and sort
            var displayCharacters = ApplyFilterAndSort(loadedCharacters);

            // Display characters
            for (int i = 0; i < displayCharacters.Count && i < MAX_CHARACTERS; i++)
            {
                characterSlots[i].SetCharacterData(displayCharacters[i]);
            }

            // Force layout rebuild to ensure visual updates
            if (characterSlotsContainer != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(characterSlotsContainer);
            }

            // Update button states
            UpdateButtonStates();
        }

        private List<CharacterData> ApplyFilterAndSort(List<CharacterData> characters)
        {
            var filtered = characters.AsEnumerable();

            // Apply class filter
            if (currentClassFilter != "All")
            {
                filtered = filtered.Where(c => c.Class == currentClassFilter);
            }

            // Apply sorting
            switch (currentSortMode)
            {
                case SortMode.Level:
                    filtered = filtered.OrderByDescending(c => c.Level);
                    break;
                case SortMode.LastPlayed:
                    filtered = filtered.OrderByDescending(c => c.LastPlayed);
                    break;
                case SortMode.Name:
                    filtered = filtered.OrderBy(c => c.Name);
                    break;
                case SortMode.Class:
                    filtered = filtered.OrderBy(c => c.Class).ThenByDescending(c => c.Level);
                    break;
            }

            return filtered.ToList();
        }

        public CharacterSlot GetCharacterSlot(int index)
        {
            return index >= 0 && index < characterSlots.Count ? characterSlots[index] : null;
        }

        public void SelectCharacter(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= characterSlots.Count)
                return;

            // Deselect previous
            if (selectedSlotIndex >= 0 && selectedSlotIndex < characterSlots.Count)
            {
                characterSlots[selectedSlotIndex].SetSelected(false);
            }

            // Select new
            selectedSlotIndex = slotIndex;
            var slot = characterSlots[slotIndex];

            if (slot.HasCharacter)
            {
                slot.SetSelected(true);
                selectedCharacterId = slot.CharacterData.Id;
                UpdateCharacterInfo(slot.CharacterData);
                OnCharacterSelected?.Invoke(selectedCharacterId);
            }
            else
            {
                selectedCharacterId = -1;
            }

            UpdateButtonStates();
        }

        private void OnSlotClicked(int slotIndex)
        {
            SelectCharacter(slotIndex);
        }

        private void UpdateCharacterInfo(CharacterData character)
        {
            if (selectedCharacterInfo != null && character != null)
            {
                selectedCharacterInfo.text = $"<b>{character.Name}</b>\n" +
                                            $"Level {character.Level} {character.Class}\n" +
                                            $"Gender: {character.Gender}\n" +
                                            $"Last Played: {character.LastPlayed}\n";
            }
        }

        public void PlaySelectedCharacter()
        {
            Debug.Log($"[CharacterSelection] Play button clicked. Selected ID: {selectedCharacterId}");

            if (selectedCharacterId > 0)
            {
                // Save selected character ID
                PlayerPrefs.SetInt("selected_character_id", selectedCharacterId);
                PlayerPrefs.Save();

                SetStatus("Entering game world...", Color.green);
                Debug.Log($"[CharacterSelection] Character ID {selectedCharacterId} saved to PlayerPrefs");

                OnPlayCharacter?.Invoke(selectedCharacterId);

                // Get the selected character data
                CharacterData selectedChar = loadedCharacters.Find(c => c.Id == selectedCharacterId);
                if (selectedChar != null)
                {
                    SetStatus("Entering game world...", Color.green);

                    // Get GameHUD and initialize character stats
                    GameHUD gameHUD = UIManager.Instance.GetScreen<GameHUD>(ScreenType.GameHUD);
                    if (gameHUD != null)
                    {
                        // Initialize character stats
                        gameHUD.UpdateLevel(selectedChar.Level);
                        gameHUD.UpdateHealth(100, 100); // Default health values
                        gameHUD.UpdateMana(50, 50); // Default mana values
                        gameHUD.UpdateExperience(selectedChar.Experience, 1000); // Default exp requirement

                        // Subscribe to OnScreenShown event (one-time)
                        System.Action<UIScreen> onGameHUDShown = null;
                        onGameHUDShown = (screen) =>
                        {
                            if (screen is GameHUD)
                            {
                                Debug.Log($"[CharacterSelection] GameHUD shown! Loading map {selectedChar.MapId}, cell {selectedChar.CellId}");
                                GameHUD hud = (GameHUD)screen;
                                
                                // Set character data (class, gender, name) BEFORE loading map/positioning
                                bool isMale = selectedChar.Gender.ToLower() == "male" || selectedChar.Gender.ToLower() == "m";
                                hud.SetCharacterData(selectedChar.ClassId, isMale, selectedChar.Name);
                                
                                // Now load map and position character
                                hud.LoadMap(selectedChar.MapId, selectedChar.CellId);

                                // Unsubscribe after use
                                UIManager.Instance.OnScreenShown -= onGameHUDShown;
                            }
                        };

                        UIManager.Instance.OnScreenShown += onGameHUDShown;

                        Debug.Log($"[CharacterSelection] Transitioning to GameHUD for character: {selectedChar.Name}");
                        UIManager.Instance.ShowScreen(ScreenType.GameHUD);
                    }
                    else
                    {
                        Debug.LogError("[CharacterSelection] GameHUD screen not found!");
                        SetStatus("Error: GameHUD not found", Color.red);
                    }
                }
                else
                {
                    SetStatus("Error: Character data not found", Color.red);
                    Debug.LogError($"[CharacterSelection] Character data not found for ID: {selectedCharacterId}");
                }
            }
            else
            {
                SetStatus("Please select a character first", Color.red);
                Debug.LogWarning("[CharacterSelection] Play clicked but no character selected");
            }
        }

        public bool DeleteCharacter(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= characterSlots.Count)
                return false;

            var slot = characterSlots[slotIndex];
            if (!slot.HasCharacter)
                return false;

            // Request confirmation
            bool confirmed = OnDeleteConfirmationRequested?.Invoke(slot.CharacterData.Id) ?? false;

            if (confirmed)
            {
                // Remove from loaded characters
                loadedCharacters.RemoveAll(c => c.Id == slot.CharacterData.Id);

                // Refresh display
                RefreshDisplay();

                // Notify
                OnCharacterDeleted?.Invoke(slot.CharacterData.Id);

                return true;
            }

            return false;
        }

        private void DeleteSelectedCharacter()
        {
            if (selectedSlotIndex >= 0)
            {
                DeleteCharacter(selectedSlotIndex);
            }
        }

        public void RequestRefresh()
        {
            LoadCharactersFromBackend();
            OnRefreshRequested?.Invoke();
        }

        private void OnCreateCharacterClicked()
        {
            if (!CanCreateNew)
            {
                SetStatus("Maximum character limit reached (5/5)", Color.red);
                return;
            }

            SetStatus("Opening character creation...", Color.white);
            Debug.Log($"[CharacterSelection] Create character clicked. Available slots: {AvailableSlots}");

            // Navigate to character creation screen
            UIManager.Instance.ShowScreen(ScreenType.CharacterCreation);

            OnCreateCharacter?.Invoke();
        }

        private void OnLogoutClicked()
        {
            // Clear saved data
            PlayerPrefs.DeleteKey("jwt_token");
            PlayerPrefs.DeleteKey("account_id");
            PlayerPrefs.DeleteKey("selected_character_id");
            PlayerPrefs.Save();

            SetStatus("Logging out...", Color.yellow);
            Debug.Log("[CharacterSelection] Logging out");

            // Go back to login
            UIManager.Instance.ShowScreen(ScreenType.Login);
            OnLogoutConfirmed?.Invoke();
        }

        private void UpdateButtonStates()
        {
            if (playButton != null)
                playButton.interactable = CanPlay;

            if (createButton != null)
                createButton.interactable = CanCreateNew;

            if (deleteButton != null)
                deleteButton.interactable = selectedCharacterId > 0;
        }

        public void SortByLevel()
        {
            currentSortMode = SortMode.Level;
            RefreshDisplay();
        }

        public void SortByLastPlayed()
        {
            currentSortMode = SortMode.LastPlayed;
            RefreshDisplay();
        }

        public void FilterByClass(string className)
        {
            currentClassFilter = className;
            RefreshDisplay();
        }

        public int GetVisibleCharacterCount()
        {
            return characterSlots.Count(s => s.HasCharacter && s.IsVisible);
        }

        private void OnSortModeChanged(int value)
        {
            currentSortMode = (SortMode)value;
            RefreshDisplay();
        }

        private void OnClassFilterChanged(int value)
        {
            if (classFilterDropdown != null && value < classFilterDropdown.options.Count)
            {
                currentClassFilter = classFilterDropdown.options[value].text;
                RefreshDisplay();
            }
        }

        private void UpdateClassFilterOptions()
        {
            if (classFilterDropdown == null) return;

            var classes = new List<string> { "All", "Iop", "Cra", "Xelor", "Eniripsa", "Sacrieur",
                                            "Sram", "Eca", "Osamodas", "Feca", "Sadida",
                                            "Panda", "Rogue", "Masq", "Foggernaut", "Eliotrope",
                                            "Huppermage", "Ouginak" };

            classFilterDropdown.options.Clear();
            foreach (var cls in classes)
            {
                classFilterDropdown.options.Add(new TMP_Dropdown.OptionData(cls));
            }
        }
    }

    /// <summary>
    /// Individual character slot in the selection screen
    /// </summary>
    public class CharacterSlot : MonoBehaviour
    {
        private int slotIndex;
        private CharacterData characterData;
        private bool isSelected;
        private ClassSpriteManager classSpriteManager;

        [Header("UI Elements")]
        private Image background;
        private Image portrait;
        private Image classIcon;
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI levelText;
        private TextMeshProUGUI classText;
        private GameObject selectionBorder;
        private Button slotButton;

        public CharacterData CharacterData => characterData;
        public bool HasCharacter => characterData != null;
        public bool IsSelected => isSelected;
        public bool IsVisible => gameObject.activeSelf;
        public string CharacterName => characterData?.Name ?? "";
        public int Level => characterData?.Level ?? 0;

        public event Action<int> OnSlotClicked;

        public void Initialize(int index)
        {
            slotIndex = index;
            classSpriteManager = ClassSpriteManager.Instance;
            CreateUI();
        }

        private void CreateUI()
        {
            // Background
            background = gameObject.AddComponent<Image>();
            background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Button
            slotButton = gameObject.AddComponent<Button>();
            slotButton.targetGraphic = background;
            slotButton.onClick.AddListener(() => OnSlotClicked?.Invoke(slotIndex));

            // Class Icon (left side)
            GameObject iconObj = new GameObject("ClassIcon");
            iconObj.transform.SetParent(transform, false);
            classIcon = iconObj.AddComponent<Image>();
            classIcon.preserveAspect = true;

            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.05f, 0.2f);
            iconRect.anchorMax = new Vector2(0.25f, 0.8f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            // Portrait (if needed for character preview)
            GameObject portraitObj = new GameObject("Portrait");
            portraitObj.transform.SetParent(transform, false);
            portrait = portraitObj.AddComponent<Image>();
            portrait.enabled = false; // Hidden by default

            RectTransform portraitRect = portraitObj.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.75f, 0.2f);
            portraitRect.anchorMax = new Vector2(0.95f, 0.8f);
            portraitRect.offsetMin = Vector2.zero;
            portraitRect.offsetMax = Vector2.zero;

            // Name text (center-top)
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(transform, false);
            nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.fontSize = 24;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontStyle = FontStyles.Bold;

            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.3f, 0.6f);
            nameRect.anchorMax = new Vector2(0.7f, 0.9f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            // Level text (center-middle)
            GameObject levelObj = new GameObject("Level");
            levelObj.transform.SetParent(transform, false);
            levelText = levelObj.AddComponent<TextMeshProUGUI>();
            levelText.fontSize = 18;
            levelText.alignment = TextAlignmentOptions.Center;

            RectTransform levelRect = levelObj.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.3f, 0.35f);
            levelRect.anchorMax = new Vector2(0.7f, 0.6f);
            levelRect.offsetMin = Vector2.zero;
            levelRect.offsetMax = Vector2.zero;

            // Class text (center-bottom)
            GameObject classObj = new GameObject("Class");
            classObj.transform.SetParent(transform, false);
            classText = classObj.AddComponent<TextMeshProUGUI>();
            classText.fontSize = 16;
            classText.alignment = TextAlignmentOptions.Center;

            RectTransform classRect = classObj.GetComponent<RectTransform>();
            classRect.anchorMin = new Vector2(0.3f, 0.1f);
            classRect.anchorMax = new Vector2(0.7f, 0.35f);
            classRect.offsetMin = Vector2.zero;
            classRect.offsetMax = Vector2.zero;

            // Selection border (hidden by default)
            selectionBorder = new GameObject("SelectionBorder");
            selectionBorder.transform.SetParent(transform, false);

            RectTransform borderRect = selectionBorder.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-2, -2);
            borderRect.offsetMax = new Vector2(2, 2);

            Image borderImage = selectionBorder.AddComponent<Image>();
            borderImage.color = Color.yellow;
            borderImage.raycastTarget = false;

            // Create outline effect
            Outline outline = selectionBorder.AddComponent<Outline>();
            outline.effectColor = Color.yellow;
            outline.effectDistance = new Vector2(2, 2);

            selectionBorder.SetActive(false);
        }

        public void SetCharacterData(CharacterData data)
        {
            characterData = data;

            if (data != null)
            {
                // Set text fields
                if (nameText) nameText.text = data.Name;
                if (levelText) levelText.text = $"Level {data.Level}";
                if (classText) classText.text = data.Class;

                // Set class icon
                if (classIcon && classSpriteManager != null)
                {
                    Sprite icon = classSpriteManager.GetClassIcon(data.ClassId);
                    if (icon != null)
                    {
                        classIcon.sprite = icon;
                        classIcon.enabled = true;
                        classIcon.color = Color.white;
                    }
                    else
                    {
                        classIcon.enabled = false;
                        Debug.LogWarning($"[CharacterSlot] No icon found for class {data.Class} (ID: {data.ClassId})");
                    }
                }

                // Set background color based on class (optional)
                background.color = GetClassColor(data.ClassId);
            }
            else
            {
                Clear();
            }
        }

        private Color GetClassColor(int classId)
        {
            // Optional: Return different colors for different classes
            switch (classId)
            {
                case 1: return new Color(0.4f, 0.3f, 0.2f, 0.9f); // Feca - Brown
                case 2: return new Color(0.2f, 0.4f, 0.3f, 0.9f); // Osamodas - Green
                case 3: return new Color(0.4f, 0.4f, 0.2f, 0.9f); // Enutrof - Gold
                case 4: return new Color(0.3f, 0.2f, 0.3f, 0.9f); // Sram - Purple
                case 5: return new Color(0.2f, 0.3f, 0.4f, 0.9f); // Xelor - Blue
                case 6: return new Color(0.3f, 0.4f, 0.2f, 0.9f); // Ecaflip - Light Green
                case 7: return new Color(0.4f, 0.2f, 0.3f, 0.9f); // Eniripsa - Pink
                case 8: return new Color(0.4f, 0.2f, 0.2f, 0.9f); // Iop - Red
                case 9: return new Color(0.2f, 0.4f, 0.4f, 0.9f); // Cra - Cyan
                case 10: return new Color(0.2f, 0.3f, 0.2f, 0.9f); // Sadida - Dark Green
                case 11: return new Color(0.3f, 0.2f, 0.2f, 0.9f); // Sacrieur - Dark Red
                case 12: return new Color(0.3f, 0.3f, 0.3f, 0.9f); // Pandawa - Gray
                default: return new Color(0.3f, 0.3f, 0.3f, 0.9f); // Default gray
            }
        }

        public void Clear()
        {
            characterData = null;
            if (nameText) nameText.text = "Empty Slot";
            if (levelText) levelText.text = "";
            if (classText) classText.text = "";
            if (classIcon) classIcon.enabled = false;
            if (portrait) portrait.enabled = false;
            background.color = new Color(0.15f, 0.15f, 0.15f, 0.6f);
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (selectionBorder != null)
                selectionBorder.SetActive(selected);

            background.color = selected ?
                new Color(0.4f, 0.4f, 0.2f, 1f) :
                (HasCharacter ? new Color(0.3f, 0.3f, 0.3f, 0.9f) : new Color(0.15f, 0.15f, 0.15f, 0.6f));
        }
    }

    /// <summary>
    /// Character data structure
    /// </summary>
    [Serializable]
    public class CharacterData
    {
        public int Id;
        public string Name;
        public int Level;
        public int ClassId;
        public string Class;
        public string ClassDescription;
        public string Gender;
        public string LastPlayed;
        public int Experience;
        public int MapId;
        public int CellId; // Cell position on map
        public int Kamas;
    }
}