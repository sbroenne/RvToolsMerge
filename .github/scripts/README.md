# GitHub Scripts

This directory contains PowerShell scripts used by GitHub Actions workflows and for manual operations.

## Scripts

### generate-winget-manifests.ps1

Generates Windows Package Manager (winget) manifests from templates during the release process.

**Usage:**

```powershell
.\generate-winget-manifests.ps1 -Version "1.3.4" -X64MsiPath "path\to\x64.msi" -Arm64MsiPath "path\to\arm64.msi" -OutputDir "output"
```

**Parameters:**

-   `Version` - Package version (required)
-   `X64MsiPath` - Path to x64 MSI installer (required)
-   `Arm64MsiPath` - Path to ARM64 MSI installer (required)
-   `OutputDir` - Output directory for generated manifests (required)
-   `ReleaseNotes` - Release notes text (optional)
-   `ValidateWithWinget` - Switch to enable winget validation (optional)

**Called by:** `version-management.yml` workflow during release process

**Examples:**

```powershell
# Basic usage
.\generate-winget-manifests.ps1 -Version "1.3.4" -X64MsiPath "path\to\x64.msi" -Arm64MsiPath "path\to\arm64.msi" -OutputDir "output"
```

```powershell
# With custom release notes
.\generate-winget-manifests.ps1 -Version "1.3.4" -X64MsiPath "path\to\x64.msi" -Arm64MsiPath "path\to\arm64.msi" -OutputDir "output" -ReleaseNotes "Bug fixes and improvements"
```

```powershell
# Enable winget validation
.\generate-winget-manifests.ps1 -Version "1.3.4" -X64MsiPath "path\to\x64.msi" -Arm64MsiPath "path\to\arm64.msi" -OutputDir "output" -ValidateWithWinget
```

### prepare-winget-submission.ps1

Prepares winget manifest submission by downloading manifests from GitHub releases and optionally creating a branch in the winget-pkgs fork.

**Usage:**

```powershell
# Prepare submission for latest release
.\prepare-winget-submission.ps1

# Prepare for specific version
.\prepare-winget-submission.ps1 -Version "1.3.4"

# Dry run mode (no branch creation)
.\prepare-winget-submission.ps1 -DryRun

# Custom repository
.\prepare-winget-submission.ps1 -Repository "owner/repo" -Version "1.3.4"
```

**Parameters:**

-   `Version` - Version to prepare submission for (optional, uses latest if not specified)
-   `Repository` - GitHub repository in format "owner/repo" (optional, auto-detected)
-   `ForkUrl` - URL of winget-pkgs fork (optional, defaults to sbroenne/winget-pkgs)
-   `OutputDir` - Output directory (optional, defaults to "winget-submission")
-   `DryRun` - Only prepare files locally, don't create branch (optional)
-   `GitHubToken` - GitHub API token (optional, uses GITHUB_TOKEN environment variable)

**Called by:** `winget-submission.yml` workflow and manual operations

**Requirements:**

-   Git must be installed and available in PATH
-   GitHub token must be available (GITHUB_TOKEN environment variable or -GitHubToken parameter)
-   Fork repository must exist and be accessible

## Environment Variables

Both scripts may use the following environment variables:

-   `GITHUB_TOKEN` - GitHub API token for accessing releases and repositories

## Output

### generate-winget-manifests.ps1

Generates three winget manifest files:

-   `RvToolsMerge.RvToolsMerge.yaml` - Version manifest
-   `RvToolsMerge.RvToolsMerge.installer.yaml` - Installer manifest
-   `RvToolsMerge.RvToolsMerge.locale.en-US.yaml` - Locale manifest

### prepare-winget-submission.ps1

Creates a submission directory containing:

-   `manifests/` - Downloaded winget manifest files
-   `submission-info.md` - Submission information and instructions
-   `pr-template.md` - Pull request template
-   `release-notes.md` - Release notes from GitHub
-   `winget-fork/` - Cloned fork repository (if not dry run)

## Error Handling

Both scripts use `$ErrorActionPreference = "Stop"` and include comprehensive error handling for:

-   File operations
-   GitHub API calls
-   Git operations
-   Parameter validation

## Cross-Platform Compatibility

Scripts are designed to work on Windows, Linux, and macOS with PowerShell Core (pwsh).
