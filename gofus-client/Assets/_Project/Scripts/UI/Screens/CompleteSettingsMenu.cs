using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GOFUS.UI.Screens
{
    /// <summary>
    /// Complete Settings Menu with all game options
    /// Implements comprehensive settings management with persistence
    /// </summary>
    public class CompleteSettingsMenu : UIScreen
    {
        #region Audio Properties

        public float MasterVolume { get; private set; } = 1.0f;
        public float SFXVolume { get; private set; } = 1.0f;
        public float MusicVolume { get; private set; } = 1.0f;
        public float AmbientVolume { get; private set; } = 0.8f;
        public float VoiceVolume { get; private set; } = 1.0f;
        public bool IsMuted { get; private set; }

        #endregion

        #region Graphics Properties

        public int GraphicsQuality { get; private set; }
        public string QualityPresetName { get; private set; }
        public Resolution CurrentResolution { get; private set; }
        public bool IsFullscreen { get; private set; }
        public bool VSyncEnabled { get; private set; }
        public int FrameRateLimit { get; private set; }
        public int ShadowQuality { get; private set; }
        public int TextureQuality { get; private set; }
        public int AntiAliasing { get; private set; }
        public int AnisotropicFiltering { get; private set; }

        #endregion

        #region Controls Properties

        public float MouseSensitivity { get; private set; } = 1.0f;
        public bool InvertMouseY { get; private set; }
        public bool InvertMouseX { get; private set; }
        private Dictionary<string, KeyCode> keybindings = new Dictionary<string, KeyCode>();
        private Dictionary<string, KeyCode> defaultKeybindings;

        #endregion

        #region Gameplay Properties

        public string CurrentLanguage { get; private set; } = "English";
        public bool AutoTargeting { get; private set; }
        public bool SmartCasting { get; private set; }
        public bool ShowDamageNumbers { get; private set; }
        public float UIScale { get; private set; } = 1.0f;
        public bool ShowQuestNotifications { get; private set; } = true;
        public bool ShowCombatNotifications { get; private set; } = true;
        public bool ShowItemNotifications { get; private set; } = true;

        #endregion

        #region Network Properties

        public string ServerRegion { get; private set; } = "NA-East";
        public bool LowLatencyMode { get; private set; }
        public bool PacketLossCompensation { get; private set; }

        #endregion

        #region Accessibility Properties

        public ColorblindMode ColorblindMode { get; private set; } = ColorblindMode.None;
        public bool SubtitlesEnabled { get; private set; }
        public int SubtitleSize { get; private set; } = 16;
        public bool ScreenReaderEnabled { get; private set; }
        public float ScreenReaderSpeed { get; private set; } = 1.0f;

        #endregion

        #region Profile Management

        private Dictionary<string, SettingsProfile> profiles = new Dictionary<string, SettingsProfile>();
        private string currentProfile = "Default";

        #endregion

        #region Events

        public event Action OnSettingsApplied;
        public event Action OnSettingsChanged;

        #endregion

        #region UI Elements

        private TabGroup tabGroup;
        private GameObject audioTab;
        private GameObject graphicsTab;
        private GameObject controlsTab;
        private GameObject gameplayTab;
        private GameObject networkTab;
        private GameObject accessibilityTab;

        #endregion

        #region Initialization

        public override void Initialize()
        {
            base.Initialize();
            InitializeDefaultKeybindings();
            AutoDetectGraphicsQuality();
            LoadSettings();
            CreateSettingsUI();
        }

        private void InitializeDefaultKeybindings()
        {
            defaultKeybindings = new Dictionary<string, KeyCode>
            {
                ["MoveForward"] = KeyCode.W,
                ["MoveBackward"] = KeyCode.S,
                ["MoveLeft"] = KeyCode.A,
                ["MoveRight"] = KeyCode.D,
                ["Jump"] = KeyCode.Space,
                ["Sprint"] = KeyCode.LeftShift,
                ["Crouch"] = KeyCode.LeftControl,
                ["Inventory"] = KeyCode.I,
                ["Skills"] = KeyCode.K,
                ["Quest"] = KeyCode.Q,
                ["Map"] = KeyCode.M,
                ["Chat"] = KeyCode.Return,
                ["Settings"] = KeyCode.Escape
            };

            // Initialize current keybindings with defaults
            foreach (var kvp in defaultKeybindings)
            {
                keybindings[kvp.Key] = kvp.Value;
            }
        }

        private void AutoDetectGraphicsQuality()
        {
            // Simple auto-detection based on system info
            GraphicsQuality = GetAutoDetectedQuality();
            UpdateQualityPresetName();
        }

        public int GetAutoDetectedQuality()
        {
            // Simulate quality detection based on hardware
            int vram = SystemInfo.graphicsMemorySize;
            if (vram >= 8192) return 5; // Ultra
            if (vram >= 4096) return 4; // High
            if (vram >= 2048) return 3; // Medium-High
            if (vram >= 1024) return 2; // Medium
            if (vram >= 512) return 1;  // Low-Medium
            return 0; // Low
        }

        private void CreateSettingsUI()
        {
            // Main container
            GameObject container = new GameObject("SettingsContainer");
            container.transform.SetParent(transform, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;

            // Background
            Image bg = container.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Title
            CreateTitle(container.transform);

            // Tab buttons
            CreateTabButtons(container.transform);

            // Tab content
            CreateTabContent(container.transform);

            // Bottom buttons
            CreateBottomButtons(container.transform);
        }

        private void CreateTitle(Transform parent)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);

            RectTransform rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -20);
            rect.sizeDelta = new Vector2(-40, 40);

            TextMeshProUGUI text = titleObj.AddComponent<TextMeshProUGUI>();
            text.text = "Settings";
            text.fontSize = 28;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
        }

        private void CreateTabButtons(Transform parent)
        {
            GameObject tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(parent, false);

            RectTransform rect = tabBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -70);
            rect.sizeDelta = new Vector2(-40, 40);

            HorizontalLayoutGroup layout = tabBar.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            // Create tab buttons
            CreateTabButton(tabBar.transform, "Audio");
            CreateTabButton(tabBar.transform, "Graphics");
            CreateTabButton(tabBar.transform, "Controls");
            CreateTabButton(tabBar.transform, "Gameplay");
            CreateTabButton(tabBar.transform, "Network");
            CreateTabButton(tabBar.transform, "Accessibility");
        }

        private void CreateTabButton(Transform parent, string tabName)
        {
            GameObject button = new GameObject($"Tab_{tabName}");
            button.transform.SetParent(parent, false);

            Button btn = button.AddComponent<Button>();
            Image img = button.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 1);

            TextMeshProUGUI text = new GameObject("Text").AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(button.transform, false);
            text.text = tabName;
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.Center;

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }

        private void CreateTabContent(Transform parent)
        {
            GameObject content = new GameObject("TabContent");
            content.transform.SetParent(parent, false);

            RectTransform rect = content.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, -40);
            rect.sizeDelta = new Vector2(-40, -160);

            // Create each tab's content (initially hidden)
            audioTab = CreateAudioTab(content.transform);
            graphicsTab = CreateGraphicsTab(content.transform);
            controlsTab = CreateControlsTab(content.transform);
            gameplayTab = CreateGameplayTab(content.transform);
            networkTab = CreateNetworkTab(content.transform);
            accessibilityTab = CreateAccessibilityTab(content.transform);

            // Show first tab by default
            ShowTab(audioTab);
        }

        private GameObject CreateAudioTab(Transform parent)
        {
            GameObject tab = new GameObject("AudioTab");
            tab.transform.SetParent(parent, false);

            RectTransform rect = tab.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            // Add audio controls
            CreateSlider(tab.transform, "Master Volume", 0, 1, MasterVolume, (v) => SetMasterVolume(v));
            CreateSlider(tab.transform, "SFX Volume", 0, 1, SFXVolume, (v) => SetSFXVolume(v));
            CreateSlider(tab.transform, "Music Volume", 0, 1, MusicVolume, (v) => SetMusicVolume(v));
            CreateSlider(tab.transform, "Ambient Volume", 0, 1, AmbientVolume, (v) => SetAmbientVolume(v));
            CreateSlider(tab.transform, "Voice Volume", 0, 1, VoiceVolume, (v) => SetVoiceVolume(v));

            return tab;
        }

        private GameObject CreateGraphicsTab(Transform parent)
        {
            GameObject tab = new GameObject("GraphicsTab");
            tab.transform.SetParent(parent, false);

            RectTransform rect = tab.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            // Add graphics controls
            CreateDropdown(tab.transform, "Quality", GetQualityOptions(), GraphicsQuality, (v) => SetGraphicsQuality(v));
            CreateToggle(tab.transform, "Fullscreen", IsFullscreen, (v) => SetFullscreen(v));
            CreateToggle(tab.transform, "VSync", VSyncEnabled, (v) => SetVSync(v));
            CreateSlider(tab.transform, "Frame Rate Limit", 0, 240, FrameRateLimit, (v) => SetFrameRateLimit((int)v));

            tab.SetActive(false);
            return tab;
        }

        private GameObject CreateControlsTab(Transform parent)
        {
            GameObject tab = new GameObject("ControlsTab");
            tab.transform.SetParent(parent, false);

            RectTransform rect = tab.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            // Add control settings
            CreateSlider(tab.transform, "Mouse Sensitivity", 0.1f, 10f, MouseSensitivity, (v) => SetMouseSensitivity(v));
            CreateToggle(tab.transform, "Invert Mouse Y", InvertMouseY, (v) => SetInvertMouseY(v));
            CreateToggle(tab.transform, "Invert Mouse X", InvertMouseX, (v) => SetInvertMouseX(v));

            tab.SetActive(false);
            return tab;
        }

        private GameObject CreateGameplayTab(Transform parent)
        {
            GameObject tab = new GameObject("GameplayTab");
            tab.transform.SetParent(parent, false);

            RectTransform rect = tab.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            // Add gameplay settings
            CreateDropdown(tab.transform, "Language", GetSupportedLanguages(), 0, (v) => SetLanguage(GetSupportedLanguages()[v]));
            CreateToggle(tab.transform, "Auto Targeting", AutoTargeting, (v) => SetAutoTargeting(v));
            CreateToggle(tab.transform, "Smart Casting", SmartCasting, (v) => SetSmartCasting(v));
            CreateToggle(tab.transform, "Show Damage Numbers", ShowDamageNumbers, (v) => SetShowDamageNumbers(v));
            CreateSlider(tab.transform, "UI Scale", 0.5f, 2f, UIScale, (v) => SetUIScale(v));

            tab.SetActive(false);
            return tab;
        }

        private GameObject CreateNetworkTab(Transform parent)
        {
            GameObject tab = new GameObject("NetworkTab");
            tab.transform.SetParent(parent, false);

            RectTransform rect = tab.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            // Add network settings
            CreateDropdown(tab.transform, "Server Region", GetAvailableRegions(), 0, (v) => SetServerRegion(GetAvailableRegions()[v]));
            CreateToggle(tab.transform, "Low Latency Mode", LowLatencyMode, (v) => SetLowLatencyMode(v));
            CreateToggle(tab.transform, "Packet Loss Compensation", PacketLossCompensation, (v) => SetPacketLossCompensation(v));

            tab.SetActive(false);
            return tab;
        }

        private GameObject CreateAccessibilityTab(Transform parent)
        {
            GameObject tab = new GameObject("AccessibilityTab");
            tab.transform.SetParent(parent, false);

            RectTransform rect = tab.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            // Add accessibility settings
            CreateDropdown(tab.transform, "Colorblind Mode", GetColorblindModes(), (int)ColorblindMode, (v) => SetColorblindMode((ColorblindMode)v));
            CreateToggle(tab.transform, "Subtitles", SubtitlesEnabled, (v) => SetSubtitlesEnabled(v));
            CreateSlider(tab.transform, "Subtitle Size", 12, 24, SubtitleSize, (v) => SetSubtitleSize((int)v));
            CreateToggle(tab.transform, "Screen Reader", ScreenReaderEnabled, (v) => SetScreenReaderEnabled(v));

            tab.SetActive(false);
            return tab;
        }

        private void CreateBottomButtons(Transform parent)
        {
            GameObject buttonPanel = new GameObject("ButtonPanel");
            buttonPanel.transform.SetParent(parent, false);

            RectTransform rect = buttonPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 20);
            rect.sizeDelta = new Vector2(-40, 40);

            HorizontalLayoutGroup layout = buttonPanel.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childAlignment = TextAnchor.MiddleCenter;

            // Create buttons
            CreateButton(buttonPanel.transform, "Apply", () => ApplySettings());
            CreateButton(buttonPanel.transform, "Save", () => SaveSettings());
            CreateButton(buttonPanel.transform, "Reset", () => ResetAllSettings());
            CreateButton(buttonPanel.transform, "Cancel", () => Close());
        }

        #endregion

        #region UI Helpers

        private void CreateSlider(Transform parent, string label, float min, float max, float value, Action<float> onChanged)
        {
            GameObject row = new GameObject($"Slider_{label}");
            row.transform.SetParent(parent, false);

            // Layout would be implemented here
        }

        private void CreateToggle(Transform parent, string label, bool value, Action<bool> onChanged)
        {
            GameObject row = new GameObject($"Toggle_{label}");
            row.transform.SetParent(parent, false);

            // Layout would be implemented here
        }

        private void CreateDropdown(Transform parent, string label, List<string> options, int value, Action<int> onChanged)
        {
            GameObject row = new GameObject($"Dropdown_{label}");
            row.transform.SetParent(parent, false);

            // Layout would be implemented here
        }

        private void CreateButton(Transform parent, string label, Action onClick)
        {
            GameObject button = new GameObject($"Button_{label}");
            button.transform.SetParent(parent, false);

            Button btn = button.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            // Add text and styling
        }

        private void ShowTab(GameObject tab)
        {
            // Hide all tabs
            audioTab?.SetActive(false);
            graphicsTab?.SetActive(false);
            controlsTab?.SetActive(false);
            gameplayTab?.SetActive(false);
            networkTab?.SetActive(false);
            accessibilityTab?.SetActive(false);

            // Show selected tab
            tab?.SetActive(true);
        }

        #endregion

        #region Audio Settings

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            OnSettingsChanged?.Invoke();
        }

        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
        }

        public void SetAmbientVolume(float volume)
        {
            AmbientVolume = Mathf.Clamp01(volume);
        }

        public void SetVoiceVolume(float volume)
        {
            VoiceVolume = Mathf.Clamp01(volume);
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
        }

        public float GetEffectiveMasterVolume()
        {
            return IsMuted ? 0 : MasterVolume;
        }

        #endregion

        #region Graphics Settings

        public void SetGraphicsQuality(int quality)
        {
            GraphicsQuality = Mathf.Clamp(quality, 0, 5);
            UpdateQualityPresetName();
            QualitySettings.SetQualityLevel(GraphicsQuality);
        }

        private void UpdateQualityPresetName()
        {
            string[] presets = { "Low", "Low-Medium", "Medium", "Medium-High", "High", "Ultra" };
            QualityPresetName = presets[GraphicsQuality];
        }

        public void SetResolution(Resolution resolution)
        {
            CurrentResolution = resolution;
            Screen.SetResolution(resolution.width, resolution.height, IsFullscreen);
        }

        public void SetFullscreen(bool fullscreen)
        {
            IsFullscreen = fullscreen;
            Screen.fullScreen = fullscreen;
        }

        public void SetVSync(bool enabled)
        {
            VSyncEnabled = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;
        }

        public void SetFrameRateLimit(int limit)
        {
            FrameRateLimit = limit;
            Application.targetFrameRate = limit == 0 ? -1 : limit;
        }

        public void SetShadowQuality(int quality)
        {
            ShadowQuality = quality;
        }

        public void SetTextureQuality(int quality)
        {
            TextureQuality = quality;
        }

        public void SetAntiAliasing(int level)
        {
            AntiAliasing = level;
            QualitySettings.antiAliasing = level;
        }

        public void SetAnisotropicFiltering(int level)
        {
            AnisotropicFiltering = level;
        }

        private List<string> GetQualityOptions()
        {
            return new List<string> { "Low", "Low-Medium", "Medium", "Medium-High", "High", "Ultra" };
        }

        public int GetRecommendedQuality()
        {
            return GetAutoDetectedQuality();
        }

        public bool CanRunQualityLevel(int level)
        {
            return GetAutoDetectedQuality() >= level;
        }

        #endregion

        #region Controls Settings

        public Dictionary<string, KeyCode> GetDefaultKeybindings()
        {
            return new Dictionary<string, KeyCode>(defaultKeybindings);
        }

        public void RemapKey(string action, KeyCode key)
        {
            if (keybindings.ContainsKey(action))
                keybindings[action] = key;
        }

        public KeyCode GetKeybinding(string action)
        {
            return keybindings.ContainsKey(action) ? keybindings[action] : KeyCode.None;
        }

        public bool CheckKeyConflict(string action, KeyCode key)
        {
            foreach (var kvp in keybindings)
            {
                if (kvp.Key != action && kvp.Value == key)
                    return true;
            }
            return false;
        }

        public string GetConflictingAction(KeyCode key)
        {
            foreach (var kvp in keybindings)
            {
                if (kvp.Value == key)
                    return kvp.Key;
            }
            return null;
        }

        public void ResetKeybindings()
        {
            keybindings = new Dictionary<string, KeyCode>(defaultKeybindings);
        }

        public void SetMouseSensitivity(float sensitivity)
        {
            MouseSensitivity = sensitivity;
        }

        public void SetInvertMouseY(bool invert)
        {
            InvertMouseY = invert;
        }

        public void SetInvertMouseX(bool invert)
        {
            InvertMouseX = invert;
        }

        #endregion

        #region Gameplay Settings

        public void SetLanguage(string language)
        {
            CurrentLanguage = language;
        }

        public List<string> GetSupportedLanguages()
        {
            return new List<string> { "English", "Français", "Español", "Deutsch", "Italiano", "Português", "日本語", "中文" };
        }

        public void SetAutoTargeting(bool enabled)
        {
            AutoTargeting = enabled;
        }

        public void SetSmartCasting(bool enabled)
        {
            SmartCasting = enabled;
        }

        public void SetShowDamageNumbers(bool show)
        {
            ShowDamageNumbers = show;
        }

        public void SetUIScale(float scale)
        {
            UIScale = Mathf.Clamp(scale, 0.5f, 2.0f);
        }

        public void SetShowQuestNotifications(bool show)
        {
            ShowQuestNotifications = show;
        }

        public void SetShowCombatNotifications(bool show)
        {
            ShowCombatNotifications = show;
        }

        public void SetShowItemNotifications(bool show)
        {
            ShowItemNotifications = show;
        }

        #endregion

        #region Network Settings

        public void SetServerRegion(string region)
        {
            ServerRegion = region;
        }

        public List<string> GetAvailableRegions()
        {
            return new List<string> { "NA-East", "NA-West", "EU-West", "EU-Central", "Asia", "Oceania", "South America" };
        }

        public void SetLowLatencyMode(bool enabled)
        {
            LowLatencyMode = enabled;
        }

        public void SetPacketLossCompensation(bool enabled)
        {
            PacketLossCompensation = enabled;
        }

        #endregion

        #region Accessibility Settings

        public void SetColorblindMode(ColorblindMode mode)
        {
            ColorblindMode = mode;
        }

        private List<string> GetColorblindModes()
        {
            return Enum.GetNames(typeof(ColorblindMode)).ToList();
        }

        public void SetSubtitlesEnabled(bool enabled)
        {
            SubtitlesEnabled = enabled;
        }

        public void SetSubtitleSize(int size)
        {
            SubtitleSize = size;
        }

        public void SetScreenReaderEnabled(bool enabled)
        {
            ScreenReaderEnabled = enabled;
        }

        public void SetScreenReaderSpeed(float speed)
        {
            ScreenReaderSpeed = speed;
        }

        #endregion

        #region Persistence

        public void SaveSettings()
        {
            // Audio
            PlayerPrefs.SetFloat("MasterVolume", MasterVolume);
            PlayerPrefs.SetFloat("SFXVolume", SFXVolume);
            PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
            PlayerPrefs.SetFloat("AmbientVolume", AmbientVolume);
            PlayerPrefs.SetFloat("VoiceVolume", VoiceVolume);

            // Graphics
            PlayerPrefs.SetInt("GraphicsQuality", GraphicsQuality);
            PlayerPrefs.SetInt("Fullscreen", IsFullscreen ? 1 : 0);
            PlayerPrefs.SetInt("VSync", VSyncEnabled ? 1 : 0);
            PlayerPrefs.SetInt("FrameRateLimit", FrameRateLimit);

            // Controls
            PlayerPrefs.SetFloat("MouseSensitivity", MouseSensitivity);
            PlayerPrefs.SetInt("InvertMouseY", InvertMouseY ? 1 : 0);
            PlayerPrefs.SetInt("InvertMouseX", InvertMouseX ? 1 : 0);

            // Save keybindings
            foreach (var kvp in keybindings)
            {
                PlayerPrefs.SetInt($"Keybind_{kvp.Key}", (int)kvp.Value);
            }

            // Gameplay
            PlayerPrefs.SetString("Language", CurrentLanguage);
            PlayerPrefs.SetInt("AutoTargeting", AutoTargeting ? 1 : 0);
            PlayerPrefs.SetInt("SmartCasting", SmartCasting ? 1 : 0);
            PlayerPrefs.SetInt("ShowDamageNumbers", ShowDamageNumbers ? 1 : 0);
            PlayerPrefs.SetFloat("UIScale", UIScale);

            // Network
            PlayerPrefs.SetString("ServerRegion", ServerRegion);
            PlayerPrefs.SetInt("LowLatencyMode", LowLatencyMode ? 1 : 0);

            // Accessibility
            PlayerPrefs.SetInt("ColorblindMode", (int)ColorblindMode);
            PlayerPrefs.SetInt("SubtitlesEnabled", SubtitlesEnabled ? 1 : 0);
            PlayerPrefs.SetInt("SubtitleSize", SubtitleSize);

            PlayerPrefs.Save();
        }

        public void LoadSettings()
        {
            // Audio
            MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
            SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
            MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
            AmbientVolume = PlayerPrefs.GetFloat("AmbientVolume", 0.8f);
            VoiceVolume = PlayerPrefs.GetFloat("VoiceVolume", 1.0f);

            // Graphics
            GraphicsQuality = PlayerPrefs.GetInt("GraphicsQuality", GetAutoDetectedQuality());
            IsFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            VSyncEnabled = PlayerPrefs.GetInt("VSync", 1) == 1;
            FrameRateLimit = PlayerPrefs.GetInt("FrameRateLimit", 60);

            // Controls
            MouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
            InvertMouseY = PlayerPrefs.GetInt("InvertMouseY", 0) == 1;
            InvertMouseX = PlayerPrefs.GetInt("InvertMouseX", 0) == 1;

            // Load keybindings
            foreach (var key in defaultKeybindings.Keys.ToList())
            {
                int savedKey = PlayerPrefs.GetInt($"Keybind_{key}", (int)defaultKeybindings[key]);
                keybindings[key] = (KeyCode)savedKey;
            }

            // Gameplay
            CurrentLanguage = PlayerPrefs.GetString("Language", "English");
            AutoTargeting = PlayerPrefs.GetInt("AutoTargeting", 0) == 1;
            SmartCasting = PlayerPrefs.GetInt("SmartCasting", 0) == 1;
            ShowDamageNumbers = PlayerPrefs.GetInt("ShowDamageNumbers", 1) == 1;
            UIScale = PlayerPrefs.GetFloat("UIScale", 1.0f);

            // Network
            ServerRegion = PlayerPrefs.GetString("ServerRegion", "NA-East");
            LowLatencyMode = PlayerPrefs.GetInt("LowLatencyMode", 0) == 1;

            // Accessibility
            ColorblindMode = (ColorblindMode)PlayerPrefs.GetInt("ColorblindMode", 0);
            SubtitlesEnabled = PlayerPrefs.GetInt("SubtitlesEnabled", 0) == 1;
            SubtitleSize = PlayerPrefs.GetInt("SubtitleSize", 16);

            UpdateQualityPresetName();
        }

        public void ResetAllSettings()
        {
            // Reset to defaults
            MasterVolume = 1.0f;
            SFXVolume = 1.0f;
            MusicVolume = 1.0f;
            AmbientVolume = 0.8f;
            VoiceVolume = 1.0f;
            IsMuted = false;

            GraphicsQuality = GetAutoDetectedQuality();
            IsFullscreen = true;
            VSyncEnabled = true;
            FrameRateLimit = 60;

            MouseSensitivity = 1.0f;
            InvertMouseY = false;
            InvertMouseX = false;
            ResetKeybindings();

            CurrentLanguage = "English";
            AutoTargeting = false;
            SmartCasting = false;
            ShowDamageNumbers = true;
            UIScale = 1.0f;

            ServerRegion = "NA-East";
            LowLatencyMode = false;
            PacketLossCompensation = false;

            ColorblindMode = ColorblindMode.None;
            SubtitlesEnabled = false;
            SubtitleSize = 16;
            ScreenReaderEnabled = false;
            ScreenReaderSpeed = 1.0f;

            UpdateQualityPresetName();
        }

        public string ExportSettings()
        {
            var data = new Dictionary<string, object>();

            // Add all settings to dictionary
            data["MasterVolume"] = MasterVolume;
            data["SFXVolume"] = SFXVolume;
            data["MusicVolume"] = MusicVolume;
            data["GraphicsQuality"] = GraphicsQuality;
            data["Language"] = CurrentLanguage;
            // ... add all other settings

            return JsonUtility.ToJson(data);
        }

        public bool ImportSettings(string data)
        {
            try
            {
                var settings = JsonUtility.FromJson<Dictionary<string, object>>(data);

                // Apply imported settings
                if (settings.ContainsKey("MasterVolume"))
                    MasterVolume = Convert.ToSingle(settings["MasterVolume"]);
                if (settings.ContainsKey("SFXVolume"))
                    SFXVolume = Convert.ToSingle(settings["SFXVolume"]);
                if (settings.ContainsKey("MusicVolume"))
                    MusicVolume = Convert.ToSingle(settings["MusicVolume"]);
                if (settings.ContainsKey("GraphicsQuality"))
                    GraphicsQuality = Convert.ToInt32(settings["GraphicsQuality"]);
                if (settings.ContainsKey("Language"))
                    CurrentLanguage = settings["Language"].ToString();
                // ... apply all other settings

                UpdateQualityPresetName();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Profile Management

        public void CreateProfile(string name)
        {
            profiles[name] = new SettingsProfile
            {
                Name = name,
                MasterVolume = MasterVolume,
                GraphicsQuality = GraphicsQuality,
                MouseSensitivity = MouseSensitivity,
                Language = CurrentLanguage
                // ... copy all settings
            };
        }

        public void SaveToProfile(string name)
        {
            if (!profiles.ContainsKey(name))
                CreateProfile(name);

            var profile = profiles[name];
            profile.MasterVolume = MasterVolume;
            profile.GraphicsQuality = GraphicsQuality;
            profile.MouseSensitivity = MouseSensitivity;
            profile.Language = CurrentLanguage;
            // ... save all current settings
        }

        public void LoadProfile(string name)
        {
            if (!profiles.ContainsKey(name)) return;

            var profile = profiles[name];
            MasterVolume = profile.MasterVolume;
            GraphicsQuality = profile.GraphicsQuality;
            MouseSensitivity = profile.MouseSensitivity;
            CurrentLanguage = profile.Language;
            // ... load all settings

            UpdateQualityPresetName();
        }

        public List<string> GetProfiles()
        {
            return profiles.Keys.ToList();
        }

        #endregion

        #region Apply Settings

        public void ApplySettings()
        {
            // Apply all settings to the game
            Screen.SetResolution(CurrentResolution.width, CurrentResolution.height, IsFullscreen);
            QualitySettings.SetQualityLevel(GraphicsQuality);
            QualitySettings.vSyncCount = VSyncEnabled ? 1 : 0;
            Application.targetFrameRate = FrameRateLimit == 0 ? -1 : FrameRateLimit;
            QualitySettings.antiAliasing = AntiAliasing;

            // Apply audio settings
            AudioListener.volume = GetEffectiveMasterVolume();

            OnSettingsApplied?.Invoke();
        }

        public void Close()
        {
            // Close the settings menu
            gameObject.SetActive(false);
        }

        #endregion
    }

    #region Supporting Classes

    public class SettingsProfile
    {
        public string Name;
        public float MasterVolume;
        public int GraphicsQuality;
        public float MouseSensitivity;
        public string Language;
        // Add all other settings
    }

    public class TabGroup : MonoBehaviour
    {
        // Tab management functionality
    }

    #endregion
}