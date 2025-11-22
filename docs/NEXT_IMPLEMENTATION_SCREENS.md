# GOFUS - Next Implementation: Multiplayer & Advanced Features

**Priority**: 🟡 HIGH (Game World Complete)
**Goal**: Add multiplayer synchronization and advanced UI features
**Estimated Time**: 1-2 weeks for multiplayer MVP
**Last Updated**: November 21, 2025

## ✅ COMPLETED (November 20-21, 2025)

### Game World Systems - ALL COMPLETE
- ✅ GameHUD (Main Game Interface) - COMPLETE
- ✅ Map Rendering System - COMPLETE
- ✅ Movement System - COMPLETE
- ✅ Camera Controls - COMPLETE
- ✅ Sprite Positioning Fix - COMPLETE

**What Works Now:**
- Players can login, create/select characters
- Enter game world with full GameHUD
- See 560-cell isometric map
- Click to move with A* pathfinding
- Camera follows, pan, zoom, drag
- Full character sprites visible (not just face!)
- Movement animations in 8 directions

---

## 📋 TABLE OF CONTENTS

1. [Screen 1: GameHUD (Main Game Interface)](#screen-1-gamehud)
2. [Screen 2: Map Rendering System](#screen-2-map-rendering)
3. [Screen 3: Movement System](#screen-3-movement-system)
4. [Screen 4: Inventory UI](#screen-4-inventory-ui)
5. [Screen 5: Chat UI](#screen-5-chat-ui)
6. [Technical Architecture](#technical-architecture)
7. [Implementation Order](#implementation-order)

---

## ✅ COMPLETED: GameHUD (Main Game Interface)

### Status: ✅ COMPLETE (November 19-21, 2025)
### Implementation Time: 2 days
### Complexity: Medium - DONE

### Overview
The GameHUD is the main game screen that displays after clicking "Play" on Character Selection. It's the primary interface where players spend most of their time.

---

### UI Layout Design

```
┌─────────────────────────────────────────────────────────────┐
│  [Character Info]          [Time/Server]      [Menu Buttons]│
│  Name: TestWarrior                 19:34       [Inv][Char]   │
│  Level: 50 | Class: Iop          Astrub       [Guild][Set]   │
│  ┌────────────────┐                                          │
│  │ HP: 2500/3000  ██████████░░░░ (83%)                       │
│  │ MP: 180/200    ████████████░░ (90%)                       │
│  └────────────────┘                                          │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│                    [GAME WORLD VIEW]                         │
│                                                               │
│                   Map renders here with                      │
│                   character in center                        │
│                   (Grid: 14x20 cells)                        │
│                                                               │
│                                                               │
│                                                               │
│                                                               │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│              [Action Bar - Spells/Abilities]                 │
│  ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐    [Chat Toggle]  │
│  │1 │ │2 │ │3 │ │4 │ │5 │ │6 │ │7 │ │8 │    [Minimap]       │
│  └──┘ └──┘ └──┘ └──┘ └──┘ └──┘ └──┘ └──┘                   │
│  Spell names, AP costs, cooldowns displayed below           │
│                                                               │
│  [Emotes] [Auto] [Attack]           [Logout] [Settings]     │
└─────────────────────────────────────────────────────────────┘
```

---

### Component Breakdown

#### 1. Character Info Panel (Top-Left)
**Position**: Anchored top-left (10, -10)
**Size**: 300x120

**Elements:**
- **Name Label**: Character name (16pt, bold, white with black outline)
- **Level/Class Label**: "Level X | Class Name" (12pt, yellow)
- **Health Bar**:
  - Background: Dark red
  - Fill: Bright red gradient
  - Text: "HP: 2500/3000 (83%)" centered, white
  - Update in real-time as damage taken/healed
- **Mana Bar**:
  - Background: Dark blue
  - Fill: Bright blue gradient
  - Text: "MP: 180/200 (90%)" centered, white
  - Update when spells cast/mana regenerated

**Code Example:**
```csharp
private void CreateCharacterInfoPanel()
{
    GameObject panel = UIUtils.CreatePanel(transform, "CharacterInfo",
        new Vector2(10, -10), new Vector2(300, 120), UIUtils.AnchorPreset.TopLeft);

    characterNameText = UIUtils.CreateText(panel.transform, "NameLabel",
        "Character Name", 16, FontStyle.Bold);
    characterNameText.GetComponent<Outline>().effectDistance = new Vector2(1, -1);

    GameObject healthBarBg = UIUtils.CreateImage(panel.transform, "HealthBG",
        new Vector2(0, -30), new Vector2(280, 20), Color.red * 0.3f);

    healthBar = UIUtils.CreateSlider(healthBarBg.transform, "HealthBar",
        1.0f, Color.red, Color.white);

    // Similar for mana bar...
}
```

#### 2. Time/Server Info (Top-Center)
**Position**: Anchored top-center
**Size**: 200x60

**Elements:**
- **Current Time**: System time or server time (14pt)
- **Server Name**: "Astrub Server" or connection status (12pt)
- **Ping Display**: Network latency (optional)

#### 3. Menu Buttons (Top-Right)
**Position**: Anchored top-right (-10, -10)
**Size**: 180x100

**Buttons (32x32 each):**
- **Inventory**: Opens inventory panel
- **Character Sheet**: Opens character stats
- **Guild**: Opens guild panel
- **Settings**: Opens settings menu

**Layout**: 2x2 grid with 5px spacing

```csharp
private void CreateMenuButtons()
{
    string[] buttonNames = {"Inventory", "Character", "Guild", "Settings"};
    int index = 0;

    for (int row = 0; row < 2; row++)
    {
        for (int col = 0; col < 2; col++)
        {
            Vector2 pos = new Vector2(-10 - col * 37, -10 - row * 37);
            Button btn = UIUtils.CreateButton(transform, buttonNames[index],
                pos, new Vector2(32, 32), UIUtils.AnchorPreset.TopRight);

            btn.onClick.AddListener(() => OnMenuButtonClick(buttonNames[index]));
            index++;
        }
    }
}
```

#### 4. Game World View (Center)
**Position**: Anchored center, fills most of screen
**Size**: Full width, from top-120 to bottom-100

**Contents:**
- **Map Canvas**: Renders game map (initially simple background)
- **Character Layer**: Player character positioned at center
- **Grid Overlay**: Visual grid for cell selection (optional debug)
- **Other Players**: Other player characters
- **Effects Layer**: Spell effects, damage numbers

**Camera Setup:**
- Orthographic camera
- Follow player character
- Zoom controls (mouse wheel or buttons)
- Pan limits (don't scroll beyond map bounds)

#### 5. Action Bar (Bottom-Center)
**Position**: Anchored bottom-center (0, 10)
**Size**: 500x80

**Elements:**
- **8 Spell Slots**: Buttons with icons, hotkeys 1-8
- **Spell Info**: Name, AP cost, cooldown below each slot
- **Cooldown Overlay**: Gray overlay with timer during cooldown
- **Hotkey Labels**: Small "1", "2", etc. in corner

**Spell Slot Structure:**
```csharp
public class SpellSlot : MonoBehaviour
{
    public Image icon;
    public Text hotkeyLabel;
    public Text nameLabel;
    public Text apCostLabel;
    public Image cooldownOverlay;
    public Text cooldownText;

    private int spellId;
    private int apCost;
    private float cooldownRemaining;

    public void SetSpell(SpellData spell)
    {
        icon.sprite = spell.icon;
        nameLabel.text = spell.name;
        apCostLabel.text = $"{spell.apCost} AP";
        spellId = spell.id;
        apCost = spell.apCost;
    }

    public void OnClick()
    {
        if (cooldownRemaining > 0) return;
        if (GameManager.Instance.CurrentAP < apCost) return;

        // Cast spell
        SpellManager.Instance.CastSpell(spellId);
        StartCooldown(spell.cooldown);
    }
}
```

#### 6. Bottom-Right Controls
**Position**: Anchored bottom-right (-10, 10)
**Size**: 200x80

**Buttons:**
- **Chat Toggle**: Show/hide chat panel
- **Minimap**: Show/hide minimap
- **Logout**: Return to character selection
- **Settings**: Quick settings access

---

### Data Flow

```
Character Selection "Play" Click
    ↓
UIManager.ShowScreen(ScreenType.GameHUD)
    ↓
GameHUD.Initialize()
    ↓
Load Character Data:
    - characterId = PlayerPrefs.GetInt("selected_character_id")
    - Fetch from backend: GET /api/characters/{id}
    ↓
Parse Character Data:
    - name, level, classId, sex, mapId, cellId
    - currentHP, maxHP, currentMP, maxMP
    - equipped spells
    ↓
Display Character:
    - Spawn CharacterLayerRenderer at starting position
    - Update health/mana bars
    - Load spells into action bar
    ↓
Connect to Game Server:
    - WebSocket connection with JWT
    - Send player_enter message
    - Receive initial world state
    ↓
Ready to Play
```

---

### Implementation Steps

**Step 1: Create Basic GameHUD.cs** (1 hour)
```csharp
using UnityEngine;
using UnityEngine.UI;
using GOFUS.Rendering;
using GOFUS.Core;

namespace GOFUS.UI.Screens
{
    public class GameHUD : UIScreen
    {
        [Header("UI References")]
        private Text characterNameText;
        private Text levelClassText;
        private Slider healthBar;
        private Text healthText;
        private Slider manaBar;
        private Text manaText;
        private Text timeServerText;

        [Header("Character")]
        private CharacterLayerRenderer playerCharacter;
        private int characterId;
        private CharacterData characterData;

        [Header("Action Bar")]
        private SpellSlot[] spellSlots = new SpellSlot[8];

        public override void Initialize()
        {
            base.Initialize();
            Debug.Log("[GameHUD] Initializing...");

            CreateUI();
            LoadCharacterData();
        }

        private void CreateUI()
        {
            // Create character info panel
            CreateCharacterInfoPanel();

            // Create menu buttons
            CreateMenuButtons();

            // Create action bar
            CreateActionBar();

            // Create bottom controls
            CreateBottomControls();
        }

        private void LoadCharacterData()
        {
            characterId = PlayerPrefs.GetInt("selected_character_id", -1);

            if (characterId <= 0)
            {
                Debug.LogError("[GameHUD] No character selected!");
                UIManager.Instance.ShowScreen(ScreenType.CharacterSelection);
                return;
            }

            Debug.Log($"[GameHUD] Loading character ID: {characterId}");

            // TODO: Fetch from backend
            // For now, use mock data
            SpawnPlayerCharacter();
            UpdateCharacterInfo();
        }

        private void SpawnPlayerCharacter()
        {
            // Create player character at center
            playerCharacter = ClassSpriteManager.Instance.CreateCharacterRenderer(
                classId: 1, // TODO: Get from character data
                isMale: true,
                parent: transform,
                position: Vector3.zero
            );

            playerCharacter.gameObject.name = "PlayerCharacter";
            Debug.Log("[GameHUD] Player character spawned");
        }

        private void UpdateCharacterInfo()
        {
            // Update UI with character data
            characterNameText.text = "Test Warrior"; // TODO: Real name
            levelClassText.text = "Level 50 | Feca"; // TODO: Real data

            UpdateHealthMana(2500, 3000, 180, 200);
        }

        public void UpdateHealthMana(int hp, int maxHP, int mp, int maxMP)
        {
            healthBar.value = (float)hp / maxHP;
            healthText.text = $"HP: {hp}/{maxHP} ({(int)(healthBar.value * 100)}%)";

            manaBar.value = (float)mp / maxMP;
            manaText.text = $"MP: {mp}/{maxMP} ({(int)(manaBar.value * 100)}%)";
        }

        private void CreateCharacterInfoPanel()
        {
            // TODO: Implement UI creation
        }

        private void CreateMenuButtons()
        {
            // TODO: Implement menu buttons
        }

        private void CreateActionBar()
        {
            // TODO: Implement action bar
        }

        private void CreateBottomControls()
        {
            // TODO: Implement bottom controls
        }

        private void Update()
        {
            // Update time display
            if (timeServerText != null)
            {
                timeServerText.text = System.DateTime.Now.ToString("HH:mm");
            }
        }
    }
}
```

**Step 2: Register in UIManager** (5 minutes)
Edit `UIManager.cs`, uncomment:
```csharp
CreateScreen<GameHUD>(ScreenType.GameHUD);
```

**Step 3: Update Character Selection** (5 minutes)
Edit `CharacterSelectionScreen.cs`:
```csharp
private void PlaySelectedCharacter()
{
    if (selectedCharacterId <= 0) return;

    PlayerPrefs.SetInt("selected_character_id", selectedCharacterId);
    PlayerPrefs.Save();

    Debug.Log($"[CharacterSelection] Playing character ID: {selectedCharacterId}");

    // Transition to GameHUD
    UIManager.Instance.ShowScreen(ScreenType.GameHUD);
}
```

**Step 4: Test** (10 minutes)
- Play game
- Login
- Select character
- Click "Play"
- Should see GameHUD with character

**Step 5: Implement Full UI** (2-3 hours)
- Create all UI panels
- Add proper layout
- Style with colors and fonts
- Add button functionality

**Step 6: Polish** (1 hour)
- Add transitions
- Test different resolutions
- Add tooltips
- Error handling

---

### Testing Checklist

- [ ] GameHUD appears after clicking "Play"
- [ ] Character name displays correctly
- [ ] Level and class show properly
- [ ] Health and mana bars visible
- [ ] Player character renders in center
- [ ] Menu buttons clickable
- [ ] Action bar displays
- [ ] Time updates every second
- [ ] Can logout and return to character selection
- [ ] Works at different screen resolutions

---

## ✅ COMPLETED: Map Rendering System

### Status: ✅ COMPLETE (November 20, 2025)
### Implementation Time: 1 day (Backend already existed)
### Complexity: High - DONE

### Overview
The map rendering system displays the game world where characters can move and interact. This implementation uses a full-stack approach with backend API, database storage, and Unity integration for seamless map transitions and combat mode support.

### Reference Documentation
See `MAP_SYSTEM_IMPLEMENTATION.md` for complete implementation guide with code samples.

---

### Architecture Overview

**Tech Stack:**
- **Backend**: Next.js API + PostgreSQL (Drizzle ORM) + Redis caching
- **Unity**: MapRenderer + IsometricHelper (already exists)
- **Map Format**: JSON-based cell data (560 cells per map)
- **Integration**: RESTful API for map data

**Key Features:**
- ✅ Seamless map transitions (edge detection)
- ✅ Combat mode switching (exploration ↔ turn-based)
- ✅ Globally accessible maps
- ✅ Persistent state across mode changes
- ✅ On-map battle support

---

### Grid System Specifications

**Dofus Grid:**
- **Dimensions**: 14 columns × 20 rows = 560 cells (corrected from previous 280)
- **Projection**: Isometric (diamond shape)
- **Cell Size**: ~43px width × ~21px height (isometric)
- **Numbering**: Cells numbered 0-559 (left-to-right, top-to-bottom)

**Cell Data Structure:**
```typescript
{
  cellId: number;           // 0-559
  walkable: boolean;        // Can walk on this cell
  lineOfSight: boolean;     // Can see through
  level: number;            // Height level (0-15)
  movementCost: number;     // AP cost to move here
  interactive: boolean;     // Has interactions
  coordX: number;           // Grid X coordinate
  coordY: number;           // Grid Y coordinate
}
```

---

### Implementation Phases

### Phase 1: Backend Foundation (1-2 days)

**Database Schema** (`gofus-backend/lib/db/schema/maps.ts`):
```typescript
export const maps = pgTable('maps', {
  id: integer('id').primaryKey(),
  name: varchar('name', { length: 255 }).notNull(),
  width: integer('width').default(14).notNull(),
  height: integer('height').default(20).notNull(),
  cells: jsonb('cells').notNull(), // Array of 560 cell objects
  adjacentMaps: jsonb('adjacent_maps'), // {top, bottom, left, right}
  backgroundMusic: varchar('background_music', { length: 255 }),
  subArea: integer('sub_area'),
  createdAt: timestamp('created_at').defaultNow(),
});
```

**MapService** (`gofus-backend/lib/services/map/map.service.ts`):
- `getMapById(mapId)` - Fetch map with Redis caching
- `getAdjacentMap(mapId, edge)` - Get neighboring map
- Cache invalidation strategy

**API Endpoint** (`gofus-backend/app/api/maps/[id]/route.ts`):
- `GET /api/maps/:id` - Returns map data with cells
- Response includes adjacent map IDs for preloading

**Seed Script** (`gofus-backend/scripts/seed-maps.ts`):
- Seeds 5 connected test maps:
  - Map 7411 (Center) - Starting zone
  - Map 7410 (Left) - Forest area
  - Map 7412 (Right) - Plains
  - Map 7339 (Top) - Mountains
  - Map 7340 (Bottom) - Village

**Tasks:**
- [ ] Create database schema file
- [ ] Run migration to create maps table
- [ ] Create MapService class with caching
- [ ] Create API endpoint with proper error handling
- [ ] Create and run seed script for 5 test maps
- [ ] Test API with curl/Postman

---

### Phase 2: Unity Integration (2-3 days)

**MapRenderer Enhancement** (`gofus-client/Assets/_Project/Scripts/Map/MapRenderer.cs`):

Already exists with:
- `LoadMap(MapData)` - Main loading method
- `CreateTestMap(mapId)` - Test map generation
- `GenerateMapVisuals()` - Cell rendering
- Complete isometric calculations

**New Methods Needed:**
```csharp
public async Task LoadMapFromServer(int mapId)
{
    string url = $"{APIConfig.BaseURL}/api/maps/{mapId}";
    
    using (UnityWebRequest request = UnityWebRequest.Get(url))
    {
        await request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            MapData mapData = JsonUtility.FromJson<MapData>(request.downloadHandler.text);
            LoadMap(mapData);
        }
    }
}
```

**GameHUD Integration** (`gofus-client/Assets/_Project/Scripts/UI/Screens/GameHUD.cs`):

Add to Initialize():
```csharp
private void SetupMapRenderer()
{
    GameObject mapObj = new GameObject("MapRenderer");
    mapObj.transform.SetParent(transform, false);
    
    RectTransform mapRect = mapObj.AddComponent<RectTransform>();
    mapRect.anchorMin = new Vector2(0.1f, 0.15f);
    mapRect.anchorMax = new Vector2(0.9f, 0.85f);
    
    mapRenderer = mapObj.AddComponent<MapRenderer>();
    mapRenderer.Initialize();
    
    // Load map from character data
    if (CurrentMapId > 0)
    {
        await mapRenderer.LoadMapFromServer(CurrentMapId);
        
        // Position character on map
        SetCharacterCell(CurrentCellId);
    }
}
```

**Character Positioning:**
```csharp
private void SetCharacterCell(int cellId)
{
    if (characterSprite == null)
    {
        characterSprite = new GameObject("PlayerCharacter");
        characterSprite.transform.SetParent(mapRenderer.transform);
        
        // Add CharacterLayerRenderer component
        var renderer = characterSprite.AddComponent<CharacterLayerRenderer>();
        renderer.SetClass(characterClassId, isMale);
    }
    
    // Position at cell
    Vector3 cellPos = IsometricHelper.CellIdToWorldPosition(cellId);
    characterSprite.transform.position = cellPos;
}
```

**Camera Setup:**
```csharp
private void SetupCamera()
{
    Camera.main.orthographic = true;
    Camera.main.orthographicSize = 5f;
    
    // Center on character
    if (characterSprite != null)
    {
        Vector3 camPos = characterSprite.transform.position;
        camPos.z = -10f;
        Camera.main.transform.position = camPos;
    }
}
```

**Tasks:**
- [ ] Add MapRenderer to GameHUD.Initialize()
- [ ] Implement SetupMapRenderer() method
- [ ] Implement SetupCamera() method
- [ ] Implement SetCharacterCell() method
- [ ] Modify CharacterSelectionScreen to pass cellId
- [ ] Update MapRenderer.LoadMapFromServer() to use API
- [ ] Test map loading in Play mode

---

### Phase 3: Map Transitions (1-2 days)

**Edge Detection** (Already in GameHUD):
```csharp
private void Update()
{
    if (characterSprite != null)
    {
        CheckMapEdgeProximity();
    }
}

private void CheckMapEdgeProximity()
{
    Vector3 charPos = characterSprite.transform.position;
    int cellId = IsometricHelper.WorldPositionToCellId(charPos);
    
    MapEdge? edge = IsometricHelper.GetEdgeProximity(cellId, EDGE_DETECTION_DISTANCE);
    
    if (edge.HasValue && !PreloadingMapId.HasValue)
    {
        // Trigger map transition
        int? adjacentMapId = mapRenderer.GetAdjacentMapId(edge.Value);
        
        if (adjacentMapId.HasValue)
        {
            HandleMapTransition(adjacentMapId.Value, edge.Value);
        }
    }
}
```

**Transition Handler:**
```csharp
private async void HandleMapTransition(int newMapId, MapEdge fromEdge)
{
    Debug.Log($"Transitioning to map {newMapId} from {fromEdge} edge");
    
    // Save current position
    int oldCellId = IsometricHelper.WorldPositionToCellId(characterSprite.transform.position);
    
    // Load new map
    await mapRenderer.LoadMapFromServer(newMapId);
    
    // Position character at opposite edge
    int newCellId = GetOppositeSideCell(fromEdge);
    SetCharacterCell(newCellId);
    
    // Update current map ID
    CurrentMapId = newMapId;
    
    // Notify backend of map change
    await UpdateCharacterMapPosition(newMapId, newCellId);
    
    OnMapTransition?.Invoke(newMapId, newCellId);
}

private int GetOppositeSideCell(MapEdge fromEdge)
{
    switch (fromEdge)
    {
        case MapEdge.Top: return IsometricHelper.GetBottomCell();
        case MapEdge.Bottom: return IsometricHelper.GetTopCell();
        case MapEdge.Left: return IsometricHelper.GetRightCell();
        case MapEdge.Right: return IsometricHelper.GetLeftCell();
        default: return 0;
    }
}
```

**Tasks:**
- [ ] Implement HandleMapTransition() method
- [ ] Implement GetOppositeSideCell() helper
- [ ] Test edge detection in all 4 directions
- [ ] Verify character positioning after transition
- [ ] Test seamless transitions between all 5 maps

---

### Phase 4: Testing & Documentation (1-2 days)

**Backend Tests:**
```bash
# Test API endpoint
curl https://gofus-backend.vercel.app/api/maps/7411

# Verify response structure
{
  "id": 7411,
  "name": "Astrub Center",
  "width": 14,
  "height": 20,
  "cells": [...560 cells...],
  "adjacentMaps": {
    "top": 7339,
    "bottom": 7340,
    "left": 7410,
    "right": 7412
  }
}
```

**Unity Integration Tests:**
- [ ] Map loads from API successfully
- [ ] Character spawns at correct cell
- [ ] Camera centers on character
- [ ] All 560 cells render correctly
- [ ] Cell highlighting works on hover
- [ ] No performance issues

**Full Flow Test:**
- [ ] Login → Character Selection → Play
- [ ] GameHUD shows with map rendered
- [ ] Character appears at saved position
- [ ] Can move to map edges
- [ ] Map transitions work in all directions
- [ ] Character position persists
- [ ] Combat mode can be toggled
- [ ] Map state preserved across modes

**Documentation Updates:**
- [ ] Update NEXT_IMPLEMENTATION_SCREENS.md ✅
- [ ] Update PROJECT_MASTER_DOC.md ✅
- [ ] Create MAP_SYSTEM_IMPLEMENTATION.md ✅
- [ ] Update CURRENT_STATE.md with map status

---

### Test Map Layout

```
        [7339]
     Mountains
          |
   [7410]-[7411]-[7412]
   Forest Center Plains
          |
        [7340]
       Village
```

---

### Camera Setup



## ✅ COMPLETED: Movement System + Sprite Positioning Fix

### Status: ✅ COMPLETE (November 20-21, 2025)
### Implementation Time: Already existed + 5 minutes for sprite fix
### Complexity: High - DONE

### Overview
Allows player to click on map cells and move their character with pathfinding.

### Sprite Positioning Fix (November 21, 2025)

**Problem Solved**: Characters were only showing their face/head, with the body cut off below cell level.

**Root Cause**:
- `PlayerController.cs` had `SPRITE_VERTICAL_OFFSET = 0f`
- Character sprite positioned directly at cell center
- Only top portion visible above the cell

**Solution Applied**:
```csharp
// PlayerController.cs (Line ~58)
private const float SPRITE_VERTICAL_OFFSET = 1.0f; // Changed from 0f

// SetPosition() already uses this offset:
transform.position = IsometricHelper.CellIdToWorldPosition(cellId) + Vector3.up * SPRITE_VERTICAL_OFFSET;

// FollowPath() also uses it:
Vector3 targetPos = IsometricHelper.CellIdToWorldPosition(targetCell) + Vector3.up * SPRITE_VERTICAL_OFFSET;
```

**Results**:
- ✅ Full character sprite now visible
- ✅ Character feet positioned appropriately above cell
- ✅ Character body extends upward from feet
- ✅ Sprite maintains proper position during movement
- ✅ All 8 direction animations work correctly

**Files Modified**:
- `PlayerController.cs`: Changed SPRITE_VERTICAL_OFFSET constant
- `CharacterLayerRenderer.cs`: Already optimized with DestroyImmediate()

**Testing**:
- Click on cells in all directions
- Verify full sprite visible throughout movement
- Confirm no sprite overlap during animation changes

---

### Components Needed

1. **Click Handler**: Detect clicks on cells
2. **Pathfinding**: A* algorithm for optimal path
3. **Movement Controller**: Move character along path
4. **Animation Controller**: Change animations during movement

---

### Implementation

**File**: `Assets/_Project/Scripts/Movement/PlayerMovement.cs`

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GOFUS.Map;
using GOFUS.Rendering;

namespace GOFUS.Movement
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        public MapRenderer mapRenderer;
        public CharacterLayerRenderer character;

        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public bool isMoving = false;

        private int currentCell;
        private Queue<int> currentPath;
        private Pathfinder pathfinder;

        private void Awake()
        {
            pathfinder = new Pathfinder(mapRenderer);
            currentPath = new Queue<int>();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) && !isMoving)
            {
                HandleCellClick();
            }

            if (isMoving)
            {
                MoveAlongPath();
            }
        }

        private void HandleCellClick()
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int targetCell = mapRenderer.WorldPositionToCell(mousePos);

            if (targetCell < 0 || targetCell >= 280) return;
            if (!mapRenderer.IsCellWalkable(targetCell)) return;

            Debug.Log($"[Movement] Clicked cell {targetCell}");

            // Calculate path
            List<int> path = pathfinder.FindPath(currentCell, targetCell);

            if (path != null && path.Count > 0)
            {
                currentPath.Clear();
                foreach (int cell in path)
                {
                    currentPath.Enqueue(cell);
                }

                isMoving = true;
            }
        }

        private void MoveAlongPath()
        {
            if (currentPath.Count == 0)
            {
                isMoving = false;
                character.SetAnimation("staticS");
                return;
            }

            int nextCell = currentPath.Peek();
            Vector3 targetPos = mapRenderer.CellToWorldPosition(nextCell);

            // Move towards target
            transform.position = Vector3.MoveTowards(transform.position,
                targetPos, moveSpeed * Time.deltaTime);

            // Check if reached
            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
            {
                currentPath.Dequeue();
                currentCell = nextCell;

                // Update animation based on direction
                if (currentPath.Count > 0)
                {
                    int next = currentPath.Peek();
                    UpdateAnimationDirection(currentCell, next);
                }
            }
            else
            {
                // Play walk animation
                if (character.CurrentAnimation != "walkS")
                {
                    character.SetAnimation("walkS");
                }
            }
        }

        private void UpdateAnimationDirection(int from, int to)
        {
            Vector2Int fromCoords = mapRenderer.GetCellCoords(from);
            Vector2Int toCoords = mapRenderer.GetCellCoords(to);

            int dx = toCoords.x - fromCoords.x;
            int dy = toCoords.y - fromCoords.y;

            // Determine direction
            if (dy > 0) character.SetAnimation("walkS"); // South
            else if (dy < 0) character.SetAnimation("walkB"); // Back
            else if (dx > 0) character.SetAnimation("walkR"); // Right
            else if (dx < 0) character.SetAnimation("walkL"); // Left
        }

        public void SetStartCell(int cellId)
        {
            currentCell = cellId;
            Vector3 pos = mapRenderer.CellToWorldPosition(cellId);
            transform.position = pos;
        }
    }
}
```

---

### Pathfinding (A* Algorithm)

**File**: `Assets/_Project/Scripts/Movement/Pathfinder.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;
using GOFUS.Map;

namespace GOFUS.Movement
{
    public class Pathfinder
    {
        private MapRenderer mapRenderer;

        public Pathfinder(MapRenderer map)
        {
            mapRenderer = map;
        }

        public List<int> FindPath(int start, int end)
        {
            // A* pathfinding implementation
            HashSet<int> closedSet = new HashSet<int>();
            HashSet<int> openSet = new HashSet<int>() { start };
            Dictionary<int, int> cameFrom = new Dictionary<int, int>();
            Dictionary<int, float> gScore = new Dictionary<int, float>();
            Dictionary<int, float> fScore = new Dictionary<int, float>();

            gScore[start] = 0;
            fScore[start] = Heuristic(start, end);

            while (openSet.Count > 0)
            {
                int current = GetLowestFScore(openSet, fScore);

                if (current == end)
                {
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (int neighbor in GetNeighbors(current))
                {
                    if (closedSet.Contains(neighbor)) continue;
                    if (!mapRenderer.IsCellWalkable(neighbor)) continue;

                    float tentativeGScore = gScore[current] + 1;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                    else if (tentativeGScore >= gScore.GetValueOrDefault(neighbor, float.MaxValue))
                    {
                        continue;
                    }

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, end);
                }
            }

            return null; // No path found
        }

        private List<int> GetNeighbors(int cellId)
        {
            List<int> neighbors = new List<int>();
            Vector2Int coords = mapRenderer.GetCellCoords(cellId);

            // 4-directional (up, down, left, right)
            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int newX = coords.x + dx[i];
                int newY = coords.y + dy[i];

                if (newX >= 0 && newX < 14 && newY >= 0 && newY < 20)
                {
                    neighbors.Add(newY * 14 + newX);
                }
            }

            return neighbors;
        }

        private float Heuristic(int a, int b)
        {
            Vector2Int coordsA = mapRenderer.GetCellCoords(a);
            Vector2Int coordsB = mapRenderer.GetCellCoords(b);

            // Manhattan distance
            return Mathf.Abs(coordsA.x - coordsB.x) + Mathf.Abs(coordsA.y - coordsB.y);
        }

        private int GetLowestFScore(HashSet<int> openSet, Dictionary<int, float> fScore)
        {
            float lowestScore = float.MaxValue;
            int lowestCell = -1;

            foreach (int cell in openSet)
            {
                float score = fScore.GetValueOrDefault(cell, float.MaxValue);
                if (score < lowestScore)
                {
                    lowestScore = score;
                    lowestCell = cell;
                }
            }

            return lowestCell;
        }

        private List<int> ReconstructPath(Dictionary<int, int> cameFrom, int current)
        {
            List<int> path = new List<int>() { current };

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }

            return path;
        }
    }
}
```

---

---

## 🔴 NEXT PRIORITY: WebSocket Client & Multiplayer

### Screen 4: WebSocket Integration

### Priority: 🔴 CRITICAL - Implement FIRST
### Estimated Time: 1-2 days
### Complexity: Medium

### Overview
Real-time bidirectional communication with game server for multiplayer synchronization.

### Implementation Steps

**Step 1: Install Socket.IO Client for Unity** (30 minutes)
- Add Socket.IO client package (Best.SocketIO or similar)
- Configure package settings

**Step 2: Create WebSocketManager.cs** (2 hours)
```csharp
using UnityEngine;
using SocketIOClient;
using System;
using System.Collections.Generic;

namespace GOFUS.Network
{
    public class WebSocketManager : MonoBehaviour
    {
        private static WebSocketManager instance;
        public static WebSocketManager Instance => instance;

        private SocketIO socket;
        private bool isConnected = false;

        [Header("Connection")]
        [SerializeField] private string serverUrl = "wss://gofus-game-server-production.up.railway.app";

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string, object> OnMessage;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async void Connect(string jwt, int characterId)
        {
            socket = new SocketIO(serverUrl);

            // Setup event handlers
            socket.OnConnected += OnSocketConnected;
            socket.OnDisconnected += OnSocketDisconnected;

            // Authentication
            socket.On("authenticated", (response) => {
                Debug.Log("[WebSocket] Authenticated successfully");
                OnConnected?.Invoke();
            });

            // Player movement
            socket.On("player:moved", (data) => {
                OnMessage?.Invoke("player:moved", data);
            });

            // Other players
            socket.On("player:spawned", (data) => {
                OnMessage?.Invoke("player:spawned", data);
            });

            socket.On("player:despawned", (data) => {
                OnMessage?.Invoke("player:despawned", data);
            });

            await socket.ConnectAsync();

            // Send authentication
            await socket.EmitAsync("authenticate", new { token = jwt, characterId });
        }

        public async void SendMovement(int cellId)
        {
            if (!isConnected) return;
            await socket.EmitAsync("player:move", new { cellId });
        }

        private void OnSocketConnected()
        {
            isConnected = true;
            Debug.Log("[WebSocket] Connected to game server");
        }

        private void OnSocketDisconnected()
        {
            isConnected = false;
            Debug.Log("[WebSocket] Disconnected from game server");
            OnDisconnected?.Invoke();
        }

        private void OnDestroy()
        {
            if (socket != null)
            {
                socket.Disconnect();
            }
        }
    }
}
```

**Step 3: Integrate with GameHUD** (1 hour)
```csharp
// In GameHUD.Initialize()
private void ConnectToGameServer()
{
    string jwt = PlayerPrefs.GetString("jwt_token", "");
    int characterId = PlayerPrefs.GetInt("selected_character_id", -1);

    if (WebSocketManager.Instance != null)
    {
        WebSocketManager.Instance.OnConnected += OnServerConnected;
        WebSocketManager.Instance.OnMessage += OnServerMessage;
        WebSocketManager.Instance.Connect(jwt, characterId);
    }
}

private void OnServerConnected()
{
    Debug.Log("[GameHUD] Connected to game server");
    ShowNotification("Connected to server", NotificationType.Success);
}

private void OnServerMessage(string eventName, object data)
{
    switch (eventName)
    {
        case "player:moved":
            HandlePlayerMovement(data);
            break;
        case "player:spawned":
            HandlePlayerSpawned(data);
            break;
        case "player:despawned":
            HandlePlayerDespawned(data);
            break;
    }
}
```

**Step 4: Sync Player Movement** (2 hours)
- Update PlayerController to broadcast movement
- Receive and apply other players' movement

**Success Criteria:**
- ✅ Connect to game server on GameHUD load
- ✅ Authenticate with JWT
- ✅ Send movement updates
- ✅ Receive other players' positions
- ✅ Graceful reconnection on disconnect

---

### Screen 5: Other Players Rendering

### Priority: 🔴 HIGH - Implement AFTER WebSocket
### Estimated Time: 1 day
### Complexity: Medium

### Overview
Display other players in the game world and synchronize their positions.

### Implementation Steps

**Step 1: Create RemotePlayerController.cs** (1 hour)
```csharp
public class RemotePlayerController : MonoBehaviour
{
    public int PlayerId { get; private set; }
    public string PlayerName { get; private set; }
    private CharacterLayerRenderer characterRenderer;
    private int currentCellId;

    public void Initialize(int playerId, string playerName, int classId, bool isMale, int cellId)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        currentCellId = cellId;

        // Create character renderer
        characterRenderer = ClassSpriteManager.Instance.CreateCharacterRenderer(
            classId,
            isMale,
            transform,
            IsometricHelper.CellIdToWorldPosition(cellId)
        );

        // Add name tag
        CreateNameTag(playerName);
    }

    public void MoveTo(int targetCellId)
    {
        // Smooth movement to new cell
        StartCoroutine(SmoothMoveTo(targetCellId));
    }

    private IEnumerator SmoothMoveTo(int targetCellId)
    {
        Vector3 targetPos = IsometricHelper.CellIdToWorldPosition(targetCellId) + Vector3.up;
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, 5f * Time.deltaTime);
            yield return null;
        }
        currentCellId = targetCellId;
    }
}
```

**Step 2: Add Remote Player Manager to GameHUD** (2 hours)
- Track all remote players
- Spawn when player:spawned received
- Update when player:moved received
- Despawn when player:despawned received

**Success Criteria:**
- ✅ Other players appear in game world
- ✅ Positions update smoothly
- ✅ Name tags display correctly
- ✅ Players despawn when they disconnect

---

## 🟡 MEDIUM PRIORITY: UI Features

### Screen 6: Inventory UI

### Priority: 🟡 MEDIUM - Implement AFTER multiplayer
### Estimated Time: 2 days
### Complexity: Medium

*(Implementation details for Inventory, Chat, Combat UI...)*

---

### Screen 7: Chat UI

### Priority: 🟡 MEDIUM
### Estimated Time: 1-2 days
### Complexity: Low-Medium

*(Implementation details...)*

---

### Screen 8: Combat UI

### Priority: 🟡 MEDIUM
### Estimated Time: 3-4 days
### Complexity: High

*(Implementation details...)*

---

## Technical Architecture

### Layer Organization

```
Sorting Layers (bottom to top):
1. Background (-10) - Map backgrounds
2. Ground (0) - Ground tiles
3. Objects (10) - Decorations, obstacles
4. Characters (20) - Player and NPCs
5. Effects (30) - Spell effects, damage numbers
6. UI (100) - HUD elements
7. Overlay (200) - Modal dialogs, tooltips
```

### Scene Structure

```
GameScene
├── UIManager (DontDestroyOnLoad)
│   ├── MainCanvas
│   │   ├── GameHUD
│   │   ├── InventoryPanel
│   │   ├── ChatPanel
│   │   └── ...
│   └── OverlayCanvas
│
├── GameWorld
│   ├── Map
│   │   ├── MapRenderer
│   │   └── GridOverlay
│   ├── Characters
│   │   ├── PlayerCharacter
│   │   └── OtherPlayers
│   └── Effects
│
├── Main Camera
│   └── GameCamera (follow script)
│
└── Managers
    ├── WebSocketManager
    ├── GameStateManager
    └── InputManager
