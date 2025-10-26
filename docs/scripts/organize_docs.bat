@echo off
echo Organizing documentation files...

REM Create directories
mkdir docs\setup 2>nul
mkdir docs\fixes 2>nul
mkdir docs\assets 2>nul
mkdir docs\scripts 2>nul
mkdir docs\old_logs 2>nul

REM Move setup documentation
move UNITY_SETUP_STATUS.md docs\setup\ 2>nul
move UNITY_STARTUP_GUIDE.md docs\setup\ 2>nul
move HOW_TO_RUN_GOFUS.md docs\setup\ 2>nul
move ORGANIZATION_COMPLETE.md docs\setup\ 2>nul

REM Move fix documentation
move COMPILATION_FIXES_REPORT.md docs\fixes\ 2>nul
move COMPILATION_FIXES_FINAL_REPORT.md docs\fixes\ 2>nul
move REMAINING_ERRORS.md docs\fixes\ 2>nul
move Errors-Client.md docs\fixes\ 2>nul
move UNITY_PACKAGE_ERRORS_FIX_GUIDE.md docs\fixes\ 2>nul

REM Move asset documentation
move FIX_GRAY_SCREEN_NOW.md docs\assets\ 2>nul
move QUICK_FIX_IMAGES.md docs\assets\ 2>nul

REM Move scripts
move add_unity_to_path.ps1 docs\scripts\ 2>nul
move fix_unity_packages.ps1 docs\scripts\ 2>nul
move extract_retro_assets.bat docs\scripts\ 2>nul
move copy_retro_images.bat docs\scripts\ 2>nul
move install_unity_packages.bat docs\scripts\ 2>nul
move run_unity_tests.bat docs\scripts\ 2>nul

REM Move old log files
move *.log docs\old_logs\ 2>nul

REM Clean up test directories
rmdir /s /q test_export 2>nul
rmdir /s /q test_shapes 2>nul
rmdir /s /q test_sprites 2>nul

echo.
echo Documentation organized!
echo.
echo Structure:
echo docs\
echo   - MASTER_IMPLEMENTATION_PLAN.md (Main guide)
echo   - LOGIN_SCREEN_NOW.md (Immediate action)
echo   - setup\ (Unity setup docs)
echo   - fixes\ (Compilation fixes)
echo   - assets\ (Asset guides)
echo   - scripts\ (Utility scripts)
echo.
pause