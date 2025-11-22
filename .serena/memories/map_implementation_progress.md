# Map System Implementation Progress - November 19, 2025

## Current Status: Phase 2 - Database Schema Mismatch Resolved

### Critical Discovery: Existing Database Schema

The production database already has a `maps` table with a DIFFERENT schema than our migrations:

**Actual Database Schema (Production):**
```sql
id              integer PRIMARY KEY (auto-increment)
x               integer NOT NULL           -- World X coordinate
y               integer NOT NULL           -- World Y coordinate  
width           integer DEFAULT 14
height          integer DEFAULT 20
sub_area_id     integer                    -- Region/zone ID
music_id        integer                    -- Background music reference
capabilities    integer DEFAULT 0          -- Map capabilities flags
outdoor         boolean DEFAULT false      -- Indoor/outdoor flag
background_num  integer                    -- Background graphic ID
cells           jsonb NOT NULL             -- 560 cell data
interactives    jsonb                      -- Interactive objects
fight_positions jsonb                      -- Combat spawn points
```

**Our Migration Schema (Needs Update):**
```sql
id              integer PRIMARY KEY
name            varchar(255)
width           integer DEFAULT 14
height          integer DEFAULT 20
cells           jsonb
adjacent_maps   jsonb
background_music varchar(255)
sub_area        integer
created_at      timestamp
updated_at      timestamp
```

### Decision: Use Existing Schema

We will ADAPT to the existing database schema rather than migrate. This is the correct Dofus structure.

## Implementation Plan (Step-by-Step)

### Phase 1: Backend Adaptation ✅ (Complete)
- [x] Database schema exists in production
- [x] Map Service created (needs minor updates for schema)
- [x] API endpoint created
- [ ] Update seed script to match ACTUAL schema

### Phase 2: Database Seeding (Current Step)
- [ ] Check if test maps already exist
- [ ] Update seed script with correct fields:
  - Add: x, y coordinates
  - Add: sub_area_id (instead of subArea)
  - Add: music_id (instead of backgroundMusic string)
  - Add: capabilities, outdoor, background_num
  - Add: interactives, fight_positions (empty for test maps)
  - Remove: name, created_at, updated_at
- [ ] Run updated seed script
- [ ] Verify maps in database

### Phase 3: Map Graphics Extraction (Next)
- [ ] Extract 5 map SWF files using JPEXS FFDec
  - 7411_0711291819X.swf (Center - Astrub)
  - 7410_0907071142X.swf (Left - Forest)
  - 7412_0905131019X.swf (Right - Plains)
  - 7339_0706131721X.swf (Top - Mountains)
  - 7340_0706131721X.swf (Bottom - Village)
- [ ] Extract to: `gofus-client/Assets/_Project/Resources/Sprites/Maps/`
- [ ] Extract: backgrounds, cell graphics, objects

### Phase 4: Unity Integration
- [ ] Update MapRenderer to fetch from API
- [ ] Create MapDataResponse models
- [ ] Implement GameHUD methods
- [ ] Test rendering

## Map SWF Files Available

Located in: `C:\Users\HardM\Desktop\Enterprise\gofus\Cliente retro\resources\app\retroclient\data\maps\`

Target maps:
- ✅ 7411_0711291819X.swf - EXISTS
- ✅ 7410_0907071142X.swf - EXISTS
- ✅ 7412_0905131019X.swf - EXISTS
- ✅ 7339_0706131721X.swf - EXISTS
- ✅ 7340_0706131721X.swf - EXISTS

Total available: 14,000+ map SWF files

## Extraction Commands (JPEXS FFDec)

Based on EXTRACTION_SUMMARY.md pattern:

```bash
# Navigate to JPEXS location
cd "C:\Program Files (x86)\FFDec"

# Extract Map 7411 (Center)
java -jar ffdec.jar -export image,shape,sprite "C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client\Assets\_Project\Resources\Sprites\Maps\7411" "C:\Users\HardM\Desktop\Enterprise\gofus\Cliente retro\resources\app\retroclient\data\maps\7411_0711291819X.swf" -format image:png -onerror ignore

# Repeat for other maps (7410, 7412, 7339, 7340)
```

## Backend Files Status

### ✅ Complete
- `lib/db/schema/maps.ts` - Schema defined (needs update)
- `lib/services/map/map.service.ts` - Service complete
- `app/api/maps/[id]/route.ts` - API endpoint complete
- `drizzle/0001_solid_mephistopheles.sql` - Migration exists

### 🟡 Needs Update
- `scripts/seed-maps.ts` - Must match production schema

## Unity Files Status

### 🟡 Partial
- `Map/MapRenderer.cs` - Grid system exists, needs API fetch
- `UI/Screens/GameHUD.cs` - Hooks exist, needs implementation

### ❌ Needs Creation
- `Models/MapDataResponse.cs` - Parse backend response

## Next Immediate Actions

1. **Check database for existing maps**
   ```bash
   # Query production database
   SELECT id, x, y, width, height FROM maps WHERE id IN (7411, 7410, 7412, 7339, 7340);
   ```

2. **Update seed script** to match production schema

3. **Run seed script** if maps don't exist

4. **Extract map graphics** using JPEXS

5. **Implement Unity integration**

## Adjacent Maps Logic

For our 5 test maps, the adjacent map connections (stored in cells or separate field):

```
Map 7411 (Center) at x=4, y=-18:
  - Top: 7339 (x=4, y=-19)
  - Bottom: 7340 (x=4, y=-17)
  - Left: 7410 (x=3, y=-18)
  - Right: 7412 (x=5, y=-18)

Map 7410 (Left) at x=3, y=-18:
  - Right: 7411

Map 7412 (Right) at x=5, y=-18:
  - Left: 7411

Map 7339 (Top) at x=4, y=-19:
  - Bottom: 7411

Map 7340 (Bottom) at x=4, y=-17:
  - Top: 7411
```

Adjacent maps are calculated from X,Y coordinates: if map A is at (4,-18) and map B is at (5,-18), then B is to the right of A.

## Progress Tracking

- Backend: 90% (needs schema adaptation)
- Database: 50% (table exists, needs seeding)
- Graphics: 0% (SWF files available, not extracted)
- Unity: 40% (structure exists, needs implementation)

**Overall: 45% Complete**

**Estimated Time to Playable Map: 4-6 hours**

## Success Criteria

- [ ] 5 maps seeded in database
- [ ] Map graphics extracted to Unity
- [ ] API returns map data correctly
- [ ] Unity renders map with 560 cells
- [ ] Character spawns on map
- [ ] Can transition between adjacent maps

---

**Last Updated:** November 19, 2025
**Phase:** Database Schema Adaptation
**Next Step:** Check existing maps and update seed script
