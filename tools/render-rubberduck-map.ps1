param(
    [Parameter(Mandatory = $true)][string]$MapPackage,
    [Parameter(Mandatory = $true)][string]$Destination,
    [int]$CenterU = 48,
    [int]$CenterV = 64,
    [int]$Width = 1600,
    [int]$Height = 1000,
    [double]$Scale = 0.5
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$tilesetPath = Join-Path $root "mods\ca\tilesets\rubberduck-temperate.yaml"
$tileset = Get-Content $tilesetPath -Raw
$terrainRoot = Join-Path $root "mods\ca"

$templates = @{}
$matches = [regex]::Matches($tileset, '(?ms)^\tTemplate@(\d+):\r?\n(.*?)(?=^\tTemplate@|\z)')
foreach ($match in $matches) {
    $id = [int]$match.Groups[1].Value
    $body = $match.Groups[2].Value
    $imageMatch = [regex]::Match($body, '(?m)^\t\tImages:\s*(.+?)\s*$')
    if (-not $imageMatch.Success) { continue }
    $framesMatch = [regex]::Match($body, '(?m)^\t\tFrames:\s*([0-9, ]+)\s*$')
    $frames = @()
    if ($framesMatch.Success) {
        $frames = @($framesMatch.Groups[1].Value.Split(',') | ForEach-Object { [int]$_.Trim() })
    }
    $templates[$id] = @{ Image = $imageMatch.Groups[1].Value.Trim(); Frames = $frames }
}

$zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $MapPackage))
try {
    $yamlEntry = $zip.GetEntry('map.yaml')
    $yamlReader = New-Object System.IO.StreamReader($yamlEntry.Open())
    $mapYaml = $yamlReader.ReadToEnd()
    $yamlReader.Dispose()
    $entry = $zip.GetEntry('map.bin')
    $stream = $entry.Open()
    $memory = New-Object System.IO.MemoryStream
    $stream.CopyTo($memory)
    $stream.Dispose()
    $data = $memory.ToArray()
    $memory.Dispose()
}
finally { $zip.Dispose() }

$mapWidth = [BitConverter]::ToUInt16($data, 1)
$mapHeight = [BitConverter]::ToUInt16($data, 3)
$tilesOffset = [BitConverter]::ToUInt32($data, 5)
$heightsOffset = [BitConverter]::ToUInt32($data, 9)

