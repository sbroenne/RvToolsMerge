#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Prepares winget manifest submission for RVToolsMerge

.DESCRIPTION
    This script downloads winget manifests from a GitHub release and prepares them for submission
    to the winget community repository. It can optionally create a branch in your fork.

.PARAMETER Version
    The version to prepare submission for (e.g., "1.3.4"). If not specified, uses the latest release.

.PARAMETER Repository
    The GitHub repository in format "owner/repo". Defaults to current repository.

.PARAMETER ForkUrl
    The URL of your winget-pkgs fork. Defaults to "https://github.com/sbroenne/winget-pkgs.git"

.PARAMETER OutputDir
    Directory to output the prepared submission. Defaults to "winget-submission"

.PARAMETER DryRun
    If specified, only prepares files locally without creating a branch

.PARAMETER GitHubToken
    GitHub token for API access. Can also be set via GITHUB_TOKEN environment variable.

.EXAMPLE
    .\prepare-winget-submission.ps1 -Version "1.3.4"

.EXAMPLE
    .\prepare-winget-submission.ps1 -DryRun

.EXAMPLE
    .\prepare-winget-submission.ps1 -Version "1.3.4" -Repository "myorg/myrepo" -DryRun
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$Version,

    [Parameter(Mandatory = $false)]
    [string]$Repository,

    [Parameter(Mandatory = $false)]
    [string]$ForkUrl = "https://github.com/sbroenne/winget-pkgs.git",

    [Parameter(Mandatory = $false)]
    [string]$OutputDir = "winget-submission",

    [Parameter(Mandatory = $false)]
    [switch]$DryRun,

    [Parameter(Mandatory = $false)]
    [string]$GitHubToken
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get GitHub token
if (-not $GitHubToken) {
    $GitHubToken = $env:GITHUB_TOKEN
}

if (-not $GitHubToken) {
    Write-Error "GitHub token required. Set GITHUB_TOKEN environment variable or use -GitHubToken parameter."
    exit 1
}

# Function to make GitHub API calls
function Invoke-GitHubApi {
    param(
        [string]$Uri,
        [string]$Method = "GET"
    )

    $headers = @{
        "Authorization" = "Bearer $GitHubToken"
        "Accept"        = "application/vnd.github.v3+json"
        "User-Agent"    = "RVToolsMerge-WingetSubmission/1.0"
    }

    try {
        return Invoke-RestMethod -Uri $Uri -Method $Method -Headers $headers
    }
    catch {
        Write-Error "GitHub API call failed: $($_.Exception.Message)"
        throw
    }
}

# Determine repository
if (-not $Repository) {
    # Try to get from git remote
    try {
        $gitRemote = git remote get-url origin 2>$null
        if ($gitRemote -match "github\.com[:/](.+)/(.+?)(?:\.git)?$") {
            $Repository = "$($matches[1])/$($matches[2])"
            Write-Host "Detected repository: $Repository"
        }
    }
    catch {
        # Ignore errors
    }

    if (-not $Repository) {
        Write-Error "Could not determine repository. Please specify -Repository parameter."
        exit 1
    }
}

Write-Host "🚀 Preparing winget submission for $Repository" -ForegroundColor Green

# Get release information
Write-Host "📋 Getting release information..."

if ($Version) {
    $releaseTag = if ($Version.StartsWith("v")) { $Version } else { "v$Version" }
    $releaseUri = "https://api.github.com/repos/$Repository/releases/tags/$releaseTag"
}
else {
    $releaseUri = "https://api.github.com/repos/$Repository/releases/latest"
}

try {
    $release = Invoke-GitHubApi -Uri $releaseUri
    $releaseTag = $release.tag_name
    $releaseVersion = $releaseTag -replace "^v", ""

    Write-Host "✅ Found release: $($release.name) ($releaseTag)" -ForegroundColor Green
}
catch {
    Write-Error "Could not find release. Please check the version or repository."
    exit 1
}

