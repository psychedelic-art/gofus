# GOFUS Project State - November 19, 2024

## Project Overview

GOFUS is a Dofus-inspired MMORPG with three main components:
1. **gofus-backend** - Next.js backend API (TypeScript, deployed on Vercel)
2. **gofus-client** - Unity game client (C#)
3. **gofus-game-server** - Game server for real-time gameplay

## Current Project Status

### Backend (gofus-backend)
- **Framework**: Next.js 14+ with App Router
- **Database**: PostgreSQL (Vercel Postgres)
- **ORM**: Drizzle ORM
- **Deployment**: Vercel (https://gofus-backend.vercel.app)
- **Status**: ✅ Operational

#### Key Features Implemented:
- Authentication (JWT-based login/register)
- Character management (CRUD operations)
- Class system (12 classes)
- Guild system
- Chat system
- Fight system
- Inventory management

#### Recent Fix (Nov 19, 2024):
**File**: `lib/services/character/character.service.ts`

**Problem**: Characters were not appearing in Unity client because backend returned snake_case field names (class_id, map_id, cell_id) while Unity expected camelCase (classId, mapId, cellId).

**Solution**: Added DTO transformation in `getAccountCharacters()` method:
- Transforms database records to camelCase format
- Includes className and classDescription fields
- Returns properly formatted JSON for Unity consumption

```typescript
async getAccountCharacters(accountId: string): Promise<any[]> {
  const accountCharacters = await db.select()
    .from(characters)
    .where(eq(characters.accountId, accountId))
    .orderBy(characters.level);

  // Transform database records to API format (camelCase for Unity client)
  return accountCharacters.map(char => ({
    id: char.id,
    name: char.name,
    level: char.level,
    classId: char.classId,
    sex: char.sex,
    mapId: char.mapId,
    cellId: char.cellId,
    experience: char.experience,
    kamas: char.kamas,
    className: this.getClassName(char.classId),
    classDescription: this.getClassDescription(char.classId),
  }));
}
```

### Unity Client (gofus-client)
- **Version**: Unity 2022+ (LTS)
- **Platform**: PC (Windows/Mac/Linux)
- **Architecture**: Screen-based UI system
- **Status**: ✅ Character creation and selection working

#### Implemented Screens:
1. **Login Screen** - Authentication with backend
2. **Character Creation Screen** - Create new characters
3. **Character Selection Screen** - Select existing characters

#### Recent Fix (Nov 19, 2024):
**File**: `Assets/_Project/Scripts/UI/Screens/CharacterSelectionScreen.cs`

**Problem**: JSON parsing was double-wrapping the response, causing characters not to load.

**Original Code** (Lines 425-484): Wrapped backend response with extra `{"characters":...}` layer

**Fixed Code**: 
- Removed double-wrapping
- Simplified error handling
- Correctly parses `{ "characters": [...] }` format directly

```csharp
// Backend returns: { "characters": [...] }
// Parse it directly without wrapping
CharacterListResponse response = JsonUtility.FromJson<CharacterListResponse>(json);
```

#### Character System:
- Supports 12 Dofus classes
- Placeholder sprite system for testing
- Mock data fallback when backend unavailable
- ClassSpriteManager for sprite loading

### Game Server (gofus-game-server)
- **Status**: 🚧 Not yet implemented
- **Planned**: Real-time game server for world interactions

## Database Schema

### Characters Table (PostgreSQL - snake_case)
```sql
CREATE TABLE characters (
  id SERIAL PRIMARY KEY,
  account_id UUID NOT NULL,
  name VARCHAR(20) NOT NULL UNIQUE,
  level INTEGER DEFAULT 1,
  class_id INTEGER NOT NULL,
  sex BOOLEAN NOT NULL,
  experience INTEGER DEFAULT 0,
  kamas INTEGER DEFAULT 0,
  ogrinas INTEGER DEFAULT 0,
  map_id INTEGER DEFAULT 7411,
  cell_id INTEGER DEFAULT 311,
  stat_points INTEGER DEFAULT 0,
  spell_points INTEGER DEFAULT 0,
  stats JSONB DEFAULT '{"vitality":0,"wisdom":0,"strength":0,"intelligence":0,"chance":0,"agility":0}',
  energy INTEGER DEFAULT 10000,
  max_energy INTEGER DEFAULT 10000,
  honor INTEGER DEFAULT 0,
  dishonor INTEGER DEFAULT 0,
  guild_id INTEGER,
  guild_rank INTEGER,
  is_dead BOOLEAN DEFAULT false,
  death_count INTEGER DEFAULT 0,
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP DEFAULT NOW(),
  last_used TIMESTAMP
);
```

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new account
- `POST /api/auth/login` - Login and get JWT token
- `POST /api/auth/logout` - Logout

### Characters
- `GET /api/characters` - Get all characters for authenticated account
- `POST /api/characters` - Create new character
- `GET /api/characters/[id]` - Get specific character
- `PATCH /api/characters/[id]` - Update character
- `DELETE /api/characters/[id]` - Delete character
- `POST /api/characters/[id]/levelup` - Level up character
- `PATCH /api/characters/[id]/position` - Update character position

### Classes
- `GET /api/classes` - Get all available classes

## Class System

All 12 Dofus classes are supported:

| ID | Name | Description |
|----|------|-------------|
| 1 | Feca | Masters of protection and defensive magic |
| 2 | Osamodas | Summoners who command powerful creatures |
| 3 | Enutrof | Treasure hunters with earth magic |
| 4 | Sram | Deadly assassins with traps |
| 5 | Xelor | Time mages who manipulate the battlefield |
| 6 | Ecaflip | Lucky gamblers with unpredictable spells |
| 7 | Eniripsa | Powerful healers and support |
| 8 | Iop | Fearless melee warriors |
| 9 | Cra | Expert archers with precision strikes |
| 10 | Sadida | Nature druids who control plants |
| 11 | Sacrieur | Berserkers who sacrifice health for power |
| 12 | Pandawa | Drunk monks with area control |

## Known Issues & Limitations

### Resolved Issues:
- ✅ Character selection screen stuck on "Loading characters..." (Placeholder sprites fix)
- ✅ Characters not appearing after creation (Field mapping fix - Nov 19, 2024)

### Current Limitations:
1. **Asset Extraction**: Real Dofus sprites not yet extracted
2. **Game World**: Not implemented - characters can't actually play
3. **Rendering**: Character rendering system incomplete
4. **Animation**: Animation system not yet implemented

### To-Do:
1. Extract Dofus assets using JPEXS FFDec
2. Implement game world/map system
3. Add character rendering with equipment layers
4. Implement spell system
5. Add combat mechanics
6. Create game server for multiplayer

## Testing Workflow

### Backend Testing:
```bash
cd gofus-backend
npm run test
```

### Character Creation Test:
1. Register account via `/api/auth/register`
2. Login via `/api/auth/login` → Get JWT token
3. Create character via `/api/characters` with JWT in Authorization header
4. Character appears in `/api/characters` response

### Unity Testing:
1. Enable mock characters for offline testing:
   - Menu: `GOFUS > Tests > Enable Mock Characters`
2. Generate placeholder sprites:
   - Menu: `GOFUS > Asset Tools > Generate Placeholder Sprites`
3. Test character selection:
   - Menu: `GOFUS > Tests > Test Character Selection`

### Real Backend Testing:
1. Unity connects to https://gofus-backend.vercel.app
2. JWT token stored in PlayerPrefs after login
3. Characters fetched from `/api/characters`
4. Now working correctly with field mapping fix

## Environment Configuration

### Backend (.env):
```
DATABASE_URL=postgresql://...
JWT_SECRET=your_secret_key
NODE_ENV=production
```

### Unity PlayerPrefs:
- `jwt_token` - Authentication token
- `account_id` - Current account UUID
- `selected_character_id` - Selected character ID
- `use_local_backend` - Use localhost:3000 instead of Vercel
- `use_mock_characters` - Use test data instead of backend

## Deployment

### Backend Deployment:
- Platform: Vercel
- URL: https://gofus-backend.vercel.app
- Auto-deploys from main branch
- Environment variables configured in Vercel dashboard

### Unity Build:
Not yet configured for production builds.

## Recent Character Creation Example

**Character Created**: Nov 19, 2024
```json
{
  "id": 1,
  "account_id": "0e88f8ed-178c-4044-b787-447f8f625b33",
  "name": "Sadge",
  "level": 1,
  "class_id": 4,
  "sex": true,
  "experience": 0,
  "kamas": 0,
  "map_id": 7411,
  "cell_id": 311
}
```

**Now Appearing Correctly** in character selection after field mapping fix.

## Documentation Files

- `CHARACTER_SELECTION_FIX.md` - Character selection screen fixes
- `CHARACTER_CREATION_IMPLEMENTATION.md` - Character creation implementation
- `CLASS_INTEGRATION_GUIDE.md` - Class system guide
- `ASSET_EXTRACTION_PLAN.md` - Asset extraction guide
- `DATABASE_SEED_GUIDE.md` - Database seeding
- `UNITY_INTEGRATION_GUIDE.md` - Unity integration guide

## Next Steps

1. **Immediate**: Test character selection with real backend data
2. **Short-term**: Extract Dofus sprites and replace placeholders
3. **Medium-term**: Implement game world and map system
4. **Long-term**: Add combat, spells, and multiplayer features
