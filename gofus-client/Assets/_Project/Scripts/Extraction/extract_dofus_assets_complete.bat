@echo off
setlocal enabledelayedexpansion

:: ========================================
:: GOFUS Complete Asset Extraction Tool
:: Comprehensive Dofus Asset Extractor using JPEXS FFDec
:: ========================================

echo.
echo ================================================================
echo    GOFUS Complete Asset Extraction Tool
echo    Extracts all Dofus assets using JPEXS FFDec
echo ================================================================
echo.

:: Configuration
set "SCRIPT_DIR=%~dp0"
set "TIMESTAMP=%date:~-4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%"
set "TIMESTAMP=%TIMESTAMP: =0%"

:: Default paths - User can override via command line
set "FFDEC_PATH=C:\Program Files\FFDec\ffdec.jar"
set "DOFUS_PATH=C:\Program Files (x86)\Dofus"
set "OUTPUT_PATH=%SCRIPT_DIR%ExtractedAssets\%TIMESTAMP%"
set "LOG_PATH=%OUTPUT_PATH%\logs"

:: Process command line arguments
if not "%1"=="" set "FFDEC_PATH=%~1"
if not "%2"=="" set "DOFUS_PATH=%~2"
if not "%3"=="" set "OUTPUT_PATH=%~3"

:: Create log directory
mkdir "%LOG_PATH%" 2>nul

:: Start logging
set "MAIN_LOG=%LOG_PATH%\extraction_main.log"
call :LOG "========================================="
call :LOG "Extraction started at %date% %time%"
call :LOG "========================================="
call :LOG "FFDec Path: %FFDEC_PATH%"
call :LOG "Dofus Path: %DOFUS_PATH%"
call :LOG "Output Path: %OUTPUT_PATH%"
call :LOG ""

:: Display configuration
echo Configuration:
echo   FFDec:  %FFDEC_PATH%
echo   Dofus:  %DOFUS_PATH%
echo   Output: %OUTPUT_PATH%
echo   Logs:   %LOG_PATH%
echo.

:: Validate paths
call :VALIDATE_PATHS
if errorlevel 1 (
    echo.
    echo ERROR: Validation failed. See log for details.
    pause
    exit /b 1
)

:: Create directory structure
call :LOG "Creating directory structure..."
call :CREATE_DIRECTORIES

:: Initialize statistics
set TOTAL_FILES=0
set SUCCESS_COUNT=0
set FAILED_COUNT=0
set SKIPPED_COUNT=0

:: Start extraction
echo.
echo ================================================================
echo Starting asset extraction...
echo ================================================================
echo.

:: Phase 1: Character Sprites
call :LOG ""
call :LOG "PHASE 1: Extracting Character Sprites"
call :LOG "======================================="
call :EXTRACT_CHARACTERS

:: Phase 2: UI Elements
call :LOG ""
call :LOG "PHASE 2: Extracting UI Elements"
call :LOG "======================================="
call :EXTRACT_UI

:: Phase 3: Map Assets
call :LOG ""
call :LOG "PHASE 3: Extracting Map Assets"
call :LOG "======================================="
call :EXTRACT_MAPS

:: Phase 4: Effects and Particles
call :LOG ""
call :LOG "PHASE 4: Extracting Effects and Particles"
call :LOG "======================================="
call :EXTRACT_EFFECTS

:: Phase 5: Audio Files
call :LOG ""
call :LOG "PHASE 5: Extracting Audio Files"
call :LOG "======================================="
call :EXTRACT_AUDIO

:: Phase 6: Items and Icons
call :LOG ""
call :LOG "PHASE 6: Extracting Items and Icons"
call :LOG "======================================="
call :EXTRACT_ITEMS

:: Generate summary report
call :GENERATE_REPORT

:: Complete
echo.
echo ================================================================
echo    Extraction Complete!
echo ================================================================
echo.
echo Statistics:
echo   Total Files Processed: %TOTAL_FILES%
echo   Successfully Extracted: %SUCCESS_COUNT%
echo   Failed: %FAILED_COUNT%
echo   Skipped: %SKIPPED_COUNT%
echo.
echo Output Location: %OUTPUT_PATH%
echo Full Report: %LOG_PATH%\extraction_report.txt
echo.
pause
exit /b 0

