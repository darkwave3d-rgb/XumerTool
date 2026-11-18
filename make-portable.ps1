# Crea bin\XumerTool-<versione>-portable.zip (versione senza installer). Richiamato da build-installer.bat.
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
Compress-Archive -Path $t -DestinationPath $z
Remove-Item $tmp -Recurse -Force
Write-Host "Fatto: $z"
