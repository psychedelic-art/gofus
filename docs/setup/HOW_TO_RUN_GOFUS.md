# How to Run GOFUS Unity Client

## Quick Start (Recommended)

### Option 1: Open in Unity Editor (Best for Development)

1. **Open Unity Hub**
   - Launch Unity Hub application

2. **Add Project**
   - Click "Add" or "Open"
   - Navigate to: `C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client`
   - Select the folder and click "Open"

3. **Open with Unity 6000.0.60f1**
   - Unity Hub will show the project
   - It should auto-detect Unity version 6000.0.60f1
   - Click on the project to open

4. **Let Unity Import**
   - Unity will compile scripts and import assets
   - This may take a few minutes on first load
   - Package errors for Tilemap/Aseprite won't affect gameplay

5. **Run the Game**
   - Once Unity opens, press the **Play button (▶)** at the top center
   - The game will start in the Game view
   - You can stop with the same button

---

## Option 2: Build and Run Standalone

### Build the Game

1. **In Unity Editor:**
   - Open the project as described above
   - Go to `File > Build Settings`

2. **Configure Build:**
   - Platform: PC, Mac & Linux Standalone (should be selected)
   - Target Platform: Windows
   - Architecture: x86_64

3. **Add Scenes:**
   - Click "Add Open Scenes" if scenes aren't listed
   - Or drag scenes from `Assets/_Project/Scenes/` to the build list
   - Ensure scenes are in correct order (Login/MainMenu first)

4. **Build:**
   - Click "Build" or "Build and Run"
   - Choose output folder (e.g., `gofus-client/Builds/Windows`)
   - Wait for build to complete

5. **Run the Built Game:**
   - Navigate to your build folder
   - Run `gofus.exe` (or whatever you named it)

---

## Development Workflow

### Testing in Editor (Fastest)

1. **Open Unity Editor**
2. **Open the Main Scene:**
   - In Project window: `Assets/_Project/Scenes/`
   - Double-click your main scene (likely MainMenu or Login)
3. **Press Play (▶)**
4. **Test Features:**
   - Login screen should appear
   - Use test credentials or local server
   - Navigate through UI screens

### Keyboard Shortcuts

- **Play/Stop:** `Ctrl + P`
- **Pause:** `Ctrl + Shift + P`
- **Step Frame:** `Ctrl + Alt + P` (while paused)
- **Maximize Game View:** `Shift + Space` (with Game view focused)

---

## Configuration

### Server Settings

The game can connect to different servers:

1. **Production Server:** `https://gofus-backend.vercel.app`
2. **Local Server:** `http://localhost:3000`

To switch servers:
- In game: Use the dropdown in Login screen
- Or modify: `Assets/_Project/Scripts/Networking/ServerConfig.cs`

### Test Credentials

For testing, you can use:
- Username: `test_player`
- Password: `test_password`

---

## Troubleshooting

### If Unity Won't Open:

1. **Check Unity Hub:**
   - Ensure Unity 6000.0.60f1 is installed
   - Try removing and re-adding the project

2. **Clear Cache:**
   ```cmd
   cd C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client
   rmdir /s /q Library
   rmdir /s /q Temp
   ```
   Then reopen in Unity (will reimport)

### If Play Button is Grayed Out:

1. **Check for compilation errors:**
   - Open Console window (`Window > General > Console`)
   - Fix any errors (though package errors can be ignored)

2. **Ensure scene is loaded:**
   - Load a scene from `Assets/_Project/Scenes/`

### Package Errors (Safe to Ignore):

The 4 package errors (TileTemplate, SpriteAtlas) won't prevent the game from running. They only affect:
- Aseprite file importing (not used in your project)
- Some advanced tilemap templates (not critical)

---

## Console/Command Line Run

### Quick Test via Command Line:

```cmd
"C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe" ^
-projectPath "C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client"
```

### Run in Play Mode from Command Line:

```cmd
"C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe" ^
-projectPath "C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client" ^
-executeMethod GameManager.AutoPlay
```

Note: AutoPlay method would need to be implemented to start play mode automatically.

---

## Build from Command Line

### Windows Build:

```cmd
"C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe" ^
-batchmode -quit ^
-projectPath "C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client" ^
-buildWindows64Player "Builds\Windows\gofus.exe" ^
-logFile build.log
```

---

## Project Features to Test

Once running, you can test:

1. **Login System**
   - Login screen with server selection
   - Authentication with backend

2. **Character Selection**
   - View character list
   - Select and play character

3. **Main Game**
   - Movement with WASD/Arrow keys
   - Inventory (Tab key)
   - Chat system
   - Settings menu

4. **Combat System**
   - Real-time and turn-based modes
   - Spell casting
   - Status effects

5. **UI Systems**
   - Screen transitions
   - HUD elements
   - Minimap
   - Notifications

---

## Performance Tips

1. **In Editor:**
   - Use Game view Stats button to monitor FPS
   - Keep Console closed when not debugging (impacts performance)

2. **Quality Settings:**
   - `Edit > Project Settings > Quality`
   - Select appropriate quality level

3. **Resolution:**
   - In Game view, select appropriate resolution
   - Or use Free Aspect for flexible testing

---

## Next Steps

1. **Open in Unity Editor** (easiest)
2. **Press Play** to start testing
3. **Explore the UI** and game systems
4. **Check Console** for any runtime errors
5. **Build standalone** when ready to share

The game should run despite the 4 package errors. Those only affect importing certain asset types, not runtime functionality.