param(
    [string]$ModId = "ca",
    [string]$Destination = "",
    [int]$TimeoutSec = 8
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName Microsoft.VisualBasic
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

namespace Win32
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static class User32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    }
}
"@

function Test-BitmapHasContent {
    param(
        [Parameter(Mandatory = $true)]
        [System.Drawing.Bitmap]$Bitmap
    )

    $stepX = [Math]::Max(1, [int]($Bitmap.Width / 16))
    $stepY = [Math]::Max(1, [int]($Bitmap.Height / 16))
    $nonBlack = 0
    $sampled = 0

    for ($y = 0; $y -lt $Bitmap.Height; $y += $stepY) {
        for ($x = 0; $x -lt $Bitmap.Width; $x += $stepX) {
            $pixel = $Bitmap.GetPixel($x, $y)
            $sampled++
            if (($pixel.A -gt 0) -and (($pixel.R + $pixel.G + $pixel.B) -gt 24)) {
                $nonBlack++
            }
        }
    }

    return $sampled -gt 0 -and $nonBlack -ge 4
}

$process = Get-Process OpenRA -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowHandle -ne 0 } |
    Sort-Object StartTime -Descending |
    Select-Object -First 1

if (-not $process) {
    throw "No running OpenRA window found."
}

[void][Win32.User32]::ShowWindowAsync($process.MainWindowHandle, 9)
Start-Sleep -Milliseconds 200
[void][Win32.User32]::SetForegroundWindow($process.MainWindowHandle)
try { [Microsoft.VisualBasic.Interaction]::AppActivate($process.Id) | Out-Null } catch { }
[void][Win32.User32]::SetWindowPos($process.MainWindowHandle, [IntPtr](-1), 0, 0, 0, 0, 0x0001 -bor 0x0002 -bor 0x0040)
Start-Sleep -Milliseconds 400

$rect = New-Object Win32.RECT
if (-not [Win32.User32]::GetClientRect($process.MainWindowHandle, [ref]$rect)) {
    throw "Failed to query OpenRA client bounds."
}

$topLeft = New-Object Win32.User32+POINT
$topLeft.X = $rect.Left
$topLeft.Y = $rect.Top
if (-not [Win32.User32]::ClientToScreen($process.MainWindowHandle, [ref]$topLeft)) {
    throw "Failed to convert OpenRA client origin to screen coordinates."
}

$bottomRight = New-Object Win32.User32+POINT
$bottomRight.X = $rect.Right
$bottomRight.Y = $rect.Bottom
if (-not [Win32.User32]::ClientToScreen($process.MainWindowHandle, [ref]$bottomRight)) {
    throw "Failed to convert OpenRA client bottom-right to screen coordinates."
}

$width = $bottomRight.X - $topLeft.X
$height = $bottomRight.Y - $topLeft.Y
if ($width -le 0 -or $height -le 0) {
    throw "OpenRA window bounds are invalid: ${width}x${height}."
}

$windowRect = New-Object Win32.RECT
if (-not [Win32.User32]::GetWindowRect($process.MainWindowHandle, [ref]$windowRect)) {
    throw "Failed to query OpenRA window bounds."
}

$windowWidth = $windowRect.Right - $windowRect.Left
$windowHeight = $windowRect.Bottom - $windowRect.Top
if ($windowWidth -le 0 -or $windowHeight -le 0) {
    throw "OpenRA outer window bounds are invalid: ${windowWidth}x${windowHeight}."
}

$destPath = if ($Destination) {
    [System.IO.Path]::GetFullPath($Destination)
}
else {
    Join-Path (Get-Location) "openra-capture.png"
}

$destDir = Split-Path -Parent $destPath
if ($destDir -and -not (Test-Path $destDir)) {
    New-Item -ItemType Directory -Path $destDir | Out-Null
}

$bitmap = $null
$graphics = $null
$windowBitmap = $null
$windowGraphics = $null
try {
    $clientX = $topLeft.X - $windowRect.Left
    $clientY = $topLeft.Y - $windowRect.Top
    $clientRect = New-Object System.Drawing.Rectangle($clientX, $clientY, $width, $height)

    $windowBitmap = New-Object System.Drawing.Bitmap($windowWidth, $windowHeight)
    $windowGraphics = [System.Drawing.Graphics]::FromImage($windowBitmap)
    $windowHdc = $windowGraphics.GetHdc()
    try {
        $printOk = [Win32.User32]::PrintWindow($process.MainWindowHandle, $windowHdc, 2)
    }
    finally {
        $windowGraphics.ReleaseHdc($windowHdc)
    }

    if ($printOk -and (Test-BitmapHasContent -Bitmap $windowBitmap)) {
        $bitmap = $windowBitmap.Clone($clientRect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    }
    else {
        $bitmap = New-Object System.Drawing.Bitmap($width, $height)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.CopyFromScreen($topLeft.X, $topLeft.Y, 0, 0, $bitmap.Size)
    }

    $bitmap.Save($destPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    [void][Win32.User32]::SetWindowPos($process.MainWindowHandle, [IntPtr](-2), 0, 0, 0, 0, 0x0001 -bor 0x0002 -bor 0x0040)
    if ($null -ne $graphics) { $graphics.Dispose() }
    if ($null -ne $bitmap) { $bitmap.Dispose() }
    if ($null -ne $windowGraphics) { $windowGraphics.Dispose() }
    if ($null -ne $windowBitmap) { $windowBitmap.Dispose() }
}

Write-Output $destPath
