# CI/CD Workflows for RVToolsMerge

This document describes the GitHub Actions workflows used for continuous integration and continuous deployment in the RVToolsMerge project.

## Overview

The CI/CD system in RVToolsMerge consists of multiple workflows that handle different aspects of the development lifecycle:

1. Continuous Integration
2. Release Management
3. Version Management
4. Security Monitoring
5. Dependency Management

## Main Workflows

### 1. Build and Test (dotnet.yml)

This workflow runs on every push to the main branch and on pull requests. It:
- Builds the application across multiple platforms
- Runs unit tests
- Ignores changes to documentation files
- Uses the reusable build workflow that handles the actual build process

### 2. Build Workflow (build.yml)

This is a reusable workflow used by both the CI and release processes:
- Builds for multiple platforms: Windows (x64/ARM64), Linux, and macOS ARM64
- Runs tests with code coverage collection
- Creates self-contained, single-file executables for release builds
- Uploads build artifacts for later use in the release process
- Implements caching for NuGet packages to speed up builds

### 3. Release Management (release.yml)

This workflow is triggered when:
- A tag is pushed in the format `vX.Y.Z`
- Manually triggered with a specified version

Features:
- Creates release builds for multiple platforms (Windows x64/ARM64, Linux, macOS ARM64)
- Generates checksums for all artifacts
- Creates ZIP archives for each platform
- Creates a GitHub release with all binaries
- Supports pre-release designation
- Updates version information in the project file
- Generates release notes based on merged PRs
- Automatically commits version updates back to the main branch

### 4. Version Management (version-management.yml)

This workflow handles semantic versioning:
- Allows incrementing the major, minor, or patch version
- Supports adding pre-release suffix
- Automatically creates tags and triggers the release workflow
- Updates version information in the csproj file
- Validates version compatibility
- Creates Git tags for new versions

### 5. Security Workflows

Multiple security-focused workflows:

#### CodeQL Analysis (codeql.yml)
- Runs advanced code scanning for security vulnerabilities
- Scheduled to run weekly and on all pull requests and pushes to main
- Focuses on C# code quality and security issues
- Uses extended security and quality queries

#### Dependency Review (dependency-review.yml)
- Analyzes dependencies in pull requests
- Checks for known vulnerabilities in packages
- Fails on critical severity issues

#### Security Alert Notification (security-alert-notification.yml)
- Monitors results from all security workflows
- Sends notifications when security workflows fail
- Provides configurable alerting capabilities

## Using the Workflows

### Creating a Release

1. **Automated Version Increment and Release**:
   - Go to the Actions tab in GitHub repository
   - Select "Version Management" workflow
   - Click "Run workflow" button
   - Choose the version increment type (major/minor/patch)
   - Optionally add a pre-release suffix (e.g., "alpha", "beta", "rc1")
   - Click "Run workflow" to start the process
   - The workflow will automatically update version files, create a tag, and trigger the release process

2. **Manual Release Creation**:
   - Go to the Actions tab in GitHub repository
   - Select "Release" workflow
   - Click "Run workflow" button
   - Enter a specific version number in semver format (X.Y.Z)
   - Choose whether it's a pre-release using the dropdown
   - Click "Run workflow" to start the release process

### Handling Release Failures

If a release workflow fails:

1. Check the workflow logs for specific error messages
2. Common issues include:
   - Test failures preventing release
   - Version conflicts (tag already exists)
   - Insufficient permissions for the GitHub token
   - Build failures on specific platforms
3. After fixing the issue, you can:
   - Rerun the failed workflow
   - Delete the partially created tag (if applicable) and restart
   - Create a manual release if automated processes continue to fail

## Troubleshooting CI/CD Issues

### Common Problems and Solutions

1. **Workflow Permission Issues**:
   - Ensure repository permissions for GitHub Actions are configured correctly
   - Check that secrets are properly set up and accessible

2. **Failed Tests Blocking Release**:
   - Review test logs to identify and fix failing tests
   - Consider conditionally allowing releases with known test issues using workflow parameters

3. **Version Conflicts**:
   - If a tag already exists, either delete it or choose a different version
   - Use the version management workflow to avoid conflicts

4. **Platform-Specific Build Issues**:
   - Check logs for the specific platform that failed
   - Test locally using Docker to simulate the build environment
   - Consider platform-specific conditionals in the build process

For additional help, contact the repository maintainers or consult the GitHub Actions documentation.
