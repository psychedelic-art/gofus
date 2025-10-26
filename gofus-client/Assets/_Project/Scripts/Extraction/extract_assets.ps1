# GOFUS Asset Extraction Script
param(
    [string]$FFDecPath = "ffdec.exe",
    [string]$DofusPath = "C:\Program Files (x86)\Dofus\content",
    [string]$OutputPath = "$PSScriptRoot\ExtractedAssets\Raw"
)

Write-Host "========================================"
Write-Host "GOFUS Asset Extraction Tool" -ForegroundColor Cyan
Write-Host "========================================"
Write-Host ""

# Create output directories
Write-Host "Creating output directories..." -ForegroundColor Yellow
$directories = @(
    "$OutputPath\Characters\TestCharacter",
    "$OutputPath\Maps\Tiles",
    "$OutputPath\Maps\Objects",
    "$OutputPath\UI\Buttons",
    "$OutputPath\UI\Windows",
    "$OutputPath\UI\Icons",
    "$OutputPath\Effects",
    "$OutputPath\Audio\Music",
    "$OutputPath\Audio\SFX"
)

foreach ($dir in $directories) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}

Write-Host "Output directories created at: $OutputPath" -ForegroundColor Green
Write-Host ""

# Function to create test images
function Create-TestImage {
    param(
        [string]$Path,
        [int]$Width,
        [int]$Height,
        [System.Drawing.Color]$Color,
        [string]$Text = ""
    )

    Add-Type -AssemblyName System.Drawing

    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    # Fill background
    $brush = New-Object System.Drawing.SolidBrush($Color)
    $graphics.FillRectangle($brush, 0, 0, $Width, $Height)

    # Add text if provided
    if ($Text) {
        $font = New-Object System.Drawing.Font("Arial", 8)
        $textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
        $graphics.DrawString($Text, $font, $textBrush, 2, 2)
        $font.Dispose()
        $textBrush.Dispose()
    }

    # Add border
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::Black, 1)
    $graphics.DrawRectangle($pen, 0, 0, $Width - 1, $Height - 1)

    $graphics.Dispose()
    $brush.Dispose()
    $pen.Dispose()

    # Save image
    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

# Check if we can extract real assets
$useRealAssets = $false
if ((Test-Path $FFDecPath) -and (Test-Path $DofusPath)) {
    Write-Host "FFDec and Dofus found - attempting real extraction..." -ForegroundColor Green
    $useRealAssets = $true

    # Try to extract real Dofus assets
    Write-Host "Extracting character sprites..." -ForegroundColor Yellow

    $characterFiles = @{
        "Feca" = "$DofusPath\gfx\sprites\actors\characters\11.swf"
        "Osamodas" = "$DofusPath\gfx\sprites\actors\characters\10.swf"
        "Enutrof" = "$DofusPath\gfx\sprites\actors\characters\12.swf"
    }

    foreach ($char in $characterFiles.Keys) {
        if (Test-Path $characterFiles[$char]) {
            Write-Host "  Extracting $char..." -ForegroundColor Cyan
            & $FFDecPath -export image "$OutputPath\Characters\$char" $characterFiles[$char]
        }
    }
} else {
    Write-Host "FFDec or Dofus not found - generating test assets..." -ForegroundColor Yellow
}

# Generate test assets (always do this for demonstration)
Write-Host ""
Write-Host "Generating test assets for pipeline validation..." -ForegroundColor Yellow

# Character sprites (8 directions x 4 animations = 32 sprites)
$directions = @("north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest")
$animations = @("idle", "walk", "attack", "cast")
$charColors = @{
    "idle" = [System.Drawing.Color]::FromArgb(100, 150, 200)
    "walk" = [System.Drawing.Color]::FromArgb(100, 200, 100)
    "attack" = [System.Drawing.Color]::FromArgb(200, 100, 100)
    "cast" = [System.Drawing.Color]::FromArgb(200, 100, 200)
}

$spriteCount = 0
foreach ($anim in $animations) {
    foreach ($dir in $directions) {
        $fileName = "$OutputPath\Characters\TestCharacter\$($anim)_$($dir)_0.png"
        Create-TestImage -Path $fileName -Width 64 -Height 64 -Color $charColors[$anim] -Text "$anim-$dir"
        $spriteCount++
        Write-Progress -Activity "Creating character sprites" -Status "$anim $dir" -PercentComplete (($spriteCount / 32) * 100)
    }
}
Write-Host "  Created 32 character sprites" -ForegroundColor Green

# UI Elements
Write-Host "Generating UI elements..." -ForegroundColor Yellow

# Buttons
Create-TestImage -Path "$OutputPath\UI\Buttons\btn_normal.png" -Width 128 -Height 48 `
    -Color ([System.Drawing.Color]::FromArgb(180, 180, 180)) -Text "Normal"
Create-TestImage -Path "$OutputPath\UI\Buttons\btn_hover.png" -Width 128 -Height 48 `
    -Color ([System.Drawing.Color]::FromArgb(200, 200, 200)) -Text "Hover"
