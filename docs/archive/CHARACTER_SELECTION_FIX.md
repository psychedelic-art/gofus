# Character Selection Screen Fix Documentation

## Problem Summary

The character selection screen had two major issues:

### Initial Issue (Resolved)
The screen was stuck displaying "Loading characters..." indefinitely. This was caused by:

1. **Missing Assets**: No character sprites existed in the Resources folder
2. **ClassSpriteManager Failures**: Unable to load non-existent sprites
3. **Backend Dependencies**: Screen failed when backend was unavailable
4. **No Fallback Mechanism**: No way to test without real assets or backend

### Current Issue (Now Fixed - Nov 19, 2024)
Characters were being created successfully but not appearing in the selection screen:

1. **Field Name Mismatch**: Backend database uses snake_case (class_id, map_id, cell_id) but Unity expects camelCase (classId, mapId, cellId)
2. **JSON Parsing Error**: Unity's JsonUtility couldn't map fields with different naming conventions
3. **Double-Wrapping Bug**: Unity was wrapping the already-formatted JSON response with an extra layer

## Solution Implemented

We've implemented a comprehensive solution with multiple fallback mechanisms to ensure the character selection screen always works.

### 1. Placeholder Asset System

**File**: `PlaceholderAssetGenerator.cs`

- Generates colored placeholder sprites for all 12 character classes
- Creates both character sprites (64x64) and icons (32x32)
- Each class has a unique color and simple stick figure representation
- Sprites are generated on-demand when real assets are missing

**Usage**:
```csharp
// Generate all placeholders
PlaceholderAssetGenerator.GenerateAllPlaceholders();

// Generate single placeholder
Sprite placeholder = PlaceholderAssetGenerator.GeneratePlaceholderSprite(classId);
```

### 2. Enhanced ClassSpriteManager

**File**: `ClassSpriteManager.cs` (Modified)

Changes:
- Automatically detects missing sprites
- Generates placeholders when sprites can't be loaded
- Provides fallback for both sprites and icons
- Logs detailed information about asset loading

Key Features:
- Attempts to load from multiple paths
- Falls back to placeholder generation
- Maintains sprite cache for performance
- Reports loading statistics

### 3. Mock Character Data System

**File**: `CharacterSelectionScreen.cs` (Modified)

The screen now supports three data sources:
1. **Live Backend**: Production server at gofus-backend.vercel.app
2. **Local Backend**: Development server at localhost:3000
3. **Mock Data**: Built-in test characters for offline testing

Mock Characters Created:
- TestFeca (Level 50) - Tank/Support
- TestSram (Level 30) - Stealth/DPS
- TestEni (Level 25) - Healer
- TestIop (Level 40) - Melee DPS

### 4. Improved Error Handling

The character selection screen now:
- Retries failed backend connections (3 attempts)
- Falls back to mock data after failures
- Shows clear status messages
- Handles missing JWT tokens gracefully
- Works offline with test data

### 5. Testing Framework

**File**: `CharacterSelectionTest.cs`

Comprehensive test suite that verifies:
- Placeholder generation
- ClassSpriteManager functionality
- Screen initialization
- Mock data loading
- Character selection interactions

## How to Use

### Testing with Mock Data

1. **Enable Mock Characters** (Unity Editor):
   ```
   Menu: GOFUS > Tests > Enable Mock Characters
   ```

2. **Or via Code**:
   ```csharp
   PlayerPrefs.SetInt("use_mock_characters", 1);
   PlayerPrefs.Save();
   ```

3. **Run Tests**:
   ```
   Menu: GOFUS > Tests > Test Character Selection
   ```

### Testing with Placeholders

1. **Generate Placeholders**:
   ```
   Menu: GOFUS > Asset Tools > Generate Placeholder Sprites
   ```

2. **Clear Placeholders** (to test regeneration):
   ```
   Menu: GOFUS > Asset Tools > Clear Placeholder Sprites
   ```

### Working with Real Assets

Once you have installed the prerequisites:

1. **Install JPEXS FFDec**:
   - Download from: https://github.com/jindrapetrik/jpexs-decompiler/releases
   - Install to: `C:\Program Files\FFDec\`

2. **Install Dofus Client**:
   - Download from: https://www.dofus.com/en/download
   - Install to: `C:\Program Files (x86)\Dofus`

3. **Extract Assets**:
   ```batch
   cd gofus-client\Assets\_Project\Scripts\Extraction
   extract_priority_assets.bat
   ```

4. **Import to Unity**:
   - Assets will be in: `ExtractedAssets\Priority\`
   - Use Unity's import tools to process them

## Current State

The character selection screen now:
- ✅ Loads without crashing
- ✅ Displays test characters with placeholder sprites
- ✅ Allows character selection and interaction
- ✅ Shows appropriate status messages
- ✅ Handles backend failures gracefully
- ✅ Works offline with mock data

## Files Modified/Created

### Created Files:
1. `PlaceholderAssetGenerator.cs` - Generates placeholder sprites
2. `CharacterSelectionTest.cs` - Test suite for verification
3. `ASSET_EXTRACTION_PLAN.md` - Comprehensive extraction guide
4. `CHARACTER_SELECTION_FIX.md` - This documentation

### Modified Files (Initial Fix):
1. `ClassSpriteManager.cs` - Added placeholder generation fallback
2. `CharacterSelectionScreen.cs` - Added mock data support and better error handling

### Modified Files (Nov 19, 2024 - Field Mapping Fix):
1. `gofus-backend/lib/services/character/character.service.ts` - Added DTO transformation to convert snake_case to camelCase
   - Added `getClassName()` method to return class names
   - Added `getClassDescription()` method to return class descriptions
   - Modified `getAccountCharacters()` to transform database records to API format
2. `gofus-client/Assets/_Project/Scripts/UI/Screens/CharacterSelectionScreen.cs` - Fixed JSON parsing
   - Removed double-wrapping of JSON response
   - Simplified error handling
   - Now correctly parses `{ "characters": [...] }` format

## Testing Results

When you run the character selection screen now, you should see:

1. **Initial Load**: "Loading test characters..." message
2. **Character Display**: 4 test characters with colored placeholder sprites
3. **Selection**: Clicking a character highlights it
4. **Play Button**: Becomes enabled when a character is selected
5. **Status Messages**: Clear feedback about the current state

## Next Steps

### Immediate (Already Completed):
- ✅ Fix loading stuck issue
- ✅ Create placeholder assets
- ✅ Implement mock data
- ✅ Add error handling
- ✅ Create test framework

### Short-term (To Do):
1. Install JPEXS FFDec and Dofus
2. Run `extract_priority_assets.bat`
3. Process extracted assets for Unity
4. Replace placeholders with real sprites
5. Test with actual backend connection

### Long-term:
1. Extract all 18 character classes
2. Complete UI asset extraction
3. Implement character creation screen
4. Add animation support
5. Integrate with game world

## Troubleshooting

### Screen Still Shows "Loading..."
1. Check Unity Console for errors
2. Ensure PlaceholderAssetGenerator.cs is compiled
3. Try enabling mock characters via menu
4. Clear PlayerPrefs and restart

### No Sprites Visible
1. Generate placeholders: `GOFUS > Asset Tools > Generate Placeholder Sprites`
2. Check Resources folder structure
3. Verify ClassSpriteManager is initialized
4. Check console for sprite loading errors

### Backend Connection Issues
1. The screen will automatically fall back to mock data
2. Check internet connection
3. Verify JWT token exists (if using real backend)
4. Try local backend option

### Test Failures
1. Run tests individually to isolate issues
2. Check console output for specific error messages
3. Ensure all scripts are compiled without errors
4. Try clearing and regenerating placeholders

## Performance Notes

The current implementation:
- Generates placeholders on first run (one-time cost)
- Caches all sprites in memory
- Mock data loads instantly
- Backend timeout set to 10 seconds
- Retry delay is 2 seconds

## Unity Menu Commands

All functionality is accessible via Unity menus:

```
GOFUS/
├── Asset Tools/
│   ├── Generate Placeholder Sprites
│   └── Clear Placeholder Sprites
├── Tests/
│   ├── Test Character Selection
│   ├── Enable Mock Characters
│   └── Disable Mock Characters
└── Asset Migration/
    └── [Future extraction tools]
```

## Summary

The character selection screen is now fully functional with:
- Placeholder sprite generation for immediate testing
- Mock character data for offline development
- Graceful fallback mechanisms
- Comprehensive error handling
- Clear status messaging
- Full test coverage

The screen will work immediately with placeholder assets and mock data, while being ready to use real assets once they're extracted from Dofus.

---

*Documentation Version: 1.0*
*Last Updated: November 2024*
*Author: GOFUS Development Team*