:: ========================================
:: SUBROUTINES
:: ========================================

:VALIDATE_PATHS
call :LOG "Validating paths..."

:: Check FFDec
if exist "%FFDEC_PATH%" (
    call :LOG "  FFDec found: %FFDEC_PATH%"
) else (
    call :LOG "  ERROR: FFDec not found at %FFDEC_PATH%"
    echo ERROR: FFDec not found at %FFDEC_PATH%
    echo.
    echo Please install JPEXS FFDec from:
    echo https://github.com/jindrapetrik/jpexs-decompiler/releases
    echo.
    echo Or specify the correct path:
    echo   %~nx0 "C:\path\to\ffdec.jar" "C:\path\to\Dofus"
    exit /b 1
)

:: Check Dofus installation
if exist "%DOFUS_PATH%" (
    call :LOG "  Dofus installation found: %DOFUS_PATH%"
) else (
    call :LOG "  WARNING: Dofus not found at %DOFUS_PATH%"
    echo WARNING: Dofus installation not found at %DOFUS_PATH%
    echo.
    echo Checking alternative paths...

    :: Try alternative locations
    if exist "%LOCALAPPDATA%\Dofus" (
        set "DOFUS_PATH=%LOCALAPPDATA%\Dofus"
        call :LOG "  Found alternative path: !DOFUS_PATH!"
        echo Found: !DOFUS_PATH!
    ) else if exist "C:\Dofus" (
        set "DOFUS_PATH=C:\Dofus"
        call :LOG "  Found alternative path: !DOFUS_PATH!"
        echo Found: !DOFUS_PATH!
    ) else (
        call :LOG "  ERROR: No Dofus installation found"
        echo ERROR: Could not locate Dofus installation
        echo Please specify the correct path as second argument
        exit /b 1
    )
)

:: Verify Java installation
java -version >nul 2>&1
if errorlevel 1 (
    call :LOG "  ERROR: Java not found in PATH"
    echo ERROR: Java is required to run FFDec
    echo Please install Java Runtime Environment (JRE) 8 or higher
    exit /b 1
) else (
    call :LOG "  Java runtime detected"
)

call :LOG "  All paths validated successfully"
exit /b 0

:CREATE_DIRECTORIES
mkdir "%OUTPUT_PATH%\Characters" 2>nul
mkdir "%OUTPUT_PATH%\UI\Buttons" 2>nul
mkdir "%OUTPUT_PATH%\UI\Windows" 2>nul
mkdir "%OUTPUT_PATH%\UI\Icons" 2>nul
mkdir "%OUTPUT_PATH%\UI\Cursors" 2>nul
mkdir "%OUTPUT_PATH%\UI\Backgrounds" 2>nul
mkdir "%OUTPUT_PATH%\Maps\Tiles" 2>nul
mkdir "%OUTPUT_PATH%\Maps\Objects" 2>nul
mkdir "%OUTPUT_PATH%\Maps\Interactive" 2>nul
mkdir "%OUTPUT_PATH%\Effects\Spells" 2>nul
mkdir "%OUTPUT_PATH%\Effects\Particles" 2>nul
mkdir "%OUTPUT_PATH%\Effects\Animations" 2>nul
mkdir "%OUTPUT_PATH%\Audio\Music" 2>nul
mkdir "%OUTPUT_PATH%\Audio\SFX" 2>nul
mkdir "%OUTPUT_PATH%\Audio\Ambiance" 2>nul
mkdir "%OUTPUT_PATH%\Items\Equipment" 2>nul
mkdir "%OUTPUT_PATH%\Items\Resources" 2>nul
mkdir "%OUTPUT_PATH%\Items\Consumables" 2>nul
mkdir "%OUTPUT_PATH%\Monsters" 2>nul
mkdir "%OUTPUT_PATH%\NPCs" 2>nul
call :LOG "  Directory structure created"
exit /b 0

:EXTRACT_CHARACTERS
echo [1/6] Extracting character sprites...

