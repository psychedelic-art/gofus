# GOFUS Testing Guide - Movement System

**Last Updated**: November 20, 2025
**Version**: 1.0
**Status**: Ready for Testing

---

## Overview

This guide provides step-by-step instructions for testing the complete movement system implementation, including Unity client movement, game server validation, and real-time synchronization.

---

## 🎯 What We're Testing

1. **Unity Client Movement**:
   - Character sprite rendering at correct size
   - Cell click detection
   - A* pathfinding
   - Character movement animation
   - Camera following

2. **Game Server**:
   - Map data loading (3-tier caching)
   - Movement validation (server-side)
   - Position broadcasting
   - Redis state persistence

3. **End-to-End Flow**:
   - Login → Character Selection → GameHUD
   - Click cell → Calculate path → Move character
   - Server validates → Broadcasts to others → Updates position

---

## 🔧 Prerequisites

### 1. Backend Server (REST API)
**Required**: Must be running for map data
- Production: `https://gofus-backend.vercel.app` (always available)
- Local: `http://localhost:3000` (if testing locally)

**Verify Backend**:
```bash
# Test backend health
curl https://gofus-backend.vercel.app/api/health

# Expected response:
# {"status":"ok","database":"connected","redis":"connected"}
```

### 2. Game Server (Real-Time)
**Location**: `gofus-game-server/`
**Required**: For real-time movement validation

**Start Game Server**:
```bash
cd gofus-game-server
npm install
npm run dev
```

**Expected Output**:
```
🚀 Game server starting...
✅ MapManager initialized with 5 maps cached
✅ MovementManager initialized with 20Hz update rate
✅ Game server running on port 3001
```

**Verify Game Server**:
- Check console for "Game server running" message
- Verify Redis connection (should show "connected")
- Check that MapManager preloaded 5 test maps

### 3. Unity Client
**Location**: `gofus-client/`
**Scene**: `Assets/_Project/Scenes/LoginScene.unity`

**Unity Settings**:
- Unity version: 2021.3+
- Build target: PC, Mac & Linux Standalone
- Scripting backend: Mono

---

## 📋 Test Cases

### Test 1: Character Sprite Size ✅

**Objective**: Verify character sprites are visible and properly sized (2.5x scale)

**Steps**:
1. Open Unity project
2. Open `LoginScene.unity`
3. Click Play
4. Complete login flow: Login → Select Character → Click "Play"
5. Wait for GameHUD to load

**Expected Results**:
- ✅ Character sprite appears on map
- ✅ Character is clearly visible (not too small)
- ✅ Character is roughly 1.5-2x the height of a cell
- ✅ Character has black outline for visibility
- ✅ Character renders ABOVE map cells (sorting order 100+)

**Debug Console Output**:
```
[GameHUD] Character renderer created successfully with 10-20 layers (scale: 2.5x)
[GameHUD] PlayerController added and initialized
[GameHUD] Character positioned at cell X
```

**Failure Modes**:
- Character too small: Check `GameHUD.cs` line 448 (should be `Vector3.one * 2.5f`)
- Character behind cells: Check sorting order (should be 100+)
- No character visible: Check CharacterLayerRenderer initialization

---

### Test 2: Cell Click Detection ✅

**Objective**: Verify clicking cells triggers event handlers

**Steps**:
1. In Unity Play mode with GameHUD loaded
2. Click on any cell in the map
3. Watch Unity Console for debug messages

**Expected Console Output**:
```
[CellClickHandler] Cell X clicked!
[MapRenderer] HandleCellClick called for cell X
[MapRenderer] OnCellClicked has 1 subscribers
[PlayerController] Cell clicked: X (current: Y)
[PlayerController] No GameManager found - allowing free movement
[PlayerController] RequestMove called: target=X, isMoving=false
[PlayerController] Calculating path from Y to X
[PlayerController] Path found with N cells: [cell1, cell2, ...]
[PlayerController] Starting FollowPath coroutine
```

**Expected Results**:
- ✅ Cell click detected immediately
- ✅ PlayerController receives event
- ✅ Path calculation starts
- ✅ No errors in console

**Failure Modes**:
- No click detected: Check PolygonCollider2D on cells
- Click detected but no path: Check MapRenderer event subscription
- Error messages: Check GameManager null handling

---

### Test 3: Character Movement Animation ✅

