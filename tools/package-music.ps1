param(
	[string]$Source,
	[string]$Output = "packaging/ymca-music.zip"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $PSCommandPath
$repoRoot = Resolve-Path (Join-Path $scriptRoot "..")
$repoMusicDir = Join-Path $repoRoot "mods/ca/bits/music"
$documentsDir = [Environment]::GetFolderPath("MyDocuments")
$documentsMusicDir = Resolve-Path -LiteralPath (Join-Path $documentsDir "OpenRA/Content/ca/music") -ErrorAction SilentlyContinue
$supportMusicDir = Resolve-Path -LiteralPath "$env:APPDATA/../Roaming/OpenRA/Content/ca/music" -ErrorAction SilentlyContinue

if ([string]::IsNullOrWhiteSpace($Source)) {
	if (Test-Path $repoMusicDir) {
		$musicDir = $repoMusicDir
	} elseif ($documentsMusicDir) {
		$musicDir = $documentsMusicDir.Path
	} elseif ($supportMusicDir) {
		$musicDir = $supportMusicDir.Path
	} else {
		throw "Music directory not found. Looked for '$repoMusicDir', '$documentsDir\\OpenRA\\Content\\ca\\music', and '$env:APPDATA/../Roaming/OpenRA/Content/ca/music'."
	}
} else {
	$resolvedSource = Resolve-Path -LiteralPath $Source -ErrorAction SilentlyContinue
	if (-not $resolvedSource) {
		throw "Music directory not found: $Source"
	}
	$musicDir = $resolvedSource.Path
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