:: Dofus 2.0 has 18 character classes
:: Class IDs and names mapping
set "CLASS_01=Feca"
set "CLASS_02=Osamodas"
set "CLASS_03=Enutrof"
set "CLASS_04=Sram"
set "CLASS_05=Xelor"
set "CLASS_06=Ecaflip"
set "CLASS_07=Eniripsa"
set "CLASS_08=Iop"
set "CLASS_09=Cra"
set "CLASS_10=Sadida"
set "CLASS_11=Sacrier"
set "CLASS_12=Pandawa"
set "CLASS_13=Rogue"
set "CLASS_14=Masqueraider"
set "CLASS_15=Foggernaut"
set "CLASS_16=Eliotrope"
set "CLASS_17=Huppermage"
set "CLASS_18=Ouginak"

:: Character sprites are typically in: content/gfx/sprites/actors/characters/
set "CHAR_BASE=%DOFUS_PATH%\content\gfx\sprites\actors\characters"

:: Extract each character class
for /L %%i in (1,1,18) do (
    set "CLASS_NUM=%%i"
    set "CLASS_NUM=0!CLASS_NUM!"
    set "CLASS_NUM=!CLASS_NUM:~-2!"

    set "CLASS_NAME=!CLASS_%%i!"
    set "CHAR_FILE=!CHAR_BASE!\!CLASS_NUM!.swf"

    if exist "!CHAR_FILE!" (
        echo   Extracting !CLASS_NAME! (Class !CLASS_NUM!)...
        call :LOG "  Extracting !CLASS_NAME! from !CHAR_FILE!"

        call :EXTRACT_FILE "!CHAR_FILE!" "%OUTPUT_PATH%\Characters\!CLASS_NAME!" "image,shape,sprite"

        if !errorlevel! equ 0 (
            set /a SUCCESS_COUNT+=1
            call :LOG "    SUCCESS: !CLASS_NAME! extracted"
        ) else (
            set /a FAILED_COUNT+=1
            call :LOG "    FAILED: !CLASS_NAME! extraction failed"
        )
    ) else (
        call :LOG "  WARNING: Character file not found: !CHAR_FILE!"
        set /a SKIPPED_COUNT+=1
    )
    set /a TOTAL_FILES+=1
)

echo   Character extraction complete.
exit /b 0

:EXTRACT_UI
echo [2/6] Extracting UI elements...

:: UI paths in Dofus installation
set "UI_BASE=%DOFUS_PATH%\content\gfx\ui"

:: Extract buttons
if exist "%UI_BASE%\buttons.swf" (
    echo   Extracting buttons...
    call :EXTRACT_FILE "%UI_BASE%\buttons.swf" "%OUTPUT_PATH%\UI\Buttons" "image,shape"
    set /a TOTAL_FILES+=1
)

:: Extract windows/panels
for %%f in ("%UI_BASE%\*.swf") do (
    set "FILENAME=%%~nf"
    if /I "!FILENAME:~0,6!"=="window" (
        echo   Extracting !FILENAME!...
        call :EXTRACT_FILE "%%f" "%OUTPUT_PATH%\UI\Windows\!FILENAME!" "image,shape,sprite"
        set /a TOTAL_FILES+=1
    )
)

:: Extract icons
if exist "%UI_BASE%\icons" (
    for %%f in ("%UI_BASE%\icons\*.swf") do (
        echo   Extracting icons from %%~nf...
        call :EXTRACT_FILE "%%f" "%OUTPUT_PATH%\UI\Icons" "image"
        set /a TOTAL_FILES+=1
    )
)

:: Extract cursors
if exist "%UI_BASE%\cursors.swf" (
    echo   Extracting cursors...
    call :EXTRACT_FILE "%UI_BASE%\cursors.swf" "%OUTPUT_PATH%\UI\Cursors" "image"
    set /a TOTAL_FILES+=1
)

echo   UI extraction complete.
exit /b 0

:EXTRACT_MAPS
echo [3/6] Extracting map assets...

set "MAP_BASE=%DOFUS_PATH%\content\gfx\maps"

