@echo off
echo ========================================
echo GOFUS Unity Test Runner
echo ========================================
echo.

set PROJECT_PATH=%cd%\gofus-client
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\6000.2.9f1\Editor\Unity.exe"
set RESULTS_PATH=%PROJECT_PATH%\TestResults.xml

echo Project: %PROJECT_PATH%
echo Unity: %UNITY_PATH%
echo.

echo Running Unity Edit Mode Tests...
%UNITY_PATH% -runTests -batchmode -projectPath "%PROJECT_PATH%" -testResults "%RESULTS_PATH%" -testPlatform EditMode -logFile "%PROJECT_PATH%\test-log.txt"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Tests PASSED!
    echo ========================================
    echo.
    echo Results saved to: %RESULTS_PATH%
    echo Log saved to: %PROJECT_PATH%\test-log.txt
    echo.
) else (
    echo.
    echo ========================================
    echo Tests FAILED!
    echo ========================================
    echo.
    echo Check log at: %PROJECT_PATH%\test-log.txt
    echo.
)

pause