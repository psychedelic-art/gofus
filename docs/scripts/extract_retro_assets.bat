@echo off
echo ========================================
echo GOFUS - Dofus Retro Asset Extraction
echo ========================================
echo.

set FFDEC="C:\Program Files (x86)\FFDec\ffdec.jar"
set RETRO_PATH="C:\Users\HardM\Desktop\Enterprise\gofus\Cliente retro\resources\app\retroclient"
set OUTPUT="C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client\Assets\ExtractedAssets\Raw"

echo Creating output directories...
mkdir "%OUTPUT%" 2>nul
mkdir "%OUTPUT%\Characters" 2>nul
mkdir "%OUTPUT%\UI" 2>nul
mkdir "%OUTPUT%\Maps" 2>nul
mkdir "%OUTPUT%\Effects" 2>nul
mkdir "%OUTPUT%\Items" 2>nul

echo.
echo [1/6] Extracting UI elements...
java -jar %FFDEC% -export image "%OUTPUT%\UI\buttons" %RETRO_PATH%\clips\defaultcc.swf
java -jar %FFDEC% -export image "%OUTPUT%\UI\icons" %RETRO_PATH%\clips\icons\*.swf
java -jar %FFDEC% -export image "%OUTPUT%\UI\smileys" %RETRO_PATH%\clips\smileys.swf

echo.
echo [2/6] Extracting priority character sprites (classes 1-10)...
REM Extract main character classes (IDs 1-10 are usually the main classes)
for %%i in (1 2 3 4 5 6 7 8 9 10) do (
    echo Extracting character %%i...
    java -jar %FFDEC% -export image "%OUTPUT%\Characters\class_%%i" %RETRO_PATH%\clips\sprites\%%i.swf
)

echo.
echo [3/6] Extracting ground/map tiles...
java -jar %FFDEC% -export image "%OUTPUT%\Maps\ground" %RETRO_PATH%\clips\ground.swf

echo.
echo [4/6] Extracting objects...
java -jar %FFDEC% -export image "%OUTPUT%\Maps\objects" %RETRO_PATH%\clips\objects.swf

echo.
echo [5/6] Extracting effect icons...
java -jar %FFDEC% -export image "%OUTPUT%\Effects\icons" %RETRO_PATH%\clips\effectsicons.swf
java -jar %FFDEC% -export image "%OUTPUT%\Effects\states" %RETRO_PATH%\clips\statesicons.swf

echo.
echo [6/6] Extracting items...
for %%f in (%RETRO_PATH%\clips\items\*.swf) do (
    echo Extracting items from %%~nf...
    java -jar %FFDEC% -export image "%OUTPUT%\Items\%%~nf" "%%f"
    REM Extract only first few to speed up testing
    goto :items_done
)
:items_done

echo.
echo ========================================
echo Extraction complete!
echo ========================================
echo.
echo Assets extracted to:
echo %OUTPUT%
echo.
echo Next steps:
echo 1. Open Unity
echo 2. The assets should auto-import
echo 3. If not, go to GOFUS menu > Asset Migration > Process Assets
echo.
pause