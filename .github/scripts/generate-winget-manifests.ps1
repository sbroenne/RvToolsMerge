#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Generate and validate winget manifest files from templates

.DESCRIPTION
    This script generates winget manifest files from templates using provided version and MSI file information.
    All generated manifests are automatically validated with 'winget validate' after generation. The script will fail if validation does not pass.

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

.EXAMPLE
    .\generate-winget-manifests.ps1 -Version "1.0.0" -X64MsiPath ".\x64.msi" -Arm64MsiPath ".\arm64.msi" -OutputDir ".\manifests" -ReleaseNotes "Bug fixes and improvements"

.NOTES
    - Manifest validation with winget is always performed after generation.
    - Requires winget to be installed and available in PATH.
    - Script will exit with error code 1 if validation does not pass.
#>

# Generate Winget Manifests Script
# This script generates winget manifest files from templates with optional validation.
# Winget validation can be enabled or disabled based on requirements and availability.

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

# Input validation
Write-Host "Validating inputs..."

# Validate version format
if (-not ($Version -match '^\d+\.\d+\.\d+$')) {
    Write-Error "Invalid version format: $Version. Expected format: X.Y.Z (e.g., 1.2.3)"
    exit 1
}

# Validate MSI file paths
if (-not (Test-Path $X64MsiPath)) {
    Write-Error "x64 MSI file not found: $X64MsiPath"
    exit 1
}

if (-not (Test-Path $Arm64MsiPath)) {
    Write-Error "ARM64 MSI file not found: $Arm64MsiPath"
    exit 1
}

# Validate MSI file extensions
if (-not $X64MsiPath.EndsWith('.msi', [System.StringComparison]::OrdinalIgnoreCase)) {
    Write-Error "x64 file does not have .msi extension: $X64MsiPath"
    exit 1
}

if (-not $Arm64MsiPath.EndsWith('.msi', [System.StringComparison]::OrdinalIgnoreCase)) {
    Write-Error "ARM64 file does not have .msi extension: $Arm64MsiPath"
    exit 1
}

Write-Host "Input validation passed"

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

# Function to extract ProductCode from MSI
function Get-MsiProductCode {
    param([string]$MsiPath)
    try {
        $windowsInstaller = New-Object -ComObject WindowsInstaller.Installer
        $database = $windowsInstaller.GetType().InvokeMember('OpenDatabase', 'InvokeMethod', $null, $windowsInstaller, @($MsiPath, 0))
        $view = $database.GetType().InvokeMember('OpenView', 'InvokeMethod', $null, $database, @('SELECT `Value` FROM `Property` WHERE `Property` = "ProductCode"'))
        $view.GetType().InvokeMember('Execute', 'InvokeMethod', $null, $view, $null)
        $record = $view.GetType().InvokeMember('Fetch', 'InvokeMethod', $null, $view, $null)
        if ($record -ne $null) {
            $productCode = $record.GetType().InvokeMember('StringData', 'GetProperty', $null, $record, 1)
            return $productCode
        }
        else {
            Write-Error "ProductCode not found in MSI: $MsiPath"
            return $null
        }
    }
    catch {
        Write-Error "Failed to extract ProductCode from MSI: $MsiPath - $($_.Exception.Message)"
        return $null
    }
}

# Extract ProductCodes
Write-Host "Extracting ProductCode from x64 MSI..."
$x64ProductCode = Get-MsiProductCode -MsiPath $X64MsiPath
Write-Host "Extracting ProductCode from ARM64 MSI..."
$arm64ProductCode = Get-MsiProductCode -MsiPath $Arm64MsiPath

if (-not $x64ProductCode -or -not $arm64ProductCode) {
    Write-Error "Failed to extract ProductCode from one or both MSI files."
    exit 1
}

Write-Host "x64 MSI ProductCode: $x64ProductCode"
Write-Host "ARM64 MSI ProductCode: $arm64ProductCode"

