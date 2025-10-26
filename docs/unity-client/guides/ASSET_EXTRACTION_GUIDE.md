# Dofus Asset Extraction Guide

## Prerequisites

### 1. Download JPEXS Free Flash Decompiler
- **Download Link**: https://github.com/jindrapetrik/jpexs-decompiler/releases
- **Version Recommended**: Latest stable (v20.0.0 or higher)
- **Installation**: Extract and run `ffdec.exe`

### 2. Locate Dofus Game Files
Common locations:
- **Windows**: `C:\Program Files (x86)\Dofus` or `C:\Users\[Username]\AppData\Local\Ankama\Dofus`
- **Steam**: `C:\Program Files (x86)\Steam\steamapps\common\Dofus`

Key directories:
- `/content/gfx/` - Graphics and sprites
- `/content/audio/` - Sound effects and music
- `/content/maps/` - Map data files

## Step-by-Step Extraction Process

### Step 1: Extract Character Sprites

1. **Open JPEXS FFDec**
2. **File > Open** > Navigate to Dofus installation
3. **Look for these SWF files**:
   ```
   /content/gfx/sprites/actors/characters/
   - 10.swf (Osamodas)
   - 11.swf (Feca)
   - 12.swf (Enutrof)
   - 13.swf (Sram)
   - 14.swf (Xelor)
   - 15.swf (Ecaflip)
   - 16.swf (Eniripsa)
   - 17.swf (Iop)
   - 18.swf (Cra)
   - 19.swf (Sadida)
   - 110.swf (Sacrier)
   - 111.swf (Pandawa)
   ```

4. **For each SWF file**:
   - Open in JPEXS
   - Navigate to `sprites` folder in the tree
   - Right-click > Export selection
   - Choose format: PNG (with alpha)
   - Export to: `gofus-client/ExtractedAssets/Raw/Characters/[ClassName]/`

5. **Expected output per character**:
   - 8 directions (N, NE, E, SE, S, SW, W, NW)
   - 8 animations (idle, walk, run, attack, cast, hit, death, sit)
   - Total: 64 sprites per character

### Step 2: Extract UI Elements

1. **Navigate to UI SWFs**:
   ```
   /content/gfx/ui/
   - buttons.swf
   - windows.swf
   - icons.swf
   - cursors.swf
   ```

2. **Export Process**:
   - Open each SWF in JPEXS
   - Export all images as PNG
   - Export to: `gofus-client/ExtractedAssets/Raw/UI/[Category]/`

### Step 3: Extract Map Tiles

1. **Navigate to map graphics**:
   ```
   /content/gfx/maps/
   - tiles/
   - objects/
   - backgrounds/
   ```

2. **Export tiles**:
   - Look for numbered tile files (0.swf, 1.swf, etc.)
   - Export as PNG sequences
   - Export to: `gofus-client/ExtractedAssets/Raw/Maps/Tiles/`

### Step 4: Extract Audio

1. **Navigate to audio files**:
   ```
   /content/audio/
   - music/
   - sfx/
   - ambient/
   ```

2. **Export audio**:
   - In JPEXS, navigate to `sounds` folder
   - Export as MP3 or WAV
   - Export to: `gofus-client/ExtractedAssets/Raw/Audio/[Category]/`

## Sample Asset Structure

After extraction, your folder should look like:

```
ExtractedAssets/
└── Raw/
    ├── Characters/
    │   ├── Feca/
    │   │   ├── idle_north_0.png
    │   │   ├── idle_north_1.png
    │   │   ├── walk_northeast_0.png
    │   │   └── ... (64 total sprites)
    │   └── Osamodas/
    │       └── ... (64 total sprites)
    ├── Maps/
    │   ├── Tiles/
    │   │   ├── grass_01.png
    │   │   ├── stone_01.png
    │   │   └── ...
    │   └── Objects/
    │       ├── tree_01.png
    │       └── ...
    ├── UI/
    │   ├── Buttons/
    │   │   ├── btn_normal.png
    │   │   ├── btn_hover.png
    │   │   └── btn_pressed.png
    │   ├── Windows/
    │   │   ├── window_frame.png
    │   │   └── window_close.png
    │   └── Icons/
    │       ├── icon_sword.png
    │       └── ...
    ├── Effects/
    │   ├── spell_fire_01.png
    │   └── ...
    └── Audio/
        ├── Music/
        │   ├── incarnam.mp3
        │   └── ...
        ├── SFX/
        │   ├── sword_hit.mp3
        │   └── ...
        └── Ambient/
            ├── birds.mp3
            └── ...
```

## Automated Processing with Unity Tools

Once you have extracted assets into the Raw folder:

1. **Open Unity Project** (gofus-client)

2. **Run Dofus Asset Processor**:
   - Menu: GOFUS > Asset Migration > Dofus Asset Processor
   - Source Path: `[Project]/ExtractedAssets/Raw`
   - Output Path: `Assets/_Project/ImportedAssets`
   - Options:
     - ✅ Create Atlases
     - ✅ Generate Animations
     - ✅ Optimize Textures
   - Click "Process Assets"

3. **For Character Sprites**:
   - Menu: GOFUS > Asset Migration > Sprite Sheet Slicer
   - Select character sprite sheet
   - Grid: 8x8 (auto-detect)
   - Click "Slice and Generate Animations"

4. **Generate Animation Controllers**:
   - Menu: GOFUS > Asset Migration > Character Animation Generator
   - Character Name: [Enter class name]
   - Click "Generate Controller"

5. **Validate Results**:
   - Menu: GOFUS > Asset Migration > Validation Report
   - Click "Generate Report"
   - Review missing assets
   - Check progress percentages

## Quick Test Assets

For initial testing, extract these minimum assets:

1. **One Character Class** (e.g., Feca - 11.swf)
   - Just the idle and walk animations
   - 16 sprites minimum

2. **Basic UI Elements**
   - One button set (normal, hover, pressed)
   - One window frame
   - 5-10 basic icons

3. **Few Map Tiles**
   - 5 grass tiles
   - 5 stone tiles
   - 2-3 trees/objects

4. **Sample Audio**
   - 1 background music
   - 3-5 sound effects

## Troubleshooting

### JPEXS Issues
- **Can't open SWF**: Try running as administrator
- **Export fails**: Use "Export all parts" instead of selection
- **Missing transparency**: Ensure PNG format with alpha channel

### Common Problems
- **Sprite sheets not aligned**: Manually adjust in Unity Sprite Editor
- **Animation frames out of order**: Check naming convention
- **Assets too large**: Enable compression in processor

### Legal Note
Only extract assets from games you legally own. These tools are for educational and development purposes.

## Validation Checklist

After extraction and processing:

- [ ] Characters have 8 directional sprites
- [ ] Animations play smoothly at 12 FPS
- [ ] UI elements maintain transparency
- [ ] Map tiles align properly on grid
- [ ] Audio files play without distortion
- [ ] File sizes are optimized (textures < 2MB each)
- [ ] Folder structure matches expected hierarchy
- [ ] Animation controllers are generated
- [ ] Sprite atlases are created
- [ ] Validation report shows > 0% progress

## Next Steps

1. Start with minimal extraction (1 character, basic UI)
2. Test the pipeline end-to-end
3. Fix any issues
4. Extract remaining assets in batches
5. Run validation after each batch
6. Create prefabs for commonly used assets

---

*Note: This guide assumes you have legal access to Dofus game files. Always respect intellectual property rights.*