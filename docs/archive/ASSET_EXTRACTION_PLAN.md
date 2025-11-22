# GOFUS Asset Extraction Plan

## Executive Summary

This document provides a comprehensive plan for extracting Dofus game assets and fixing the character selection screen in the GOFUS Unity client. The character selection screen is currently stuck at "Loading characters..." due to missing character sprites and assets.

## Current Status

### Issues Identified
1. **Character Selection Screen Stuck**: The screen displays "Loading characters..." indefinitely
2. **Missing Prerequisites**:
   - JPEXS FFDec not installed at `C:\Program Files\FFDec\`
   - Dofus client not installed at `C:\Program Files (x86)\Dofus`
3. **Missing Assets**: No character sprites in Resources folder
4. **ClassSpriteManager**: Unable to load sprites from Resources

### Root Cause
The ClassSpriteManager attempts to load sprites from `Resources/Sprites/Classes/` but these assets don't exist because they haven't been extracted from Dofus.

## Solution Approach

We will implement a two-phase solution:

### Phase 1: Immediate Fix (Placeholder Assets)
1. Create placeholder character sprites
2. Generate mock character data
3. Fix loading logic to handle missing assets gracefully
4. Enable character selection with placeholders

### Phase 2: Full Asset Extraction
1. Install prerequisites (JPEXS FFDec, Dofus)
2. Extract priority assets using scripts
3. Process assets for Unity
4. Organize in proper folder structure

## Prerequisites Installation

### 1. JPEXS Free Flash Decompiler (FFDec)

**Download**: https://github.com/jindrapetrik/jpexs-decompiler/releases
**Version Required**: v19.0.0 or higher
**Installation Path**: `C:\Program Files\FFDec\`

#### Installation Steps:
1. Download latest release from GitHub
2. Extract to `C:\Program Files\FFDec\`
3. Verify `ffdec.jar` exists in installation folder
4. Install Java Runtime if not present

### 2. Dofus Client

**Download**: https://www.dofus.com/en/download
**Installation Path**: `C:\Program Files (x86)\Dofus`

#### Alternative Locations:
- Steam: `C:\Program Files (x86)\Steam\steamapps\common\Dofus`
- User Profile: `%LOCALAPPDATA%\Dofus`
- Custom: `C:\Dofus` or `C:\Games\Dofus`

### 3. Java Runtime Environment

**Download**: https://www.java.com/download/
**Version**: 8 or higher
**Verification**: Run `java -version` in command prompt

## Asset Extraction Process

### Priority Assets (MVP)

The following assets are essential for basic functionality:

#### Characters (4 Classes)
1. **Feca** (ID: 01) - Tank/Support
2. **Sram** (ID: 04) - Stealth/DPS
3. **Eniripsa** (ID: 07) - Healer
4. **Iop** (ID: 08) - Melee DPS

#### UI Elements
- Buttons (normal, hover, pressed, disabled)
- Windows (frames, panels)
- Icons (health, mana, inventory)
- Cursor

#### Maps
- 10 basic tile sets
- Essential ground textures

#### Effects
- Damage indicators
- Heal effects
- Movement indicators

#### Audio
- 5 core UI sounds

### Extraction Scripts

Located in: `gofus-client\Assets\_Project\Scripts\Extraction\`

1. **extract_priority_assets.bat** - Quick MVP extraction (15-30 min)
2. **extract_dofus_assets_complete.bat** - Full extraction (2-4 hours)
3. **extract_dofus_assets_advanced.ps1** - Parallel processing

### Expected Output

| Category | File Count | Size | Priority |
|----------|-----------|------|----------|
| Characters (Priority) | ~1,600 | 500MB | 1 |
| UI Elements | ~2,000 | 500MB | 1 |
| Maps (Basic) | ~500 | 200MB | 2 |
| Effects (Core) | ~200 | 100MB | 2 |
| Audio (UI) | ~50 | 50MB | 3 |

## Unity Asset Processing

### Folder Structure

```
Assets/_Project/Resources/
├── Sprites/
│   ├── Classes/
│   │   ├── Feca/
│   │   │   ├── idle.png
│   │   │   ├── walk.png
│   │   │   └── ...
│   │   ├── Sram/
│   │   ├── Eniripsa/
│   │   └── Iop/
│   ├── UI/
│   │   ├── Buttons/
│   │   ├── Windows/
│   │   └── Icons/
│   └── Effects/
├── Audio/
│   └── UI/
└── Prefabs/
    └── Characters/
