using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GOFUS.Core;

namespace GOFUS.UI.Screens
{
    /// <summary>
    /// Main menu screen with all game navigation options
    /// </summary>
    public class MainMenuScreen : UIScreen
    {
        [Header("Main Buttons")]
        public Button PlayButton { get; private set; }
        public Button InventoryButton { get; private set; }
        public Button SkillsButton { get; private set; }
        public Button QuestButton { get; private set; }
        public Button GuildButton { get; private set; }
        public Button SettingsButton { get; private set; }
        public Button ShopButton { get; private set; }
        public Button LogoutButton { get; private set; }
        public Button QuitButton { get; private set; }

        [Header("Player Info")]
        private TextMeshProUGUI playerNameText;
        private TextMeshProUGUI playerLevelText;
        private TextMeshProUGUI playerClassText;
        private TextMeshProUGUI playerGuildText;
        private Image playerPortrait;

        [Header("Notifications")]
        private Dictionary<string, int> notifications;
        private Dictionary<Button, GameObject> notificationBadges;

        [Header("Status")]
        private TextMeshProUGUI onlineStatusText;
        private TextMeshProUGUI serverTimeText;
        private TextMeshProUGUI playTimeText;
        private TextMeshProUGUI versionText;

        [Header("Quick Actions")]
        private Dictionary<int, Action> quickActions;
        private List<Button> quickActionButtons;

        [Header("Keyboard Shortcuts")]
        private Dictionary<KeyCode, Action> keyboardShortcuts;

        // State
        private bool isLoading = false;
        private float sessionPlayTime = 0;

        // Properties
        public string PlayerName { get; private set; }
        public int PlayerLevel { get; private set; }
        public string PlayerClass { get; private set; }
        public string PlayerGuild { get; private set; }
        public int QuestNotificationCount => GetNotificationCount("Quest");
        public int GuildNotificationCount => GetNotificationCount("Guild");
        public bool HasShopNotification => GetNotificationCount("Shop") > 0;
        public bool IsOnline { get; private set; }
        public int OnlinePlayerCount { get; private set; }
        public string ServerTime { get; private set; }
        public string PlayTimeFormatted { get; private set; }
        public string Version { get; private set; }
        public string BuildNumber { get; private set; }

        // Events
        public event Action OnPlayClicked;
        public event Action OnInventoryClicked;
        public event Action OnSkillsClicked;
        public event Action OnQuestClicked;
        public event Action OnGuildClicked;
        public event Action OnSettingsClicked;
        public event Action OnShopClicked;
        public event Func<bool> OnLogoutConfirmationRequested;
        public event Action OnLogoutConfirmed;
        public event Func<bool> OnQuitConfirmationRequested;
        public event Action OnQuitConfirmed;

        public override void Initialize()
        {
            base.Initialize();
            notifications = new Dictionary<string, int>();
            notificationBadges = new Dictionary<Button, GameObject>();
            quickActions = new Dictionary<int, Action>();
            quickActionButtons = new List<Button>();
            keyboardShortcuts = new Dictionary<KeyCode, Action>();

            CreateUI();
            SetupEventHandlers();
            RegisterKeyboardShortcuts();
        }

        private void CreateUI()
        {
            // Main container
            GameObject container = CreateContainer("MainMenuContainer", transform);

            // Create menu sections
            CreatePlayerInfoPanel(container.transform);
            CreateMainButtons(container.transform);
            CreateQuickActionBar(container.transform);
            CreateStatusBar(container.transform);
        }

        private GameObject CreateContainer(string name, Transform parent)
        {
            GameObject container = new GameObject(name);
            container.transform.SetParent(parent, false);

            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return container;
        }

        private void CreatePlayerInfoPanel(Transform parent)
        {
            GameObject panel = new GameObject("PlayerInfoPanel");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.02f, 0.85f);
            rect.anchorMax = new Vector2(0.25f, 0.98f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Player portrait placeholder
            GameObject portraitObj = new GameObject("Portrait");
            portraitObj.transform.SetParent(panel.transform, false);

            RectTransform portraitRect = portraitObj.AddComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.05f, 0.3f);
            portraitRect.anchorMax = new Vector2(0.35f, 0.9f);
            portraitRect.offsetMin = Vector2.zero;
            portraitRect.offsetMax = Vector2.zero;

