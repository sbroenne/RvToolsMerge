#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Comprehensive end-to-end validation of the winget submission process

.DESCRIPTION
    This script performs a complete validation of the winget submission process readiness:
    - Validates all templates and scripts exist
    - Tests manifest generation with realistic data
    - Validates generated manifests against winget standards
    - Checks workflow configurations
    - Verifies documentation completeness
    
.PARAMETER ProjectRoot
    Path to the project root directory. Defaults to current directory.

.PARAMETER CreateSampleRelease
    If specified, creates sample MSI files for testing manifest generation.

.EXAMPLE
    .\comprehensive-winget-validation.ps1

.EXAMPLE
    .\comprehensive-winget-validation.ps1 -ProjectRoot "C:\RvToolsMerge" -CreateSampleRelease
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$ProjectRoot = ".",
    
    [Parameter(Mandatory = $false)]
    [switch]$CreateSampleRelease
)

$ErrorActionPreference = "Stop"

# Resolve project root to absolute path
$ProjectRoot = Resolve-Path $ProjectRoot

Write-Host "🚀 Comprehensive Winget Submission Process Validation" -ForegroundColor Green
Write-Host "Project root: $ProjectRoot" -ForegroundColor Cyan
Write-Host ""

$validationResults = @()
$warningCount = 0
$errorCount = 0

function Add-ValidationResult {
    param(
        [string]$Component,
        [string]$Test,
        [string]$Status,
        [string]$Message = ""
    )
    
    $result = [PSCustomObject]@{
        Component = $Component
        Test = $Test
        Status = $Status
        Message = $Message
        Timestamp = Get-Date
    }
    
    $script:validationResults += $result
    
    $icon = switch ($Status) {
        "PASS" { "✅"; break }
        "WARN" { "⚠️"; $script:warningCount++; break }
        "FAIL" { "❌"; $script:errorCount++; break }
    }
    
    Write-Host "  $icon $Test" -ForegroundColor $(if ($Status -eq "PASS") { "Green" } elseif ($Status -eq "WARN") { "Yellow" } else { "Red" })
    if ($Message) {
        Write-Host "    $Message" -ForegroundColor Gray
    }
}

# 1. Basic Infrastructure Validation
Write-Host "1️⃣ Infrastructure Validation" -ForegroundColor Cyan

$templateDir = Join-Path $ProjectRoot ".github/winget-templates"
$scriptsDir = Join-Path $ProjectRoot ".github/scripts"
$workflowsDir = Join-Path $ProjectRoot ".github/workflows"

if (Test-Path $templateDir) {
    Add-ValidationResult "Infrastructure" "Template directory exists" "PASS"
    
    $expectedTemplates = @(
        "RvToolsMerge.RvToolsMerge.yaml.template",
        "RvToolsMerge.RvToolsMerge.installer.yaml.template", 
        "RvToolsMerge.RvToolsMerge.locale.en-US.yaml.template"
    )
    
    $missingTemplates = @()
    foreach ($template in $expectedTemplates) {
        if (Test-Path (Join-Path $templateDir $template)) {
            Add-ValidationResult "Infrastructure" "Template $template exists" "PASS"
        } else {
            Add-ValidationResult "Infrastructure" "Template $template exists" "FAIL" "Template file not found"
            $missingTemplates += $template
        }
    }
} else {
    Add-ValidationResult "Infrastructure" "Template directory exists" "FAIL" "Directory not found: $templateDir"
}

$generationScript = Join-Path $scriptsDir "generate-winget-manifests.ps1"
if (Test-Path $generationScript) {
    Add-ValidationResult "Infrastructure" "Generation script exists" "PASS"
} else {
    Add-ValidationResult "Infrastructure" "Generation script exists" "FAIL" "Script not found: $generationScript"
}