**Objective**: Verify character moves smoothly along calculated path

**Steps**:
1. In Unity Play mode with GameHUD loaded
2. Click a cell 5-10 cells away from current position
3. Watch character move

**Expected Behavior**:
- ✅ Character moves smoothly cell-by-cell
- ✅ Movement speed is visible but not too slow (~5 units/second)
- ✅ Character faces movement direction
- ✅ Camera follows character during movement
- ✅ Movement stops at target cell

**Expected Console Output**:
```
[PlayerController] Moving to cell X at position (x, y, z) (step 1/N)
[PlayerController] Animation direction: South (or other direction)
[PlayerController] Reached cell X in 0.25 seconds
[PlayerController] Moving to cell Y at position (x, y, z) (step 2/N)
...
[PlayerController] Path complete
```

**Expected Timing**:
- ~0.2-0.4 seconds per cell
- Total movement time: N cells × 0.3s average

**Failure Modes**:
- Character teleports: Check `moveSpeed` (should be 5f)
- Character stuck: Check walkability validation
- No movement: Check isMoving flag

---

### Test 4: Camera Follow Behavior ✅

**Objective**: Verify camera follows character and responds to controls

**Steps**:
1. In Unity Play mode with GameHUD loaded
2. Click cells to move character around
3. Try camera controls:
   - WASD keys (pan)
   - Mouse scroll (zoom)
   - Q/E keys (zoom)
   - Middle mouse drag (pan)
   - Space key (re-center on character)

**Expected Behavior**:
- ✅ Camera follows character by default
- ✅ Manual pan disables follow mode
- ✅ Zoom in/out works smoothly
- ✅ Space key re-centers and re-enables follow
- ✅ Camera stays within bounds

**Camera Settings**:
- Default zoom: 35 units
- Zoom range: 20-50 units
- Pan speed: Configurable
- Follow smoothing: 5f

**Failure Modes**:
- Camera doesn't follow: Check CameraController initialization
- Camera goes out of bounds: Check SetBounds() call
- Controls don't work: Check Input system

---

### Test 5: Map Data Loading (Client) ✅

**Objective**: Verify map loads from backend or falls back to test map

**Steps**:
1. In Unity Play mode
2. Complete login and character selection
3. Note the character's mapId (shown in character info)
4. Click "Play" to load GameHUD
5. Watch console for map loading messages

**Expected Console Output (Success)**:
```
[MapRenderer] Fetching map 7411 from: https://gofus-backend.vercel.app/api/maps/7411
[MapRenderer] Received response: {"success":true,"map":...}
[MapRenderer] API response success: true, cells: 560
[MapRenderer] Successfully converted map 7411 with 560 cells
[MapRenderer] Generating visuals for 560 cells
[MapRenderer] Created 560 cell visuals (540 walkable, 20 obstacles)
```

**Expected Console Output (Fallback)**:
```
[MapRenderer] Failed to load map 7411: Connection error
[MapRenderer] Falling back to test map for map 7411
[MapRenderer] Generating visuals for 560 cells
```

**Expected Results**:
- ✅ Map loads from backend if available
- ✅ Falls back to test map if backend unavailable
- ✅ All 560 cells are rendered
- ✅ Cells are clickable
- ✅ Walkable/unwalkable distinction visible

**Failure Modes**:
- No map loads: Check backend URL in NetworkManager
- Wrong number of cells: Check cell count validation
- Cells not clickable: Check PolygonCollider2D creation

---

### Test 6: Game Server Map Loading ✅

**Objective**: Verify game server loads maps with 3-tier caching

**Requirements**: Game server must be running

**Steps**:
1. Start game server: `npm run dev`
2. Watch server console during startup
3. Note preloaded maps

**Expected Console Output**:
```
[MapManager] MapManager initializing...
[MapManager] Fetching map 7411 from https://gofus-backend.vercel.app/api/maps/7411
[MapManager] Map 7411 loaded from API and cached (560 cells)
[MapManager] Preloaded map 7411
[MapManager] Preloaded map 7410
[MapManager] Preloaded map 7412
[MapManager] Preloaded map 7339
[MapManager] Preloaded map 7340
[MapManager] MapManager initialized with 5 maps cached
```

**Verify Caching**:
```bash
# Check Redis for cached map (requires redis-cli)
redis-cli GET map:7411

# Should return JSON map data
```

