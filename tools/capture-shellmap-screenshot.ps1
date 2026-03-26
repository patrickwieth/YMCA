param(
    [string]$Destination = "",
    [int]$StartupDelaySec = 12,
    [int]$CaptureTimeoutSec = 8,
    [switch]$KeepOpen
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

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
    if (-not $KeepOpen) {
        Get-Process OpenRA -ErrorAction SilentlyContinue | Stop-Process -Force
        if (-not $launcher.HasExited) {
            Stop-Process -Id $launcher.Id -Force -ErrorAction SilentlyContinue
        }
    }
}