```

---

## Implementation Order

### Week 1: Core Game World
1. **Day 1-2**: GameHUD screen with full UI layout
2. **Day 2-3**: Map rendering system with grid
3. **Day 3-4**: Camera system with follow/zoom
4. **Day 4-5**: Basic movement (click-to-move)
5. **Day 5**: Integration testing and polish

### Week 2: Multiplayer & Features
1. **Day 1-2**: WebSocket integration
2. **Day 2-3**: Other players spawning/movement
3. **Day 3**: Inventory UI
4. **Day 4**: Chat UI
5. **Day 5**: Combat UI (basic)

---

## Testing Strategy

### Unit Tests
- Grid coordinate conversion
- Pathfinding algorithm
- Cell walkability checks

### Integration Tests
- Character Selection → GameHUD flow
- Movement system end-to-end
- WebSocket connection/reconnection
- Multi-player synchronization

### Manual Tests
- Click movement on all cells
- Camera follow smoothness
- UI element positioning at different resolutions
- Performance with multiple characters

---

## Success Criteria

### GameHUD
- ✅ Displays after "Play" clicked
- ✅ Character info updates correctly
- ✅ Health/mana bars functional
- ✅ Menu buttons responsive

### Movement
- ✅ Click on cell moves character
- ✅ Pathfinding avoids obstacles
- ✅ Animation changes with direction
- ✅ Smooth movement at 60 FPS

### Multiplayer
- ✅ Other players visible
- ✅ Positions sync under 100ms
- ✅ Handles disconnects gracefully

---

**END OF IMPLEMENTATION GUIDE**

*This document provides comprehensive specifications for implementing the next critical screens in GOFUS. Start with GameHUD, then Map System, then Movement. Each section includes code templates, architecture diagrams, and testing procedures.*

**Last Updated**: November 18, 2025
**Version**: 1.0
