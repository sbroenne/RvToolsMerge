# GitHub Configuration

This directory contains GitHub-specific configuration files and workflows for the RVToolsMerge project.

## Workflows

The following GitHub Actions workflows are configured:

-   **build.yml**: Main build workflow that builds and tests the project
-   **codeql.yml**: Code quality analysis using GitHub's CodeQL
-   **dependency-review.yml**: Reviews dependencies for security vulnerabilities
-   **dotnet.yml**: Continuous integration for .NET code changes
-   **labeler.yml**: Automatically adds labels to pull requests
-   **release.yml**: Creates releases with artifacts
-   **security-alert-notification.yml**: Sends notifications for security alerts
-   **version-management.yml**: Manages semantic versioning for the project

## Dependabot

The **dependabot.yml** file configures automated dependency updates for:

-   GitHub Actions workflows
-   NuGet packages

## Contributing

When modifying GitHub workflows or adding new ones, please:

1. Document the workflow purpose and trigger conditions
2. Test the workflow with a draft PR before finalizing
3. Update this README.md file with any new additions
4. Consider workflow efficiency to minimize resource usage
5. Share workflow run artifacts when useful

For more information about contributing to the project, see [CONTRIBUTING.md](../CONTRIBUTING.md) in the root directory.
