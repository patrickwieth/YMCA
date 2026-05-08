param([string]$MapPath, [string]$EntrySource, [string]$DestPath)
$work = Join-Path $env:TEMP ('oramap_edit_' + [guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $work | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($MapPath, $work)
Copy-Item -LiteralPath $EntrySource -Destination (Join-Path $work (Split-Path $DestPath -Leaf)) -Force
$zip = $MapPath + '.zip'
if (Test-Path $zip) { Remove-Item -Force $zip }
Compress-Archive -Path (Join-Path $work '*') -DestinationPath $zip -Force
Remove-Item -Force $MapPath
Move-Item -Force $zip $MapPath
Remove-Item -Recurse -Force $work
