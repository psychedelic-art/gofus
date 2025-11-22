# Database Seed Guide - Fix Missing Classes and Spells

**Issue**: Character Creation screen shows empty class buttons or no spells
**Cause**: Database not seeded with class and spell data
**Solution**: Run the seed file to populate the database

---

## ✅ **Fixed Issues**

I've already fixed the Unity code to handle empty data better:
1. ✅ Added empty array check - now falls back to mock data if backend returns empty
2. ✅ Enhanced mock data - includes stats and 3 spells per class for offline testing

**BUT** you should still seed the database to get the real data!

---

## 🗄️ **Database Seeding**

### Option 1: Using PostgreSQL Client (Recommended)

If you have `psql` installed:

```bash
# Navigate to backend folder
cd C:\Users\HardM\Desktop\Enterprise\gofus\gofus-backend

# Run seed file (replace with your actual DATABASE_URL)
psql "postgresql://username:password@host:port/database" -f drizzle/seed_classes_and_spells.sql
```

**OR if you have DATABASE_URL environment variable:**

```bash
psql $DATABASE_URL -f drizzle/seed_classes_and_spells.sql
```

### Option 2: Using Vercel Postgres Dashboard

1. Go to your Vercel dashboard: https://vercel.com/dashboard
2. Select your `gofus-backend` project
3. Go to **Storage** → **Postgres** → Your database
4. Click **Query** tab
5. Copy the entire contents of `drizzle/seed_classes_and_spells.sql`
6. Paste into the query editor
7. Click **Run Query**

### Option 3: Using Node.js Script

Create a file `scripts/seed-db.ts`:

```typescript
import { db } from '@/lib/db';
import { readFileSync } from 'fs';
import { join } from 'path';

async function seedDatabase() {
  console.log('Seeding database...');

  const seedSQL = readFileSync(
    join(process.cwd(), 'drizzle', 'seed_classes_and_spells.sql'),
    'utf-8'
  );

  await db.execute(seedSQL);

  console.log('Database seeded successfully!');
  process.exit(0);
}

seedDatabase().catch(console.error);
```

Then run:
```bash
npx tsx scripts/seed-db.ts
```

---

## 📊 **What Gets Seeded**

The seed file (`drizzle/seed_classes_and_spells.sql`) contains:

### **All 12 Classes:**
1. Feca - Tank/Support
2. Osamodas - Summoner
3. Enutrof - Treasure Hunter
4. Sram - Assassin
5. Xelor - Time Mage
6. Ecaflip - Gambler
7. Eniripsa - Healer
8. Iop - Warrior
9. Cra - Archer
10. Sadida - Nature Mage
11. Sacrieur - Berserker
12. Pandawa - Brawler

### **For Each Class:**
- ✅ Name and description
- ✅ Stats per level (Vitality, Wisdom, Strength, Intelligence, Chance, Agility)
- ✅ Starting map and cell location
- ✅ 3 Starting spells
- ✅ All available spells (5-10 spells per class)

### **60+ Spells Total:**
- Damage spells (fire, water, earth, air, neutral)
- Healing spells
- Shields and defensive abilities
- Summons and traps
- Buffs and debuffs
- Teleportation and movement
- Special effects (invisibility, freezing, etc.)

---

## 🧪 **Verify Database Seeding**

After running the seed file, verify the data:

### **Check Classes:**
```sql
SELECT id, name, description FROM classes ORDER BY id;
```

Should return 12 rows (Feca through Pandawa).

### **Check Spells:**
```sql
SELECT COUNT(*) FROM spells;
```

Should return 60+ spells.

### **Check Specific Class Data:**
```sql
SELECT * FROM classes WHERE id = 1;
```

Should show Feca with stats_per_level JSON.

---

## 🎮 **Testing in Unity**

### **Before Seeding (Mock Data):**
1. Open Unity project
2. Play scene
3. Navigate to Character Creation
4. **Expected**: Status message shows "Using mock class data (backend unavailable)"
5. **Expected**: Classes show with placeholder spells ("Feca Strike", "Feca Shield", etc.)

