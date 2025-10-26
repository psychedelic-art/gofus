# Asset Extraction Scripts

This folder contains all the scripts needed to extract and process Dofus assets for use in the Unity client.

## 📁 Script Files

### Main Extraction Scripts

#### `extract_dofus_assets.bat`
The primary extraction script that processes all Dofus assets systematically.

**Usage:**
```batch
extract_dofus_assets.bat [ffdec_path] [dofus_path]
```

**Example:**
```batch
extract_dofus_assets.bat "C:\Tools\ffdec.exe" "C:\Program Files (x86)\Dofus"
```

**Features:**
- Extracts all 18 character classes
- Processes UI elements (buttons, windows, icons)
- Extracts map tiles and objects
- Processes spell effects
- Extracts monster sprites
- Exports audio files

---

#### `extract_priority_assets.bat`
Extracts only the most essential assets for quick testing.

**What it extracts:**
- 5 main character classes (Feca, Osamodas, Enutrof, Sram, Xelor)
- Essential UI elements
- Basic map tiles
- Core sound effects

---

#### `extract_dofus_assets_complete.bat`
Extended version with additional asset types and error handling.

**Additional features:**
- More detailed progress reporting
- Extended error recovery
- Validation after extraction
- Automatic retry on failures

---

### PowerShell Scripts

#### `extract_dofus_assets_advanced.ps1`
Advanced extraction with better error handling and logging.

**Features:**
- Colored console output
- Detailed logging to file
- Parallel processing support
- Progress bars

**Usage:**
```powershell
.\extract_dofus_assets_advanced.ps1 -FFDecPath "path\to\ffdec" -DofusPath "path\to\dofus"
```

---

### Test Scripts

#### `generate_test_assets.bat`
Creates placeholder assets for testing the pipeline without real Dofus files.

**Usage:**
```batch
generate_test_assets.bat
```

**Creates:**
- Test character sprites (32 files)
- UI placeholders
- Sample map tiles
- Test effects

---

## 🚀 Quick Start

### Step 1: Prepare Tools
1. Download JPEXS FFDec from: https://github.com/jindrapetrik/jpexs-decompiler/releases
2. Extract FFDec to a known location (e.g., `C:\Tools\ffdec\`)
3. Locate your Dofus installation (typically `C:\Program Files (x86)\Dofus`)

### Step 2: Run Extraction
```batch
cd Assets\_Project\Scripts\Extraction
extract_dofus_assets.bat "C:\Tools\ffdec\ffdec.exe" "C:\Program Files (x86)\Dofus"
```

### Step 3: Process in Unity
1. Return to Unity Editor
2. Menu: GOFUS > Asset Migration > Extraction Validator
3. Click "Run Validation"
4. Click "Process Assets"

---

## 📊 Expected Output

After successful extraction, you should have:

```
ExtractedAssets/Raw/
├── Characters/           # ~2,304 sprites (18 classes)
│   ├── Feca/
│   ├── Osamodas/
│   └── ...
├── UI/                   # ~200 UI elements
│   ├── Buttons/
│   ├── Windows/
│   └── Icons/
├── Maps/                 # ~500 tiles
│   ├── Tiles/
│   └── Objects/
├── Effects/              # ~100 spell effects
├── Monsters/             # ~200 monster sprites
└── Audio/                # ~50 audio files
    ├── Music/
    └── SFX/
```

---

## 🔧 Troubleshooting

### Common Issues

#### "FFDec not found"
- Verify FFDec path is correct
- Try using full path with quotes
- Ensure ffdec.exe exists at specified location

#### "Dofus path not found"
- Check Dofus is installed
- Try different common locations:
  - `C:\Program Files (x86)\Dofus`
  - `C:\Games\Dofus`
  - `C:\Ankama\Dofus`

#### "Access denied" errors
- Run scripts as Administrator
- Check file permissions
- Close Dofus client if running

#### Extraction seems stuck
- Some SWF files are large and take time
- Check Task Manager for ffdec.exe activity
- Be patient, especially for character sprites

---

## 📝 Script Parameters

### Common Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `ffdec_path` | Path to FFDec executable | `ffdec.exe` |
| `dofus_path` | Path to Dofus installation | `C:\Program Files (x86)\Dofus` |
| `output_path` | Where to save extracted assets | `..\..\ExtractedAssets\Raw` |

### Environment Variables

You can set these to avoid passing parameters:
```batch
set FFDEC_PATH=C:\Tools\ffdec\ffdec.exe
set DOFUS_PATH=C:\Program Files (x86)\Dofus
```

---

## 🎯 Best Practices

1. **Start with priority assets** - Use `extract_priority_assets.bat` first
2. **Test the pipeline** - Use `generate_test_assets.bat` to verify setup
3. **Extract in batches** - Don't try to extract everything at once
4. **Monitor progress** - Watch the console output for errors
5. **Validate after extraction** - Always run the validation tool in Unity

---

## 📚 Additional Resources

- **JPEXS Documentation**: https://github.com/jindrapetrik/jpexs-decompiler/wiki
- **Unity Asset Pipeline**: See main documentation
- **Phase 7 Documentation**: [Asset Migration Guide](../../../../docs/unity-client/phases/GOFUS_PHASE_7_IMPLEMENTATION_SUMMARY.md)

---

*Scripts Version: 1.0*
*Compatible with: JPEXS FFDec 15.0+*
*Last Updated: October 25, 2025*