using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using GOFUS.Core;
using GOFUS.Networking;

namespace GOFUS.UI.Screens
{
    /// <summary>
    /// Character selection screen for choosing or creating characters
    /// </summary>
    public class CharacterSelectionScreen : UIScreen
    {
        public const int MAX_CHARACTERS = 5;

        [Header("Character Display")]
        [SerializeField] private Transform characterSlotsContainer;
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
            characterSlots = new List<CharacterSlot>();
            loadedCharacters = new List<CharacterData>();
            CreateUI();
            SetupEventHandlers();
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
            titleText.fontSize = 32;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
        }

        private void CreateCharacterSlotsContainer(Transform parent)
        {
            GameObject container = new GameObject("CharacterSlots");
            container.transform.SetParent(parent, false);

            characterSlotsContainer = container.transform;

            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.3f);
            rect.anchorMax = new Vector2(0.9f, 0.85f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

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
        }

        private void CreateCharacterSlot(int index)
        {
            GameObject slotObj = new GameObject($"CharacterSlot_{index}");
            slotObj.transform.SetParent(characterSlotsContainer, false);

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
            createButton.onClick.AddListener(() => OnCreateCharacter?.Invoke());

            // Delete button
            deleteButton = CreateButton("Delete", parent,
                new Vector2(0.65f, 0.15f), new Vector2(0.8f, 0.25f));
            deleteButton.onClick.AddListener(DeleteSelectedCharacter);
            deleteButton.interactable = false;

            // Refresh button
            refreshButton = CreateButton("Refresh", parent,
                new Vector2(0.85f, 0.85f), new Vector2(0.95f, 0.9f));
            refreshButton.onClick.AddListener(RequestRefresh);

            // Back button
            backButton = CreateButton("Back", parent,
                new Vector2(0.05f, 0.05f), new Vector2(0.15f, 0.1f));
            backButton.onClick.AddListener(() => UIManager.Instance?.GoBack());
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
            buttonText.fontSize = 16;
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
            selectedCharacterInfo.fontSize = 14;
            selectedCharacterInfo.color = Color.white;
        }

        private void SetupEventHandlers()
        {
            // Button handlers are set up in CreateControlButtons
        }

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
            if (selectedCharacterId > 0)
            {
                OnPlayCharacter?.Invoke(selectedCharacterId);
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
            OnRefreshRequested?.Invoke();
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

        [Header("UI Elements")]
        private Image background;
        private Image portrait;
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

            // Name text
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(transform, false);
            nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.fontSize = 18;
            nameText.alignment = TextAlignmentOptions.Center;

            // Level text
            GameObject levelObj = new GameObject("Level");
            levelObj.transform.SetParent(transform, false);
            levelText = levelObj.AddComponent<TextMeshProUGUI>();
            levelText.fontSize = 14;
            levelText.alignment = TextAlignmentOptions.Center;

            // Class text
            GameObject classObj = new GameObject("Class");
            classObj.transform.SetParent(transform, false);
            classText = classObj.AddComponent<TextMeshProUGUI>();
            classText.fontSize = 14;
            classText.alignment = TextAlignmentOptions.Center;

            // Selection border (hidden by default)
            selectionBorder = new GameObject("SelectionBorder");
            selectionBorder.transform.SetParent(transform, false);
            Image borderImage = selectionBorder.AddComponent<Image>();
            borderImage.color = Color.yellow;
            selectionBorder.SetActive(false);
        }

        public void SetCharacterData(CharacterData data)
        {
            characterData = data;

            if (data != null)
            {
                if (nameText) nameText.text = data.Name;
                if (levelText) levelText.text = $"Level {data.Level}";
                if (classText) classText.text = data.Class;
                background.color = new Color(0.3f, 0.3f, 0.3f, 0.9f);
            }
            else
            {
                Clear();
            }
        }

        public void Clear()
        {
            characterData = null;
            if (nameText) nameText.text = "Empty Slot";
            if (levelText) levelText.text = "";
            if (classText) classText.text = "";
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
        public string Class;
        public string Gender;
        public string LastPlayed;
        public int Experience;
        public int MapId;
    }
}