<#
.SYNOPSIS
    Configures branch protection rules for the main branch using GitHub CLI.

.DESCRIPTION
    This script configures branch protection rules for the main branch to require:
    - CI status checks to pass before merging
    - Branch to be up-to-date before merging
    
    The script is idempotent and safe to run multiple times.

.PARAMETER Owner
    Repository owner (username or organization). Auto-detected from git remote if not specified.

.PARAMETER Repo
    Repository name. Auto-detected from git remote if not specified.

.PARAMETER Branch
    Branch to protect. Defaults to "main".

.PARAMETER WhatIf
    Preview changes without applying them.

.EXAMPLE
    ./Set-BranchProtection.ps1
    Configures branch protection for the current repository.

.EXAMPLE
    ./Set-BranchProtection.ps1 -Owner "ryandanthony" -Repo "dottie" -WhatIf
    Preview branch protection changes for a specific repository.

.NOTES
    Requires:
    - gh CLI installed and authenticated
    - Admin permissions on the repository
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter()]
    [string]$Owner,

    [Parameter()]
    [string]$Repo,

    [Parameter()]
    [string]$Branch = "main"
)

$ErrorActionPreference = "Stop"

# Auto-detect owner and repo from git remote if not specified
if (-not $Owner -or -not $Repo) {
    Write-Host "Detecting repository from git remote..." -ForegroundColor Cyan
    
    $remoteUrl = git remote get-url origin 2>$null
    if (-not $remoteUrl) {
        Write-Error "Could not detect repository. Please specify -Owner and -Repo parameters."
        exit 3
    }
    
    # Parse GitHub URL (supports both HTTPS and SSH formats)
    if ($remoteUrl -match "github\.com[:/]([^/]+)/([^/.]+)") {
        $Owner = $Matches[1]
        $Repo = $Matches[2] -replace "\.git$", ""
        Write-Host "  Detected: $Owner/$Repo" -ForegroundColor Green
    }
    else {
        Write-Error "Could not parse repository from remote URL: $remoteUrl"
        exit 3
    }
}

# Check gh CLI is available
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "gh CLI is not installed. Please install it from https://cli.github.com/"
    exit 1
}

# Check gh CLI is authenticated
$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "gh CLI is not authenticated. Please run 'gh auth login' first."
    exit 1
}

# Build protection rules payload
$protectionRules = @{
    required_status_checks = @{
        strict = $true
        contexts = @("build")
    }
    enforce_admins = $false
    required_pull_request_reviews = $null
    restrictions = $null
    allow_force_pushes = $false
    allow_deletions = $false
} | ConvertTo-Json -Depth 10 -Compress

Write-Host ""
Write-Host "Branch Protection Configuration" -ForegroundColor Cyan
Write-Host "  Repository: $Owner/$Repo"
Write-Host "  Branch: $Branch"
Write-Host "  Required status check: build"
Write-Host "  Require up-to-date: Yes"
Write-Host "  Enforce admins: No"
Write-Host ""

if ($WhatIf -or $PSCmdlet.ShouldProcess("$Owner/$Repo branch '$Branch'", "Configure branch protection")) {
    if ($WhatIf) {
        Write-Host "What if: Would configure branch protection with the above settings" -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host "Applying branch protection rules..." -ForegroundColor Cyan
    
    try {
        $result = $protectionRules | gh api `
            --method PUT `
            "/repos/$Owner/$Repo/branches/$Branch/protection" `
            --input - 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "✓ Branch protection configured for $Branch" -ForegroundColor Green
            Write-Host "  - Required status check: build"
            Write-Host "  - Require up-to-date: Yes"
            Write-Host "  - Enforce admins: No"
            exit 0
        }
        else {
            throw $result
        }
    }
    catch {
        $errorMessage = $_.Exception.Message
        
        Write-Host ""
        Write-Host "✗ Failed to configure branch protection" -ForegroundColor Red
        Write-Host "  Error: $errorMessage" -ForegroundColor Red
        
        if ($errorMessage -match "Resource not accessible") {
            Write-Host "  Hint: Ensure you have admin permissions on the repository" -ForegroundColor Yellow
            exit 2
        }
        elseif ($errorMessage -match "Not Found") {
            Write-Host "  Hint: Repository or branch not found" -ForegroundColor Yellow
            exit 3
        }
        else {
            exit 4
        }
    }
}
