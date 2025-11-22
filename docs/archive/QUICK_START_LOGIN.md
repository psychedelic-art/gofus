# Quick Start: Using Your Programmatic LoginScreen

## Fastest Way to Get It Running (2 minutes)

### Step 1: Create New Scene
1. Open Unity
2. File → New Scene
3. File → Save As → Name it "LoginScene"

### Step 2: Create Game Objects
1. **Create UIManager:**
   - Hierarchy → Create Empty → Name: "UIManager"
   - Add Component → Scripts → GOFUS.Core → UIManager

2. **Create EventSystem (for input):**
   - Hierarchy → UI → Event System (this creates EventSystem automatically)

3. **Create LoginScreen:**
   - Hierarchy → Create Empty → Name: "LoginScreen"
   - Add Component → Scripts → GOFUS.UI.Screens → LoginScreen

### Step 3: Press Play
That's it! The LoginScreen will:
- Automatically create its Canvas
- Build all UI elements programmatically
- Show username/password fields
- Create login/register buttons
- Be fully functional

## What Happens When You Press Play

Your LoginScreen's code flow:

1. **Awake()** (from UIScreen base class)
   - Creates Canvas if not present
   - Adds CanvasGroup for fading
   - Hides screen initially

2. **Initialize()** (from LoginScreen)
   - Calls CreateUI()
   - Sets up event handlers
   - Loads saved credentials
   - Checks server status

3. **CreateUI()** builds everything:
   ```
   LoginPanel (dark background)
   ├── Title ("GOFUS - Login")
   ├── UsernameField
   ├── PasswordField
   ├── RememberMeToggle
   ├── ShowPasswordToggle
   ├── LoginButton
   ├── RegisterButton
   ├── ForgotPasswordButton
   ├── OfflineModeButton
   ├── ServerDropdown
   ├── ServerStatusText
   └── StatusText
   ```

## Test It Works

1. **Type in fields:**
   - Username field accepts text
   - Password field shows dots

2. **Click Show Password:**
   - Password becomes visible

3. **Select Server:**
   - Dropdown switches between Live/Local

4. **Click Login:**
   - Shows "Authenticating..."
   - If no backend: Shows demo success
   - If backend running: Real authentication

## Connecting to Backend

Your code already handles this! It will:
1. Check if backend is running (port 3000)
2. Try to authenticate
3. Fall back to demo mode if offline

## Visual Customization

To change colors/sizes, modify in LoginScreen.cs:

```csharp
// Panel background color
bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // Dark gray

// Button colors
bg.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Medium gray

// Panel size (in CreatePanel)
rect.anchorMin = new Vector2(0.3f, 0.3f); // 30% from edges
rect.anchorMax = new Vector2(0.7f, 0.7f); // 70% to edges
```

## Benefits of Your Approach

✅ **No Unity Scene Conflicts** - Everything in code
✅ **Easy to Version Control** - Just .cs files
✅ **Reusable Components** - CreateButton(), CreateInputField()
✅ **Dynamic Layouts** - Adapts to screen sizes
✅ **Professional Pattern** - Used by AAA games

## Troubleshooting

### Gray Screen?
- Check EventSystem exists
- Verify LoginScreen GameObject is active
- Look for errors in Console

### Can't Type?
- EventSystem must exist
- Canvas needs GraphicRaycaster

### Buttons Don't Work?
- Check Console for errors
- Verify button listeners are added

## Next: Add Character Selection

Once login works, create CharacterSelectionScreen.cs using the same pattern:
```csharp
public class CharacterSelectionScreen : UIScreen
{
    private void CreateUI()
    {
        CreatePanel("CharacterPanel", transform);
        CreateCharacterSlots();
        CreateButtons();
    }
    // Similar programmatic creation...
}
```

Your programmatic approach is excellent for a professional MMO client!