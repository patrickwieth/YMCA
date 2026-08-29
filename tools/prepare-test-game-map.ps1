param(
    [Parameter(Mandatory = $true)]
    [string]$SourceMap,

    [Parameter(Mandatory = $true)]
    [string]$DestinationMap
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

Copy-Item -LiteralPath $SourceMap -Destination $DestinationMap -Force

$archive = [System.IO.Compression.ZipFile]::Open(
    $DestinationMap,
    [System.IO.Compression.ZipArchiveMode]::Update)

try {
    $entry = $archive.GetEntry('map.yaml')
    if ($null -eq $entry) {
        throw "The test map does not contain map.yaml."
    }

    $reader = [System.IO.StreamReader]::new($entry.Open())
    try {
        $yaml = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }

    $needle = "`t`t-ConquestVictoryConditions:"
    $needleIndex = $yaml.IndexOf($needle, [System.StringComparison]::Ordinal)
    if ($needleIndex -lt 0 -or $yaml.IndexOf($needle, $needleIndex + $needle.Length, [System.StringComparison]::Ordinal) -ge 0) {
        throw "Expected exactly one Player/-ConquestVictoryConditions rule in the test map."
    }

    $newline = if ($yaml.Contains("`r`n")) { "`r`n" } else { "`n" }
    $developerMode = @(
        $needle
        "`t`tDeveloperMode:"
        "`t`t`tCheckboxEnabled: True"
        "`t`t`tCheckboxLocked: True"
        "`t`t`tFastBuild: True"
        "`t`t`tUnlimitedPower: True"
    ) -join $newline

    $yaml = $yaml.Remove($needleIndex, $needle.Length).Insert($needleIndex, $developerMode)

    $entry.Delete()
    $entry = $archive.CreateEntry('map.yaml', [System.IO.Compression.CompressionLevel]::Optimal)
    $writer = [System.IO.StreamWriter]::new($entry.Open(), [System.Text.UTF8Encoding]::new($false))
    try {
        $writer.Write($yaml)
    }
    finally {
        $writer.Dispose()
    }
}
finally {
    $archive.Dispose()
}
