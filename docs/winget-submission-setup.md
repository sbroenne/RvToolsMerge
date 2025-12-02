# Winget Submission Workflow Setup

## Overview

The winget submission workflow automates submitting RVToolsMerge to the Microsoft Windows Package Manager (winget) repository.

## Prerequisites

### 1. Fork Repository Setup

1. Create a fork of `microsoft/winget-pkgs` at `https://github.com/sbroenne/winget-pkgs`
2. Keep the fork synchronized with the upstream `master` branch

### 2. Authentication Setup

1. Create a personal access token at: https://github.com/settings/tokens
2. Grant the token `repo` scope (Full control of repositories)
3. **Add the token as a repository secret in the RVToolsMerge repository**:
    - Go to `https://github.com/sbroenne/RvToolsMerge/settings/secrets/actions`
    - Click "New repository secret"
    - Name: `WINGET_SUBMISSION_TOKEN`
    - Value: Your personal access token

**Important**: The secret must be created in the RVToolsMerge repository (where the workflow runs), not in the winget-pkgs fork.

### 3. Dependencies

-   A GitHub release must exist with winget manifest files
-   The version management workflow must complete successfully to generate required manifests

## Required Manifest Files

The workflow expects these manifest files in the GitHub release:

-   `RvToolsMerge.RvToolsMerge.yaml` (version manifest)
-   `RvToolsMerge.RvToolsMerge.installer.yaml` (installer manifest)
-   `RvToolsMerge.RvToolsMerge.locale.en-US.yaml` (locale manifest)

## Running the Workflow

1. Navigate to Actions → Winget Submission Preparation
2. Select "Run workflow"
3. Configure parameters:
    - `releaseTag`: Specific release tag (e.g., "v1.3.4") or leave empty for latest
    - `dryRun`: Set to `true` for testing, `false` for actual submission

## Testing

Always test with `dryRun: true` before making actual submissions to verify:

-   Fork is accessible
-   Manifests download correctly
-   Git operations work properly

## After Successful Run

1. Check workflow artifacts for submission information
2. Use the provided PR creation URL to submit to Microsoft
3. Follow Microsoft's winget community review process

## Troubleshooting

-   Ensure the fork exists and is up-to-date
-   Verify the personal access token has correct permissions
-   Check that manifest files are present in the specified release
-   Review workflow logs for specific error messages
-   **ProductCode Issues**: If winget validation fails with ProductCode errors, ensure the installer manifest template does not specify hardcoded ProductCode values. WiX installers with `ProductCode="*"` should not have ProductCode fields in winget manifests
-   **MSI Validation Failures**: Verify that MSI files are properly built and signed before manifest generation, and that SHA256 hashes match the actual files
-   **Version Mismatch Issues**: The winget PackageVersion **must match** the MSI installer's ProductVersion. The automation ensures this by:
    -   Extracting ProductVersion from MSI files during manifest generation
    -   Normalizing to 3-part semantic version (MSI uses only major.minor.build)
    -   Validating against the release tag version
    -   Issuing warnings if mismatches are detected

## Version Consistency Requirements

**Critical for Winget Submission**: The package version in the winget manifest must exactly match the MSI installer's ProductVersion to avoid installation and upgrade issues.

### How Version Consistency is Maintained

1. **Project Configuration**:
   - Package Version: 3-part semantic version (e.g., `1.4.2`) in `.csproj`
   - File Version: 4-part version (e.g., `1.4.2.0`) in `.csproj`
   - MSI ProductVersion: Binds to FileVersion via `!(bind.FileVersion.RVToolsMerge.exe)`

2. **MSI Version Behavior**:
   - Windows Installer uses only the first 3 parts (major.minor.build)
   - The 4th part (revision) is ignored
   - Effective ProductVersion: `1.4.2` (normalized from `1.4.2.0`)

3. **Automated Validation**:
   - Manifest generation script extracts ProductVersion from MSI files
   - Normalizes to 3-part version for comparison
   - Validates against release tag version (e.g., `v1.4.2` → `1.4.2`)
   - Fails if x64 and ARM64 versions don't match
   - Warns if MSI version doesn't match provided version

4. **Integration Tests**:
   - `WingetVersionConsistencyTests` validates version alignment across:
     - Project file versions (Package, File, Assembly)
     - WiX configuration (version binding)
     - Winget manifest templates (placeholders)
     - Version management workflow (3-part vs 4-part handling)
