# 🔴 CREATE LOGIN SCREEN - Step-by-Step Guide

## DO THIS RIGHT NOW IN UNITY

### Step 1: Open Unity and Create Login Scene
```
1. Open Unity Hub
2. Open gofus-client project
3. In Unity: File > New Scene
4. File > Save As...
   - Navigate to: Assets/_Project/Scenes/
   - Name: "LoginScene"
   - Click Save
```

### Step 2: Create UI Canvas
```
1. In Hierarchy window:
   - Right-click empty space
   - UI > Canvas

2. Select Canvas in Hierarchy
3. In Inspector:
   - Canvas Scaler component
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080
   - Screen Match Mode: 0.5
```

### Step 3: Add Background
```
1. Right-click Canvas in Hierarchy
2. UI > Image
3. Rename to "Background"
4. In Inspector:
   - Rect Transform:
     - Click Anchor Presets (square icon)
     - Hold Alt+Shift, click bottom-right (stretch)
   - Set all positions to 0

5. Add temporary color:
   - Image component > Color
   - Pick dark blue or gray
```

### Step 4: Create Login Panel
```
1. Right-click Canvas
2. UI > Panel
3. Rename to "LoginPanel"
4. In Inspector:
   - Width: 400
   - Height: 500
   - Position: (0, 0, 0) centered

5. Style the panel:
   - Image component > Color
   - Set alpha to 0.9 (slightly transparent)
   - Color: Dark gray/black
```

### Step 5: Add Title Text
```
1. Right-click LoginPanel
2. UI > Text - TextMeshPro
3. First time: Import TMP Essentials (click Import)
4. Rename to "TitleText"
5. In Inspector:
   - Text: "GOFUS LOGIN"
   - Font Size: 36
   - Alignment: Center
   - Position Y: 150
```

### Step 6: Add Username Input
```
1. Right-click LoginPanel
2. UI > Input Field - TextMeshPro
3. Rename to "UsernameInput"
4. Position Y: 50
5. Width: 300
6. In TMP_InputField component:
   - Text placeholder: "Username"
```

### Step 7: Add Password Input
```
1. Right-click LoginPanel
2. UI > Input Field - TextMeshPro
3. Rename to "PasswordInput"
4. Position Y: -20
5. Width: 300
6. In TMP_InputField component:
   - Content Type: Password
   - Text placeholder: "Password"
```

### Step 8: Add Login Button
```
1. Right-click LoginPanel
2. UI > Button - TextMeshPro
3. Rename to "LoginButton"
4. Position Y: -100
5. Width: 200, Height: 50
6. Select child Text (TMP)
7. Change text to "LOGIN"
```

### Step 9: Add Register Button
```
1. Right-click LoginPanel
2. UI > Button - TextMeshPro
3. Rename to "RegisterButton"
4. Position Y: -160
5. Width: 200, Height: 40
6. Select child Text (TMP)
7. Change text to "Create Account"
```

### Step 10: Add Status Text
```
1. Right-click LoginPanel
2. UI > Text - TextMeshPro
3. Rename to "StatusText"
4. Position Y: -220
5. Width: 350
6. Text: "" (empty)
7. Color: Yellow/Red for errors
8. Alignment: Center
```

### Step 11: Create LoginScreen Script
```
1. In Project window:
   - Navigate to: Assets/_Project/Scripts/UI/Screens/
   - Right-click > Create > C# Script
   - Name: "LoginScreen"

2. Double-click to open in code editor
3. Replace with this code:
```

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

namespace GOFUS.UI.Screens
{
    public class LoginScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Settings")]
        [SerializeField] private string backendUrl = "http://localhost:3000";

        private void Start()
        {
            // Add button listeners
            loginButton.onClick.AddListener(OnLoginClick);
            registerButton.onClick.AddListener(OnRegisterClick);

            // Clear status
            SetStatus("");

            // Focus username field
            usernameInput.Select();
        }

        private void OnLoginClick()
        {
            string username = usernameInput.text.Trim();
            string password = passwordInput.text;

            if (string.IsNullOrEmpty(username))
            {
                SetStatus("Please enter username", Color.red);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                SetStatus("Please enter password", Color.red);
                return;
            }

            SetStatus("Logging in...", Color.yellow);
            StartCoroutine(LoginCoroutine(username, password));
        }

        private void OnRegisterClick()
        {
            SetStatus("Registration not implemented yet", Color.yellow);
        }

