# GOFUS Unity Client - Comprehensive Development Plan

## Project Overview
Transform the existing Flash/Electron Dofus Retro client into a modern Unity 2D client that integrates with the existing Node.js backend (`gofus-backend`) and game server (`gofus-game-server`).

---

## 🎯 Integration Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     GOFUS Architecture                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│   Unity Client              Game Server         Backend API  │
│  ┌────────────┐         ┌──────────────┐     ┌────────────┐│
│  │ gofus-     │ WS/3001 │ gofus-game-  │ HTTP│ gofus-     ││
│  │ client     │◄────────│ server       │◄────│ backend    ││
│  │ (Unity 2D) │         │ (Socket.IO)  │     │ (Next.js)  ││
│  └────────────┘         └──────────────┘     └────────────┘│
│       │                        │                     │       │
│   Rendering              State Mgmt            Database      │
│   - Sprites              - Players             - Supabase    │
│   - Maps                 - Combat              - PostgreSQL  │
│   - UI                   - Movement            - Redis       │
│                          - Chat                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 📁 Project Structure

```
gofus-client/
├── Assets/
│   ├── _Project/
│   │   ├── Scripts/
│   │   │   ├── Core/
│   │   │   │   ├── GameManager.cs
│   │   │   │   ├── Singleton.cs
│   │   │   │   ├── Constants.cs
│   │   │   │   ├── GameState.cs
│   │   │   │   └── Configuration.cs
│   │   │   ├── Networking/
│   │   │   │   ├── NetworkManager.cs
│   │   │   │   ├── PacketHandler.cs
│   │   │   │   ├── MessageQueue.cs
│   │   │   │   ├── Protocols/
│   │   │   │   │   ├── AuthProtocol.cs
│   │   │   │   │   ├── CharacterProtocol.cs
│   │   │   │   │   ├── MapProtocol.cs
│   │   │   │   │   ├── CombatProtocol.cs
│   │   │   │   │   └── ChatProtocol.cs
│   │   │   │   └── Messages/
│   │   │   │       ├── BaseMessage.cs
│   │   │   │       ├── AuthMessages.cs
│   │   │   │       ├── CharacterMessages.cs
│   │   │   │       ├── MapMessages.cs
│   │   │   │       ├── CombatMessages.cs
│   │   │   │       └── ChatMessages.cs
│   │   │   ├── Player/
│   │   │   │   ├── PlayerController.cs
│   │   │   │   ├── PlayerAnimator.cs
│   │   │   │   ├── PlayerStats.cs
│   │   │   │   ├── PlayerInventory.cs
│   │   │   │   └── PlayerNameplate.cs
│   │   │   ├── Map/
│   │   │   │   ├── MapRenderer.cs
│   │   │   │   ├── MapLoader.cs
│   │   │   │   ├── CellGrid.cs
│   │   │   │   ├── IsometricHelper.cs
│   │   │   │   ├── Pathfinding/
│   │   │   │   │   ├── AStar.cs
│   │   │   │   │   └── PathNode.cs
│   │   │   │   └── InteractiveElements.cs
│   │   │   ├── Combat/
│   │   │   │   ├── BattleManager.cs
│   │   │   │   ├── TurnManager.cs
│   │   │   │   ├── SpellSystem.cs
│   │   │   │   ├── Fighter.cs
│   │   │   │   ├── SpellAnimator.cs
│   │   │   │   └── DamageCalculator.cs
│   │   │   ├── Entities/
│   │   │   │   ├── EntityManager.cs
│   │   │   │   ├── Entity.cs
│   │   │   │   ├── NPC.cs
│   │   │   │   ├── Monster.cs
│   │   │   │   └── GameObject.cs
│   │   │   ├── UI/
│   │   │   │   ├── UIManager.cs
│   │   │   │   ├── Screens/
│   │   │   │   │   ├── LoginScreen.cs
│   │   │   │   │   ├── CharacterSelectionScreen.cs
│   │   │   │   │   ├── GameScreen.cs
│   │   │   │   │   └── LoadingScreen.cs
│   │   │   │   ├── Panels/
│   │   │   │   │   ├── InventoryPanel.cs
│   │   │   │   │   ├── CharacterPanel.cs
│   │   │   │   │   ├── SpellsPanel.cs
│   │   │   │   │   ├── ChatPanel.cs
│   │   │   │   │   ├── MapPanel.cs
│   │   │   │   │   ├── GuildPanel.cs
│   │   │   │   │   ├── FriendsPanel.cs
│   │   │   │   │   └── MarketplacePanel.cs
│   │   │   │   └── Components/
│   │   │   │       ├── ItemSlot.cs
│   │   │   │       ├── SpellSlot.cs
│   │   │   │       ├── ChatMessage.cs
│   │   │   │       ├── Tooltip.cs
│   │   │   │       └── ContextMenu.cs
│   │   │   ├── Audio/
│   │   │   │   ├── AudioManager.cs
│   │   │   │   ├── MusicController.cs
│   │   │   │   └── SFXController.cs
│   │   │   └── Utils/
│   │   │       ├── ObjectPool.cs
│   │   │       ├── SpriteManager.cs
│   │   │       ├── LocalizationManager.cs
│   │   │       ├── SaveManager.cs
│   │   │       └── InputManager.cs
│   │   ├── Prefabs/
│   │   │   ├── Characters/
│   │   │   │   ├── PlayerPrefab.prefab
│   │   │   │   ├── NPCPrefab.prefab
│   │   │   │   └── MonsterPrefab.prefab
│   │   │   ├── UI/
│   │   │   │   ├── Screens/
│   │   │   │   └── Panels/
│   │   │   ├── Effects/
│   │   │   │   ├── SpellEffects/
│   │   │   │   └── ParticleEffects/
│   │   │   └── Map/
│   │   │       ├── MapTile.prefab
│   │   │       └── InteractiveObject.prefab
│   │   ├── Resources/
│   │   │   ├── Sprites/
│   │   │   │   ├── Characters/
│   │   │   │   │   ├── Classes/ (12 classes × 2 genders)
│   │   │   │   │   ├── NPCs/
│   │   │   │   │   └── Monsters/
│   │   │   │   ├── Items/
│   │   │   │   ├── Spells/
│   │   │   │   ├── Maps/
│   │   │   │   │   ├── Tiles/
│   │   │   │   │   └── Objects/
│   │   │   │   └── UI/
│   │   │   ├── Animations/
│   │   │   ├── Audio/
│   │   │   │   ├── Music/
│   │   │   │   └── SFX/
│   │   │   └── Data/
│   │   │       ├── Maps/
│   │   │       ├── Items/
│   │   │       └── Spells/
│   │   ├── Scenes/
│   │   │   ├── Main.unity
│   │   │   ├── Login.unity
│   │   │   └── Game.unity
│   │   └── Settings/
│   │       ├── InputSettings.asset
│   │       └── GameSettings.asset
│   ├── Plugins/
│   │   ├── WebSocket/
│   │   └── JsonDotNet/
│   └── StreamingAssets/
│       ├── Maps/
│       └── Localization/
├── Packages/
├── ProjectSettings/
└── UserSettings/
```

