# Map System Comprehensive Analysis - November 19, 2024

## Executive Summary

Complete analysis of the map system across all GOFUS components (Cliente retro, Java source, backend, Unity). Identified a practical MVP implementation path using test maps instead of complex SWF extraction.

## Current State

### What EXISTS
1. ✅ **Unity MapRenderer** - Complete isometric grid system (14x20, 560 cells)
2. ✅ **Unity IsometricHelper** - Grid calculations, cell positioning, pathfinding helpers
3. ✅ **Unity GameHUD** - UI framework with map integration hooks
4. ✅ **Database Schema** - `mapas` table in bustar_estaticos.sql
5. ✅ **Java Implementation** - Complete Mapa.java and Celda.java source code
6. ✅ **SWF Map Files** - 14,000+ map files in Cliente retro/data/maps

### What DOES NOT EXIST
1. ❌ **Backend Map API** - No /api/maps endpoint
2. ❌ **Map Service** - No backend map service layer
3. ❌ **Map Data in Unity** - No extracted map graphics
4. ❌ **Database Seeding** - Maps not in PostgreSQL/Supabase
5. ❌ **Map Conversion** - No SWF to JSON converter

## Map Data Structure Analysis

### Database Schema (MySQL/PostgreSQL)
```sql
mapas table:
- id INT PRIMARY KEY (e.g., 7411)
- fecha VARCHAR (version/date)
- ancho TINYINT (width, typically 15)
- alto TINYINT (height, typically 17)
- mapData TEXT (encrypted cell data - base64 encoded)
- key VARCHAR (decryption key)
- posPelea TEXT (fight positions: "a-bm...")
- X SMALLINT (world X coordinate)
- Y SMALLINT (world Y coordinate)
- subArea SMALLINT (region ID)
- bgID SMALLINT (background graphics ID)
- musicID SMALLINT (background music ID)
- ambienteID SMALLINT (ambient sound ID)
- outDoor TINYINT (indoor/outdoor flag)
- maxGrupoMobs TINYINT (max mob groups)
- maxMobsPorGrupo TINYINT (max mobs per group)
- minNivelGrupoMob INT (min mob level)
- maxNivelGrupoMob INT (max mob level)
```

### Java Source Code Structure

**Mapa.java:**
- Decrypts mapData using Encriptador.decifrarMapData(key, mapData)
- Parses into Map<Short, Celda> (560 cells)
- Stores NPCs, mob groups, interactive objects
- Manages fights and player lists
- Handles map transitions

**Celda.java:**
- celdaID: 0-559 (cell index)
- activo: boolean (walkable)
- movimiento: byte (movement cost 1-10)
- level: byte (height/elevation -7 to 7)
- slope: byte (incline)
- lineaDeVista: boolean (line of sight)
- coordX, coordY: byte (grid position)
- objetoInterac: ObjetoInteractivo (interactive object)

### Unity Current Implementation

**MapRenderer.cs (Existing):**
- CellGrid system with 560 cells
- Isometric rendering
- Cell highlighting
- Movement range display
- Path visualization
- Interactive cell clicking
- Test map generation: CreateTestMap(mapId)

**IsometricHelper.cs (Existing):**
- Grid constants: 14x20 (560 total cells)
- Cell dimensions: 86x43 pixels
- CellIdToWorldPosition() - Convert cell ID to screen position
- WorldPositionToCellId() - Reverse conversion
- GetNeighborCells() - 8-directional neighbors
- GetDistance() - Manhattan distance
- Area of effect calculations

## Recommended MVP Implementation

### Strategy: Test Maps First, Real Maps Later

**Why This Approach:**
1. Gets game playable quickly (5-7 days vs months)
2. Tests all systems without complex data extraction
3. Avoids SWF decryption/parsing complexity
4. Can replace with real maps later without code changes
5. Focuses on gameplay, not data conversion

### 5 Connected Test Maps

**Map Layout:**
```
        7339 (North)
          |
7410 - 7411 - 7412
(West) (Center)(East)
          |
        7340 (South)
```

**Map IDs Chosen:**
- 7411: Astrub Village Center (starting map)
- 7410/7412: Adjacent streets
- 7339/7340: Connected areas

**Each Map Contains:**
- 560 cells (14x20 grid)
- Mix of walkable/obstacle cells
- Adjacent map references
- Spawn points
- Fight positions

## Implementation Architecture

### Backend Stack

**Database Schema (PostgreSQL/Supabase):**
```typescript
maps: {
  id: number (7411, 7410, 7412, 7339, 7340)
  name: string ("Astrub Village", etc.)
  width: number (14)
  height: number (20)
  cells: jsonb (array of 560 cell objects)
  adjacentMaps: jsonb {
    top?: number,
    bottom?: number,
    left?: number,
    right?: number
  }
  backgroundMusic: string
  subArea: number
  createdAt: timestamp
}
```

**Cell Data Format:**
```typescript
{
  id: number (0-559)
  walkable: boolean
  movementCost: number (1-10)
  type: 'normal' | 'obstacle' | 'water' | 'lava'
  level: number (-7 to 7, height)
}
```

**API Endpoint:**
```
GET /api/maps/[id]

Response:
{
  "id": 7411,
  "name": "Astrub Village",
  "width": 14,
  "height": 20,
  "cells": [...560 cells...],
  "adjacentMaps": {
    "top": 7339,
    "bottom": 7340,
    "left": 7410,
    "right": 7412
  },
  "backgroundMusic": "astrub_theme",
  "subArea": 10
}
```

### Unity Integration

