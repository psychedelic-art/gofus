# Unity Setup Instructions for GOFUS Client

## 🎮 Complete Unity Setup Guide

### Prerequisites
- **Unity Hub**: Latest version
- **Unity Version**: 2022.3 LTS (Long Term Support)
- **Disk Space**: At least 20GB free
- **RAM**: 8GB minimum, 16GB recommended
- **OS**: Windows 10/11, macOS 10.14+, or Ubuntu 20.04+

---

## Step 1: Install Unity Hub and Editor

### 1.1 Download Unity Hub
- **Official Download**: https://unity.com/download
- Install Unity Hub on your system

### 1.2 Install Unity 2022.3 LTS
1. Open Unity Hub
2. Go to **Installs** tab
3. Click **Install Editor**
4. Select **Unity 2022.3 LTS** (Latest 2022.3.x version)
5. Add modules:
   - ✅ **Visual Studio** (or your preferred IDE)
   - ✅ **Windows Build Support** (IL2CPP)
   - ✅ **WebGL Build Support** (optional for web version)
   - ✅ **Documentation**

---

## Step 2: Open GOFUS Project

### 2.1 Add Existing Project
1. In Unity Hub, click **Open**
2. Navigate to: `C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client`
3. Select the folder and click **Open**
4. Unity will import the project (first time may take 10-15 minutes)

### 2.2 Fix Any Import Issues
If Unity shows errors on first import:
1. Go to **Edit > Project Settings > Player**
2. Set **Company Name**: GOFUS
3. Set **Product Name**: GOFUS Client
4. **Configuration**:
   - Api Compatibility Level: **.NET Standard 2.1**
   - Active Input Handling: **Both**

---

## Step 3: Install Required Packages

### 3.1 Open Package Manager
- **Window > Package Manager**
- Set filter to **Unity Registry**

### 3.2 Essential Packages for MMORPG

Install these packages in order:

#### Core 2D Packages
1. **2D Sprite** (com.unity.2d.sprite)
   - Latest version
   - For sprite rendering

2. **2D Tilemap** (com.unity.2d.tilemap)
   - Latest version
   - For isometric map rendering

3. **2D Animation** (com.unity.2d.animation)
   - Latest version
   - For skeletal animations

4. **2D Pixel Perfect** (com.unity.2d.pixel-perfect)
   - Latest version
   - For crisp pixel art

#### Networking
5. **Netcode for GameObjects** (com.unity.netcode.gameobjects)
   - Version 1.7.0+
   - For multiplayer networking

6. **Unity Transport** (com.unity.transport)
   - Latest version
   - Network transport layer

#### UI & Input
7. **Input System** (com.unity.inputsystem)
   - Latest version
   - Modern input handling

8. **TextMeshPro** (com.unity.textmeshpro)
   - Latest version
   - Advanced text rendering

9. **UI Toolkit** (com.unity.ui)
   - Latest version (optional)
   - Modern UI system

#### Performance & Tools
10. **Burst** (com.unity.burst)
    - Latest version
    - Performance optimization

11. **Collections** (com.unity.collections)
    - Latest version
    - Data structures

12. **Addressables** (com.unity.addressables)
    - Latest version
    - Asset management

13. **Cinemachine** (com.unity.cinemachine)
    - Latest version
    - Camera system

---

## Step 4: Project Settings Configuration

### 4.1 Graphics Settings
**Edit > Project Settings > Graphics**
- **Scriptable Render Pipeline**: None (Built-in)
- **Tier Settings**: Low/Medium/High configured
- **Always Included Shaders**: Add used shaders

### 4.2 Quality Settings
**Edit > Project Settings > Quality**
- Create quality levels:
  - **Low**: Mobile/Low-end PC
  - **Medium**: Standard PC
  - **High**: High-end PC
  - **Ultra**: Maximum quality

Settings per level:
```
Low:
- Texture Quality: Half Res
- Anisotropic Textures: Disabled
- Anti Aliasing: Disabled
- Soft Particles: False
- Shadows: Hard Shadows Only

Medium:
- Texture Quality: Full Res
- Anisotropic Textures: Per Texture
- Anti Aliasing: 2x
- Soft Particles: True
- Shadows: Hard and Soft

High/Ultra:
- Texture Quality: Full Res
- Anisotropic Textures: Forced On
- Anti Aliasing: 4x/8x
- Soft Particles: True
- Shadows: Soft Shadows
```

### 4.3 2D & Sprite Settings
**Edit > Project Settings > 2D**
- **Pixel Perfect Camera**: Enable for pixel art
- **Default Sprite Settings**:
  - Pixels Per Unit: 100
  - Filter Mode: Point (for pixel art)
  - Compression: None or High Quality

### 4.4 Physics 2D Settings
**Edit > Project Settings > Physics 2D**
```
- Gravity: (0, 0) for isometric
- Default Material: None
- Velocity Iterations: 8
- Position Iterations: 3
- Time to Sleep: 0.5
- Linear Sleep Tolerance: 0.01
- Angular Sleep Tolerance: 2
```

### 4.5 Tags and Layers
**Edit > Project Settings > Tags and Layers**

**Layers**:
- 0: Default
- 5: UI
- 6: Ground
- 7: Player
- 8: Enemy
- 9: NPC
- 10: Obstacle
- 11: Interactive
- 12: Effects
- 13: Projectile

