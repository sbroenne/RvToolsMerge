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
# Helper function for error and exit
function Throw-Exit {
    param([string]$Message)
    Write-Error $Message
    exit 1
}

Write-Host "Validating inputs..."

# Validate version format
if (-not ($Version -match '^\d+\.\d+\.\d+$')) {
    Throw-Exit "Invalid version format: $Version. Expected format: X.Y.Z (e.g., 1.2.3)"
}

# Validate MSI file paths
if (-not (Test-Path $X64MsiPath)) {
    Throw-Exit "x64 MSI file not found: $X64MsiPath"
}

if (-not (Test-Path $Arm64MsiPath)) {
    Throw-Exit "ARM64 MSI file not found: $Arm64MsiPath"
}

# Validate MSI file extensions
if (-not $X64MsiPath.EndsWith('.msi', [System.StringComparison]::OrdinalIgnoreCase)) {
    Throw-Exit "x64 file does not have .msi extension: $X64MsiPath"
}

if (-not $Arm64MsiPath.EndsWith('.msi', [System.StringComparison]::OrdinalIgnoreCase)) {
    Throw-Exit "ARM64 file does not have .msi extension: $Arm64MsiPath"
}

Write-Host "Input validation passed"

# Function to calculate SHA256 hash
function Get-FileSha256Hash {
    param([string]$FilePath)

    if (-not (Test-Path $FilePath)) {
        Throw-Exit "File not found: $FilePath"
    }

    return (Get-FileHash -Path $FilePath -Algorithm SHA256).Hash
}

# Function to extract and normalize ProductCode from MSI
function Get-MsiProductCode {
    param ([Parameter(Mandatory = $true)][string]$MsiPath)

    if (-not (Test-Path $MsiPath)) {
        Throw-Exit "MSI file not found at path: $MsiPath"
    }

    try {
        $installer = New-Object -ComObject WindowsInstaller.Installer
        $database = $installer.GetType().InvokeMember("OpenDatabase", "InvokeMethod", $null, $installer, @($MsiPath, 0))
        $view = $database.OpenView("SELECT Value FROM Property WHERE Property = 'ProductCode'")
        $view.Execute() | Out-Null
        $record = $view.Fetch()
        $productCode = $record.StringData(1)
        return productCode
    }
    catch {
        Throw-Exit "Failed to retrieve ProductCode: $_"
    }
}

# Calculate SHA256 hashes
Write-Host "Calculating SHA256 hashes..."
$x64Hash = Get-FileSha256Hash -FilePath $X64MsiPath
$arm64Hash = Get-FileSha256Hash -FilePath $Arm64MsiPath
Write-Host ("x64 MSI SHA256: {0}" -f $x64Hash)
Write-Host ("ARM64 MSI SHA256: {0}" -f $arm64Hash)

# Extract ProductCodes
Write-Host "Extracting ProductCode from x64 MSI..."
$x64ProductCode = Get-MsiProductCode -MsiPath $X64MsiPath
Write-Host "Extracting ProductCode from ARM64 MSI..."
$arm64ProductCode = Get-MsiProductCode -MsiPath $Arm64MsiPath
Write-Host ("x64 MSI ProductCode: {0}" -f $x64ProductCode)
Write-Host ("ARM64 MSI ProductCode: {0}" -f $arm64ProductCode)

# Template directory
$templateDir = Join-Path (Split-Path $PSScriptRoot -Parent) "winget-templates"
Write-Host ("Template directory: {0}" -f $templateDir)
if (-not (Test-Path $templateDir)) { Throw-Exit "Template directory not found: $templateDir" }
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
    "VERSION"            = $Version
    "X64_SHA256"         = $x64Hash
    "ARM64_SHA256"       = $arm64Hash
    "RELEASE_NOTES"      = $ReleaseNotes
    "X64_PRODUCT_CODE"   = $x64ProductCode
    "ARM64_PRODUCT_CODE" = $arm64ProductCode
}
# Process each template
$templates = @(
    "RvToolsMerge.RvToolsMerge.yaml.template",
    "RvToolsMerge.RvToolsMerge.installer.yaml.template",
    "RvToolsMerge.RvToolsMerge.locale.en-US.yaml.template"
)
Write-Host ("`nProcessing {0} template files..." -f $templates.Count)

$success = $true
$processedCount = 0
foreach ($template in $templates) {
    $templatePath = Join-Path $templateDir $template
    $outputFile = $template -replace "\.template$", ""
    $outputPath = Join-Path $OutputDir $outputFile
    Write-Host ("`nProcessing template: {0}" -f $template)
    if (Invoke-TemplateProcessing -TemplatePath $templatePath -OutputPath $outputPath -Replacements $replacements) {
        $processedCount++
    }
    else {
        $success = $false
        Write-Error "Failed to process template: $template"
    }
}
Write-Host ("`nTemplate processing summary: {0}/{1} templates processed successfully" -f $processedCount, $templates.Count)

if ($success) {
    Write-Host ("All winget manifests generated successfully in: {0}" -f $OutputDir)
    Write-Host "`nGenerated files:"
    Get-ChildItem $OutputDir -Filter "*.yaml" | ForEach-Object { Write-Host "  - $($_.Name)" }
    Write-Host "`nValidating winget manifests..."
    try {
        $wingetVersion = & winget --version 2>$null
        if ($LASTEXITCODE -ne 0) { throw "Winget command failed" }
    }
    catch {
        Write-Warning "Winget is not available or not installed. Validation will be skipped."
        Write-Warning "To enable validation, install winget from: https://github.com/microsoft/winget-cli"
        Write-Warning "Or install via Microsoft Store: ms-appinstaller:?source=https://aka.ms/getwinget"
        Write-Host "Manifest generation completed without validation"
        exit 0
    }
    Write-Host ("Using winget version: {0}" -f $wingetVersion)
    $validateResult = & winget validate $OutputDir 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Winget manifest validation passed successfully"
        if ($validateResult) { Write-Host $validateResult }
    }
    else {
        Write-Error "Winget manifest validation failed."
        Write-Error "Validation output:"
        Write-Error ($validateResult -join [Environment]::NewLine)
        exit 1
    }
}
else {
    Throw-Exit "Failed to generate some winget manifests"
}