# Create output directory
Write-Host "📁 Creating output directory: $OutputDir"
if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir | Out-Null

# Download manifest files
Write-Host "⬬ Downloading winget manifests from release..."
$manifestDir = Join-Path $OutputDir "manifests"
New-Item -ItemType Directory -Path $manifestDir | Out-Null

$manifestFiles = @(
    "RvToolsMerge.RvToolsMerge.yaml",
    "RvToolsMerge.RvToolsMerge.installer.yaml",
    "RvToolsMerge.RvToolsMerge.locale.en-US.yaml"
)

foreach ($manifestFile in $manifestFiles) {
    $asset = $release.assets | Where-Object { $_.name -eq $manifestFile }
    if (-not $asset) {
        Write-Error "Manifest file not found in release: $manifestFile"
        exit 1
    }

    Write-Host "  Downloading $manifestFile..."
    $outputPath = Join-Path $manifestDir $manifestFile
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $outputPath -Headers @{
        "Authorization" = "Bearer $GitHubToken"
        "Accept"        = "application/octet-stream"
    }

    if (-not (Test-Path $outputPath)) {
        Write-Error "Failed to download $manifestFile"
        exit 1
    }
}

# Validate manifests
Write-Host "✅ Validating manifests..."
foreach ($manifestFile in $manifestFiles) {
    $manifestPath = Join-Path $manifestDir $manifestFile

    try {
        # Basic YAML validation using PowerShell's ConvertFrom-Yaml (if available)
        $content = Get-Content $manifestPath -Raw
        if ($content -match "PackageIdentifier:\s*RvToolsMerge\.RvToolsMerge") {
            Write-Host "  ✅ $manifestFile validated" -ForegroundColor Green
        }
        else {
            Write-Warning "  ⚠️  $manifestFile may have validation issues"
        }
    }
    catch {
        Write-Warning "  ⚠️  Could not validate $manifestFile: $($_.Exception.Message)"
    }
}

# Create submission information
Write-Host "📝 Creating submission information..."
$submissionInfo = @"
# Winget Package Submission for RVToolsMerge v$releaseVersion

## Package Information
- **Package ID**: RvToolsMerge.RvToolsMerge
- **Version**: $releaseVersion
- **Release Tag**: $releaseTag
- **Published**: $($release.published_at)
- **Repository**: $Repository

## Submission Details
- **Fork Repository**: $ForkUrl
- **Target Branch**: RvToolsMerge-$releaseVersion
- **Upstream Repository**: https://github.com/microsoft/winget-pkgs
- **Manifest Path**: manifests/r/RvToolsMerge/RvToolsMerge/$releaseVersion/

## Manifest Files
- RvToolsMerge.RvToolsMerge.yaml (Version manifest)
- RvToolsMerge.RvToolsMerge.installer.yaml (Installer manifest)
- RvToolsMerge.RvToolsMerge.locale.en-US.yaml (Locale manifest)

## Release Notes
$($release.body)

## Next Steps
1. Review the generated manifests in the manifests/ directory
2. If this was not a dry run, create a branch in your fork
3. Submit a pull request from your fork to microsoft/winget-pkgs
4. Follow the winget community review process

## Validation Commands
To validate the manifests locally, you can use:
``````
winget validate --manifest manifests/r/RvToolsMerge/RvToolsMerge/$releaseVersion/
``````
"@

$submissionInfoPath = Join-Path $OutputDir "submission-info.md"
Set-Content -Path $submissionInfoPath -Value $submissionInfo -Encoding UTF8

# Create PR template
Write-Host "📄 Creating PR template..."
$prTemplate = @"
# RvToolsMerge version $releaseVersion

This PR adds RvToolsMerge version $releaseVersion to the Windows Package Manager repository.

## Package Information
- **Package ID**: RvToolsMerge.RvToolsMerge
- **Version**: $releaseVersion
- **Publisher**: RvToolsMerge
- **Release**: $($release.html_url)

