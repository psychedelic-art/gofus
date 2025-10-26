@echo off
echo Copying Dofus Retro images to Unity project...

set SOURCE=C:\Users\HardM\Desktop\Enterprise\gofus\Cliente retro\resources\app\retroclient
set DEST=C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client\Assets\ExtractedAssets\Raw

echo Creating directories...
mkdir "%DEST%\UI\Backgrounds" 2>nul
mkdir "%DEST%\UI\Buttons" 2>nul
mkdir "%DEST%\Characters" 2>nul

echo Copying loading banners as backgrounds...
copy "%SOURCE%\loadingbanners\*.png" "%DEST%\UI\Backgrounds\"
copy "%SOURCE%\loadingbanners\*.jpg" "%DEST%\UI\Backgrounds\"

echo Images copied successfully!
echo.
echo Copied to: %DEST%
echo.
echo Now open Unity and the images should import automatically!
pause