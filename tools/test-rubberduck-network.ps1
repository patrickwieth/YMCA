param(
    [Parameter(Mandatory = $true)][string]$FixtureDirectory,
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [int]$Port = 57341,
    [int]$TimeoutSec = 420
)

# Requires --rubberduck-network-test output. Uses real isolated clients and server;
# never changes the user's settings, content, multiplayer lobby or saved games.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$gameRoot = Split-Path -Parent $PSScriptRoot
$engine = Join-Path $gameRoot 'engine'
$fixture = [IO.Path]::GetFullPath($FixtureDirectory)
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path $output) { throw 'Use a fresh output directory to exclude stale logs and screenshots.' }
if ([Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners().Port -contains $Port) { throw 'Test port is already in use.' }
$uid = (Get-Content (Join-Path $fixture 'map-uid.txt') -Raw).Trim()
if ($uid -notmatch '^[0-9a-fA-F]{40}$') { throw 'Invalid fixture map UID.' }
$package = Join-Path $gameRoot ('mods\ca\maps\network-diagnostic-' + [Guid]::NewGuid().ToString('N') + '.oramap')
$oldSearch = $env:MOD_SEARCH_PATHS
$processes = @()
try {
    New-Item -ItemType Directory -Path $output | Out-Null
    Copy-Item (Join-Path $fixture 'rubberduck-network-test.oramap') $package
    $env:MOD_SEARCH_PATHS = "$(Join-Path $gameRoot 'mods'),$(Join-Path $engine 'mods')"
    foreach ($name in @('server', 'client-a', 'client-b')) {
        $support = Join-Path $output $name
        New-Item -ItemType Directory -Path $support | Out-Null
        New-Item -ItemType Junction -Path (Join-Path $support 'Content') -Target "$env:APPDATA\OpenRA\Content" | Out-Null
        Copy-Item "$env:APPDATA\OpenRA\settings.yaml" (Join-Path $support 'settings.yaml')
        $arguments = @('Game.Mod=ca', 'Engine.EngineDir=..', "Engine.ModSearchPaths=$env:MOD_SEARCH_PATHS", "Engine.SupportDir=$support", 'Debug.EnableSimulationPerfLogging=false')
        if ($name -eq 'server') {
            $exe = Join-Path $engine 'bin\OpenRA.Server.exe'
            $arguments += @('Server.Name=RubberduckLoopbackTest', "Server.ListenPort=$Port", 'Server.AdvertiseOnline=false', 'Server.EnableNat=false', 'Server.QueryMapRepository=false', "Server.Map=$uid")
        } else {
            $exe = Join-Path $engine 'bin\OpenRA.exe'
            $arguments += @("Launch.Connect=127.0.0.1:$Port", "Player.Name=$name", 'Graphics.Mode=Windowed', 'Graphics.WindowedSize=1024,768', 'Sound.SoundVolume=0', 'Sound.MusicVolume=0')
        }
        $quoted = $arguments | ForEach-Object { '"' + $_ + '"' }
        $processes += Start-Process -FilePath $exe -ArgumentList $quoted -WorkingDirectory $engine -PassThru -RedirectStandardOutput (Join-Path $output "$name.stdout.log") -RedirectStandardError (Join-Path $output "$name.stderr.log")
        if ($name -eq 'server') {
            $listenDeadline = (Get-Date).AddSeconds(120)
            while (-not ([Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners().Port -contains $Port)) {
                $processes[-1].Refresh()
                if ($processes[-1].HasExited) { throw 'Dedicated server exited during startup.' }
                if ((Get-Date) -gt $listenDeadline) { throw 'Dedicated server did not begin listening.' }
                Start-Sleep -Milliseconds 500
            }
        }
    }
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $complete = 0
    while ((Get-Date) -lt $deadline) {
        foreach ($p in $processes) { $p.Refresh(); if ($p.HasExited) { throw "Process $($p.Id) exited prematurely." } }
        $complete = 0
        foreach ($name in @('client-a', 'client-b')) {
            $log = Join-Path $output "$name\Logs\terrain-network.log"
            if ((Test-Path $log) -and (Select-String -Path $log -Pattern '^NETWORK COMPLETE' -Quiet)) { $complete++ }
        }
        if ($complete -eq 2) { break }
        Start-Sleep -Seconds 2
    }
    if ($complete -ne 2) { throw 'Two-client simulation did not complete.' }
    $a = @(Get-Content (Join-Path $output 'client-a\Logs\terrain-network.log') | Where-Object { $_ -like 'NETWORK HASH*' })
    $b = @(Get-Content (Join-Path $output 'client-b\Logs\terrain-network.log') | Where-Object { $_ -like 'NETWORK HASH*' })
    if ($a.Count -ne 30 -or ($a -join "`n") -cne ($b -join "`n")) { throw 'Client sync hashes or positions differ.' }
    for ($i = 0; $i -lt 2; $i++) {
        $positions = @($a | ForEach-Object { (($_ -split 'cells=')[1] -split ';')[$i] } | Sort-Object -Unique)
        if ($positions.Count -lt 2) { throw "Tank $i did not move." }
    }
    Start-Sleep -Seconds 3
    $result = 'PASS: two live clients; 3000 ticks; 30 identical hash/position snapshots; both tanks moved.'
    Set-Content -Path (Join-Path $output 'results.txt') -Value $result -Encoding UTF8
    Write-Output $result
} finally {
    foreach ($p in $processes) { $p.Refresh(); if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue } }
    if (Test-Path $package) { Remove-Item -LiteralPath $package }
    $env:MOD_SEARCH_PATHS = $oldSearch
}
