<#
.SYNOPSIS
    Synchronizes README.md and README_github.md files.
.DESCRIPTION
    This script ensures that README.md and README_github.md stay in sync.
    It extracts the common content from README_github.md and updates README.md,
    while preserving the GitHub-specific content in README_github.md.
.EXAMPLE
    .\sync-readme.ps1
    Syncs the README files.
#>

# Paths to README files
$githubReadme = "d:\repos\RVToolsMerge\README_github.md"
$standardReadme = "d:\repos\RVToolsMerge\README.md"

# Check if the GitHub README exists
if (-not (Test-Path $githubReadme)) {
    Write-Error "GitHub README file not found at: $githubReadme"
    exit 1
}

# Read the GitHub README content
$content = Get-Content -Path $githubReadme -Raw

# Extract the main content (without the GitHub-specific sections)
$mainContent = $content -replace '(?ms)^# Github Project.*$', ''

# Extract the badges section
$badges = ""
if ($content -match '(?ms)^(\[\!\[.*?\]\(.*?\)\])') {
    $badges = $Matches[1]
}

# Remove the badges from the standard README content
$mainContent = $mainContent -replace '(?ms)^\[\!\[.*?\]\(.*?\)\]', ''

# Add license and acknowledgments back if they were removed
if ($mainContent -notmatch '(?ms)^## License') {
    $licenseSection = ""
    if ($content -match '(?ms)^## License.*?(\n##|\z)') {
        $licenseSection = $Matches[0] -replace '(\n##|\z)', ''
    }
    $mainContent += "`n" + $licenseSection
}

if ($mainContent -notmatch '(?ms)^## Acknowledgments') {
    $acknowledgeSection = ""
    if ($content -match '(?ms)^## Acknowledgments.*?(\n##|\z)') {
        $acknowledgeSection = $Matches[0] -replace '(\n##|\z)', ''
    }
    $mainContent += "`n" + $acknowledgeSection
}

# Save to the standard README
$mainContent = $mainContent.Trim()
$mainContent | Set-Content -Path $standardReadme -Encoding UTF8

Write-Host "README files synchronized successfully!" -ForegroundColor Green
Write-Host "Source: $githubReadme" -ForegroundColor Cyan
Write-Host "Target: $standardReadme" -ForegroundColor Cyan

# Optionally, add this script to git hooks to auto-sync on commit
$gitHooksDir = "d:\repos\RVToolsMerge\.git\hooks"
if (Test-Path $gitHooksDir) {
    Write-Host "To automate README synchronization, consider adding this script to git hooks:" -ForegroundColor Yellow
    Write-Host "  - Copy to pre-commit hook: $gitHooksDir\pre-commit" -ForegroundColor Yellow
}
