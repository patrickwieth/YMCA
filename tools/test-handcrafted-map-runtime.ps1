param(
    [Parameter(Mandatory=$true)][string]$MapPackage,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [string]$EngineDirectory,
    [double]$UIScale = 0,
    [int]$WindowWidth = 1600,
    [int]$WindowHeight = 1000,
    [string]$WaitForResultFile = '',
    [switch]$WaitForMovement
)
# Isolated native acceptance run. Never stops or changes an existing user game.
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!$EngineDirectory){$EngineDirectory=Join-Path $root 'engine'}
$engine=[IO.Path]::GetFullPath($EngineDirectory)
$out=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path $out){throw 'A fresh output directory is required.'}
$source=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'OpenRA'
if(!(Test-Path "$source/Content")){throw 'Install game content before running the native acceptance test.'}
New-Item -ItemType Directory $out | Out-Null
New-Item -ItemType Junction "$out/Content" -Target "$source/Content" | Out-Null
if(Test-Path "$source/settings.yaml"){Copy-Item "$source/settings.yaml" "$out/settings.yaml"}
New-Item -ItemType Directory -Force "$out/maps/ca/generated" | Out-Null
Copy-Item $MapPackage "$out/maps/ca/generated/handcrafted-review.oramap"
$args=@('Game.Mod=ca',"Engine.EngineDir=$engine","Engine.ModSearchPaths=$root/mods,$engine/mods","Engine.SupportDir=$out",'Launch.Map=handcrafted-review.oramap','PlayerType.Multi0=Human','PlayerFaction.Multi0=russia','SpawnPoint.Multi0=1','Game.FetchNews=false','Game.AllowDownloading=false','Graphics.Mode=Windowed',"Graphics.WindowedSize=$WindowWidth,$WindowHeight",'Graphics.CapFramerate=true','Graphics.MaxFramerate=30','Sound.SoundVolume=0','Sound.MusicVolume=0')
if($UIScale -gt 0){$args += "Graphics.UIScale=$($UIScale.ToString([Globalization.CultureInfo]::InvariantCulture))"}
for($i=1;$i-lt16;$i++){$args+="PlayerType.Multi$i=Closed"}
$p=$null
$resultName=if($WaitForMovement){'handcrafted-runtime.json'}else{$WaitForResultFile}
try{
    $p=Start-Process "$engine/bin/OpenRA.exe" -WorkingDirectory $root -ArgumentList ($args | ForEach-Object {'"'+$_+'"'}) -PassThru -RedirectStandardOutput "$out/stdout.log" -RedirectStandardError "$out/stderr.log"
    $deadline=(Get-Date).AddSeconds(360)
    while((Get-Date)-lt$deadline){
        Start-Sleep -Seconds 2
        $p.Refresh()
        if(Test-Path "$out/Logs/server.log"){
            if(Select-String -Path "$out/Logs/server.log" -Pattern 'Failed to load rules for' -Quiet){throw "Map rules failed: inspect $out/Logs/server.log"}
        }
        $shots=Get-ChildItem "$out/Screenshots" -Recurse -Filter '*.png' -ErrorAction SilentlyContinue
        if($shots -and (!$resultName -or (Test-Path "$out/$resultName"))){
            Write-Output $shots[-1].FullName
            if($resultName){
                $result=Get-Content "$out/$resultName" -Raw | ConvertFrom-Json
                if(!$result.Passed){throw 'Native acceptance failed.'}
                Write-Output "$out/$resultName"
            }
            return
        }
        if($p.HasExited){throw "Probe exited: inspect $out/Logs and stderr.log"}
    }
    throw "Timed out: inspect $out"
}finally{
    if($p){$p.Refresh();if(!$p.HasExited){Stop-Process -Id $p.Id}}
}