### **After Seeding (Real Data):**
1. Run seed file on database
2. Restart Unity (if needed)
3. Play scene
4. Navigate to Character Creation
5. **Expected**: Status message shows "Loaded 12 classes"
6. **Expected**: Classes show with real spells from database
7. **Verify**: Click each class and see unique spells with proper:
   - Spell names (Staff Strike, Blazing Glyph, etc.)
   - AP costs (3 AP, 4 AP, etc.)
   - Ranges (1-3, 2-6, etc.)
   - Descriptions (actual spell effects)
   - Element colors (fire=red, water=blue, etc.)

---

## 🔍 **Debug Console Logs**

### **Successful Backend Load:**
```
[CharacterCreation] Loading classes from: https://gofus-backend.vercel.app/api/classes
[CharacterCreation] Classes response: {"classes":[...], "totalClasses":12}
[CharacterCreation] Successfully loaded 12 classes from backend
```

### **Empty Database (Falls back to mock):**
```
[CharacterCreation] Loading classes from: https://gofus-backend.vercel.app/api/classes
[CharacterCreation] Classes response: {"classes":[], "totalClasses":0}
[CharacterCreation] Backend returned empty classes array. Using mock data.
[CharacterCreation] Loading mock class data. Classes and spells shown are placeholders.
```

### **Network Error (Falls back to mock):**
```
[CharacterCreation] Failed to load classes: Connection timeout
[CharacterCreation] Loading mock class data. Classes and spells shown are placeholders.
```

---

## ⚠️ **Common Issues**

### **Issue 1: "relation 'classes' does not exist"**
**Cause**: Database migrations not run
**Fix**:
```bash
cd gofus-backend
npm run db:push
# OR
npx drizzle-kit push
```

### **Issue 2: "permission denied"**
**Cause**: Database user doesn't have INSERT permissions
**Fix**: Grant permissions or use admin user

### **Issue 3: "duplicate key value violates unique constraint"**
**Cause**: Seed file already run
**Fix**: Clear existing data first:
```sql
DELETE FROM spells;
DELETE FROM classes;
```
Then re-run seed file.

### **Issue 4: Unity still shows mock data after seeding**
**Possible causes**:
1. Unity is using local backend (`localhost:3000`) instead of production
   - **Fix**: Check PlayerPrefs `use_local_backend` setting
2. Backend deployment hasn't updated
   - **Fix**: Redeploy backend to Vercel
3. Browser/Unity cache
   - **Fix**: Restart Unity and clear cache

---

## 📝 **Alternative: Use Mock Data Only**

If you don't want to seed the database yet, the enhanced mock data will work fine for testing:

**What you get with mock data:**
- ✅ All 12 classes with names and descriptions
- ✅ Stats per level for each class
- ✅ 3 placeholder spells per class
- ✅ Fully functional character creation
- ⚠️ Spells are generic ("Feca Strike", "Feca Shield", etc.)
- ⚠️ Not production-ready

**What you get with seeded database:**
- ✅ Everything above PLUS
- ✅ Real spell names from Dofus (Staff Strike, Blazing Glyph, Summon Tofu, etc.)
- ✅ Accurate spell effects and descriptions
- ✅ Proper element types and damage values
- ✅ Cooldowns, ranges, and special effects
- ✅ Production-ready data

---

## 🚀 **Next Steps**

1. **Run seed file** (choose one of the 3 options above)
2. **Verify data** in database
3. **Test in Unity** - should see real spells now
4. **Create a character** with proper class and spells
5. **Proceed to GameHUD implementation** (next major milestone)

---

## 📚 **Related Documentation**

- `CHARACTER_CREATION_IMPLEMENTATION.md` - Character creation system details
- `CURRENT_STATE.md` - Complete project status
- `NEXT_IMPLEMENTATION_SCREENS.md` - GameHUD implementation guide

---

**Last Updated**: November 18, 2025
**Status**: ✅ Seed file exists and ready to use
**Unity Code**: ✅ Fixed to handle empty data gracefully
