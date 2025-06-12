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
7. **Winget Submission** - Manual workflow for preparing and submitting packages to the Windows Package Manager repository

## Build Workflow

The Build workflow (`build.yml`) is a reusable workflow that handles building and testing the application on all supported platforms:

-   **Platforms**: Windows (x64, ARM64), Linux (x64), macOS (ARM64)
-   **Usage**: Called by other workflows to ensure consistent builds
-   **Features**:
    -   Caches NuGet packages for faster builds
    -   Builds the application in the specified configuration
    -   Runs all tests
    -   When building in Release mode, publishes platform-specific self-contained executables
    -   For Windows builds in Release mode, creates MSI installers using WiX Toolset 6.0.1
    -   Uploads both executable and MSI artifacts for release builds
    -   **Code signing support**: Optionally signs Windows executables and MSI installers when enabled

### Code Signing

Code signing is supported for Windows artifacts (executables and MSI installers) and is enabled only during release builds:

-   **Trigger**: Only enabled for release builds when `enableCodeSigning` parameter is set to `true`
-   **Runner**: Uses self-hosted runner with `codesign-runner` label when code signing is enabled
-   **Fallback**: Uses GitHub-hosted `windows-latest` runner when code signing is disabled
-   **Requirements**:
    -   Self-hosted Windows runner with signtool.exe from Windows SDK
    -   Code signing certificate installed in Windows Certificate Store
    -   Interactive user session (not Windows service) for certificate access
-   **Process**:
    1. Signs the published executable with SHA256 digest and timestamp
    2. Creates the MSI installer
    3. Signs the MSI installer with SHA256 digest and timestamp
    4. Verifies signatures for both executable and MSI
-   **Certificates**: Uses best available certificate from Windows Certificate Store (`/a` flag)
-   **Timestamping**: Uses DigiCert timestamp server for long-term signature validity

## .NET CI Workflow

The .NET CI workflow (`dotnet.yml`) is triggered on code changes:

-   **Trigger**: Push or pull request that modifies .NET code files or project settings
-   **Platform**: Windows
-   **Function**:
    -   Builds the application in Debug configuration
    -   Runs all tests

## Azure Infrastructure Requirements

For self-hosted runners and Azure deployments:

-   **Operating System**: Windows 11 Pro (23H2) only
-   **Platform Support**: Standardized on Windows 11 Pro for consistency and optimal Azure Hybrid Use Benefit licensing
-   **Azure Hybrid Use Benefit**: Enabled by default for up to 40% cost savings with proper Windows Client licensing

## Code Coverage Workflow

The Code Coverage workflow (`code-coverage.yml`) generates detailed coverage reports and badges:

-   **Trigger**: Push to main branch that modifies .NET code files or manual workflow dispatch
-   **Platform**: Windows
-   **Function**:
    -   Builds the application in Release configuration
    -   Runs all tests with code coverage collection
    -   Generates HTML coverage reports and badges
    -   Publishes coverage badge to GitHub Pages
    -   Makes reports available as workflow artifacts

## Workflow Cleanup

The Workflow Cleanup workflow (`cleanup-workflow-runs.yml`) maintains repository hygiene by automatically removing old workflow runs:

-   **Trigger**:
    -   Scheduled daily at 2 AM UTC
    -   Manual workflow dispatch with optional parameters
-   **Platform**: Ubuntu
-   **Parameters** (manual dispatch only):
    -   `keep_runs`: Number of runs to keep for each workflow (default: 3)
    -   `retain_days`: Number of days to retain workflow runs (default: 30)
-   **Function**:
    -   Keeps only the latest runs of each workflow (configurable via manual trigger)
    -   Removes runs older than the specified retention period (configurable via manual trigger)
    -   Cleans up runs with all conclusion states (cancelled, failure, success, skipped)
    -   Provides summary of cleanup actions performed
