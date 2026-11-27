param([Parameter(Mandatory=$true)][string]$EngineDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory,[switch]$Baseline,[switch]$Slopes,[switch]$Buildings,[switch]$Gdi,[switch]$China,[switch]$Voxel)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$engine=[IO.Path]::GetFullPath($EngineDirectory)
$output=[IO.Path]::GetFullPath($OutputDirectory)
if((Test-Path $output) -or (Test-Path "$output.oramap")){throw 'Use a fresh isolated output directory and map path.'}
$oldEngine=$env:ENGINE_DIR;$oldSearch=$env:MOD_SEARCH_PATHS;$oldProbe=$env:YMCA_ROCK_OCCLUSION_PROBE
Push-Location $root
try {
 $env:ENGINE_DIR=$engine;$env:MOD_SEARCH_PATHS="$root/mods,$engine/mods";$env:YMCA_ROCK_OCCLUSION_PROBE='1'
 $mode=if($Buildings){$name=if($Voxel){'buildings-voxel'}elseif($China){'buildings-china'}elseif($Gdi){'buildings-gdi'}else{'buildings'};if($Baseline){"$name-baseline"}else{$name}}elseif($Slopes){if($Baseline){'slope-baseline'}else{'slopes'}}elseif($Baseline){'baseline'}else{'fixed'}
 & dotnet "$engine/bin/OpenRA.Utility.dll" ca --create-rock-occlusion-probe "$output.oramap" "$output/results.txt" $mode
 if($LASTEXITCODE -ne 0){throw 'Fixture generation/validation failed.'}
 & "$PSScriptRoot/test-refinery-docking.ps1" -EngineDirectory $engine -MapPackage "$output.oramap" -OutputDirectory $output
} finally {
 Pop-Location
 $env:ENGINE_DIR=$oldEngine;$env:MOD_SEARCH_PATHS=$oldSearch;$env:YMCA_ROCK_OCCLUSION_PROBE=$oldProbe
}
