# Using Programmatic LoginScreen in Unity

## Overview
Your LoginScreen.cs creates all UI elements through code rather than manually in Unity Editor. This is a professional approach that provides better control and reusability.

## Setup Steps

### 1. Create Empty Scene
1. Open Unity Hub → Open gofus-client project
2. File → New Scene
3. Save as: `Assets/_Project/Scenes/LoginScene.unity`

### 2. Create LoginScreen GameObject
1. In Hierarchy: Right-click → Create Empty
2. Rename to "LoginScreen"
3. Position at (0, 0, 0)

### 3. Add Required Components
1. Select LoginScreen GameObject
2. Add Component → Canvas
   - Render Mode: Screen Space - Overlay
   - Pixel Perfect: Checked (optional)
3. Add Component → Canvas Scaler
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080
   - Screen Match Mode: 0.5
4. Add Component → Graphic Raycaster (for input detection)

### 4. Attach Your LoginScreen Script
1. With LoginScreen GameObject selected
2. Add Component → Scripts → GOFUS.UI.Screens → LoginScreen
3. The script will automatically:
   - Create the background panel
   - Create title text
   - Create username/password inputs
   - Create login/register buttons
   - Create status text

### 5. Configure Script Settings
In the Inspector for LoginScreen component:
```
Backend Settings:
- Backend URL: http://localhost:3000
- Game Server URL: ws://localhost:3001
- Mock Authentication: ✓ (for testing)

Panel Settings:
- Panel Width: 400
- Panel Height: 500
- Background Color: (Dark gray with 90% alpha)
```

## How the Programmatic Creation Works

Your LoginScreen inherits from UIScreen which provides:
- `Initialize()` - Called when screen activates
- `Show()` / `Hide()` - Screen visibility
- `CreateUI()` - Where your UI elements are built

### Execution Flow:
1. **Start()** → Calls CreateUI()
2. **CreateUI()** → Orchestrates creation:
   ```csharp
   private void CreateUI()
   {
       CreateCanvas();      // If not already present
       CreatePanel();       // Background panel
       CreateTitle();       // "GOFUS LOGIN" text
       CreateInputFields(); // Username & password
       CreateButtons();     // Login & register
       CreateStatusText();  // Error/success messages
   }
   ```

3. **CreatePanel()** → Creates centered panel:
   ```csharp
   GameObject panel = new GameObject("LoginPanel");
   panel.transform.SetParent(canvas.transform, false);

   RectTransform rect = panel.AddComponent<RectTransform>();
   rect.sizeDelta = new Vector2(panelWidth, panelHeight);
   rect.anchoredPosition = Vector2.zero;

   Image bg = panel.AddComponent<Image>();
   bg.color = backgroundColor;
   ```

4. **CreateInputFields()** → Builds inputs dynamically:
   ```csharp
   usernameInput = CreateInputField("UsernameInput",
                                    "Enter username...",
                                    new Vector2(0, 50));
   passwordInput = CreateInputField("PasswordInput",
                                    "Enter password...",
                                    new Vector2(0, -20));
   passwordInput.contentType = TMP_InputField.ContentType.Password;
   ```

## Testing Your Setup

### 1. Quick Test
1. Save scene (Ctrl+S)
2. Press Play (▶)
3. You should see:
   - Dark background panel
   - Title "GOFUS LOGIN"
   - Two input fields
   - Two buttons
   - All created programmatically!

### 2. Verify Functionality
- Type in username field → Text appears
- Type in password field → Shows dots/asterisks
- Click Login → Shows "Logging in..." status
- With mock auth → Shows "Login successful!"

### 3. Check Console
Look for these debug messages:
```
[LoginScreen] UI Created Successfully
[LoginScreen] Canvas: 1920x1080
[LoginScreen] Panel: 400x500 at (0,0)
[LoginScreen] Input fields created
[LoginScreen] Buttons configured
```

## Advantages of Programmatic UI

1. **Version Control Friendly**
   - All changes tracked in code
   - No Unity scene merge conflicts
   - Easy to review changes

2. **Reusable Components**
   - CreateInputField() method can be reused
   - Easy to create multiple screens
   - Consistent styling across UI

3. **Dynamic Adaptation**
   - Can adjust based on screen size
   - Easy to support multiple resolutions
   - Can create different layouts programmatically

4. **Testing**
   - Unit testable
   - Can mock UI creation
   - Easier automated testing

## Common Issues & Solutions

### Nothing Appears?
- Check Canvas component exists on GameObject
- Verify EventSystem exists (GameObject → UI → Event System)
- Check Console for errors in CreateUI()

### Input Fields Don't Work?
- EventSystem must exist in scene
- Canvas needs Graphic Raycaster component
- Check input fields have RaycastTarget enabled

### Layout Issues?
- Verify Canvas Scaler settings
- Check anchor presets in CreatePanel()
- Adjust sizeDelta values in code

## Next Steps

1. **Connect to Backend**
   ```csharp
   private IEnumerator LoginCoroutine(string username, string password)
   {
       var loginData = new { login = username, password = password };
       string json = JsonUtility.ToJson(loginData);

       using (UnityWebRequest request = UnityWebRequest.Post(
           backendUrl + "/api/auth/login", json))
       {
           request.SetRequestHeader("Content-Type", "application/json");
           yield return request.SendWebRequest();

           if (request.result == UnityWebRequest.Result.Success)
           {
               var response = JsonUtility.FromJson<LoginResponse>(
                   request.downloadHandler.text);
               PlayerPrefs.SetString("jwt_token", response.token);

               // Transition to character selection
               uiManager.TransitionTo(ScreenType.CharacterSelection);
           }
       }
   }
   ```

2. **Add Visual Polish**
   - Add background image loading
   - Implement button hover states
   - Add input field focus animations
   - Create loading spinner

3. **Extend for Other Screens**
   - CharacterSelectionScreen (same pattern)
   - GameHUD (programmatic UI)
   - InventoryScreen (dynamic grid)

## Code Architecture

Your approach follows the **UI Factory Pattern**:
- Each screen is a factory for its UI elements
- Consistent creation methods
- Easy to maintain and extend

This is how professional Unity projects handle UI, especially for:
- Mobile games (dynamic layouts)
- MMOs (complex UIs)
- Cross-platform games