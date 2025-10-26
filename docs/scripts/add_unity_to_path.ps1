# PowerShell script to add Unity to PATH
# Run this script as Administrator

$unityPath = "C:\Program Files\Unity\Hub\Editor\6000.0.60f1\Editor"

# Get current PATH
$currentPath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::Machine)

# Check if Unity is already in PATH
if ($currentPath -notlike "*$unityPath*") {
    # Add Unity to PATH
    $newPath = $currentPath + ";" + $unityPath
    [Environment]::SetEnvironmentVariable("Path", $newPath, [System.EnvironmentVariableTarget]::Machine)
    Write-Host "Unity has been added to PATH successfully!" -ForegroundColor Green
    Write-Host "Path added: $unityPath" -ForegroundColor Yellow
    Write-Host "Please restart your command prompt or PowerShell for changes to take effect." -ForegroundColor Cyan
} else {
    Write-Host "Unity is already in PATH!" -ForegroundColor Yellow
}