```

### Asset Processing Steps

1. **Import Raw Assets**: Copy extracted assets to Unity project
2. **Configure Import Settings**:
   - Sprites: Set to Sprite mode, Multiple if sprite sheet
   - Texture Type: Sprite (2D and UI)
   - Pixels Per Unit: 100
   - Filter Mode: Point (for pixel art)
3. **Create Sprite Sheets**: Combine individual sprites
4. **Generate Animations**: Create animation clips from sprite sequences
5. **Build Prefabs**: Create character prefabs with components

## Character Selection Screen Fix

### Immediate Fixes Required

1. **Handle Missing Assets Gracefully**:
   ```csharp
   // In ClassSpriteManager.cs
   if (sprite == null) {
       // Create colored placeholder
       sprite = CreatePlaceholderSprite(classId);
   }
   ```

2. **Mock Character Data**:
   ```csharp
   // Create test characters if backend fails
   if (characters == null || characters.Count == 0) {
       characters = GenerateMockCharacters();
   }
   ```

3. **Loading State Management**:
   ```csharp
   // Add timeout and retry logic
   if (loadingTime > maxLoadTime) {
       ShowErrorState();
       EnableRetryButton();
   }
   ```

### Test Data Generation

Create mock characters for testing:
- Test Character 1: Level 50 Feca
- Test Character 2: Level 30 Sram
- Test Character 3: Level 25 Eniripsa
- Test Character 4: Level 40 Iop

## Implementation Timeline

### Day 1: Immediate Fixes
- [x] Analyze current issues
- [ ] Create placeholder assets
- [ ] Implement mock data generation
- [ ] Fix loading logic
- [ ] Test character selection

### Day 2: Asset Extraction
- [ ] Install prerequisites
- [ ] Run priority extraction
- [ ] Verify extracted assets
- [ ] Process for Unity

### Day 3: Integration
- [ ] Import processed assets
- [ ] Update ClassSpriteManager
- [ ] Test with real assets
- [ ] Polish and optimize

## Testing Strategy

### Unit Tests
1. ClassSpriteManager loading tests
2. Character data parsing tests
3. UI component tests

### Integration Tests
1. Character selection flow
2. Backend connection handling
3. Asset loading pipeline

### Manual Testing
1. Character selection interactions
2. Visual verification of sprites
3. Performance testing

## Success Criteria

1. ✅ Character selection screen loads properly
2. ✅ Characters display with correct sprites
3. ✅ Selection and interaction work correctly
4. ✅ Graceful handling of missing assets
5. ✅ Backend integration functional

## Troubleshooting

### Common Issues

1. **FFDec Not Found**:
   - Verify installation path
   - Check Java installation
   - Run as administrator

2. **Dofus Not Found**:
   - Check alternative installation paths
   - Verify game is installed
   - Check Steam installation

3. **Extraction Fails**:
   - Increase timeout values
   - Check disk space
   - Verify file permissions

4. **Unity Import Issues**:
   - Check texture import settings
   - Verify sprite mode configuration
   - Clear and reimport assets

## Next Steps

1. **Immediate**: Create placeholder assets to unblock development
2. **Short-term**: Extract priority assets once prerequisites installed
3. **Long-term**: Complete full asset extraction and processing

## Resources

- [JPEXS FFDec Documentation](https://github.com/jindrapetrik/jpexs-decompiler/wiki)
- [Unity Sprite Documentation](https://docs.unity3d.com/Manual/Sprites.html)
- [Dofus Asset Structure Guide](./unity-client/guides/DOFUS_ASSET_EXTRACTION_GUIDE.md)

## Contact

For issues or questions regarding asset extraction:
- Check existing documentation in `/docs/unity-client/guides/`
- Review extraction scripts in `/gofus-client/Assets/_Project/Scripts/Extraction/`

---

*Last Updated: November 2024*
*Document Version: 1.0*