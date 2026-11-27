param([Parameter(Mandatory=$true)][string]$EngineDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory,[string]$Mode='fixed')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$engine=[IO.Path]::GetFullPath($EngineDirectory)
$output=[IO.Path]::GetFullPath($OutputDirectory)
if((Test-Path $output) -or (Test-Path "$output.oramap")){throw 'Use a fresh isolated output directory and map path.'}
$oldEngine=$env:ENGINE_DIR;$oldSearch=$env:MOD_SEARCH_PATHS;$oldProbe=$env:YMCA_CLIFF_FIRE_PROBE
Push-Location $root
try {
 $env:ENGINE_DIR=$engine;$env:MOD_SEARCH_PATHS="$root/mods,$engine/mods";$env:YMCA_CLIFF_FIRE_PROBE='1'
 & dotnet "$engine/bin/OpenRA.Utility.dll" ca --create-cliff-fire-probe "$output.oramap" "$output/results.txt" $Mode
 if($LASTEXITCODE -ne 0){throw 'Fixture generation/validation failed.'}
 & "$PSScriptRoot/test-refinery-docking.ps1" -EngineDirectory $engine -MapPackage "$output.oramap" -OutputDirectory $output
} finally {
 Pop-Location
 $env:ENGINE_DIR=$oldEngine;$env:MOD_SEARCH_PATHS=$oldSearch;$env:YMCA_CLIFF_FIRE_PROBE=$oldProbe
}
