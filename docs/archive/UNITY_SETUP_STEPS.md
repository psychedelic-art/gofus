# Unity Setup Steps - Using Your Programmatic LoginScreen

## Quick Setup (2 minutes)

### Step 1: Open Unity and Create Scene
1. Open Unity Hub → Open `gofus-client` project
2. In Unity: **File → New Scene**
3. **File → Save As** → Name it "LoginScene" → Save in `Assets/_Project/Scenes/`

### Step 2: Create Required GameObjects

#### A. Create UIManager
1. In Hierarchy: **Right-click → Create Empty**
2. Rename to "UIManager"
3. With UIManager selected, in Inspector:
   - **Add Component** → Type "UIManager" → Select `GOFUS.UI.UIManager`

#### B. Create EventSystem (Required for Input!)
1. In Hierarchy: **Right-click → UI → Event System**
   - This automatically creates the EventSystem GameObject

### Step 3: Start the Scene
1. Press **Play** button (▶)
2. The UIManager will automatically:
   - Create the LoginScreen programmatically
   - Build all UI elements through code
   - Show the login interface

## What You'll See

When you press Play, your LoginScreen code creates:
```
Canvas (auto-created)
└── LoginScreen
    └── LoginPanel (dark background)
        ├── Title: "GOFUS - Login"
        ├── UsernameField
        ├── PasswordField
        ├── RememberMe checkbox
        ├── ShowPassword checkbox
        ├── Login button
        ├── Register button
        ├── ForgotPassword link
        ├── OfflineMode button
        ├── ServerDropdown (Live/Local)
        └── Status text
```

## Testing Your Login Screen

### 1. Basic Functionality
- ✅ Type in username field → Text appears
- ✅ Type in password field → Shows dots/asterisks
- ✅ Click "Show Password" → Password becomes visible
- ✅ Server dropdown → Switch between Live Server / Local Server

### 2. Login Flow
- Enter any username (3+ characters)
- Enter any password (6+ characters)
- Click **Login**
- You'll see: "Authenticating..." → "Login successful!" (demo mode)

### 3. Server Connection
- **Local Server** selected → Tries http://localhost:3000
- **Live Server** selected → Tries https://gofus-backend.vercel.app
- If backend not running → Falls back to demo mode

## Compilation Errors?

If you see errors about missing types:

1. **CharacterSelectionScreen not found:**
   ```csharp
   // In UIManager.cs line 86, comment out:
   // CreateScreen<CharacterSelectionScreen>(ScreenType.CharacterSelection);
   ```

2. **MainMenuScreen not found:**
   ```csharp
   // In UIManager.cs line 85, comment out:
   // CreateScreen<MainMenuScreen>(ScreenType.MainMenu);
   ```

3. **Other missing screens:**
   - Comment them out in `CreateAllScreens()` method
   - They'll be created later

## How Your Code Works

### 1. UIManager Initialization
```
UIManager.Initialize()
├── Creates Dictionary of screens
├── Creates Canvas objects
└── Calls CreateAllScreens()
    └── CreateScreen<LoginScreen>()
        ├── Creates GameObject
        ├── Adds LoginScreen component
        └── Calls LoginScreen.Initialize()
            └── Calls CreateUI() ← Your programmatic UI
```

### 2. Your LoginScreen.CreateUI()
```csharp
private void CreateUI()
{
    CreatePanel("LoginPanel", transform);      // Dark background
    CreateTitle("GOFUS - Login", panel);       // Title text
    CreateInputFields(panel.transform);        // Username & Password
    CreateButtons(panel.transform);            // All buttons
    CreateStatusDisplay(panel.transform);      // Status messages
    CreateServerSelection(panel.transform);    // Server dropdown
}
```

Each `Create` method builds UI elements programmatically - no manual Unity Editor work needed!

## Advantages of Your Approach

✅ **Version Control Friendly** - All UI in code, no scene conflicts
✅ **Reusable Components** - CreateButton(), CreateInputField() can be reused
✅ **Dynamic Layouts** - Easy to adjust positions in code
✅ **Professional Pattern** - Used by major game studios
✅ **Easy Testing** - Can unit test UI creation

## Next Steps

Once login works:

1. **Test with Backend:**
   ```bash
   cd gofus-backend
   npm start
   # Backend runs on http://localhost:3000
   ```

2. **Create More Screens:**
   - Copy LoginScreen pattern
   - Create CharacterSelectionScreen.cs
   - Use same programmatic approach

3. **Add Visual Polish:**
   - Load background images
   - Add animations
   - Improve colors/styling

## Troubleshooting

### Nothing Appears?
- Check EventSystem exists in Hierarchy
- Check Console for errors
- Verify UIManager component is attached

### Can't Type in Fields?
- EventSystem must exist
- Check no errors in Console

### Gray Screen Still?
- This is normal! Your UI creates everything programmatically
- Press Play to see the UI
- Check Console for any errors

## Success Checklist

- [ ] Scene created and saved
- [ ] UIManager GameObject exists
- [ ] EventSystem exists
- [ ] Press Play → Login screen appears
- [ ] Can type in username/password
- [ ] Login button works (shows status)

Your programmatic UI approach is excellent for an MMO client!