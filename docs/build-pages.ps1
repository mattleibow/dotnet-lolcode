#!/usr/bin/env pwsh

<#
.SYNOPSIS
Builds the complete dotnet-lolcode GitHub Pages artifact.

.DESCRIPTION
Restores the pinned .NET tools, builds the DocFX documentation and Blazor playground, and assembles the root
chooser, documentation, and playground under docs/_site.

.EXAMPLE
./docs/build-pages.ps1

Builds the complete website from the repository root.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory)] [string] $Command,
        [Parameter(Mandatory)] [string[]] $Arguments
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Command exited with code $LASTEXITCODE."
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$site = Join-Path $repoRoot 'docs/_site'
$landing = Join-Path $repoRoot 'docs/public/landing'
$playgroundOutput = Join-Path $repoRoot 'artifacts/lolcode-web'
$playground = Join-Path $playgroundOutput 'wwwroot'

Push-Location $repoRoot
try {
    # Restore pinned tools and build both published applications.
    Invoke-NativeCommand -Command 'dotnet' -Arguments @('tool', 'restore')

    foreach ($path in @($site, $playgroundOutput)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }

    Invoke-NativeCommand -Command 'dotnet' -Arguments @(
        'tool', 'run', 'docfx', 'docs/public/docfx.json', '--warningsAsErrors'
    )
    Invoke-NativeCommand -Command 'dotnet' -Arguments @(
        'publish', 'src/Lolcode.Web/Lolcode.Web.csproj', '--configuration', 'Release', '--output', $playgroundOutput
    )

    # Assemble the root chooser, DocFX output, and playground into one Pages artifact.
    $playgroundIndex = Join-Path $playground 'index.html'
    $expectedBase = '<base href="/dotnet-lolcode/playground/" />'
    if (-not (Get-Content -LiteralPath $playgroundIndex -Raw).Contains($expectedBase)) {
        throw 'The playground base path is not configured for GitHub Pages.'
    }

    New-Item -ItemType Directory -Path $site -Force | Out-Null
    Get-ChildItem -LiteralPath $landing -Force | Copy-Item -Destination $site -Recurse -Force

    $sitePlayground = Join-Path $site 'playground'
    New-Item -ItemType Directory -Path $sitePlayground -Force | Out-Null
    Get-ChildItem -LiteralPath $playground -Force | Copy-Item -Destination $sitePlayground -Recurse -Force
    New-Item -ItemType File -Path (Join-Path $site '.nojekyll') -Force | Out-Null

    # Verify the deployment contract and ensure private development notes stay private.
    $requiredFiles = @(
        'index.html',
        'main.css',
        '404.html',
        'docs/index.html',
        'docs/api/Lolcode.html',
        'playground/_framework/blazor.webassembly.js'
    )

    foreach ($relativePath in $requiredFiles) {
        if (-not (Test-Path -LiteralPath (Join-Path $site $relativePath) -PathType Leaf)) {
            throw "Required Pages artifact is missing: $relativePath"
        }
    }

    if (Test-Path -LiteralPath (Join-Path $site 'docs/dev')) {
        throw 'Repository-only documentation was included in the public site.'
    }

    Write-Host "Pages artifact built at $site"
}
finally {
    Pop-Location
}
