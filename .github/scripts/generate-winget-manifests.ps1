#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Generate and validate winget manifest files from templates

.DESCRIPTION
    This script generates winget manifest files from templates using provided version and MSI file information.
    The script performs mandatory winget validation - it will fail if winget is not available or validation fails.

.PARAMETER Version
    The version number for the package (e.g., "1.0.0")

.PARAMETER X64MsiPath
    Path to the x64 MSI installer file

.PARAMETER Arm64MsiPath
    Path to the ARM64 MSI installer file

.PARAMETER OutputDir
    Directory where the generated manifest files will be saved

.PARAMETER ReleaseNotes
    Release notes for this version (optional)

.EXAMPLE
    .\generate-winget-manifests.ps1 -Version "1.0.0" -X64MsiPath ".\x64.msi" -Arm64MsiPath ".\arm64.msi" -OutputDir ".\manifests"

.NOTES
    - Requires winget to be installed and available in PATH
    - All generated manifests are automatically validated with 'winget validate'
    - Script will exit with error code 1 if validation fails
#>

# Generate Winget Manifests Script
# This script generates winget manifest files from templates and ALWAYS validates them.
# Winget validation is mandatory - the script will fail if winget is not available or validation fails.

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$X64MsiPath,

    [Parameter(Mandatory = $true)]
    [string]$Arm64MsiPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputDir,
    [Parameter(Mandatory = $false)]
    [string]$ReleaseNotes = "See the release notes for details about this version."
)

# Ensure output directory exists
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Function to calculate SHA256 hash
function Get-FileSha256Hash {
    param([string]$FilePath)

    if (-not (Test-Path $FilePath)) {
        Write-Error "File not found: $FilePath"
        return $null
    }

    $hash = Get-FileHash -Path $FilePath -Algorithm SHA256
    return $hash.Hash
}

# Calculate SHA256 hashes
Write-Host "Calculating SHA256 hashes..."
$x64Hash = Get-FileSha256Hash -FilePath $X64MsiPath
$arm64Hash = Get-FileSha256Hash -FilePath $Arm64MsiPath

if (-not $x64Hash -or -not $arm64Hash) {
    Write-Error "Failed to calculate SHA256 hashes"
    exit 1
}

Write-Host "x64 MSI SHA256: $x64Hash"
Write-Host "ARM64 MSI SHA256: $arm64Hash"

# Template directory
$templateDir = Join-Path (Split-Path $PSScriptRoot -Parent) "winget-templates"

# Function to process template file
function Invoke-TemplateProcessing {
    param(
        [string]$TemplatePath,
        [string]$OutputPath,
        [hashtable]$Replacements
    )

    if (-not (Test-Path $TemplatePath)) {
        Write-Error "Template file not found: $TemplatePath"
        return $false
    }

    $content = Get-Content $TemplatePath -Raw

    foreach ($key in $Replacements.Keys) {
        $content = $content -replace [regex]::Escape("{{$key}}"), $Replacements[$key]
    }

    Set-Content -Path $OutputPath -Value $content -Encoding UTF8
    Write-Host "Generated: $OutputPath"
    return $true
}

# Prepare replacement values
$replacements = @{
    "VERSION"       = $Version
    "X64_SHA256"    = $x64Hash
    "ARM64_SHA256"  = $arm64Hash
    "RELEASE_NOTES" = $ReleaseNotes
}

# Process each template
$templates = @(
    "RvToolsMerge.RvToolsMerge.yaml.template",
    "RvToolsMerge.RvToolsMerge.installer.yaml.template",
    "RvToolsMerge.RvToolsMerge.locale.en-US.yaml.template"
)

$success = $true
foreach ($template in $templates) {
    $templatePath = Join-Path $templateDir $template
    $outputFile = $template -replace "\.template$", ""
    $outputPath = Join-Path $OutputDir $outputFile

    if (-not (Invoke-TemplateProcessing -TemplatePath $templatePath -OutputPath $outputPath -Replacements $replacements)) {
        $success = $false
    }
}

if ($success) {
    Write-Host "✅ All winget manifests generated successfully in: $OutputDir"
    # List generated files
    Write-Host "`nGenerated files:"
    Get-ChildItem $OutputDir -Filter "*.yaml" | ForEach-Object {
        Write-Host "  - $($_.Name)"
    }

    # Validate the generated manifests - MANDATORY
    Write-Host "`nValidating winget manifests..."

    # Check if winget is available
    try {
        $wingetVersion = & winget --version 2>$null
        if ($LASTEXITCODE -ne 0) {
            throw "Winget command failed"
        }
    }
    catch {
        Write-Error "❌ Winget is not available or not installed. Winget validation is mandatory."
        Write-Error "Please install winget from: https://github.com/microsoft/winget-cli"
        Write-Error "Or install via Microsoft Store: ms-appinstaller:?source=https://aka.ms/getwinget"
        exit 1
    }

    Write-Host "Using winget version: $wingetVersion"

    # Run winget validate - this must succeed
    $validateResult = & winget validate $OutputDir 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Winget manifest validation passed successfully"
        if ($validateResult) {
            Write-Host $validateResult
        }
    }
    else {
        Write-Error "❌ Winget manifest validation failed. This is a mandatory check."
        Write-Error "Validation output:"
        Write-Error $validateResult
        exit 1
    }
}
else {
    Write-Error "❌ Failed to generate some winget manifests"
    exit 1
}