---

## 📋 Detailed Feature Implementation Plan

### Phase 1: Core Infrastructure (Week 1)

#### 1.1 Project Setup (Day 1)
**Steps:**
1. Create Unity 2022.3 LTS project named "gofus-client"
2. Configure project settings:
   - 2D mode
   - Universal Render Pipeline (URP)
   - Target platforms: PC, Mac, Linux
3. Install packages via Package Manager:
   - TextMeshPro
   - 2D Sprite
   - 2D Tilemap
   - Cinemachine
   - Addressables
4. Import third-party packages:
   - NativeWebSocket
   - Newtonsoft.Json
   - DOTween (free)

#### 1.2 Core Systems (Day 2-3)
**GameManager.cs Implementation:**
```csharp
// Singleton pattern for game state management
// Features to implement:
- Application lifecycle management
- Scene transitions
- Game state machine (Login, CharSelection, InGame, Battle)
- Player data caching
- Settings management
```

**Configuration System:**
```csharp
// Load from StreamingAssets/config.json
- Server endpoints (ws://localhost:3001)
- Game settings (resolution, quality)
- Debug settings
- Asset paths
```

#### 1.3 Input System (Day 4)
**InputManager.cs:**
```csharp
// Unified input handling
- Mouse controls (click-to-move)
- Keyboard shortcuts (inventory, spells, etc.)
- Controller support (optional)
- Touch support preparation
```

#### 1.4 Localization System (Day 5)
**LocalizationManager.cs:**
```csharp
// Multi-language support
- Load language files from StreamingAssets
- Runtime language switching
- Text replacement system
- Font management per language
```

---

### Phase 2: Networking Layer (Week 2)

