param(
    [Parameter(Mandatory = $true)]
    [string]$SourceMap,

    [Parameter(Mandatory = $true)]
    [string]$DestinationMap
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

if ([System.IO.Path]::GetFullPath($SourceMap) -eq [System.IO.Path]::GetFullPath($DestinationMap)) {
    throw "Source and disposable destination map must be different files."
}
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

    # Only change the disposable test copy: show the current Allied Prism Tower
    # in the old Allied Turret slot, preserving actor IDs, ownership and placement.
    $actors = [regex]::Match($yaml, '(?ms)(^Actors:\r?\n)(.*?)(?=^[^\t\r\n#][^\r\n]*:|\z)')
    if (-not $actors.Success) {
        throw "The test map does not contain an Actors section."
    }
    $turretPattern = '(?m)^(\t[^\t\r\n:]+:[ \t]*)gun(?=\r?$)'
    $actorText = $actors.Groups[2].Value
    $turretCount = [regex]::Matches($actorText, $turretPattern).Count
    if ($turretCount -gt 0) {
        $actorText = [regex]::Replace($actorText, $turretPattern, '${1}pris')
        Write-Host "Test map: replaced $turretCount Allied Turret slot(s) with Prism Tower."
    }
    elseif (-not [regex]::IsMatch($actorText, '(?m)^\t[^\t\r\n:]+:[ \t]*pris\r?$')) {
        throw "No Allied Turret slot or existing Prism Tower found in the test map."
    }

    # The Soviet display block in this legacy map still uses Allied actor IDs.
    # Replace only these exact slots, never all instances of the shared names.
    $sovietSlots = @(
        @('Actor23', 'fact', 'fact.soviet', '116,-40'),
        @('Actor24', 'powr', 'powr.soviet', '122,-46'),
        @('Actor25', 'apwr', 'apwr.soviet', '128,-52'),
        @('Actor30', 'weap', 'weap.soviet', '132,-48'),
        @('Actor31', 'dome', 'dome.soviet', '138,-54'),
        @('Actor32', 'hpad', 'hpad.soviet', '144,-60'),
        @('Actor35', 'fix', 'fix.soviet', '136,-44')
    )
    foreach ($slot in $sovietSlots) {
        $pattern = '(?m)^(\t' + [regex]::Escape($slot[0]) + ':[ \t]*)(' +
            [regex]::Escape($slot[1]) + '|' + [regex]::Escape($slot[2]) +
            ')(\r?\n(?:\t\t[^\r\n]*(?:\r?\n|\z))*)'
        $slotMatches = [regex]::Matches($actorText, $pattern)
        if ($slotMatches.Count -ne 1 -or $slotMatches[0].Groups[3].Value -notmatch
            ('(?m)^\t\tLocation:[ \t]*' + [regex]::Escape($slot[3]) + '\r?$')) {
            throw "Soviet display slot $($slot[0]) has changed; refusing an ambiguous replacement."
        }
        $match = $slotMatches[0]
        $replacement = $match.Groups[1].Value + $slot[2] + $match.Groups[3].Value
        $actorText = $actorText.Remove($match.Index, $match.Length).Insert($match.Index, $replacement)
    }
    Write-Host "Test map: assigned 7 Soviet display slots to their dedicated Soviet actors."

    # Remove only resource-storage silos, never missile/nuclear silo actors.
    $siloPattern = '(?mi)^\t[^\t\r\n:]+:[ \t]*(?:silo|silo\.td|silo\.scrin|silo\.soviet)[ \t]*\r?\n(?:\t\t[^\r\n]*(?:\r?\n|\z))*'
    $siloCount = [regex]::Matches($actorText, $siloPattern).Count
    $actorText = [regex]::Replace($actorText, $siloPattern, '')
    $yaml = $yaml.Remove($actors.Index, $actors.Length).Insert($actors.Index, $actors.Groups[1].Value + $actorText)
    Write-Host "Test map: removed $siloCount resource silo(s); missile/nuclear silos unchanged."

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
