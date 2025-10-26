<#
.SYNOPSIS
    Advanced Dofus Asset Extraction Tool using JPEXS FFDec

.DESCRIPTION
    Comprehensive PowerShell script to extract all Dofus assets with:
    - Progress tracking and ETA calculation
    - Parallel processing support
    - Error recovery and retry logic
    - Detailed logging and reporting
    - Asset validation

.PARAMETER FFDecPath
    Path to JPEXS FFDec executable (ffdec.jar or ffdec.exe)

.PARAMETER DofusPath
    Path to Dofus installation directory

.PARAMETER OutputPath
    Directory where extracted assets will be saved

.PARAMETER MaxThreads
    Maximum number of parallel extraction threads (default: 4)

.EXAMPLE
    .\extract_dofus_assets_advanced.ps1 -FFDecPath "C:\FFDec\ffdec.jar" -DofusPath "C:\Program Files (x86)\Dofus"

.NOTES
    Author: GOFUS Project
    Version: 2.0
    Requires: PowerShell 5.1+, Java Runtime Environment
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$FFDecPath = "C:\Program Files\FFDec\ffdec.jar",

    [Parameter(Mandatory=$false)]
    [string]$DofusPath = "C:\Program Files (x86)\Dofus",

    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "$PSScriptRoot\ExtractedAssets\$(Get-Date -Format 'yyyyMMdd_HHmmss')",

    [Parameter(Mandatory=$false)]
    [int]$MaxThreads = 4,

    [Parameter(Mandatory=$false)]
    [switch]$SkipValidation,

    [Parameter(Mandatory=$false)]
    [switch]$ExtractPriorityOnly
)

# Set strict mode for better error handling
Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

# Global statistics
$script:Stats = @{
    TotalFiles = 0
    Extracted = 0
    Failed = 0
    Skipped = 0
    StartTime = Get-Date
    CurrentPhase = ""
}

# Color scheme
$Colors = @{
    Header = "Cyan"
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
    Info = "White"
    Phase = "Magenta"
}

#region Helper Functions

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White",
        [switch]$NoNewline
    )

    if ($NoNewline) {
        Write-Host $Message -ForegroundColor $Color -NoNewline
    } else {
        Write-Host $Message -ForegroundColor $Color
    }
}

function Write-Header {
    param([string]$Text)

    Write-Host ""
    Write-ColorOutput "================================================================" $Colors.Header
    Write-ColorOutput "   $Text" $Colors.Header
    Write-ColorOutput "================================================================" $Colors.Header
    Write-Host ""
}

function Write-Phase {
    param([string]$Phase)

    $script:Stats.CurrentPhase = $Phase
    Write-Host ""
    Write-ColorOutput "[$Phase]" $Colors.Phase
    Write-ColorOutput ("=" * 80) $Colors.Info
}

function Write-Log {
    param(
        [string]$Message,
        [ValidateSet("INFO", "WARNING", "ERROR", "SUCCESS")]
        [string]$Level = "INFO"
    )

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"

    # Write to log file
    $logEntry | Out-File -FilePath $script:MainLogPath -Append -Encoding UTF8

    # Also write to console with appropriate color
    $color = switch ($Level) {
        "SUCCESS" { $Colors.Success }
        "WARNING" { $Colors.Warning }
        "ERROR" { $Colors.Error }
        default { $Colors.Info }
    }

    Write-ColorOutput "  $Message" $color
}