#### 2.1 WebSocket Client (Day 1-2)
**NetworkManager.cs Features:**
```csharp
// WebSocket connection to game server
1. Connection Management:
   - Connect to ws://localhost:3001
   - Auto-reconnection with exponential backoff
   - Connection state tracking
   - Heartbeat/ping-pong

2. Message Handling:
   - Binary/JSON message support
   - Message queuing for thread safety
   - Compression support

3. Error Handling:
   - Connection failures
   - Timeout management
   - Graceful disconnection
```

#### 2.2 Protocol Implementation (Day 3-4)
**Message Types (from game-server):**
```typescript
// Port these to C# classes
- HelloConnectMessage
- IdentificationMessage
- CharactersListMessage
- CharacterSelectionMessage
- MapDataMessage
- GameContextCreateMessage
- GameMapMovementMessage
- GameFightStartingMessage
- ChatServerMessage
- InventoryContentMessage
- StatsUpgradeMessage
- LevelUpMessage
```

**PacketHandler.cs:**
```csharp
// Message routing system
- Message type registration
- Handler delegation
- Serialization/deserialization
- Message validation
```

#### 2.3 Integration Testing (Day 5)
```csharp
// Test connections with game-server
- Authentication flow
- Character selection
- Map loading
- Movement synchronization
```

---

### Phase 3: Map System (Week 3)

#### 3.1 Isometric Grid (Day 1-2)
**MapRenderer.cs Features:**
```csharp
// Dofus isometric map rendering
1. Grid Setup:
   - 14x20 cell grid
   - Cell size: 86x43 pixels
   - Isometric projection matrix
   - Diamond-shaped grid

2. Coordinate Systems:
   - Cell ID to world position
   - World position to cell ID
   - Screen to world conversion
   - Isometric to cartesian conversion
```

**CellGrid.cs:**
```csharp
// Grid data management
- Cell walkability
- Line of sight
- Movement costs
- Interactive elements per cell
- Occupancy tracking
```

#### 3.2 Map Loading (Day 3)
**MapLoader.cs:**
```csharp
// Load map data from server/cache
1. Map Data Structure:
   - Parse server map format
   - Decrypt map data (if needed)
   - Cache loaded maps

2. Tilemap Generation:
   - Ground layer
   - Object layer 1
   - Object layer 2
   - Collision layer
```

#### 3.3 Pathfinding (Day 4)
**AStar.cs Implementation:**
```csharp
// A* pathfinding for movement
- Diagonal movement support
- Movement cost calculation
- Obstacle avoidance
- Path smoothing
- Multi-threading support
```

#### 3.4 Camera System (Day 5)
**Using Cinemachine:**
```csharp
// Camera controls
- Follow player
- Map bounds limiting
- Zoom in/out
- Edge scrolling
- Smooth transitions
```

---

### Phase 4: Character System (Week 4)

#### 4.1 Player Controller (Day 1-2)
**PlayerController.cs Features:**
```csharp
1. Movement:
   - Click-to-move
   - Path following
   - Movement validation
   - Animation triggering

2. Actions:
   - Interact with objects
   - Attack enemies
   - Cast spells
   - Use items
```

**PlayerAnimator.cs:**
```csharp
// Animation management
- 8-direction sprites
- Walk/run animations
- Combat animations
- Emote animations
- Death animation
```

#### 4.2 Entity System (Day 3)
**EntityManager.cs:**
```csharp
// Manage all entities in the game
- Spawn/despawn entities
- Update entity positions
- Entity lookup by ID
- Batch updates
```

**Entity Types:**
- Player (local and remote)
- NPC
- Monster
- Interactive GameObject

#### 4.3 Character Stats (Day 4)
**PlayerStats.cs:**
```csharp
// Character statistics
- Base stats (STR, INT, AGI, CHA, VIT, WIS)
- Derived stats (HP, AP, MP, Initiative)
- Level and experience
- Stat point allocation
```

#### 4.4 Inventory System (Day 5)
**PlayerInventory.cs:**
```csharp
// Item management
- Equipment slots
- Bag inventory
- Item stacking
- Drag & drop
- Item usage
```

---

### Phase 5: Combat System (Week 5)

#### 5.1 Battle Manager (Day 1-2)
**BattleManager.cs:**
```csharp
// Turn-based combat management
1. Battle Initialization:
   - Fighter placement
   - Turn order calculation
   - UI transition

2. Turn Management:
   - Action points tracking
   - Movement points tracking
   - Turn timer
   - End turn
```

#### 5.2 Spell System (Day 3)
**SpellSystem.cs:**
```csharp
// Spell casting mechanics
- Spell data loading
- Range calculation
- Area of effect
- Line of sight checking
- Cooldowns
```

