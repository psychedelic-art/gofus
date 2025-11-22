# Class Integration Guide

## Overview
This guide explains how to integrate the 12 Dofus classes with sprites and spells into your GOFUS Character Selection screen.

## 📋 Table of Contents
1. [Database Seeding](#1-database-seeding)
2. [Class Sprite Setup](#2-class-sprite-setup)
3. [Backend API Enhancement](#3-backend-api-enhancement)
4. [Unity CharacterSelection Integration](#4-unity-characterselection-integration)
5. [Testing](#5-testing)

---

## 1. Database Seeding

### Run the Seed File

The seed file contains all 12 classes and 60+ spells based on official Dofus lore.

**Location**: `gofus-backend/drizzle/seed_classes_and_spells.sql`

**Execute the seed:**

```bash
cd gofus-backend

# Option 1: Using psql
psql $DATABASE_URL -f drizzle/seed_classes_and_spells.sql

# Option 2: Using Drizzle (if you have a seed script)
npm run db:seed

# Option 3: Copy/paste SQL into Supabase SQL Editor
# Go to your Supabase Dashboard → SQL Editor → New Query
# Paste the entire contents of seed_classes_and_spells.sql
# Click "Run"
```

### What Gets Seeded

**12 Classes:**
1. **Feca** - Masters of protection and defensive magic
2. **Osamodas** - Beast masters who summon creatures
3. **Enutrof** - Treasure hunters with earth magic
4. **Sram** - Deadly assassins with traps
5. **Xelor** - Time mages who manipulate AP
6. **Ecaflip** - Gamblers relying on luck
7. **Eniripsa** - Powerful healers and support
8. **Iop** - Fearless melee warriors
9. **Cra** - Expert archers with precision
10. **Sadida** - Nature mages commanding plants
11. **Sacrieur** - Berserkers gaining power from pain
12. **Pandawa** - Drunken brawlers

**60+ Spells** with full properties:
- Damage values
- AP cost
- Range
- Cooldowns
- Special effects
- Critical hit chances

---

## 2. Class Sprite Setup

### Extracted Sprites Location

Class sprites have been decompiled from the Dofus client and extracted to:

```
gofus-client/Assets/_Project/Resources/Sprites/Classes/
```

**Extracted classes:**
- `Class_01_Feca_Male/`
- `Class_02_Osamodas_Male/`
- `Class_08_Iop_Male/`
- `Class_09_Cra_Male/`
- `Class_10_Sadida_Male/`

### Extract Remaining Classes

To extract more class sprites, use the JPEXS FFDec decompiler:

```bash
# Navigate to sprite directory
cd "C:\Users\HardM\Desktop\Enterprise\gofus\Cliente retro\resources\app\retroclient\clips\sprites"

# Extract specific class (replace XX with sprite ID)
java -jar "/c/Program Files (x86)/FFDec/ffdec.jar" \
  -export image \
  "C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client\Assets\_Project\Resources\Sprites\Classes\Class_XX_Name_Male" \
  XX.swf
```

**Sprite ID Mapping:**
- 10.swf/11.swf = Feca (Male/Female)
- 100.swf/101.swf = Iop (Male/Female)
- 110.swf/111.swf = Cra (Male/Female)
- 120.swf/121.swf = Sadida (Male/Female)
- Continue pattern for remaining classes

### Import Sprites into Unity

1. **Open Unity Editor**
2. **Select** `Assets/_Project/Resources/Sprites/Classes/`
3. **For each sprite folder:**
   - Select all PNG files
   - In Inspector, set:
     - Texture Type: **Sprite (2D and UI)**
     - Sprite Mode: **Multiple** (if spritesheet) or **Single**
     - Pixels Per Unit: **100**
     - Filter Mode: **Point (no filter)** for pixel art
     - Compression: **None** or **Low Quality**
   - Click **Apply**

4. **Create Sprite Atlas (Optional but recommended):**
   ```csharp
   // In Unity: Window → 2D → Sprite Atlas
   // Create new Sprite Atlas for each class
   // Drag class sprites into the atlas
   // This improves performance
   ```

---

## 3. Backend API Enhancement

### Update GET /api/characters Response

Modify the character endpoint to include class information.

**File**: `gofus-backend/app/api/characters/route.ts`

```typescript
import { db } from '@/lib/db';
import { characters, classes } from '@/lib/db/schema';
import { eq } from 'drizzle-orm';

export async function GET(request: Request) {
  const token = getTokenFromRequest(request);
  const accountId = verifyToken(token);

  // Join characters with classes table
  const userCharacters = await db
    .select({
      id: characters.id,
      name: characters.name,
      level: characters.level,
      classId: characters.classId,
      className: classes.name,
      classDescription: classes.description,
      sex: characters.sex,
      mapId: characters.mapId,
      cellId: characters.cellId,
      experience: characters.experience,
      kamas: characters.kamas,
    })
    .from(characters)
    .leftJoin(classes, eq(characters.classId, classes.id))
    .where(eq(characters.accountId, accountId));

  return Response.json({ characters: userCharacters });
}
```

### Create GET /api/classes Endpoint

Create a new endpoint to fetch all available classes for character creation.

**File**: `gofus-backend/app/api/classes/route.ts` (Create new file)

```typescript
import { NextResponse } from 'next/server';
import { db } from '@/lib/db';
import { classes, static_spells } from '@/lib/db/schema';
import { eq } from 'drizzle-orm';

export async function GET() {
  try {
    // Fetch all classes with their starting spells
    const allClasses = await db
      .select({
        id: classes.id,
        name: classes.name,
        description: classes.description,
        startSpells: classes.startSpells,
        statsPerLevel: classes.statsPerLevel,
      })
      .from(classes)
      .orderBy(classes.id);

    // For each class, fetch spell details
    const classesWithSpells = await Promise.all(
      allClasses.map(async (classInfo) => {
        const spellIds = classInfo.startSpells as number[];

        const spellDetails = await db
          .select()
          .from(static_spells)
          .where(eq(static_spells.classId, classInfo.id));

        return {
          ...classInfo,
          spells: spellDetails,
        };
      })
    );

    return NextResponse.json({ classes: classesWithSpells });
  } catch (error) {
    console.error('Failed to fetch classes:', error);
    return NextResponse.json(
      { error: 'Failed to fetch classes' },
      { status: 500 }
    );
  }
}
```

---

## 4. Unity CharacterSelection Integration

### Step 1: Create ClassData Model

Add class information to your CharacterData struct.

**File**: `CharacterSelectionScreen.cs`

```csharp
[Serializable]
public class ClassInfo
{
    public int id;
    public string name;
    public string description;
    public string[] startSpells;
    public StatsPerLevel statsPerLevel;
}

[Serializable]
public class StatsPerLevel
{
    public int vitality;
    public int wisdom;
    public int strength;
    public int intelligence;
    public int chance;
    public int agility;
}

// Update CharacterData to include classDescription
public struct CharacterData
{
    public int Id;
    public string Name;
    public int Level;
    public string Class;
    public string ClassDescription; // NEW
    public string Gender;
    public string LastPlayed;
    public int Experience;
    public int MapId;
}
```

### Step 2: Load Class Sprites from Resources

```csharp
using UnityEngine;

public class ClassSpriteManager
{
    private Dictionary<int, Sprite> classSprites;

    public void LoadClassSprites()
    {
        classSprites = new Dictionary<int, Sprite>();

        // Load sprites from Resources folder
        // Assumes sprites are named: Class_01_Feca_Male, etc.
        for (int classId = 1; classId <= 12; classId++)
        {
            string spritePath = $"Sprites/Classes/Class_{classId:D2}_Icon";
            Sprite sprite = Resources.Load<Sprite>(spritePath);

            if (sprite != null)
            {
                classSprites[classId] = sprite;
            }
            else
            {
                Debug.LogWarning($"[ClassSprites] Could not load sprite for class {classId} at path: {spritePath}");
            }
        }
    }

    public Sprite GetClassSprite(int classId)
    {
        if (classSprites.TryGetValue(classId, out Sprite sprite))
        {
            return sprite;
        }

        Debug.LogWarning($"[ClassSprites] No sprite found for class ID {classId}");
        return null;
    }
}
```

### Step 3: Update CharacterSlot to Display Class Icon

Modify the `CharacterSlot` class to show class sprites.

**File**: `CharacterSelectionScreen.cs` (CharacterSlot section)

```csharp
public class CharacterSlot : MonoBehaviour
{
    private Image classIcon; // NEW
    private ClassSpriteManager spriteManger;

    private void CreateUI()
    {
        // ... existing code ...

        // Add Class Icon
        GameObject iconObj = new GameObject("ClassIcon");
        iconObj.transform.SetParent(transform, false);
        classIcon = iconObj.AddComponent<Image>();

        RectTransform iconRect = classIcon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.05f, 0.6f);
        iconRect.anchorMax = new Vector2(0.25f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        classIcon.preserveAspect = true;
    }

    public void SetCharacterData(CharacterData data, ClassSpriteManager spriteManager)
    {
        characterData = data;
        this.spriteManger = spriteManager;

        if (data != null)
        {
            if (nameText) nameText.text = data.Name;
            if (levelText) levelText.text = $"Level {data.Level}";
            if (classText) classText.text = data.Class;

            // Set class icon sprite
            if (classIcon && spriteManager != null)
            {
                Sprite sprite = spriteManager.GetClassSprite(GetClassIdFromName(data.Class));
                if (sprite != null)
                {
                    classIcon.sprite = sprite;
                    classIcon.enabled = true;
                }
            }

            background.color = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        }
        else
        {
            Clear();
        }
    }

    private int GetClassIdFromName(string className)
    {
        // Map class names to IDs
        switch (className.ToLower())
        {
            case "feca": return 1;
            case "osamodas": return 2;
            case "enutrof": return 3;
            case "sram": return 4;
            case "xelor": return 5;
            case "ecaflip": return 6;
            case "eniripsa": return 7;
            case "iop": return 8;
            case "cra": return 9;
            case "sadida": return 10;
            case "sacrieur": return 11;
            case "pandawa": return 12;
            default: return 0;
        }
    }
}
```

### Step 4: Initialize ClassSpriteManager in CharacterSelectionScreen

```csharp
public class CharacterSelectionScreen : UIScreen
{
    private ClassSpriteManager classSpriteManager;

    public override void Initialize()
    {
        base.Initialize();

        // Initialize class sprite manager
        classSpriteManager = new ClassSpriteManager();
        classSpriteManager.LoadClassSprites();

        // ... rest of initialization ...
    }

    public void LoadCharacters(List<CharacterData> characters)
    {
        loadedCharacters = characters;

        // Display characters in slots
        for (int i = 0; i < characterSlots.Count && i < characters.Count; i++)
        {
            // Pass sprite manager to character slot
            characterSlots[i].SetCharacterData(characters[i], classSpriteManager);
        }

        // ... rest of method ...
    }
}
```

### Step 5: Update Backend Response Parsing

Update the `ConvertToCharacterDataList` method to include class description:

```csharp
private List<CharacterData> ConvertToCharacterDataList(BackendCharacter[] backendChars)
{
    List<CharacterData> charList = new List<CharacterData>();

    foreach (var bc in backendChars)
    {
        charList.Add(new CharacterData
        {
            Id = bc.id,
            Name = bc.name,
            Level = bc.level,
            Class = bc.className, // From joined classes table
            ClassDescription = bc.classDescription, // NEW
            Gender = bc.sex ? "Male" : "Female",
            LastPlayed = "Today",
            Experience = bc.experience,
            MapId = bc.mapId
        });
    }

    return charList;
}

[Serializable]
private class BackendCharacter
{
    public int id;
    public string name;
    public int level;
    public int classId;
    public string className; // NEW
    public string classDescription; // NEW
    public bool sex;
    public int mapId;
    public int cellId;
}
```

---

## 5. Testing

### Test Database Seeding

```sql
-- Verify classes were inserted
SELECT * FROM classes ORDER BY id;

-- Verify spells were inserted
SELECT COUNT(*) FROM spells;
SELECT COUNT(*) FROM static_spells;

-- Check class with spells
SELECT
  c.id,
  c.name,
  c.description,
  s.name as spell_name
FROM classes c
LEFT JOIN static_spells s ON s.class_id = c.id
WHERE c.id = 1; -- Test Feca
```

### Test Backend API

```bash
# Test GET /api/classes
curl https://gofus-backend.vercel.app/api/classes

# Expected response:
{
  "classes": [
    {
      "id": 1,
      "name": "Feca",
      "description": "Masters of protection...",
      "spells": [...]
    },
    ...
  ]
}
```

### Test Unity Integration

1. **Run Unity Project**
2. **Login** with test account
3. **Character Selection screen should show:**
   - Character names
   - Levels
   - **Class icons** (NEW)
   - Class names
4. **Click on a character**
5. **Info panel should display:**
   - Character name
   - Level
   - Class name
   - **Class description** (NEW)

---

## 6. Next Steps

### Add Character Creation Screen

Now that you have classes seeded, create a character creation screen:

```csharp
public class CharacterCreationScreen : UIScreen
{
    private List<ClassInfo> availableClasses;
    private int selectedClassId;

    private void LoadAvailableClasses()
    {
        StartCoroutine(FetchClassesFromBackend());
    }

    private IEnumerator FetchClassesFromBackend()
    {
        string url = $"{backendUrl}/api/classes";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<ClassListResponse>(request.downloadHandler.text);
                availableClasses = response.classes;
                DisplayClassSelection();
            }
        }
    }

    private void DisplayClassSelection()
    {
        // Show grid of class buttons
        foreach (var classInfo in availableClasses)
        {
            CreateClassButton(classInfo);
        }
    }

    private void CreateClassButton(ClassInfo classInfo)
    {
        // Create button with class icon and name
        // On click: selectedClassId = classInfo.id
        // Show class description and stats
    }

    private void CreateCharacter(string characterName)
    {
        // POST /api/characters with:
        // - name
        // - classId (selectedClassId)
        // - sex (selected gender)
    }
}
```

### Enhance Character Display

Add more visual elements:
- Class-colored backgrounds
- Stat indicators (HP, AP, MP)
- Equipment preview
- Last login timestamp
- Character achievements/titles

---

## 📚 Reference

### Class ID Mapping

| ID | Class | Element Focus | Role |
|----|-------|---------------|------|
| 1 | Feca | All | Tank/Support |
| 2 | Osamodas | All | Summoner |
| 3 | Enutrof | Earth/Chance | Support/Loot |
| 4 | Sram | Air/Strength | Assassin |
| 5 | Xelor | Fire/Intelligence | Control |
| 6 | Ecaflip | Chance | Hybrid |
| 7 | Eniripsa | Water/Intelligence | Healer |
| 8 | Iop | Strength | Warrior |
| 9 | Cra | Agility | Archer |
| 10 | Sadida | Intelligence/Chance | Summoner/Support |
| 11 | Sacrieur | Strength | Berserker |
| 12 | Pandawa | Strength/Chance | Brawler/Support |

### Stat Types

- **Vitality**: Increases max HP
- **Wisdom**: Increases max XP gain and resistances
- **Strength**: Increases earth/neutral damage
- **Intelligence**: Increases fire damage and heals
- **Chance**: Increases water damage
- **Agility**: Increases air damage and dodge

---

## ✅ Completion Checklist

- [ ] Database seed file executed successfully
- [ ] All 12 classes appear in `classes` table
- [ ] All 60+ spells appear in `spells` and `static_spells` tables
- [ ] Class sprites extracted from Dofus client
- [ ] Sprites imported into Unity Resources folder
- [ ] GET /api/classes endpoint created and tested
- [ ] GET /api/characters returns class information
- [ ] ClassSpriteManager loads sprites correctly
- [ ] CharacterSlot displays class icons
- [ ] CharacterSelection shows class descriptions
- [ ] All tests pass

---

**Your character selection is now fully integrated with class sprites and data!** 🎉

The next logical step is creating a **Character Creation** screen where players can choose their class, customize appearance, and name their character.
