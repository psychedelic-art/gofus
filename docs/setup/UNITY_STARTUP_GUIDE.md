# GOFUS Unity Startup Guide

## 🚀 AUTOMATED SETUP READY!

When you open Unity, the following will happen **automatically**:

### 1. **Package Verification** (Automatic)
The `AutoPackageImporter.cs` script will:
- ✅ Check for all required packages
- ✅ Prompt to install missing packages
- ✅ Detect if TextMeshPro needs importing
- ✅ Show import dialog if needed

### 2. **TMP Import Helper** (Semi-Automatic)
When TextMeshPro prompt appears:
- Click **"Import"** button
- Select all items (should be pre-selected)
- Click **"Import"** again at bottom-right

### 3. **Project Configuration** (Automatic)
After packages are verified:
- ✅ Sets 2D mode as default
- ✅ Configures camera for orthographic
- ✅ Optimizes quality settings for 2D
- ✅ Creates folder structure

---

## 📋 MANUAL STEPS (If Needed)

### Option A: Quick Setup (Recommended)
1. Open Unity
2. Wait for packages to download (progress bar at bottom)
3. When TMP dialog appears, click "Import"
4. Menu: **GOFUS → Project → Initialize Project**
5. Menu: **GOFUS → Project → Open Main Scene**

### Option B: Manual Package Install
If packages don't auto-install:

1. **Window → Package Manager**
2. Dropdown: **Unity Registry**
3. Search and install:
   - TextMeshPro
   - 2D Sprite
   - 2D Tilemap
   - Input System
   - Newtonsoft Json

4. **Window → TextMeshPro → Import TMP Essential Resources**

---

## 🎮 GOFUS MENU STRUCTURE

Once Unity compiles, you'll have these menu options:

### **GOFUS → Setup**
- **Configure for 2D Mode** - Auto-configure all 2D settings
- **Create 2D Scene** - Create a new 2D-ready scene
- **Verify Package Installation** - Check all packages
- **Import TMP Resources** - Manually import TextMeshPro
- **Reset Setup Status** - Reset and re-check everything

### **GOFUS → Project**
- **Initialize Project** - Create all folders and setup
- **Open Main Scene** - Open the main game scene
- **Create Test Assets** - Generate placeholder assets

### **GOFUS → Asset Migration**
- **Dofus Asset Processor** - Process extracted assets
- **Sprite Sheet Slicer** - Slice character sprites
- **Character Animation Generator** - Create animations
- **Asset Validation Report** - Check extraction status
- **Extraction Validator** - Validate asset pipeline

---

## ✅ SUCCESS INDICATORS

You'll know setup is complete when:

1. **Console shows:**
   ```
   [GOFUS] All required packages are installed!
   [GOFUS] ✓ TextMeshPro Essential Resources found!
   [GOFUS] ✓ Project configured for 2D development!
   ```

2. **Dialog appears:**
   ```
   "Setup Complete!"
   ✓ All packages installed
   ✓ TextMeshPro configured
   ✓ 2D mode enabled
   ✓ Project settings optimized
   ```

3. **No red errors in Console**

4. **Can press Play button without errors**

---

## 🔧 TROUBLESHOOTING

### "Missing Assembly References"
- Packages are still downloading
- Check bottom-right progress bar
- Wait for "Importing..." to complete

### "TMP Resources Not Found"
- Menu: **GOFUS → Setup → Import TMP Resources**
- Or: **Window → TextMeshPro → Import TMP Essential Resources**

### "Can't Enter Play Mode"
- Check Console for red errors
- Menu: **GOFUS → Setup → Verify Package Installation**
- Try: **Assets → Reimport All**

### "Scene is Black/Empty"
- Menu: **GOFUS → Project → Initialize Project**
- Menu: **GOFUS → Project → Open Main Scene**
- Check 2D button is ON in Scene view

---

## 🎯 FIRST PLAY TEST

Once everything is set up:

1. **Open Main Scene:**
   - Menu: **GOFUS → Project → Open Main Scene**
   - Or: Navigate to `Assets/_Project/Scenes/MainScene.unity`

2. **Verify 2D Mode:**
   - Scene view has **2D** button active (blue)
   - Camera shows orthographic view

3. **Press Play:**
   - Click Play button at top
   - Should enter play mode without errors
   - Basic scene loads with UI

4. **Test Systems:**
   - Check GameManager initializes
   - UI Canvas is visible
   - No null reference errors

---

## 📁 PROJECT STRUCTURE AFTER SETUP

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/          ✅ Game systems
│   │   ├── Combat/        ✅ Combat mechanics
│   │   ├── UI/            ✅ User interface
│   │   ├── Network/       ✅ Networking
│   │   ├── Editor/        ✅ Unity tools
│   │   └── Extraction/    ✅ Asset scripts
│   ├── Scenes/
│   │   └── MainScene.unity ✅ Created
│   ├── Prefabs/           ✅ Created
│   ├── Materials/         ✅ Created
│   ├── Textures/          ✅ Created
│   ├── Audio/             ✅ Created
│   └── ImportedAssets/    ✅ Ready for assets
├── TextMeshPro/           ✅ After import
└── Plugins/               ✅ After Newtonsoft import
```

---

## 🚦 READY CHECKLIST

Before starting development:

- [ ] Unity opened without crashes
- [ ] Packages downloaded (no progress bar)
- [ ] TMP Essential Resources imported
- [ ] Console has no red errors
- [ ] Can enter Play mode
- [ ] 2D mode is active
- [ ] GOFUS menu is visible
- [ ] Main Scene created/opened

---

## 🎨 NEXT STEPS

Once setup is complete:

1. **Extract Dofus Assets** (if available):
   ```bash
   cd gofus-client\Assets\_Project\Scripts\Extraction
   extract_priority_assets.bat "C:\Tools\ffdec\ffdec.exe" "C:\Dofus"
   ```

2. **Process Assets:**
   - Menu: **GOFUS → Asset Migration → Dofus Asset Processor**

3. **Begin Phase 8:**
   - Performance optimization
   - Visual polish
   - Audio implementation

---

## 💡 TIPS

- **Save Scene Often:** Ctrl+S
- **Save Project:** File → Save Project
- **Console Errors:** Double-click to go to error location
- **2D View:** Use scroll wheel to zoom
- **Play Mode:** Ctrl+P to toggle quickly

---

**Everything is prepared! Just open Unity and follow the prompts!**