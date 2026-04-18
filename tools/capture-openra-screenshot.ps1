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

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    }
}
"@

function Test-BitmapHasUsefulContent {
    param(
        [Parameter(Mandatory = $true)]
        [System.Drawing.Bitmap]$Bitmap
    )

    $stepX = [Math]::Max(1, [int]($Bitmap.Width / 32))
    $stepY = [Math]::Max(1, [int]($Bitmap.Height / 32))
    $nonBlack = 0
    $sampled = 0

    for ($y = 0; $y -lt $Bitmap.Height; $y += $stepY) {
        for ($x = 0; $x -lt $Bitmap.Width; $x += $stepX) {
            $pixel = $Bitmap.GetPixel($x, $y)
            $sampled++
            if (($pixel.A -gt 0) -and (($pixel.R + $pixel.G + $pixel.B) -gt 40)) {
                $nonBlack++
            }
        }
    }

    if ($sampled -le 0) { return $false }
    return ($nonBlack / $sampled) -ge 0.08
}

function Get-OpenRAScreenshotRoots {
    param(
        [string]$ModId,
        [System.Diagnostics.Process]$Process
    )

    $roots = New-Object System.Collections.Generic.List[string]

    $appDataRoot = Join-Path $env:APPDATA "OpenRA\Screenshots\$ModId"
    if (Test-Path $appDataRoot) {
        $roots.Add($appDataRoot)
    }

    try {
        $processDir = Split-Path -Parent $Process.MainModule.FileName
        $engineDir = Split-Path -Parent $processDir
        $localRoot = Join-Path $engineDir "Support\Screenshots\$ModId"
        if (Test-Path $localRoot) {
            $roots.Add($localRoot)
        }
    }
    catch { }

    return $roots
}

function Find-NewestOpenRAScreenshot {
    param(
        [string[]]$Roots,
        [datetime]$After
    )

    $candidate = $null
    foreach ($root in $Roots) {
        if (-not (Test-Path $root)) {
            continue
        }

        $latest = Get-ChildItem -Path $root -Recurse -Filter "*.png" -ErrorAction SilentlyContinue |
            Where-Object { $_.LastWriteTime -ge $After } |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

        if ($latest -and (($null -eq $candidate) -or ($latest.LastWriteTime -gt $candidate.LastWriteTime))) {
            $candidate = $latest
        }
    }

    return $candidate
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

$screenshotRoots = @(Get-OpenRAScreenshotRoots -ModId $ModId -Process $process)
$screenshotStart = (Get-Date).AddSeconds(-1)
if ($screenshotRoots.Length -gt 0) {
    [void][Win32.User32]::SetForegroundWindow($process.MainWindowHandle)
    try { [Microsoft.VisualBasic.Interaction]::AppActivate($process.Id) | Out-Null } catch { }

    # Trigger OpenRA's own framebuffer screenshot. This avoids black captures from
    # OpenGL/DirectX windows where PrintWindow and CopyFromScreen can fail.
    [Win32.User32]::keybd_event(0x7B, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 50
    [Win32.User32]::keybd_event(0x7B, 0, 2, [UIntPtr]::Zero)

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 250
        $screenshot = Find-NewestOpenRAScreenshot -Roots $screenshotRoots -After $screenshotStart
        if ($screenshot) {
            Copy-Item -LiteralPath $screenshot.FullName -Destination $destPath -Force
            [void][Win32.User32]::SetWindowPos($process.MainWindowHandle, [IntPtr](-2), 0, 0, 0, 0, 0x0001 -bor 0x0002 -bor 0x0040)
            Write-Output $destPath
            return
        }
    }
}

$bitmap = $null
$graphics = $null
$printBitmap = $null
$printGraphics = $null
try {
    $printBitmap = New-Object System.Drawing.Bitmap($windowWidth, $windowHeight)
    $printGraphics = [System.Drawing.Graphics]::FromImage($printBitmap)
    $hdc = $printGraphics.GetHdc()
    try {
        $printOk = [Win32.User32]::PrintWindow($process.MainWindowHandle, $hdc, 0)
    }
    finally {
        $printGraphics.ReleaseHdc($hdc)
    }

    if ($printOk -and (Test-BitmapHasUsefulContent -Bitmap $printBitmap)) {
        $bitmap = $printBitmap
        $printBitmap = $null
    }
    else {
        $bitmap = New-Object System.Drawing.Bitmap($windowWidth, $windowHeight)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.CopyFromScreen($windowRect.Left, $windowRect.Top, 0, 0, $bitmap.Size)
    }

    $bitmap.Save($destPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    [void][Win32.User32]::SetWindowPos($process.MainWindowHandle, [IntPtr](-2), 0, 0, 0, 0, 0x0001 -bor 0x0002 -bor 0x0040)
    if ($null -ne $graphics) { $graphics.Dispose() }
    if ($null -ne $bitmap) { $bitmap.Dispose() }
    if ($null -ne $printGraphics) { $printGraphics.Dispose() }
    if ($null -ne $printBitmap) { $printBitmap.Dispose() }
}

Write-Output $destPath
