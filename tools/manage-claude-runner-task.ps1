[CmdletBinding()]
param(
    [string]$WorkingDirectory = "",
    [ValidateRange(15, 3600)]
    [int]$PollSeconds = 60,
    [switch]$AllowPendingReview,
    [switch]$Uninstall,
    [switch]$StartNow
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$taskName = "Elyndor Claude Queue Runner"

if ([string]::IsNullOrWhiteSpace($WorkingDirectory)) {
    $WorkingDirectory = Split-Path -Parent $PSScriptRoot
}

$WorkingDirectory = [System.IO.Path]::GetFullPath($WorkingDirectory)
$runnerPath = Join-Path $PSScriptRoot "claude-queue-runner.ps1"

if ($Uninstall) {
    $existing = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    if ($null -ne $existing) {
        Stop-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
        Write-Host "Removed scheduled task: $taskName"
    }
    else {
        Write-Host "Scheduled task does not exist: $taskName"
    }

    exit 0
}

if (-not (Test-Path -LiteralPath $runnerPath -PathType Leaf)) {
    throw "Runner script not found: $runnerPath"
}

$pwsh = Get-Command pwsh.exe -ErrorAction SilentlyContinue
if ($null -ne $pwsh) {
    $engine = $pwsh.Source
}
else {
    $powershell = Get-Command powershell.exe -ErrorAction Stop
    $engine = $powershell.Source
}

$argumentText = '-NoLogo -NoProfile -ExecutionPolicy Bypass -File "{0}" -WorkingDirectory "{1}" -PollSeconds {2}' -f `
    $runnerPath, $WorkingDirectory, $PollSeconds

if ($AllowPendingReview) {
    $argumentText += " -AllowPendingReview"
}

$action = New-ScheduledTaskAction `
    -Execute $engine `
    -Argument $argumentText `
    -WorkingDirectory $WorkingDirectory

$trigger = New-ScheduledTaskTrigger -AtLogOn
$currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
$principal = New-ScheduledTaskPrincipal `
    -UserId $currentUser `
    -LogonType Interactive `
    -RunLevel Limited

$settings = New-ScheduledTaskSettingsSet `
    -MultipleInstances IgnoreNew `
    -RestartCount 3 `
    -RestartInterval (New-TimeSpan -Minutes 1) `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -ExecutionTimeLimit ([TimeSpan]::Zero)

Register-ScheduledTask `
    -TaskName $taskName `
    -Action $action `
    -Trigger $trigger `
    -Principal $principal `
    -Settings $settings `
    -Description "Checks the Echoes of Elyndor Claude GitHub queue and starts exactly one Claude task at a time." `
    -Force | Out-Null

Write-Host "Installed scheduled task: $taskName"
Write-Host "Runner: $runnerPath"
Write-Host "Working directory: $WorkingDirectory"

if ($StartNow) {
    Start-ScheduledTask -TaskName $taskName
    Write-Host "Scheduled task started."
}
