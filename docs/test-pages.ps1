#!/usr/bin/env pwsh

<#
.SYNOPSIS
Hosts and tests a complete dotnet-lolcode Pages artifact.

.DESCRIPTION
Uses the pinned dotnet-serve tool to host the site beneath /dotnet-lolcode/, then runs the desktop and mobile
Playwright tests and writes screenshots under artifacts/playwright.

.PARAMETER Site
The Pages artifact directory to host. Defaults to docs/_site.

.PARAMETER Port
The localhost port to use. The default value of 0 selects an available port automatically.

.EXAMPLE
./docs/test-pages.ps1

Tests the artifact in docs/_site on an available port.

.EXAMPLE
./docs/test-pages.ps1 -Site docs/_site -Port 8090

Tests a specific artifact directory on port 8090.
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)] [string] $Site,
    [Parameter(Position = 1)] [ValidateRange(0, 65535)] [int] $Port = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-LastExitCode {
    param([Parameter(Mandatory)] [string] $Command)

    if ($LASTEXITCODE -ne 0) {
        throw "$Command exited with code $LASTEXITCODE."
    }
}

function Get-AvailablePort {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start()
    try {
        return ([Net.IPEndPoint] $listener.LocalEndpoint).Port
    }
    finally {
        $listener.Stop()
    }
}

function Start-PagesServer {
    param(
        [Parameter(Mandatory)] [string] $WorkingDirectory,
        [Parameter(Mandatory)] [string] $Directory,
        [Parameter(Mandatory)] [int] $Port
    )

    Start-Job -ScriptBlock {
        param($WorkingDirectory, $Directory, $Port)

        Set-Location $WorkingDirectory
        & dotnet tool run dotnet-serve -- --directory $Directory --port $Port --address 127.0.0.1 `
            --path-base /dotnet-lolcode --quiet
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet-serve exited with code $LASTEXITCODE."
        }
    } -ArgumentList @($WorkingDirectory, $Directory, $Port)
}

function Wait-ForServer {
    param(
        [Parameter(Mandatory)] [string] $Url,
        [Parameter(Mandatory)] [System.Management.Automation.Job] $Job
    )

    foreach ($attempt in 1..30) {
        try {
            Invoke-WebRequest -Uri $Url -TimeoutSec 2 | Out-Null
            return
        }
        catch {
            if ($Job.State -in @('Completed', 'Failed', 'Stopped')) {
                Receive-Job -Job $Job
                throw 'dotnet-serve stopped before it became ready.'
            }
        }

        Start-Sleep -Seconds 1
    }

    throw "dotnet-serve did not become ready at $Url."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$sitePath = if ([string]::IsNullOrWhiteSpace($Site)) {
    Join-Path $repoRoot 'docs/_site'
}
else {
    [IO.Path]::GetFullPath($Site)
}

if (-not (Test-Path -LiteralPath (Join-Path $sitePath 'index.html') -PathType Leaf)) {
    throw "Pages artifact not found at $sitePath. Run docs/build-pages.ps1 first."
}

if ($Port -eq 0) {
    $Port = Get-AvailablePort
}

Push-Location $repoRoot
$serverJob = $null
try {
    # Restore dotnet-serve and host the artifact at the same path base used by GitHub Pages.
    & dotnet tool restore
    Assert-LastExitCode -Command 'dotnet tool restore'

    $serverJob = Start-PagesServer -WorkingDirectory $repoRoot -Directory $sitePath -Port $Port
    $baseUrl = "http://127.0.0.1:$Port/dotnet-lolcode/"
    Wait-ForServer -Url $baseUrl -Job $serverJob

    # Run the browser suite without leaking test configuration into the caller's environment.
    $previousSiteUrl = $env:LOLCODE_SITE_URL
    $previousScreenshotDirectory = $env:LOLCODE_SCREENSHOT_DIR
    try {
        $env:LOLCODE_SITE_URL = $baseUrl
        $env:LOLCODE_SCREENSHOT_DIR = Join-Path $repoRoot 'artifacts/playwright'
        & dotnet test tests/Lolcode.Docs.Tests/Lolcode.Docs.Tests.csproj --verbosity normal
        Assert-LastExitCode -Command 'dotnet test'
    }
    finally {
        $env:LOLCODE_SITE_URL = $previousSiteUrl
        $env:LOLCODE_SCREENSHOT_DIR = $previousScreenshotDirectory
    }

    Write-Host "Playwright screenshots: $(Join-Path $repoRoot 'artifacts/playwright')"
}
finally {
    # The server is temporary even when a test fails or the script is interrupted.
    if ($null -ne $serverJob) {
        Stop-Job -Job $serverJob -ErrorAction SilentlyContinue
        Remove-Job -Job $serverJob -Force -ErrorAction SilentlyContinue
    }

    Pop-Location
}