-   **Benefits**:
    -   Reduces repository storage usage
    -   Improves workflow run browsing performance
    -   Maintains relevant run history while removing clutter
    -   Configurable retention settings for different cleanup scenarios

## Winget Submission Workflow

The Winget Submission workflow (`winget-submission.yml`) automates the preparation and submission of RVToolsMerge to the Windows Package Manager repository:

-   **Trigger**: Manual workflow dispatch only
-   **Inputs**:
    -   `releaseTag`: Specific release tag to process (e.g., "v1.3.4") or empty for latest release
    -   `dryRun`: Set to `true` for testing without creating branches (default: false)
-   **Platform**: Ubuntu
-   **Prerequisites**:
    -   Fork of `microsoft/winget-pkgs` repository at `https://github.com/sbroenne/winget-pkgs`
    -   Personal access token with `repo` scope stored as `WINGET_SUBMISSION_TOKEN` repository secret
    -   GitHub release with required winget manifest files
-   **Function**:
    -   Downloads winget manifests from the specified or latest GitHub release
    -   Validates manifest files and YAML syntax
    -   Creates submission branch in the forked winget-pkgs repository
    -   Prepares complete submission information and PR template
    -   Provides direct links for creating pull requests to Microsoft's repository
-   **Required Manifest Files**:
    -   `RvToolsMerge.RvToolsMerge.yaml` (version manifest)
    -   `RvToolsMerge.RvToolsMerge.installer.yaml` (installer manifest)
    -   `RvToolsMerge.RvToolsMerge.locale.en-US.yaml` (locale manifest)
-   **Artifacts Generated**:
    -   Validated winget manifests
    -   Submission information with package details
    -   Pull request template for winget community submission
    -   Release notes extracted from GitHub release

## Version Management & Release Workflow

The Version Management & Release workflow (`version-management.yml`) handles systematic version increments and optional release creation:

-   **Trigger**: Manual workflow dispatch only
-   **Inputs**:
    -   `versionType`: Type of version increment (major, minor, patch)
    -   `createRelease`: Whether to create a GitHub release after version bump (default: true)
-   **Function**:
    1. Extracts current version from the project file
    2. Validates version components are within .NET limits (0-65535)
    3. Increments version based on specified type
    4. Updates both package version and assembly version
    5. Creates a PR with version changes
    6. Automatically approves and attempts to merge the PR
    7. Creates a version tag if the merge is successful
    8. If `createRelease` is true:
        - Builds release artifacts for all platforms
        - Creates platform-specific archives
        - Creates a GitHub Release with the archives attached

### Version Management & Release Process

The automated version management and release process follows these steps:

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
11. If release creation is enabled:
    - Build release artifacts for all supported platforms (Windows x64/ARM64, Linux x64, macOS ARM64) with code signing enabled
    - Sign Windows executables and MSI installers using the self-hosted runner
    - Create MSI installers for Windows platforms using WiX Toolset 6.0.1
    - Generate winget manifests with calculated MSI SHA256 hashes
    - Create platform-specific ZIP archives
    - Create GitHub Release with release notes, download links, and winget manifests

## Release Process Guide

To create a new release:

1. Use the Version Management & Release workflow to increment the version
2. Choose whether to create a release immediately by setting `createRelease` to true
3. The workflow will automatically build and publish the release if enabled

To submit to Windows Package Manager (winget):

1. Ensure a GitHub release exists with winget manifest files
2. Use the Winget Submission workflow with the desired release tag
3. Test with `dryRun: true` first to validate the submission
4. Run without dry run to create the submission branch
5. Use the provided PR creation link to submit to Microsoft's winget-pkgs repository

## Code Coverage in Releases

Code coverage is now handled exclusively by the dedicated code-coverage.yml workflow to optimize workflow performance while maintaining comprehensive test coverage reporting.

## MSI Installer and WiX Integration

The project includes automated MSI installer generation for Windows platforms using WiX Toolset 6.0.1:

### MSI Build Process

