$Principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $Principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be run as Administrator."
    exit
}

$source = "IndexMaintenanceSystem"
$logName = "Application"

if (-not [System.Diagnostics.EventLog]::SourceExists($source)) {
    New-EventLog -LogName $logName -Source $source
    Write-Host "Event Log Source '$source' created successfully in $logName log."
} else {
    Write-Host "Event Log Source '$source' already exists."
}
