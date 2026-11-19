param(
    [Parameter(Mandatory = $true)][string]$EngineDirectory,
    [Parameter(Mandatory = $true)][string]$FixtureMap,
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [int]$Port = 57349,
    [int]$TimeoutSec = 300,
    [switch]$RestrictedMapPool,
    [switch]$QueryMapRepository
)
# Local-only smoke test: remote admin UI, rejected non-admin request, two isolated
# clients reproducing a server-generated map, and a third client joining afterward.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$gameRoot = Split-Path -Parent $PSScriptRoot
$engine = [IO.Path]::GetFullPath($EngineDirectory)
$output = [IO.Path]::GetFullPath($OutputDirectory)
$probeRoot = Join-Path ([IO.Path]::GetTempPath()) 'ymca-dedicated-map-probes'
if (-not $output.StartsWith($probeRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Output must be beneath TEMP/ymca-dedicated-map-probes.' }
if (Test-Path $output) { throw 'Use a fresh output directory.' }
if ([Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners().Port -contains $Port) { throw 'Port already in use.' }
$supportSource = Join-Path $env:APPDATA 'OpenRA'
if (-not (Test-Path (Join-Path $supportSource 'Content'))) { $supportSource = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'OpenRA' }
$package = Join-Path $gameRoot ('mods/ca/maps/dedicated-generator-test-' + [Guid]::NewGuid().ToString('N') + '.oramap')
$oldSearch = $env:MOD_SEARCH_PATHS
$oldEngine = $env:ENGINE_DIR
$oldProbe = $env:YMCA_DEDICATED_MAP_PROBE
$oldRole = $env:YMCA_DEDICATED_MAP_PROBE_ROLE
$processes = [Collections.Generic.List[Diagnostics.Process]]::new()
try {
    New-Item -ItemType Directory -Path $output | Out-Null
    Copy-Item -LiteralPath $FixtureMap -Destination $package
    $env:MOD_SEARCH_PATHS = "$(Join-Path $gameRoot 'mods'),$(Join-Path $engine 'mods')"
    $env:ENGINE_DIR = $engine
    $uid = (& dotnet (Join-Path $engine 'bin/OpenRA.Utility.dll') ca --map-hash $package | Select-Object -Last 1).Trim()
    if ($LASTEXITCODE -ne 0 -or $uid -notmatch '^[0-9a-fA-F]{40}$') { throw "Invalid map UID: $uid" }
    function Start-TestProcess([string]$role) {
        $support = Join-Path $output $role
        New-Item -ItemType Directory -Path $support | Out-Null
        New-Item -ItemType Junction -Path (Join-Path $support 'Content') -Target (Join-Path $supportSource 'Content') | Out-Null
        Copy-Item (Join-Path $supportSource 'settings.yaml') (Join-Path $support 'settings.yaml')
        $arguments = @('Game.Mod=ca', "Engine.EngineDir=$engine", "Engine.ModSearchPaths=$env:MOD_SEARCH_PATHS", "Engine.SupportDir=$support", 'Game.AllowDownloading=false', 'Debug.EnableSimulationPerfLogging=false')
        if ($role -eq 'server') {
            $exe = Join-Path $engine 'bin/OpenRA.Server.exe'
            $env:YMCA_DEDICATED_MAP_PROBE = $null
            $env:YMCA_DEDICATED_MAP_PROBE_ROLE = $null
            $arguments += @('Server.Name=LocalGeneratorTest', "Server.ListenPort=$Port", 'Server.AdvertiseOnline=false', 'Server.EnableNat=false', "Server.QueryMapRepository=$($QueryMapRepository.IsPresent)", "Server.Map=$uid")
            if ($RestrictedMapPool) { $arguments += "Server.MapPool=$uid" }
        } else {
            $exe = Join-Path $engine 'bin/OpenRA.exe'
            $env:YMCA_DEDICATED_MAP_PROBE = $support
            $env:YMCA_DEDICATED_MAP_PROBE_ROLE = $role
            $arguments += @("Launch.Connect=127.0.0.1:$Port", "Player.Name=generator-$role", 'Graphics.Mode=Windowed', 'Graphics.WindowedSize=1100,800', 'Sound.SoundVolume=0', 'Sound.MusicVolume=0')
        }
        $quoted = $arguments | ForEach-Object { '"' + $_ + '"' }
        $p = Start-Process -FilePath $exe -ArgumentList $quoted -WorkingDirectory $engine -PassThru -RedirectStandardOutput (Join-Path $output "$role.stdout.log") -RedirectStandardError (Join-Path $output "$role.stderr.log")
        $processes.Add($p)
    }
    function Wait-For([scriptblock]$condition, [string]$description) {
        $deadline = (Get-Date).AddSeconds($TimeoutSec)
        while (-not (& $condition)) {
            foreach ($p in $processes) { $p.Refresh(); if ($p.HasExited) { throw "Test process $($p.Id) exited; inspect logs in $output" } }
            if ((Get-Date) -gt $deadline) { throw "Timed out: $description" }
            Start-Sleep -Seconds 1
        }
    }
    function Has-Result([string]$role, [string]$pattern) {
        $file = Join-Path $output "$role/results.txt"
        return (Test-Path $file) -and (Select-String -Path $file -Pattern $pattern -Quiet)
    }
    Start-TestProcess 'server'
    Wait-For { [Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners().Port -contains $Port } 'server listening'
    Start-TestProcess 'host'
    Wait-For { Has-Result 'host' '^START host' } 'admin joined'
    Start-TestProcess 'guest'
    Wait-For { (Has-Result 'host' '^MAP_READY ') -and (Has-Result 'guest' '^MAP_READY ') } 'two clients generated the map'
    # Retire only our verified guest before starting a fresh late joiner. Two
    # concurrent renderers are sufficient and avoid exhausting GPU texture memory.
    $departing = $processes[2]
    Stop-Process -Id $departing.Id -Force -ErrorAction SilentlyContinue
    [void]$processes.Remove($departing)
    Start-TestProcess 'late'
    Wait-For { Has-Result 'late' '^MAP_READY ' } 'late joiner generated the map'
    $uids = @('host', 'guest', 'late') | ForEach-Object { ((Select-String -Path (Join-Path $output "$_/results.txt") -Pattern '^MAP_READY ').Line -split ' ')[1] }
    $existing = $env:YMCA_DEDICATED_MAP_PROBE_EXISTING -eq '1'
    if (@($uids | Select-Object -Unique).Count -ne 1 -or ($existing -and $uids[0] -ne $uid) -or (-not $existing -and $uids[0] -eq $uid)) { throw 'Clients did not receive the expected selected map.' }
    if (-not (Has-Result 'guest' '^PASS server rejected non-admin')) { throw 'Missing permissions check.' }
    $result = "PASS: waiting lobby/start guard, admin map selection (existing=$existing), non-admin rejection, identical UID on two clients plus late joiner: $($uids[0])"
    Set-Content -Path (Join-Path $output 'results.txt') -Value $result
    Write-Output $result
} finally {
    foreach ($p in $processes) { $p.Refresh(); if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue } }
    if (Test-Path $package) { Remove-Item -LiteralPath $package }
    $env:MOD_SEARCH_PATHS = $oldSearch
    $env:ENGINE_DIR = $oldEngine
    $env:YMCA_DEDICATED_MAP_PROBE = $oldProbe
    $env:YMCA_DEDICATED_MAP_PROBE_ROLE = $oldRole
}