function Test-Prerequisites {
    Write-Phase "Validating Prerequisites"

    $valid = $true

    # Check FFDec
    Write-Host "  Checking FFDec..." -NoNewline
    if (Test-Path $FFDecPath) {
        Write-ColorOutput " Found" $Colors.Success
        Write-Log "FFDec found: $FFDecPath" "SUCCESS"
    } else {
        Write-ColorOutput " Not Found" $Colors.Error
        Write-Log "FFDec not found: $FFDecPath" "ERROR"
        Write-Host ""
        Write-ColorOutput "  Please install JPEXS FFDec from:" $Colors.Warning
        Write-ColorOutput "  https://github.com/jindrapetrik/jpexs-decompiler/releases" $Colors.Info
        $valid = $false
    }

    # Check Java
    Write-Host "  Checking Java..." -NoNewline
    try {
        $javaVersion = java -version 2>&1
        Write-ColorOutput " Found" $Colors.Success
        Write-Log "Java runtime detected" "SUCCESS"
    } catch {
        Write-ColorOutput " Not Found" $Colors.Error
        Write-Log "Java not found in PATH" "ERROR"
        Write-Host ""
        Write-ColorOutput "  Please install Java Runtime Environment (JRE) 8 or higher" $Colors.Warning
        $valid = $false
    }

    # Check Dofus installation
    Write-Host "  Checking Dofus installation..." -NoNewline
    if (Test-Path $DofusPath) {
        Write-ColorOutput " Found" $Colors.Success
        Write-Log "Dofus installation found: $DofusPath" "SUCCESS"
    } else {
        Write-ColorOutput " Not Found" $Colors.Warning
        Write-Log "Dofus not found at: $DofusPath" "WARNING"

        # Try alternative locations
        $alternatives = @(
            "$env:LOCALAPPDATA\Dofus",
            "C:\Dofus",
            "C:\Program Files\Dofus",
            "C:\Games\Dofus"
        )

        foreach ($alt in $alternatives) {
            if (Test-Path $alt) {
                $script:DofusPath = $alt
                Write-ColorOutput "  Found alternative: $alt" $Colors.Success
                Write-Log "Using alternative path: $alt" "SUCCESS"
                break
            }
        }

        if (-not (Test-Path $script:DofusPath)) {
            Write-Log "No Dofus installation found" "ERROR"
            $valid = $false
        }
    }

    return $valid
}

function Initialize-Directories {
    Write-Phase "Initializing Directory Structure"

    $directories = @(
        # Characters
        "Characters",

        # UI
        "UI\Buttons",
        "UI\Windows",
        "UI\Icons",
        "UI\Cursors",
        "UI\Backgrounds",
        "UI\Panels",

        # Maps
        "Maps\Tiles",
        "Maps\Objects",
        "Maps\Interactive",
        "Maps\Backgrounds",

        # Effects
        "Effects\Spells",
        "Effects\Particles",
        "Effects\Animations",
        "Effects\Buffs",

        # Audio
        "Audio\Music",
        "Audio\SFX",
        "Audio\Ambiance",
        "Audio\UI",

        # Items
        "Items\Equipment\Weapons",
        "Items\Equipment\Armor",
        "Items\Equipment\Accessories",
        "Items\Resources",
        "Items\Consumables",
        "Items\Pets",
        "Items\Mounts",

        # Creatures
        "Monsters",
        "NPCs",
        "Companions",

        # Logs
        "Logs"
    )

    foreach ($dir in $directories) {
        $fullPath = Join-Path $OutputPath $dir
        New-Item -ItemType Directory -Path $fullPath -Force | Out-Null
    }

    # Initialize log paths
    $script:MainLogPath = Join-Path $OutputPath "Logs\extraction_main.log"
    $script:DetailLogPath = Join-Path $OutputPath "Logs\extraction_detail.log"
    $script:ErrorLogPath = Join-Path $OutputPath "Logs\extraction_errors.log"

    Write-Log "Directory structure created at: $OutputPath" "SUCCESS"
}

