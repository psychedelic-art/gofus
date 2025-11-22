# GOFUS Unity Integration Guide

**Date**: November 17, 2025
**Status**: ✅ Core System Implemented
**Version**: 1.0

## Table of Contents

1. [Overview](#overview)
2. [System Architecture](#system-architecture)
3. [Components](#components)
4. [Quick Start](#quick-start)
5. [Usage Examples](#usage-examples)
6. [Testing](#testing)
7. [Troubleshooting](#troubleshooting)
8. [Next Steps](#next-steps)

## Overview

The GOFUS Unity client uses a **layered sprite composition system** for character rendering. Unlike traditional single-sprite approaches, Dofus characters are assembled from multiple sprite layers (shapes) that combine to create the final character appearance.

### Key Features

- **Layered Rendering**: Characters built from multiple sprite layers
- **Dynamic Composition**: Mix-and-match equipment and appearance
- **Resource Management**: Efficient sprite loading and caching
- **Modular Design**: Easy to extend and customize
- **Test Suite**: Comprehensive testing tools included

### Character Composition

Each character consists of:
1. Base body shape
2. Skin/color layer
3. Hair layer
4. Facial features
5. Clothing/armor layers
6. Accessory layers
7. Weapon layers

## System Architecture

### Core Components

```
GOFUS.Rendering
├── CharacterLayerRenderer.cs    // Main character rendering component
└── (Future: Animation system)

GOFUS.UI
├── ClassSpriteManager.cs        // Sprite loading and caching
└── ClassSpriteManagerExtensions.cs // Helper methods

GOFUS.Tests
└── CharacterRenderingTest.cs    // Testing utilities
```

### Data Flow

```
1. ClassSpriteManager
   ↓ (loads sprites from Resources)
2. CharacterLayerRenderer
   ↓ (composes sprite layers)
3. Unity SpriteRenderer(s)
   ↓ (renders to screen)
4. Visual Character
```

## Components

### CharacterLayerRenderer

**Location**: `Assets/_Project/Scripts/Rendering/CharacterLayerRenderer.cs`

Main component for rendering layered characters.

#### Public Methods

```csharp
// Initialize the character
void InitializeCharacter()

// Change animation
void SetAnimation(string animationName)

// Change character class
void SetClass(int classId, bool isMale = true)

// Update sorting
void SetSortingOrder(int order)
void SetSortingLayer(string layerName)
```

#### Properties

```csharp
int ClassId { get; }           // Current class ID (1-12)
bool IsMale { get; }           // Gender
string CurrentAnimation { get; } // Current animation name
int LayerCount { get; }        // Number of sprite layers
```

#### Inspector Fields

- **classId**: Character class (1-12)
- **isMale**: Gender selection
- **currentAnimation**: Starting animation
- **sortingOrder**: Rendering order
- **sortingLayerName**: Unity sorting layer
- **showDebugInfo**: Enable debug logging

### ClassSpriteManager

**Location**: `Assets/_Project/Scripts/UI/ClassSpriteManager.cs`

Singleton manager for loading and caching character sprites.

#### Public Methods

```csharp
// Load all class sprites
void LoadClassSprites()

// Get sprite for class
Sprite GetClassSprite(int classId)

// Get icon for class
Sprite GetClassIcon(int classId)

// Get class information
string GetClassName(int classId)
string GetClassDescription(int classId)
int GetClassIdFromName(string className)

// Utility
List<int> GetAllClassIds()
List<string> GetAllClassNames()
void ClearSprites()
```

#### Properties

```csharp
bool IsLoaded { get; }         // Are sprites loaded?
int LoadedSpriteCount { get; } // Number of sprites loaded
int LoadedIconCount { get; }   // Number of icons loaded
```

### ClassSpriteManagerExtensions

**Location**: `Assets/_Project/Scripts/UI/ClassSpriteManagerExtensions.cs`

Extension methods for easy character creation.

#### Public Methods

```csharp
// Create a character renderer
CharacterLayerRenderer CreateCharacterRenderer(
    this ClassSpriteManager manager,
    int classId,
    bool isMale = true,
    Transform parent = null,
    Vector3? position = null
)
```

## Quick Start

### Step 1: Verify Assets

Ensure extracted assets are in the Resources folder:

```
Assets/_Project/Resources/Sprites/Classes/
├── Feca/
│   ├── sprites/
│   │   ├── DefineSprite_XXX/
│   │   │   └── *.png
├── Osamodas/
├── Enutrof/
└── ... (all 12 classes)
```

### Step 2: Create a Test Scene

1. Create new scene: `Assets/_Project/Scenes/CharacterTest.unity`
2. Add empty GameObject: "CharacterTester"
3. Attach `CharacterRenderingTest` component
4. Configure in Inspector:
   - Create On Start: ✓
   - Test Class Id: 1 (Feca)
   - Is Male: ✓
5. Play scene

### Step 3: Verify Rendering

Check Console for output:
```
[CharacterRenderingTest] Testing character class 1 (Male)
[CharacterLayerRenderer] Initializing character - Class: Feca, Male: True
[CharacterLayerRenderer] Found X sprites
[CharacterLayerRenderer] Created X sprite layers
```

## Usage Examples

### Example 1: Create Single Character

```csharp
using GOFUS.UI;
using GOFUS.Rendering;
using UnityEngine;

public class MyScript : MonoBehaviour
{
    void Start()
    {
        // Create a Feca character (class ID 1)
        CharacterLayerRenderer feca = ClassSpriteManager.Instance
            .CreateCharacterRenderer(
                classId: 1,
                isMale: true,
                parent: transform,
                position: Vector3.zero
            );

        Debug.Log($"Created {feca.LayerCount} layer character");
    }
}
```

### Example 2: Create Multiple Characters

```csharp
void CreateAllClasses()
{
    for (int classId = 1; classId <= 12; classId++)
    {
        Vector3 position = new Vector3(classId * 2f, 0, 0);

        CharacterLayerRenderer character = ClassSpriteManager.Instance
            .CreateCharacterRenderer(
                classId,
                true,
                transform,
                position
            );

        string name = ClassSpriteManager.Instance.GetClassName(classId);
        character.gameObject.name = $"Character_{name}";
    }
}
```

### Example 3: Change Character Animation

```csharp
CharacterLayerRenderer character;

void ChangeToWalkAnimation()
{
    if (character != null)
    {
        character.SetAnimation("walkS"); // Walk south
    }
}

void ChangeToRunAnimation()
{
    if (character != null)
    {
        character.SetAnimation("runS"); // Run south
    }
}
```

### Example 4: Change Character Class

```csharp
CharacterLayerRenderer character;

void SwitchToIop()
{
    if (character != null)
    {
        character.SetClass(8, true); // Switch to Iop (ID 8)
    }
}
```

### Example 5: Manual Character Creation

```csharp
GameObject characterObj = new GameObject("MyCharacter");
CharacterLayerRenderer renderer = characterObj.AddComponent<CharacterLayerRenderer>();

// This will be called automatically, but you can call it manually
renderer.InitializeCharacter();

// Customize
renderer.SetSortingLayer("Characters");
renderer.SetSortingOrder(100);
```

## Testing

### Using CharacterRenderingTest

The `CharacterRenderingTest` component provides a comprehensive testing interface.

#### In-Game UI Controls

When running the test scene, you'll see an on-screen menu:

- **Test Single Character**: Create one character
- **Test All Classes**: Create all 12 classes in a grid
- **Clear All**: Remove all test characters
- **Animation Tests**: Switch between animations
- **Class Slider**: Change class ID (1-12)

#### Inspector Configuration

```
Character Rendering Test
├── Test Configuration
│   ├── Create On Start: Auto-create on scene start
│   ├── Test Class Id: Which class to test (1-12)
│   ├── Is Male: Gender selection
│   └── Spawn Position: Where to spawn
├── Test All Classes
│   ├── Test All Classes: Create grid of all classes
│   └── Spacing: Grid spacing
└── Runtime Controls
    └── Show Debug Info: Display on-screen menu
```

#### Programmatic Testing

```csharp
CharacterRenderingTest tester = GetComponent<CharacterRenderingTest>();

// Test single character
tester.TestSingleCharacter();

// Test all classes
tester.TestAllCharacterClasses();

// Change animation
tester.ChangeAnimation("walkS");

// Change class
tester.ChangeClass(8); // Switch to Iop

// Cleanup
tester.ClearAllCharacters();
```

## Troubleshooting

### Issue: No Sprites Appear

**Symptoms**: Character renders but no visible sprites

**Causes & Solutions**:

1. **Assets not in Resources folder**
   - Verify: `Assets/_Project/Resources/Sprites/Classes/` exists
   - Run asset extraction if needed

2. **Incorrect sprite paths**
   - Check Console for warnings
   - Verify folder structure matches extracted assets

3. **Camera settings**
   - Ensure camera can see sprite layers
   - Check sorting layers in Project Settings

### Issue: "No sprites found" Warning

**Symptoms**: Console shows "No sprites found at Sprites/Classes/..."

**Solutions**:

1. **Re-import assets**
   - Right-click Resources folder → Reimport

2. **Check .meta files**
   - Each .png should have a .meta file
   - If missing, Unity will regenerate on import

3. **Verify extraction**
   - Check `docs/EXTRACTION_SUMMARY.md`
   - Confirm 112,614+ files extracted

### Issue: Performance Problems

**Symptoms**: Low FPS with many characters

**Solutions**:

1. **Use sprite atlases**
   - Create sprite atlases for character classes
   - Enable atlas packing in Project Settings

2. **Limit active characters**
   - Use object pooling
   - Disable off-screen characters

3. **Optimize layers**
   - Reduce number of active layers
   - Combine static layers

### Issue: Characters Not Layering Correctly

**Symptoms**: Body parts render in wrong order

**Solutions**:

1. **Check sorting order**
   ```csharp
   character.SetSortingOrder(100);
   ```

2. **Verify sorting layers**
   - Edit → Project Settings → Tags and Layers
   - Add "Characters" sorting layer

3. **Adjust layer z-position**
   - Modify `CharacterLayerRenderer.SetupSpriteLayers()`

## Class Reference

### Character Class IDs

| ID | Class | Description |
|----|-------|-------------|
| 1 | Feca | Masters of protection and defensive magic |
| 2 | Osamodas | Beast masters who summon creatures |
| 3 | Enutrof | Treasure hunters with earth magic |
| 4 | Sram | Deadly assassins with traps |
| 5 | Xelor | Time mages who manipulate AP |
| 6 | Ecaflip | Gamblers relying on luck |
| 7 | Eniripsa | Powerful healers and support |
| 8 | Iop | Fearless melee warriors |
| 9 | Cra | Expert archers with precision |
| 10 | Sadida | Nature mages commanding plants |
| 11 | Sacrieur | Berserkers gaining power from pain |
| 12 | Pandawa | Drunken brawlers |

### Animation Names

Common animation states:

- `staticS` - Static south (default)
- `staticR` - Static right
- `staticL` - Static left
- `staticF` - Static forward
- `staticB` - Static back
- `walkS` - Walk south
- `walkR` - Walk right
- `walkL` - Walk left
- `walkF` - Walk forward
- `walkB` - Walk back
- `runS` - Run south
- `runR` - Run right
- `runL` - Run left
- `runF` - Run forward
- `runB` - Run back

## Next Steps

### Immediate

1. **Test Character Rendering**
   - Create test scene
   - Verify all 12 classes render
   - Test animations

2. **Integrate with Character Selection**
   - Update `CharacterSelectionScreen.cs`
   - Use `CharacterLayerRenderer` instead of placeholders
   - Test character preview

### Short-term

1. **Animation System**
   - Create `AnimatorController` templates
   - Define animation state machines
   - Implement blend trees for directions

2. **Equipment System**
   - Load equipment sprites
   - Add equipment layers dynamically
   - Handle equipment changes

3. **Optimization**
   - Create sprite atlases
   - Implement object pooling
   - Profile performance

### Long-term

1. **Advanced Features**
   - Color customization
   - Skin tone variations
   - Particle effects
   - Shadows and lighting

2. **Tools**
   - Character preview editor
   - Animation timeline
   - Equipment visualizer

## Additional Resources

- **Extraction Guide**: `docs/EXTRACTION_SUMMARY.md`
- **Asset Extraction Plan**: `docs/ASSET_EXTRACTION_PLAN.md`
- **Character Selection Fix**: `docs/CHARACTER_SELECTION_FIX.md`
- **Class Integration**: `docs/CLASS_INTEGRATION_GUIDE.md`

## API Reference

### CharacterLayerRenderer API

```csharp
namespace GOFUS.Rendering
{
    public class CharacterLayerRenderer : MonoBehaviour
    {
        // Properties
        public int ClassId { get; }
        public bool IsMale { get; }
        public string CurrentAnimation { get; }
        public int LayerCount { get; }

        // Methods
        public void InitializeCharacter();
        public void SetAnimation(string animationName);
        public void SetClass(int newClassId, bool male = true);
        public void SetSortingOrder(int order);
        public void SetSortingLayer(string layerName);
    }
}
```

### ClassSpriteManager API

```csharp
namespace GOFUS.UI
{
    public class ClassSpriteManager : MonoBehaviour
    {
        // Singleton
        public static ClassSpriteManager Instance { get; }

        // Properties
        public bool IsLoaded { get; }
        public int LoadedSpriteCount { get; }
        public int LoadedIconCount { get; }

        // Methods
        public void LoadClassSprites();
        public Sprite GetClassSprite(int classId);
        public Sprite GetClassIcon(int classId);
        public string GetClassName(int classId);
        public string GetClassDescription(int classId);
        public int GetClassIdFromName(string className);
        public List<int> GetAllClassIds();
        public List<string> GetAllClassNames();
        public void ClearSprites();
    }
}
```

### Extension Methods API

```csharp
namespace GOFUS.UI
{
    public static class ClassSpriteManagerExtensions
    {
        public static CharacterLayerRenderer CreateCharacterRenderer(
            this ClassSpriteManager manager,
            int classId,
            bool isMale = true,
            Transform parent = null,
            Vector3? position = null
        );
    }
}
```

---

**Last Updated**: November 17, 2025
**Document Version**: 1.0
**Status**: Ready for Integration
