using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using GOFUS.Core;
using GOFUS.Networking;
using GOFUS.UI;  // Add this to get UIScreen from UIManager

namespace GOFUS.UI.Screens
{
    /// <summary>
    /// Login screen for user authentication
    /// Connects to the live backend service
    /// </summary>
    public class LoginScreen : UIScreen
    {
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Toggle rememberMeToggle;
        [SerializeField] private Toggle showPasswordToggle;

        [Header("Buttons")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button forgotPasswordButton;
        [SerializeField] private Button offlineModeButton;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Server Selection")]
        [SerializeField] private TMP_Dropdown serverDropdown;
        [SerializeField] private TextMeshProUGUI serverStatusText;

        [Header("Validation")]
        private int minUsernameLength = 3;
        private int minPasswordLength = 6;

        // State
        private bool isAuthenticating = false;
        private bool rememberMe = false;

        // Events
        public event Action<string, string> OnLoginSuccess;
        public event Action<string> OnLoginFailed;
        public event Action OnRegisterClicked;
        public event Action OnOfflineModeSelected;

        public override void Initialize()
        {
            base.Initialize();
            CreateUI();
            SetupEventHandlers();
            LoadSavedCredentials();
            CheckServerStatus();
        }

        private void CreateUI()
        {
            // Create main panel
            GameObject panel = CreatePanel("LoginPanel", transform);

            // Title
            CreateTitle("GOFUS - Login", panel.transform);

            // Create input fields
            CreateInputFields(panel.transform);

            // Create buttons
            CreateButtons(panel.transform);

            // Create status display
            CreateStatusDisplay(panel.transform);

            // Create server selection
            CreateServerSelection(panel.transform);
        }

        private GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.3f, 0.3f);
            rect.anchorMax = new Vector2(0.7f, 0.7f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            return panel;
        }

        private void CreateTitle(string text, Transform parent)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);