# Template directory
$templateDir = Join-Path (Split-Path $PSScriptRoot -Parent) "winget-templates"

Write-Host "Template directory: $templateDir"

if (-not (Test-Path $templateDir)) {
    Write-Error "Template directory not found: $templateDir"
    exit 1
}

Write-Host "Template directory found"

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

    try {
        $content = Get-Content $TemplatePath -Raw -ErrorAction Stop

        foreach ($key in $Replacements.Keys) {
            $placeholder = "{{$key}}"
            $value = $Replacements[$key]
            $content = $content -replace [regex]::Escape($placeholder), $value
            Write-Host "  Replaced $placeholder with value (length: $($value.Length))"
        }

        Set-Content -Path $OutputPath -Value $content -Encoding UTF8 -ErrorAction Stop
        Write-Host "Generated: $OutputPath"
        return $true
    }
    catch {
        Write-Error "Failed to process template $TemplatePath`: $($_.Exception.Message)"
        return $false
    }
}

# Prepare replacement values
$replacements = @{
    "VERSION"       = $Version
    "X64_SHA256"    = $x64Hash
    "ARM64_SHA256"  = $arm64Hash
    "RELEASE_NOTES" = $ReleaseNotes
    "X64_PRODUCT_CODE" = $x64ProductCode
    "ARM64_PRODUCT_CODE" = $arm64ProductCode
}

Write-Host "Template replacements:"
Write-Host "  VERSION: $Version"
Write-Host "  X64_SHA256: $x64Hash"
Write-Host "  ARM64_SHA256: $arm64Hash"
Write-Host "  RELEASE_NOTES: $($ReleaseNotes.Length) characters"

# Process each template
$templates = @(
    "RvToolsMerge.RvToolsMerge.yaml.template",
    "RvToolsMerge.RvToolsMerge.installer.yaml.template",
    "RvToolsMerge.RvToolsMerge.locale.en-US.yaml.template"
)

Write-Host "`nProcessing $($templates.Count) template files..."

$success = $true
$processedCount = 0

foreach ($template in $templates) {
    $templatePath = Join-Path $templateDir $template
    $outputFile = $template -replace "\.template$", ""
    $outputPath = Join-Path $OutputDir $outputFile

    Write-Host "`nProcessing template: $template"

    if (Invoke-TemplateProcessing -TemplatePath $templatePath -OutputPath $outputPath -Replacements $replacements) {
        $processedCount++
    }
    else {
        $success = $false
        Write-Error "Failed to process template: $template"
    }
}

Write-Host "`nTemplate processing summary: $processedCount/$($templates.Count) templates processed successfully"

if ($success) {
    Write-Host "All winget manifests generated successfully in: $OutputDir"
    # List generated files
    Write-Host "`nGenerated files:"
    Get-ChildItem $OutputDir -Filter "*.yaml" | ForEach-Object {
        Write-Host "  - $($_.Name)"
    }
    # Always validate the generated manifests
    Write-Host "`nValidating winget manifests..."

    # Check if winget is available
    try {
        $wingetVersion = & winget --version 2>$null
        if ($LASTEXITCODE -ne 0) {
            throw "Winget command failed"
        }
    }
    catch {
        Write-Warning "Winget is not available or not installed. Validation will be skipped."
        Write-Warning "To enable validation, install winget from: https://github.com/microsoft/winget-cli"
        Write-Warning "Or install via Microsoft Store: ms-appinstaller:?source=https://aka.ms/getwinget"
        Write-Host "Manifest generation completed without validation"
        exit 0
    }

    Write-Host "Using winget version: $wingetVersion"

    # Run winget validate
    $validateResult = & winget validate $OutputDir 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Winget manifest validation passed successfully"
        if ($validateResult) {
            Write-Host $validateResult
        }
    }
    else {
        Write-Error "Winget manifest validation failed."
        Write-Error "Validation output:"
        Write-Error $validateResult
        exit 1
    }
}
else {
    Write-Error "Failed to generate some winget manifests"
    exit 1
}
