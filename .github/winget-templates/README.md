# Winget Manifest Automation

This directory contains templates and scripts for automatically generating Windows Package Manager (winget) manifests for RVToolsMerge releases.

## Overview

The winget manifest generation is fully automated as part of the release process. When a new version is released via the version management workflow, winget manifests are automatically generated with the correct version numbers, download URLs, and SHA256 hashes.

## Files

### Templates

-   `RvToolsMerge.RvToolsMerge.yaml.template` - Version manifest template
-   `RvToolsMerge.RvToolsMerge.installer.yaml.template` - Installer manifest template
-   `RvToolsMerge.RvToolsMerge.locale.en-US.yaml.template` - Locale manifest template

### Scripts

-   `../scripts/generate-winget-manifests.ps1` - PowerShell script that processes templates and generates final manifests. **Manifest validation with `winget` is now always performed automatically after generation. The `-ValidateWithWinget` switch is no longer supported.**

## Automation Process

The automation works as follows:

1. **Release Workflow Trigger**: When a new version is released via the version management workflow
2. **MSI Creation**: Windows MSI installers are built for x64 and ARM64 architectures
3. **Hash Calculation**: SHA256 hashes are calculated for both MSI files
4. **Template Processing**: The PowerShell script processes the templates, replacing placeholders with actual values:
    - `{{VERSION}}` - The release version (e.g., "1.3.4")
    - `{{X64_SHA256}}` - SHA256 hash of the x64 MSI file
    - `{{ARM64_SHA256}}` - SHA256 hash of the ARM64 MSI file
    - `{{RELEASE_NOTES}}` - Brief release notes for the version
5. **Artifact Upload**: Generated manifests are uploaded as workflow artifacts
6. **Release Inclusion**: Manifests are included in the GitHub release for easy access

## Generated Manifests

The generated manifests follow the winget manifest specification v1.6.0 and include:

-   **Package Information**: Name, version, publisher details
-   **Installer Details**: Download URLs pointing to GitHub releases, SHA256 hashes, installer type (WiX MSI)
-   **Application Metadata**: Description, license, tags, documentation links
-   **Installation Configuration**: Silent install support, upgrade behavior, file associations

## Package ID

The winget package uses the ID: `RvToolsMerge.RvToolsMerge`

## Winget Submission

The generated manifests can be submitted to the Microsoft winget-pkgs repository for community distribution. The manifests are designed to be compliant with winget submission requirements.

### Automated Submission Preparation

A dedicated GitHub Action workflow (`winget-submission.yml`) is available to automate the submission process:

1. **Manual Trigger**: Go to Actions → "Winget Submission Preparation" and run the workflow
2. **Release Selection**: Specify a release tag or use the latest release
3. **Dry Run Option**: Test the process without creating branches
4. **Fork Management**: Automatically creates branches in your winget-pkgs fork

#### Workflow Features

-   Downloads manifests from GitHub releases automatically
-   Validates manifest syntax and completeness
-   Creates properly structured branches in your fork repository
-   Generates submission information and PR templates
-   Supports both dry-run and live submission modes

#### Running the Workflow

**Option 1: Latest Release**

```
Go to Actions → Winget Submission Preparation → Run workflow
Leave release tag empty to use latest release
```

**Option 2: Specific Release**

```
Go to Actions → Winget Submission Preparation → Run workflow
Enter release tag (e.g., "v1.3.4")
```

**Option 3: Dry Run**

```
Go to Actions → Winget Submission Preparation → Run workflow
Check "Dry run" to prepare without creating branches
```

### Manual Submission Preparation

You can also use the PowerShell script for manual preparation:

```powershell
# Prepare submission for latest release
.\.github\scripts\prepare-winget-submission.ps1

# Prepare for specific version
.\.github\scripts\prepare-winget-submission.ps1 -Version "1.3.4"

# Dry run mode
.\.github\scripts\prepare-winget-submission.ps1 -DryRun
```

### Fork Repository

The submission process uses the fork: `https://github.com/sbroenne/winget-pkgs`

### Submission Process

1. **Preparation**: Run the winget submission workflow or script
2. **Review**: Check the generated manifests and submission information
3. **Create PR**: Use the provided link to create a pull request to microsoft/winget-pkgs
4. **Follow Up**: Monitor the community review process

## Manual Manifest Generation

If you need to generate manifests manually, you can run the generation script directly:

```powershell
.\.github\scripts\generate-winget-manifests.ps1 -Version "1.3.4" -X64MsiPath "path\to\x64.msi" -Arm64MsiPath "path\to\arm64.msi" -OutputDir "output"
```

> **Note:** Manifest validation with `winget` is always performed after generation. The script will fail if validation does not pass.

## Template Variables

The templates use the following placeholder variables:

-   `{{VERSION}}` - Package version number
-   `{{X64_SHA256}}` - SHA256 hash of x64 MSI installer
-   `{{ARM64_SHA256}}` - SHA256 hash of ARM64 MSI installer
-   `{{RELEASE_NOTES}}` - Brief description of changes in the release

## ProductCode Handling

The MSI installer uses an auto-generated ProductCode (`ProductCode="*"`) that changes with each version to ensure proper upgrade handling. The winget manifests do not specify ProductCode fields, allowing winget to automatically detect the ProductCode from the MSI file during installation. This approach ensures compatibility with WiX's auto-generated ProductCodes while maintaining proper upgrade behavior through the stable UpgradeCode (`A7B8C9D0-E1F2-4A5B-8C9D-0E1F2A5B8C9D`).

## Version Consistency

**IMPORTANT**: For winget submission, the PackageVersion in the manifest **must match** the MSI installer's ProductVersion. 

The project uses the following version configuration:
- **Package Version** (in `.csproj`): 3-part semantic version (e.g., `1.4.2`)
- **File Version** (in `.csproj`): 4-part version (e.g., `1.4.2.0`)
- **MSI ProductVersion**: Binds to FileVersion via `!(bind.FileVersion.RVToolsMerge.exe)`

According to Windows Installer and winget requirements:
- MSI installers effectively use only the **first 3 parts** of the version number (major.minor.build)
- The 4th part (revision) is ignored by Windows Installer
- The winget manifest generation script automatically:
  - Extracts the ProductVersion from MSI files
  - Normalizes it to a 3-part version
  - Validates it matches the provided version from the git tag
  - Issues warnings if mismatches are detected

This ensures version consistency across:
- Git release tags (e.g., `v1.4.2`)
- MSI installer ProductVersion (normalized to `1.4.2`)
- Winget manifest PackageVersion (`1.4.2`)
- DisplayVersion in Apps and Features (`1.4.2`)
