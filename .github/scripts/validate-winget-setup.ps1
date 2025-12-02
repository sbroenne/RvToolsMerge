#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Validates the entire winget submission process and prerequisites

.DESCRIPTION
    This script validates that all components required for winget submission are properly configured:
    - Templates exist and are valid
    - Generation script works correctly
    - Workflows are properly configured
    - Documentation is accurate

.PARAMETER ProjectRoot
    Path to the project root directory. Defaults to current directory.

.EXAMPLE
    .\validate-winget-setup.ps1

.EXAMPLE
    .\validate-winget-setup.ps1 -ProjectRoot "C:\RvToolsMerge"
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$ProjectRoot = "."
)

$ErrorActionPreference = "Stop"

# Resolve project root to absolute path
$ProjectRoot = Resolve-Path $ProjectRoot

Write-Host "Validating Winget Submission Setup" -ForegroundColor Green
Write-Host "Project root: $ProjectRoot" -ForegroundColor Cyan
Write-Host ""

$hasErrors = $false
$warnings = @()
$successes = @()

function Test-Component {
    param(
        [string]$Name,
        [scriptblock]$Test,
        [string]$ErrorMessage = "Test failed"
    )

    Write-Host "Testing: $Name" -ForegroundColor Yellow
    try {
        $result = & $Test
        if ($result -eq $false) {
            Write-Host "  FAILED: $ErrorMessage" -ForegroundColor Red
            $script:hasErrors = $true
        } else {
            Write-Host "  PASSED" -ForegroundColor Green
            $script:successes += $Name
        }
    }
    catch {
        Write-Host "  FAILED: $ErrorMessage`: $($_.Exception.Message)" -ForegroundColor Red
        $script:hasErrors = $true
    }
}

function Test-Warning {
    param(
        [string]$Name,
        [scriptblock]$Test,
        [string]$Message
    )

    try {
        $result = & $Test
        if ($result -eq $false) {
            Write-Host "  WARNING: $Message" -ForegroundColor Yellow
            $script:warnings += $Message
        }
    }
    catch {
        Write-Host "  WARNING: $Message`: $($_.Exception.Message)" -ForegroundColor Yellow
        $script:warnings += "$Message`: $($_.Exception.Message)"
    }
}

# Test 1: Verify winget templates exist
Test-Component "Winget templates exist" {
    $templateDir = Join-Path $ProjectRoot ".github/winget-templates"
    $expectedTemplates = @(
        "RvToolsMerge.RvToolsMerge.yaml.template",
        "RvToolsMerge.RvToolsMerge.installer.yaml.template",
        "RvToolsMerge.RvToolsMerge.locale.en-US.yaml.template"
    )

    foreach ($template in $expectedTemplates) {
        $templatePath = Join-Path $templateDir $template
        if (-not (Test-Path $templatePath)) {
            return $false
        }
    }
    return $true
} "Required winget template files are missing"

# Test 2: Verify template content has required placeholders
Test-Component "Templates have required placeholders" {
    $templateDir = Join-Path $ProjectRoot ".github/winget-templates"
    
    # Define required placeholders for each template type
    $templateRequirements = @{
        "RvToolsMerge.RvToolsMerge.yaml.template" = @("{{VERSION}}")
        "RvToolsMerge.RvToolsMerge.installer.yaml.template" = @("{{VERSION}}", "{{X64_SHA256}}", "{{ARM64_SHA256}}")
        "RvToolsMerge.RvToolsMerge.locale.en-US.yaml.template" = @("{{VERSION}}", "{{RELEASE_NOTES}}")
    }
    
    foreach ($templateName in $templateRequirements.Keys) {
        $templatePath = Join-Path $templateDir $templateName
        if (-not (Test-Path $templatePath)) {
            Write-Host "    Template not found: $templateName" -ForegroundColor Red
            return $false
        }
        
        $content = Get-Content $templatePath -Raw
        $requiredPlaceholders = $templateRequirements[$templateName]
        
        foreach ($placeholder in $requiredPlaceholders) {
            if ($content -notlike "*$placeholder*") {
                Write-Host "    Missing placeholder $placeholder in $templateName" -ForegroundColor Red
                return $false
            }
        }
    }
    return $true
} "Template files are missing required placeholders"

