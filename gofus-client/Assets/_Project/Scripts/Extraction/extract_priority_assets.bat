@echo off
setlocal enabledelayedexpansion

:: ========================================
:: GOFUS Priority Asset Extraction
:: Quick extraction of minimum viable assets
:: ========================================

echo.
echo ================================================================
echo    GOFUS Priority Asset Extraction
echo    Minimum Viable Client Assets
echo ================================================================
echo.

set "SCRIPT_DIR=%~dp0"
set "FFDEC_PATH=C:\Program Files\FFDec\ffdec.jar"
set "DOFUS_PATH=C:\Program Files (x86)\Dofus"
set "OUTPUT_PATH=%SCRIPT_DIR%ExtractedAssets\Priority"

if not "%1"=="" set "FFDEC_PATH=%~1"
if not "%2"=="" set "DOFUS_PATH=%~2"

echo Configuration:
echo   FFDec:  %FFDEC_PATH%
echo   Dofus:  %DOFUS_PATH%
echo   Output: %OUTPUT_PATH%
echo.

:: Create directories
mkdir "%OUTPUT_PATH%\Characters" 2>nul
mkdir "%OUTPUT_PATH%\UI" 2>nul
mkdir "%OUTPUT_PATH%\Maps" 2>nul
mkdir "%OUTPUT_PATH%\Effects" 2>nul
mkdir "%OUTPUT_PATH%\Audio" 2>nul

:: Priority characters (4 classes for MVP)
echo ========================================
echo Priority 1: Core Character Classes
echo ========================================
echo.

set "CHAR_BASE=%DOFUS_PATH%\content\gfx\sprites\actors\characters"

:: Feca (ID: 01)
echo [1/4] Extracting Feca...
if exist "%CHAR_BASE%\01.swf" (
    java -jar "%FFDEC_PATH%" -export image,shape,sprite "%OUTPUT_PATH%\Characters\Feca" "%CHAR_BASE%\01.swf" -format image:png,shape:svg -onerror ignore
    echo   Done.
) else (
    echo   Not found: %CHAR_BASE%\01.swf
)

:: Sram (ID: 04)
echo [2/4] Extracting Sram...
if exist "%CHAR_BASE%\04.swf" (
    java -jar "%FFDEC_PATH%" -export image,shape,sprite "%OUTPUT_PATH%\Characters\Sram" "%CHAR_BASE%\04.swf" -format image:png,shape:svg -onerror ignore
    echo   Done.
) else (
    echo   Not found: %CHAR_BASE%\04.swf
)

:: Eniripsa (ID: 07)
echo [3/4] Extracting Eniripsa...
if exist "%CHAR_BASE%\07.swf" (
    java -jar "%FFDEC_PATH%" -export image,shape,sprite "%OUTPUT_PATH%\Characters\Eniripsa" "%CHAR_BASE%\07.swf" -format image:png,shape:svg -onerror ignore
    echo   Done.
) else (
    echo   Not found: %CHAR_BASE%\07.swf
)

:: Iop (ID: 08)
echo [4/4] Extracting Iop...
if exist "%CHAR_BASE%\08.swf" (
    java -jar "%FFDEC_PATH%" -export image,shape,sprite "%OUTPUT_PATH%\Characters\Iop" "%CHAR_BASE%\08.swf" -format image:png,shape:svg -onerror ignore
    echo   Done.
) else (
    echo   Not found: %CHAR_BASE%\08.swf
)

:: Priority UI elements
echo.
echo ========================================
echo Priority 2: Essential UI Elements
echo ========================================
echo.

set "UI_BASE=%DOFUS_PATH%\content\gfx\ui"

echo Extracting core UI elements...

if exist "%UI_BASE%\buttons.swf" (
    echo   - Buttons...
    java -jar "%FFDEC_PATH%" -export image,shape "%OUTPUT_PATH%\UI\Buttons" "%UI_BASE%\buttons.swf" -format image:png -onerror ignore
)

if exist "%UI_BASE%\cursor.swf" (
    echo   - Cursor...
    java -jar "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\UI\Cursor" "%UI_BASE%\cursor.swf" -format image:png -onerror ignore
)

if exist "%UI_BASE%\windows.swf" (
    echo   - Windows...
    java -jar "%FFDEC_PATH%" -export image,shape,sprite "%OUTPUT_PATH%\UI\Windows" "%UI_BASE%\windows.swf" -format image:png -onerror ignore
)

if exist "%UI_BASE%\icons.swf" (
    echo   - Icons...
    java -jar "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\UI\Icons" "%UI_BASE%\icons.swf" -format image:png -onerror ignore
)

echo   Done.

:: Priority map tiles
echo.
echo ========================================
echo Priority 3: Basic Map Tiles
echo ========================================
echo.

