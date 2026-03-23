$exePath = Join-Path $PSScriptRoot "IndexMaintenanceSystem.exe"

if (-not (Test-Path $exePath)) {
    Write-Error "Could not find IndexMaintenanceSystem.exe in $currentPath. Please run this script from the directory containing the executable."
    exit
}

$Principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $Principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be run as Administrator."
    exit
}

$serviceName = "IndexMaintenanceSystem"
$displayName = "Index Maintenance System"

if (Get-Service $serviceName -ErrorAction SilentlyContinue) {
    Write-Host "Service $serviceName already exists. Removing it first..."
    Stop-Service $serviceName -ErrorAction SilentlyContinue
    sc.exe delete $serviceName
    Start-Sleep -Seconds 2
}

New-Service -Name $serviceName `
            -BinaryPathName "`"$exePath`""   `
            -DisplayName $displayName `
            -Description "Automated SQL Server Index Maintenance Service with Web Dashboard." `
            -StartupType Automatic

Write-Host "Service '$displayName' has been successfully installed."
Write-Host "You can start it manually or run: Start-Service $serviceName"
