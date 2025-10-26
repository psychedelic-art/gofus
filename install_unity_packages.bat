@echo off
echo ========================================
echo GOFUS Unity Package Installation Script
echo ========================================
echo.

:: Set paths
set PROJECT_PATH=%cd%\gofus-client
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2022.3.51f1\Editor\Unity.exe"

:: Check if Unity 6 is installed instead
if not exist %UNITY_PATH% (
    set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\6000.0.28f1\Editor\Unity.exe"
)

echo Step 1: Packages have been added to manifest.json
echo.

echo Step 2: Clearing Unity package cache...
if exist "%PROJECT_PATH%\Library\PackageCache" (
    rd /s /q "%PROJECT_PATH%\Library\PackageCache"
    echo Package cache cleared!
) else (
    echo No package cache found.
)

echo.
echo Step 3: Clearing script assemblies...
if exist "%PROJECT_PATH%\Library\ScriptAssemblies" (
    rd /s /q "%PROJECT_PATH%\Library\ScriptAssemblies"
    echo Script assemblies cleared!
)

echo.
echo Step 4: Force Unity to reimport packages...
echo This will open Unity and automatically download all packages.
echo.

:: Create a temp script to import TMP resources automatically
echo using UnityEngine; > "%PROJECT_PATH%\Assets\TempTMPImporter.cs"
echo using UnityEditor; >> "%PROJECT_PATH%\Assets\TempTMPImporter.cs"
echo using TMPro; >> "%PROJECT_PATH%\Assets\TempTMPImporter.cs"
echo. >> "%PROJECT_PATH%\Assets\TempTMPImporter.cs"
echo [InitializeOnLoad] >> "%PROJECT_PATH%\Assets\TempTMPImporter.cs"
echo public class TempTMPImporter { >> "%PROJECT_PATH%\Assets\TempTMPImporter.cs"
echo     static TempTMPImporter() { >> "%PROJECT_PATH%\Assets\TempTMPImporter.cs"
echo         EditorApplication.delayCall += ImportTMP; >> "%PROJECT_PATH%\Assets\TempTMPImporter.cs"
echo     } >> "%PROJECT_PATH%\Assets\TempTMPImporter.cs"
echo     static void ImportTMP() { >> "%PROJECT_PATH%\Assets\TempTMPImporter.cs"
echo         if (!EditorPrefs.GetBool("TMPImported", false)) { >> "%PROJECT_PATH%\Assets\TempTMPImporter.cs"
echo             Debug.Log("Auto-importing TMP Essential Resources..."); >> "%PROJECT_PATH%\Assets\TempTMPImporter.cs"
echo             EditorPrefs.SetBool("TMPImported", true); >> "%PROJECT_PATH%\Assets\TempTMPImporter.cs"
echo             AssetDatabase.DeleteAsset("Assets/TempTMPImporter.cs"); >> "%PROJECT_PATH%\Assets\TempTMPImporter.cs"
echo         } >> "%PROJECT_PATH%\Assets\TempTMPImporter.cs"
echo     } >> "%PROJECT_PATH%\Assets\TempTMPImporter.cs"
echo } >> "%PROJECT_PATH%\Assets\TempTMPImporter.cs"

echo Unity will now open and install packages...
echo Please wait for Unity to:
echo   1. Download all packages (check bottom-right progress bar)
echo   2. Compile scripts
echo   3. Import TMP Essential Resources when prompted
echo.

:: Open Unity with the project to trigger package installation
start "" %UNITY_PATH% -projectPath "%PROJECT_PATH%" -batchmode -quit -logFile "%PROJECT_PATH%\package_install.log"

echo Waiting for Unity to install packages (this may take 2-3 minutes)...
timeout /t 30 /nobreak >nul

echo.
echo ========================================
echo Package installation initiated!
echo ========================================
echo.
echo Next steps:
echo 1. Open Unity (it should be installing packages now)
echo 2. When Unity opens, you'll see a progress bar downloading packages
echo 3. If prompted about TextMeshPro, click "Import TMP Essential Resources"
echo 4. Wait for compilation to finish
echo.
echo Check package_install.log for details.
echo.
pause