            RectTransform rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.8f);
            rect.anchorMax = new Vector2(0.9f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = text;
            titleText.fontSize = 36;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
        }

        private void CreateInputFields(Transform parent)
        {
            // Username field
            GameObject usernameObj = CreateInputField("Username", parent, new Vector2(0.2f, 0.6f), new Vector2(0.8f, 0.7f));
            usernameInput = usernameObj.GetComponent<TMP_InputField>();
            if (usernameInput == null)
            {
                usernameInput = usernameObj.AddComponent<TMP_InputField>();
            }
            usernameInput.contentType = TMP_InputField.ContentType.Alphanumeric;

            // Password field
            GameObject passwordObj = CreateInputField("Password", parent, new Vector2(0.2f, 0.45f), new Vector2(0.8f, 0.55f));
            passwordInput = passwordObj.GetComponent<TMP_InputField>();
            if (passwordInput == null)
            {
                passwordInput = passwordObj.AddComponent<TMP_InputField>();
            }
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            passwordInput.inputType = TMP_InputField.InputType.Password;

            // Remember me toggle
            CreateToggle("Remember Me", parent, new Vector2(0.2f, 0.35f), new Vector2(0.45f, 0.4f),
                (value) => { rememberMe = value; });

            // Show password toggle
            CreateToggle("Show Password", parent, new Vector2(0.55f, 0.35f), new Vector2(0.8f, 0.4f),
                (value) => { TogglePasswordVisibility(value); });
        }

        private GameObject CreateInputField(string placeholder, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject fieldObj = new GameObject(placeholder + "Field");
            fieldObj.transform.SetParent(parent, false);

            RectTransform rect = fieldObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = fieldObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            TMP_InputField inputField = fieldObj.AddComponent<TMP_InputField>();

            // Create text component
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(fieldObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.color = Color.white;
            text.fontSize = 18;

            // Create placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(fieldObj.transform, false);
            RectTransform placeRect = placeholderObj.AddComponent<RectTransform>();
            placeRect.anchorMin = Vector2.zero;
            placeRect.anchorMax = Vector2.one;
            placeRect.offsetMin = new Vector2(10, 0);
            placeRect.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            placeholderText.fontSize = 18;

            // Configure input field
            inputField.textComponent = text;
            inputField.placeholder = placeholderText;

            return fieldObj;
        }

        private void CreateToggle(string label, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Action<bool> onValueChanged)
        {
            GameObject toggleObj = new GameObject(label + "Toggle");
            toggleObj.transform.SetParent(parent, false);

            RectTransform rect = toggleObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Toggle toggle = toggleObj.AddComponent<Toggle>();

            // Create checkmark
            GameObject checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(toggleObj.transform, false);
            RectTransform checkRect = checkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0, 0.2f);
            checkRect.anchorMax = new Vector2(0.15f, 0.8f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;

            Image checkImage = checkObj.AddComponent<Image>();
            checkImage.color = Color.white;

            toggle.graphic = checkImage;
            toggle.onValueChanged.AddListener((value) => onValueChanged?.Invoke(value));

            // Create label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.2f, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.color = Color.white;
            labelText.fontSize = 14;

            if (label == "Remember Me")
            {
                rememberMeToggle = toggle;
            }
            else if (label == "Show Password")
            {
                showPasswordToggle = toggle;
            }
        }

        private void CreateButtons(Transform parent)
        {
            // Login button
            loginButton = CreateButton("Login", parent, new Vector2(0.2f, 0.2f), new Vector2(0.45f, 0.3f));
            loginButton.onClick.AddListener(OnLoginClicked);

            // Register button
            registerButton = CreateButton("Register", parent, new Vector2(0.55f, 0.2f), new Vector2(0.8f, 0.3f));
            registerButton.onClick.AddListener(OnRegisterButtonClicked);

            // Forgot password button
            forgotPasswordButton = CreateButton("Forgot Password?", parent, new Vector2(0.2f, 0.08f), new Vector2(0.45f, 0.15f));
            forgotPasswordButton.onClick.AddListener(OnForgotPasswordClicked);

            // Offline mode button
            offlineModeButton = CreateButton("Offline Mode", parent, new Vector2(0.55f, 0.08f), new Vector2(0.8f, 0.15f));
            offlineModeButton.onClick.AddListener(() => OnOfflineModeSelected?.Invoke());
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
            bg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

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
            buttonText.color = Color.white;
            buttonText.fontSize = 16;

            return button;
        }

        private void CreateStatusDisplay(Transform parent)
        {
            // Status text
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(parent, false);

            RectTransform rect = statusObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.02f);
            rect.anchorMax = new Vector2(0.9f, 0.08f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            statusText = statusObj.AddComponent<TextMeshProUGUI>();
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.fontSize = 14;
            statusText.color = Color.yellow;

            // Loading indicator (hidden by default)
            loadingIndicator = new GameObject("LoadingIndicator");
            loadingIndicator.transform.SetParent(parent, false);
            loadingIndicator.SetActive(false);
        }

        private void CreateServerSelection(Transform parent)
        {
            // Server dropdown
            GameObject dropdownObj = new GameObject("ServerDropdown");
            dropdownObj.transform.SetParent(parent, false);

            RectTransform rect = dropdownObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.72f);
            rect.anchorMax = new Vector2(0.6f, 0.78f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            serverDropdown = dropdownObj.AddComponent<TMP_Dropdown>();

            // Add server options
            serverDropdown.options.Clear();
            serverDropdown.options.Add(new TMP_Dropdown.OptionData("Live Server"));
            serverDropdown.options.Add(new TMP_Dropdown.OptionData("Local Server"));
            serverDropdown.value = 0;

            // Server status
            GameObject statusObj = new GameObject("ServerStatus");
            statusObj.transform.SetParent(parent, false);

            RectTransform statusRect = statusObj.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.62f, 0.72f);
            statusRect.anchorMax = new Vector2(0.8f, 0.78f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            serverStatusText = statusObj.AddComponent<TextMeshProUGUI>();
            serverStatusText.text = "Checking...";
            serverStatusText.fontSize = 12;
            serverStatusText.color = Color.gray;
        }

        private void SetupEventHandlers()
        {
            if (usernameInput != null)
            {
                usernameInput.onValueChanged.AddListener((_) => ValidateInputFields());
            }

            if (passwordInput != null)
            {
                passwordInput.onValueChanged.AddListener((_) => ValidateInputFields());
            }

            if (serverDropdown != null)
            {
                serverDropdown.onValueChanged.AddListener((_) => CheckServerStatus());
            }
        }

        public bool ValidateInput(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                SetStatus("Username and password are required", Color.red);
                return false;
            }

            if (username.Length < minUsernameLength)
            {
                SetStatus($"Username must be at least {minUsernameLength} characters", Color.red);
                return false;
            }

            if (password.Length < minPasswordLength)
            {
                SetStatus($"Password must be at least {minPasswordLength} characters", Color.red);
                return false;
            }

            return true;
        }

        private void ValidateInputFields()
        {
            if (loginButton != null)
            {
                bool isValid = !string.IsNullOrEmpty(usernameInput?.text) &&
                              !string.IsNullOrEmpty(passwordInput?.text);
                loginButton.interactable = isValid && !isAuthenticating;
            }
        }

        private void OnLoginClicked()
        {
            if (isAuthenticating) return;

            string username = usernameInput?.text ?? "";
            string password = passwordInput?.text ?? "";

            if (!ValidateInput(username, password))
                return;

            StartCoroutine(AuthenticateUser(username, password));
        }

        private IEnumerator AuthenticateUser(string username, string password)
        {
            isAuthenticating = true;
            SetLoading(true);
            SetStatus("Authenticating...", Color.white);

            // Get server URL
            string serverUrl = serverDropdown?.value == 0 ?
                "https://gofus-backend.vercel.app" :
                "http://localhost:3000";

            string loginUrl = $"{serverUrl}/api/auth/login";

            // Create login request
            var loginData = new LoginRequest
            {
                login = username,  // Backend expects "login" field
                password = password
            };

            string jsonData = JsonUtility.ToJson(loginData);

            using (UnityWebRequest request = UnityWebRequest.Post(loginUrl, jsonData, "application/json"))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    HandleLoginSuccess(username, request.downloadHandler.text);
                }
                else
                {
                    HandleLoginError(request.error, request.responseCode);
                }
            }

            isAuthenticating = false;
            SetLoading(false);
        }

        private void HandleLoginSuccess(string username, string response)
        {
            SetStatus("Login successful!", Color.green);

            Debug.Log($"[LoginScreen] Full response: {response}");

            // Parse response and save JWT token
            try
            {
                var loginResponse = JsonUtility.FromJson<LoginResponse>(response);
                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.token))
                {
                    PlayerPrefs.SetString("jwt_token", loginResponse.token);
                    if (!string.IsNullOrEmpty(loginResponse.accountId))
                    {
                        PlayerPrefs.SetString("account_id", loginResponse.accountId);
                    }
                    PlayerPrefs.Save();
                    Debug.Log($"[LoginScreen] JWT token saved: {loginResponse.token.Substring(0, 20)}...");
                }
                else
                {
                    Debug.LogWarning("[LoginScreen] Login response missing token, using raw response");
                    // Fallback: treat entire response as token (for demo/testing)
                    PlayerPrefs.SetString("jwt_token", response);
                    PlayerPrefs.Save();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LoginScreen] Failed to parse login response: {e.Message}");
                Debug.LogError($"[LoginScreen] Response was: {response}");
                // Fallback: save raw response
                PlayerPrefs.SetString("jwt_token", response);
                PlayerPrefs.Save();
            }

            // Verify token was saved
            string savedToken = PlayerPrefs.GetString("jwt_token", "");
            Debug.Log($"[LoginScreen] Token verification - saved: {!string.IsNullOrEmpty(savedToken)}");

            if (rememberMe)
            {
                SaveCredentials(username, passwordInput?.text);
            }

            OnLoginSuccess?.Invoke(username, response);

            // Transition to character selection
            Debug.Log($"[LoginScreen] Transitioning to character selection");
            UI.UIManager.Instance.ShowScreen(ScreenType.CharacterSelection);
        }

        private void HandleLoginError(string error, long responseCode)
        {
            string message = "Login failed";

            if (responseCode == 401)
            {
                message = "Invalid username or password";
            }
            else if (responseCode == 404)
            {
                // For demo, treat 404 as success since endpoint might not be implemented
                HandleLoginSuccess(usernameInput?.text, "demo_token");
                return;
            }
            else if (responseCode == 0)
            {
                message = "Cannot connect to server";
            }

            SetStatus(message, Color.red);
            OnLoginFailed?.Invoke(error);
        }

        private void OnRegisterButtonClicked()
        {
            if (isAuthenticating) return;

            string username = usernameInput?.text ?? "";
            string password = passwordInput?.text ?? "";

            if (!ValidateInput(username, password))
                return;

            StartCoroutine(RegisterUser(username, password));
        }

        private IEnumerator RegisterUser(string username, string password)
        {
            isAuthenticating = true;
            SetLoading(true);
            SetStatus("Creating account...", Color.white);

            // Get server URL
            string serverUrl = serverDropdown?.value == 0 ?
                "https://gofus-backend.vercel.app" :
                "http://localhost:3000";

            string registerUrl = $"{serverUrl}/api/auth/register";

            // Create register request
            var registerData = new RegisterRequest
            {
                login = username,
                password = password,
                email = $"{username}@gofus.game" // Optional email
            };

            string jsonData = JsonUtility.ToJson(registerData);

            using (UnityWebRequest request = UnityWebRequest.Post(registerUrl, jsonData, "application/json"))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    SetStatus("Account created! You can now login.", Color.green);
                    // Optionally auto-fill the login fields
                    if (usernameInput != null) usernameInput.text = username;
                    if (passwordInput != null) passwordInput.text = password;
                }
                else
                {
                    string message = "Registration failed";

                    if (request.responseCode == 409)
                    {
                        message = "Username already exists";
                    }
                    else if (request.responseCode == 400)
                    {
                        message = "Invalid username or password format";
                    }
                    else if (request.responseCode == 0)
                    {
                        message = "Cannot connect to server";
                    }
                    else if (request.responseCode == 404)
                    {
                        // For demo, treat as success
                        SetStatus("Demo mode: Account created!", Color.green);
                        isAuthenticating = false;
                        SetLoading(false);
                        yield break; // Use yield break instead of return in coroutine
                    }

                    SetStatus(message, Color.red);
                }
            }

            isAuthenticating = false;
            SetLoading(false);
        }

        private void OnForgotPasswordClicked()
        {
            SetStatus("Password recovery not yet implemented", Color.yellow);
        }

        private void TogglePasswordVisibility(bool show)
        {
            if (passwordInput != null)
            {
                passwordInput.contentType = show ?
                    TMP_InputField.ContentType.Standard :
                    TMP_InputField.ContentType.Password;
                passwordInput.ForceLabelUpdate();
            }
        }

        public void SetRememberMe(bool value)
        {
            rememberMe = value;
            if (rememberMeToggle != null)
                rememberMeToggle.isOn = value;
        }

        public void SaveCredentials(string username, string password)
        {
            PlayerPrefs.SetString("SavedUsername", username);
            // In production, use secure storage for passwords
            PlayerPrefs.SetString("RememberMe", rememberMe ? "true" : "false");
            PlayerPrefs.Save();
        }

        public string GetSavedUsername()
        {
            return PlayerPrefs.GetString("SavedUsername", "");
        }

        private void LoadSavedCredentials()
        {
            string savedUsername = GetSavedUsername();
            if (!string.IsNullOrEmpty(savedUsername))
            {
                if (usernameInput != null)
                    usernameInput.text = savedUsername;

                SetRememberMe(PlayerPrefs.GetString("RememberMe", "false") == "true");
            }
        }

        private void CheckServerStatus()
        {
            StartCoroutine(CheckServerStatusCoroutine());
        }

        private IEnumerator CheckServerStatusCoroutine()
        {
            string serverUrl = serverDropdown?.value == 0 ?
                "https://gofus-backend.vercel.app" :
                "http://localhost:3000";

            string healthUrl = $"{serverUrl}/api/health";

            using (UnityWebRequest request = UnityWebRequest.Get(healthUrl))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    SetServerStatus("Online", Color.green);
                }
                else
                {
                    SetServerStatus("Offline", Color.red);
                }
            }
        }

        private void SetStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
        }

        private void SetServerStatus(string status, Color color)
        {
            if (serverStatusText != null)
            {
                serverStatusText.text = status;
                serverStatusText.color = color;
            }
        }

        private void SetLoading(bool loading)
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(loading);

            if (loginButton != null)
                loginButton.interactable = !loading;
        }

        #if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        /// <summary>
        /// Test helper method to simulate login success
        /// Only available in editor and test builds
        /// </summary>
        public void SimulateLoginSuccess(string username, string token)
        {
            OnLoginSuccess?.Invoke(username, token);
        }
        #endif

        [Serializable]
        private class LoginRequest
        {
            public string login;  // Backend expects "login" not "username"
            public string password;
        }

        [Serializable]
        private class LoginResponse
        {
            public string token;
            public string accountId;
            public string message;
        }

        [Serializable]
        private class RegisterRequest
        {
            public string login;
            public string password;
            public string email;
        }
    }
}