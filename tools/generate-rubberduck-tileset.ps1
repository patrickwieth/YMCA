param(
	[string]$SourceDir = "E:\\src\\YMCA\\rubberduck terrain",
	[string]$OutDir = "E:\\src\\YMCA\\Game\\mods\\ca\\bits\\terrain\\rubberduck",
	[string]$GameDir = "E:\\src\\YMCA\\Game"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$tileW = 128
$tileH = 64
$cols = 8
$blockRows = 10

function New-EmptyBitmap {
	param([int]$Width, [int]$Height)
	$bmp = [System.Drawing.Bitmap]::new($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
	$bmp.SetResolution(96, 96)
	return $bmp
}

function Copy-Tile {
	param(
		[System.Drawing.Graphics]$g,
		[System.Drawing.Image]$src,
		[int]$srcRow,
		[int]$srcCol,
		[int]$dstX,
		[int]$dstY
	)
	$srcRect = New-Object System.Drawing.Rectangle ($srcCol * $tileW), ($srcRow * $tileH), $tileW, $tileH
	$dstRect = New-Object System.Drawing.Rectangle $dstX, $dstY, $tileW, $tileH
	$g.DrawImage($src, $dstRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
}

function Save-BaseSheet {
	param(
		[string]$srcPath,
		[int]$blockIndex,
		[string]$outPath
	)
	$src = [System.Drawing.Image]::FromFile($srcPath)
	try {
		$outBmp = New-EmptyBitmap ($cols * $tileW) (2 * $tileH)
		$g = [System.Drawing.Graphics]::FromImage($outBmp)
		$g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
		for ($row = 0; $row -lt 2; $row++) {
			for ($col = 0; $col -lt $cols; $col++) {
				$srcRow = ($blockIndex * $blockRows) + $row
				Copy-Tile -g $g -src $src -srcRow $srcRow -srcCol $col -dstX ($col * $tileW) -dstY ($row * $tileH)
			}
		}
		$outBmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
		$g.Dispose()
		$outBmp.Dispose()
	}
	finally {
		$src.Dispose()
	}
}

function Save-CompositeSheet {
	param(
		[string]$topPath,
		[int]$topBlock,
		[string]$basePath,
		[int]$baseBlock,
		[string]$outPath
	)
	$top = [System.Drawing.Image]::FromFile($topPath)
	$base = [System.Drawing.Image]::FromFile($basePath)
	try {
		$outBmp = New-EmptyBitmap ($cols * $tileW) ($blockRows * $tileH)
		$g = [System.Drawing.Graphics]::FromImage($outBmp)
		$g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver

		# Use the top-left tile of the base material block as the base.
		$baseRow = $baseBlock * $blockRows
		$baseRect = New-Object System.Drawing.Rectangle 0, ($baseRow * $tileH), $tileW, $tileH
		$baseTile = [System.Drawing.Bitmap]::new($tileW, $tileH, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
		$bg = [System.Drawing.Graphics]::FromImage($baseTile)
		$bg.DrawImage($base, (New-Object System.Drawing.Rectangle 0, 0, $tileW, $tileH), $baseRect, [System.Drawing.GraphicsUnit]::Pixel)
		$bg.Dispose()

		for ($row = 0; $row -lt $blockRows; $row++) {
			for ($col = 0; $col -lt $cols; $col++) {
				$tile = [System.Drawing.Bitmap]::new($tileW, $tileH, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
				$tg = [System.Drawing.Graphics]::FromImage($tile)
				$tg.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
				$tg.DrawImage($baseTile, 0, 0, $tileW, $tileH)
				$srcRow = ($topBlock * $blockRows) + $row
				$srcRect = New-Object System.Drawing.Rectangle ($col * $tileW), ($srcRow * $tileH), $tileW, $tileH
				$tg.DrawImage($top, (New-Object System.Drawing.Rectangle 0, 0, $tileW, $tileH), $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
				$tg.Dispose()

				$dstRect = New-Object System.Drawing.Rectangle ($col * $tileW), ($row * $tileH), $tileW, $tileH
				$g.DrawImage($tile, $dstRect, (New-Object System.Drawing.Rectangle 0, 0, $tileW, $tileH), [System.Drawing.GraphicsUnit]::Pixel)
				$tile.Dispose()
			}
		}

		$baseTile.Dispose()
		$outBmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
		$g.Dispose()
		$outBmp.Dispose()
	}
	finally {
		$top.Dispose()
		$base.Dispose()
	}
}

function Write-FrameMetadata {
	param(
		[string]$pngPath,
		[int]$frames
	)
	$yamlPath = [System.IO.Path]::ChangeExtension($pngPath, "yaml")
	@"
FrameSize: 128,64
FrameAmount: $frames
"@ | Set-Content -Path $yamlPath -Encoding Ascii
}

function Embed-FrameMetadata {
	param(
		[string]$pngPath
	)
	$utility = Join-Path $GameDir "utility.cmd"
	Push-Location $GameDir
	try {
		& $utility ca --png-sheet-import $pngPath | Out-Null
	}
	finally {
		Pop-Location
	}
}

if (!(Test-Path $OutDir)) {
	New-Item -ItemType Directory -Path $OutDir | Out-Null
}

$grass = Join-Path $SourceDir "grass_tiles_w_trans.png"
$dirt = Join-Path $SourceDir "dirt_tiles_w_trans.png"
$sand = Join-Path $SourceDir "sand_tiles_w_trans.png"

if (!(Test-Path $grass) -or !(Test-Path $dirt) -or !(Test-Path $sand)) {
	throw "Missing source sheets in $SourceDir"
}

$jobs = @()

# Normal tileset (blocks 0 and 2)
$jobs += @{ Type = "base"; Src = $grass; Block = 0; Out = "grass_a_base.png"; Frames = 16 }
$jobs += @{ Type = "base"; Src = $grass; Block = 2; Out = "grass_b_base.png"; Frames = 16 }
$jobs += @{ Type = "base"; Src = $dirt; Block = 0; Out = "dirt_a_base.png"; Frames = 16 }
$jobs += @{ Type = "base"; Src = $dirt; Block = 2; Out = "dirt_b_base.png"; Frames = 16 }
$jobs += @{ Type = "base"; Src = $sand; Block = 0; Out = "sand_base.png"; Frames = 16 }

$jobs += @{ Type = "comp"; Top = $grass; TopBlock = 0; Base = $dirt; BaseBlock = 0; Out = "grass_a_over_dirt.png"; Frames = 80 }
$jobs += @{ Type = "comp"; Top = $grass; TopBlock = 2; Base = $dirt; BaseBlock = 0; Out = "grass_b_over_dirt.png"; Frames = 80 }
$jobs += @{ Type = "comp"; Top = $grass; TopBlock = 0; Base = $sand; BaseBlock = 0; Out = "grass_a_over_sand.png"; Frames = 80 }
$jobs += @{ Type = "comp"; Top = $grass; TopBlock = 2; Base = $sand; BaseBlock = 0; Out = "grass_b_over_sand.png"; Frames = 80 }
$jobs += @{ Type = "comp"; Top = $sand; TopBlock = 0; Base = $dirt; BaseBlock = 0; Out = "sand_over_dirt.png"; Frames = 80 }
$jobs += @{ Type = "comp"; Top = $grass; TopBlock = 0; Base = $dirt; BaseBlock = 2; Out = "grass_a_over_dirt_dark.png"; Frames = 80 }
$jobs += @{ Type = "comp"; Top = $grass; TopBlock = 2; Base = $dirt; BaseBlock = 2; Out = "grass_b_over_dirt_dark.png"; Frames = 80 }
$jobs += @{ Type = "comp"; Top = $sand; TopBlock = 0; Base = $dirt; BaseBlock = 2; Out = "sand_over_dirt_dark.png"; Frames = 80 }

# Shaded tileset (blocks 1 and 3; sand has block 1)
$jobs += @{ Type = "base"; Src = $grass; Block = 1; Out = "grass_a_base_shaded.png"; Frames = 16 }
$jobs += @{ Type = "base"; Src = $grass; Block = 3; Out = "grass_b_base_shaded.png"; Frames = 16 }
$jobs += @{ Type = "base"; Src = $dirt; Block = 1; Out = "dirt_a_base_shaded.png"; Frames = 16 }
$jobs += @{ Type = "base"; Src = $dirt; Block = 3; Out = "dirt_b_base_shaded.png"; Frames = 16 }
$jobs += @{ Type = "base"; Src = $sand; Block = 1; Out = "sand_base_shaded.png"; Frames = 16 }

$jobs += @{ Type = "comp"; Top = $grass; TopBlock = 1; Base = $dirt; BaseBlock = 1; Out = "grass_a_over_dirt_shaded.png"; Frames = 80 }
$jobs += @{ Type = "comp"; Top = $grass; TopBlock = 3; Base = $dirt; BaseBlock = 1; Out = "grass_b_over_dirt_shaded.png"; Frames = 80 }
$jobs += @{ Type = "comp"; Top = $grass; TopBlock = 1; Base = $sand; BaseBlock = 1; Out = "grass_a_over_sand_shaded.png"; Frames = 80 }
$jobs += @{ Type = "comp"; Top = $grass; TopBlock = 3; Base = $sand; BaseBlock = 1; Out = "grass_b_over_sand_shaded.png"; Frames = 80 }
$jobs += @{ Type = "comp"; Top = $sand; TopBlock = 1; Base = $dirt; BaseBlock = 1; Out = "sand_over_dirt_shaded.png"; Frames = 80 }
$jobs += @{ Type = "comp"; Top = $grass; TopBlock = 1; Base = $dirt; BaseBlock = 3; Out = "grass_a_over_dirt_dark_shaded.png"; Frames = 80 }
$jobs += @{ Type = "comp"; Top = $grass; TopBlock = 3; Base = $dirt; BaseBlock = 3; Out = "grass_b_over_dirt_dark_shaded.png"; Frames = 80 }
$jobs += @{ Type = "comp"; Top = $sand; TopBlock = 1; Base = $dirt; BaseBlock = 3; Out = "sand_over_dirt_dark_shaded.png"; Frames = 80 }

foreach ($job in $jobs) {
	$outPath = Join-Path $OutDir $job.Out
	if ($job.Type -eq "base") {
		Save-BaseSheet -srcPath $job.Src -blockIndex $job.Block -outPath $outPath
	}
	else {
		Save-CompositeSheet -topPath $job.Top -topBlock $job.TopBlock -basePath $job.Base -baseBlock $job.BaseBlock -outPath $outPath
	}

	Write-FrameMetadata -pngPath $outPath -frames $job.Frames
	Embed-FrameMetadata -pngPath $outPath
}

Write-Host "Generated tilesheets in $OutDir"
