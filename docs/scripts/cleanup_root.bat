@echo off
echo Cleaning up root directory...
echo.

cd /d "C:\Users\HardM\Desktop\Enterprise\gofus"

echo Creating directories...
mkdir docs\setup 2>nul
mkdir docs\fixes 2>nul
mkdir docs\assets 2>nul
mkdir docs\scripts 2>nul
mkdir docs\old_logs 2>nul

echo.
echo Moving setup documentation...
if exist UNITY_SETUP_STATUS.md move UNITY_SETUP_STATUS.md docs\setup\
if exist UNITY_STARTUP_GUIDE.md move UNITY_STARTUP_GUIDE.md docs\setup\
if exist HOW_TO_RUN_GOFUS.md move HOW_TO_RUN_GOFUS.md docs\setup\
if exist ORGANIZATION_COMPLETE.md move ORGANIZATION_COMPLETE.md docs\setup\

echo Moving fix documentation...
if exist COMPILATION_FIXES_REPORT.md move COMPILATION_FIXES_REPORT.md docs\fixes\
if exist COMPILATION_FIXES_FINAL_REPORT.md move COMPILATION_FIXES_FINAL_REPORT.md docs\fixes\
if exist REMAINING_ERRORS.md move REMAINING_ERRORS.md docs\fixes\
if exist Errors-Client.md move Errors-Client.md docs\fixes\
if exist UNITY_PACKAGE_ERRORS_FIX_GUIDE.md move UNITY_PACKAGE_ERRORS_FIX_GUIDE.md docs\fixes\

echo Moving asset documentation...
if exist FIX_GRAY_SCREEN_NOW.md move FIX_GRAY_SCREEN_NOW.md docs\assets\
if exist QUICK_FIX_IMAGES.md move QUICK_FIX_IMAGES.md docs\assets\

echo Moving scripts...
if exist add_unity_to_path.ps1 move add_unity_to_path.ps1 docs\scripts\
if exist fix_unity_packages.ps1 move fix_unity_packages.ps1 docs\scripts\
if exist extract_retro_assets.bat move extract_retro_assets.bat docs\scripts\
if exist copy_retro_images.bat move copy_retro_images.bat docs\scripts\
if exist install_unity_packages.bat move install_unity_packages.bat docs\scripts\
if exist run_unity_tests.bat move run_unity_tests.bat docs\scripts\
if exist organize_docs.bat move organize_docs.bat docs\scripts\

echo Moving log files...
if exist *.log move *.log docs\old_logs\
if exist compile_check.log move compile_check.log docs\old_logs\
if exist compile_results.log move compile_results.log docs\old_logs\
if exist compile_check_new.log move compile_check_new.log docs\old_logs\
if exist compile_check_correct_version.log move compile_check_correct_version.log docs\old_logs\
if exist final_compile_check.log move final_compile_check.log docs\old_logs\
if exist final_compile_check_2.log move final_compile_check_2.log docs\old_logs\

echo Cleaning up test directories...
if exist test_export rmdir /s /q test_export
if exist test_shapes rmdir /s /q test_shapes
if exist test_sprites rmdir /s /q test_sprites

echo.
echo ========================================
echo Root directory cleaned!
echo ========================================
echo.
echo Remaining in root (as intended):
dir /b | findstr /v "docs gofus-backend gofus-client gofus-game-server migration architecture Cliente Codigo Bases Lang Emulador README nul image"
echo.
echo All documentation moved to:
echo   docs\
echo   ├── MASTER_IMPLEMENTATION_PLAN.md
echo   ├── LOGIN_SCREEN_NOW.md
echo   ├── BACKEND_API_REFERENCE.md
echo   ├── setup\
echo   ├── fixes\
echo   ├── assets\
echo   ├── scripts\
echo   └── old_logs\
echo.
echo ========================================
echo NEXT STEP: Follow docs\LOGIN_SCREEN_NOW.md
echo ========================================
pause