$validationScript = Join-Path $scriptsDir "validate-winget-setup.ps1"
if (Test-Path $validationScript) {
    Add-ValidationResult "Infrastructure" "Validation script exists" "PASS"
} else {
    Add-ValidationResult "Infrastructure" "Validation script exists" "FAIL" "Script not found: $validationScript"
}

# 2. Template Content Validation
Write-Host "`n2️⃣ Template Content Validation" -ForegroundColor Cyan

if (Test-Path $templateDir) {
    $templates = Get-ChildItem $templateDir -Filter "*.template"
    
    foreach ($template in $templates) {
        $content = Get-Content $template.FullName -Raw
        
        # Check for required placeholders
        $requiredPlaceholders = @("{{VERSION}}")
        if ($template.Name -like "*installer*") {
            $requiredPlaceholders += @("{{X64_SHA256}}", "{{ARM64_SHA256}}")
        }
        if ($template.Name -like "*locale*") {
            $requiredPlaceholders += @("{{RELEASE_NOTES}}")
        }
        
        foreach ($placeholder in $requiredPlaceholders) {
            if ($content -like "*$placeholder*") {
                Add-ValidationResult "Templates" "$($template.Name) has $placeholder" "PASS"
            } else {
                Add-ValidationResult "Templates" "$($template.Name) has $placeholder" "FAIL" "Missing placeholder"
            }
        }
        
        # Check for consistent package identifier
        if ($content -like "*PackageIdentifier: RvToolsMerge.RvToolsMerge*") {
            Add-ValidationResult "Templates" "$($template.Name) has correct package ID" "PASS"
        } else {
            Add-ValidationResult "Templates" "$($template.Name) has correct package ID" "FAIL" "Incorrect or missing package identifier"
        }
        
        # Check schema version
        if ($content -like "*ManifestVersion: 1.6.0*") {
            Add-ValidationResult "Templates" "$($template.Name) uses current schema" "PASS"
        } else {
            Add-ValidationResult "Templates" "$($template.Name) uses current schema" "WARN" "May not be using the latest schema version"
        }
    }
}

# 3. Manifest Generation Testing
Write-Host "`n3️⃣ Manifest Generation Testing" -ForegroundColor Cyan

