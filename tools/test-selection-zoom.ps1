param([Parameter(Mandatory=$true)][string]$EngineDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$engine=[IO.Path]::GetFullPath($EngineDirectory)
$output=[IO.Path]::GetFullPath($OutputDirectory)
if((Test-Path $output) -or (Test-Path "$output.oramap")){throw 'Use a fresh isolated output path.'}
$oldEngine=$env:ENGINE_DIR;$oldSearch=$env:MOD_SEARCH_PATHS;$oldProbe=$env:YMCA_SELECTION_ZOOM_PROBE
Push-Location $root
try {
 $env:ENGINE_DIR=$engine;$env:MOD_SEARCH_PATHS="$root/mods,$engine/mods";$env:YMCA_SELECTION_ZOOM_PROBE='1'
 & dotnet "$engine/bin/OpenRA.Utility.dll" ca --check-selection-zoom
 if($LASTEXITCODE -ne 0){throw 'Settings regression checks failed.'}
 & dotnet "$engine/bin/OpenRA.Utility.dll" ca --create-selection-zoom-probe "$output.oramap" "$output/results.txt"
 if($LASTEXITCODE -ne 0){throw 'Fixture generation failed.'}
 & "$PSScriptRoot/test-refinery-docking.ps1" -EngineDirectory $engine -MapPackage "$output.oramap" -OutputDirectory $output
} finally {
 Pop-Location
 $env:ENGINE_DIR=$oldEngine;$env:MOD_SEARCH_PATHS=$oldSearch;$env:YMCA_SELECTION_ZOOM_PROBE=$oldProbe
}
