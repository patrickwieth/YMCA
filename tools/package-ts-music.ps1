param(
	[string]$Source = "$env:APPDATA/../Roaming/OpenRA/Content/ca/ts",
	[string]$Output = "packaging/ts-music.zip"
)

$ErrorActionPreference = "Stop"

$scores = @("scores.mix", "scores01.mix")
$missing = @()
$resolved = @()
foreach ($file in $scores)
{
	$path = Join-Path $Source $file
	$resolvedPath = Resolve-Path -LiteralPath $path -ErrorAction SilentlyContinue
	if (-not $resolvedPath)
	{
		$missing += $path
	}
	else
	{
		$resolved += [PSCustomObject]@{ Name = $file; Path = $resolvedPath }
	}
}

if ($missing.Count -gt 0)
{
	throw "Missing Tiberian Sun music files:`n$($missing -join "`n")"
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
	foreach ($entry in $resolved)
	{
		Copy-Item $entry.Path (Join-Path $tempDir $entry.Name)
	}
	Compress-Archive -Path (Join-Path $tempDir "*") -DestinationPath $destination -Force
}
finally
{
	Remove-Item $tempDir -Recurse -Force
}

$hash = Get-FileHash -Algorithm SHA1 $destination
Write-Host "Created $destination"
Write-Host "SHA1: $($hash.Hash)"
