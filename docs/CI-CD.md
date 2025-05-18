# CI/CD Workflows for RVToolsMerge

This document describes the GitHub Actions workflows used for continuous integration and continuous deployment in the RVToolsMerge project.

## Overview

The CI/CD system in RVToolsMerge consists of multiple workflows that handle different aspects of the development lifecycle:

1. Continuous Integration
2. Release Management
3. Quality Control
4. Security Monitoring
5. Dependency Management

## Main Workflows

### 1. Build and Test (dotnet.yml)

This workflow runs on every push to the main branch and on pull requests. It:
- Builds the application across multiple platforms
- Runs unit tests
- Performs code quality checks
- Enforces code style and formatting rules

### 2. Release Management (release.yml)

This workflow is triggered when:
- A tag is pushed in the format `vX.Y.Z`
- Manually triggered with a specified version

Features:
- Creates release builds for multiple platforms (Windows x64/ARM64, Linux, macOS)
- Generates checksums for all artifacts
- Creates a GitHub release with all binaries
- Supports pre-release designation
- Updates version information in the project file

### 3. Version Management (version-management.yml)

This workflow handles semantic versioning:
- Allows incrementing the major, minor, or patch version
- Supports adding pre-release suffix
- Automatically creates tags and triggers the release workflow
- Updates version information in all relevant files

### 4. Code Quality Analysis (code-quality-analysis.yml)

Weekly automated analysis that:
- Performs detailed code analysis with .NET analyzers
- Generates code coverage reports
- Checks for code formatting issues
- Produces metrics on code quality

### 5. Security Workflows

Multiple security-focused workflows:
- `codeql.yml`: Static code analysis for security vulnerabilities
- `nuget-vulnerability-scan.yml`: Checks for vulnerable packages
- `dependency-review.yml`: Reviews dependencies in PRs
- `secret-scanning.yml`: Prevents accidental exposure of secrets

### 6. Dependency Management

- `dependabot.yml`: Configuration for automated dependency updates
- `dependabot-auto-merge.yml`: Automates approval and merging of safe updates

## Using the Workflows

### Creating a Release

1. **Automated Version Increment and Release**:
   - Go to the Actions tab
   - Select "Version Management"
   - Choose the version increment type (major/minor/patch)
   - Optionally add a pre-release suffix
   - Run workflow

2. **Manual Release Creation**:
   - Go to the Actions tab
   - Select "Release"
   - Enter a specific version number
   - Choose whether it's a pre-release
   - Run workflow

### Workflow Changes

**Note**: The workflow validation process that previously checked workflow files in pull requests has been removed from this project.

## Best Practices

- Always use the version management workflow for releasing new versions
- Monitor dependabot pull requests for dependency updates
- Review code quality analysis reports regularly
- Check security scans for potential vulnerabilities
