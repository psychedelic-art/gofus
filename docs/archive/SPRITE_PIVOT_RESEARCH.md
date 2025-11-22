# Sprite Pivot and Multi-Layer Character Rendering - Research Summary

**Date:** November 21, 2025  
**Status:** Research Complete - Implementation Updated  
**Priority:** Critical

---

## 🔍 Research Findings

### Multi-Layer Sprite Composition Best Practices

Based on research from Unity documentation, Stack Overflow, and game development forums:

#### 1. **Isometric Character Pivot Points**
- For isometric games, sprite pivots MUST be at **ground level** (bottom of sprite, at character's feet)
- This ensures proper depth sorting and positioning in isometric scenes
- **Standard pivot for isometric characters: (0.5, 0) = bottom-center**

#### 2. **Multi-Layer Character Systems**
- All sprite layers (head, body, legs, equipment) must be positioned at **the same point**
- Each sprite's individual pivot point handles its own alignment
- Layers are designed to align correctly when all placed at `localPosition = Vector3.zero`
- **DO NOT offset individual layers** - this breaks the composition

#### 3. **Dofus-Style Layered Characters**
- Dofus uses vector-based layered sprite system
- Multiple sprite layers per character (body parts, clothing, equipment)
- Each layer has pre-configured pivot points from asset extraction
- Layers stack with z-ordering for proper rendering

#### 4. **Parent Transform Control**
- Parent GameObject position controls overall character placement
- All child layers use `localPosition = Vector3.zero`
- Vertical offset applied at parent level, not per-layer

---

## ✅ Implementation Fixes Applied

### 1. Character Layer Rendering (CharacterLayerRenderer.cs)

**Fixed:**
```csharp
// All layers at same position - DO NOT offset individually
for (int i = 0; i < sprites.Count; i++)
{
    GameObject layerObj = new GameObject($"Layer_{i}");
    layerObj.transform.SetParent(transform);
    layerObj.transform.localPosition = Vector3.zero; // All layers aligned
    layerObj.transform.localScale = Vector3.one;
    
    SpriteRenderer sr = layerObj.AddComponent<SpriteRenderer>();
    sr.sprite = sprites[i];
    sr.sortingOrder = sortingOrder + i; // Stack layers
    
    spriteLayers.Add(sr);
}
```

**Why This Works:**
- Each sprite layer has its own pivot point from Dofus asset extraction
- When all layers are at `Vector3.zero`, their individual pivots align them correctly
- This preserves the designed composition of head, body, legs, etc.

### 2. Layer Cleanup (CharacterLayerRenderer.cs)

**Fixed:**
```csharp
// Use DestroyImmediate to prevent visual overlap
foreach (var layer in spriteLayers)
{
    if (layer != null && layer.gameObject != this.gameObject)
    {
        DestroyImmediate(layer.gameObject); // Immediate destruction
    }
}
spriteLayers.Clear();
```

**Why This Works:**
- `Destroy()` waits until end of frame, causing brief overlap
- `DestroyImmediate()` removes old layers instantly before creating new ones
- Eliminates ghosting/overlap during animation changes

### 3. Vertical Offset Adjustment (PlayerController.cs)

**Changed:**
```csharp
// Reduced from 2.0f to 0.5f
private const float SPRITE_VERTICAL_OFFSET = 0.5f;
```

**Why:**
- Previous 2.0f offset was too high, assuming sprite pivot at center
- 0.5f is more appropriate for sprites with bottom-center pivots
- Can be fine-tuned based on actual sprite pivot configuration

### 4. Debug Logging Added

**New Diagnostics:**
```csharp
if (showDebugInfo && i == 0 && sprites[i] != null)
{
    Sprite sprite = sprites[i];
    Vector2 pivot = sprite.pivot;
    Rect rect = sprite.rect;
    Vector2 pivotNormalized = new Vector2(pivot.x / rect.width, pivot.y / rect.height);
    
    Debug.Log($"[CharacterLayerRenderer] First sprite info:");
    Debug.Log($"  - Size: {rect.width}x{rect.height}");
    Debug.Log($"  - Pivot (pixels): {pivot}");
    Debug.Log($"  - Pivot (normalized): {pivotNormalized}");
    Debug.Log($"  - Bounds size: {sprite.bounds.size}");
}
```

---

## 🧪 Testing Instructions

### Step 1: Check Sprite Pivot Configuration

When you run the game, look for debug logs like:
```
[CharacterLayerRenderer] First sprite info:
  - Size: 200x150
  - Pivot (pixels): (100, 75)
  - Pivot (normalized): (0.5, 0.5)  <-- CENTER PIVOT
```

### Step 2: Interpret Pivot Values

**Normalized Pivot Values:**
- `(0.5, 0)` = **Bottom-center** (IDEAL for isometric)
- `(0.5, 0.5)` = **Center** (requires offset)
- `(0, 0)` = Bottom-left
- `(0.5, 1)` = Top-center

### Step 3: Adjust Vertical Offset Based on Pivot

**If pivot is at bottom (0.5, 0):**
```csharp
private const float SPRITE_VERTICAL_OFFSET = 0f; // No offset needed
```

**If pivot is at center (0.5, 0.5):**
```csharp
// Calculate: sprite height in world units / 2
// Example: if sprite is 1.5 units tall
private const float SPRITE_VERTICAL_OFFSET = 0.75f;
```

**Current Setting:**
```csharp
private const float SPRITE_VERTICAL_OFFSET = 0.5f; // Starting point
```

### Step 4: Test Character Positioning

1. **Click on cells** - Character should move smoothly
2. **Check feet position** - Feet should be at cell level, not floating or sinking
3. **Test all 8 directions** - Animation changes should be clean, no overlapping layers
4. **Verify layer alignment** - All body parts should stay aligned during movement

---

## 🔧 How to Fix Sprite Pivots (If Needed)

If the debug logs show incorrect pivot points, you can fix them:

### Option 1: Fix in Unity (Per-Sprite)

1. Select sprite in Project window
2. In Inspector, change **Sprite Mode** to **Multiple** (if sprite sheet)
3. Click **Sprite Editor**
4. Select each sprite
5. Change **Pivot** dropdown to **Bottom** or **Custom**
6. If Custom, set to `(0.5, 0)` for bottom-center
7. Click **Apply**

### Option 2: Fix All Sprites in Import Settings

1. Select all character sprites in Project window
2. In Inspector:
   - **Sprite Mode**: Multiple
   - **Pivot**: Bottom
   - **Pixels Per Unit**: Check value (default 100)
3. Click **Apply**
4. Reimport assets

### Option 3: Programmatic Fix (Advanced)

```csharp
// In CharacterLayerRenderer.SetupSpriteLayers()
// Only if sprite pivots cannot be fixed in import settings

// Calculate the lowest pivot point among all layers
float lowestPivotY = float.MaxValue;
foreach (var sprite in sprites)
{
    if (sprite != null)
    {
        float pivotNormalized = sprite.pivot.y / sprite.rect.height;
        if (pivotNormalized < lowestPivotY)
            lowestPivotY = pivotNormalized;
    }
}

// Offset parent if pivots are not at bottom
if (lowestPivotY > 0.1f) // Not at bottom
{
    float spriteHeight = sprites[0].bounds.size.y;
    transform.localPosition += Vector3.down * (spriteHeight * lowestPivotY);
}
```

---

## 📊 Expected Results

### Before Fixes:
- ❌ Sprite layers offset individually (breaks composition)
- ❌ Old layers overlap new ones during animation change
- ❌ Character positioned too high (2.0f offset with wrong pivot assumption)
- ❌ Only head visible or layers misaligned

### After Fixes:
- ✅ All sprite layers at `Vector3.zero` (proper composition)
- ✅ Clean animation changes with `DestroyImmediate`
- ✅ Appropriate vertical offset (0.5f, adjustable)
- ✅ Full character visible with proper alignment
- ✅ Debug logs show sprite pivot information

---

## 🎯 Key Takeaways

1. **Never offset individual sprite layers** - breaks multi-layer composition
2. **All layers must be at localPosition = Vector3.zero** - pivot points handle alignment
3. **Parent transform controls overall position** - use SPRITE_VERTICAL_OFFSET
4. **Sprite pivots should be at bottom (0.5, 0)** for isometric characters
5. **Use DestroyImmediate for layer cleanup** - prevents visual overlap
6. **Check debug logs** - pivot information reveals configuration issues

---

## 📞 If Issues Persist

**Check Debug Logs for:**
```
[CharacterLayerRenderer] Pivot (normalized): (X, Y)
```

**Then adjust:**
- If Y ≈ 0: `SPRITE_VERTICAL_OFFSET = 0f`
- If Y ≈ 0.5: `SPRITE_VERTICAL_OFFSET = sprite_height / 2`
- If Y ≈ 1: `SPRITE_VERTICAL_OFFSET = sprite_height`

**Provide:**
1. Full debug log output showing sprite info
2. Screenshot of character positioning
3. Screenshot of sprite in Unity Inspector showing pivot point

---

**End of Document**