**SpellAnimator.cs:**
```csharp
// Visual effects
- Spell animations
- Projectiles
- Impact effects
- Damage numbers
```

#### 5.3 Combat UI (Day 4)
**Combat HUD:**
```csharp
- Turn order display
- Action/movement points
- Spell bar
- Fighter information
- Combat log
```

#### 5.4 AI Integration (Day 5)
**For PvE battles:**
```csharp
// Monster AI
- Basic AI behaviors
- Spell selection
- Target prioritization
- Movement decisions
```

---

### Phase 6: UI Implementation (Week 6)

#### 6.1 Screen Management (Day 1)
**Login Screen:**
```csharp
- Username/password fields
- Remember me checkbox
- Server selection
- Version display
- Connection status
```

**Character Selection:**
```csharp
- Character list
- Character preview
- Create character button
- Delete character
- Select server
```

#### 6.2 Game HUD (Day 2)
**Main Interface:**
```csharp
- Health/energy bars
- Experience bar
- Quick action bar
- Minimap
- Chat window
- Menu buttons
```

#### 6.3 Panels Implementation (Day 3-4)
**Inventory Panel:**
```csharp
- Equipment slots
- Bag grid
- Stats display
- Item tooltips
```

**Character Panel:**
```csharp
- Stats display
- Stat point allocation
- Character info
- Titles/achievements
```

**Spells Panel:**
```csharp
- Spell list
- Spell levels
- Spell point allocation
- Spell descriptions
```

#### 6.4 Chat System (Day 5)
**ChatPanel.cs:**
```csharp
- Multiple channels (General, Trade, Guild, Party, Private)
- Message formatting
- Emotes support
- Link parsing
- Message history
```

---

### Phase 7: Asset Migration (Week 7)

#### 7.1 Asset Extraction (Day 1-2)
**Using JPEXS Decompiler:**
```bash
# Extract from existing SWF files
1. Character sprites:
   - 12 classes (Feca, Osamodas, Enutrof, Sram, Xelor, Ecaflip,
                Eniripsa, Iop, Cra, Sadida, Sacrieur, Pandawa)
   - 2 genders per class
   - 8 directions per character
   - Multiple animation states

2. Map assets:
   - Tiles
   - Objects
   - Interactive elements

3. UI elements:
   - Buttons
   - Panels
   - Icons

4. Spell effects:
   - Icons
   - Animations

5. Audio:
   - Music tracks
   - Sound effects
```

#### 7.2 Sprite Processing (Day 3)
**Asset Pipeline:**
```csharp
1. Import settings:
   - Pixels per unit: 100
   - Filter mode: Point (no filter)
   - Compression: None/High Quality

2. Sprite slicing:
   - Automatic slicing for spritesheets
   - Pivot point adjustment

3. Animation creation:
   - Create animation clips
   - Set frame rates
```

#### 7.3 Sprite Atlas Creation (Day 4)
**Using Unity Sprite Atlas:**
```csharp
- Group related sprites
- Optimize draw calls
- Configure compression per platform
```

#### 7.4 Audio Integration (Day 5)
**Audio Setup:**
```csharp
- Import audio files
- Configure compression
- Set up audio mixers
- Create audio zones
```

---

### Phase 8: Integration & Testing (Week 8)

#### 8.1 Backend Integration (Day 1-2)
**Connect to gofus-backend:**
```csharp
// REST API calls for:
- User authentication
- Character data
- Inventory persistence
- Guild management
- Market transactions
```

**Connect to gofus-game-server:**
```csharp
// WebSocket for:
- Real-time gameplay
- Movement synchronization
- Combat
- Chat
```

#### 8.2 Performance Optimization (Day 3)
```csharp
1. Object Pooling:
   - Reuse game objects
   - Reduce instantiation overhead

2. Draw Call Batching:
   - Sprite atlases
   - Material sharing

3. Memory Management:
   - Asset unloading
   - Garbage collection optimization
```

#### 8.3 Testing (Day 4)
**Test Scenarios:**
```csharp
1. Connection tests:
   - Login flow
   - Disconnection handling
   - Reconnection

2. Gameplay tests:
   - Movement
   - Combat
   - Inventory
   - Chat

3. Stress tests:
   - Multiple entities
   - Large maps
   - Many spell effects
```

#### 8.4 Build & Deployment (Day 5)
**Platform Builds:**
```csharp
1. Windows:
   - 64-bit build
   - Installer creation

2. Mac:
   - Universal build
   - App bundle

3. Linux:
   - AppImage/Snap package
```

---

