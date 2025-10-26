# GOFUS Dofus Asset Extraction Guide

## Complete guide for extracting Dofus assets using JPEXS FFDec

---

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Dofus File Structure](#dofus-file-structure)
4. [Extraction Scripts](#extraction-scripts)
5. [Asset Priority List](#asset-priority-list)
6. [Character Class IDs](#character-class-ids)
7. [Common File Locations](#common-file-locations)
8. [FFDec Command Reference](#ffdec-command-reference)
9. [Troubleshooting](#troubleshooting)
10. [Next Steps](#next-steps)

---

## Overview

This guide provides comprehensive instructions for extracting all Dofus game assets using JPEXS Free Flash Decompiler (FFDec). The extracted assets will be used to build the GOFUS client in Unity.

### What Gets Extracted

- **Character Sprites**: All 18 character classes with animations
- **UI Elements**: Buttons, windows, icons, cursors, panels
- **Map Assets**: Tiles, objects, interactive elements, backgrounds
- **Effects**: Spell effects, particles, buff/debuff indicators
- **Audio Files**: Music, sound effects, ambiance, UI sounds
- **Items**: Equipment, resources, consumables, pets, mounts
- **Creatures**: Monsters, NPCs, companions

---

## Prerequisites

### Required Software

1. **JPEXS Free Flash Decompiler (FFDec)**
   - Download: https://github.com/jindrapetrik/jpexs-decompiler/releases
   - Version: 19.0.0 or higher recommended
   - Install location: `C:\Program Files\FFDec\` (or custom)

2. **Java Runtime Environment (JRE)**
   - Version: 8 or higher
   - Download: https://www.java.com/download/
   - Verify: Run `java -version` in command prompt

3. **Dofus Client Installation**
   - Official Dofus client must be installed
   - Default location: `C:\Program Files (x86)\Dofus`
   - Alternative: `%LOCALAPPDATA%\Dofus` or `C:\Dofus`

### System Requirements

- **OS**: Windows 10/11 (64-bit)
- **RAM**: 8GB minimum, 16GB recommended
- **Storage**: 20GB free space for extracted assets
- **CPU**: Multi-core processor recommended for parallel extraction

---

## Dofus File Structure

### Standard Installation Paths

```
C:\Program Files (x86)\Dofus\
├── content\
│   ├── gfx\
│   │   ├── sprites\
│   │   │   ├── actors\
│   │   │   │   ├── characters\      # Character class sprites
│   │   │   │   │   ├── 01.swf      # Feca
│   │   │   │   │   ├── 02.swf      # Osamodas
│   │   │   │   │   └── ...
│   │   │   │   ├── monsters\        # Monster sprites
│   │   │   │   └── npcs\            # NPC sprites
│   │   ├── ui\                      # User Interface elements
│   │   │   ├── buttons.swf
│   │   │   ├── windows.swf
│   │   │   ├── icons\
│   │   │   └── cursors.swf
│   │   ├── maps\                    # Map assets
│   │   │   ├── tiles\               # Ground tiles
│   │   │   ├── objects\             # Decorative objects
│   │   │   └── interactive\         # Interactive elements
│   │   ├── effects\                 # Visual effects
│   │   │   ├── spells\              # Spell effects
│   │   │   ├── particles\           # Particle systems
│   │   │   └── animations\          # General animations
│   │   └── items\                   # Item icons
│   │       ├── equipment\
│   │       │   ├── weapons\
│   │       │   ├── armor\
│   │       │   └── accessories\
│   │       ├── resources\
│   │       └── consumables\
│   └── sounds\                      # Audio files
│       ├── music\
│       ├── sfx\
│       ├── ambiance\
│       └── ui\
```

### File Format Details

- **SWF Files**: Flash files containing sprites, animations, and assets
- **Embedded Assets**: Images (PNG/JPEG), shapes (vector), sounds (MP3)
- **Naming Convention**: Numerical IDs or descriptive names

---

## Extraction Scripts

### 1. Complete Extraction (Comprehensive)

**File**: `extract_dofus_assets_complete.bat`

**Purpose**: Extracts ALL Dofus assets systematically

**Usage**:
```batch
extract_dofus_assets_complete.bat
```

**With custom paths**:
```batch
extract_dofus_assets_complete.bat "C:\Path\to\ffdec.jar" "C:\Path\to\Dofus"
```

**Features**:
- Extracts all 18 character classes
- Complete UI library
- Full map asset collection
- All effects and particles
- Complete audio library
- All item icons
- Detailed logging
- Error handling and retry logic
- Progress tracking
- Automatic report generation

**Estimated Time**: 2-4 hours (depending on system)

**Output Size**: ~10-15GB

---

### 2. Advanced PowerShell Extraction

**File**: `extract_dofus_assets_advanced.ps1`

**Purpose**: Advanced extraction with parallel processing and detailed progress

**Usage**:
```powershell
.\extract_dofus_assets_advanced.ps1
```

**With parameters**:
```powershell
.\extract_dofus_assets_advanced.ps1 `
    -FFDecPath "C:\FFDec\ffdec.jar" `
    -DofusPath "C:\Dofus" `
    -MaxThreads 8
```

**Parameters**:
- `-FFDecPath`: Path to ffdec.jar
- `-DofusPath`: Dofus installation directory
- `-OutputPath`: Custom output location
- `-MaxThreads`: Number of parallel extraction threads (default: 4)
- `-ExtractPriorityOnly`: Extract only MVP assets
- `-SkipValidation`: Skip prerequisite checks

**Features**:
- Color-coded console output
- Real-time progress bars
- Parallel processing support
- ETA calculation
- Retry logic for failed extractions
- Comprehensive error logging
- Automatic report generation
- Opens output folder when complete

**Estimated Time**: 1-3 hours (with parallel processing)

---

### 3. Priority Asset Extraction (MVP)

**File**: `extract_priority_assets.bat`

**Purpose**: Quick extraction of minimum viable assets

**Usage**:
```batch
extract_priority_assets.bat
```

**What Gets Extracted**:
- 4 core character classes (Feca, Sram, Eniripsa, Iop)
- Essential UI elements (buttons, windows, icons, cursor)
- 10 basic map tile sets
- Core combat effects (damage, heal)
- 5 essential UI sounds

**Estimated Time**: 15-30 minutes

**Output Size**: ~500MB-1GB

**Use Case**:
- Quick prototyping
- Testing asset pipeline
- Minimum viable client development
- Demo/preview builds

---

## Asset Priority List

### Priority 1: Critical (Required for MVP)

**Characters** (4 classes):
1. **Feca** (ID: 01) - Tank/Support
2. **Sram** (ID: 04) - Stealth/DPS
3. **Eniripsa** (ID: 07) - Healer
4. **Iop** (ID: 08) - Melee DPS

**UI Elements**:
- Buttons (normal, hover, pressed, disabled states)
- Windows (main frame, close button)
- Cursor
- Basic icons (health, mana, inventory)

**Maps**:
- 10 essential tile sets (grass, stone, water, dirt, sand)
- Basic collision tiles

**Effects**:
- Damage indicator
- Heal effect
- Movement indicator

**Audio**:
- UI click sound
- Error sound
- Success sound

### Priority 2: High (Needed for Alpha)

**Characters** (4 additional classes):
5. **Osamodas** (ID: 02) - Summoner
6. **Cra** (ID: 09) - Ranged DPS
7. **Sadida** (ID: 10) - Summoner/Control
8. **Enutrof** (ID: 03) - Support/Utility

**UI Elements**:
- All window types
- Complete icon set
- Tooltips
- Chat bubbles
- Health/energy bars

**Maps**:
- Complete tile library
- Decorative objects (trees, rocks, buildings)
- Interactive elements (doors, chests)

**Effects**:
- All spell effects for Priority 1 & 2 classes
- Buff/debuff indicators
- Status effects

**Audio**:
- Background music (3-5 tracks)
- Combat sounds
- Movement sounds

### Priority 3: Medium (Needed for Beta)

**Characters** (5 additional classes):
9. **Xelor** (ID: 05) - Time mage
10. **Ecaflip** (ID: 06) - Gambler/DPS
11. **Sacrier** (ID: 11) - Berserker
12. **Pandawa** (ID: 12) - Tank/Support
13. **Rogue** (ID: 13) - DPS/Traps

**Items**:
- Equipment icons (weapons, armor, accessories)
- Resource icons
- Consumable icons

**Creatures**:
- 20 common monster sprites
- 10 NPC sprites

**Effects**:
- Environmental effects (rain, snow, fog)
- Particle effects
- Animation effects

### Priority 4: Low (Nice to have)

**Characters** (5 remaining classes):
14. **Masqueraider** (ID: 14)
15. **Foggernaut** (ID: 15)
16. **Eliotrope** (ID: 16)
17. **Huppermage** (ID: 17)
18. **Ouginak** (ID: 18)

**Complete Asset Library**:
- All remaining monsters
- All NPCs
- Complete item database
- All spell effects
- Full audio library

### Priority 5: Optional (Polish)

- Alternative character skins
- Seasonal effects
- Special event assets
- Emotes and animations
- Achievement icons

---

## Character Class IDs

### Complete Class Mapping

| ID | Class Name      | Type            | Role           | Priority | SWF File |
|----|----------------|-----------------|----------------|----------|----------|
| 01 | Feca           | Support/Tank    | Tank/Support   | 1        | 01.swf   |
| 02 | Osamodas       | Summoner        | Summoner       | 2        | 02.swf   |
| 03 | Enutrof        | Support         | Support/Utility| 2        | 03.swf   |
| 04 | Sram           | Stealth         | Stealth/DPS    | 1        | 04.swf   |
| 05 | Xelor          | Mage            | Time Mage      | 3        | 05.swf   |
| 06 | Ecaflip        | DPS             | Gambler/DPS    | 3        | 06.swf   |
| 07 | Eniripsa       | Healer          | Healer         | 1        | 07.swf   |
| 08 | Iop            | Melee DPS       | Melee DPS      | 1        | 08.swf   |
| 09 | Cra            | Ranged DPS      | Ranged DPS     | 2        | 09.swf   |
| 10 | Sadida         | Summoner        | Summoner/Control| 2       | 10.swf   |
| 11 | Sacrier        | Berserker       | Berserker      | 3        | 11.swf   |
| 12 | Pandawa        | Tank            | Tank/Support   | 3        | 12.swf   |
| 13 | Rogue          | DPS             | DPS/Traps      | 4        | 13.swf   |
| 14 | Masqueraider   | Hybrid          | Versatile      | 4        | 14.swf   |
| 15 | Foggernaut     | Mage/Engineer   | Mage/Engineer  | 4        | 15.swf   |
| 16 | Eliotrope      | Mage/Mobility   | Portal Mage    | 5        | 16.swf   |
| 17 | Huppermage     | Mage/Elemental  | Elemental Mage | 5        | 17.swf   |
| 18 | Ouginak        | Melee/Beast     | Melee/Beast    | 5        | 18.swf   |

### Character Sprite Components

Each character SWF contains:
- **8 Directions**: North, NE, East, SE, South, SW, West, NW
- **Multiple Animations**:
  - Idle (standing still)
  - Walk (movement)
  - Run (fast movement)
  - Attack (basic attack)
  - Cast (spell casting)
  - Hit (taking damage)
  - Death (character dies)
  - Emotes (various expressions)

**Estimated Sprites per Character**: 200-500 individual sprites

---

## Common File Locations

### Typical Dofus Installations

1. **Default (Ankama Launcher)**:
   ```
   C:\Program Files (x86)\Dofus\
   ```

2. **Alternative (Steam)**:
   ```
   C:\Program Files (x86)\Steam\steamapps\common\Dofus\
   ```

3. **User Profile**:
   ```
   C:\Users\[USERNAME]\AppData\Local\Dofus\
   ```

4. **Custom Installation**:
   ```
   C:\Dofus\
   C:\Games\Dofus\
   ```

### Configuration Files

- **Player Settings**: `C:\Users\[USERNAME]\AppData\Roaming\Dofus`
- **Cache**: `C:\Users\[USERNAME]\AppData\Local\Dofus\cache`
- **Logs**: `C:\Users\[USERNAME]\AppData\Roaming\Dofus\logs`

### Asset CDN URLs

Dofus also loads assets from CDN:
```
http://dl-ak.ankama.com/games/dofus/
https://cdn.dofus.com/content/
```

---

## FFDec Command Reference

### Basic Extraction Command

```batch
java -jar ffdec.jar -export <types> <output> <input> [options]
```

### Asset Types

| Type       | Description                    | Output Format        |
|------------|--------------------------------|----------------------|
| `image`    | Raster images                  | PNG, JPEG, GIF       |
| `shape`    | Vector shapes                  | SVG, PDF, Canvas     |
| `sprite`   | Movie clips/sprites            | SVG, Canvas          |
| `sound`    | Audio files                    | MP3, WAV, FLV        |
| `movie`    | Video content                  | AVI, FLV             |
| `text`     | Text content                   | TXT, HTML            |
| `script`   | ActionScript code              | AS, P-code           |
| `font`     | Font resources                 | TTF                  |
| `binary`   | Binary data                    | BIN                  |
| `fla`      | Complete project               | FLA, XFL             |

### Format Options

```batch
-format image:png              # Images as PNG
-format image:png_gif          # Images as PNG or GIF
-format shape:svg              # Shapes as SVG
-format sound:mp3              # Audio as MP3
-format sound:wav              # Audio as WAV
-format movie:avi              # Video as AVI
```

### Common Options

```batch
-onerror ignore                # Ignore errors and continue
-onerror retry 3               # Retry failed operations 3 times
-timeout 120                   # Timeout per operation (seconds)
-exportTimeout 300             # Total export timeout (seconds)
-selectid 1-10,15,20-          # Select specific character IDs
-selectclass com.package.*     # Select by class name (AS3)
```

### Example Commands

**Extract all images from a character SWF**:
```batch
java -jar ffdec.jar -export image "C:\Output\Feca" "C:\Dofus\content\gfx\sprites\actors\characters\01.swf" -format image:png
```

**Extract images and shapes**:
```batch
java -jar ffdec.jar -export image,shape "C:\Output\UI" "C:\Dofus\content\gfx\ui\buttons.swf" -format image:png,shape:svg
```

**Extract with error handling**:
```batch
java -jar ffdec.jar -export image "C:\Output" "input.swf" -onerror ignore -timeout 60
```

**Batch extract all SWF files in a directory**:
```batch
for %%f in ("C:\Dofus\content\gfx\maps\tiles\*.swf") do (
    java -jar ffdec.jar -export image "C:\Output\Tiles" "%%f" -format image:png
)
```

---

## Troubleshooting

### Common Issues

#### 1. "Java not found" Error

**Problem**: Java is not installed or not in PATH

**Solution**:
```batch
# Verify Java installation
java -version

# If not found, download from:
https://www.java.com/download/

# Or add Java to PATH:
set PATH=%PATH%;C:\Program Files\Java\jre1.8.0_XXX\bin
```

#### 2. "FFDec not found" Error

**Problem**: FFDec path is incorrect

**Solution**:
- Verify FFDec installation location
- Update script with correct path:
  ```batch
  set FFDEC_PATH=C:\Correct\Path\To\ffdec.jar
  ```
- Or pass as parameter:
  ```batch
  extract_dofus_assets_complete.bat "C:\Path\To\ffdec.jar"
  ```

#### 3. "Dofus not found" Error

**Problem**: Dofus is not installed or in non-standard location

**Solution**:
- Verify Dofus installation
- Check alternative locations
- Update script or pass custom path:
  ```batch
  extract_dofus_assets_complete.bat "" "C:\Custom\Dofus\Path"
  ```

#### 4. Extraction Fails or Hangs

**Problem**: FFDec timeout or corrupted SWF

**Solutions**:
- Increase timeout values:
  ```batch
  -timeout 300 -exportTimeout 600
  ```
- Skip problematic files:
  ```batch
  -onerror ignore
  ```
- Try extracting individual files manually
- Update to latest FFDec version

#### 5. Incomplete Extractions

**Problem**: Some assets missing from output

**Solutions**:
- Check extraction logs for errors
- Verify SWF files exist in Dofus directory
- Re-run extraction for failed files
- Use retry logic:
  ```batch
  -onerror retry 5
  ```

#### 6. Out of Memory Errors

**Problem**: Java heap space exceeded

**Solution**:
```batch
# Increase Java heap size
java -Xmx4G -jar ffdec.jar -export ...
```

#### 7. Permission Denied Errors

**Problem**: Insufficient permissions to read/write files

**Solutions**:
- Run script as Administrator
- Check file/folder permissions
- Disable antivirus temporarily (if blocking)

### Getting Help

- **FFDec Documentation**: https://github.com/jindrapetrik/jpexs-decompiler/wiki
- **FFDec Issues**: https://github.com/jindrapetrik/jpexs-decompiler/issues
- **GOFUS Project**: Check project README and documentation

---

## Next Steps

### After Extraction

1. **Verify Extraction**:
   - Check output directory for all expected assets
   - Review extraction logs for errors
   - Verify file counts match expectations

2. **Organize Assets**:
   - Assets are already organized by category
   - Review directory structure
   - Identify any missing critical assets

3. **Import to Unity**:
   - Open Unity project
   - Use GOFUS Asset Migration tools
   - Menu: `GOFUS > Asset Migration > Extraction Validator`
   - Click "Run Validation"
   - Click "Process Assets" to import

4. **Asset Processing**:
   - Unity will convert assets to optimal formats
   - Create sprite sheets and atlases
   - Generate materials and prefabs
   - Set up animations

5. **Testing**:
   - Verify assets load correctly
   - Test character animations
   - Verify UI elements
   - Check map rendering
   - Test audio playback

### Continuous Updates

As Dofus releases updates:
1. Re-run extraction scripts
2. Compare with existing assets
3. Import new/changed assets
4. Update Unity project

---

## Script Execution Order

### For Complete Extraction

1. **First Time Setup**:
   ```batch
   # 1. Extract priority assets for quick testing
   extract_priority_assets.bat

   # 2. Verify tools and pipeline work
   # (Import to Unity and test)

   # 3. Run complete extraction
   extract_dofus_assets_complete.bat
   ```

2. **Advanced Users**:
   ```powershell
   # Use PowerShell script for better control
   .\extract_dofus_assets_advanced.ps1 -MaxThreads 8
   ```

3. **Incremental Updates**:
   ```batch
   # Extract only new/changed assets
   # (Custom script based on file dates)
   ```

---

## Performance Optimization

### Tips for Faster Extraction

1. **Parallel Processing**:
   - Use PowerShell script with `MaxThreads` parameter
   - Default: 4 threads
   - Recommended: 8 threads on modern CPUs

2. **Storage**:
   - Extract to SSD for faster I/O
   - Ensure 20GB+ free space

3. **Java Heap Size**:
   ```batch
   java -Xmx4G -jar ffdec.jar ...
   ```

4. **Skip Unnecessary Assets**:
   - Use priority extraction for MVP
   - Extract additional assets as needed

5. **Batch by Category**:
   - Extract characters first (critical)
   - Then UI elements
   - Maps and effects can wait

---

## Asset Statistics

### Expected Output Volumes

| Category        | File Count | Size (Est.) | Priority |
|----------------|-----------|-------------|----------|
| Characters     | ~9,000    | 3-4 GB      | 1        |
| UI Elements    | ~2,000    | 500 MB      | 1        |
| Maps           | ~5,000    | 2-3 GB      | 2        |
| Effects        | ~3,000    | 1-2 GB      | 2        |
| Items          | ~4,000    | 800 MB      | 3        |
| Monsters       | ~8,000    | 2-3 GB      | 3        |
| NPCs           | ~1,000    | 500 MB      | 4        |
| Audio          | ~500      | 500 MB      | 2        |
| **Total**      | **~32,500**| **10-15 GB**| -        |

### Extraction Time Estimates

| Method                  | Duration   | Output       |
|------------------------|-----------|--------------|
| Priority Assets        | 15-30 min | 500 MB-1 GB  |
| Complete (Sequential)  | 2-4 hours | 10-15 GB     |
| Advanced (Parallel)    | 1-3 hours | 10-15 GB     |

---

## Legal and Ethical Considerations

### Important Notes

1. **Personal Use Only**: These extraction tools are for personal, educational, or development purposes
2. **No Distribution**: Do not distribute extracted Dofus assets publicly
3. **Copyright**: All Dofus assets are copyrighted by Ankama Games
4. **GOFUS Project**: For learning and recreation only
5. **Respect ToS**: Ensure compliance with Dofus Terms of Service

### Disclaimer

This guide is provided for educational purposes. The GOFUS project is a fan-made, non-commercial recreation project. All rights to Dofus assets belong to Ankama Games.

---

## Version History

- **v2.0** (2025-10-25): Complete rewrite with advanced scripts and comprehensive documentation
- **v1.0** (Previous): Initial extraction scripts

---

## Credits

- **JPEXS FFDec**: https://github.com/jindrapetrik/jpexs-decompiler
- **Dofus**: © Ankama Games
- **GOFUS Project**: Community-driven recreation project

---

**End of Guide**

For questions or issues, refer to project documentation or FFDec wiki.
