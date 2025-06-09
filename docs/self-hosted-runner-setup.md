# Self-Hosted Runner Configuration

## Overview

The RVToolsMerge build workflow has been configured to use a self-hosted runner specifically for Windows builds, while keeping Linux and macOS builds on GitHub-hosted runners. This setup allows for better control over the Windows build environment while maintaining cross-platform compatibility.

**📖 For complete runner deployment and setup instructions, see the [Azure GitHub Runner Deployment Guide](azure-github-runner-deployment.md).**

This document focuses on the GitHub Actions workflow configuration and variable setup. For infrastructure deployment, runner installation, and software requirements, use the Azure deployment guide.

## Configuration

### GitHub Variable Setup

To configure the self-hosted runner, you need to set up a GitHub repository variable:

1. Navigate to your repository on GitHub
2. Go to **Settings** → **Secrets and variables** → **Actions**
3. Click on the **Variables** tab
4. Click **New repository variable**
5. Set the following:
    - **Name**: `SELF_HOSTED_RUNNER`
    - **Value**: The name or label of your self-hosted runner (e.g., `codesign-runner`)

### Runner Selection Logic

The build workflow uses the following logic for runner selection:

-   **Windows builds** (`win-x64`, `win-arm64`): **REQUIRES** the self-hosted runner specified in `SELF_HOSTED_RUNNER` variable - builds will fail if not available
-   **Linux builds** (`linux-x64`, `linux-arm64`): Always uses `ubuntu-latest` (GitHub-hosted)
-   **macOS builds** (`osx-arm64`): Always uses `macos-latest` (GitHub-hosted)

## Testing the Configuration

To test your self-hosted runner configuration:

1. Set the `SELF_HOSTED_RUNNER` variable as described above
2. Trigger a build workflow (manually or via push/PR)
3. Check the workflow logs to verify that Windows builds are running on your self-hosted runner
4. Verify that Linux and macOS builds still run on GitHub-hosted runners

For detailed runner setup and configuration instructions, see the [Azure GitHub Runner Deployment Guide](azure-github-runner-deployment.md).

## Troubleshooting

### Common Issues

1. **Runner not found**: Ensure the `SELF_HOSTED_RUNNER` variable value exactly matches your runner's name/label
2. **Permissions issues**: Verify your self-hosted runner has appropriate permissions to access repository and build artifacts
3. **Missing runner variable**: If the `SELF_HOSTED_RUNNER` variable is not set or empty, Windows builds will fail

For detailed troubleshooting of runner setup and installation issues, see the [Azure GitHub Runner Deployment Guide](azure-github-runner-deployment.md).