## 🔌 Integration Points with Existing Systems

### gofus-backend Integration
```typescript
// REST API Endpoints to consume:
- POST /api/auth/login
- POST /api/auth/register
- GET /api/characters
- POST /api/characters
- GET /api/inventory/:characterId
- PUT /api/inventory/:characterId
- GET /api/guilds/:guildId
- GET /api/market
```

### gofus-game-server Integration
```typescript
// WebSocket Events to handle:
// From server:
- 'connect'
- 'authenticated'
- 'charactersList'
- 'gameReady'
- 'mapData'
- 'playerMove'
- 'entitySpawn'
- 'entityDespawn'
- 'battleStart'
- 'turnStart'
- 'spellCast'
- 'chatMessage'

// To server:
- 'authenticate'
- 'selectCharacter'
- 'requestMove'
- 'requestSpellCast'
- 'endTurn'
- 'sendChat'
- 'interactObject'
```

---

## 🎮 Key Technical Decisions

### Why Unity over Godot/Unreal?
- ✅ Best 2D support for isometric games
- ✅ Mature WebSocket libraries
- ✅ Large asset store
- ✅ Cross-platform builds
- ✅ C# (similar to TypeScript)

### Architecture Pattern: MVC
```csharp
Model (Data):
- Game state
- Player data
- Map data

View (Unity):
- Sprites
- UI
- Animations

Controller (Logic):
- NetworkManager
- GameManager
- InputManager
```

### Network Architecture
```csharp
// Client-authoritative for:
- UI interactions
- Animation
- Temporary effects

// Server-authoritative for:
- Movement validation
- Combat
- Inventory changes
- Game state
```

---

## 📊 Performance Targets

- **FPS**: 60 FPS minimum
- **Network Latency**: <100ms average
- **Load Time**: <3 seconds
- **Memory Usage**: <2GB RAM
- **Build Size**: <500MB

---

## 🧪 Testing Strategy

### Unit Tests
```csharp
// Test core systems:
- Pathfinding algorithms
- Coordinate conversions
- Message serialization
- State management
```

### Integration Tests
```csharp
// Test integrations:
- Server communication
- Database operations
- Asset loading
```

### Playtests
```csharp
// Test gameplay:
- Movement feel
- Combat balance
- UI responsiveness
- Performance
```

---

## 📅 Timeline Summary

| Week | Focus | Deliverables |
|------|-------|--------------|
| 1 | Core Infrastructure | Project setup, core systems |
| 2 | Networking | WebSocket client, protocols |
| 3 | Map System | Isometric rendering, pathfinding |
| 4 | Character System | Player control, entities |
| 5 | Combat System | Turn-based combat, spells |
| 6 | UI Implementation | All screens and panels |
| 7 | Asset Migration | Extract and import assets |
| 8 | Integration & Testing | Full integration, optimization |

---

## 🚀 Next Immediate Steps

1. **Create Unity Project**
   ```bash
   # In Unity Hub
   - New Project > 2D (URP)
   - Name: gofus-client
   - Location: C:\Users\HardM\Desktop\Enterprise\gofus\
   ```

2. **Install Required Packages**
   ```
   Window > Package Manager
   - Install listed packages
   ```

3. **Create Folder Structure**
   ```
   - Set up all directories as outlined
   ```

4. **Implement Core Scripts**
   ```
   - Start with GameManager.cs
   - Then NetworkManager.cs
   ```

5. **Test Connection**
   ```
   - Connect to gofus-game-server
   - Verify WebSocket communication
   ```

---

## 🎯 Success Metrics

- ✅ Successful connection to game server
- ✅ Character movement working
- ✅ Map rendering functional
- ✅ Combat system operational
- ✅ All UI panels implemented
- ✅ Assets successfully migrated
- ✅ Performance targets met
- ✅ Cross-platform builds working

---

## 📚 Resources & References

### Documentation
- Unity 2D: https://docs.unity3d.com/Manual/Unity2D.html
- NativeWebSocket: https://github.com/endel/NativeWebSocket
- Socket.IO Client: https://github.com/Rocher0724/socket.io-unity

### Asset Tools
- JPEXS Decompiler: https://github.com/jindrapetrik/jpexs-decompiler
- TexturePacker: https://www.codeandweb.com/texturepacker

### Similar Projects
- Wakfu (Unity-based MMO)
- Albion Online (Unity MMO)
- Tactical RPGs in Unity

---

This plan provides a complete roadmap for migrating from the Flash client to a modern Unity implementation while maintaining full compatibility with your existing backend infrastructure.