**Expected Results**:
- ✅ Game server fetches maps from backend on startup
- ✅ Maps cached in Redis with 5-minute TTL
- ✅ Maps cached in memory for fast access
- ✅ Fallback to test map if backend fails

**Failure Modes**:
- Maps don't load: Check BACKEND_URL env variable
- Redis errors: Check Redis connection
- All fallback maps: Check backend API availability

---

### Test 7: Server-Side Movement Validation ✅

**Objective**: Verify game server validates movement requests

**Requirements**:
- Game server running
- Unity client connected (WebSocket - future implementation)

**Current Status**: ⚠️ **Not testable yet** - Unity client doesn't send WebSocket messages yet

**When Implemented, Test**:
1. Unity client clicks cell
2. Client sends movement request via WebSocket
3. Game server validates path
4. Server broadcasts to other players
5. Server updates Redis

**Expected Server Console Output**:
```
[MovementManager] Movement request from character 123: 250 -> 280 (5 cells)
[MovementManager] Queued movement for character 123: 250 -> 280 (5 cells)
[MovementManager] Executing movement for character 123: 5 cells
[MovementManager] Character 123 moved to cell 280
```

**Validation Checks**:
- Cell bounds (0-559)
- Walkability
- Adjacency (8-directional)
- Movement cost

**Expected Redis Updates**:
```bash
# Check position in Redis
redis-cli GET character:123:position

# Should return: {"mapId":7411,"cellId":280,"timestamp":1234567890}
```

---

### Test 8: Path Validation (Edge Cases) ✅

**Objective**: Test pathfinding handles obstacles and invalid moves

**Steps**:
1. In Unity Play mode with GameHUD loaded
2. Try clicking:
   - Unwalkable cells (obstacles)
   - Cells very far away (20+ cells)
   - Cells behind walls
   - Same cell character is on

**Expected Behavior**:

**Clicking Obstacle**:
```
[PlayerController] Invalid move to cell X
```
- ✅ No movement occurs
- ✅ Error logged

**Clicking Far Cell**:
```
[PlayerController] Path found with 25 cells: [...]
[PlayerController] Starting FollowPath coroutine
```
- ✅ Long path calculated
- ✅ Character moves along entire path

**Clicking Behind Wall**:
```
[PlayerController] No path found to cell X
```
- ✅ No path exists message
- ✅ No movement occurs

**Clicking Current Cell**:
- ✅ Path contains just current cell
- ✅ Character doesn't move (already there)

---

### Test 9: Multiple Character Sprites ✅

**Objective**: Test different class/gender combinations

**Steps**:
1. Create characters of different classes:
   - Feca (1), Osamodas (2), Enutrof (3), Sram (4)
   - Xelor (5), Ecaflip (6), Eniripsa (7), Iop (8)
   - Cra (9), Sadida (10), Sacrieur (11), Pandawa (12)
2. Create both male and female versions
3. Enter game with each character
4. Verify sprite renders correctly

**Expected Results**:
- ✅ Each class has unique sprite appearance
- ✅ Male/female variants visible
- ✅ All sprites same size (2.5x scale)
- ✅ All sprites render above cells

**Known Issues**:
- ⚠️ Sprites may be white placeholders if assets not imported
- ⚠️ CharacterLayerRenderer uses Resources folder (may not work in builds)

---

## 🐛 Common Issues & Solutions

### Issue 1: Character Too Small
**Symptom**: Character barely visible compared to cells
**Solution**: Check `GameHUD.cs` line 448
```csharp
characterSprite.transform.localScale = Vector3.one * 2.5f; // Should be 2.5x
```

### Issue 2: Clicks Not Detected
**Symptom**: Clicking cells does nothing, no console output
**Solutions**:
1. Check camera has Physics2DRaycaster component
2. Verify PolygonCollider2D on cell GameObjects
3. Check EventSystem exists in scene
4. Verify cells are on correct layer (not UI layer)

### Issue 3: No Movement
**Symptom**: Click detected but character doesn't move
**Solutions**:
1. Check `isMoving` flag (might be stuck as true)
2. Verify MapRenderer event subscription in PlayerController.Initialize()
3. Check pathfinding returns valid path
4. Verify moveSpeed > 0 (should be 5f)