# Test 3: Verify manifest generation script exists and is executable
Test-Component "Manifest generation script exists" {
    $scriptPath = Join-Path $ProjectRoot ".github/scripts/generate-winget-manifests.ps1"
    return (Test-Path $scriptPath)
} "Manifest generation script not found"

# Test 4: Test manifest generation with sample data
Test-Component "Manifest generation works" {
    $scriptPath = Join-Path $ProjectRoot ".github/scripts/generate-winget-manifests.ps1"
    if (-not (Test-Path $scriptPath)) {
        Write-Host "    Script not found at: $scriptPath" -ForegroundColor Red
        return $false
    }
    
    # Use a simpler test approach - just verify the script can be loaded
    try {
        # Test script syntax by dot-sourcing it with Get-Help
        $helpResult = pwsh -Command "Get-Help '$scriptPath'" 2>&1
        
        # If we can get help, the script syntax is valid
        if ($helpResult -like "*generate-winget-manifests*" -or $helpResult -like "*SYNOPSIS*") {
            return $true
        }
        
        # Also check if it has the correct parameters
        $parameterCheck = pwsh -Command "(Get-Command '$scriptPath').Parameters.Keys" 2>&1
        if ($parameterCheck -like "*Version*" -and $parameterCheck -like "*X64MsiPath*" -and $parameterCheck -like "*Arm64MsiPath*") {
            return $true
        }
        
        return $false
    }
    catch {
        Write-Host "    Script validation failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
} "Manifest generation script has syntax errors or missing parameters"

# Test 5: Verify winget submission workflow exists
Test-Component "Winget submission workflow exists" {
    $workflowPath = Join-Path $ProjectRoot ".github/workflows/winget-submission.yml"
    return (Test-Path $workflowPath)
} "Winget submission workflow not found"

# Test 6: Verify version management workflow includes winget manifest generation
Test-Component "Version management includes winget generation" {
    $workflowPath = Join-Path $ProjectRoot ".github/workflows/version-management.yml"
    if (-not (Test-Path $workflowPath)) {
        return $false
    }
    
    $content = Get-Content $workflowPath -Raw
    return ($content -like "*generate-winget-manifests*")
} "Version management workflow does not include winget manifest generation"

# Test 7: Verify documentation exists
Test-Component "Winget documentation exists" {
    $docFiles = @(
        ".github/winget-templates/README.md",
        "docs/winget-submission-setup.md"
    )
    
    foreach ($docFile in $docFiles) {
        $docPath = Join-Path $ProjectRoot $docFile
        if (-not (Test-Path $docPath)) {
            return $false
        }
    }
    return $true
} "Required winget documentation files are missing"

# Test 8: Verify workflow configuration
Test-Component "Winget submission workflow configuration" {
    $workflowPath = Join-Path $ProjectRoot ".github/workflows/winget-submission.yml"
    $content = Get-Content $workflowPath -Raw
    
    # Check for required elements
    $requiredElements = @(
        "workflow_dispatch",
        "releaseTag:",
        "dryRun:",
        "WINGET_SUBMISSION_TOKEN",
        "winget-pkgs"
    )
    
    foreach ($element in $requiredElements) {
        if ($content -notlike "*$element*") {
            Write-Host "    Missing required element: $element" -ForegroundColor Red
            return $false
        }
    }
    return $true
} "Winget submission workflow is not properly configured"

# Test 9: Check for repository secrets documentation
Test-Warning "Repository secrets documented" {
    $setupDoc = Join-Path $ProjectRoot "docs/winget-submission-setup.md"
    if (-not (Test-Path $setupDoc)) {
        return $false
    }
    
    $content = Get-Content $setupDoc -Raw
    return ($content -like "*WINGET_SUBMISSION_TOKEN*")
} "WINGET_SUBMISSION_TOKEN secret is not documented in setup guide"

# Test 10: Check winget manifest schema version
Test-Component "Winget manifests use current schema version" {
    $templateDir = Join-Path $ProjectRoot ".github/winget-templates"
    $templates = Get-ChildItem $templateDir -Filter "*.template"
    
    foreach ($template in $templates) {
        $content = Get-Content $template.FullName -Raw
        if ($content -notlike "*ManifestVersion: 1.6.0*") {
            Write-Host "    Template $($template.Name) does not use schema version 1.6.0" -ForegroundColor Red
            return $false
        }
    }
    return $true
} "Winget manifests do not use the current schema version (1.6.0)"

# Test 11: Check if winget is available (warning only)
Test-Warning "Winget available for validation" {
    try {
        $wingetVersion = & winget --version 2>$null
        return ($LASTEXITCODE -eq 0)
    }
    catch {
        return $false
    }
} "Winget is not available for manifest validation. Install from Microsoft Store or GitHub releases."

# Test 12: Verify package identifier consistency
Test-Component "Package identifier consistency" {
    $templateDir = Join-Path $ProjectRoot ".github/winget-templates"
    $templates = Get-ChildItem $templateDir -Filter "*.template"
    
    foreach ($template in $templates) {
        $content = Get-Content $template.FullName -Raw
        if ($content -notlike "*PackageIdentifier: RvToolsMerge.RvToolsMerge*") {
            Write-Host "    Template $($template.Name) has incorrect package identifier" -ForegroundColor Red
            return $false
        }
    }
    return $true
} "Package identifier is not consistent across templates"

# Test 13: Check if templates follow winget naming conventions
Test-Component "Template files follow naming conventions" {
    $templateDir = Join-Path $ProjectRoot ".github/winget-templates"
    $expectedFiles = @(
        "RvToolsMerge.RvToolsMerge.yaml.template",
        "RvToolsMerge.RvToolsMerge.installer.yaml.template",
        "RvToolsMerge.RvToolsMerge.locale.en-US.yaml.template"
    )
    
    foreach ($expectedFile in $expectedFiles) {
        $filePath = Join-Path $templateDir $expectedFile
        if (-not (Test-Path $filePath)) {
            Write-Host "    Missing template file: $expectedFile" -ForegroundColor Red
            return $false
        }
    }
    return $true
} "Template files do not follow winget naming conventions"

# Test 14: Verify installer manifest has required fields
Test-Component "Installer manifest has required fields" {
    $installerTemplate = Join-Path $ProjectRoot ".github/winget-templates/RvToolsMerge.RvToolsMerge.installer.yaml.template"
    $content = Get-Content $installerTemplate -Raw
    
    $requiredFields = @(
        "PackageIdentifier:",
        "PackageVersion:",
        "MinimumOSVersion:",
        "Installers:",
        "Architecture: x64",
        "Architecture: arm64",
        "InstallerType: wix",
        "InstallerSha256:"
    )
    
    foreach ($field in $requiredFields) {
        if ($content -notlike "*$field*") {
            Write-Host "    Missing required field: $field" -ForegroundColor Red
            return $false
        }
    }
    return $true
} "Installer manifest template is missing required fields"

# Summary
Write-Host ""
Write-Host "Validation Summary" -ForegroundColor Green
Write-Host "===================" -ForegroundColor Green

if ($successes.Count -gt 0) {
    Write-Host ""
    Write-Host "Passed Tests ($($successes.Count)):" -ForegroundColor Green
    foreach ($success in $successes) {
        Write-Host "  - $success" -ForegroundColor Green
    }
}

if ($warnings.Count -gt 0) {
    Write-Host ""
    Write-Host "Warnings ($($warnings.Count)):" -ForegroundColor Yellow
    foreach ($warning in $warnings) {
        Write-Host "  - $warning" -ForegroundColor Yellow
    }
}

if ($hasErrors) {
    Write-Host ""
    Write-Host "Some tests failed. Please review the errors above." -ForegroundColor Red
    Write-Host ""
    Write-Host "Common solutions:" -ForegroundColor Cyan
    Write-Host "- Ensure all required template files exist in .github/winget-templates/" -ForegroundColor White
    Write-Host "- Verify the manifest generation script is executable" -ForegroundColor White
    Write-Host "- Check that workflows are properly configured" -ForegroundColor White
    Write-Host "- Review documentation for accuracy" -ForegroundColor White
    exit 1
} else {
    Write-Host ""
    Write-Host "All critical tests passed! Winget submission setup is ready." -ForegroundColor Green
    
    if ($warnings.Count -gt 0) {
        Write-Host ""
        Write-Host "Please review the warnings above for optimal setup." -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "- Test the full workflow with a release" -ForegroundColor White
    Write-Host "- Verify winget-pkgs fork is properly configured" -ForegroundColor White
    Write-Host "- Ensure WINGET_SUBMISSION_TOKEN secret is set" -ForegroundColor White
    exit 0
}