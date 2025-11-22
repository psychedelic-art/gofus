@echo off
REM Extract map graphics from SWF files using JPEXS FFDec
REM Based on EXTRACTION_SUMMARY.md pattern

SET FFDEC="C:\Program Files (x86)\FFDec\ffdec.jar"
SET MAPS_INPUT="C:\Users\HardM\Desktop\Enterprise\gofus\Cliente retro\resources\app\retroclient\data\maps"
SET OUTPUT="C:\Users\HardM\Desktop\Enterprise\gofus\gofus-client\Assets\_Project\Resources\Sprites\Maps"

echo ========================================
echo GOFUS Map Graphics Extraction
echo ========================================
echo.
echo Extracting 5 test maps...
echo.

REM Map 7411 - Astrub Center
echo [1/5] Extracting Map 7411 (Astrub Center)...
java -jar %FFDEC% -export image,shape,sprite "%OUTPUT%\7411" "%MAPS_INPUT%\7411_0711291819X.swf" -format image:png -onerror ignore
echo Done!
echo.

REM Map 7410 - Forest
echo [2/5] Extracting Map 7410 (Forest)...
java -jar %FFDEC% -export image,shape,sprite "%OUTPUT%\7410" "%MAPS_INPUT%\7410_0907071142X.swf" -format image:png -onerror ignore
echo Done!
echo.

REM Map 7412 - Plains
echo [3/5] Extracting Map 7412 (Plains)...
java -jar %FFDEC% -export image,shape,sprite "%OUTPUT%\7412" "%MAPS_INPUT%\7412_0905131019X.swf" -format image:png -onerror ignore
echo Done!
echo.

REM Map 7339 - Mountains
echo [4/5] Extracting Map 7339 (Mountains)...
java -jar %FFDEC% -export image,shape,sprite "%OUTPUT%\7339" "%MAPS_INPUT%\7339_0706131721X.swf" -format image:png -onerror ignore
echo Done!
echo.

REM Map 7340 - Village
echo [5/5] Extracting Map 7340 (Village)...
java -jar %FFDEC% -export image,shape,sprite "%OUTPUT%\7340" "%MAPS_INPUT%\7340_0706131721X.swf" -format image:png -onerror ignore
echo Done!
echo.

echo ========================================
echo Extraction Complete!
echo ========================================
echo.
echo Maps extracted to:
echo %OUTPUT%
echo.
echo You can now import these into Unity!
echo.

pause