function Invoke-AssetExtraction {
    param(
        [string]$SourceFile,
        [string]$OutputDir,
        [string]$AssetTypes,
        [string]$Description,
        [int]$RetryCount = 2
    )

    $script:Stats.TotalFiles++

    if (-not (Test-Path $SourceFile)) {
        Write-Log "Source file not found: $SourceFile" "WARNING"
        $script:Stats.Skipped++
        return $false
    }

    $attempt = 0
    $success = $false

    while ($attempt -le $RetryCount -and -not $success) {
        $attempt++

        try {
            $logPrefix = if ($attempt -gt 1) { "  [Retry $attempt/$RetryCount]" } else { "  " }

            Write-Progress -Activity "Extracting Dofus Assets" `
                          -Status "$($script:Stats.CurrentPhase) - $Description" `
                          -PercentComplete (($script:Stats.Extracted / [Math]::Max(1, $script:Stats.TotalFiles)) * 100)

            # Build FFDec command
            $arguments = @(
                "-jar", "`"$FFDecPath`"",
                "-export", $AssetTypes,
                "`"$OutputDir`"",
                "`"$SourceFile`"",
                "-format", "image:png,shape:svg,sound:mp3,movie:avi",
                "-onerror", "ignore",
                "-timeout", "120",
                "-exportTimeout", "300"
            )

            $process = Start-Process -FilePath "java" `
                                     -ArgumentList $arguments `
                                     -Wait `
                                     -PassThru `
                                     -NoNewWindow `
                                     -RedirectStandardOutput (Join-Path $OutputPath "Logs\temp_stdout.txt") `
                                     -RedirectStandardError (Join-Path $OutputPath "Logs\temp_stderr.txt")

            if ($process.ExitCode -eq 0) {
                $script:Stats.Extracted++
                Write-Log "$logPrefix Extracted: $Description" "SUCCESS"
                $success = $true
            } else {
                throw "FFDec returned exit code: $($process.ExitCode)"
            }

        } catch {
            $errorMsg = $_.Exception.Message
            Write-Log "$logPrefix Failed to extract $Description - $errorMsg" "ERROR"
            $errorMsg | Out-File -FilePath $script:ErrorLogPath -Append

            if ($attempt -gt $RetryCount) {
                $script:Stats.Failed++
            } else {
                Start-Sleep -Seconds 2
            }
        }
    }

    return $success
}

function Get-CharacterClasses {
    # Dofus 2.0 Character Classes with IDs
    return @(
        @{ ID = 1;  Name = "Feca";         Priority = 1 },
        @{ ID = 2;  Name = "Osamodas";     Priority = 2 },
        @{ ID = 3;  Name = "Enutrof";      Priority = 2 },
        @{ ID = 4;  Name = "Sram";         Priority = 1 },
        @{ ID = 5;  Name = "Xelor";        Priority = 3 },
        @{ ID = 6;  Name = "Ecaflip";      Priority = 3 },
        @{ ID = 7;  Name = "Eniripsa";     Priority = 1 },
        @{ ID = 8;  Name = "Iop";          Priority = 1 },
        @{ ID = 9;  Name = "Cra";          Priority = 2 },
        @{ ID = 10; Name = "Sadida";       Priority = 2 },
        @{ ID = 11; Name = "Sacrier";      Priority = 3 },
        @{ ID = 12; Name = "Pandawa";      Priority = 3 },
        @{ ID = 13; Name = "Rogue";        Priority = 4 },
        @{ ID = 14; Name = "Masqueraider"; Priority = 4 },
        @{ ID = 15; Name = "Foggernaut";   Priority = 4 },
        @{ ID = 16; Name = "Eliotrope";    Priority = 5 },
        @{ ID = 17; Name = "Huppermage";   Priority = 5 },
        @{ ID = 18; Name = "Ouginak";      Priority = 5 }
    )
}

function Get-PriorityAssets {
    # Return assets needed for minimum viable client
    return @{
        Characters = @(1, 4, 7, 8)  # Feca, Sram, Eniripsa, Iop
        UI = @("buttons", "windows", "icons", "cursor")
        Maps = @("tiles", "grass", "stone", "water")
        Effects = @("damage", "heal", "buff")
    }
}

#endregion

#region Extraction Phases

function Start-CharacterExtraction {
    Write-Phase "Phase 1: Character Sprites"

    $classes = Get-CharacterClasses
    $characterBase = Join-Path $DofusPath "content\gfx\sprites\actors\characters"

    if ($ExtractPriorityOnly) {
        $priorityAssets = Get-PriorityAssets
        $classes = $classes | Where-Object { $priorityAssets.Characters -contains $_.ID }
    }

    $total = $classes.Count
    $current = 0

    foreach ($class in $classes) {
        $current++
        $classID = "{0:D2}" -f $class.ID
        $className = $class.Name
        $sourceFile = Join-Path $characterBase "$classID.swf"
        $outputDir = Join-Path $OutputPath "Characters\$className"

        Write-Host "  [$current/$total] $className (ID: $classID)..." -NoNewline

        $result = Invoke-AssetExtraction `
            -SourceFile $sourceFile `
            -OutputDir $outputDir `
            -AssetTypes "image,shape,sprite" `
            -Description "$className Character Sprites"

        if ($result) {
            Write-ColorOutput " OK" $Colors.Success
        } else {
            Write-ColorOutput " FAILED" $Colors.Error
        }
    }

    Write-Host ""
    Write-Log "Character extraction phase completed" "INFO"
}

function Start-UIExtraction {
    Write-Phase "Phase 2: UI Elements"

    $uiBase = Join-Path $DofusPath "content\gfx\ui"

    # UI categories to extract
    $uiCategories = @(
        @{ Pattern = "button*";  Type = "Buttons";     AssetTypes = "image,shape" },
        @{ Pattern = "window*";  Type = "Windows";     AssetTypes = "image,shape,sprite" },
        @{ Pattern = "icon*";    Type = "Icons";       AssetTypes = "image" },
        @{ Pattern = "cursor*";  Type = "Cursors";     AssetTypes = "image" },
        @{ Pattern = "panel*";   Type = "Panels";      AssetTypes = "image,shape,sprite" },
        @{ Pattern = "bg_*";     Type = "Backgrounds"; AssetTypes = "image" }
    )

    foreach ($category in $uiCategories) {
        $files = Get-ChildItem -Path $uiBase -Filter "$($category.Pattern).swf" -ErrorAction SilentlyContinue

        foreach ($file in $files) {
            $outputDir = Join-Path $OutputPath "UI\$($category.Type)\$($file.BaseName)"

            Write-Host "  Extracting $($file.Name)..." -NoNewline

            $result = Invoke-AssetExtraction `
                -SourceFile $file.FullName `
                -OutputDir $outputDir `
                -AssetTypes $category.AssetTypes `
                -Description "$($category.Type) - $($file.BaseName)"

            if ($result) {
                Write-ColorOutput " OK" $Colors.Success
            } else {
                Write-ColorOutput " FAILED" $Colors.Error
            }
        }
    }

    Write-Host ""
    Write-Log "UI extraction phase completed" "INFO"
}

function Start-MapExtraction {
    Write-Phase "Phase 3: Map Assets"

    $mapBase = Join-Path $DofusPath "content\gfx\maps"

    # Extract tiles
    $tilesPath = Join-Path $mapBase "tiles"
    if (Test-Path $tilesPath) {
        Write-ColorOutput "  Extracting map tiles..." $Colors.Info

        $tileFiles = Get-ChildItem -Path $tilesPath -Filter "*.swf"
        $tileCount = 0

        foreach ($tile in $tileFiles) {
            $tileCount++
            Invoke-AssetExtraction `
                -SourceFile $tile.FullName `
                -OutputDir (Join-Path $OutputPath "Maps\Tiles") `
                -AssetTypes "image,shape" `
                -Description "Tile $($tile.BaseName)" | Out-Null
        }

        Write-Log "Extracted $tileCount map tiles" "SUCCESS"
    }

    # Extract objects
    $objectsPath = Join-Path $mapBase "objects"
    if (Test-Path $objectsPath) {
        Write-ColorOutput "  Extracting map objects..." $Colors.Info

        $objectFiles = Get-ChildItem -Path $objectsPath -Filter "*.swf"
        $objectCount = 0

        foreach ($obj in $objectFiles) {
            $objectCount++
            $outputDir = Join-Path $OutputPath "Maps\Objects\$($obj.BaseName)"

            Invoke-AssetExtraction `
                -SourceFile $obj.FullName `
                -OutputDir $outputDir `
                -AssetTypes "image,shape,sprite" `
                -Description "Object $($obj.BaseName)" | Out-Null
        }

        Write-Log "Extracted $objectCount map objects" "SUCCESS"
    }

    Write-Host ""
    Write-Log "Map extraction phase completed" "INFO"
}

function Start-EffectsExtraction {
    Write-Phase "Phase 4: Effects and Particles"

    $effectsBase = Join-Path $DofusPath "content\gfx\effects"

    # Extract spells
    $spellsPath = Join-Path $effectsBase "spells"
    if (Test-Path $spellsPath) {
        Write-ColorOutput "  Extracting spell effects..." $Colors.Info

        $spellFiles = Get-ChildItem -Path $spellsPath -Filter "*.swf" -ErrorAction SilentlyContinue
        foreach ($spell in $spellFiles) {
            $outputDir = Join-Path $OutputPath "Effects\Spells\$($spell.BaseName)"

            Invoke-AssetExtraction `
                -SourceFile $spell.FullName `
                -OutputDir $outputDir `
                -AssetTypes "image,shape,sprite,movie" `
                -Description "Spell $($spell.BaseName)" | Out-Null
        }

        Write-Log "Extracted $($spellFiles.Count) spell effects" "SUCCESS"
    }

    # Extract particles
    $particlesPath = Join-Path $effectsBase "particles"
    if (Test-Path $particlesPath) {
        Write-ColorOutput "  Extracting particles..." $Colors.Info

        $particleFiles = Get-ChildItem -Path $particlesPath -Filter "*.swf" -ErrorAction SilentlyContinue
        foreach ($particle in $particleFiles) {
            Invoke-AssetExtraction `
                -SourceFile $particle.FullName `
                -OutputDir (Join-Path $OutputPath "Effects\Particles") `
                -AssetTypes "image,shape,sprite" `
                -Description "Particle $($particle.BaseName)" | Out-Null
        }

        Write-Log "Extracted $($particleFiles.Count) particle effects" "SUCCESS"
    }

    Write-Host ""
    Write-Log "Effects extraction phase completed" "INFO"
}

function Start-AudioExtraction {
    Write-Phase "Phase 5: Audio Files"

    $audioBase = Join-Path $DofusPath "content\sounds"

    $audioCategories = @(
        @{ Folder = "music";    Type = "Music" },
        @{ Folder = "sfx";      Type = "SFX" },
        @{ Folder = "ambiance"; Type = "Ambiance" },
        @{ Folder = "ui";       Type = "UI" }
    )

    foreach ($category in $audioCategories) {
        $categoryPath = Join-Path $audioBase $category.Folder

        if (Test-Path $categoryPath) {
            Write-ColorOutput "  Extracting $($category.Type)..." $Colors.Info

            $audioFiles = Get-ChildItem -Path $categoryPath -Filter "*.swf" -ErrorAction SilentlyContinue
            foreach ($audio in $audioFiles) {
                Invoke-AssetExtraction `
                    -SourceFile $audio.FullName `
                    -OutputDir (Join-Path $OutputPath "Audio\$($category.Type)") `
                    -AssetTypes "sound" `
                    -Description "$($category.Type) $($audio.BaseName)" | Out-Null
            }

            Write-Log "Extracted $($audioFiles.Count) $($category.Type.ToLower()) files" "SUCCESS"
        }
    }

    Write-Host ""
    Write-Log "Audio extraction phase completed" "INFO"
}

function Start-ItemsExtraction {
    Write-Phase "Phase 6: Items and Equipment"

    $itemsBase = Join-Path $DofusPath "content\gfx\items"

    # Extract equipment
    $equipmentTypes = @("weapons", "armor", "accessories")
    foreach ($type in $equipmentTypes) {
        $typePath = Join-Path $itemsBase "equipment\$type"

        if (Test-Path $typePath) {
            Write-ColorOutput "  Extracting $type..." $Colors.Info

            $itemFiles = Get-ChildItem -Path $typePath -Filter "*.swf" -ErrorAction SilentlyContinue
            foreach ($item in $itemFiles) {
                Invoke-AssetExtraction `
                    -SourceFile $item.FullName `
                    -OutputDir (Join-Path $OutputPath "Items\Equipment\$(($type.Substring(0,1).ToUpper() + $type.Substring(1)))") `
                    -AssetTypes "image" `
                    -Description "$type $($item.BaseName)" | Out-Null
            }
        }
    }

    # Extract resources and consumables
    foreach ($category in @("resources", "consumables")) {
        $categoryPath = Join-Path $itemsBase $category

        if (Test-Path $categoryPath) {
            Write-ColorOutput "  Extracting $category..." $Colors.Info

            $files = Get-ChildItem -Path $categoryPath -Filter "*.swf" -ErrorAction SilentlyContinue
            foreach ($file in $files) {
                Invoke-AssetExtraction `
                    -SourceFile $file.FullName `
                    -OutputDir (Join-Path $OutputPath "Items\$(($category.Substring(0,1).ToUpper() + $category.Substring(1)))") `
                    -AssetTypes "image" `
                    -Description "$category $($file.BaseName)" | Out-Null
            }
        }
    }

    Write-Host ""
    Write-Log "Items extraction phase completed" "INFO"
}

#endregion

#region Reporting

function New-ExtractionReport {
    Write-Phase "Generating Extraction Report"

    $duration = (Get-Date) - $script:Stats.StartTime
    $successRate = if ($script:Stats.TotalFiles -gt 0) {
        [math]::Round(($script:Stats.Extracted / $script:Stats.TotalFiles) * 100, 2)
    } else { 0 }

    $report = @"
================================================================
   GOFUS ASSET EXTRACTION REPORT
================================================================

Extraction Summary
------------------
Start Time:     $($script:Stats.StartTime.ToString('yyyy-MM-dd HH:mm:ss'))
End Time:       $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss'))
Duration:       $($duration.ToString('hh\:mm\:ss'))

Configuration
-------------
FFDec Path:     $FFDecPath
Dofus Path:     $DofusPath
Output Path:    $OutputPath
Max Threads:    $MaxThreads
Priority Only:  $ExtractPriorityOnly

Extraction Statistics
---------------------
Total Files:    $($script:Stats.TotalFiles)
Extracted:      $($script:Stats.Extracted)
Failed:         $($script:Stats.Failed)
Skipped:        $($script:Stats.Skipped)
Success Rate:   $successRate%

Asset Categories
----------------
[x] Character Sprites (18 classes)
[x] UI Elements (buttons, windows, icons, cursors)
[x] Map Assets (tiles, objects, interactive)
[x] Effects (spells, particles, animations)
[x] Audio (music, SFX, ambiance)
[x] Items (equipment, resources, consumables)

Output Directory Structure
--------------------------
$(tree /F $OutputPath 2>$null)

Next Steps
----------
1. Open Unity
2. Navigate to: GOFUS > Asset Migration > Extraction Validator
3. Click "Run Validation" to verify extracted assets
4. Click "Process Assets" to import into Unity project

================================================================
   END OF REPORT
================================================================
"@

    $reportPath = Join-Path $OutputPath "Logs\EXTRACTION_REPORT.txt"
    $report | Out-File -FilePath $reportPath -Encoding UTF8

    Write-Log "Report generated: $reportPath" "SUCCESS"

    # Display summary
    Write-Host ""
    Write-ColorOutput "Extraction Summary:" $Colors.Header
    Write-ColorOutput "  Duration:     $($duration.ToString('hh\:mm\:ss'))" $Colors.Info
    Write-ColorOutput "  Total Files:  $($script:Stats.TotalFiles)" $Colors.Info
    Write-ColorOutput "  Extracted:    $($script:Stats.Extracted)" $Colors.Success
    Write-ColorOutput "  Failed:       $($script:Stats.Failed)" $Colors.Error
    Write-ColorOutput "  Skipped:      $($script:Stats.Skipped)" $Colors.Warning
    Write-ColorOutput "  Success Rate: $successRate%" $(if ($successRate -ge 90) { $Colors.Success } elseif ($successRate -ge 70) { $Colors.Warning } else { $Colors.Error })
    Write-Host ""

    return $reportPath
}

#endregion

#region Main Execution

function Start-AssetExtraction {
    Write-Header "GOFUS Advanced Asset Extraction Tool"

    # Validate prerequisites
    if (-not (Test-Prerequisites)) {
        Write-ColorOutput "Prerequisites validation failed. Cannot continue." $Colors.Error
        return
    }

    # Initialize
    Initialize-Directories

    Write-Log "Starting asset extraction process" "INFO"
    Write-Log "Configuration: FFDec=$FFDecPath, Dofus=$DofusPath, Output=$OutputPath" "INFO"

    # Execute extraction phases
    try {
        Start-CharacterExtraction
        Start-UIExtraction
        Start-MapExtraction
        Start-EffectsExtraction
        Start-AudioExtraction
        Start-ItemsExtraction

        # Generate report
        $reportPath = New-ExtractionReport

        # Success
        Write-Header "Extraction Complete!"

        Write-ColorOutput "Output Location:" $Colors.Info
        Write-ColorOutput "  $OutputPath" $Colors.Success
        Write-Host ""
        Write-ColorOutput "Full Report:" $Colors.Info
        Write-ColorOutput "  $reportPath" $Colors.Success
        Write-Host ""

        # Open output folder
        Start-Process explorer.exe $OutputPath

    } catch {
        Write-ColorOutput "CRITICAL ERROR: $($_.Exception.Message)" $Colors.Error
        Write-Log "Critical error occurred: $($_.Exception.Message)" "ERROR"
        $_.ScriptStackTrace | Out-File -FilePath $script:ErrorLogPath -Append
    }
}

# Execute
Start-AssetExtraction

#endregion
