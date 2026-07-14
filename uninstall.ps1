$ErrorActionPreference = 'SilentlyContinue'
Stop-Process -Name 'Nimvio' -Force
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'Nimvio'
Remove-Item -LiteralPath (Join-Path ([Environment]::GetFolderPath('Desktop')) 'Nimvio.lnk') -Force
Remove-Item -LiteralPath (Join-Path $env:LOCALAPPDATA 'Nimvio') -Recurse -Force
Write-Host 'Nimvio removed.'
