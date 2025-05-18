# CI/CD Documentation for RVToolsMerge

This document outlines the continuous integration and continuous deployment (CI/CD) processes used in the RVToolsMerge project.

## Workflow Overview

RVToolsMerge uses GitHub Actions for all CI/CD pipelines. The main workflows are:

1. **Build and Test** - Triggered on pull requests and pushes to main branch for code changes
2. **Version Management** - Manual workflow for incrementing version numbers with automatic PR creation
3. **Release** - Triggered when a version tag is pushed, builds and publishes releases
4. **Security Scanning** - Includes CodeQL analysis, dependency review, and vulnerability scanning
5. **Auto Labeling** - Automatically adds labels to pull requests based on file changes

## Build Workflow

The Build workflow (`build.yml`) is a reusable workflow that handles building and testing the application on all supported platforms:

- **Platforms**: Windows (x64, ARM64), Linux (x64), macOS (ARM64)
- **Usage**: Called by other workflows to ensure consistent builds
- **Features**:
  - Caches NuGet packages for faster builds
  - Builds the application in the specified configuration
  - Runs all tests with code coverage collection
  - When building in Release mode, publishes platform-specific self-contained executables

## .NET CI Workflow

The .NET CI workflow (`dotnet.yml`) is triggered on code changes:

- **Trigger**: Push or pull request that modifies .NET code files or project settings 
- **Platform**: Windows
- **Function**:
  - Builds the application in Debug configuration
  - Runs all tests with code coverage collection

## Version Management Workflow

The Version Management workflow (`version-management.yml`) handles systematic version increments:

- **Trigger**: Manual workflow dispatch
- **Inputs**:
  - `versionType`: Type of version increment (major, minor, patch)
- **Function**:
  1. Extracts current version from the project file
  2. Validates version components are within .NET limits (0-65535)
  3. Increments version based on specified type
  4. Updates both package version and assembly version
  5. Creates a PR with version changes
  6. Automatically approves and attempts to merge the PR
  7. Creates a version tag if the merge is successful

### Version Management Process

The automated version management process follows these steps:

1. Extract the current version number from the project file
2. Validate that all version components are valid integers within the .NET range (0-65535)
3. Increment the appropriate component based on the selected increment type
4. Update version elements in the project file:
   - `<Version>` (NuGet package version)
   - `<AssemblyVersion>` (Assembly metadata version)
   - `<FileVersion>` (File version for Windows)
5. Create a new branch for the version change
6. Commit the changes and push to the repository
7. Create a pull request with conventional commit format: "chore: bump version to X.Y.Z"
8. Auto-approve the PR to satisfy branch protection requirements
9. Attempt to auto-merge the PR with retry logic
10. Create and push a version tag (vX.Y.Z) once the PR is merged

## Release Workflow

The Release workflow (`release.yml`) handles the build and deployment process:

- **Trigger**: Push of a version tag (e.g., `v1.2.3`)
- **Function**:
  1. Builds the application in Release configuration
  2. Runs all tests
  3. Creates platform-specific single-file executables (Windows, Linux, macOS)
  4. Packages executables into platform-specific archives
  5. Creates a GitHub Release with the archives attached

## Release Process Guide

To create a new release:

1. Use the Version Management workflow to increment the version
2. The Release workflow will automatically build and publish the release

## CI/CD Security Considerations

- GitHub Actions permissions are carefully scoped for each workflow
- Secrets are managed through GitHub Secrets
- Build artifacts are created in a clean, isolated environment
- All dependencies are explicitly pinned to specific versions

## Troubleshooting

Common issues and solutions:

- **Version workflow fails**: Ensure csproj file exists and has proper version elements
- **Release workflow fails**: Verify tag format follows `v1.2.3` pattern exactly
- **Publication error**: Check for valid GitHub permissions and tokens
