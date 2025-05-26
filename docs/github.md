# GitHub Configuration

This directory contains GitHub-specific configuration files and workflows for the RVToolsMerge project, which focuses on combining and processing multiple RVTools exports from VMware environments.

## Workflows

The following GitHub Actions workflows are configured:

-   **build.yml**: Main build workflow that builds and tests the project across multiple platforms (Windows, Linux, macOS)
-   **codeql.yml**: Code quality analysis using GitHub's CodeQL, with special focus on Excel data handling and processing
-   **dependency-review.yml**: Reviews dependencies for security vulnerabilities, particularly focusing on Excel libraries
-   **dotnet.yml**: Continuous integration for .NET 9 code changes, including Excel processing tests
-   **labeler.yml**: Automatically adds labels to pull requests based on affected components (UI, Excel processing, VMware data)
-   **version-management.yml**: Manages semantic versioning for the project with automated version bumps and optional release creation
-   **code-coverage.yml**: Standalone code coverage analysis and reporting

## Dependabot

The **dependabot.yml** file configures automated dependency updates for:

-   GitHub Actions workflows
-   NuGet packages

## GitHub Copilot Integration

RVToolsMerge leverages GitHub Copilot for development assistance:

-   **Copilot Agent**: Configured to assist with issues and pull requests
-   **Prompt Templates**: Standardized templates in `.github/copilot/templates/` for consistent code generation
-   **Coding Standards**: Enforces project-specific coding standards when generating suggestions

## Cross-Platform Testing

Workflows are configured to test on multiple platforms:

-   Windows: Primary development target
-   Linux: Server deployment target
-   macOS: Development environment support

## Contributing

When modifying GitHub workflows or adding new ones, please:

1. Document the workflow purpose and trigger conditions
2. Test the workflow with a draft PR before finalizing
3. Update this README.md file with any new additions
4. Consider workflow efficiency to minimize resource usage
5. Share workflow run artifacts when useful
6. Include test cases using sample RVTools exports when relevant
7. Ensure cross-platform compatibility testing is maintained

For more information about contributing to the project, see [CONTRIBUTING.md](../CONTRIBUTING.md) in the root directory.
