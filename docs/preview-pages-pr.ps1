#!/usr/bin/env pwsh

<#
.SYNOPSIS
Downloads, tests, and hosts the GitHub Pages artifact from a pull request.

.DESCRIPTION
Finds the successful Pages workflow for the pull request's current head commit, downloads the
dotnet-lolcode-pages artifact, validates it with Playwright, and leaves it running beneath /dotnet-lolcode/
until Ctrl+C is pressed.

.PARAMETER PullRequestNumber
The pull request number whose Pages artifact should be downloaded.

.PARAMETER Port
The localhost port to use. The default value of 0 selects an available port automatically.

.EXAMPLE
./docs/preview-pages-pr.ps1 -PullRequestNumber 15

Downloads and tests the artifact for pull request 15, then hosts it on an available port.

.EXAMPLE
./docs/preview-pages-pr.ps1 -PullRequestNumber 15 -Port 8090

Downloads and tests the artifact for pull request 15, then hosts it on port 8090.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidateRange(1, [int]::MaxValue)]
    [int] $PullRequestNumber,

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
Push-Location $repoRoot
try {
    if ($null -eq (Get-Command gh -ErrorAction SilentlyContinue)) {
        throw 'GitHub CLI (gh) is required to download a pull request artifact.'
    }

    # Locate a successful Pages workflow for the pull request's current head commit.
    # This includes manual dispatches, which are useful when a retargeted PR does not
    # automatically enqueue a pull_request workflow from its new base branch.
    & dotnet tool restore
    Assert-LastExitCode -Command 'dotnet tool restore'

    if ($Port -eq 0) {
        $Port = Get-AvailablePort
    }

    $headSha = (& gh pr view $PullRequestNumber --json headRefOid --jq '.headRefOid').Trim()
    Assert-LastExitCode -Command 'gh pr view'

    $runArguments = @(
        'run', 'list', '--workflow', 'pages.yml', '--commit', $headSha,
        '--status', 'completed', '--limit', '20', '--json', 'databaseId,conclusion',
        '--jq', '[.[] | select(.conclusion == "success")][0].databaseId'
    )
    $runId = (& gh @runArguments).Trim()
    Assert-LastExitCode -Command 'gh run list'

    if ([string]::IsNullOrWhiteSpace($runId) -or $runId -eq 'null') {
        throw "No successful Pages workflow run found for PR #$PullRequestNumber at $headSha.`n" +
            "Check it with: gh pr checks $PullRequestNumber"
    }

    # Download the exact CI artifact, then serve it at the production path base.
    $previewDirectory = Join-Path ([IO.Path]::GetTempPath()) (
        "dotnet-lolcode-pr-$PullRequestNumber-$([Guid]::NewGuid().ToString('N'))"
    )
    $artifactDirectory = Join-Path $previewDirectory 'artifact'
    $screenshotDirectory = Join-Path $previewDirectory 'screenshots'
    New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null

    & gh run download $runId --name dotnet-lolcode-pages --dir $artifactDirectory
    Assert-LastExitCode -Command 'gh run download'

    $serverJob = Start-PagesServer -WorkingDirectory $repoRoot -Directory $artifactDirectory -Port $Port
    try {
        $baseUrl = "http://127.0.0.1:$Port/dotnet-lolcode/"
        Wait-ForServer -Url $baseUrl -Job $serverJob

        # Validate the downloaded artifact before leaving the preview server running.
        $previousSiteUrl = $env:LOLCODE_SITE_URL
        $previousScreenshotDirectory = $env:LOLCODE_SCREENSHOT_DIR
        try {
            $env:LOLCODE_SITE_URL = $baseUrl
            $env:LOLCODE_SCREENSHOT_DIR = $screenshotDirectory
            & dotnet test tests/Lolcode.Docs.Tests/Lolcode.Docs.Tests.csproj --verbosity normal
            Assert-LastExitCode -Command 'dotnet test'
        }
        finally {
            $env:LOLCODE_SITE_URL = $previousSiteUrl
            $env:LOLCODE_SCREENSHOT_DIR = $previousScreenshotDirectory
        }

        Write-Host "`nPR #$PullRequestNumber Pages preview is ready:"
        Write-Host $baseUrl
        Write-Host "Artifact: $artifactDirectory"
        Write-Host "Screenshots: $screenshotDirectory"
        Write-Host 'Press Ctrl+C to stop the server.'

        Wait-Job -Job $serverJob | Receive-Job
    }
    finally {
        # Ctrl+C and test failures must not leave dotnet-serve running.
        Stop-Job -Job $serverJob -ErrorAction SilentlyContinue
        Remove-Job -Job $serverJob -Force -ErrorAction SilentlyContinue
    }
}
finally {
    Pop-Location
}
