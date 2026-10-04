param(
    [Parameter(Mandatory = $true)]
    [string]$MapPackage,
    [Parameter(Mandatory = $true)]
    [string]$Destination,
    [int]$StartupDelaySec = 100,
    [int]$CaptureTimeoutSec = 8,
    [switch]$Observer,
    [switch]$AutomaticFramebuffer,
    [switch]$SimulationPerf,
    [switch]$KeepOpen
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$gameRoot = Split-Path -Parent $scriptDir
$engineRoot = Join-Path $gameRoot "engine"
$openRa = Join-Path $engineRoot "bin\OpenRA.exe"
$capture = Join-Path $scriptDir "capture-openra-screenshot.ps1"
$mapPath = Join-Path $gameRoot "mods\ca\maps\$MapPackage"

if (-not (Test-Path $openRa)) { throw "Missing OpenRA executable: $openRa" }
if (-not (Test-Path $capture)) { throw "Missing screenshot helper: $capture" }
if (-not (Test-Path $mapPath)) { throw "Missing map package: $mapPath" }

Add-Type -AssemblyName System.Windows.Forms
[System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point(0, 0)

$modSearchPaths = "$(Join-Path $gameRoot 'mods'),$(Join-Path $scriptDir 'mods'),./mods"
$arguments = @(
    "Game.Mod=ca",
    "Game.ViewportEdgeScroll=false",
    "Debug.EnableSimulationPerfLogging=$($SimulationPerf.IsPresent)",
    "Debug.LongTickThresholdMs=$(if ($SimulationPerf) { 10 } else { 1 })",
    "Launch.Map=$MapPackage",
    "Engine.EngineDir=..",
    "Engine.ModSearchPaths=$modSearchPaths",
    "PlayerFaction.Multi0=blackh",
    "PlayerType.Multi0=$(if ($Observer) { 'Observer' } else { 'Human' })",
    "SpawnPoint.Multi0=1"
)
for ($slot = 1; $slot -lt 16; $slot++) {
    $arguments += "PlayerType.Multi$slot=Closed"
}

$process = Start-Process -FilePath $openRa -ArgumentList $arguments -WorkingDirectory $engineRoot -PassThru
try {
    $deadline = (Get-Date).AddSeconds(90)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 500
        $process.Refresh()
        if ($process.HasExited) { throw "OpenRA exited before the screenshot could be captured." }
        if ($process.MainWindowHandle -ne 0) { break }
    }

    if ($process.MainWindowHandle -eq 0) { throw "Timed out waiting for the OpenRA window." }
    Start-Sleep -Seconds $StartupDelaySec
    $process.Refresh()
    if ($process.HasExited) { throw "OpenRA exited while loading the test map." }
    if ($AutomaticFramebuffer) {
        # Calibration maps request their own screenshot; no focus or synthetic input is needed.
        $deadline = (Get-Date).AddSeconds($CaptureTimeoutSec)
        $shot = $null
        while ((Get-Date) -lt $deadline -and $null -eq $shot) {
            $process.Refresh()
            if ($process.HasExited) { throw "OpenRA exited before the diagnostic framebuffer was produced; inspect exception logs." }
            $shot = Get-ChildItem "$env:APPDATA\OpenRA\Screenshots\ca" -Recurse -Filter *.png -ErrorAction SilentlyContinue |
                Where-Object { $_.LastWriteTime -ge $process.StartTime } |
                Sort-Object LastWriteTime -Descending | Select-Object -First 1
            if ($null -eq $shot) { Start-Sleep -Milliseconds 250 }
        }
        if ($null -eq $shot) { throw "The calibration map did not produce a new framebuffer screenshot." }
        $destinationPath = [IO.Path]::GetFullPath($Destination)
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destinationPath)) | Out-Null
        Copy-Item -LiteralPath $shot.FullName -Destination $destinationPath -Force
        Write-Output $destinationPath
    }
    else {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $capture -ModId ca -Destination $Destination -TimeoutSec $CaptureTimeoutSec
        if ($LASTEXITCODE -ne 0) { throw "Framebuffer capture failed (exit $LASTEXITCODE)." }
    }
}
finally {
    if (-not $KeepOpen -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
}