set "MAP_BASE=%DOFUS_PATH%\content\gfx\maps\tiles"

echo Extracting essential map tiles...

:: Extract first 10 tile files for MVP
set COUNT=0
for %%f in ("%MAP_BASE%\*.swf") do (
    if !COUNT! lss 10 (
        echo   - %%~nf...
        java -jar "%FFDEC_PATH%" -export image,shape "%OUTPUT_PATH%\Maps\Tiles" "%%f" -format image:png -onerror ignore
        set /a COUNT+=1
    )
)

echo   Done. Extracted !COUNT! tile sets.

:: Priority effects
echo.
echo ========================================
echo Priority 4: Basic Combat Effects
echo ========================================
echo.

set "FX_BASE=%DOFUS_PATH%\content\gfx\effects"

echo Extracting essential effects...

:: Damage and heal effects are highest priority
if exist "%FX_BASE%\spells\damage.swf" (
    echo   - Damage effect...
    java -jar "%FFDEC_PATH%" -export image,shape,sprite "%OUTPUT_PATH%\Effects\Damage" "%FX_BASE%\spells\damage.swf" -format image:png -onerror ignore
)

if exist "%FX_BASE%\spells\heal.swf" (
    echo   - Heal effect...
    java -jar "%FFDEC_PATH%" -export image,shape,sprite "%OUTPUT_PATH%\Effects\Heal" "%FX_BASE%\spells\heal.swf" -format image:png -onerror ignore
)

echo   Done.

:: Priority audio
echo.
echo ========================================
echo Priority 5: Core Audio
echo ========================================
echo.

set "AUDIO_BASE=%DOFUS_PATH%\content\sounds"

echo Extracting essential audio...

:: UI sounds are highest priority
if exist "%AUDIO_BASE%\ui" (
    set AUDIO_COUNT=0
    for %%f in ("%AUDIO_BASE%\ui\*.swf") do (
        if !AUDIO_COUNT! lss 5 (
            echo   - %%~nf...
            java -jar "%FFDEC_PATH%" -export sound "%OUTPUT_PATH%\Audio" "%%f" -format sound:mp3 -onerror ignore
            set /a AUDIO_COUNT+=1
        )
    )
    echo   Done. Extracted !AUDIO_COUNT! audio files.
) else (
    echo   UI audio directory not found.
)

:: Generate priority asset list
echo.
echo ========================================
echo Generating Priority Asset List
echo ========================================
echo.

(
echo GOFUS Priority Asset Extraction
echo ================================
echo Extraction Date: %date% %time%
echo.
echo This extraction includes the minimum viable assets needed for:
echo - Basic client functionality
echo - Character selection and creation
echo - Simple movement and combat
echo - Essential UI interaction
echo.
echo Assets Extracted:
echo.
echo 1. CHARACTERS (Priority Classes^)
echo    - Feca      (Tank/Support^)
echo    - Sram      (Stealth/DPS^)
echo    - Eniripsa  (Healer^)
echo    - Iop       (Melee DPS^)
echo.
echo 2. UI ELEMENTS
echo    - Buttons (all states^)
echo    - Windows (frames and panels^)
echo    - Icons (items, skills^)
echo    - Cursor
echo.
echo 3. MAP ASSETS
echo    - 10 essential tile sets
echo    - Basic ground textures
echo.
echo 4. EFFECTS
echo    - Damage indicators
echo    - Heal effects
echo.
echo 5. AUDIO
echo    - 5 core UI sounds
echo.
echo Next Steps:
echo -----------
echo 1. Verify extraction in: %OUTPUT_PATH%
echo 2. Run full extraction for remaining assets: extract_dofus_assets_complete.bat
echo 3. Import into Unity using Asset Migration tools
echo.
echo Missing Assets for Full Client:
echo --------------------------------
echo - 14 additional character classes
echo - Advanced spell effects
echo - Monster sprites
echo - Full item database
echo - Complete map tile library
echo - Background music
echo - NPC sprites
echo.
echo Run extract_dofus_assets_complete.bat for full extraction.
echo.
) > "%OUTPUT_PATH%\PRIORITY_ASSETS_README.txt"

echo.
echo ================================================================
echo    Priority Extraction Complete!
echo ================================================================
echo.
echo Assets extracted to: %OUTPUT_PATH%
echo.
echo These assets provide the minimum viable functionality.
echo For complete assets, run: extract_dofus_assets_complete.bat
echo.
echo Next Steps:
echo 1. Review extracted assets in %OUTPUT_PATH%
echo 2. Open Unity and import using GOFUS Asset Migration tools
echo 3. Run full extraction when ready for additional content
echo.
pause
