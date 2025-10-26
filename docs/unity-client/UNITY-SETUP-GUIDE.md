# GOFUS Unity 2022.3 LTS Setup Guide
## Complete Installation and Configuration for 2D MMORPG Development

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Unity Hub Installation](#unity-hub-installation)
3. [Unity 2022.3 LTS Installation](#unity-2023-lts-installation)
4. [Project Creation and Configuration](#project-creation-and-configuration)
5. [Required Unity Packages](#required-unity-packages)
6. [2D Isometric Setup](#2d-isometric-setup)
7. [Project Settings Optimization](#project-settings-optimization)
8. [MMORPG-Specific Configuration](#mmorpg-specific-configuration)
9. [Dofus-Like Game Checklist](#dofus-like-game-checklist)
10. [Additional Resources](#additional-resources)

---

## Prerequisites

### System Requirements

**Minimum Requirements:**
- **OS:** Windows 10 (64-bit), macOS 10.15+, or Ubuntu 20.04+
- **CPU:** Intel Core i5 or equivalent
- **RAM:** 8 GB (16 GB recommended)
- **GPU:** DirectX 11/12 compatible graphics card
- **Storage:** 10 GB free space for Unity installation
- **Internet:** Required for Unity Hub and package downloads

**Recommended Specifications:**
- **RAM:** 16 GB or more
- **GPU:** Dedicated graphics card with 2+ GB VRAM
- **Storage:** SSD with 20+ GB free space
- **.NET:** .NET 4+ runtime (Unity 2022.3 LTS requirement)

### Required Software

- **Unity Hub:** Latest version (2025+)
- **Unity 2022.3 LTS:** Latest patch release
- **Code Editor:** Visual Studio 2022 (Windows), Visual Studio Code, or JetBrains Rider
- **Git:** For version control (optional but recommended)

---

## Unity Hub Installation

### Step 1: Download Unity Hub

1. Navigate to the official Unity download page:
   - **URL:** https://unity.com/download
   - **Direct Link:** https://unity.com/unity-hub

2. Click the **Download Unity Hub** button
   - The website automatically detects your operating system
   - Available for Windows, macOS (Intel & Apple Silicon), and Linux

### Step 2: Install Unity Hub

#### Windows Installation

```bash
# Download UnityHubSetup.exe
# Double-click the installer

# Installation steps:
1. Run UnityHubSetup.exe
2. Read and accept the License Agreement
3. Choose installation directory (default: C:\Program Files\Unity Hub)
4. Click "Install"
5. Wait for installation to complete
6. Launch Unity Hub
```

#### macOS Installation

```bash
# Download UnityHubSetup.dmg
# Open the .dmg file

# Installation steps:
1. Open UnityHubSetup.dmg
2. Read and accept the license agreement
3. Drag Unity Hub icon to Applications folder
4. Open Unity Hub from Applications
5. Grant necessary permissions when prompted
```

#### Linux Installation (Debian/Ubuntu)

```bash
# Add Unity Hub repository
wget -qO - https://hub.unity3d.com/linux/keys/public | sudo apt-key add -

# Add repository to sources
sudo sh -c 'echo "deb https://hub.unity3d.com/linux/repos/deb stable main" > /etc/apt/sources.list.d/unityhub.list'

# Update and install
sudo apt update
sudo apt install unityhub

# Launch Unity Hub
unityhub
```

### Step 3: Sign In to Unity Account

1. Launch Unity Hub
2. Click **Sign In** (top-right corner)
3. Options:
   - Sign in with existing Unity ID
   - Create new Unity account
   - Sign in with Google/Facebook/Apple

4. **Choose License:**
   - **Personal (Free):** For individuals and small teams (revenue < $100k/year)
   - **Plus:** $40/month - Additional features and cloud storage
   - **Pro:** $150/month - Advanced features for professionals
   - **Enterprise:** Custom pricing for large organizations

5. **Activate Personal License:**
   - Click **Preferences** > **Licenses**
   - Click **Add** > **Get a free personal license**
   - Accept terms and conditions

---

## Unity 2022.3 LTS Installation

### Why Unity 2022.3 LTS?

- **Long Term Support:** Receives updates for 2+ years
- **Stability:** Thoroughly tested, production-ready
- **2D Features:** Enhanced 2D tools and Tilemap improvements
- **Performance:** Optimized for 2D and 3D games
- **Industry Standard:** Widely adopted in game development

### Step 1: Install Unity Editor

1. In Unity Hub, navigate to **Installs** tab (left sidebar)
2. Click **Install Editor** button (top-right)
3. Select **2022.3 LTS** (Latest patch recommended)
   - Example: 2022.3.52f1 (as of October 2025)
4. Click **Install**

### Step 2: Select Modules

**Essential Modules for GOFUS:**

#### Build Support
- [x] **Windows Build Support (IL2CPP)** - For Windows standalone builds
- [x] **Mac Build Support (Mono)** - For macOS builds
- [x] **Linux Build Support (Mono)** - For Linux builds
- [x] **WebGL Build Support** - For browser-based version (optional)

#### Development Tools
- [x] **Visual Studio** (Windows) or **Visual Studio for Mac**
- [x] **Documentation** - Offline Unity documentation

#### Optional Modules
- [ ] Android Build Support - If planning mobile port
- [ ] iOS Build Support - If planning mobile port

**Module Selection Example:**
```
Selected Modules:
✓ Microsoft Visual Studio Community 2022
✓ Windows Build Support (IL2CPP)
✓ Mac Build Support (Mono)
✓ Linux Build Support (Mono)
✓ WebGL Build Support
✓ Documentation
```

### Step 3: Complete Installation

1. Click **Install** (bottom-right)
2. Accept license agreements
3. Wait for download and installation (15-30 minutes depending on internet speed)
4. Unity Editor 2022.3 LTS will appear in the **Installs** tab when complete

**Download Size:** Approximately 3-5 GB (varies by selected modules)

---

## Project Creation and Configuration

### Step 1: Create New Project

1. In Unity Hub, click **Projects** tab
2. Click **New Project** button (top-right)
3. Select Unity Editor version: **2022.3.X LTS**

### Step 2: Choose Template

**Recommended Template:** **2D (URP)** - Universal Render Pipeline for 2D

**Why 2D URP?**
- Optimized for 2D graphics
- Advanced lighting and shadows for 2D
- Better performance than built-in renderer
- Post-processing effects support
- Modern rendering pipeline

**Alternative Templates:**
- **2D Core:** Basic 2D template (if URP not needed)
- **2D Mobile:** Optimized for mobile (future consideration)

### Step 3: Project Settings

```
Project Name: GOFUS-Unity-Client
Location: C:\Projects\GOFUS\ (or your preferred location)
Organization: [Your Organization Name]
Template: 2D (URP)
```

### Step 4: Create Project

1. Click **Create Project** (bottom-right)
2. Wait for Unity to initialize (2-3 minutes)
3. Unity Editor will open automatically

---

## Required Unity Packages

### Core Unity Packages (via Package Manager)

#### Access Package Manager
1. **Window** > **Package Manager**
2. Change dropdown from **In Project** to **Unity Registry**

### Essential Packages for GOFUS

#### 1. 2D Packages

**2D Sprite (com.unity.2d.sprite)**
- **Version:** 1.0.0+
- **Status:** Usually pre-installed with 2D template
- **Purpose:** Sprite rendering and management
- **Installation:** Automatic with 2D template

**2D Tilemap Editor (com.unity.2d.tilemap)**
- **Version:** 1.0.0+
- **Purpose:** Isometric map creation
- **Installation:**
  1. Package Manager > Unity Registry
  2. Search "2D Tilemap Editor"
  3. Click **Install**

**2D Tilemap Extras (com.unity.2d.tilemap.extras)**
- **Version:** 3.1.0+
- **Purpose:** Advanced tilemap features (Rule Tiles, Animated Tiles)
- **Installation:**
  1. Package Manager > Unity Registry
  2. Search "2D Tilemap Extras"
  3. Click **Install**

**2D Pixel Perfect (com.unity.2d.pixel-perfect)**
- **Version:** 5.0.3+
- **Purpose:** Crisp pixel art rendering
- **Installation:**
  1. Package Manager > Unity Registry
  2. Search "2D Pixel Perfect"
  3. Click **Install**

#### 2. UI Packages

**TextMeshPro (com.unity.textmeshpro)**
- **Version:** 3.0.6+
- **Purpose:** Advanced text rendering
- **Installation:** Usually pre-installed
- **First Use:** Import TMP Essential Resources when prompted

**UI Toolkit (com.unity.ui)**
- **Version:** 1.0.0+
- **Purpose:** Modern UI framework (optional, for advanced UI)
- **Installation:** Package Manager > UI Toolkit

#### 3. Input System

**Input System (com.unity.inputsystem)**
- **Version:** 1.8.2+
- **Purpose:** Modern input handling (keyboard, mouse, gamepad)
- **Installation:**
  1. Package Manager > Unity Registry
  2. Search "Input System"
  3. Click **Install**
  4. When prompted: "Enable new Input System?" > Click **Yes**
  5. Unity will restart

**Configuration:**
```
Edit > Project Settings > Player > Other Settings
Active Input Handling: Input System Package (New)
```

#### 4. Networking Packages

**Unity Netcode for GameObjects**
- **Version:** 1.8.0+
- **Purpose:** Multiplayer networking framework
- **Installation:**
  1. Package Manager > Unity Registry
  2. Search "Netcode for GameObjects"
  3. Click **Install**

**Note:** GOFUS uses WebSocket networking, so you may need third-party packages:
- **NativeWebSocket** (via GitHub or Asset Store)
- **SocketIOClient** for Socket.IO compatibility

#### 5. Animation & Effects

**Cinemachine (com.unity.cinemachine)**
- **Version:** 2.9.0+
- **Purpose:** Advanced camera control
- **Installation:**
  1. Package Manager > Unity Registry
  2. Search "Cinemachine"
  3. Click **Install**

**Animation Rigging (com.unity.animation.rigging)**
- **Version:** 1.3.0+ (optional)
- **Purpose:** Character animation enhancements
- **Installation:** Package Manager > Animation Rigging

#### 6. Utilities

**Newtonsoft Json (com.unity.nuget.newtonsoft-json)**
- **Version:** 3.2.1+
- **Purpose:** JSON serialization/deserialization
- **Installation:**
  1. Package Manager > Unity Registry
  2. Search "Newtonsoft Json"
  3. Click **Install**

**Addressables (com.unity.addressables)**
- **Version:** 1.21.0+
- **Purpose:** Asset management and loading
- **Installation:**
  1. Package Manager > Unity Registry
  2. Search "Addressables"
  3. Click **Install**

### Complete Package List

```json
{
  "dependencies": {
    "com.unity.2d.sprite": "1.0.0",
    "com.unity.2d.tilemap": "1.0.0",
    "com.unity.2d.tilemap.extras": "3.1.2",
    "com.unity.2d.pixel-perfect": "5.0.3",
    "com.unity.textmeshpro": "3.0.8",
    "com.unity.ugui": "1.0.0",
    "com.unity.inputsystem": "1.8.2",
    "com.unity.nuget.newtonsoft-json": "3.2.1",
    "com.unity.cinemachine": "2.9.7",
    "com.unity.addressables": "1.21.21",
    "com.unity.netcode.gameobjects": "1.8.1",
    "com.unity.multiplayer.tools": "1.1.1"
  }
}
```

### Third-Party Assets (Unity Asset Store / GitHub)

#### Essential Third-Party Packages

**1. NativeWebSocket**
- **Source:** GitHub - https://github.com/endel/NativeWebSocket
- **Purpose:** WebSocket client for Unity
- **Installation:**
  ```
  1. Download from GitHub
  2. Extract to Assets/Plugins/NativeWebSocket
  ```

**2. DOTween (FREE)**
- **Source:** Unity Asset Store
- **Purpose:** Powerful animation library
- **Installation:**
  1. Open Asset Store in Unity (Window > Asset Store)
  2. Search "DOTween"
  3. Download and import

**3. A* Pathfinding Project** (Optional - $100)
- **Source:** Unity Asset Store
- **Purpose:** Advanced pathfinding for isometric maps
- **Alternative:** Use Unity's built-in NavMesh or custom A* implementation

---

## 2D Isometric Setup

### Step 1: Configure Project for Isometric Rendering

#### Graphics Settings

1. **Edit** > **Project Settings** > **Graphics**

**Transparency Sort Mode:**
```
Transparency Sort Mode: Custom Axis
Transparency Sort Axis: (0, 1, -0.26)
```

**Why this setting?**
- Ensures sprites at different Z positions render correctly
- Critical for isometric depth sorting
- Value `-0.26` is tangent of 15° (half of 30° isometric angle)

#### Camera Settings

2. **Main Camera** configuration:
```
Projection: Orthographic
Size: 5 (adjust based on your needs)
Background: Solid Color (or Skybox)
Culling Mask: Everything

Component: Pixel Perfect Camera (Add if using pixel art)
- Assets Pixels Per Unit: 100 (match your sprite settings)
- Reference Resolution: 1920x1080
```

### Step 2: Create Isometric Grid

1. **GameObject** > **2D Object** > **Tilemap** > **Isometric** > **Isometric Z as Y**

**Hierarchy Structure:**
```
Grid (Isometric)
├── Ground (Tilemap)
├── Objects (Tilemap)
├── Collision (Tilemap)
└── Overlay (Tilemap)
```

**Grid Component Settings:**
```
Cell Size: (1, 0.5, 1)
Cell Gap: (0, 0, 0)
Cell Layout: Isometric
Cell Swizzle: XYZ
```

### Step 3: Isometric Tilemap Configuration

**Tilemap Renderer Settings:**
```
Mode: Individual (for sprite interaction)
Detect Chunk Culling Bounds: Automatic
Sort Order: Bottom Left (for isometric)
```

**Tilemap Collider 2D (for walkable areas):**
```
Used By Composite: True (for optimization)
```

### Step 4: Sprite Import Settings for Isometric

**Recommended Sprite Settings:**
```
Texture Type: Sprite (2D and UI)
Sprite Mode: Single (or Multiple for sprite sheets)
Pixels Per Unit: 100 (standard for Dofus-like tiles)
Mesh Type: Tight
Extrude Edges: 0
Pivot: Center
Generate Mip Maps: False
Filter Mode: Point (for pixel art) or Bilinear
Compression: None (for quality) or Low Quality
```

**Isometric Tile Dimensions (Dofus-style):**
- **Cell Width:** 86 pixels
- **Cell Height:** 43 pixels
- **Aspect Ratio:** 2:1 (standard isometric)

### Step 5: Sorting Layers Setup

1. **Edit** > **Project Settings** > **Tags and Layers**
2. Add Sorting Layers (order matters):

```
Sorting Layers (top to bottom):
0. Default
1. Background
2. Ground
3. Objects_Bottom
4. Characters
5. Objects_Top
6. Effects
7. UI_World
8. UI
```

### Step 6: Layer Configuration

**Physics Layers:**
```
0. Default
3. Player
6. Enemy
7. NPC
8. Interactable
9. Ground
10. Obstacle
```

**Layer Collision Matrix:**
- Configure in **Edit** > **Project Settings** > **Physics 2D**
- Disable unnecessary collisions for performance

---

## Project Settings Optimization

### Graphics Settings

**Edit > Project Settings > Graphics**

```
Scriptable Render Pipeline Settings: URP-HighQuality-Renderer
Transparency Sort Mode: Custom Axis (0, 1, -0.26)
Camera-Relative Culling: Enabled

Shader Stripping:
- Instancing Variants: Keep All
- Lightmap Modes: Strip Unused
```

### Quality Settings

**Edit > Project Settings > Quality**

**For 2D MMORPG:**
```
Levels: 3 (Low, Medium, High)

Low Quality:
- Pixel Light Count: 1
- Texture Quality: Medium
- Anisotropic Textures: Disabled
- Anti-Aliasing: Disabled
- Soft Particles: Disabled
- V Sync Count: Don't Sync
- Target Frame Rate: 30

Medium Quality:
- Pixel Light Count: 2
- Texture Quality: Full Res
- Anisotropic Textures: Per Texture
- Anti-Aliasing: 2x Multi Sampling
- Soft Particles: Enabled
- V Sync Count: Every V Blank
- Target Frame Rate: 60

High Quality:
- Pixel Light Count: 4
- Texture Quality: Full Res
- Anisotropic Textures: Forced On
- Anti-Aliasing: 4x Multi Sampling
- Soft Particles: Enabled
- V Sync Count: Every V Blank
- Target Frame Rate: 60
```

### Physics 2D Settings

**Edit > Project Settings > Physics 2D**

```
Gravity: (0, 0) - No gravity for isometric top-down
Default Material: None
Velocity Iterations: 8
Position Iterations: 3
Velocity Threshold: 1
Max Linear Correction: 0.2
Max Angular Correction: 8
Max Translation Speed: 100
Baumgarte Scale: 0.2
Baumgarte Time of Impact Scale: 0.75
Time to Sleep: 0.5
Linear Sleep Tolerance: 0.01
Angular Sleep Tolerance: 2

Auto Simulation: True
Queries Hit Triggers: True
Queries Start In Colliders: False
Callbacks On Disable: True
Reuse Collision Callbacks: True
Auto Sync Transforms: False (enable only if needed)
```

### Player Settings

**Edit > Project Settings > Player**

**General Settings:**
```
Company Name: [Your Company]
Product Name: GOFUS
Version: 0.1.0
Default Icon: [Your Icon]
Default Cursor: [Custom Cursor]
Cursor Hotspot: (0, 0)
```

**Resolution and Presentation:**
```
Fullscreen Mode: Fullscreen Window
Default Screen Width: 1920
Default Screen Height: 1080
Run In Background: True (important for MMORPG)
Display Resolution Dialog: Enabled
Resizable Window: True
Visible in Background: False
Force Single Instance: True
```

**Other Settings:**
```
Scripting Backend: IL2CPP (for performance)
API Compatibility Level: .NET Standard 2.1
Active Input Handling: Input System Package (New)
Stack Trace: ScriptOnly (for performance)
```

### Time Settings

**Edit > Project Settings > Time**

```
Fixed Timestep: 0.02 (50 FPS for physics)
Maximum Allowed Timestep: 0.1
Time Scale: 1
Maximum Particle Timestep: 0.03
```

### Audio Settings

**Edit > Project Settings > Audio**

```
Global Volume: 1
Rolloff Scale: 1
Doppler Factor: 1
Default Speaker Mode: Stereo
DSP Buffer Size: Best Latency
Virtual Voice Count: 512
Real Voice Count: 32
```

---

## MMORPG-Specific Configuration

### Networking Configuration

#### Scene Setup for Multiplayer

**Scenes Structure:**
```
Assets/Scenes/
├── Bootstrap.unity          # Initial loading scene
├── Login.unity             # Login screen
├── CharacterSelection.unity # Character selection
├── Game.unity              # Main game scene
└── Battle.unity            # Combat scene (optional)
```

#### Network Manager Setup

**Create NetworkManager GameObject:**
```
1. Create Empty GameObject: "NetworkManager"
2. Add Script: NetworkManager.cs
3. Tag: "NetworkManager"
4. DontDestroyOnLoad: True
```

**NetworkManager Settings:**
```csharp
Server URL: ws://localhost:3001 (development)
Auto Reconnect: True
Reconnect Delay: 3.0 seconds
Max Reconnect Attempts: 5
Timeout Duration: 30 seconds
Packet Buffer Size: 1024
```

### Performance Optimization for MMORPG

#### Object Pooling Setup

**Pool Categories:**
```
1. Players Pool: 50 objects
2. NPCs Pool: 100 objects
3. Monsters Pool: 200 objects
4. Projectiles Pool: 500 objects
5. Effects Pool: 300 objects
6. Damage Numbers Pool: 200 objects
```

#### Asset Bundle Configuration

**Addressables Groups:**
```
1. Characters - Remote, Load on demand
2. Maps - Remote, Load on demand
3. Items - Remote, Load on demand
4. Spells - Remote, Load on demand
5. UI - Local, Loaded at start
6. Audio - Remote, Load on demand
```

### Memory Management

**Texture Settings:**
```
Max Texture Size: 2048 (for sprites)
Compression: Normal Quality
Use Crunch Compression: True (for WebGL)
Streaming Mip Maps: False (2D doesn't need mipmaps)
```

**Mesh Settings:**
```
Read/Write Enabled: False
Optimize Mesh: True
Keep Quads: False
```

---

## Dofus-Like Game Checklist

### Graphics & Rendering

- [ ] **Isometric Grid Setup**
  - Grid cell size: 86x43 pixels (or custom)
  - Tilemap renderer mode: Individual
  - Transparency sort axis configured

- [ ] **Sprite Configuration**
  - Pixels per unit: 100
  - Mesh type: Tight
  - Filter mode: Point (for pixel art)
  - Compression: None or Low Quality

- [ ] **Sorting Layers**
  - Background, Ground, Characters, Objects, Effects, UI
  - Character sorting based on Y position

- [ ] **Camera Setup**
  - Orthographic projection
  - Pixel Perfect Camera component (optional)
  - Cinemachine for smooth following

### Map System

- [ ] **Tilemap Layers**
  - Ground layer (walkable terrain)
  - Objects layer (decorations)
  - Collision layer (obstacles)
  - Overlay layer (effects)

- [ ] **Cell Grid**
  - 14x20 cell map size (Dofus standard)
  - Cell walkability data
  - Cell movement cost

- [ ] **Pathfinding**
  - A* algorithm implementation
  - Diagonal movement support
  - Path smoothing

### Character System

- [ ] **Character Classes**
  - 12 breeds (Feca, Osamodas, Enutrof, etc.)
  - Gender variants (Male/Female)
  - Class-specific sprites and animations

- [ ] **Character Controller**
  - Click-to-move with pathfinding
  - 8-directional sprite rotation
  - Smooth movement interpolation

- [ ] **Character Animations**
  - Idle (4 directions)
  - Walk (4 directions)
  - Attack (4 directions)
  - Cast spell
  - Take damage
  - Death

### Combat System

- [ ] **Turn-Based Combat**
  - Turn order based on initiative
  - Movement points (MP) system
  - Action points (AP) system
  - Turn timer (120 seconds default)

- [ ] **Spell System**
  - Spell range visualization
  - Area of effect (AOE) display
  - Line of sight checking
  - Spell cooldowns

- [ ] **Battle UI**
  - Fighter health bars
  - Turn indicator
  - Spell bar
  - Timeline display

### UI System

- [ ] **Login Screen**
  - Username/password fields
  - Server selection
  - Remember me option

- [ ] **Character Selection**
  - Character list
  - Character preview
  - Create new character
  - Delete character

- [ ] **Game UI**
  - Inventory panel (grid-based)
  - Character sheet
  - Spells panel
  - Map overview
  - Chat window (All, Team, Guild, Private)

- [ ] **Keyboard Shortcuts**
  - I: Inventory
  - C: Character
  - S: Spells
  - M: Map
  - Enter: Chat focus

### Networking

- [ ] **WebSocket Client**
  - Connection management
  - Auto-reconnect
  - Packet queue system
  - Message handlers

- [ ] **Protocol Messages**
  - HelloConnectMessage
  - IdentificationMessage
  - CharactersListMessage
  - CharacterSelectionMessage
  - CurrentMapMessage
  - GameMapMovementMessage
  - GameFightStartingMessage
  - ChatServerMessage

- [ ] **State Synchronization**
  - Character position
  - Character stats
  - Inventory state
  - Combat state

### Data Management

- [ ] **Local Caching**
  - Map data caching
  - Item data caching
  - Character data caching

- [ ] **Save System**
  - PlayerPrefs for settings
  - Local file storage for cache

### Audio

- [ ] **Music System**
  - Background music for zones
  - Combat music
  - Menu music

- [ ] **Sound Effects**
  - Footsteps
  - Spell casts
  - UI clicks
  - Combat sounds

### Performance

- [ ] **Object Pooling**
  - Characters
  - Effects
  - Damage numbers
  - Projectiles

- [ ] **Sprite Atlases**
  - UI elements
  - Character sprites
  - Item icons
  - Spell icons

- [ ] **LOD System**
  - Distant character simplification
  - Particle effect reduction

---

## Additional Resources

### Official Unity Documentation

**Unity 2022.3 LTS Documentation:**
- **Main Docs:** https://docs.unity3d.com/2022.3/Documentation/Manual/
- **Scripting Reference:** https://docs.unity3d.com/2022.3/Documentation/ScriptReference/

**Isometric Tilemap:**
- **Manual:** https://docs.unity3d.com/2022.3/Documentation/Manual/Tilemap-Isometric.html
- **Tutorial:** https://learn.unity.com/tutorial/working-with-hexagonal-and-isometric-tile-shapes

**Input System:**
- **Manual:** https://docs.unity3d.com/Packages/com.unity.inputsystem@1.8/manual/index.html
- **Quick Start:** https://docs.unity3d.com/Packages/com.unity.inputsystem@1.8/manual/QuickStartGuide.html

**Netcode for GameObjects:**
- **Docs:** https://docs-multiplayer.unity3d.com/
- **Getting Started:** https://docs-multiplayer.unity3d.com/netcode/current/tutorials/get-started-ngo/

### Unity Learn Tutorials

**2D Game Development:**
- **2D Game Kit:** https://learn.unity.com/project/2d-game-kit
- **Ruby's Adventure:** https://learn.unity.com/project/ruby-s-2d-rpg
- **Isometric Environments:** https://blog.unity.com/engine-platform/isometric-2d-environments-with-tilemap

**Multiplayer Networking:**
- **Boss Room Sample:** https://unity.com/demos/small-scale-coop-sample
- **Multiplayer Networking:** https://learn.unity.com/course/multiplayer-networking

### Unity Blog Posts

**Isometric 2D Development:**
- **Blog:** https://blog.unity.com/engine-platform/isometric-2d-environments-with-tilemap
- **Tips:** https://www.evozon.com/two-unity-tricks-isometric-games/

**What's New in Unity 2022 LTS:**
- **Release Overview:** https://unity.com/releases/2022-lts
- **What's New:** https://docs.unity3d.com/2022.3/Documentation/Manual/WhatsNew2022LTS.html

### GitHub Resources

**Sample Projects:**
- **2D Isometric Tilemaps:** https://github.com/UnityTechnologies/2D_IsoTilemaps
- **Boss Room (Multiplayer):** https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop

**Third-Party Libraries:**
- **NativeWebSocket:** https://github.com/endel/NativeWebSocket
- **A* Pathfinding:** https://github.com/arongranberg/astar-pathfinding-project

### Community Forums

**Unity Discussions:**
- **Isometric MMORPG Discussions:** https://discussions.unity.com/t/creating-an-isometric-2d-rpg-mmo-ongoing-series/815274
- **2D Forum:** https://discussions.unity.com/c/2d/11

**Reddit Communities:**
- **r/Unity2D:** https://www.reddit.com/r/Unity2D/
- **r/gamedev:** https://www.reddit.com/r/gamedev/

### Asset Store Recommendations

**Free Assets:**
- **DOTween:** https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676
- **Post Processing Stack:** Built into URP

**Paid Assets:**
- **MMORPG Kit:** https://assetstore.unity.com/packages/templates/systems/mmorpg-kit-2d-3d-survival-110188
- **A* Pathfinding Project:** https://assetstore.unity.com/packages/tools/ai/a-pathfinding-project-pro-87744
- **Odin Inspector:** https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041

### YouTube Channels

**Unity Official:**
- **Unity:** https://www.youtube.com/user/Unity3D
- **Unity Learn:** https://www.youtube.com/c/UnityLearn

**2D Game Development:**
- **Brackeys:** https://www.youtube.com/c/Brackeys
- **Blackthornprod:** https://www.youtube.com/c/Blackthornprod
- **Code Monkey:** https://www.youtube.com/c/CodeMonkeyUnity

**Multiplayer Development:**
- **Dapper Dino:** https://www.youtube.com/c/DapperDinoCodingTutorials
- **Infallible Code:** https://www.youtube.com/c/InfallibleCode

---

## Quick Start Commands

### Package Manager Console

```bash
# Add package by name
Window > Package Manager > + > Add package by name...

# Example packages to add:
com.unity.2d.tilemap.extras
com.unity.inputsystem
com.unity.cinemachine
com.unity.nuget.newtonsoft-json
```

### Project Structure Setup

```bash
# Create folder structure (in Unity Project window)
Assets/
├── _Project/
│   ├── Art/
│   │   ├── Sprites/
│   │   ├── Animations/
│   │   └── UI/
│   ├── Audio/
│   │   ├── Music/
│   │   └── SFX/
│   ├── Prefabs/
│   ├── Resources/
│   ├── Scenes/
│   ├── Scripts/
│   │   ├── Core/
│   │   ├── Networking/
│   │   ├── Player/
│   │   ├── Map/
│   │   ├── Combat/
│   │   ├── UI/
│   │   └── Utils/
│   └── Settings/
├── Plugins/
└── StreamingAssets/
```

---

## Troubleshooting

### Common Issues

**Issue: "Assembly has reference to non-existent assembly"**
```
Solution:
1. Edit > Project Settings > Player > Other Settings
2. Set API Compatibility Level to .NET Standard 2.1
3. Restart Unity
```

**Issue: Input System not working**
```
Solution:
1. Edit > Project Settings > Player > Other Settings
2. Active Input Handling: Input System Package (New)
3. Restart Unity Editor
```

**Issue: Isometric tiles not sorting correctly**
```
Solution:
1. Edit > Project Settings > Graphics
2. Transparency Sort Mode: Custom Axis
3. Transparency Sort Axis: (0, 1, -0.26)
4. Set Tilemap Renderer Mode to "Individual"
```

**Issue: WebGL build fails**
```
Solution:
1. Remove any System.IO.File operations
2. Replace with UnityWebRequest or Resources.Load
3. Check for mobile-only or unsupported APIs
```

---

## Next Steps

After completing this setup guide:

1. **Read the Migration Guide:** `docs/architecture/unity-migration.md`
2. **Review Backend API:** `docs/architecture/backend-migration.md`
3. **Set up Version Control:** Initialize Git repository
4. **Create First Scene:** Implement Login scene
5. **Test WebSocket Connection:** Verify server connectivity

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-10-25 | Initial setup guide creation |

---

## License

This documentation is part of the GOFUS project.

---

**Need Help?**

- Check Unity Forums: https://discussions.unity.com/
- Unity Support: https://support.unity.com/
- GOFUS Project Documentation: See `/docs` folder

---

**Happy Developing!**
