# 🎮 GOFUS Unity Client - Master Implementation Plan

## 📋 Table of Contents
1. [Project Status](#project-status)
2. [Available Backend Services](#available-backend-services)
3. [Phase-by-Phase Implementation](#phase-by-phase-implementation)
4. [Immediate Actions](#immediate-actions)

---

## 🔴 Project Status

### Current State
- ✅ Unity project created (Unity 6000.0.60f1)
- ✅ Basic folder structure in place
- ✅ Compilation errors fixed (except 4 package warnings)
- ❌ **NO UI VISIBLE** - Gray screen when running
- ❌ No sprites/assets imported yet
- ❌ No networking implemented
- ❌ No scenes properly configured

### Critical Issues
1. **No Login Screen** - Need to create from scratch
2. **No Assets** - Cliente retro has vector SWFs, need to extract or create
3. **No Network Connection** - WebSocket client not implemented

---

## 🔌 Available Backend Services

### Authentication (gofus-backend)
```
POST /api/auth/register - Create account
POST /api/auth/login - Login (returns JWT token)
POST /api/auth/logout - Logout
```

### Character Management (gofus-backend)
```
GET /api/characters - List all characters
POST /api/characters - Create character
GET /api/characters/[id] - Get character details
PATCH /api/characters/[id] - Update character
DELETE /api/characters/[id] - Delete character
```

### Game Server (WebSocket - port 3001)
```
Events:
- authenticate { token, characterId }
- movement:request { mapId, cellId }
- chat:message { channel, message }
- combat:action { actionId, targetId }
```

---

## 📅 Phase-by-Phase Implementation

### PHASE 1: LOGIN SCREEN (Priority 1 - Do First!)
**Goal: See a working login screen with graphics**

#### Step 1.1: Create Login Scene
```
1. In Unity:
   - File > New Scene
   - Save as: Assets/_Project/Scenes/LoginScene.unity

2. Create UI Canvas:
   - GameObject > UI > Canvas
   - Set Canvas Scaler to Scale with Screen Size (1920x1080)
```

#### Step 1.2: Add Background
```
1. Add background image:
   - In Canvas, create UI > Image
   - Name it "Background"
   - Stretch to fill screen (Alt+Shift stretch)

2. Use placeholder image:
   - Copy any image from: Cliente retro\resources\app\retroclient\loadingbanners\
   - To: Assets/_Project/Resources/UI/Backgrounds/
   - Drag image to Background's Source Image
```

#### Step 1.3: Create Login Form
```
1. Create Panel for login form:
   - In Canvas, UI > Panel
   - Name: "LoginPanel"
   - Size: 400x300
   - Center on screen

2. Add input fields:
   - Username: UI > Input Field - TextMeshPro
   - Password: UI > Input Field - TextMeshPro (Content Type: Password)

3. Add buttons:
   - Login Button: UI > Button - TextMeshPro
   - Register Button: UI > Button - TextMeshPro
```

#### Step 1.4: Create LoginScreen Script
```csharp
// Assets/_Project/Scripts/UI/Screens/LoginScreen.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

namespace GOFUS.UI.Screens
{
    public class LoginScreen : MonoBehaviour
    {
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;

        [Header("Buttons")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusText;

        private void Start()
        {
            loginButton.onClick.AddListener(OnLoginClick);
            registerButton.onClick.AddListener(OnRegisterClick);
        }

        private void OnLoginClick()
        {
            StartCoroutine(Login());
        }

        private IEnumerator Login()
        {
            string url = "http://localhost:3000/api/auth/login";
            // Implementation here
            yield return null;
        }
    }
}
```

#### Step 1.5: Test Login Scene
```
1. Open LoginScene
2. Press Play
3. You should see:
   - Background image
   - Login form with inputs
   - Buttons (not functional yet)
```

---

### PHASE 2: NETWORKING SETUP
**Goal: Connect to backend and game server**

#### Step 2.1: Install WebSocket Package
```
1. Download NativeWebSocket:
   - https://github.com/endel/NativeWebSocket
   - Or use Package Manager: Add from git URL

2. Create NetworkManager:
   - Assets/_Project/Scripts/Networking/NetworkManager.cs
```

#### Step 2.2: Implement Authentication
```csharp
// NetworkManager.cs
public class NetworkManager : MonoBehaviour
{
    private const string BACKEND_URL = "http://localhost:3000";
    private const string GAME_SERVER_URL = "ws://localhost:3001";

    public async Task<bool> Login(string username, string password)
    {
        // POST to /api/auth/login
        // Store JWT token
        // Return success
    }
}
```

#### Step 2.3: Connect to Game Server
```csharp
private WebSocket websocket;

public async void ConnectToGameServer(string token)
{
    websocket = new WebSocket(GAME_SERVER_URL);
    websocket.OnOpen += OnWebSocketOpen;
    websocket.OnMessage += OnWebSocketMessage;
    await websocket.Connect();
}
```

---

### PHASE 3: CHARACTER SELECTION
**Goal: Show character list after login**

#### Step 3.1: Create Character Selection Scene
```
1. New Scene: CharacterSelectionScene.unity
2. UI Elements:
   - Character slots (3-6 slots)
   - Create character button
   - Play button
   - Back button
```

#### Step 3.2: Load Characters from API
```csharp
public async Task<List<Character>> GetCharacters()
{
    // GET /api/characters
    // Parse response
    // Return character list
}
```

---

### PHASE 4: MAIN GAME SCENE
**Goal: Enter game world**

#### Step 4.1: Create Game Scene
```
1. New Scene: GameScene.unity
2. Setup:
   - Isometric camera
   - Grid system (14x20)
   - UI overlay
```

#### Step 4.2: Implement Map Rendering
```csharp
public class MapRenderer : MonoBehaviour
{
    private const int GRID_WIDTH = 14;
    private const int GRID_HEIGHT = 20;
    private const float CELL_WIDTH = 86f;
    private const float CELL_HEIGHT = 43f;
}
```

---

## 🚀 Immediate Actions

### RIGHT NOW - Fix Gray Screen

#### Option A: Quick Manual Setup
```
1. Open Unity
2. Create new scene: File > New Scene
3. Add Canvas: GameObject > UI > Canvas
4. Add Image to Canvas
5. Use any PNG as background
6. Save as LoginScene
7. Press Play - Should see something!
```

#### Option B: Import Test Assets
```
1. Copy images from:
   FROM: C:\Users\HardM\Desktop\Enterprise\gofus\Cliente retro\resources\app\retroclient\loadingbanners\
   TO: C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client\Assets\TestImages\

2. In Unity, they auto-import
3. Use in your scenes
```

### Step-by-Step Verification

#### 1. Check Current Scene
```
In Unity:
1. Open Scenes folder
2. Double-click any scene
3. Look at Hierarchy window
4. If empty, create UI elements
```

#### 2. Create Minimal UI
```
1. GameObject > UI > Canvas
2. In Canvas: UI > Image (background)
3. In Canvas: UI > Button (test button)
4. Press Play
```

#### 3. Verify Scripts Compile
```
1. Check Console window (no red errors)
2. If errors, fix them first
3. Only yellow warnings are OK
```

---

## 📂 File Organization Plan

### Move all root files to docs/
```
docs/
├── setup/
│   ├── UNITY_SETUP_STATUS.md
│   ├── UNITY_STARTUP_GUIDE.md
│   └── HOW_TO_RUN_GOFUS.md
├── fixes/
│   ├── COMPILATION_FIXES_REPORT.md
│   ├── REMAINING_ERRORS.md
│   └── UNITY_PACKAGE_ERRORS_FIX_GUIDE.md
├── assets/
│   ├── FIX_GRAY_SCREEN_NOW.md
│   └── QUICK_FIX_IMAGES.md
└── scripts/
    ├── add_unity_to_path.ps1
    ├── extract_retro_assets.bat
    └── copy_retro_images.bat
```

---

## 🎯 Success Metrics

### Phase 1 Complete When:
- [ ] Login screen visible
- [ ] Background image showing
- [ ] Input fields working
- [ ] Can type username/password

### Phase 2 Complete When:
- [ ] Can send login request
- [ ] Receive JWT token
- [ ] Connect to WebSocket server
- [ ] Receive auth:success event

### Phase 3 Complete When:
- [ ] Character list loads
- [ ] Can select character
- [ ] Can create new character
- [ ] Transition to game works

### Phase 4 Complete When:
- [ ] Map renders correctly
- [ ] Player visible on map
- [ ] Can click to move
- [ ] Other players visible

---

## 🔧 Current Tools & Paths

### Unity
```
Version: 6000.0.60f1
Location: C:\Program Files\Unity\Hub\Editor\6000.0.60f1\
Project: C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client\
```

### Backend Services
```
Backend: http://localhost:3000
Game Server: ws://localhost:3001
```

### Asset Sources
```
Cliente retro: C:\Users\HardM\Desktop\Enterprise\gofus\Cliente retro\
Cliente2: C:\Users\HardM\Desktop\Enterprise\gofus\Cliente2\
```

---

## 🆘 Troubleshooting

### Gray Screen?
1. No scene loaded
2. No UI elements in scene
3. Camera not positioned correctly
4. Canvas not configured

### Can't See UI?
1. Check Canvas render mode
2. Check Camera settings
3. Check layer masks
4. Check Canvas Scaler

### Scripts Not Working?
1. Check Console for errors
2. Attach scripts to GameObjects
3. Link UI elements in Inspector
4. Check namespaces match

---

## Next Step: START WITH PHASE 1.1
**Create LoginScene.unity right now!**