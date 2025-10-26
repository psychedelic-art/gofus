# Unity 6 Package Errors - Fix Guide

## Overview
This guide addresses the 4 remaining Unity package errors in the GOFUS project:
1. TileTemplate errors in 2D Tilemap Extras package (2 errors)
2. SpriteAtlas errors in Aseprite package (2 errors)

---

## Error 1 & 2: TileTemplate Not Found (2D Tilemap Extras)

### The Error
```
Library\PackageCache\com.unity.2d.tilemap.extras@2338d989ff2a\Editor\Tiles\AutoTile\AutoTileTemplate.cs(12,37):
error CS0246: The type or namespace name 'TileTemplate' could not be found
```

### Root Cause
The 2D Tilemap Extras package is either incorrectly installed or has version compatibility issues with Unity 6.

### Solutions

#### Solution A: Reinstall via Package Manager (Recommended)
1. **Remove existing package:**
   - Delete any manually imported `2d-extras` folders from your Assets
   - Open `Window > Package Manager`
   - Find "2D Tilemap Extras" and click Remove

2. **Install correct version:**
   - In Package Manager, click the "+" button
   - Select "Add package from git URL"
   - Enter: `https://github.com/Unity-Technologies/2d-extras.git#master`
   - Or use specific Unity 6 compatible version: `com.unity.2d.tilemap.extras@6.0.0`

#### Solution B: Manual Installation
1. Open `Packages/manifest.json` in your project
2. Add or update this line in dependencies:
   ```json
   "com.unity.2d.tilemap.extras": "6.0.0"
   ```
3. Save and let Unity reimport

#### Solution C: Check Package Visibility
1. In Package Manager, click the gear icon (⚙️)
2. Select "Advanced Project Settings"
3. Enable "Show Preview Packages"
4. Search for "2D Tilemap Extras" again

---

## Error 3 & 4: SpriteAtlas Methods Not Found (Aseprite Package)

### The Error
```
Library\PackageCache\com.unity.2d.aseprite@dca42b450aa1\Editor\Common\InternalBridge\InternalEditorBridge.cs(38,75):
error CS1061: 'SpriteAtlas' does not contain a definition for 'SetV2'
error CS1061: 'SpriteAtlas' does not contain a definition for 'RegisterAndPackAtlas'
```

### Root Cause
The Aseprite package is using internal Unity API methods that have been changed/removed in Unity 6. This is a **known compatibility issue** with Unity 6.

### Solutions

#### Solution A: Remove Aseprite Package (If Not Needed)
If you're not using Aseprite files (.aseprite) in your project:

1. **Via Package Manager:**
   - Open `Window > Package Manager`
   - Find "2D Aseprite Importer"
   - Click "Remove"

2. **Via manifest.json:**
   - Open `Packages/manifest.json`
   - Remove the line: `"com.unity.2d.aseprite": "x.x.x"`
   - Save and restart Unity

#### Solution B: Disable Aseprite Package Temporarily
1. In Package Manager, find "2D Aseprite Importer"
2. Click "Disable" instead of Remove
3. This keeps the package but prevents compilation errors

#### Solution C: Wait for Update or Use Alternative
- **Check for Updates:** Unity/package maintainers need to update the Aseprite package for Unity 6
- **Alternative:** Convert .aseprite files to .png sprites manually and use Unity's built-in sprite tools

---

## Quick Fix Script

Create this PowerShell script to automatically fix the packages:

```powershell
# fix_unity_packages.ps1
$manifestPath = ".\gofus-client\Packages\manifest.json"
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json

# Update or add 2D Tilemap Extras
$manifest.dependencies."com.unity.2d.tilemap.extras" = "6.0.0"

# Remove Aseprite package if not needed (comment out if you need it)
if ($manifest.dependencies."com.unity.2d.aseprite") {
    $manifest.dependencies.PSObject.Properties.Remove("com.unity.2d.aseprite")
    Write-Host "Removed Aseprite package"
}

# Save updated manifest
$manifest | ConvertTo-Json -Depth 10 | Set-Content $manifestPath
Write-Host "Updated manifest.json - Please restart Unity"
```

---

## Alternative: Complete 2D Package Reset

If issues persist, you can reset all 2D packages:

1. **Remove all 2D packages:**
   ```json
   // In manifest.json, remove:
   "com.unity.feature.2d": "x.x.x",
   "com.unity.2d.aseprite": "x.x.x",
   "com.unity.2d.tilemap.extras": "x.x.x"
   ```

2. **Re-add only what you need:**
   ```json
   // Add back:
   "com.unity.feature.2d": "2.0.0",
   "com.unity.2d.tilemap.extras": "6.0.0"
   // Skip aseprite if not needed
   ```

3. **Clear cache and reimport:**
   - Close Unity
   - Delete `Library` folder
   - Reopen Unity (will reimport everything)

---

## Verification Steps

After applying fixes:

1. **Check Package Manager:**
   - Verify correct package versions
   - No error icons on packages

2. **Run compilation test:**
   ```cmd
   "C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe" ^
   -batchmode -quit ^
   -projectPath ".\gofus-client" ^
   -logFile compile_test.log ^
   -executeMethod UnityEditor.SyncVS.SyncSolution
   ```

3. **Open in Unity Editor:**
   - Let Unity's auto-resolution handle remaining issues
   - Check Console window for errors

---

## Summary

- **TileTemplate errors:** Reinstall 2D Tilemap Extras v6.0.0 via Package Manager
- **SpriteAtlas errors:** Remove/disable Aseprite package if not using .aseprite files
- These are Unity 6 compatibility issues that affect many projects
- Your game code is fine - these are package-level problems

## Next Steps

1. Try Solution A for each error type first
2. If issues persist, consider using Unity 2022.3 LTS (more stable package ecosystem)
3. Report issues to Unity if not already reported
4. Monitor Unity forums for official fixes/updates