### Issue 4: Game Server Won't Start
**Symptom**: Server crashes or doesn't initialize
**Solutions**:
```bash
# Check Node version (need 16+)
node --version

# Reinstall dependencies
rm -rf node_modules
npm install

# Check Redis connection
redis-cli ping
# Should return: PONG
```

### Issue 5: Maps Don't Load
**Symptom**: Backend returns 404 or errors
**Solutions**:
1. Verify backend is running and accessible
2. Check mapId exists in database
3. Run map seeding script:
```bash
cd gofus-backend
npm run seed:maps
```

### Issue 6: Camera Stuck
**Symptom**: Camera doesn't follow or respond to input
**Solutions**:
1. Verify CameraController attached to Main Camera
2. Check follow target is set (should be character transform)
3. Press Space to reset follow mode
4. Check camera bounds are reasonable

---

## 📊 Performance Benchmarks

### Expected Performance (Unity Client)

**Map Rendering**:
- Initial load: < 1 second for 560 cells
- Memory usage: ~50MB for cell sprites
- Frame rate: 60 FPS+ with full map

**Movement**:
- Path calculation: < 10ms for paths up to 50 cells
- Movement update: Every frame (smooth interpolation)
- Cell arrival: ~250-400ms per cell

**Camera**:
- Follow smoothing: 60 FPS smooth
- Zoom: Instant response
- Pan: Smooth at 60 FPS

### Expected Performance (Game Server)

**Map Loading**:
- Memory cache: < 1ms lookup
- Redis cache: < 10ms lookup
- Backend API: 100-200ms fetch

**Movement Processing**:
- Update rate: 20Hz (50ms intervals)
- Path validation: < 5ms
- Broadcasting: < 10ms to 10 players

**Concurrent Players**:
- Single map: 100+ players
- Total server: 1000+ players
- Queue capacity: 100 movement requests

---

## ✅ Test Checklist

Use this checklist to track testing progress:

### Unity Client
- [ ] Test 1: Character sprite size verified
- [ ] Test 2: Cell clicks detected
- [ ] Test 3: Character moves smoothly
- [ ] Test 4: Camera follows character
- [ ] Test 5: Map loads from backend
- [ ] Test 8: Path validation edge cases
- [ ] Test 9: Multiple character classes

### Game Server
- [ ] Test 6: Maps load with caching
- [ ] Test 7: Movement validation works (when WebSocket implemented)

### Integration
- [ ] Login → Character Selection → GameHUD flow works
- [ ] Backend API accessible
- [ ] Redis connected
- [ ] No errors in console
- [ ] Performance meets benchmarks

---

## 📝 Test Results Template

Use this template to record test results:

```markdown
## Test Session: [Date]
**Tester**: [Name]
**Unity Version**: [Version]
**Build**: [Windows/Mac/Linux]

### Test 1: Character Sprite Size
- Status: ✅ PASS / ❌ FAIL
- Notes: [Any issues or observations]

### Test 2: Cell Click Detection
- Status: ✅ PASS / ❌ FAIL
- Notes: [Any issues or observations]

[Continue for all tests...]

### Issues Found
1. [Issue description]
   - Severity: Critical / High / Medium / Low
   - Steps to reproduce: [Steps]
   - Expected: [Expected behavior]
   - Actual: [Actual behavior]

### Performance
- FPS: [Average FPS]
- Map load time: [Time in seconds]
- Movement smoothness: [1-10 rating]

### Conclusion
- Overall status: ✅ Ready / ⚠️ Issues / ❌ Blocked
- Notes: [Summary]
```

---

## 🚀 Next Steps After Testing

Once all tests pass:

1. **Document Results**: Fill out test results template
2. **Fix Critical Issues**: Address any blocking bugs
3. **Optimize Performance**: Profile and improve bottlenecks
4. **Implement WebSocket**: Connect Unity client to game server
5. **Test Multiplayer**: Run multiple clients simultaneously
6. **Add Combat System**: Next major feature
7. **User Acceptance Testing**: Let players test

---

## 📞 Support

For issues or questions:
- Check `CURRENT_STATE.md` for implementation details
- Review `PROJECT_MASTER_DOC.md` for architecture
- Check code comments in:
  - `GameHUD.cs`
  - `PlayerController.cs`
  - `MapRenderer.cs`
  - `MapManager.ts`
  - `MovementManager.ts`

**Last Updated**: November 20, 2025
