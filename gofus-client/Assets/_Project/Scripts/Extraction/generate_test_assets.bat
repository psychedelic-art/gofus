@echo off
echo ========================================
echo GOFUS Test Asset Generation
echo ========================================
echo.
echo This will generate test assets for pipeline validation
echo.

set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2022.3.51f1\Editor\Unity.exe"
set PROJECT_PATH="%~dp0"

:: Check if Unity exists
if not exist %UNITY_PATH% (
    echo Unity not found at %UNITY_PATH%
    echo Please update UNITY_PATH in this script
    echo.
    echo Creating placeholder assets manually...
    goto :manual_creation
)

echo Running Unity to generate test assets...
%UNITY_PATH% -batchmode -quit -projectPath %PROJECT_PATH% -executeMethod GOFUS.Editor.AssetMigration.TestAssetGenerator.GenerateFromCommandLine -logFile test_asset_generation.log

echo.
echo Unity asset generation complete!
echo Check test_asset_generation.log for details
goto :end

:manual_creation
echo.
echo Creating placeholder test files...
cd /d "%~dp0ExtractedAssets\Raw"

:: Create placeholder character sprites
echo Creating character sprites...
for %%d in (north northeast east southeast south southwest west northwest) do (
    for %%a in (idle walk attack cast) do (
        echo placeholder > "Characters\TestCharacter\%%a_%%d_0.txt"
    )
)

:: Create UI placeholders
echo Creating UI elements...
echo placeholder > "UI\Buttons\btn_normal.txt"
echo placeholder > "UI\Buttons\btn_hover.txt"
echo placeholder > "UI\Buttons\btn_pressed.txt"
echo placeholder > "UI\Icons\icon_sword.txt"
echo placeholder > "UI\Icons\icon_shield.txt"

:: Create map tile placeholders
echo Creating map tiles...
for %%t in (grass stone sand water dirt) do (
    echo placeholder > "Maps\Tiles\%%t_01.txt"
    echo placeholder > "Maps\Tiles\%%t_02.txt"
)

echo.
echo Placeholder files created!
echo Note: These are text files for testing the pipeline.
echo Real image assets should be extracted from Dofus using JPEXS FFDec.

:end
echo.
echo Next steps:
echo 1. Open Unity
echo 2. Menu: GOFUS ^> Asset Migration ^> Extraction Validator
echo 3. Click "Run Validation"
echo.
pause