        private IEnumerator LoginCoroutine(string username, string password)
        {
            // For now, just simulate login
            yield return new WaitForSeconds(1f);

            // Mock successful login
            SetStatus("Login successful!", Color.green);

            // TODO: Implement actual login
            // string url = backendUrl + "/api/auth/login";
            // using (UnityWebRequest request = UnityWebRequest.Post(url, form))
            // {
            //     yield return request.SendWebRequest();
            //     // Handle response
            // }
        }

        private void SetStatus(string message, Color color = default)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color == default ? Color.white : color;
            }
        }
    }
}
```

### Step 12: Attach Script to LoginPanel
```
1. Select LoginPanel in Hierarchy
2. In Inspector:
   - Add Component > Scripts > GOFUS.UI.Screens > LoginScreen

3. Link UI elements:
   - Username Input: Drag UsernameInput from Hierarchy
   - Password Input: Drag PasswordInput from Hierarchy
   - Login Button: Drag LoginButton from Hierarchy
   - Register Button: Drag RegisterButton from Hierarchy
   - Status Text: Drag StatusText from Hierarchy
```

### Step 13: Test the Scene
```
1. Save the scene (Ctrl+S)
2. Press Play button (▶)
3. You should see:
   - Login panel in center
   - Input fields
   - Buttons
   - Can type in fields
   - Clicking Login shows "Logging in..." then "Login successful!"
```

### Step 14: Add Background Image (Optional)
```
1. Copy any image from:
   C:\Users\HardM\Desktop\Enterprise\gofus\Cliente retro\resources\app\retroclient\loadingbanners\

2. Paste into:
   C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client\Assets\_Project\Resources\UI\

3. In Unity, image appears in Project window
4. Select Background GameObject
5. Drag image to Image component > Source Image
```

### Step 15: Make it the Default Scene
```
1. File > Build Settings
2. Add Open Scenes (adds LoginScene)
3. Drag LoginScene to top of list
4. Close Build Settings
```

---

## 🎨 Quick Styling Tips

### Make it Look Better
```
1. Background gradient:
   - Add UI > Image as child of Canvas (before LoginPanel)
   - Use gradient sprite or solid color

2. Round corners on panel:
   - Use a rounded rectangle sprite
   - Or add border Image

3. Better fonts:
   - Import custom fonts to Assets/Fonts/
   - Apply to TMP components

4. Button hover effects:
   - Button > Navigation > Visualize
   - Set Color Tint colors

5. Input field styling:
   - Adjust Image component on input fields
   - Add icons for username/password
```

---

## ✅ Verification Checklist

### Scene Setup
- [ ] Canvas exists in Hierarchy
- [ ] Canvas Scaler set to Scale With Screen Size
- [ ] Background Image/Panel visible
- [ ] LoginPanel centered on screen

### UI Elements
- [ ] Username input field works
- [ ] Password input field masks text
- [ ] Login button clickable
- [ ] Register button clickable
- [ ] Status text visible

### Script
- [ ] LoginScreen script attached to LoginPanel
- [ ] All UI elements linked in Inspector
- [ ] No compilation errors in Console
- [ ] Clicking Login shows status message

### Testing
- [ ] Press Play - scene loads
- [ ] Can type in username field
- [ ] Can type in password field (shows dots)
- [ ] Login button responds to click
- [ ] Status messages appear

---

## 🚨 Common Issues & Fixes

### Nothing Visible?
```
1. Check Canvas exists
2. Check Camera exists (should auto-create)
3. Check UI elements are children of Canvas
4. Check positions (might be off-screen)
```

### Can't Type in Input Fields?
```
1. Check EventSystem exists (auto-created with Canvas)
2. Check input fields have Interactable checked
3. Check no UI element is blocking input
```

### Script Errors?
```
1. Check namespace: GOFUS.UI.Screens
2. Check using statements at top
3. Check UI elements linked in Inspector
4. Create folders if missing: Assets/_Project/Scripts/UI/Screens/
```

### Buttons Don't Work?
```
1. Check button Interactable is checked
2. Check OnClick event is set (via script)
3. Check no transparent image blocking
4. Check EventSystem exists
```

---

## 🎯 Result

After following these steps, you should have:
1. ✅ A visible login screen
2. ✅ Working input fields
3. ✅ Clickable buttons
4. ✅ Status text feedback
5. ✅ No more gray screen!

**Time to complete: 15 minutes**

---

## 📝 Next Steps

Once login screen works:
1. Implement actual backend connection
2. Create character selection scene
3. Add scene transitions
4. Implement WebSocket connection
5. Create main game scene

But first - **GET THE LOGIN SCREEN WORKING!**