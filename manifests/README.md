# RVToolsMerge Winget Manifests

This directory contains the Windows Package Manager (winget) manifest files for RVToolsMerge.

## Package Information

- **Package ID**: `RvToolsMerge.RvToolsMerge`
- **Publisher**: Stefan Broenner
- **Current Version**: 1.3.3

## Manifest Files

The winget manifest consists of four files:

1. **RvToolsMerge.RvToolsMerge.yaml** - Version manifest (main package info)
2. **RvToolsMerge.RvToolsMerge.locale.en-US.yaml** - Default locale manifest (descriptions, metadata)
3. **RvToolsMerge.RvToolsMerge.installer.yaml** - Installer manifest (installation details)

## Installation via Winget

Once published to the Microsoft Community Repository, users can install RVToolsMerge using:

```powershell
winget install RvToolsMerge.RvToolsMerge
```

Or search for it using:

```powershell
winget search RVToolsMerge
```

## Submission Process

To submit these manifests to the Microsoft winget-pkgs repository:

1. Fork the [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) repository
2. Copy the entire `manifests/r/RvToolsMerge/RvToolsMerge/1.3.3/` directory to the same path in winget-pkgs
3. Update the `InstallerSha256` values in the installer manifest with actual SHA256 hashes of the MSI files
4. Submit a pull request to the winget-pkgs repository

## Updating for New Versions

For new versions:

1. Create a new directory under `manifests/r/RvToolsMerge/RvToolsMerge/` with the new version number
2. Copy and update all manifest files with the new version information
3. Update installer URLs and SHA256 hashes
4. Submit to winget-pkgs repository

## MSI Requirements

The winget manifests reference MSI installers that must be:

- Available as GitHub release assets
- Properly signed (recommended for production)
- Built with consistent Product Codes and upgrade paths
- Accessible via HTTPS URLs

## Validation

Before submitting, validate the manifests using winget-create:

```powershell
winget-create validate manifests/r/RvToolsMerge/RvToolsMerge/1.3.3/
```

## Notes

- The manifests assume MSI files will be available in GitHub releases with predictable naming
- SHA256 hashes must be updated manually after each release
- The Product Code in the MSI must match the one in the installer manifest
- Consider using automated tools to update manifests for new releases