**GameHUD Modifications:**
```csharp
// Add MapRenderer
private MapRenderer mapRenderer;
private GameObject characterSprite;
private int currentCellId;

void SetupMapRenderer() {
    GameObject mapObj = new GameObject("MapRenderer");
    mapRenderer = mapObj.AddComponent<MapRenderer>();
    mapRenderer.transform.SetParent(transform, false);
}

void SetCurrentMapId(int mapId) {
    CurrentMapId = mapId;
    mapRenderer.LoadMapFromServer(mapId);
}

void SetCharacterCell(int cellId) {
    currentCellId = cellId;
    Vector3 pos = IsometricHelper.CellIdToWorldPosition(cellId);
    characterSprite.transform.position = pos;
}
```

**CharacterSelectionScreen Modifications:**
```csharp
// Pass both mapId AND cellId
gameHUD.SetCurrentMapId(selectedChar.MapId);
gameHUD.SetCharacterCell(selectedChar.CellId); // NEW
```

## Map Transitions System

### Edge Detection
GameHUD already implements:
```csharp
bool IsNearMapEdge(MapEdge edge) {
    // Checks if player within 10 units of edge
}

void CheckMapTransition() {
    // Triggered when player crosses boundary
    // Loads adjacent map
    // Repositions character on opposite edge
}
```

### Seamless Transition Flow
1. Player moves toward map edge
2. `IsNearMapEdge()` returns true
3. Preload adjacent map data
4. Player crosses edge
5. `TriggerMapTransition(adjacentMapId)` fires
6. Load new map cells
7. Position character at opposite edge
8. Update camera position
9. Transition complete (no loading screen!)

## Combat Mode Integration

### Combat Mode Switching
From GameHUD.cs:
```csharp
enum CombatMode {
    TurnBased,
    RealTime,
    Transitioning
}

void SetCombatMode(CombatMode mode) {
    CurrentCombatMode = mode;
    // Show/hide combat UI
    // Enable/disable turn-based controls
}
```

### Battle on Map
- Battles occur ON the map (not separate screen)
- MapRenderer highlights fight cells
- Character sprites remain visible
- Turn-based movement uses same grid
- Combat UI overlays map

## Implementation Phases

### Phase 1: Backend Foundation (Day 1-2)
1. Create maps table schema (Drizzle ORM)
2. Create MapService with CRUD operations
3. Create /api/maps/[id] endpoint
4. Seed 5 test maps with cell data
5. Test API responses

### Phase 2: Unity Integration (Day 2-3)
1. Add MapRenderer to GameHUD
2. Implement map loading from API
3. Parse JSON cell data
4. Render 560 cells
5. Setup camera positioning
6. Add character sprite placement

### Phase 3: Character Integration (Day 3-4)
1. Pass cellId from character selection
2. Position character at spawn point
3. Test character visibility on map
4. Implement camera follow
5. Basic input handling

### Phase 4: Map Transitions (Day 4-5)
1. Implement edge detection
2. Preload adjacent maps
3. Transition logic
4. Character repositioning
5. State persistence

### Phase 5: Testing & Polish (Day 5-7)
1. Test all map transitions
2. Verify combat mode switching
3. Performance optimization
4. Bug fixes
5. Documentation

## Testing Strategy

### Backend Tests
- Map API returns valid JSON
- Cell count is always 560
- Adjacent maps exist
- Error handling for missing maps

### Unity Tests
- Map loads from API
- 560 cells render correctly
- Character spawns at cellId
- Transitions work in all 4 directions
- Combat mode doesn't break map

### Integration Tests
- Login → Character Selection → GameHUD → Map
- Move between all 5 maps
- Return to previous map preserves state
- Multiple transitions in sequence

## Performance Considerations

### Optimization Strategies
1. **Map Caching** - Store loaded maps, don't reload
2. **Cell Pooling** - Reuse cell GameObjects
3. **Lazy Loading** - Load adjacent maps only when needed
4. **Memory Management** - Unload distant maps
5. **Sprite Atlasing** - Combine cell sprites

### Expected Performance
- Initial map load: ~200-500ms
- Transition time: <100ms (map cached)
- Memory per map: ~5-10MB
- Total memory (5 maps): ~25-50MB
- FPS impact: Minimal (<5%)

## Future Enhancements

### Phase 2: Real Dofus Maps
1. SWF extraction tool
2. MapData decryption (using keys from DB)
3. Cell parsing (Encriptador.decompilarMapaData)
4. Graphics extraction
5. Database conversion script

### Phase 3: Advanced Features
1. Interactive objects
2. Mob spawning
3. NPCs
4. Teleporters
5. Environmental effects (water, lava)
6. Dynamic lighting

## Files to Create/Modify

### Backend (New)
- `lib/db/schema/maps.ts`
- `lib/services/map/map.service.ts`
- `lib/services/map/map.types.ts`
- `app/api/maps/[id]/route.ts`
- `scripts/seed-maps.ts`

### Unity (Modify)
- `GameHUD.cs` - Add MapRenderer integration
- `CharacterSelectionScreen.cs` - Pass cellId
- `MapRenderer.cs` - Minor tweaks for API loading

### Documentation (New)
- `MAP_SYSTEM_IMPLEMENTATION.md`
- `MAP_RENDERING_GUIDE.md`
- `DATABASE_SCHEMA_MAPS.md`

## Success Criteria

**MVP Complete When:**
✅ 5 maps in database
✅ Backend API working
✅ Maps render in Unity
✅ Character spawns correctly
✅ Camera positioned properly
✅ Transitions work between all maps
✅ Combat mode toggle functional
✅ No major bugs
✅ Documentation complete

**Timeline:** 5-7 days focused development

---

**Analysis Complete:** November 19, 2024
**Next Step:** Create implementation documentation and begin Phase 1