:: Extract map tiles
if exist "%MAP_BASE%\tiles" (
    echo   Extracting map tiles...
    for %%f in ("%MAP_BASE%\tiles\*.swf") do (
        call :EXTRACT_FILE "%%f" "%OUTPUT_PATH%\Maps\Tiles" "image,shape"
        set /a TOTAL_FILES+=1
    )
)

:: Extract map objects (trees, rocks, buildings, etc.)
if exist "%MAP_BASE%\objects" (
    echo   Extracting map objects...
    for %%f in ("%MAP_BASE%\objects\*.swf") do (
        set "OBJNAME=%%~nf"
        call :EXTRACT_FILE "%%f" "%OUTPUT_PATH%\Maps\Objects\!OBJNAME!" "image,shape,sprite"
        set /a TOTAL_FILES+=1
    )
)

:: Extract interactive elements
if exist "%MAP_BASE%\interactive" (
    echo   Extracting interactive elements...
    for %%f in ("%MAP_BASE%\interactive\*.swf") do (
        call :EXTRACT_FILE "%%f" "%OUTPUT_PATH%\Maps\Interactive" "image,shape,sprite"
        set /a TOTAL_FILES+=1
    )
)

echo   Map extraction complete.
exit /b 0

:EXTRACT_EFFECTS
echo [4/6] Extracting effects and particles...

set "FX_BASE=%DOFUS_PATH%\content\gfx\effects"

:: Extract spell effects
if exist "%FX_BASE%\spells" (
    echo   Extracting spell effects...
    for %%f in ("%FX_BASE%\spells\*.swf") do (
        set "SPELLNAME=%%~nf"
        call :EXTRACT_FILE "%%f" "%OUTPUT_PATH%\Effects\Spells\!SPELLNAME!" "image,shape,sprite,movie"
        set /a TOTAL_FILES+=1
    )
)

:: Extract particle effects
if exist "%FX_BASE%\particles" (
    echo   Extracting particle effects...
    for %%f in ("%FX_BASE%\particles\*.swf") do (
        call :EXTRACT_FILE "%%f" "%OUTPUT_PATH%\Effects\Particles" "image,shape,sprite"
        set /a TOTAL_FILES+=1
    )
)

:: Extract general animations
if exist "%FX_BASE%\animations" (
    echo   Extracting animations...
    for %%f in ("%FX_BASE%\animations\*.swf") do (
        call :EXTRACT_FILE "%%f" "%OUTPUT_PATH%\Effects\Animations" "image,shape,sprite,movie"
        set /a TOTAL_FILES+=1
    )
)

echo   Effects extraction complete.
exit /b 0

:EXTRACT_AUDIO
echo [5/6] Extracting audio files...

set "AUDIO_BASE=%DOFUS_PATH%\content\sounds"

:: Extract music
if exist "%AUDIO_BASE%\music" (
    echo   Extracting music...
    for %%f in ("%AUDIO_BASE%\music\*.swf") do (
        call :EXTRACT_FILE "%%f" "%OUTPUT_PATH%\Audio\Music" "sound"
        set /a TOTAL_FILES+=1
    )
)

:: Extract sound effects
if exist "%AUDIO_BASE%\sfx" (
    echo   Extracting sound effects...
    for %%f in ("%AUDIO_BASE%\sfx\*.swf") do (
        call :EXTRACT_FILE "%%f" "%OUTPUT_PATH%\Audio\SFX" "sound"
        set /a TOTAL_FILES+=1
    )
)

:: Extract ambiance
if exist "%AUDIO_BASE%\ambiance" (
    echo   Extracting ambiance...
    for %%f in ("%AUDIO_BASE%\ambiance\*.swf") do (
        call :EXTRACT_FILE "%%f" "%OUTPUT_PATH%\Audio\Ambiance" "sound"
        set /a TOTAL_FILES+=1
    )
)

echo   Audio extraction complete.
exit /b 0

:EXTRACT_ITEMS
echo [6/6] Extracting items and icons...

set "ITEM_BASE=%DOFUS_PATH%\content\gfx\items"

