param(
	[string]$Source = "$env:APPDATA/../Roaming/OpenRA/Content/ca/ra/scores.mix",
	[string]$Output = "packaging/ra-music.zip"
)

$ErrorActionPreference = "Stop"

$sourcePath = Resolve-Path -LiteralPath $Source -ErrorAction SilentlyContinue
if (-not $sourcePath)
{
	throw "Could not find RA music at '$Source'. Install the Red Alert music (scores.mix) first, then rerun this script."
}

$repoRoot = Resolve-Path (Join-Path (Split-Path -Parent $PSCommandPath) "..")
$destination = Join-Path $repoRoot $Output
$destinationDir = Split-Path -Parent $destination
if (-not (Test-Path $destinationDir))
{
	New-Item -ItemType Directory -Path $destinationDir | Out-Null
}

if (Test-Path $destination)
{
	Remove-Item $destination
}

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $tempDir | Out-Null

try
{
	Copy-Item $sourcePath (Join-Path $tempDir "scores.mix")
	Compress-Archive -Path (Join-Path $tempDir "scores.mix") -DestinationPath $destination -Force
}
finally
{
	Remove-Item $tempDir -Recurse -Force
}

$hash = Get-FileHash -Algorithm SHA1 $destination
Write-Host "Created $destination"
Write-Host "SHA1: $($hash.Hash)"