Create-TestImage -Path "$OutputPath\UI\Buttons\btn_pressed.png" -Width 128 -Height 48 `
    -Color ([System.Drawing.Color]::FromArgb(150, 150, 150)) -Text "Pressed"
Create-TestImage -Path "$OutputPath\UI\Buttons\btn_disabled.png" -Width 128 -Height 48 `
    -Color ([System.Drawing.Color]::FromArgb(100, 100, 100)) -Text "Disabled"

# Windows
Create-TestImage -Path "$OutputPath\UI\Windows\window_frame.png" -Width 256 -Height 256 `
    -Color ([System.Drawing.Color]::FromArgb(60, 60, 80)) -Text "Window"
Create-TestImage -Path "$OutputPath\UI\Windows\window_close.png" -Width 32 -Height 32 `
    -Color ([System.Drawing.Color]::FromArgb(200, 50, 50)) -Text "X"

# Icons
$icons = @("sword", "shield", "potion", "spell", "coin", "bag", "book", "gem")
$iconColors = @{
    "sword" = [System.Drawing.Color]::Silver
    "shield" = [System.Drawing.Color]::Gold
    "potion" = [System.Drawing.Color]::Red
    "spell" = [System.Drawing.Color]::Purple
    "coin" = [System.Drawing.Color]::Gold
    "bag" = [System.Drawing.Color]::Brown
    "book" = [System.Drawing.Color]::Blue
    "gem" = [System.Drawing.Color]::Cyan
}

foreach ($icon in $icons) {
    Create-TestImage -Path "$OutputPath\UI\Icons\icon_$icon.png" -Width 32 -Height 32 `
        -Color $iconColors[$icon] -Text $icon.Substring(0,1).ToUpper()
}

Write-Host "  Created UI elements (buttons, windows, icons)" -ForegroundColor Green

# Map Tiles
Write-Host "Generating map tiles..." -ForegroundColor Yellow

$tileTypes = @{
    "grass" = [System.Drawing.Color]::FromArgb(50, 150, 50)
    "stone" = [System.Drawing.Color]::FromArgb(128, 128, 128)
    "sand" = [System.Drawing.Color]::FromArgb(200, 180, 100)
    "water" = [System.Drawing.Color]::FromArgb(50, 100, 200)
    "dirt" = [System.Drawing.Color]::FromArgb(139, 69, 19)
}

foreach ($tileType in $tileTypes.Keys) {
    for ($i = 1; $i -le 3; $i++) {
        $fileName = "$OutputPath\Maps\Tiles\$($tileType)_$('{0:D2}' -f $i).png"
        $color = $tileTypes[$tileType]
        # Add variation
        $r = [Math]::Min(255, $color.R + ($i * 10))
        $g = [Math]::Min(255, $color.G + ($i * 10))
        $b = [Math]::Min(255, $color.B + ($i * 10))
        $variedColor = [System.Drawing.Color]::FromArgb($r, $g, $b)
        Create-TestImage -Path $fileName -Width 64 -Height 32 -Color $variedColor -Text "$tileType$i"
    }
}

Write-Host "  Created 15 map tiles" -ForegroundColor Green

# Effects
Write-Host "Generating effect sprites..." -ForegroundColor Yellow

$effects = @("fire", "ice", "lightning", "heal", "poison")
$effectColors = @{
    "fire" = [System.Drawing.Color]::Orange
    "ice" = [System.Drawing.Color]::LightBlue
    "lightning" = [System.Drawing.Color]::Yellow
    "heal" = [System.Drawing.Color]::LightGreen
    "poison" = [System.Drawing.Color]::Purple
}

foreach ($effect in $effects) {
    for ($frame = 0; $frame -lt 4; $frame++) {
        $fileName = "$OutputPath\Effects\$($effect)_$frame.png"
        Create-TestImage -Path $fileName -Width 64 -Height 64 `
            -Color $effectColors[$effect] -Text "$effect-$frame"
    }
}

Write-Host "  Created 20 effect sprites" -ForegroundColor Green

# Create a summary file
$summaryPath = "$OutputPath\extraction_summary.txt"
@"
GOFUS Asset Extraction Summary
Generated: $(Get-Date)

Assets Created:
- Characters: 32 sprites (8 directions x 4 animations)
- UI Buttons: 4 states
- UI Windows: 2 elements
- UI Icons: 8 items
- Map Tiles: 15 tiles (5 types x 3 variants)
- Effects: 20 sprites (5 effects x 4 frames)

Total: 83 test assets

Next Steps:
1. Open Unity
2. Go to GOFUS > Asset Migration > Extraction Validator
3. Click "Run Validation"
4. Click "Process Assets" to import
"@ | Out-File -FilePath $summaryPath

Write-Host ""
Write-Host "========================================"
Write-Host "Extraction Complete!" -ForegroundColor Green
Write-Host "========================================"
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  - Total assets created: 83"
Write-Host "  - Output location: $OutputPath"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Open Unity"
Write-Host "  2. Menu: GOFUS > Asset Migration > Extraction Validator"
Write-Host "  3. Click 'Run Validation' to verify"
Write-Host "  4. Click 'Process Assets' to import"
Write-Host ""

# Open the output folder
Start-Process explorer.exe $OutputPath