@echo off
setlocal enabledelayedexpansion

:: ========================================
:: GOFUS Dofus Asset Extraction Script
:: Using JPEXS FFDec Command Line
:: ========================================

echo ========================================
echo GOFUS Dofus Asset Extraction Tool
echo ========================================
echo.

:: Configuration - UPDATE THESE PATHS
set FFDEC_PATH=ffdec.exe
set DOFUS_PATH=C:\Program Files (x86)\Dofus\content
set OUTPUT_PATH=%~dp0ExtractedAssets\Raw

:: Override with command line arguments
if not "%1"=="" set FFDEC_PATH=%1
if not "%2"=="" set DOFUS_PATH=%2

echo Configuration:
echo - FFDec: %FFDEC_PATH%
echo - Dofus: %DOFUS_PATH%
echo - Output: %OUTPUT_PATH%
echo.

:: Check if FFDec exists
if not exist "%FFDEC_PATH%" (
    echo ERROR: FFDec not found at %FFDEC_PATH%
    echo.
    echo Usage: extract_dofus_assets.bat [ffdec_path] [dofus_path]
    echo Example: extract_dofus_assets.bat "C:\Tools\ffdec.exe" "C:\Games\Dofus"
    pause
    exit /b 1
)

:: Create output directories
echo Creating output directories...
mkdir "%OUTPUT_PATH%\Characters" 2>nul
mkdir "%OUTPUT_PATH%\Maps\Tiles" 2>nul
mkdir "%OUTPUT_PATH%\Maps\Objects" 2>nul
mkdir "%OUTPUT_PATH%\UI\Buttons" 2>nul
mkdir "%OUTPUT_PATH%\UI\Windows" 2>nul
mkdir "%OUTPUT_PATH%\UI\Icons" 2>nul
mkdir "%OUTPUT_PATH%\UI\Inventory" 2>nul
mkdir "%OUTPUT_PATH%\Effects" 2>nul
mkdir "%OUTPUT_PATH%\Monsters" 2>nul
mkdir "%OUTPUT_PATH%\Audio\Music" 2>nul
mkdir "%OUTPUT_PATH%\Audio\SFX" 2>nul
mkdir "%OUTPUT_PATH%\Audio\Ambient" 2>nul

echo.
echo ========================================
echo PHASE 1: Character Sprites (Priority)
echo ========================================
echo.

:: Character class IDs and names
set "classes[10]=Osamodas"
set "classes[11]=Feca"
set "classes[12]=Enutrof"
set "classes[13]=Sram"
set "classes[14]=Xelor"
set "classes[15]=Ecaflip"
set "classes[16]=Eniripsa"
set "classes[17]=Iop"
set "classes[18]=Cra"
set "classes[19]=Sadida"
set "classes[110]=Sacrier"
set "classes[111]=Pandawa"
set "classes[112]=Rogue"
set "classes[113]=Masqueraider"
set "classes[114]=Foggernaut"
set "classes[115]=Eliotrope"
set "classes[116]=Huppermage"
set "classes[117]=Ouginak"

:: Extract character sprites
for %%i in (10 11 12 13 14 15 16 17 18 19 110 111 112 113 114 115 116 117) do (
    if defined classes[%%i] (
        set className=!classes[%%i]!
        set swfPath=%DOFUS_PATH%\gfx\sprites\actors\characters\%%i.swf

        if exist "!swfPath!" (
            echo Extracting !className! sprites...
            mkdir "%OUTPUT_PATH%\Characters\!className!" 2>nul
            "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\Characters\!className!" "!swfPath!" -onerror ignore
            echo - !className! complete
        ) else (
            echo - !className! not found at !swfPath!
        )
    )
)

echo.
echo ========================================
echo PHASE 2: UI Elements
echo ========================================
echo.