1. **Automatic Trigger**: MSI installers are built during Release configuration builds on Windows runners
2. **WiX Installation**: The build workflow automatically installs WiX Toolset 6.0.1 and required extensions
3. **Version Extraction**: MSI version is automatically extracted from the compiled executable
4. **Multi-Architecture**: Creates MSI files for both x64 and ARM64 Windows architectures
5. **File Naming**: MSI files follow the pattern `RVToolsMerge-{version}-{runtime}.msi`

### Winget Integration

The release process includes automated Windows Package Manager (winget) manifest generation:

1. **Template Processing**: Uses templates in `.github/winget-templates/` to generate manifests
2. **SHA256 Calculation**: Automatically calculates and includes MSI file hashes
3. **Version Synchronization**: Ensures winget package version matches release version
4. **Artifact Upload**: Generated manifests are included in GitHub releases
5. **Community Distribution**: Manifests can be submitted to Microsoft's winget-pkgs repository

### Manual MSI Build

For local development and testing, MSI installers can be built manually:

```powershell
# Install WiX Toolset
dotnet tool install --global wix --version 6.0.1

# Publish application
dotnet publish src/RVToolsMerge/RVToolsMerge.csproj --configuration Release --runtime win-x64 --self-contained true --output publish

# Build MSI
cd installer
wix build RVToolsMerge.wxs -define PublishDir="../publish" -out "RVToolsMerge-win-x64.msi" -ext WixToolset.UI.wixext
```

## CI/CD Security Considerations

-   GitHub Actions permissions are carefully scoped for each workflow
-   Secrets are managed through GitHub Secrets
-   Build artifacts are created in a clean, isolated environment
-   All dependencies are explicitly pinned to specific versions

## Troubleshooting

Common issues and solutions:

-   **Version workflow fails**:
    -   Ensure csproj file exists and has proper version elements (`<Version>`, `<AssemblyVersion>`, `<FileVersion>`)
    -   Check that the version format is valid (X.Y.Z format)
    -   Verify that version components don't exceed .NET assembly version limits (0-65535)
    -   Check workflow logs for detailed error messages with validation steps
-   **Release creation fails**:
    -   Verify build artifacts are properly generated and version format is correct
    -   Ensure MSI creation succeeded on Windows runners using WiX Toolset
    -   Check winget manifest generation completed successfully
    -   Verify that all expected platforms completed their builds
    -   Ensure MSI SHA256 hashes were calculated correctly for winget manifests
-   **Publication error**:
    -   Check for valid GitHub permissions and tokens
    -   Verify that the repository allows Actions to create PRs and releases
    -   Ensure no existing PR conflicts with the version update branch
-   **Code coverage not found**: Ensure Windows x64 build completes successfully and generates coverage files
-   **Workflow cleanup fails**:
    -   Verify the GITHUB_TOKEN has sufficient permissions for Actions scope
    -   Check if any workflow runs are currently in progress (they cannot be deleted)
    -   Ensure the cleanup action has proper access to repository Actions
    -   Review retention settings if unexpected runs are being deleted
    -   Verify that custom retention day values are valid positive integers
-   **Winget submission fails**:
    -   Ensure the fork repository exists and is accessible (`https://github.com/sbroenne/winget-pkgs`)
    -   Verify the personal access token (`WINGET_SUBMISSION_TOKEN`) has correct permissions
    -   Check that all required manifest files are present in the GitHub release
    -   Validate manifest files have correct YAML syntax
    -   Ensure the release tag format is correct (e.g., "v1.3.4")
    -   Test with dry run first to identify issues before actual submission
-   **Branch creation fails**:
    -   Check for existing version update branches that may conflict
    -   Verify git configuration and push permissions
    -   Ensure the version increment didn't create duplicate branch names
-   **PR auto-merge fails**:
    -   Verify repository branch protection rules allow auto-merge
    -   Check that required status checks are configured correctly
    -   Ensure the GitHub token has sufficient permissions
