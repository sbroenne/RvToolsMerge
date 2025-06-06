#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$true)]
    [string]$X64MsiPath,
    
    [Parameter(Mandatory=$true)]
    [string]$Arm64MsiPath,
    
    [Parameter(Mandatory=$true)]
    [string]$OutputDir,
    
    [Parameter(Mandatory=$false)]
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
function Process-Template {
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
    "VERSION" = $Version
    "X64_SHA256" = $x64Hash
    "ARM64_SHA256" = $arm64Hash
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
    
    if (-not (Process-Template -TemplatePath $templatePath -OutputPath $outputPath -Replacements $replacements)) {
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
} else {
    Write-Error "❌ Failed to generate some winget manifests"
    exit 1
}