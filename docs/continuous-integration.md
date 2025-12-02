# CI/CD Documentation for RVToolsMerge

This document outlines the continuous integration and continuous deployment (CI/CD) processes used in the RVToolsMerge project.

## Workflow Overview

RVToolsMerge uses GitHub Actions for all CI/CD pipelines. The main workflows are:

1. **Build and Test** - Triggered on pull requests and pushes to main branch for code changes
2. **Version Management & Release** - Manual workflow for incrementing version numbers with automatic PR creation and optional release creation
3. **Code Coverage** - Generates code coverage reports and badges for the project
4. **Security Scanning** - Includes CodeQL analysis, dependency review, and vulnerability scanning
5. **Auto Labeling** - Automatically adds labels to pull requests based on file changes
6. **Workflow Cleanup** - Automatically cleans up old workflow runs to maintain repository hygiene

## Build Workflow

The Build workflow (`build.yml`) is a reusable workflow that handles building and testing the application on all supported platforms:

- **Platforms**: Windows (x64, ARM64), Linux (x64, ARM64), macOS (ARM64)
- **Usage**: Called by other workflows to ensure consistent builds
- **Features**:
  - Caches NuGet packages for faster builds
  - Builds the application in the specified configuration
  - Runs all tests
  - When building in Release mode, publishes platform-specific self-contained executables
  - Uploads build artifacts for release builds

## .NET CI Workflow

The .NET CI workflow (`dotnet.yml`) is triggered on code changes:

- **Trigger**: Push or pull request that modifies .NET code files or project settings
- **Platform**: Windows
- **Function**:
  - Builds the application in Debug configuration
  - Runs all tests

## Code Coverage Workflow

The Code Coverage workflow (`code-coverage.yml`) generates detailed coverage reports and badges:

- **Trigger**: Push to main branch that modifies .NET code files or manual workflow dispatch
- **Platform**: Windows
- **Function**:
  - Builds the application in Release configuration
  - Runs all tests with code coverage collection
  - Generates HTML coverage reports and badges
  - Publishes coverage badge to GitHub Pages
  - Makes reports available as workflow artifacts

## Workflow Cleanup

The Workflow Cleanup workflow (`cleanup-workflow-runs.yml`) maintains repository hygiene by automatically removing old workflow runs:

- **Trigger**:
  - Scheduled daily at 2 AM UTC
  - Manual workflow dispatch with optional parameters
- **Platform**: Ubuntu
- **Parameters** (manual dispatch only):
  - `keep_runs`: Number of runs to keep for each workflow (default: 3)
  - `retain_days`: Number of days to retain workflow runs (default: 30)
- **Function**:
  - Keeps only the latest runs of each workflow (configurable via manual trigger)
  - Removes runs older than the specified retention period (configurable via manual trigger)
  - Cleans up runs with all conclusion states (cancelled, failure, success, skipped)
  - Provides summary of cleanup actions performed
- **Benefits**:
  - Reduces repository storage usage
  - Improves workflow run browsing performance
  - Maintains relevant run history while removing clutter
  - Configurable retention settings for different cleanup scenarios

## Version Management & Release Workflow

The Version Management & Release workflow (`version-management.yml`) handles systematic version increments and optional release creation:

- **Trigger**: Manual workflow dispatch only
- **Inputs**:
  - `versionType`: Type of version increment (major, minor, patch)
  - `createRelease`: Whether to create a GitHub release after version bump (default: true)
- **Function**:
  1. Extracts current version from the project file
  2. Validates version components are within .NET limits (0-65535)
  3. Increments version based on specified type
  4. Updates both package version (3-part) and assembly version (4-part)
  5. Creates a PR with version changes
  6. Automatically approves and attempts to merge the PR
  7. Creates a version tag if the merge is successful
  8. If `createRelease` is true:
     - Builds release artifacts for all platforms
     - Creates platform-specific ZIP archives
     - Creates a GitHub Release with the archives attached

### Version Update Process

1. **Version Extraction**: Current version is read from `src/RVToolsMerge/RVToolsMerge.csproj`
2. **Version Calculation**:
   - Parse current 3-part version (e.g., `1.4.2`)
   - Increment appropriate part based on `versionType`
   - Generate 3-part package version (e.g., `1.4.3`)
   - Generate 4-part assembly version (e.g., `1.4.3.0`)
3. **Version Validation**:
   - Each component must be 0-65535 (within .NET limits)
   - Format must match `X.Y.Z` for package version
   - Assembly version is package version with `.0` appended
4. **File Updates**:
   - `<Version>` tag updated with 3-part version
   - `<AssemblyVersion>` and `<FileVersion>` updated with 4-part version
5. **Verification**: Changes are verified before creating PR

## Release Process Guide

To create a new release:

1. Use the Version Management & Release workflow to increment the version
2. Choose whether to create a release immediately by setting `createRelease` to true
3. The workflow will automatically build and publish the release if enabled

## CI/CD Security Considerations

- GitHub Actions permissions are carefully scoped for each workflow
- Secrets are managed through GitHub Secrets
- Build artifacts are created in a clean, isolated environment
- All dependencies are explicitly pinned to specific versions

## Troubleshooting

Common issues and solutions:

- **Version workflow fails**:
  - Ensure csproj file exists and has proper version elements (`<Version>`, `<AssemblyVersion>`, `<FileVersion>`)
  - Check that the version format is valid (X.Y.Z format)
  - Verify that version components don't exceed .NET assembly version limits (0-65535)
  - Check workflow logs for detailed error messages with validation steps
- **Release creation fails**:
  - Verify build artifacts are properly generated and version format is correct
  - Verify that all expected platforms completed their builds
- **Publication error**:
  - Check for valid GitHub permissions and tokens
  - Verify that the repository allows Actions to create PRs and releases
  - Ensure no existing PR conflicts with the version update branch
- **Code coverage not found**: Ensure Windows x64 build completes successfully and generates coverage files
- **Workflow cleanup fails**:
  - Verify the GITHUB_TOKEN has sufficient permissions for Actions scope
  - Check if any workflow runs are currently in progress (they cannot be deleted)
  - Ensure the cleanup action has proper access to repository Actions
  - Review retention settings if unexpected runs are being deleted
  - Verify that custom retention day values are valid positive integers
- **Branch creation fails**:
  - Check for existing version update branches that may conflict
  - Verify git configuration and push permissions
  - Ensure the version increment didn't create duplicate branch names
- **PR auto-merge fails**:
  - Verify repository branch protection rules allow auto-merge
  - Check that required status checks are configured correctly
  - Ensure the GitHub token has sufficient permissions
