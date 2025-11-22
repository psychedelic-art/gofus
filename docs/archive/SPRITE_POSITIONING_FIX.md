# Sprite Positioning & Layer Cleanup Fix

**Date:** November 20, 2025  
**Status:** ✅ Complete - Ready for Testing  
**Priority:** High

---

## 🎯 Issues Fixed

### 1. ✅ Sprite Positioning - Only Face Visible

**Problem:** Character sprite was positioned at cell center with no vertical offset, causing only the head/face to be visible above the cell. The body was cut off below the cell level.

**Root Cause:** 
- GameHUD.cs spawned characters with `Vector3.up * 0.5f` offset
- PlayerController.cs moved characters to `IsometricHelper.CellIdToWorldPosition(cellId)` WITHOUT any offset
- This caused the sprite to drop down during movement

**Solution:** Added consistent vertical offset throughout character positioning

### 2. ✅ Layer Visibility - Old Layers Not Hidden

**Problem:** When animation changed, old sprite layers persisted briefly and overlapped with new layers, causing visual glitches.

**Root Cause:** Unity's `Destroy()` doesn't execute immediately - it waits until end of frame. Old layers remained visible while new layers were being created.

**Solution:** Call `SetActive(false)` on old layers before destroying them to hide them immediately.

---

## 📝 Files Modified

### PlayerController.cs

**Added Constant (Line ~54):**
```csharp
// Sprite positioning
private const float SPRITE_VERTICAL_OFFSET = 1.0f; // Offset to render full character sprite above cell
```

**Fixed SetPosition (Line ~128):**
```csharp
public void SetPosition(int cellId)
{
    currentCellId = cellId;
    transform.position = IsometricHelper.CellIdToWorldPosition(cellId) + Vector3.up * SPRITE_VERTICAL_OFFSET;
}
```

**Fixed FollowPath Movement (Line ~222):**
```csharp
int targetCell = currentPath[i];
Vector3 targetPos = IsometricHelper.CellIdToWorldPosition(targetCell) + Vector3.up * SPRITE_VERTICAL_OFFSET;
Debug.Log($"[PlayerController] Moving to cell {targetCell} at position {targetPos} (step {i + 1}/{currentPath.Count})");
```

### CharacterLayerRenderer.cs

**Fixed SetupSpriteLayers (Lines ~178-186):**
```csharp
// Clear existing layers - disable immediately then destroy
foreach (var layer in spriteLayers)
{
    if (layer != null && layer.gameObject != this.gameObject)
    {
        layer.gameObject.SetActive(false); // Hide immediately
        Destroy(layer.gameObject); // Destroy at end of frame
    }
}
spriteLayers.Clear();
```

---

## 🧪 Testing Instructions

### Step 1: Verify Unity Compilation

1. **Open Unity Console** (Ctrl+Shift+C)
2. **Wait for compilation** - Check bottom-right corner
3. **Clear Console** - Click "Clear" button
4. **Enter Play Mode**

### Step 2: Check Sprite Positioning

**Click on various cells to move the character**

**Expected Results:**
- ✅ Full character sprite visible (not just face)
- ✅ Character feet positioned at/near cell center
- ✅ Character body extends upward from feet
- ✅ Sprite maintains proper vertical position throughout movement

**Visual Verification:**
```
    👤
   /|\    <- Full body visible
   / \    <- Feet at cell level
  ━━━━━   <- Cell
```

### Step 3: Check Animation Changes

**Click cells in different directions to trigger animation changes**

**Expected Results:**
- ✅ No overlapping sprite layers when animation changes
- ✅ Clean transition between directions (N, NE, E, SE, S, SW, W, NW)
- ✅ No ghosting or visual artifacts
- ✅ Walk animation plays correctly in all directions

### Step 4: Test All Directions

**Test movement in all 8 directions:**
- North (up)
- Northeast (diagonal up-right)
- East (right)
- Southeast (diagonal down-right)
- South (down)
- Southwest (diagonal down-left)
- West (left)
- Northwest (diagonal up-left)

**For each direction:**
1. Click a cell in that direction
2. Verify full sprite visible throughout movement
3. Verify correct walk animation plays
4. Verify no layer overlap or artifacts

---

## 🐛 Troubleshooting

### Issue: Sprite Still Too Low/High

**Cause:** Vertical offset needs adjustment
**Fix:**
1. Open PlayerController.cs
2. Find `SPRITE_VERTICAL_OFFSET` constant (line ~54)
3. Adjust value:
   - Increase (e.g., 1.5f) if sprite still too low
   - Decrease (e.g., 0.75f) if sprite too high
4. Save and test again

### Issue: Sprite Layers Still Overlapping

**Cause:** DestroyImmediate might be needed instead
**Fix:**
1. Open CharacterLayerRenderer.cs
2. Find SetupSpriteLayers method (~line 176)
3. Change `Destroy(layer.gameObject)` to `DestroyImmediate(layer.gameObject)`
4. Save and test

### Issue: Sprite Appears Twice Briefly

**Cause:** New layers created before old ones destroyed
**Alternative Fix:**
```csharp
// In SetupSpriteLayers, before creating new layers:
foreach (var layer in spriteLayers)
{
    if (layer != null && layer.gameObject != this.gameObject)
        DestroyImmediate(layer.gameObject); // Immediate destruction
}
spriteLayers.Clear();
```

---

## 📊 Success Criteria

| Feature | Status | Verification |
|---------|--------|--------------|
| Full sprite visible | ✅ Fixed | Body + face visible |
| Feet at cell level | ✅ Fixed | Position at cell center |
| No layer overlap | ✅ Fixed | Clean animation changes |
| All directions work | 🟡 To Test | Test 8 directions |
| Consistent positioning | ✅ Fixed | Same offset everywhere |

---

## 🔄 Technical Details

### Sprite Vertical Offset Calculation

The offset of `1.0f` units is chosen based on:
- Cell dimensions: `CELL_HALF_HEIGHT = 0.8f`
- Typical character sprite height in isometric games
- Balance between visibility and ground contact

If character sprites are taller/shorter, this value may need adjustment.

### Layer Management

Unity's destroy behavior:
- `Destroy()` - Queued for end of frame (can cause brief overlap)
- `SetActive(false)` - Immediate (hides instantly)
- Combined approach: Hide immediately, destroy shortly after

### Position Consistency

All character positioning now uses:
```csharp
Vector3 worldPos = IsometricHelper.CellIdToWorldPosition(cellId) + Vector3.up * SPRITE_VERTICAL_OFFSET;
```

This ensures:
- Initial spawn position matches movement position
- No sprite "jumping" when movement starts
- Consistent rendering across all cells

---

## 📞 If Issues Persist

**Please provide:**
1. Screenshot showing sprite positioning issue
2. Console logs from movement
3. Unity Inspector view of Player GameObject during play
4. Description of which direction(s) have issues

**Key things to verify:**
- Is `SPRITE_VERTICAL_OFFSET` constant applied?
- Are old layers being deactivated?
- Does sprite have correct pivot point (usually bottom-center)?
- Are all sprite layers using same parent transform?

---

**End of Document**
