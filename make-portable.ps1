# Crea bin\XumerTool-<versione>-portable.zip (la versione pubblicata). Richiamato da build-portable.bat.
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$v = ([regex]::Match((Get-Content 'src\About.cs' -Raw), 'AppVersion\s*=\s*"([^"]+)"')).Groups[1].Value
$tmp = 'bin\portable'
$t = Join-Path $tmp 'XUMER TOOL'
if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }
New-Item -ItemType Directory -Force (Join-Path $t 'Licenze') | Out-Null
Copy-Item 'bin\XumerTool.exe', 'installer\LEGGIMI.txt', 'installer\README.txt' $t
Copy-Item 'LICENSE' (Join-Path $t 'LICENSE.txt')
Copy-Item 'licenses\BSD-OpenCBM-d64copy.txt', 'fonts\OFL-*.txt' (Join-Path $t 'Licenze')
$z = "bin\XumerTool-$v-portable.zip"
if (Test-Path $z) { Remove-Item $z }
# ogni file viene aggiunto a mano con il nome scritto con "/": Compress-Archive e ZipFile, in PowerShell 5.1,
# scrivono "\" e alcuni programmi di decompressione (macOS, Linux, vecchie versioni) non creano le cartelle
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$root = (Resolve-Path $tmp).Path
$zipPath = Join-Path (Resolve-Path 'bin').Path (Split-Path $z -Leaf)
$zip = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($f in Get-ChildItem $root -Recurse -File) {
        $name = $f.FullName.Substring($root.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $f.FullName, $name, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $zip.Dispose() }
Remove-Item $tmp -Recurse -Force
Write-Host "Fatto: $z"
