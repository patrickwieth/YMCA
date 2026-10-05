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
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

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

    $minX = [int]($Bitmap.Width / 10)
    $maxX = [int]($Bitmap.Width * 9 / 10)
    $minY = [int]($Bitmap.Height / 10)
    $maxY = [int]($Bitmap.Height * 9 / 10)
    for ($y = $minY; $y -lt $maxY; $y += $stepY) {
        for ($x = $minX; $x -lt $maxX; $x += $stepX) {
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
    # The directory does not exist until the first successful screenshot.
    # Still try the framebuffer hotkey on a fresh installation.
    $roots.Add($appDataRoot)

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

$previousForegroundWindow = [Win32.User32]::GetForegroundWindow()

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
    # TakeScreenshot is Ctrl+P. SDL does not reliably preserve modifier state
    # for posted window messages, so briefly focus OpenRA and send real key events.
    [void][Win32.User32]::ShowWindowAsync($process.MainWindowHandle, 9)
    [void][Win32.User32]::SetForegroundWindow($process.MainWindowHandle)
    Start-Sleep -Milliseconds 500
    if ([Win32.User32]::GetForegroundWindow() -ne $process.MainWindowHandle) {
        [Microsoft.VisualBasic.Interaction]::AppActivate($process.Id)
        Start-Sleep -Milliseconds 500
    }
    try {
        if ([Win32.User32]::GetForegroundWindow() -ne $process.MainWindowHandle) {
            throw "Could not focus OpenRA; refusing to send hotkeys to another application."
        }
        [Win32.User32]::keybd_event(0x11, 0x1D, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 100
        [Win32.User32]::keybd_event(0x50, 0x19, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 100
        [Win32.User32]::keybd_event(0x50, 0x19, 2, [UIntPtr]::Zero)
        [Win32.User32]::keybd_event(0x11, 0x1D, 2, [UIntPtr]::Zero)

        $deadline = (Get-Date).AddSeconds($TimeoutSec)
        while ((Get-Date) -lt $deadline) {
            Start-Sleep -Milliseconds 250
            $screenshot = Find-NewestOpenRAScreenshot -Roots $screenshotRoots -After $screenshotStart
            if ($screenshot) {
                Copy-Item -LiteralPath $screenshot.FullName -Destination $destPath -Force
                Write-Output $destPath
                return
            }
        }
    }
    finally {
        [Win32.User32]::keybd_event(0x50, 0x19, 2, [UIntPtr]::Zero)
        [Win32.User32]::keybd_event(0x11, 0x1D, 2, [UIntPtr]::Zero)
        if ($previousForegroundWindow -ne [IntPtr]::Zero) {
            [void][Win32.User32]::SetForegroundWindow($previousForegroundWindow)
        }
    }

    throw "OpenRA did not produce a framebuffer screenshot within $TimeoutSec seconds."
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
        [void][Win32.User32]::ShowWindowAsync($process.MainWindowHandle, 9)
        [void][Win32.User32]::SetForegroundWindow($process.MainWindowHandle)
        Start-Sleep -Milliseconds 250
        $bitmap = New-Object System.Drawing.Bitmap($windowWidth, $windowHeight)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.CopyFromScreen($windowRect.Left, $windowRect.Top, 0, 0, $bitmap.Size)
    }

    $bitmap.Save($destPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    [void][Win32.User32]::SetWindowPos($process.MainWindowHandle, [IntPtr](-2), 0, 0, 0, 0, 0x0001 -bor 0x0002 -bor 0x0040)
    if ($previousForegroundWindow -ne [IntPtr]::Zero) {
        [void][Win32.User32]::SetForegroundWindow($previousForegroundWindow)
    }
    if ($null -ne $graphics) { $graphics.Dispose() }
    if ($null -ne $bitmap) { $bitmap.Dispose() }
    if ($null -ne $printGraphics) { $printGraphics.Dispose() }
    if ($null -ne $printBitmap) { $printBitmap.Dispose() }
}

Write-Output $destPath
