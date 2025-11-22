using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GOFUS.Core;
using GOFUS.UI.Screens;

namespace GOFUS.UI
{
    /// <summary>
    /// Central manager for all UI screens and navigation
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [Header("Screen Management")]
        private Dictionary<ScreenType, UIScreen> screens;
        private UIScreen currentScreen;
        private Stack<ScreenType> navigationHistory;

        [Header("Canvas References")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private Canvas worldCanvas;
        [SerializeField] private Canvas overlayCanvas;

        [Header("Settings")]
        [SerializeField] private float defaultTransitionDuration = 0.3f;
        [SerializeField] private bool enableTransitions = true;

        // Events
        public event Action<ScreenType> OnScreenChanged;
        public event Action<UIScreen> OnScreenShown;
        public event Action<UIScreen> OnScreenHidden;

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        private void Start()
        {
            // Show login screen on start - force immediate show without transitions
            if (screens.ContainsKey(ScreenType.Login))
            {
                currentScreen = screens[ScreenType.Login];
                currentScreen.Show();
                OnScreenShown?.Invoke(currentScreen);
                OnScreenChanged?.Invoke(ScreenType.Login);
                Debug.Log("[UIManager] LoginScreen shown on start");
            }
            else
            {
                Debug.LogError("[UIManager] LoginScreen not found in screens dictionary!");
            }
        }

        public void Initialize()
        {
            screens = new Dictionary<ScreenType, UIScreen>();
            navigationHistory = new Stack<ScreenType>();

            CreateCanvases();
            CreateAllScreens();
        }

        private void CreateCanvases()
        {
            if (mainCanvas == null)
            {
                GameObject mainCanvasObj = new GameObject("MainCanvas");
                mainCanvasObj.transform.parent = transform;
                mainCanvas = mainCanvasObj.AddComponent<Canvas>();
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                mainCanvas.sortingOrder = 0;

                var scaler = mainCanvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                mainCanvasObj.AddComponent<GraphicRaycaster>();
            }

            if (worldCanvas == null)
            {
                GameObject worldCanvasObj = new GameObject("WorldCanvas");
                worldCanvasObj.transform.parent = transform;
                worldCanvas = worldCanvasObj.AddComponent<Canvas>();
                worldCanvas.renderMode = RenderMode.WorldSpace;
                worldCanvas.sortingOrder = -1;
            }

            if (overlayCanvas == null)
            {
                GameObject overlayCanvasObj = new GameObject("OverlayCanvas");
                overlayCanvasObj.transform.parent = transform;
                overlayCanvas = overlayCanvasObj.AddComponent<Canvas>();
                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                overlayCanvas.sortingOrder = 100;

                overlayCanvasObj.AddComponent<GraphicRaycaster>();
            }
        }

        private void CreateAllScreens()
        {
            // Create all UI screens
            CreateScreen<LoginScreen>(ScreenType.Login);

            // TODO: Uncomment these as you create the screen classes
            // CreateScreen<MainMenuScreen>(ScreenType.MainMenu);
            CreateScreen<CharacterSelectionScreen>(ScreenType.CharacterSelection);
            CreateScreen<CharacterCreationScreen>(ScreenType.CharacterCreation);

            // Create GameHUD - essential for gameplay
            CreateScreen<GameHUD>(ScreenType.GameHUD);

            // TODO: Create these screens as needed
            // CreateScreen<EnhancedInventoryUI>(ScreenType.Inventory);
            // CreateScreen<CompleteSettingsMenu>(ScreenType.Settings);
            // CreateScreen<FullChatSystem>(ScreenType.Chat);

            // TODO: Create LoadingScreen class
            // CreateScreen<LoadingScreen>(ScreenType.Loading);
        }

        private T CreateScreen<T>(ScreenType type) where T : UIScreen
        {
            GameObject screenObj = new GameObject(type.ToString());
            screenObj.transform.SetParent(mainCanvas.transform, false);

            // Add RectTransform and configure
            RectTransform rect = screenObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            T screen = screenObj.AddComponent<T>();
            screen.Initialize();
            screen.SetScreenType(type);

            screens[type] = screen;

            // Hide by default
            screen.Hide();

            return screen;
        }

        public UIScreen GetScreen(ScreenType type)
        {
            return screens.TryGetValue(type, out UIScreen screen) ? screen : null;
        }

        public T GetScreen<T>(ScreenType type) where T : UIScreen
        {
            return GetScreen(type) as T;
        }

        public void ShowScreen(ScreenType type, bool addToHistory = true)
        {
            if (!screens.ContainsKey(type))
            {
                Debug.LogError($"Screen {type} not found!");
                return;
            }

            // Hide current screen
            if (currentScreen != null)
            {
                if (enableTransitions)
                {
                    StartCoroutine(TransitionScreens(currentScreen, screens[type]));
                }
                else
                {
                    currentScreen.Hide();
                    OnScreenHidden?.Invoke(currentScreen);
                }

                if (addToHistory)
                {
                    navigationHistory.Push(currentScreen.ScreenType);
                }
            }

            // Show new screen
            currentScreen = screens[type];

            if (!enableTransitions || currentScreen.IsVisible)
            {
                currentScreen.Show();
                OnScreenShown?.Invoke(currentScreen);
            }

            OnScreenChanged?.Invoke(type);
        }

        private System.Collections.IEnumerator TransitionScreens(UIScreen from, UIScreen to)
        {
            // Fade out current
            if (from != null)
            {
                yield return from.FadeOut(defaultTransitionDuration);
                from.Hide();
                OnScreenHidden?.Invoke(from);
            }

            // Fade in new
            to.Show();
            yield return to.FadeIn(defaultTransitionDuration);
            OnScreenShown?.Invoke(to);
        }

        public void GoBack()
        {
            if (navigationHistory.Count > 0)
            {
                ScreenType previousScreen = navigationHistory.Pop();
                ShowScreen(previousScreen, false);
            }
        }

        public void HideAllScreens()
        {
            foreach (var screen in screens.Values)
            {
                screen.Hide();
            }
            currentScreen = null;
        }

        public void ShowOverlay(ScreenType type)
        {
            if (screens.ContainsKey(type))
            {
                screens[type].transform.SetParent(overlayCanvas.transform, false);
                screens[type].Show();
            }
        }

        public void HideOverlay(ScreenType type)
        {
            if (screens.ContainsKey(type))
            {
                screens[type].Hide();
                screens[type].transform.SetParent(mainCanvas.transform, false);
            }
        }

        public bool IsScreenVisible(ScreenType type)
        {
            return screens.ContainsKey(type) && screens[type].IsVisible;
        }

        // Utility methods for common UI operations
        public void ShowLoading(string message = "Loading...")
        {
            // TODO: Create LoadingScreen class
            // var loadingScreen = GetScreen<LoadingScreen>(ScreenType.Loading);
            // if (loadingScreen != null)
            // {
            //     loadingScreen.SetMessage(message);
            //     ShowOverlay(ScreenType.Loading);
            // }
            Debug.Log($"[UIManager] Loading: {message}");
        }

        public void HideLoading()
        {
            HideOverlay(ScreenType.Loading);
        }

        public void ShowMessage(string title, string message, Action onConfirm = null)
        {
            // This would show a message dialog
            Debug.Log($"Message: {title} - {message}");
            onConfirm?.Invoke();
        }

        public void ShowConfirmation(string title, string message, Action onConfirm, Action onCancel = null)
        {
            // This would show a confirmation dialog
            Debug.Log($"Confirmation: {title} - {message}");
        }
    }

