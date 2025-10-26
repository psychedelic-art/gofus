@echo off
setlocal enabledelayedexpansion

echo ========================================
echo GOFUS Asset Extraction Tool
echo ========================================
echo.

:: Set paths - Update these to match your system
set FFDEC_PATH=ffdec.exe
set DOFUS_PATH=C:\Program Files (x86)\Dofus\content
set OUTPUT_PATH=%~dp0ExtractedAssets\Raw

:: Check if custom FFDec path was provided
if not "%1"=="" (
    set FFDEC_PATH=%1
)

:: Check if custom Dofus path was provided
if not "%2"=="" (
    set DOFUS_PATH=%2
)

echo Configuration:
echo - FFDec Path: %FFDEC_PATH%
echo - Dofus Path: %DOFUS_PATH%
echo - Output Path: %OUTPUT_PATH%
echo.

:: Create output directories
echo Creating output directories...
mkdir "%OUTPUT_PATH%\Characters" 2>nul
mkdir "%OUTPUT_PATH%\Maps\Tiles" 2>nul
mkdir "%OUTPUT_PATH%\Maps\Objects" 2>nul
mkdir "%OUTPUT_PATH%\UI\Buttons" 2>nul
mkdir "%OUTPUT_PATH%\UI\Windows" 2>nul
mkdir "%OUTPUT_PATH%\UI\Icons" 2>nul
mkdir "%OUTPUT_PATH%\Effects" 2>nul
mkdir "%OUTPUT_PATH%\Audio\Music" 2>nul
mkdir "%OUTPUT_PATH%\Audio\SFX" 2>nul

:: Check if FFDec exists
if not exist "%FFDEC_PATH%" (
    echo.
    echo ERROR: FFDec not found at %FFDEC_PATH%
    echo Please provide the correct path as first argument:
    echo   extract_assets.bat "C:\path\to\ffdec.exe"
    echo.
    echo Generating test assets instead...
    goto :generate_test_assets
)

:: Check if Dofus directory exists
if not exist "%DOFUS_PATH%" (
    echo.
    echo WARNING: Dofus not found at %DOFUS_PATH%
    echo Generating test assets instead...
    goto :generate_test_assets
)

echo.
echo Starting extraction...
echo.

:: Extract character sprites
echo Extracting character sprites...
if exist "%DOFUS_PATH%\gfx\sprites\actors\characters\11.swf" (
    "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\Characters\Feca" "%DOFUS_PATH%\gfx\sprites\actors\characters\11.swf"
    echo - Feca sprites extracted
)

if exist "%DOFUS_PATH%\gfx\sprites\actors\characters\10.swf" (
    "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\Characters\Osamodas" "%DOFUS_PATH%\gfx\sprites\actors\characters\10.swf"
    echo - Osamodas sprites extracted
)

:: Extract UI elements
echo.
echo Extracting UI elements...
if exist "%DOFUS_PATH%\gfx\ui\buttons.swf" (
    "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\UI\Buttons" "%DOFUS_PATH%\gfx\ui\buttons.swf"
    echo - Button sprites extracted
)

:: Extract map tiles
echo.
echo Extracting map tiles...
if exist "%DOFUS_PATH%\gfx\maps\tiles\" (
    for %%f in ("%DOFUS_PATH%\gfx\maps\tiles\*.swf") do (
        "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\Maps\Tiles" "%%f"
        echo - Extracted %%~nf
    )
)

goto :finish

:generate_test_assets
echo.
echo ========================================
echo Generating Test Assets
echo ========================================
echo.
echo Since Dofus files are not available, generating test assets...
echo This will create sample assets to test the pipeline.
echo.

:: Call Unity to generate test assets
cd /d "%~dp0"
echo Running Unity test asset generator...

:: Create a simple PowerShell script to generate test images
powershell -Command "& {
    Write-Host 'Generating test sprites...'

    # Function to create a test image
    function Create-TestImage {
        param($Path, $Width, $Height, $Color)
        Add-Type -AssemblyName System.Drawing
        $bitmap = New-Object System.Drawing.Bitmap($Width, $Height)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $brush = New-Object System.Drawing.SolidBrush($Color)
        $graphics.FillRectangle($brush, 0, 0, $Width, $Height)

        # Add text
        $font = New-Object System.Drawing.Font('Arial', 10)
        $textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
        $graphics.DrawString([System.IO.Path]::GetFileNameWithoutExtension($Path), $font, $textBrush, 5, 5)

        $graphics.Dispose()
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
        $bitmap.Dispose()
    }

    # Create character test sprites
    $charPath = '%OUTPUT_PATH%\Characters\TestCharacter'
    New-Item -ItemType Directory -Path $charPath -Force | Out-Null

    $directions = @('north', 'northeast', 'east', 'southeast', 'south', 'southwest', 'west', 'northwest')
    $animations = @('idle', 'walk', 'attack', 'cast')

    foreach ($anim in $animations) {
        foreach ($dir in $directions) {
            $fileName = Join-Path $charPath ('{0}_{1}_0.png' -f $anim, $dir)
            Create-TestImage -Path $fileName -Width 64 -Height 64 -Color ([System.Drawing.Color]::FromArgb(100, 150, 200))
            Write-Host '  Created:' (Split-Path $fileName -Leaf)
        }
    }

    # Create UI test elements
    $uiPath = '%OUTPUT_PATH%\UI'
    Create-TestImage -Path (Join-Path $uiPath 'Buttons\btn_normal.png') -Width 128 -Height 48 -Color ([System.Drawing.Color]::Gray)
    Create-TestImage -Path (Join-Path $uiPath 'Buttons\btn_hover.png') -Width 128 -Height 48 -Color ([System.Drawing.Color]::LightGray)
    Create-TestImage -Path (Join-Path $uiPath 'Windows\window_frame.png') -Width 256 -Height 256 -Color ([System.Drawing.Color]::DarkGray)
    Create-TestImage -Path (Join-Path $uiPath 'Icons\icon_sword.png') -Width 32 -Height 32 -Color ([System.Drawing.Color]::Silver)

    # Create map tiles
    $mapPath = '%OUTPUT_PATH%\Maps\Tiles'
    Create-TestImage -Path (Join-Path $mapPath 'grass_01.png') -Width 64 -Height 32 -Color ([System.Drawing.Color]::Green)
    Create-TestImage -Path (Join-Path $mapPath 'stone_01.png') -Width 64 -Height 32 -Color ([System.Drawing.Color]::Gray)
    Create-TestImage -Path (Join-Path $mapPath 'water_01.png') -Width 64 -Height 32 -Color ([System.Drawing.Color]::Blue)

    Write-Host 'Test assets generated successfully!'
}"

:finish
echo.
echo ========================================
echo Extraction Complete!
echo ========================================
echo.
echo Assets have been extracted to:
echo %OUTPUT_PATH%
echo.
echo Next steps:
echo 1. Open Unity
echo 2. Go to GOFUS ^> Asset Migration ^> Extraction Validator
echo 3. Click "Run Validation" to verify extraction
echo 4. Click "Process Assets" to import into Unity
echo.
pause