**Tags**:
- Player
- Enemy
- NPC
- Ground
- Wall
- Interactive
- Pickup
- Teleport

**Sorting Layers** (in order):
1. Background
2. Ground
3. GroundDecoration
4. Objects
5. Characters
6. Effects
7. UI
8. UIOverlay

---

## Step 5: Import and Process Assets

### 5.1 Run Asset Extraction
1. Open command prompt
2. Navigate to `gofus-client`
3. Run: `extract_dofus_assets.bat "path\to\ffdec.exe" "path\to\Dofus"`

### 5.2 Process in Unity
1. In Unity, go to menu: **GOFUS > Asset Migration > Extraction Validator**
2. Click **Run Validation**
3. Review the validation report
4. Click **Process Assets**
5. Wait for import to complete

### 5.3 Generate Animation Controllers
1. **GOFUS > Asset Migration > Character Animation Generator**
2. For each character class:
   - Enter character name
   - Click "Auto-Find Clips"
   - Click "Generate Controller"

### 5.4 Create Sprite Atlases
1. **GOFUS > Asset Migration > Create Atlases**
2. Select asset categories
3. Configure atlas settings:
   - Max Texture Size: 2048
   - Format: RGBA 32 bit
   - Compression: High Quality

---

## Step 6: Scene Setup

### 6.1 Create Test Scene
1. **File > New Scene**
2. Save as `Assets/_Project/Scenes/TestScene.unity`

### 6.2 Setup Camera
1. Select Main Camera
2. Add **Pixel Perfect Camera** component
3. Settings:
   - Asset Pixels Per Unit: 100
   - Reference Resolution: 1920x1080
   - Crop Frame: X and Y
   - Pixel Snapping: ✓

### 6.3 Create Game Systems
Create these GameObjects with their components:

```
GameManager
├── NetworkManager (Netcode for GameObjects)
├── MapManager (Tilemap system)
├── PlayerManager
├── CombatManager
├── UIManager
└── AudioManager
```

---

## Step 7: Build Settings

### 7.1 Configure Build
**File > Build Settings**
1. Add all game scenes
2. Platform: **PC, Mac & Linux Standalone**
3. Architecture: **x86_64**
4. Compression: **LZ4HC**

### 7.2 Player Settings
**Edit > Project Settings > Player**
- **Resolution**:
  - Default: 1920x1080
  - Fullscreen Mode: Windowed
  - Allow Fullscreen Switch: ✓
- **Splash Screen**: Configure or disable
- **Icon**: Set game icon

---

## Step 8: Testing

### 8.1 Play Mode Testing
1. Open TestScene
2. Press **Play** button
3. Test basic functionality:
   - Character movement
   - UI interaction
   - Network connection

### 8.2 Profiler Testing
**Window > Analysis > Profiler**
- Monitor:
  - CPU Usage
  - GPU Usage
  - Memory
  - Rendering (draw calls)
  - Network traffic

### 8.3 Performance Targets
- **FPS**: 60+ stable
- **Draw Calls**: <100 for mobile, <500 for PC
- **Memory**: <1GB for low-end, <2GB typical
- **Build Size**: <500MB initial, <2GB with all assets

---

## Step 9: Optimization Tips

### Texture Optimization
- Use texture atlases for UI and sprites
- Compress textures appropriately
- Use lower resolution for distant objects

### Scripting Optimization
- Use object pooling for frequently spawned objects
- Cache component references
- Avoid FindObjectOfType in Update loops
- Use events instead of constant checking

### Rendering Optimization
- Batch draw calls with atlases
- Use LOD system for complex objects
- Cull off-screen objects
- Optimize shader usage

---

## Step 10: Useful Unity Resources

### Official Documentation
- **Unity Manual 2022.3**: https://docs.unity3d.com/2022.3/Documentation/Manual/
- **Unity Learn**: https://learn.unity.com/
- **2D Game Development**: https://learn.unity.com/course/2d-game-kit

### MMORPG Specific
- **Netcode Documentation**: https://docs-multiplayer.unity3d.com/
- **Best Practices**: https://docs.unity3d.com/Manual/BestPractice2D.html

### Community Resources
- **Unity Forum**: https://forum.unity.com/
- **Unity Subreddit**: https://www.reddit.com/r/Unity3D/
- **Brackeys Archives**: YouTube tutorials

### Asset Store Recommendations (Optional)
- **A* Pathfinding Project** - Advanced pathfinding
- **DOTween** - Animation system
- **Odin Inspector** - Better inspector
- **ProGrids** - Grid snapping tools

---

## Troubleshooting

### Common Issues and Solutions

1. **Pink/Missing Textures**
   - Reimport the texture
   - Check shader compatibility
   - Verify texture import settings

2. **Animation Not Playing**
   - Check Animator Controller setup
   - Verify animation clip import settings
   - Check parameter triggers

3. **Performance Issues**
   - Use Profiler to identify bottlenecks
   - Reduce texture sizes
   - Implement object pooling
   - Optimize scripts

4. **Build Errors**
   - Clear Library folder and reimport
   - Check for missing assemblies
   - Verify all scenes are in Build Settings

---

## Next Phase: Polish & Optimization

After setup is complete, proceed to Phase 8:
1. Implement object pooling system
2. Add particle effects
3. Setup post-processing
4. Implement audio mixing
5. Add visual polish and game feel

---

*Unity Setup Guide for GOFUS - October 25, 2025*