    /// <summary>
    /// Types of UI screens in the game
    /// </summary>
    public enum ScreenType
    {
        Login,
        MainMenu,
        CharacterSelection,
        CharacterCreation,
        GameHUD,
        Inventory,
        Settings,
        Chat,
        Loading,
        Map,
        Quest,
        Shop,
        Skills,
        Social,
        Guild
    }

    /// <summary>
    /// Base class for all UI screens
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        protected CanvasGroup canvasGroup;
        protected RectTransform rectTransform;

        public ScreenType ScreenType { get; private set; }
        public bool IsVisible { get; private set; }
        public bool IsTransitioning { get; private set; }

        // Events
        public event Action OnShow;
        public event Action OnHide;

        public virtual void Initialize()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
                rectTransform = gameObject.AddComponent<RectTransform>();

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void SetScreenType(ScreenType type)
        {
            ScreenType = type;
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            IsVisible = true;
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            OnShow?.Invoke();
            OnScreenShown();
        }

        public virtual void Hide()
        {
            IsVisible = false;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            OnHide?.Invoke();
            OnScreenHidden();
        }

        public System.Collections.IEnumerator FadeIn(float duration)
        {
            IsTransitioning = true;
            gameObject.SetActive(true);
            canvasGroup.alpha = 0;

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            IsVisible = true;
            IsTransitioning = false;
        }

        public System.Collections.IEnumerator FadeOut(float duration)
        {
            IsTransitioning = true;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1 - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 0;
            IsVisible = false;
            IsTransitioning = false;
        }

        protected virtual void OnScreenShown() { }
        protected virtual void OnScreenHidden() { }

        public virtual void OnBackPressed()
        {
            UIManager.Instance.GoBack();
        }
    }

    #if UNITY_EDITOR || UNITY_INCLUDE_TESTS
    // Test helper extensions for UIManager
    public static class UIManagerTestExtensions
    {
        public static ScreenType GetCurrentScreen(this UIManager manager)
        {
            var currentScreenField = typeof(UIManager).GetField("currentScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var screen = currentScreenField?.GetValue(manager) as UIScreen;
            return screen != null ? screen.ScreenType : ScreenType.Login;
        }

        public static void TransitionTo(this UIManager manager, ScreenType screenType)
        {
            manager.ShowScreen(screenType);
        }
    }
    #endif
}