param(
    [string]$Destination = "",
    [int]$StartupDelaySec = 12,
    [int]$CaptureTimeoutSec = 8,
    [switch]$KeepOpen
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public static class PowerState
{
    [DllImport("kernel32.dll")]
    public static extern uint SetThreadExecutionState(uint esFlags);
}
"@

Add-Type -AssemblyName System.Windows.Forms

$ES_CONTINUOUS = [uint32]2147483648
$ES_SYSTEM_REQUIRED = [uint32]1
$ES_DISPLAY_REQUIRED = [uint32]2
$ES_AWAYMODE_REQUIRED = [uint32]64

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$gameRoot = Split-Path -Parent $scriptDir
$repoRoot = Split-Path -Parent $gameRoot

if (-not $Destination) {
    $Destination = Join-Path $repoRoot "example.png"
}

$testGame = Join-Path $scriptDir "test-game.cmd"
$capture = Join-Path $scriptDir "capture-openra-screenshot.ps1"

if (-not (Test-Path $testGame)) {
    throw "Missing test-game script: $testGame"
}

if (-not (Test-Path $capture)) {
    throw "Missing capture script: $capture"
}

[void][PowerState]::SetThreadExecutionState($ES_CONTINUOUS -bor $ES_SYSTEM_REQUIRED -bor $ES_DISPLAY_REQUIRED)

$cursor = [System.Windows.Forms.Cursor]::Position
[System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point(([int]$cursor.X + 1), [int]$cursor.Y)
Start-Sleep -Milliseconds 50
[System.Windows.Forms.Cursor]::Position = $cursor

$launcher = Start-Process -FilePath "cmd.exe" `
    -ArgumentList "/c", $testGame `
    -WorkingDirectory $gameRoot `
    -WindowStyle Hidden `
    -PassThru

try {
    $deadline = (Get-Date).AddSeconds(120)
    $openra = $null

    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 500
        $openra = Get-Process OpenRA -ErrorAction SilentlyContinue |
            Where-Object { $_.MainWindowHandle -ne 0 } |
            Sort-Object StartTime -Descending |
            Select-Object -First 1

        if ($openra) {
            break
        }

        if ($launcher.HasExited) {
            throw "test-game.cmd exited before OpenRA opened."
        }
    }

    if (-not $openra) {
        throw "Timed out waiting for OpenRA window."
    }

    Start-Sleep -Seconds $StartupDelaySec

    powershell -ExecutionPolicy Bypass -File $capture -ModId ca -Destination $Destination -TimeoutSec $CaptureTimeoutSec
    Write-Output $Destination
}
finally {
    [void][PowerState]::SetThreadExecutionState($ES_CONTINUOUS)
    if (-not $KeepOpen) {
        Get-Process OpenRA -ErrorAction SilentlyContinue | Stop-Process -Force
        if (-not $launcher.HasExited) {
            Stop-Process -Id $launcher.Id -Force -ErrorAction SilentlyContinue
        }
    }
}