if ($CreateSampleRelease -and (Test-Path $generationScript)) {
    $testDir = Join-Path ([System.IO.Path]::GetTempPath()) "winget-validation-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    New-Item -ItemType Directory -Path $testDir -Force | Out-Null
    
    try {
        # Create sample MSI files
        $x64Msi = Join-Path $testDir "RVToolsMerge-1.0.0-win-x64.msi"
        $arm64Msi = Join-Path $testDir "RVToolsMerge-1.0.0-win-arm64.msi"
        
        "Sample x64 MSI content for testing" | Out-File -FilePath $x64Msi -Encoding UTF8
        "Sample ARM64 MSI content for testing" | Out-File -FilePath $arm64Msi -Encoding UTF8
        
        $outputDir = Join-Path $testDir "manifests"
        
        # Test manifest generation
        $result = & pwsh $generationScript -Version "1.0.0" -X64MsiPath $x64Msi -Arm64MsiPath $arm64Msi -OutputDir $outputDir -ReleaseNotes "Test release for validation" 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Add-ValidationResult "Generation" "Manifest generation succeeds" "PASS"
            
            # Verify generated files
            $expectedFiles = @(
                "RvToolsMerge.RvToolsMerge.yaml",
                "RvToolsMerge.RvToolsMerge.installer.yaml",
                "RvToolsMerge.RvToolsMerge.locale.en-US.yaml"
            )
            
            foreach ($file in $expectedFiles) {
                $filePath = Join-Path $outputDir $file
                if (Test-Path $filePath) {
                    Add-ValidationResult "Generation" "Generated $file" "PASS"
                    
                    # Validate YAML syntax
                    try {
                        $yamlContent = Get-Content $filePath -Raw
                        # Simple YAML validation - check for basic structure
                        if ($yamlContent -match "PackageIdentifier:" -and $yamlContent -match "PackageVersion:" -and $yamlContent -match "ManifestType:") {
                            Add-ValidationResult "Generation" "$file has valid structure" "PASS"
                        } else {
                            Add-ValidationResult "Generation" "$file has valid structure" "WARN" "May be missing required fields"
                        }
                    } catch {
                        Add-ValidationResult "Generation" "$file has valid YAML" "FAIL" "YAML parsing error: $($_.Exception.Message)"
                    }
                } else {
                    Add-ValidationResult "Generation" "Generated $file" "FAIL" "File not generated"
                }
            }
        } else {
            Add-ValidationResult "Generation" "Manifest generation succeeds" "FAIL" "Script failed with exit code $LASTEXITCODE"
        }
    } catch {
        Add-ValidationResult "Generation" "Manifest generation test" "FAIL" "Exception: $($_.Exception.Message)"
    } finally {
        # Cleanup
        if (Test-Path $testDir) {
            Remove-Item $testDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
} else {
    Add-ValidationResult "Generation" "Sample generation test" "WARN" "Skipped - use -CreateSampleRelease to enable"
}

# 4. Workflow Configuration Validation
Write-Host "`n4️⃣ Workflow Configuration Validation" -ForegroundColor Cyan

$wingetWorkflow = Join-Path $workflowsDir "winget-submission.yml"
if (Test-Path $wingetWorkflow) {
    Add-ValidationResult "Workflows" "Winget submission workflow exists" "PASS"
    
    $workflowContent = Get-Content $wingetWorkflow -Raw
    $requiredElements = @(
        "workflow_dispatch",
        "releaseTag:",
        "dryRun:",
        "WINGET_SUBMISSION_TOKEN",
        "RvToolsMerge.RvToolsMerge.yaml",
        "winget-pkgs"
    )
    
    foreach ($element in $requiredElements) {
        if ($workflowContent -like "*$element*") {
            Add-ValidationResult "Workflows" "Has $element configuration" "PASS"
        } else {
            Add-ValidationResult "Workflows" "Has $element configuration" "FAIL" "Missing configuration element"
        }
    }
} else {
    Add-ValidationResult "Workflows" "Winget submission workflow exists" "FAIL" "Workflow file not found"
}

$versionWorkflow = Join-Path $workflowsDir "version-management.yml"
if (Test-Path $versionWorkflow) {
    Add-ValidationResult "Workflows" "Version management workflow exists" "PASS"
    
    $versionContent = Get-Content $versionWorkflow -Raw
    if ($versionContent -like "*generate-winget-manifests*") {
        Add-ValidationResult "Workflows" "Includes winget generation" "PASS"
    } else {
        Add-ValidationResult "Workflows" "Includes winget generation" "WARN" "May not include winget manifest generation"
    }
} else {
    Add-ValidationResult "Workflows" "Version management workflow exists" "FAIL" "Workflow file not found"
}

# 5. Documentation Validation
Write-Host "`n5️⃣ Documentation Validation" -ForegroundColor Cyan

$setupDoc = Join-Path $ProjectRoot "docs/winget-submission-setup.md"
if (Test-Path $setupDoc) {
    Add-ValidationResult "Documentation" "Setup documentation exists" "PASS"
    
    $docContent = Get-Content $setupDoc -Raw
    $requiredSections = @(
        "Prerequisites",
        "Required Manifest Files", 
        "WINGET_SUBMISSION_TOKEN",
        "microsoft/winget-pkgs"
    )
    
    foreach ($section in $requiredSections) {
        if ($docContent -like "*$section*") {
            Add-ValidationResult "Documentation" "Contains $section" "PASS"
        } else {
            Add-ValidationResult "Documentation" "Contains $section" "WARN" "Section may be missing or incomplete"
        }
    }
} else {
    Add-ValidationResult "Documentation" "Setup documentation exists" "FAIL" "Documentation file not found"
}

$templateReadme = Join-Path $templateDir "README.md"
if (Test-Path $templateReadme) {
    Add-ValidationResult "Documentation" "Template documentation exists" "PASS"
} else {
    Add-ValidationResult "Documentation" "Template documentation exists" "WARN" "Template README not found"
}

# 6. Security and Best Practices
Write-Host "`n6️⃣ Security and Best Practices" -ForegroundColor Cyan

# Check for hardcoded secrets in templates
if (Test-Path $templateDir) {
    $templates = Get-ChildItem $templateDir -Filter "*.template"
    foreach ($template in $templates) {
        $content = Get-Content $template.FullName -Raw
        if ($content -match "token|password|secret|key" -and $content -notmatch "{{") {
            Add-ValidationResult "Security" "$($template.Name) has no hardcoded secrets" "WARN" "May contain hardcoded sensitive data"
        } else {
            Add-ValidationResult "Security" "$($template.Name) has no hardcoded secrets" "PASS"
        }
    }
}

# Check for proper URL patterns
if (Test-Path $templateDir) {
    $installerTemplate = Join-Path $templateDir "RvToolsMerge.RvToolsMerge.installer.yaml.template"
    if (Test-Path $installerTemplate) {
        $content = Get-Content $installerTemplate -Raw
        if ($content -like "*github.com/sbroenne/RVToolsMerge/releases*") {
            Add-ValidationResult "Security" "Uses correct GitHub release URLs" "PASS"
        } else {
            Add-ValidationResult "Security" "Uses correct GitHub release URLs" "WARN" "URL pattern may be incorrect"
        }
    }
}

# 7. Final Summary and Report
Write-Host "`n🏁 Validation Summary" -ForegroundColor Green
Write-Host "==================" -ForegroundColor Green

$totalTests = $validationResults.Count
$passedTests = ($validationResults | Where-Object { $_.Status -eq "PASS" }).Count
$warnings = ($validationResults | Where-Object { $_.Status -eq "WARN" }).Count
$failures = ($validationResults | Where-Object { $_.Status -eq "FAIL" }).Count

Write-Host ""
Write-Host "📊 Results:" -ForegroundColor White
Write-Host "  Total Tests: $totalTests" -ForegroundColor White
Write-Host "  ✅ Passed: $passedTests" -ForegroundColor Green
Write-Host "  ⚠️ Warnings: $warnings" -ForegroundColor Yellow
Write-Host "  ❌ Failed: $failures" -ForegroundColor Red

# Export detailed results
$reportPath = Join-Path $ProjectRoot "winget-validation-report.json"
$validationResults | ConvertTo-Json -Depth 3 | Out-File $reportPath -Encoding UTF8
Write-Host ""
Write-Host "📄 Detailed report saved to: $reportPath" -ForegroundColor Cyan

if ($failures -eq 0) {
    Write-Host ""
    Write-Host "🎉 Winget submission process validation completed successfully!" -ForegroundColor Green
    
    if ($warnings -gt 0) {
        Write-Host "⚠️ $warnings warning(s) found - please review for optimal configuration." -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "✅ Ready for winget submission process!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "• Create a release to test the full workflow" -ForegroundColor White
    Write-Host "• Ensure WINGET_SUBMISSION_TOKEN secret is configured" -ForegroundColor White
    Write-Host "• Fork microsoft/winget-pkgs repository" -ForegroundColor White
    Write-Host "• Test the winget submission workflow" -ForegroundColor White
    
    exit 0
} else {
    Write-Host ""
    Write-Host "❌ Validation failed with $failures error(s)." -ForegroundColor Red
    Write-Host ""
    Write-Host "Failed tests:" -ForegroundColor Red
    $validationResults | Where-Object { $_.Status -eq "FAIL" } | ForEach-Object {
        Write-Host "  • $($_.Component): $($_.Test)" -ForegroundColor Red
        if ($_.Message) {
            Write-Host "    $($_.Message)" -ForegroundColor Gray
        }
    }
    
    exit 1
}