:: Extract UI elements
if exist "%DOFUS_PATH%\gfx\ui\" (
    echo Extracting UI elements...

    :: Buttons
    if exist "%DOFUS_PATH%\gfx\ui\buttons.swf" (
        "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\UI\Buttons" "%DOFUS_PATH%\gfx\ui\buttons.swf" -onerror ignore
        echo - Buttons extracted
    )

    :: Windows/Frames
    if exist "%DOFUS_PATH%\gfx\ui\windows.swf" (
        "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\UI\Windows" "%DOFUS_PATH%\gfx\ui\windows.swf" -onerror ignore
        echo - Windows extracted
    )

    :: Icons
    if exist "%DOFUS_PATH%\gfx\ui\icons.swf" (
        "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\UI\Icons" "%DOFUS_PATH%\gfx\ui\icons.swf" -onerror ignore
        echo - Icons extracted
    )

    :: Inventory UI
    if exist "%DOFUS_PATH%\gfx\ui\inventory.swf" (
        "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\UI\Inventory" "%DOFUS_PATH%\gfx\ui\inventory.swf" -onerror ignore
        echo - Inventory UI extracted
    )
)

echo.
echo ========================================
echo PHASE 3: Map Tiles
echo ========================================
echo.

:: Extract map tiles
if exist "%DOFUS_PATH%\gfx\maps\" (
    echo Extracting map tiles...

    :: Process tile sets (0-10 as example, expand as needed)
    for /L %%i in (0,1,10) do (
        if exist "%DOFUS_PATH%\gfx\maps\tiles\%%i.swf" (
            "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\Maps\Tiles" "%DOFUS_PATH%\gfx\maps\tiles\%%i.swf" -onerror ignore
            echo - Tile set %%i extracted
        )
    )

    :: Extract map objects
    if exist "%DOFUS_PATH%\gfx\maps\objects\" (
        for %%f in ("%DOFUS_PATH%\gfx\maps\objects\*.swf") do (
            "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\Maps\Objects" "%%f" -onerror ignore
            echo - Object %%~nf extracted
        )
    )
)

echo.
echo ========================================
echo PHASE 4: Effects & Spells
echo ========================================
echo.

:: Extract effects
if exist "%DOFUS_PATH%\gfx\spells\" (
    echo Extracting spell effects...
    for %%f in ("%DOFUS_PATH%\gfx\spells\*.swf") do (
        "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\Effects" "%%f" -onerror ignore
        echo - Effect %%~nf extracted
    )
)

echo.
echo ========================================
echo PHASE 5: Monster Sprites
echo ========================================
echo.

:: Extract monster sprites
if exist "%DOFUS_PATH%\gfx\sprites\actors\monsters\" (
    echo Extracting monster sprites...
    for %%f in ("%DOFUS_PATH%\gfx\sprites\actors\monsters\*.swf") do (
        "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\Monsters" "%%f" -onerror ignore
        echo - Monster %%~nf extracted
    )
)

echo.
echo ========================================
echo PHASE 6: Audio Files
echo ========================================
echo.

:: Extract audio
if exist "%DOFUS_PATH%\audio\" (
    echo Extracting audio files...

    :: Music
    if exist "%DOFUS_PATH%\audio\music\" (
        for %%f in ("%DOFUS_PATH%\audio\music\*.swf") do (
            "%FFDEC_PATH%" -export sound "%OUTPUT_PATH%\Audio\Music" "%%f" -onerror ignore
        )
        echo - Music extracted
    )

    :: Sound effects
    if exist "%DOFUS_PATH%\audio\sfx\" (
        for %%f in ("%DOFUS_PATH%\audio\sfx\*.swf") do (
            "%FFDEC_PATH%" -export sound "%OUTPUT_PATH%\Audio\SFX" "%%f" -onerror ignore
        )
        echo - Sound effects extracted
    )
)

echo.
echo ========================================
echo Extraction Complete!
echo ========================================
echo.
echo Assets extracted to: %OUTPUT_PATH%
echo.
echo Next steps:
echo 1. Open Unity
echo 2. Go to GOFUS ^> Asset Migration ^> Extraction Validator
echo 3. Click "Run Validation" to check extraction
echo 4. Click "Process Assets" to import into Unity
echo.
pause