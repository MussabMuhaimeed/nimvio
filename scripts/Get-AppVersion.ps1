# Reads app version from git tags (exact HEAD tag, else latest v*).
param(
    [string] $RepoRoot = (Join-Path $PSScriptRoot '..')
)

$ErrorActionPreference = 'Stop'
Push-Location $RepoRoot
try {
    $tag = git describe --tags --exact-match --match 'v*' HEAD 2>$null
    if (-not $tag) {
        $tag = git describe --tags --match 'v*' --abbrev=0 2>$null
    }

    if ([string]::IsNullOrWhiteSpace($tag)) {
        throw "No git tag matching 'v*' was found. Create one first (e.g. git tag v26.8.3)."
    }

    $version = $tag.Trim().TrimStart('v', 'V')
    if ($version -notmatch '^\d+(\.\d+){1,3}(-.+)?$') {
        throw "Invalid version '$version' from tag '$tag'."
    }

    Write-Output $version
}
finally {
    Pop-Location
}