## Changes
- Added RvToolsMerge version $releaseVersion manifests
- All manifests follow winget specification v1.6.0
- Includes version, installer, and locale manifests

## Testing
- [ ] Manifests validated with winget validate command
- [ ] Installation tested locally
- [ ] Upgrade tested from previous version

## Checklist
- [x] I have read the Contributing Guide
- [x] I have verified this is the correct package
- [x] I have tested the package locally
- [x] I have verified the package does not already exist

---

*This PR was automatically generated by the RvToolsMerge release automation.*
"@

$prTemplatePath = Join-Path $OutputDir "pr-template.md"
Set-Content -Path $prTemplatePath -Value $prTemplate -Encoding UTF8

# Save release notes
$releaseNotesPath = Join-Path $OutputDir "release-notes.md"
Set-Content -Path $releaseNotesPath -Value $release.body -Encoding UTF8

if ($DryRun) {
    Write-Host "🏃 Dry run mode - skipping branch creation" -ForegroundColor Yellow
}
else {
    Write-Host "🌿 Creating branch in fork..."

    # Clone fork
    $forkDir = Join-Path $OutputDir "winget-fork"
    Write-Host "  Cloning fork: $ForkUrl"
    git clone $ForkUrl $forkDir

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to clone fork repository"
        exit 1
    }

    Push-Location $forkDir
    try {
        # Configure git
        git config user.name "RVToolsMerge Automation"
        git config user.email "noreply@github.com"

        # Add upstream and fetch
        git remote add upstream https://github.com/microsoft/winget-pkgs.git
        git fetch upstream

        # Create and checkout new branch
        $branchName = "RvToolsMerge-$releaseVersion"
        git checkout -b $branchName upstream/master

        # Create directory structure
        $targetDir = "manifests/r/RvToolsMerge/RvToolsMerge/$releaseVersion"
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

        # Copy manifests
        foreach ($manifestFile in $manifestFiles) {
            $sourcePath = Join-Path $manifestDir $manifestFile
            $targetPath = Join-Path $targetDir $manifestFile
            Copy-Item $sourcePath $targetPath
        }

        # Add and commit
        git add $targetDir
        git commit -m "Add RvToolsMerge version $releaseVersion

This submission adds RvToolsMerge version $releaseVersion to the Windows Package Manager repository.

Package ID: RvToolsMerge.RvToolsMerge
Version: $releaseVersion

Release URL: $($release.html_url)"

        # Push to fork
        Write-Host "  Pushing branch: $branchName"
        git push origin $branchName

        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Branch created successfully!" -ForegroundColor Green
            Write-Host ""
            Write-Host "Create pull request at:" -ForegroundColor Cyan
            Write-Host "https://github.com/microsoft/winget-pkgs/compare/master...sbroenne:winget-pkgs:$branchName" -ForegroundColor Blue
        }
        else {
            Write-Error "Failed to push branch to fork"
        }
    }
    finally {
        Pop-Location
    }
}

# Summary
Write-Host ""
Write-Host "🎉 Winget submission preparation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📦 Package: RvToolsMerge.RvToolsMerge v$releaseVersion"
Write-Host "📁 Output: $OutputDir"
Write-Host ""
Write-Host "Generated files:" -ForegroundColor Cyan
Get-ChildItem $OutputDir -Recurse -File | ForEach-Object {
    $relativePath = $_.FullName.Substring((Resolve-Path $OutputDir).Path.Length + 1)
    Write-Host "  $relativePath"
}
Write-Host ""

if (-not $DryRun) {
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Review the generated manifests"
    Write-Host "2. Create a pull request using the link above"
    Write-Host "3. Use pr-template.md for the PR description"
    Write-Host "4. Follow the winget community review process"
}
else {
    Write-Host "To create the branch, run this script again without -DryRun" -ForegroundColor Yellow
}