$canvasWidth = [int]($Width / $Scale)
$canvasHeight = [int]($Height / $Scale)
$canvas = New-Object System.Drawing.Bitmap($canvasWidth, $canvasHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($canvas)
$graphics.Clear([System.Drawing.Color]::FromArgb(32, 32, 32))
$graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$imageCache = @{}
$metadataCache = @{}
$centerX = $CenterU * 128 + ($CenterV -band 1) * 64
$centerY = $CenterV * 32

function Get-SheetMetadata([string]$imagePath) {
    if ($metadataCache.ContainsKey($imagePath)) { return $metadataCache[$imagePath] }
    $yamlPath = [System.IO.Path]::ChangeExtension($imagePath, '.yaml')
    $bitmap = [System.Drawing.Bitmap]::FromFile($imagePath)
    $frameWidth = $bitmap.Width
    $frameHeight = $bitmap.Height
    $offsetX = 0
    $offsetY = 0
    if (Test-Path $yamlPath) {
        $yaml = Get-Content $yamlPath -Raw
        $size = [regex]::Match($yaml, '(?m)^FrameSize:\s*(-?\d+),(-?\d+)')
        if ($size.Success) { $frameWidth = [int]$size.Groups[1].Value; $frameHeight = [int]$size.Groups[2].Value }
        $offset = [regex]::Match($yaml, '(?m)^Offset:\s*(-?\d+),(-?\d+)')
        if ($offset.Success) { $offsetX = [int]$offset.Groups[1].Value; $offsetY = [int]$offset.Groups[2].Value }
    }
    $meta = @{ Bitmap = $bitmap; Width = $frameWidth; Height = $frameHeight; OffsetX = $offsetX; OffsetY = $offsetY }
    $metadataCache[$imagePath] = $meta
    return $meta
}

$cells = @()
for ($u = 0; $u -lt $mapWidth; $u++) {
    for ($v = 0; $v -lt $mapHeight; $v++) {
        $index = $u * $mapHeight + $v
        $tileOffset = $tilesOffset + 3 * $index
        $type = [BitConverter]::ToUInt16($data, $tileOffset)
        if (-not $templates.ContainsKey([int]$type)) { continue }
        $tileIndex = [int]$data[$tileOffset + 2]
        $terrainHeight = if ($heightsOffset -gt 0) { [int]$data[$heightsOffset + $index] } else { 0 }
        $cells += [pscustomobject]@{ U=$u; V=$v; Type=$type; Index=$tileIndex; Height=$terrainHeight; SortY=($v * 32 - $terrainHeight * 32); SortX=($u * 128 + ($v -band 1) * 64) }
    }
}

try {
    foreach ($cell in ($cells | Sort-Object SortY, SortX)) {
        $template = $templates[[int]$cell.Type]
        $imagePath = Join-Path $terrainRoot $template.Image
        $meta = Get-SheetMetadata $imagePath
        $frame = if ($template.Frames.Count -gt 0) { [int]$template.Frames[[Math]::Min($cell.Index, $template.Frames.Count - 1)] } else { $cell.Index }
        $framesPerRow = [int]($meta.Bitmap.Width / $meta.Width)
        $sourceX = ($frame % $framesPerRow) * $meta.Width
        $sourceY = [int]([Math]::Floor($frame / $framesPerRow)) * $meta.Height
        $screenX = $cell.U * 128 + ($cell.V -band 1) * 64 - $centerX + $canvasWidth / 2
        $screenY = $cell.V * 32 - $cell.Height * 32 - $centerY + $canvasHeight / 2
        $left = [int]($screenX - $meta.Width / 2 + $meta.OffsetX)
        $top = [int]($screenY - $meta.Height / 2 + $meta.OffsetY)
        if ($left -gt $canvasWidth -or $top -gt $canvasHeight -or $left + $meta.Width -lt 0 -or $top + $meta.Height -lt 0) { continue }
        $graphics.DrawImage($meta.Bitmap,
            ([System.Drawing.Rectangle]::new($left, $top, $meta.Width, $meta.Height)),
            ([System.Drawing.Rectangle]::new($sourceX, $sourceY, $meta.Width, $meta.Height)),
            [System.Drawing.GraphicsUnit]::Pixel)
    }

    $actorImages = @{
        'ymcacliff.sstraight' = 'bits/terrain/rubberduck/cliffs/high_s_straight_overlay.png'
        'ymcacliff.sleft' = 'bits/terrain/rubberduck/cliffs/high_s_straight_left_cap.png'
        'ymcacliff.sright' = 'bits/terrain/rubberduck/cliffs/high_s_straight_right_cap.png'
        'ymcacliff.sisolated' = 'bits/terrain/rubberduck/cliffs/high_s_straight_isolated_cap.png'
        'ymcacliff.sw' = 'bits/terrain/rubberduck/cliffs/source/high_sw.png'
        'ymcacliff.se' = 'bits/terrain/rubberduck/cliffs/source/high_se.png'
        'ymcacliff.souter' = 'bits/terrain/rubberduck/cliffs/source/high_s_outer.png'
        'ymcacliff.eouter' = 'bits/terrain/rubberduck/cliffs/source/high_e_outer.png'
        'ymcacliff.wouter' = 'bits/terrain/rubberduck/cliffs/source/high_w_outer.png'
        'ymcacliff.ne' = 'bits/terrain/rubberduck/cliffs/source/high_ne_outer.png'
        'ymcacliff.nw' = 'bits/terrain/rubberduck/cliffs/source/high_nw_outer.png'
        'ymcacliff.n' = 'bits/terrain/rubberduck/cliffs/source/high_n_outer.png'
    }
    $actorMatches = [regex]::Matches($mapYaml, '(?ms)^\tActor\d+:\s*(ymcacliff\.[^\r\n]+).*?^\t\tLocation:\s*(-?\d+),(-?\d+)')
    $actorRows = foreach ($match in $actorMatches) {
        $cx = [int]$match.Groups[2].Value; $cy = [int]$match.Groups[3].Value
        [pscustomobject]@{ Type=$match.Groups[1].Value.Trim().ToLowerInvariant(); U=[int][Math]::Truncate(($cx-$cy)/2.0); V=$cx+$cy }
    }
    foreach ($actor in ($actorRows | Sort-Object V, U)) {
        $actorType = $actor.Type
        $actorFrame = 0
        if ($actorType -match '^ymcacliff\.sstraight([0-8])$') {
            $actorFrame = [int]$Matches[1]
            $actorType = 'ymcacliff.sstraight'
        }
        if (-not $actorImages.ContainsKey($actorType)) { continue }
        $imagePath = Join-Path $terrainRoot $actorImages[$actorType]
        if (-not $imageCache.ContainsKey($imagePath)) { $imageCache[$imagePath] = [System.Drawing.Bitmap]::FromFile($imagePath) }
        $bitmap = $imageCache[$imagePath]
        $frameWidth = 128; $frameHeight = 192
        $screenX = $actor.U * 128 + ($actor.V -band 1) * 64 - $centerX + $canvasWidth / 2
        $screenY = $actor.V * 32 - $centerY + $canvasHeight / 2
        $left = [int]($screenX - 64)
        $top = [int]($screenY - 192)
        $graphics.DrawImage($bitmap,
            ([System.Drawing.Rectangle]::new($left, $top, $frameWidth, $frameHeight)),
            ([System.Drawing.Rectangle]::new($actorFrame * $frameWidth, 0, $frameWidth, $frameHeight)),
            [System.Drawing.GraphicsUnit]::Pixel)
    }

    $result = New-Object System.Drawing.Bitmap($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $resultGraphics = [System.Drawing.Graphics]::FromImage($result)
    $resultGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $resultGraphics.DrawImage($canvas, 0, 0, $Width, $Height)
    $result.Save($Destination, [System.Drawing.Imaging.ImageFormat]::Png)
    $resultGraphics.Dispose()
    $result.Dispose()
}
finally {
    $graphics.Dispose()
    $canvas.Dispose()
    foreach ($meta in $metadataCache.Values) { $meta.Bitmap.Dispose() }
    foreach ($bitmap in $imageCache.Values) { $bitmap.Dispose() }
}
