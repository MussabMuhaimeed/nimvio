param([switch]$StartWithWindows)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'app'
$installDir = Join-Path $env:LOCALAPPDATA 'Nimvio'
$exe = Join-Path $installDir 'Nimvio.exe'

if (-not (Test-Path -LiteralPath (Join-Path $source 'Nimvio.exe'))) { throw 'The app folder must be beside install.ps1' }
Stop-Process -Name 'Nimvio' -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 250
New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item -Path (Join-Path $source '*') -Destination $installDir -Recurse -Force

$shell = New-Object -ComObject WScript.Shell
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Nimvio.lnk'
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = 'Nimvio — Your curious desktop companions'
$shortcut.IconLocation = $exe + ',0'
$shortcut.Save()

if ($StartWithWindows) {
    New-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'Nimvio' -Value ('"' + $exe + '"') -PropertyType String -Force | Out-Null
}

Start-Process -FilePath $exe
Write-Host "Nimvio installed in $installDir"
