param(
 [Parameter(Mandatory=$true)][string]$EngineDirectory,
 [Parameter(Mandatory=$true)][string]$OutputDirectory,
 [Parameter(Mandatory=$true)][string]$MapPackage,
 [Parameter(Mandatory=$true)][string]$MapUid,
 [string]$DownloadUrl='https://tournament.crowdcontrol.network/map/M0020/download'
)
$ErrorActionPreference='Stop'
$gameRoot=Split-Path -Parent $PSScriptRoot
$engine=[IO.Path]::GetFullPath($EngineDirectory)
$output=[IO.Path]::GetFullPath($OutputDirectory)
$allowed=(Join-Path ([IO.Path]::GetTempPath()) 'ymca-map-download-probes')+[IO.Path]::DirectorySeparatorChar
if(!$output.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase) -or (Test-Path $output)){throw 'Use a fresh dedicated probe directory.'}
$port=57361
if([Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners().Port -contains $port){throw 'Probe port busy.'}
$source=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'OpenRA'
$processes=[Collections.Generic.List[Diagnostics.Process]]::new()
$oldProbe=$env:YMCA_MAP_DOWNLOAD_PROBE;$oldRole=$env:YMCA_MAP_DOWNLOAD_PROBE_ROLE
$oldSearch=$env:MOD_SEARCH_PATHS;$oldEngine=$env:ENGINE_DIR;$oldUid=$env:YMCA_MAP_DOWNLOAD_PROBE_UID
try {
 $env:MOD_SEARCH_PATHS="$gameRoot/mods,$engine/mods";$env:ENGINE_DIR=$engine;$env:YMCA_MAP_DOWNLOAD_PROBE_UID=$MapUid
 New-Item -ItemType Directory $output | Out-Null
 function Start-Role([string]$role){
  $support=Join-Path $output $role
  New-Item -ItemType Directory $support | Out-Null
  New-Item -ItemType Junction "$support/Content" -Target "$source/Content" | Out-Null
  Copy-Item "$source/settings.yaml" "$support/settings.yaml"
  $args=@('Game.Mod=ca',"Engine.EngineDir=$engine","Engine.ModSearchPaths=$gameRoot/mods,$engine/mods","Engine.SupportDir=$support",'Game.FetchNews=false')
  $exe="$engine/bin/OpenRA.exe"
  if($role -eq 'server'){
   $exe="$engine/bin/OpenRA.Server.exe"
   New-Item -ItemType Directory -Force "$support/maps/ca/generated" | Out-Null
   Copy-Item $MapPackage "$support/maps/ca/generated/test.oramap"
   $hash=(Get-FileHash $MapPackage -Algorithm SHA256).Hash.ToLowerInvariant()
   $size=(Get-Item $MapPackage).Length
   [IO.File]::WriteAllText("$support/tournament-map-download.txt","1|$MapUid|$hash|$size|$DownloadUrl")
   $env:YMCA_MAP_DOWNLOAD_PROBE=$null;$env:YMCA_MAP_DOWNLOAD_PROBE_ROLE=$null
   $args+=@("Server.Map=$MapUid","Server.MapPool=$MapUid","Server.ListenPort=$port",'Server.Name=IsolatedMapDownloadTest','Server.AdvertiseOnline=false','Server.EnableNat=false','Server.QueryMapRepository=false','Server.Password=probe','Server.AllowSpectators=true','Server.SpectatorPassword=watch','Server.AllowedPlayerNames=DownloadA,DownloadB','Server.RequiredPlayerCount=2','Server.AutoStartDelaySeconds=1')
  }else{
   $env:YMCA_MAP_DOWNLOAD_PROBE=$support;$env:YMCA_MAP_DOWNLOAD_PROBE_ROLE=$role
   $name=if($role -eq 'host'){'DownloadA'}elseif($role -eq 'spectator'){'Watcher'}else{'DownloadB'}
   $password=if($role -eq 'spectator'){'watch'}else{'probe'}
   $args+=@("Launch.Connect=127.0.0.1:$port","Launch.Password=$password","Player.Name=$name","Launch.Spectator=$($role -eq 'spectator')",'Graphics.Mode=Windowed','Graphics.WindowedSize=1100,800','Sound.SoundVolume=0','Sound.MusicVolume=0',"Game.AllowDownloading=$($role -ne 'optin')")
  }
  $quoted=$args | ForEach-Object {'"'+$_+'"'}
  $p=Start-Process $exe -WorkingDirectory $engine -ArgumentList $quoted -PassThru -RedirectStandardOutput "$output/$role.stdout.log" -RedirectStandardError "$output/$role.stderr.log"
  $processes.Add($p)
 }
 function Has([string]$role,[string]$pattern){$path="$output/$role/results.txt";return (Test-Path $path) -and (Select-String $path -Pattern $pattern -Quiet)}
 function Wait([scriptblock]$condition,[string]$name){
  $deadline=(Get-Date).AddSeconds(200)
  while(!(& $condition)){
   foreach($p in $processes){$p.Refresh();if($p.HasExited){throw "Test process $($p.Id) exited with code $($p.ExitCode): $name. See $output"}}
   if((Get-Date) -gt $deadline){throw "Timeout: $name"};Start-Sleep -Seconds 1
  }
 }
 Start-Role server
 Wait { [Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners().Port -contains $port } 'local server startup'
 Start-Role host
 Wait {Has host '^MAP_READY'} 'host automatic HTTPS download'
 foreach($role in @('guest','spectator')){
  Start-Role $role
  Wait {Has $role '^MAP_READY'} "$role download"
  $p=$processes[$processes.Count-1];Stop-Process -Id $p.Id -Force;[void]$processes.Remove($p)
 }
 Start-Role optin
 Wait {(Has host '^MATCH_STARTED') -and (Has optin '^MATCH_STARTED')} 'explicit permission and actual match start'
 $result='PASS: initial players, late spectator, download preference/explicit action, exact SHA/UID, hot-load and match start without manual installation.'
 Set-Content "$output/results.txt" $result;Write-Output $result
}finally{
 foreach($p in $processes){$p.Refresh();if(!$p.HasExited){Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue}}
 $env:YMCA_MAP_DOWNLOAD_PROBE=$oldProbe;$env:YMCA_MAP_DOWNLOAD_PROBE_ROLE=$oldRole
 $env:MOD_SEARCH_PATHS=$oldSearch;$env:ENGINE_DIR=$oldEngine;$env:YMCA_MAP_DOWNLOAD_PROBE_UID=$oldUid
}
