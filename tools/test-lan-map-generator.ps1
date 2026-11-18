param(
    [Parameter(Mandatory=$true)][string]$EngineDirectory,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [int]$TimeoutSec=300
)
$ErrorActionPreference='Stop'
$gameRoot=Split-Path -Parent $PSScriptRoot
$engine=[IO.Path]::GetFullPath($EngineDirectory)
$output=[IO.Path]::GetFullPath($OutputDirectory)
$allowed=(Join-Path ([IO.Path]::GetTempPath()) 'ymca-lan-map-probes')+[IO.Path]::DirectorySeparatorChar
if(-not $output.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Use the dedicated LAN probe temp directory.'}
if(Test-Path $output){throw 'Use a fresh output directory.'}
if([Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners().Port -contains 57351){throw 'Probe port is busy.'}
$supportSource=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'OpenRA'
$oldProbe=$env:YMCA_LAN_MAP_PROBE
$oldRole=$env:YMCA_LAN_MAP_PROBE_ROLE
$processes=[Collections.Generic.List[Diagnostics.Process]]::new()
try {
    New-Item -ItemType Directory -Path $output | Out-Null
    function Start-Client([string]$role){
        $support=Join-Path $output $role
        New-Item -ItemType Directory -Path $support | Out-Null
        New-Item -ItemType Junction -Path "$support/Content" -Target "$supportSource/Content" | Out-Null
        Copy-Item "$supportSource/settings.yaml" "$support/settings.yaml"
        $env:YMCA_LAN_MAP_PROBE=$support
        $env:YMCA_LAN_MAP_PROBE_ROLE=$role
        $arguments=@('Game.Mod=ca',"Engine.EngineDir=$engine","Engine.ModSearchPaths=$gameRoot/mods,$engine/mods","Engine.SupportDir=$support",'Game.AllowDownloading=false','Game.FetchNews=false','Server.EnableNat=false','Server.AdvertiseOnline=false',"Player.Name=LAN-$role",'Graphics.Mode=Windowed','Graphics.WindowedSize=1100,800','Sound.SoundVolume=0','Sound.MusicVolume=0')
        if($role -ne 'host'){$arguments+='Launch.Connect=127.0.0.1:57351'}
        $quoted=$arguments | ForEach-Object {'"'+$_+'"'}
        $p=Start-Process "$engine/bin/OpenRA.exe" -WorkingDirectory $engine -ArgumentList $quoted -PassThru -RedirectStandardOutput "$output/$role.stdout.log" -RedirectStandardError "$output/$role.stderr.log"
        $processes.Add($p)
    }
    function Has-Result([string]$role,[string]$pattern){
        $file="$output/$role/results.txt"
        return (Test-Path $file) -and (Select-String $file -Pattern $pattern -Quiet)
    }
    function Wait-For([scriptblock]$condition,[string]$description){
        $deadline=(Get-Date).AddSeconds($TimeoutSec)
        while(-not (& $condition)){
            foreach($p in $processes){$p.Refresh();if($p.HasExited){throw "Probe exited: $description; see $output"}}
            if((Get-Date) -gt $deadline){throw "Timed out: $description"}
            Start-Sleep -Seconds 1
        }
    }
    Start-Client 'host'
    Wait-For {Has-Result 'host' '^INITIAL_MAP_READY '} 'host created LAN game through multiplayer UI and generator'
    Start-Client 'guest'
    Wait-For {(Has-Result 'host' '^MAP_READY ') -and (Has-Result 'guest' '^MAP_READY ')} 'initial map and changed map synchronized'
    $departing=$processes[1]
    Stop-Process -Id $departing.Id -Force
    [void]$processes.Remove($departing)
    Start-Client 'late'
    Wait-For {Has-Result 'late' '^MAP_READY '} 'late joiner synchronized'
    $initial=@('host','guest') | ForEach-Object {((Select-String "$output/$_/results.txt" -Pattern '^INITIAL_MAP_READY ').Line -split ' ')[1]}
    $final=@('host','guest','late') | ForEach-Object {((Select-String "$output/$_/results.txt" -Pattern '^MAP_READY ').Line -split ' ')[1]}
    if(@($initial | Select-Object -Unique).Count -ne 1 -or @($final | Select-Object -Unique).Count -ne 1 -or $initial[0] -eq $final[0]){throw 'Map UID verification failed.'}
    Wait-For {(Has-Result 'host' '^MATCH_STARTED ') -and (Has-Result 'late' '^MATCH_STARTED ')} 'both clients started and simulated the generated map'
    if($env:YMCA_LAN_MAP_PROBE_SAVE_REPLAY -eq '1'){
        Wait-For {(Has-Result 'host' '^REPLAY_SAVED') -and (Has-Result 'late' '^REPLAY_SAVED')} 'recordings finalized normally'
    }
    $result="PASS: real LAN create/generate UI, initial guest sync, denied guest generation, host Change Map, late join and actual match start: $($initial[0]) -> $($final[0])"
    Set-Content "$output/results.txt" $result
    Write-Output $result
} finally {
    foreach($p in $processes){$p.Refresh();if(-not $p.HasExited){Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue}}
    $env:YMCA_LAN_MAP_PROBE=$oldProbe
    $env:YMCA_LAN_MAP_PROBE_ROLE=$oldRole
}