:: Extract equipment icons
if exist "%ITEM_BASE%\equipment" (
    echo   Extracting equipment icons...
    for %%f in ("%ITEM_BASE%\equipment\*.swf") do (
        call :EXTRACT_FILE "%%f" "%OUTPUT_PATH%\Items\Equipment" "image"
        set /a TOTAL_FILES+=1
    )
)

:: Extract resource icons
if exist "%ITEM_BASE%\resources" (
    echo   Extracting resource icons...
    for %%f in ("%ITEM_BASE%\resources\*.swf") do (
        call :EXTRACT_FILE "%%f" "%OUTPUT_PATH%\Items\Resources" "image"
        set /a TOTAL_FILES+=1
    )
)

:: Extract consumable icons
if exist "%ITEM_BASE%\consumables" (
    echo   Extracting consumable icons...
    for %%f in ("%ITEM_BASE%\consumables\*.swf") do (
        call :EXTRACT_FILE "%%f" "%OUTPUT_PATH%\Items\Consumables" "image"
        set /a TOTAL_FILES+=1
    )
)

:: Extract monster sprites
set "MONSTER_BASE=%DOFUS_PATH%\content\gfx\sprites\actors\monsters"
if exist "%MONSTER_BASE%" (
    echo   Extracting monster sprites...
    for %%f in ("%MONSTER_BASE%\*.swf") do (
        set "MONSTERNAME=%%~nf"
        call :EXTRACT_FILE "%%f" "%OUTPUT_PATH%\Monsters\!MONSTERNAME!" "image,shape,sprite"
        set /a TOTAL_FILES+=1
    )
)

echo   Item extraction complete.
exit /b 0

:EXTRACT_FILE
:: Parameters: %1=source file, %2=output directory, %3=asset types
set "SRC_FILE=%~1"
set "OUT_DIR=%~2"
set "TYPES=%~3"

:: Create output directory
mkdir "%OUT_DIR%" 2>nul

:: Extract using FFDec
java -jar "%FFDEC_PATH%" ^
    -export %TYPES% ^
    "%OUT_DIR%" ^
    "%SRC_FILE%" ^
    -format image:png,shape:svg,sound:mp3 ^
    -onerror ignore ^
    -timeout 120 ^
    >>"%LOG_PATH%\extraction_detail.log" 2>&1

set EXTRACT_RESULT=%errorlevel%

if %EXTRACT_RESULT% equ 0 (
    set /a SUCCESS_COUNT+=1
) else (
    set /a FAILED_COUNT+=1
)

exit /b %EXTRACT_RESULT%

:LOG
echo %~1 >> "%MAIN_LOG%"
exit /b 0

:GENERATE_REPORT
set "REPORT=%LOG_PATH%\extraction_report.txt"

(
echo ================================================================
echo    GOFUS Asset Extraction Report
echo ================================================================
echo.
echo Extraction Date: %date% %time%
echo.
echo Configuration:
echo   FFDec Path: %FFDEC_PATH%
echo   Dofus Path: %DOFUS_PATH%
echo   Output Path: %OUTPUT_PATH%
echo.
echo Statistics:
echo   Total Files Processed: %TOTAL_FILES%
echo   Successfully Extracted: %SUCCESS_COUNT%
echo   Failed: %FAILED_COUNT%
echo   Skipped: %SKIPPED_COUNT%
echo.
echo Success Rate:
set /a SUCCESS_RATE=(%SUCCESS_COUNT% * 100) / %TOTAL_FILES%
echo   !SUCCESS_RATE!%%
echo.
echo Asset Categories Extracted:
echo   [x] Character Sprites (18 classes^)
echo   [x] UI Elements (buttons, windows, icons^)
echo   [x] Map Assets (tiles, objects, interactive^)
echo   [x] Effects (spells, particles, animations^)
echo   [x] Audio (music, SFX, ambiance^)
echo   [x] Items (equipment, resources, consumables^)
echo.
echo Output Directory Structure:
tree "%OUTPUT_PATH%" /F
echo.
echo ================================================================
echo    End of Report
echo ================================================================
) > "%REPORT%"

call :LOG ""
call :LOG "Extraction report generated: %REPORT%"
exit /b 0
