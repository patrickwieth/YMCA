param(
	[string]$Source = "$env:APPDATA/../Roaming/OpenRA/Content/ca/firestorm",
	[string]$Output = "packaging/fs-music.zip"
)

$ErrorActionPreference = "Stop"

$files = @(
	"dmachine.aud",
	"elusive.aud",
	"fsmap.aud",
	"fsmenu.aud",
	"hacker.aud",
	"infiltra.aud",
	"kmachine.aud",
	"linkup.aud",
	"rainnite.aud",
	"slavesys.aud"
)

$missing = @()
$resolved = @()
foreach ($name in $files)
{
	$path = Join-Path $Source $name
	$resolvedPath = Resolve-Path -LiteralPath $path -ErrorAction SilentlyContinue
	if (-not $resolvedPath)
	{
		$missing += $path
	}
	else
	{
		$resolved += [PSCustomObject]@{ Name = $name; Path = $resolvedPath }
	}
}

if ($missing.Count -gt 0)
{
	throw "Missing Firestorm tracks:`n$($missing -join "`n")"
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
		$target = Join-Path $tempDir "firestorm"
		if (-not (Test-Path $target)) { New-Item -ItemType Directory -Path $target | Out-Null }
		Copy-Item $entry.Path (Join-Path $target $entry.Name)
	}
	Compress-Archive -Path (Join-Path $tempDir "firestorm") -DestinationPath $destination -Force
}
finally
{
	Remove-Item $tempDir -Recurse -Force
}

$hash = Get-FileHash -Algorithm SHA1 $destination
Write-Host "Created $destination"
Write-Host "SHA1: $($hash.Hash)"
