param([Parameter(Mandatory=$true)][string]$EngineDirectory)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$engine = if ([IO.Path]::IsPathRooted($EngineDirectory)) { [IO.Path]::GetFullPath($EngineDirectory) } else { [IO.Path]::GetFullPath((Join-Path (Get-Location).Path $EngineDirectory)) }
if (!(Test-Path (Join-Path $engine 'OpenRA.Game/OpenRA.Game.csproj'))) {
    throw "Engine source not found: $engine"
}
if (!(Get-Command git -ErrorAction SilentlyContinue)) {
    throw 'Git is required to prepare the YMCA engine patches. Install Git for Windows and retry.'
}
# -p2 removes a/engine/; the explicit directory supports custom engine locations.
# Do not reset/refetch an existing engine or silently ignore conflicting local edits.
Push-Location (Split-Path -Parent $engine)
try {
    $directory = Split-Path -Leaf $engine
    foreach ($entry in Get-Content (Join-Path $PSScriptRoot 'engine-patches.txt')) {
        if ([string]::IsNullOrWhiteSpace($entry)) { continue }
        $patch = Join-Path $root $entry.Trim()
        if (!(Test-Path $patch)) { throw "Missing engine patch: $patch" }
        # Capture native stderr as well: Windows PowerShell treats it as an error
        # under Stop even when a failed reverse check is expected for a fresh patch.
        $previousPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        $null = & git apply --no-index --reverse --check --ignore-space-change -p2 "--directory=$directory" $patch 2>&1
        $alreadyApplied = $LASTEXITCODE -eq 0
        $ErrorActionPreference = $previousPreference
        if ($alreadyApplied) { continue }
        & git apply --no-index --check --ignore-space-change -p2 "--directory=$directory" $patch
        if ($LASTEXITCODE -ne 0) { throw "Engine patch conflicts with local source: $entry. No build was started." }
        & git apply --no-index --ignore-space-change -p2 "--directory=$directory" $patch
        if ($LASTEXITCODE -ne 0) { throw "Could not apply engine patch: $entry" }
        Write-Host "Applied engine patch: $entry"
    }
} finally { Pop-Location }
