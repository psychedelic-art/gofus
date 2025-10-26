# GOFUS Unity Package Fix Script
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "GOFUS Unity Package Fix Script" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$projectPath = Join-Path $PSScriptRoot "gofus-client"

Write-Host "Step 1: Checking Unity project path..." -ForegroundColor Green
Write-Host "Project: $projectPath" -ForegroundColor White

# Clear Unity Library cache
Write-Host ""
Write-Host "Step 2: Clearing Unity Library cache..." -ForegroundColor Green

$foldersToDelete = @(
    "Library\PackageCache",
    "Library\ScriptAssemblies",
    "Library\Bee",
    "Library\Artifacts",
    "Temp",
    "obj"
)

foreach ($folder in $foldersToDelete) {
    $fullPath = Join-Path $projectPath $folder
    if (Test-Path $fullPath) {
        Write-Host "  Deleting: $folder" -ForegroundColor Yellow
        Remove-Item $fullPath -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "  Cache cleared!" -ForegroundColor Green

# Create import settings
Write-Host ""
Write-Host "Step 3: Creating TextMeshPro import settings..." -ForegroundColor Green

$importerScript = @"
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class PackageAutoImporter
{
    static PackageAutoImporter()
    {
        EditorApplication.delayCall += CheckPackages;
    }

    static void CheckPackages()
    {
        Debug.Log("[GOFUS] Checking for required packages...");

        // Check if TextMeshPro is installed
        if (!UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.textmeshpro") == null)
        {
            Debug.LogError("[GOFUS] TextMeshPro not found! Please install via Package Manager.");
        }
        else
        {
            Debug.Log("[GOFUS] TextMeshPro found. Remember to import Essential Resources if prompted!");
        }
    }
}
"@

$importerPath = Join-Path $projectPath "Assets\Editor"
if (!(Test-Path $importerPath)) {
    New-Item -ItemType Directory -Path $importerPath -Force | Out-Null
}

$scriptPath = Join-Path $importerPath "PackageAutoImporter.cs"
$importerScript | Out-File -FilePath $scriptPath -Encoding UTF8
Write-Host "  Import helper created!" -ForegroundColor Green

# Show instructions
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MANUAL STEPS REQUIRED:" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Open Unity Hub" -ForegroundColor Yellow
Write-Host "2. Click on your GOFUS project to open it" -ForegroundColor Yellow
Write-Host "3. Unity will automatically:" -ForegroundColor Green
Write-Host "   - Download the packages we added to manifest.json" -ForegroundColor White
Write-Host "   - Show progress in the bottom-right corner" -ForegroundColor White
Write-Host "   - Compile all scripts" -ForegroundColor White
Write-Host ""
Write-Host "4. IMPORTANT: When TextMeshPro popup appears:" -ForegroundColor Red
Write-Host "   - Click 'Import TMP Essential Resources'" -ForegroundColor Yellow
Write-Host "   - In the import window, click 'Import' button" -ForegroundColor Yellow
Write-Host ""
Write-Host "5. Wait for Unity to finish compiling (progress bar disappears)" -ForegroundColor Green
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Press any key to open the project folder..." -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan

$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Open project folder
Start-Process explorer.exe $projectPath