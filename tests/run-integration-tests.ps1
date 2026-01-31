#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs the Dottie integration tests in a Docker container.

.DESCRIPTION
    This script builds the Dottie CLI for Linux, creates a Docker test image,
    and runs the integration test scenarios.

.PARAMETER NoBuild
    Skip the dotnet publish step (use existing binary in publish/linux-x64/).

.PARAMETER NoImageBuild
    Skip the Docker image build step (use existing dottie-integration-test image).

.PARAMETER Verbose
    Show detailed output from all commands.

.EXAMPLE
    ./run-integration-tests.ps1
    # Full build and test

.EXAMPLE
    ./run-integration-tests.ps1 -NoBuild
    # Skip dotnet publish, just rebuild Docker image and run tests

.EXAMPLE
    ./run-integration-tests.ps1 -NoBuild -NoImageBuild
    # Just run tests with existing image
#>

[CmdletBinding()]
param(
    [switch]$NoBuild,
    [switch]$NoImageBuild
)

$ErrorActionPreference = 'Stop'

# Find repo root (where Dottie.slnx is)
$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName
Push-Location $repoRoot

try {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Dottie Integration Test Runner" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    # Step 1: Build the Linux binary
    if (-not $NoBuild) {
        Write-Host "[1/3] Building Dottie CLI for Linux..." -ForegroundColor Yellow
        
        $publishArgs = @(
            "publish"
            "src/Dottie.Cli/Dottie.Cli.csproj"
            "--configuration", "Release"
            "--runtime", "linux-x64"
            "--self-contained"
            "--output", "./publish/linux-x64"
            "/p:PublishSingleFile=true"
        )
        
        & dotnet @publishArgs
        
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed with exit code $LASTEXITCODE"
        }
        
        Write-Host "  ✓ Build complete" -ForegroundColor Green
    }
    else {
        Write-Host "[1/3] Skipping build (using existing binary)" -ForegroundColor DarkGray
        
        if (-not (Test-Path "./publish/linux-x64/dottie")) {
            throw "No binary found at ./publish/linux-x64/dottie. Run without -NoBuild first."
        }
    }
    Write-Host ""

    # Step 2: Build Docker image
    if (-not $NoImageBuild) {
        Write-Host "[2/3] Building Docker test image..." -ForegroundColor Yellow
        
        & docker build -t dottie-integration-test -f tests/integration/Dockerfile .
        
        if ($LASTEXITCODE -ne 0) {
            throw "docker build failed with exit code $LASTEXITCODE"
        }
        
        Write-Host "  ✓ Image built" -ForegroundColor Green
    }
    else {
        Write-Host "[2/3] Skipping image build (using existing image)" -ForegroundColor DarkGray
        
        $imageExists = docker images -q dottie-integration-test 2>$null
        if (-not $imageExists) {
            throw "No Docker image found. Run without -NoImageBuild first."
        }
    }
    Write-Host ""

    # Step 3: Run tests
    Write-Host "[3/3] Running integration tests..." -ForegroundColor Yellow
    Write-Host ""
    
    & docker run --rm dottie-integration-test
    $testExitCode = $LASTEXITCODE
    
    Write-Host ""
    
    if ($testExitCode -eq 0) {
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "  ✓ All integration tests passed!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
    }
    else {
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "  ✗ Integration tests failed" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Red
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
