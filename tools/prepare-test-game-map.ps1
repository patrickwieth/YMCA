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

    # The source locks Multi0's faction, so update the disposable player block
    # as well as the launch argument. Preserve explicit display-actor factions;
    # missing Nod initializers are supplied below so its bibs do not follow Multi0.
    $players = [regex]::Matches($yaml, '(?m)^\tPlayerReference@Multi0:\r?\n(?:\t\t[^\r\n]*\r?\n)*')
    if ($players.Count -ne 1) {
        throw "Expected exactly one Multi0 player in the test map."
    }
    $player = $players[0]
    if ([regex]::Matches($player.Value, '(?m)^\t\tFaction: [^\r\n]+').Count -ne 1) {
        throw "Expected exactly one Multi0 faction in the test map."
    }
    $alliedPlayer = [regex]::Replace($player.Value, '(?m)^(\t\tFaction: )[^\r\n]+', '${1}england')
    $yaml = $yaml.Remove($player.Index, $player.Length).Insert($player.Index, $alliedPlayer)

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

    # SENTRY.SOVIET is a separate prototype actor now. Do not override FTUR:
    # the Flame Tower must keep its own image, turning and build-menu entry.

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

    # Nod's former Legion/shared-factory display slot, not the GDI block.
    $nodFactoryPattern = '(?m)^(\tActor10:[ \t]*)(weap\.td|weap\.nod)(\r?\n(?:\t\t[^\r\n]*(?:\r?\n|\z))*)'
    $nodFactory = [regex]::Match($actorText, $nodFactoryPattern)
    if (-not $nodFactory.Success -or $nodFactory.Groups[3].Value -notmatch '(?m)^\t\tLocation:[ \t]*78,6\r?$') {
        throw "Nod factory display slot changed; refusing ambiguous replacement."
    }
    $actorText = [regex]::Replace($actorText, $nodFactoryPattern, '${1}weap.nod${3}')

    # Multi0 now starts as England. Nod's faction-gated foundations must still
    # receive a Nod FactionInit on the display actors (ownership stays Multi0).
    # These exact legacy slots are validated so no other faction block is changed.
    $nodDisplaySlots = @{
        Actor3 = 'afac.nod'; Actor4 = 'nuke.nod'; Actor5 = 'nuk2.nod';
        Actor6 = 'proc.td.nod'; Actor8 = 'hand'; Actor9 = 'airs';
        Actor10 = 'weap.nod'; Actor11 = 'hq.nod'; Actor12 = 'rep.nod';
        Actor13 = 'hpad.td.nod'; Actor14 = 'nsam'; Actor15 = 'gun.nod';
        Actor16 = 'ltur'; Actor17 = 'obli'; Actor18 = 'tmpl';
        Actor19 = 'sgen'; Actor20 = 'bio.nod'; Actor21 = 'mslo.nod'; Actor22 = 'spen.nod'
    }
    foreach ($id in $nodDisplaySlots.Keys) {
        $pattern = '(?m)^(\t' + [regex]::Escape($id) + ':[ \t]*' +
            [regex]::Escape($nodDisplaySlots[$id]) + '\r?\n)((?:\t\t[^\r\n]*(?:\r?\n|\z))*)'
        $matches = [regex]::Matches($actorText, $pattern)
        if ($matches.Count -ne 1) { throw "Nod display slot $id changed; refusing ambiguous faction assignment." }
        $match = $matches[0]
        if ($match.Groups[2].Value -notmatch '(?m)^\t\tFaction:') {
            $replacement = $match.Value.TrimEnd("`r", "`n") + $newline + "`t`tFaction: blackh" + $newline
            $actorText = $actorText.Remove($match.Index, $match.Length).Insert($match.Index, $replacement)
        }
    }
    Write-Host 'Test map: assigned explicit Nod display factions independently of the England player.'

    # The Soviet display block in this legacy map still uses Allied actor IDs.
    # Replace only these exact slots, never all instances of the shared names.
    $sovietSlots = @(
        @('Actor23', 'fact', 'fact.soviet', '116,-40'),
        @('Actor24', 'powr', 'powr.soviet', '122,-46'),
        @('Actor25', 'apwr', 'apwr.soviet', '128,-52'),
        # Reuse the removed storage-silo slot directly screen-above the helipad.
        # Same x-y (204), smaller x+y: 76 versus 84. Independent Sentry actor.
        @('Actor27', 'silo', 'sentry.soviet', '140,-64'),
        @('Actor30', 'weap', 'weap.soviet', '132,-48'),
        @('Actor31', 'dome', 'dome.soviet', '138,-54'),
        @('Actor32', 'hpad', 'hpad.soviet', '144,-60'),
        @('Actor35', 'fix', 'fix.soviet', '136,-44'),
        # Display the sprite replacement only in the disposable map.
        @('Actor37', 'sam', 'flak.soviet.sprite', '148,-56')
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
    Write-Host "Test map: assigned $($sovietSlots.Count) Soviet display slots, including the Sentry above the helipad."

    # Tesla Reactor is a separate Russian power building, not APWR.Soviet.
    # Add a dedicated slot screen-above the nuclear plant; retain both existing
    # power plants and never modify the authoritative source map.
    if (-not [regex]::IsMatch($actorText, '(?mi)^\t[^\t\r\n:]+:[ \t]*tpwr[ \t]*\r?$')) {
        if ([regex]::IsMatch($actorText, '(?m)^\tTestTeslaReactor:')) {
            throw 'TestTeslaReactor ID is already used by another actor.'
        }
        $actorText += (@(
            "`tTestTeslaReactor: tpwr"
            "`t`tOwner: Multi0"
            "`t`tLocation: 122,-58"
            "`t`tFaction: russia"
        ) -join $newline) + $newline
        Write-Host 'Test map: added Tesla Reactor above the Soviet nuclear power plant.'
    }

    # Side-by-side real voxel comparison, same screen row, two isometric cells right.
    # Only the approved sprite Sentry is displayed, never the voxel study.

    # Remove only resource-storage silos, never missile/nuclear silo actors.
    $siloPattern = '(?mi)^\t[^\t\r\n:]+:[ \t]*(?:silo|silo\.td|silo\.scrin|silo\.soviet)[ \t]*\r?\n(?:\t\t[^\r\n]*(?:\r?\n|\z))*'
    $siloCount = [regex]::Matches($actorText, $siloPattern).Count
    $actorText = [regex]::Replace($actorText, $siloPattern, '')
    $yaml = $yaml.Remove($actors.Index, $actors.Length).Insert($actors.Index, $actors.Groups[1].Value + $actorText)
    Write-Host "Test map: removed $siloCount resource silo(s); missile/nuclear silos unchanged."

    # Ground-force-fire is for this disposable firing test only; stock SAM and
    # the global anti-air flak actor retain their normal weapons.
    $rulesPattern = '(?m)^Rules:(\r?\n)'
    if ([regex]::Matches($yaml, $rulesPattern).Count -ne 1) {
        throw "Expected exactly one map Rules block."
    }
    $flakRule = 'Rules:' + $newline + (@(
        "`tFLAK.SOVIET.SPRITE:"
        "`t`tArmament:"
        "`t`t`tWeapon: SOVIET-CLUSTER-AA.Test"
    ) -join $newline) + $newline
    $yaml = [regex]::Replace($yaml, $rulesPattern, $flakRule)
    Write-Host "Test map: Soviet sprite flak replaces the SAM display slot; Ctrl+click ground tests firing."

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

& python "$PSScriptRoot/prepare_air_defense_targets.py" $DestinationMap
if ($LASTEXITCODE -ne 0) { throw "Air-defense test target preparation failed." }

& python "$PSScriptRoot/prepare_extra_faction_buildings.py" $DestinationMap
if ($LASTEXITCODE -ne 0) { throw "China/Scrin building test preparation failed." }
