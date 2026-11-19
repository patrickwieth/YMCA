param([Parameter(Mandatory=$true)][string]$EngineDirectory,[Parameter(Mandatory=$true)][string]$MapPackage,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$gameRoot=Split-Path -Parent $PSScriptRoot
$engine=[IO.Path]::GetFullPath($EngineDirectory)
$output=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path $output){throw 'Use a fresh isolated output directory.'}
$source=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'OpenRA'
New-Item -ItemType Directory $output | Out-Null
New-Item -ItemType Junction "$output/Content" -Target "$source/Content" | Out-Null
Copy-Item "$source/settings.yaml" "$output/settings.yaml"
New-Item -ItemType Directory -Force "$output/maps/ca/generated" | Out-Null
Copy-Item $MapPackage "$output/maps/ca/generated/refinery-probe.oramap"
$args=@('Game.Mod=ca',"Engine.EngineDir=$engine","Engine.ModSearchPaths=$gameRoot/mods,$engine/mods","Engine.SupportDir=$output",'Launch.Map=refinery-probe.oramap','PlayerType.Multi0=Human','PlayerFaction.Multi0=russia','SpawnPoint.Multi0=1','Game.FetchNews=false','Game.AllowDownloading=false','Graphics.Mode=Windowed','Graphics.WindowedSize=1700,1000','Sound.SoundVolume=0','Sound.MusicVolume=0')
$p=$null
$oldProbe=$env:YMCA_REFINERY_SMUDGE_PROBE
try {
 $env:YMCA_REFINERY_SMUDGE_PROBE='1'
 $p=Start-Process "$engine/bin/OpenRA.exe" -WorkingDirectory $gameRoot -ArgumentList ($args | ForEach-Object {'"'+$_+'"'}) -PassThru -RedirectStandardOutput "$output/stdout.log" -RedirectStandardError "$output/stderr.log"
 $deadline=(Get-Date).AddSeconds(280)
 while((Get-Date) -lt $deadline){
  $p.Refresh();if($p.HasExited){throw "Probe exited. See $output"}
  if((Test-Path "$output/results.txt") -and (Select-String "$output/results.txt" -Pattern '^PASS:' -Quiet)){Get-Content "$output/results.txt" | Select-Object -Last 1;return}
  Start-Sleep -Seconds 2
 }
 throw "Timed out. See $output"
} finally {
 if($p){$p.Refresh();if(!$p.HasExited){Stop-Process -Id $p.Id}}
 $env:YMCA_REFINERY_SMUDGE_PROBE=$oldProbe
}
