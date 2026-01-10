param(
	[string]$Output = "packaging/ymca-music.zip"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $PSCommandPath
$repoRoot = Resolve-Path (Join-Path $scriptRoot "..")
$musicDir = Join-Path $repoRoot "mods/ca/bits/music"

if (-not (Test-Path $musicDir)) {
	throw "Music directory not found: $musicDir"
}

$destination = Join-Path $repoRoot $Output
$destinationDir = Split-Path -Parent $destination
if (-not (Test-Path $destinationDir)) {
	New-Item -ItemType Directory -Path $destinationDir | Out-Null
}

if (Test-Path $destination) {
	Remove-Item $destination
}

$musicFolderName = Split-Path $musicDir -Leaf
Push-Location (Split-Path $musicDir -Parent)
try {
	Compress-Archive -Path $musicFolderName -DestinationPath $destination -Force
}
finally {
	Pop-Location
}

$hash = Get-FileHash -Algorithm SHA1 $destination
Write-Host "Created $destination"
Write-Host "SHA1: $($hash.Hash)"
