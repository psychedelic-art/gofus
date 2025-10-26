# 🔴 FIX GRAY SCREEN - Quick Start Guide

## The Problem
Your Unity game shows a **gray screen** because:
- ✅ Code is working fine
- ❌ **No sprites/images have been extracted**
- ❌ **No UI graphics loaded**
- ❌ **Login screen has no visual assets**

## Option 1: Quick Test with Placeholder Graphics (5 minutes)

### Create Basic Test Sprites Manually

1. **Create folders in Unity project:**
```
gofus-client\Assets\_Project\Resources\
├── UI\
│   ├── login_background.png (any 1920x1080 image)
│   ├── button.png (any button image)
│   └── logo.png (any logo)
└── Characters\
    └── placeholder.png (any character sprite)
```

2. **Use ANY images temporarily:**
   - Download any free UI pack from Unity Asset Store
   - Or use any PNG images you have
   - Or create solid color rectangles in Paint

3. **Drag images into Unity:**
   - Open Unity
   - Drag PNG files into the Resources folders
   - Unity will import them as sprites

## Option 2: Extract Real Dofus Assets (Recommended)

### Step 1: Check Prerequisites

**Do you have Dofus installed?**
Check these locations:
- `C:\Program Files (x86)\Dofus`
- `C:\Games\Dofus`
- `C:\Users\[YourName]\AppData\Local\Ankama\Dofus`

**If NO Dofus:**
- You need Dofus client files (.swf files)
- Download Dofus from: https://www.dofus.com/en/download
- Or use any Dofus client files you have

### Step 2: Get Extraction Tools

1. **Download JPEXS FFDec** (Flash decompiler):
   - Go to: https://github.com/jindrapetrik/jpexs-decompiler/releases
   - Download: `ffdec_21.1.0_setup.exe` (or latest)
   - Install to: `C:\Program Files\FFDec\`

2. **Verify Java** (required by FFDec):
   ```cmd
   java -version
   ```
   If not installed: https://www.java.com/download/

### Step 3: Run Quick Extraction

**For Login Screen Only (10 minutes):**

Create this batch file: `quick_extract.bat`
```batch
@echo off
echo Extracting minimal assets for login screen...

set FFDEC="C:\Program Files\FFDec\ffdec.jar"
set DOFUS="C:\Program Files (x86)\Dofus"
set OUTPUT="C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client\Assets\ExtractedAssets\Raw"

:: Create directories
mkdir "%OUTPUT%\UI" 2>nul
mkdir "%OUTPUT%\Characters" 2>nul

:: Extract UI elements (for login screen)
echo Extracting UI...
java -jar %FFDEC% -export image "%OUTPUT%\UI" "%DOFUS%\content\gfx\ui\ui.swf"

:: Extract one character for testing
echo Extracting test character...
java -jar %FFDEC% -export image "%OUTPUT%\Characters" "%DOFUS%\content\gfx\sprites\actors\1.swf"

echo Done! Assets extracted to %OUTPUT%
pause
```

Run it:
```cmd
cd C:\Users\HardM\Desktop\Enterprise\gofus
quick_extract.bat
```

### Step 4: Import in Unity

1. **Open Unity with your project**
2. **Assets should auto-import** from ExtractedAssets folder
3. **If not**, manually drag ExtractedAssets folder into Unity Project window

## Option 3: Use Pre-made Test Assets (Fastest)

### Download Unity 2D Sample Assets

1. **In Unity:**
   - Window > Asset Store (or Package Manager)
   - Search: "2D Game Kit" or "2D Sprites"
   - Download any free 2D asset pack
   - Import into project

2. **Assign to Login Screen:**
   - Find `LoginScreen` prefab/scene
   - Assign any sprite to background
   - Assign button sprites to buttons

## Emergency Fix: Create Minimal Sprites

**PowerShell script to create colored squares as placeholders:**

```powershell
# create_emergency_sprites.ps1
Add-Type -AssemblyName System.Drawing

$path = "C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client\Assets\_Project\Resources\UI"
New-Item -Path $path -ItemType Directory -Force

# Create a simple button sprite
$bmp = New-Object System.Drawing.Bitmap(200, 60)
$graphics = [System.Drawing.Graphics]::FromImage($bmp)
$graphics.Clear([System.Drawing.Color]::Blue)
$bmp.Save("$path\button.png")

# Create a background
$bmp = New-Object System.Drawing.Bitmap(1920, 1080)
$graphics = [System.Drawing.Graphics]::FromImage($bmp)
$graphics.Clear([System.Drawing.Color]::DarkGray)
$bmp.Save("$path\background.png")

Write-Host "Emergency sprites created!"
```

## Verification

After adding ANY sprites:

1. **In Unity:**
   - Check Project window > Assets > ExtractedAssets or Resources
   - You should see imported sprites

2. **Run the game:**
   - Press Play
   - Should see login screen with graphics (not gray)

## Still Gray? Quick Debug

1. **Check Console for errors:**
   - Window > General > Console
   - Look for "Missing sprite" or "Null reference" errors

2. **Check if LoginScreen scene has sprites:**
   - Open: Assets\_Project\Scenes\LoginScene (or similar)
   - Select UI elements in Hierarchy
   - Check Inspector > Image component > Source Image field
   - Should have sprites assigned, not "None"

3. **Assign sprites manually:**
   - Select UI elements
   - Drag any sprite to Source Image field

## Fastest Solution Right Now

**Just to see something working:**

1. Create a folder: `gofus-client\Assets\TestSprites`
2. Add ANY .png images (download from Google Images if needed):
   - `background.png` (any 1920x1080 image)
   - `button.png` (any button-like image)
3. In Unity:
   - Open LoginScreen scene
   - Select Canvas > Background
   - Drag background.png to Image component
   - Select buttons
   - Drag button.png to their Image components
4. Press Play - should work!

## The Proper Solution

Once you have time, run the full extraction:
```cmd
cd "C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client\Assets\_Project\Scripts\Extraction"
extract_priority_assets.bat "C:\Program Files\FFDec\ffdec.jar" "C:\Program Files (x86)\Dofus"
```

This will extract all proper Dofus assets (takes 15-30 minutes).

---

## Need Help?

The gray screen is ONLY because of missing sprites. The code works fine. You just need ANY images in the project to see the UI!