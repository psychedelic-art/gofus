@echo off
setlocal enabledelayedexpansion

echo ========================================
echo   GOFUS Priority Asset Extraction
echo   Extracts MVP assets for quick testing
echo ========================================
echo.

:: Set default paths (can be overridden by parameters)
set FFDEC_PATH=C:\Program Files\FFDec\ffdec.jar
set DOFUS_PATH=C:\Program Files (x86)\Dofus
set OUTPUT_PATH=C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client\Assets\_Project\Resources\Sprites

:: Check for custom parameters
if not "%1"=="" set FFDEC_PATH=%1
if not "%2"=="" set DOFUS_PATH=%2
if not "%3"=="" set OUTPUT_PATH=%3

:: Check Java installation
echo [1/5] Checking Java installation...
java -version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Java is not installed or not in PATH.
    echo Please install Java from https://www.java.com/download/
    pause
    exit /b 1
)
echo ✓ Java found

:: Check FFDec exists
echo [2/5] Checking FFDec installation...
if not exist "%FFDEC_PATH%" (
    echo ERROR: FFDec not found at %FFDEC_PATH%
    echo Please download from: https://github.com/jindrapetrik/jpexs-decompiler/releases
    echo Or provide correct path as first parameter
    pause
    exit /b 1
)
echo ✓ FFDec found at %FFDEC_PATH%

:: Check Dofus installation
echo [3/5] Checking Dofus installation...
if not exist "%DOFUS_PATH%" (
    echo WARNING: Dofus not found at %DOFUS_PATH%
    echo Trying alternative locations...

    :: Try alternative paths
    if exist "C:\Dofus" set DOFUS_PATH=C:\Dofus
    if exist "%LOCALAPPDATA%\Dofus" set DOFUS_PATH=%LOCALAPPDATA%\Dofus

    if not exist "!DOFUS_PATH!" (
        echo ERROR: Could not find Dofus installation
        echo Please provide correct path as second parameter
        pause
        exit /b 1
    )
)
echo ✓ Dofus found at %DOFUS_PATH%

:: Create output directories
echo [4/5] Creating output directories...
if not exist "%OUTPUT_PATH%" mkdir "%OUTPUT_PATH%"
if not exist "%OUTPUT_PATH%\Classes" mkdir "%OUTPUT_PATH%\Classes"
if not exist "%OUTPUT_PATH%\Classes\Icons" mkdir "%OUTPUT_PATH%\Classes\Icons"
if not exist "%OUTPUT_PATH%\UI" mkdir "%OUTPUT_PATH%\UI"
if not exist "%OUTPUT_PATH%\Effects" mkdir "%OUTPUT_PATH%\Effects"
echo ✓ Output directories created

:: Extract priority assets
echo [5/5] Extracting priority assets...
echo.
echo This will extract:
echo - 4 core character classes (Feca, Sram, Eniripsa, Iop)
echo - Essential UI elements
echo - Basic combat effects
echo.

set SUCCESS_COUNT=0
set FAIL_COUNT=0

:: Extract priority character classes
echo === Extracting Character Classes ===

:: Feca (ID: 01)
echo Extracting Feca...
java -jar "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\Classes\Class_01_Feca_Male" "%DOFUS_PATH%\content\gfx\sprites\actors\characters\01.swf" -format image:png -onerror ignore >nul 2>&1
if errorlevel 1 (
    echo   ✗ Failed to extract Feca
    set /a FAIL_COUNT+=1
) else (
    echo   ✓ Feca extracted
    set /a SUCCESS_COUNT+=1
)

:: Sram (ID: 04)
echo Extracting Sram...
java -jar "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\Classes\Class_04_Sram_Male" "%DOFUS_PATH%\content\gfx\sprites\actors\characters\04.swf" -format image:png -onerror ignore >nul 2>&1
if errorlevel 1 (
    echo   ✗ Failed to extract Sram
    set /a FAIL_COUNT+=1
) else (
    echo   ✓ Sram extracted
    set /a SUCCESS_COUNT+=1
)

:: Eniripsa (ID: 07)
echo Extracting Eniripsa...
java -jar "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\Classes\Class_07_Eniripsa_Male" "%DOFUS_PATH%\content\gfx\sprites\actors\characters\07.swf" -format image:png -onerror ignore >nul 2>&1
if errorlevel 1 (
    echo   ✗ Failed to extract Eniripsa
    set /a FAIL_COUNT+=1
) else (
    echo   ✓ Eniripsa extracted
    set /a SUCCESS_COUNT+=1
)

:: Iop (ID: 08)
echo Extracting Iop...
java -jar "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\Classes\Class_08_Iop_Male" "%DOFUS_PATH%\content\gfx\sprites\actors\characters\08.swf" -format image:png -onerror ignore >nul 2>&1
if errorlevel 1 (
    echo   ✗ Failed to extract Iop
    set /a FAIL_COUNT+=1
) else (
    echo   ✓ Iop extracted
    set /a SUCCESS_COUNT+=1
)

echo.
echo === Extracting UI Elements ===

:: Basic UI elements
echo Extracting buttons...
java -jar "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\UI\Buttons" "%DOFUS_PATH%\content\gfx\ui\buttons.swf" -format image:png -onerror ignore >nul 2>&1
if errorlevel 1 (
    echo   ✗ Failed to extract buttons
    set /a FAIL_COUNT+=1
) else (
    echo   ✓ Buttons extracted
    set /a SUCCESS_COUNT+=1
)

echo Extracting windows...
java -jar "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\UI\Windows" "%DOFUS_PATH%\content\gfx\ui\windows.swf" -format image:png -onerror ignore >nul 2>&1
if errorlevel 1 (
    echo   ✗ Failed to extract windows
    set /a FAIL_COUNT+=1
) else (
    echo   ✓ Windows extracted
    set /a SUCCESS_COUNT+=1
)

echo Extracting cursors...
java -jar "%FFDEC_PATH%" -export image "%OUTPUT_PATH%\UI\Cursors" "%DOFUS_PATH%\content\gfx\ui\cursors.swf" -format image:png -onerror ignore >nul 2>&1
if errorlevel 1 (
    echo   ✗ Failed to extract cursors
    set /a FAIL_COUNT+=1
) else (
    echo   ✓ Cursors extracted
    set /a SUCCESS_COUNT+=1
)

echo.
echo ========================================
echo   Extraction Complete!
echo ========================================
echo   Successful: %SUCCESS_COUNT% assets
echo   Failed: %FAIL_COUNT% assets
echo   Output: %OUTPUT_PATH%
echo ========================================
echo.

:: Open output folder
echo Opening output folder...
start "" "%OUTPUT_PATH%"

echo Press any key to exit...
pause >nul
exit /b 0