            playerPortrait = portraitObj.AddComponent<Image>();
            playerPortrait.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            // Player info texts
            float textX = 0.4f;
            playerNameText = CreateInfoText("PlayerName", panel.transform,
                new Vector2(textX, 0.7f), new Vector2(0.95f, 0.9f), "Player Name", 16, true);

            playerLevelText = CreateInfoText("PlayerLevel", panel.transform,
                new Vector2(textX, 0.5f), new Vector2(0.95f, 0.7f), "Level 1", 14);

            playerClassText = CreateInfoText("PlayerClass", panel.transform,
                new Vector2(textX, 0.3f), new Vector2(0.95f, 0.5f), "Class", 14);

            playerGuildText = CreateInfoText("PlayerGuild", panel.transform,
                new Vector2(textX, 0.1f), new Vector2(0.95f, 0.3f), "No Guild", 12);
        }

        private TextMeshProUGUI CreateInfoText(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, string defaultText, int fontSize, bool bold = false)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = defaultText;
            text.fontSize = fontSize;
            text.color = Color.white;
            if (bold) text.fontStyle = FontStyles.Bold;

            return text;
        }

        private void CreateMainButtons(Transform parent)
        {
            GameObject buttonPanel = new GameObject("ButtonPanel");
            buttonPanel.transform.SetParent(parent, false);

            RectTransform rect = buttonPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.35f, 0.2f);
            rect.anchorMax = new Vector2(0.65f, 0.8f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            GridLayoutGroup grid = buttonPanel.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(180, 60);
            grid.spacing = new Vector2(10, 10);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            // Create main navigation buttons
            PlayButton = CreateMenuButton("Play", buttonPanel.transform, ClickPlay);
            InventoryButton = CreateMenuButton("Inventory", buttonPanel.transform, ClickInventory);
            SkillsButton = CreateMenuButton("Skills", buttonPanel.transform, ClickSkills);
            QuestButton = CreateMenuButton("Quests", buttonPanel.transform, ClickQuest);
            GuildButton = CreateMenuButton("Guild", buttonPanel.transform, ClickGuild);
            ShopButton = CreateMenuButton("Shop", buttonPanel.transform, ClickShop);
            SettingsButton = CreateMenuButton("Settings", buttonPanel.transform, ClickSettings);
            LogoutButton = CreateMenuButton("Logout", buttonPanel.transform, ClickLogout);
            QuitButton = CreateMenuButton("Quit Game", buttonPanel.transform, ClickQuit);
        }

        private Button CreateMenuButton(string text, Transform parent, Action onClick)
        {
            GameObject buttonObj = new GameObject(text + "Button");
            buttonObj.transform.SetParent(parent, false);

            Image bg = buttonObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() => onClick());

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontSize = 16;
            buttonText.color = Color.white;

            // Create notification badge (hidden by default)
            GameObject badge = CreateNotificationBadge(buttonObj.transform);
            notificationBadges[button] = badge;

            return button;
        }

        private GameObject CreateNotificationBadge(Transform parent)
        {
            GameObject badge = new GameObject("NotificationBadge");
            badge.transform.SetParent(parent, false);

            RectTransform rect = badge.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.7f, 0.7f);
            rect.anchorMax = new Vector2(0.95f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = badge.AddComponent<Image>();
            bg.color = Color.red;

            GameObject textObj = new GameObject("Count");
            textObj.transform.SetParent(badge.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI countText = textObj.AddComponent<TextMeshProUGUI>();
            countText.text = "0";
            countText.alignment = TextAlignmentOptions.Center;
            countText.fontSize = 12;
            countText.color = Color.white;

            badge.SetActive(false);
            return badge;
        }

        private void CreateQuickActionBar(Transform parent)
        {
            GameObject quickBar = new GameObject("QuickActionBar");
            quickBar.transform.SetParent(parent, false);

            RectTransform rect = quickBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.3f, 0.05f);
            rect.anchorMax = new Vector2(0.7f, 0.15f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout = quickBar.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Create quick action slots
            for (int i = 1; i <= 5; i++)
            {
                Button quickButton = CreateQuickActionButton(i, quickBar.transform);
                quickActionButtons.Add(quickButton);
            }
        }

        private Button CreateQuickActionButton(int slot, Transform parent)
        {
            GameObject buttonObj = new GameObject($"QuickAction_{slot}");
            buttonObj.transform.SetParent(parent, false);

            Image bg = buttonObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = bg;

            int slotIndex = slot;
            button.onClick.AddListener(() => ExecuteQuickAction(slotIndex));

            // Slot number text
            GameObject textObj = new GameObject("SlotText");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI slotText = textObj.AddComponent<TextMeshProUGUI>();
            slotText.text = slot.ToString();
            slotText.alignment = TextAlignmentOptions.Center;
            slotText.fontSize = 20;
            slotText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            return button;
        }

        private void CreateStatusBar(Transform parent)
        {
            GameObject statusBar = new GameObject("StatusBar");
            statusBar.transform.SetParent(parent, false);

            RectTransform rect = statusBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0.04f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = statusBar.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);

            // Online status
            onlineStatusText = CreateStatusText("OnlineStatus", statusBar.transform,
                new Vector2(0.02f, 0.1f), new Vector2(0.15f, 0.9f), "Online: 0 players");

            // Server time
            serverTimeText = CreateStatusText("ServerTime", statusBar.transform,
                new Vector2(0.4f, 0.1f), new Vector2(0.6f, 0.9f), "00:00:00");

            // Play time
            playTimeText = CreateStatusText("PlayTime", statusBar.transform,
                new Vector2(0.7f, 0.1f), new Vector2(0.85f, 0.9f), "Play Time: 00:00:00");

            // Version
            versionText = CreateStatusText("Version", statusBar.transform,
                new Vector2(0.88f, 0.1f), new Vector2(0.98f, 0.9f), "v0.0.0");
        }

        private TextMeshProUGUI CreateStatusText(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, string defaultText)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = defaultText;
            text.fontSize = 10;
            text.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            text.alignment = TextAlignmentOptions.Center;

            return text;
        }

        private void SetupEventHandlers()
        {
            // Button handlers are set up in CreateMenuButton
        }

        private void RegisterKeyboardShortcuts()
        {
            keyboardShortcuts[KeyCode.I] = ClickInventory;
            keyboardShortcuts[KeyCode.K] = ClickSkills;
            keyboardShortcuts[KeyCode.Q] = ClickQuest;
            keyboardShortcuts[KeyCode.G] = ClickGuild;
            keyboardShortcuts[KeyCode.Escape] = ClickSettings;
        }

        private void Update()
        {
            // Handle keyboard shortcuts
            if (!isLoading)
            {
                foreach (var kvp in keyboardShortcuts)
                {
                    if (Input.GetKeyDown(kvp.Key))
                    {
                        kvp.Value?.Invoke();
                    }
                }
            }

            // Update play time
            sessionPlayTime += Time.deltaTime;
            UpdatePlayTime(sessionPlayTime);
        }

        // Button Click Handlers
        public void ClickPlay()
        {
            if (!isLoading) OnPlayClicked?.Invoke();
        }

        public void ClickInventory()
        {
            if (!isLoading) OnInventoryClicked?.Invoke();
        }

        public void ClickSkills()
        {
            if (!isLoading) OnSkillsClicked?.Invoke();
        }

        public void ClickQuest()
        {
            if (!isLoading) OnQuestClicked?.Invoke();
        }

        public void ClickGuild()
        {
            if (!isLoading) OnGuildClicked?.Invoke();
        }

        public void ClickShop()
        {
            if (!isLoading) OnShopClicked?.Invoke();
        }

        public void ClickSettings()
        {
            if (!isLoading) OnSettingsClicked?.Invoke();
        }

        public void ClickLogout()
        {
            if (isLoading) return;

            bool confirmed = OnLogoutConfirmationRequested?.Invoke() ?? false;
            if (confirmed)
            {
                OnLogoutConfirmed?.Invoke();
            }
        }

        public void ClickQuit()
        {
            if (isLoading) return;

            bool confirmed = OnQuitConfirmationRequested?.Invoke() ?? false;
            if (confirmed)
            {
                OnQuitConfirmed?.Invoke();
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }
        }

        // Player Info
        public void UpdatePlayerInfo(PlayerMenuInfo info)
        {
            PlayerName = info.Name;
            PlayerLevel = info.Level;
            PlayerClass = info.Class;
            PlayerGuild = info.Guild;

            if (playerNameText) playerNameText.text = info.Name;
            if (playerLevelText) playerLevelText.text = $"Level {info.Level}";
            if (playerClassText) playerClassText.text = info.Class;
            if (playerGuildText) playerGuildText.text = info.Guild ?? "No Guild";
        }

        // Notifications
        public void SetQuestNotification(int count)
        {
            SetNotification("Quest", count);
            UpdateBadge(QuestButton, count);
        }

        public void SetGuildNotification(int count)
        {
            SetNotification("Guild", count);
            UpdateBadge(GuildButton, count);
        }

        public void SetShopNotification(bool hasNew)
        {
            SetNotification("Shop", hasNew ? 1 : 0);
            UpdateBadge(ShopButton, hasNew ? 1 : 0);
        }

        private void SetNotification(string key, int count)
        {
            notifications[key] = count;
        }

        private int GetNotificationCount(string key)
        {
            return notifications.ContainsKey(key) ? notifications[key] : 0;
        }

        private void UpdateBadge(Button button, int count)
        {
            if (button != null && notificationBadges.ContainsKey(button))
            {
                GameObject badge = notificationBadges[button];
                badge.SetActive(count > 0);

                var countText = badge.GetComponentInChildren<TextMeshProUGUI>();
                if (countText != null)
                {
                    countText.text = count > 99 ? "99+" : count.ToString();
                }
            }
        }

        public void ClearAllNotifications()
        {
            notifications.Clear();
            foreach (var badge in notificationBadges.Values)
            {
                badge.SetActive(false);
            }
        }

        // Quick Actions
        public void SetQuickAction(int slot, string name, Action action)
        {
            if (slot > 0 && slot <= 5)
            {
                quickActions[slot] = action;

                if (slot <= quickActionButtons.Count)
                {
                    var button = quickActionButtons[slot - 1];
                    var text = button.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        text.text = name;
                        text.fontSize = 12;
                        text.color = Color.white;
                    }
                }
            }
        }

        public void ExecuteQuickAction(int slot)
        {
            if (quickActions.ContainsKey(slot) && !isLoading)
            {
                quickActions[slot]?.Invoke();
            }
        }

        // Status
        public void SetOnlineStatus(bool online, int playerCount)
        {
            IsOnline = online;
            OnlinePlayerCount = playerCount;

            if (onlineStatusText != null)
            {
                onlineStatusText.text = online ?
                    $"Online: {playerCount} players" :
                    "Offline Mode";
                onlineStatusText.color = online ? Color.green : Color.red;
            }
        }

        public void UpdateServerTime(string time)
        {
            ServerTime = time;
            if (serverTimeText != null)
                serverTimeText.text = time;
        }

        public void UpdatePlayTime(float seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            PlayTimeFormatted = string.Format("{0:D2}:{1:D2}:{2:D2}",
                time.Hours, time.Minutes, time.Seconds);

            if (playTimeText != null)
                playTimeText.text = $"Play Time: {PlayTimeFormatted}";
        }

        public void SetVersionInfo(string version, string build)
        {
            Version = version;
            BuildNumber = build;

            if (versionText != null)
                versionText.text = $"v{version}";
        }

        public void SetLoadingState(bool loading)
        {
            isLoading = loading;

            // Disable all buttons during loading
            Button[] allButtons = { PlayButton, InventoryButton, SkillsButton, QuestButton,
                                   GuildButton, SettingsButton, ShopButton, LogoutButton, QuitButton };

            foreach (var button in allButtons)
            {
                if (button != null)
                    button.interactable = !loading;
            }
        }

        public bool HasShortcut(KeyCode key)
        {
            return keyboardShortcuts.ContainsKey(key);
        }
    }

    [Serializable]
    public class PlayerMenuInfo
    {
        public string Name;
        public int Level;
        public string Class;
        public string Guild;
    }
}