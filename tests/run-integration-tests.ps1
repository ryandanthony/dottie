#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs the Dottie integration tests in a Docker container.

.DESCRIPTION
    This script builds the Dottie CLI for Linux, creates a Docker test image,
    and runs the integration test scenarios. Supports filtering tests and controlling
    verbosity for automated runs (e.g., AI agents).

.PARAMETER NoBuild
    Skip the dotnet publish step (use existing binary in publish/linux-x64/).

.PARAMETER NoImageBuild
    Skip the Docker image build step (use existing dottie-integration-test image).

.PARAMETER Verbosity
    Control output verbosity. Options: Quiet, Normal (default), Verbose.
    - Quiet: Minimal output, only failures and summary
    - Normal: Standard output with progress indicators
    - Verbose: Detailed output including command parameters and full Docker logs

.PARAMETER TestName
    Run only a specific test scenario (e.g., 'basic-symlinks', 'install-apt-packages').
    When specified, only that scenario will be executed.

.EXAMPLE
    ./run-integration-tests.ps1
    # Full build and test with normal verbosity

.EXAMPLE
    ./run-integration-tests.ps1 -NoBuild -TestName basic-symlinks
    # Run only basic-symlinks scenario, skip build

.EXAMPLE
    ./run-integration-tests.ps1 -Verbosity Quiet
    # Minimal output, good for CI/CD pipelines

.EXAMPLE
    ./run-integration-tests.ps1 -Verbosity Verbose -TestName install-apt-packages
    # Detailed output for single scenario
#>

[CmdletBinding()]
param(
    [switch]$NoBuild,
    [switch]$NoImageBuild,
    [ValidateSet('Quiet', 'Normal', 'Verbose')]
    [string]$Verbosity = 'Normal',
    [string]$TestName
)

$ErrorActionPreference = 'Stop'

# Verbosity output helper
function Write-Log {
    param(
        [string]$Message,
        [string]$Level = 'Info',
        [ConsoleColor]$Color = [ConsoleColor]::White
    )

    $levels = @{
        'Info' = 0
        'Warning' = 1
        'Error' = 2
    }

    $verbosityLevels = @{
        'Quiet' = 2
        'Normal' = 0
        'Verbose' = -1
    }

    if ($levels[$Level] -ge $verbosityLevels[$Verbosity]) {
        Write-Host $Message -ForegroundColor $Color
    }
}

# Find repo root (where Dottie.slnx is)
$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName
Push-Location $repoRoot

try {
    if ($Verbosity -ne 'Quiet') {
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "Dottie Integration Test Runner" -ForegroundColor Cyan
        Write-Host "========================================" -ForegroundColor Cyan
        if ($TestName) {
            Write-Host "Test: $TestName" -ForegroundColor Cyan
        }
        if ($Verbosity -eq 'Verbose') {
            Write-Host "Verbosity: Verbose" -ForegroundColor Cyan
        }
        Write-Host ""
    }

    # Step 1: Build the Linux binary
    if (-not $NoBuild) {
        Write-Log "[1/3] Building Dottie CLI for Linux..." -Color Yellow

        $publishArgs = @(
            "publish"
            "src/Dottie.Cli/Dottie.Cli.csproj"
            "--configuration", "Release"
            "--runtime", "linux-x64"
            "--self-contained"
            "--output", "./publish/linux-x64"
            "/p:PublishSingleFile=true"
        )

        if ($Verbosity -eq 'Verbose') {
            Write-Host "  Command: dotnet $(($publishArgs) -join ' ')" -ForegroundColor DarkGray
        }

        & dotnet @publishArgs 2>&1 | ForEach-Object {
            if ($Verbosity -eq 'Verbose') {
                Write-Host $_
            }
        }

        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed with exit code $LASTEXITCODE"
        }

        Write-Log "  ✓ Build complete" -Color Green
    }
    else {
        Write-Log "[1/3] Skipping build (using existing binary)" -Color DarkGray

        if (-not (Test-Path "./publish/linux-x64/dottie")) {
            throw "No binary found at ./publish/linux-x64/dottie. Run without -NoBuild first."
        }
    }
    Write-Log ""

    # Step 2: Build Docker image
    if (-not $NoImageBuild) {
        Write-Log "[2/3] Building Docker test image..." -Color Yellow

        if ($Verbosity -eq 'Verbose') {
            Write-Host "  Command: docker build -t dottie-integration-test -f tests/integration/Dockerfile ." -ForegroundColor DarkGray
        }

        & docker build -t dottie-integration-test -f tests/integration/Dockerfile . 2>&1 | ForEach-Object {
            if ($Verbosity -eq 'Verbose') {
                Write-Host $_
            }
        }

        if ($LASTEXITCODE -ne 0) {
            throw "docker build failed with exit code $LASTEXITCODE"
        }

        Write-Log "  ✓ Image built" -Color Green
    }
    else {
        Write-Log "[2/3] Skipping image build (using existing image)" -Color DarkGray

        $imageExists = docker images -q dottie-integration-test 2>$null
        if (-not $imageExists) {
            throw "No Docker image found. Run without -NoImageBuild first."
        }
    }
    Write-Log ""

    # Step 3: Run tests
    Write-Log "[3/3] Running integration tests..." -Color Yellow
    if ($Verbosity -eq 'Verbose') {
        if ($TestName) {
            Write-Host "  Running specific test: $TestName" -ForegroundColor DarkGray
        }
        Write-Host "  Command: docker run --rm --env TEST_NAME=$TestName --env VERBOSITY=$Verbosity dottie-integration-test" -ForegroundColor DarkGray
    }
    Write-Log ""

    $dockerArgs = @('run', '--rm')
    if ($TestName) {
        $dockerArgs += @('--env', "TEST_NAME=$TestName")
    }
    if ($Verbosity -ne 'Normal') {
        $dockerArgs += @('--env', "VERBOSITY=$Verbosity")
    }
    $dockerArgs += 'dottie-integration-test'

    & docker @dockerArgs
    $testExitCode = $LASTEXITCODE

    Write-Log ""

    if ($testExitCode -eq 0) {
        if ($Verbosity -ne 'Quiet') {
            Write-Host "========================================" -ForegroundColor Green
            Write-Host "  ✓ All integration tests passed!" -ForegroundColor Green
            Write-Host "========================================" -ForegroundColor Green
        }
    }
    else {
        if ($Verbosity -ne 'Quiet') {
            Write-Host "========================================" -ForegroundColor Red
            Write-Host "  ✗ Integration tests failed" -ForegroundColor Red
            Write-Host "========================================" -ForegroundColor Red
        }
        exit $testExitCode
    }
}
catch {
    Write-Host ""
    Write-Host "ERROR: $_" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
