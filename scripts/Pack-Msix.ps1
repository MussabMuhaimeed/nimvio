# Packs a published WinForms app folder into an unsigned .msix (for Partner Center upload).
param(
    [Parameter(Mandatory = $true)]
    [string] $PublishDir,

    [Parameter(Mandatory = $true)]
    [string] $OutputMsix,

    [Parameter(Mandatory = $false)]
    [string] $Version = '1.0.0.0',

    [Parameter(Mandatory = $false)]
    [string] $ManifestPath = (Join-Path $PSScriptRoot '..\packaging\AppxManifest.xml'),

    [Parameter(Mandatory = $false)]
    [string] $AssetsDir = (Join-Path $PSScriptRoot '..\Nimvio.App\assets\msix')
)

$ErrorActionPreference = 'Stop'

function Resolve-MakeAppx {
    $candidates = @(
        Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin\*\x64\makeappx.exe' -ErrorAction SilentlyContinue
        Get-ChildItem 'C:\Program Files\Windows Kits\10\bin\*\x64\makeappx.exe' -ErrorAction SilentlyContinue
    ) | Sort-Object FullName -Descending

    if (-not $candidates) {
        throw 'MakeAppx.exe not found. Install the Windows 10/11 SDK (App certification / packaging tools).'
    }

    return $candidates[0].FullName
}

function Normalize-Version([string] $raw) {
    $v = $raw.Trim()
    if ($v.StartsWith('v') -or $v.StartsWith('V')) {
        $v = $v.Substring(1)
    }

    $parts = $v.Split('.')
    while ($parts.Count -lt 4) {
        $parts += '0'
    }

    return ($parts[0..3] -join '.')
}

$PublishDir = (Resolve-Path $PublishDir).Path
$ManifestPath = (Resolve-Path $ManifestPath).Path
$AssetsDir = (Resolve-Path $AssetsDir).Path
$Version = Normalize-Version $Version
$makeAppx = Resolve-MakeAppx

$stage = Join-Path $env:TEMP ("nimvio-msix-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $stage | Out-Null

try {
    Write-Host "Staging package from $PublishDir"
    Copy-Item -Path (Join-Path $PublishDir '*') -Destination $stage -Recurse -Force

    $stageAssets = Join-Path $stage 'Assets'
    New-Item -ItemType Directory -Force -Path $stageAssets | Out-Null
    Copy-Item (Join-Path $AssetsDir 'StoreLogo.png') (Join-Path $stageAssets 'StoreLogo.png') -Force
    Copy-Item (Join-Path $AssetsDir 'Square150x150Logo.png') (Join-Path $stageAssets 'Square150x150Logo.png') -Force
    Copy-Item (Join-Path $AssetsDir 'Square44x44Logo.png') (Join-Path $stageAssets 'Square44x44Logo.png') -Force

    if (-not (Test-Path (Join-Path $stage 'Nimvio.exe'))) {
        throw "Nimvio.exe not found in publish folder: $PublishDir"
    }

    [xml] $manifest = Get-Content -Path $ManifestPath -Raw
    $manifest.Package.Identity.Version = $Version
    $manifest.Package.Identity.ProcessorArchitecture = 'x64'
    $outManifest = Join-Path $stage 'AppxManifest.xml'
    $manifest.Save($outManifest)

    $outDir = Split-Path -Parent $OutputMsix
    if ($outDir) {
        New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    }
    if (Test-Path $OutputMsix) {
        Remove-Item $OutputMsix -Force
    }

    Write-Host "Packing MSIX $OutputMsix (version $Version) with $makeAppx"
    & $makeAppx pack /d $stage /p $OutputMsix /o
    if ($LASTEXITCODE -ne 0) {
        throw "MakeAppx failed with exit code $LASTEXITCODE"
    }

    Write-Host "Created $OutputMsix"
}
finally {
    Remove-Item -Recurse -Force $stage -ErrorAction SilentlyContinue
}
