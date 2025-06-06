# Winget Manifest Automation

This directory contains templates and scripts for automatically generating Windows Package Manager (winget) manifests for RVToolsMerge releases.

## Overview

The winget manifest generation is fully automated as part of the release process. When a new version is released via the version management workflow, winget manifests are automatically generated with the correct version numbers, download URLs, and SHA256 hashes.

## Files

### Templates

- `RvToolsMerge.RvToolsMerge.yaml.template` - Version manifest template
- `RvToolsMerge.RvToolsMerge.installer.yaml.template` - Installer manifest template  
- `RvToolsMerge.RvToolsMerge.locale.en-US.yaml.template` - Locale manifest template

### Scripts

- `../scripts/generate-winget-manifests.ps1` - PowerShell script that processes templates and generates final manifests

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

- **Package Information**: Name, version, publisher details
- **Installer Details**: Download URLs pointing to GitHub releases, SHA256 hashes, installer type (WiX MSI)
- **Application Metadata**: Description, license, tags, documentation links
- **Installation Configuration**: Silent install support, upgrade behavior, file associations

## Package ID

The winget package uses the ID: `RvToolsMerge.RvToolsMerge`

## Winget Submission

The generated manifests can be submitted to the Microsoft winget-pkgs repository for community distribution. The manifests are designed to be compliant with winget submission requirements.

## Manual Usage

If you need to generate manifests manually, you can run the script directly:

```powershell
.\.github\scripts\generate-winget-manifests.ps1 -Version "1.3.4" -X64MsiPath "path\to\x64.msi" -Arm64MsiPath "path\to\arm64.msi" -OutputDir "output"
```

## Template Variables

The templates use the following placeholder variables:

- `{{VERSION}}` - Package version number
- `{{X64_SHA256}}` - SHA256 hash of x64 MSI installer  
- `{{ARM64_SHA256}}` - SHA256 hash of ARM64 MSI installer
- `{{RELEASE_NOTES}}` - Brief description of changes in the release

## ProductCode

The MSI installer uses a fixed ProductCode (`F3E4D5C6-B7A8-9C0D-1E2F-3A4B5C6D7E8F`) to ensure compatibility with winget package management and proper upgrade handling.