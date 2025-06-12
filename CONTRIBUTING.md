# Contributing to RVToolsMerge

First off, thank you for considering contributing to RVToolsMerge! It's people like you that make this tool better for everyone.

Following these guidelines helps to communicate that you respect the time of the developers managing and developing this open source project. In return, they should reciprocate that respect in addressing your issue, assessing changes, and helping you finalize your pull requests.

## Getting Started

### Prerequisites

-   [.NET 9 SDK](https://dotnet.microsoft.com/download) or later
-   An IDE that supports C# (Visual Studio Code with C# extension, JetBrains Rider, or any preferred editor)
-   Basic knowledge of C# and .NET
-   Familiarity with Git and GitHub workflow

### Setting Up Development Environment

1. Fork the repository on GitHub
2. Clone your fork locally:
    ```
    git clone https://github.com/yourusername/RVToolsMerge.git
    ```
3. Add the original repository as a remote to keep your fork in sync:
    ```
    git remote add upstream https://github.com/sbroenne/RVToolsMerge.git
    ```
4. Create a branch for your changes:
    ```
    git checkout -b feature/your-feature-name
    ```

## Repository Structure

The repository follows standard GitHub practices:

```
RVToolsMerge/
├── .github/           # GitHub-specific files (workflows)
├── docs/              # Documentation files
├── src/               # Source code
│   └── RVToolsMerge/  # .NET project
├── CHANGELOG.md       # Version history
├── CONTRIBUTING.md    # Guidelines for contributors (this file)
├── LICENSE            # MIT License file
├── README.md          # Main documentation
├── run.bat            # Windows script to run the application
├── run.sh             # Linux/macOS script to run the application
└── SECURITY.md        # Security policy
```

Understanding this structure will help you navigate the codebase and contribute effectively.

## Development Workflow

1. Make your changes in your feature branch
2. **Build Verification**: Run `dotnet build` and ensure the solution builds successfully with zero errors
3. **Test Verification**: Run `dotnet test` and ensure all tests pass with zero failures
4. Commit your changes (see Commit Guidelines below)
5. Push to your fork and submit a pull request

### Pre-Commit Checklist

Before committing any changes, ensure you have completed all of these steps:

-   [ ] Code builds successfully: `dotnet build`
-   [ ] All tests pass: `dotnet test`
-   [ ] Code follows the project's coding standards
-   [ ] **Documentation has been updated (MANDATORY)** - Check if any of the following need updates:
    -   [ ] `README.md` for feature changes, usage instructions, or project structure
    -   [ ] `CONTRIBUTING.md` for development process changes
    -   [ ] `docs/` directory files for specialized documentation
    -   [ ] XML documentation comments for public APIs
    -   [ ] Online help system (console application's built-in help in `ConsoleUIService.ShowHelp()`)
    -   [ ] Project structure documentation if files/folders changed
    -   [ ] `installer/README.md` for WiX installer or MSI-related changes
    -   [ ] Winget manifest templates if installer changes affect package manager integration
-   [ ] Commit message follows the guidelines below

**Note**: Documentation updates are mandatory for changes affecting public APIs, configuration, user workflows, project structure, testing approach, or any user-facing functionality.

## Coding Standards

### Naming Conventions

-   **PascalCase** for:

    -   Class names
    -   Interface names (with `I` prefix)
    -   Public members
    -   Constants
    -   Type parameters

-   **camelCase** for:

    -   Local variables
    -   Parameters
    -   Private fields (with underscore `_` prefix)

-   Use meaningful, descriptive names that represent purpose
-   Avoid abbreviations unless widely recognized
-   Use verb-noun pairs for method names (e.g., `GetData`, `ProcessFile`)
-   Use nouns for property names
-   Include 'Async' suffix for async methods

### Code Formatting

-   Use 4 spaces for indentation (not tabs)
-   Keep line length reasonable (< 120 characters)
-   Use parentheses to clarify code intent even when optional
-   Add spaces after keywords (if, for, while) but not after method names
-   Use proper XML documentation comments for public methods, classes, and interfaces
-   Keep methods focused on single responsibilities with < 30 lines when possible

### C# Best Practices

-   Prefer LINQ for data manipulation when appropriate
-   Use proper exception handling with specific exception types
-   Validate all inputs, especially file paths and user inputs
-   Use expression-bodied members for simple one-line methods and properties
-   Organize using directives alphabetically and remove unused ones
-   Use file-scoped namespaces
-   Explicitly specify access modifiers (public, private, etc.)
-   Use `var` when the type is obvious from the right side of assignment
-   Use the latest C# language features (pattern matching, nullable reference types, etc.)
-   Embrace `null` handling with nullable reference types
-   Use pattern matching for type checking and data extraction
-   Prefer init-only properties for immutable data
-   Leverage records for data objects, especially DTOs
-   Prefer string interpolation over concatenation

### Excel Processing Guidelines

-   Use ClosedXML for Excel file manipulation
-   Be aware that ClosedXML is single-threaded
-   Implement thread synchronization when multiple threads need access to ClosedXML operations
-   Implement proper data validation for RVTools-specific data formats
-   Handle Excel-specific exceptions gracefully (e.g., file not found, invalid format)

### UI Development with Spectre.Console

When working with console UI using Spectre.Console:

-   Always use `AnsiConsole.Write` instead of `AnsiConsole.Markup` for better performance
-   Use `Live` displays for dynamic content that changes frequently
-   Utilize `Prompt<T>` for type-safe user input with validation
-   Implement proper exception handling via `SafeExecution` extensions
-   Avoid repeated color markup in loops by pre-building strings
-   Use the Status API for long-running operations with indeterminate progress
-   Leverage word wrapping for better text display on different terminal sizes
-   Implement proper graceful exit with `IDisposable` patterns for live displays
-   Create rich, colorful tables for displaying data
-   Use progress bars for long-running operations
-   Display file trees for directory navigation
-   Use FigletText for application branding/header

### Testing

The project maintains comprehensive test coverage (currently 76%) with two distinct test approaches:

-   **Unit Tests** (`RVToolsMerge.UnitTests`): Use mocking for isolated testing of individual components and methods
-   **Integration Tests** (`RVToolsMerge.IntegrationTests`): Use real data and actual file system operations rather than mocking:
    -   Create temporary test files and directories for each test scenario
    -   Use actual Excel files with realistic RVTools data structures
    -   Perform real file I/O operations to validate end-to-end functionality
    -   Clean up test files and directories after test completion

Additional testing requirements:

-   Write tests for all core functionality
-   Test with various RVTools export versions and formats
-   Include edge cases like malformed files, missing data, and extremely large datasets
-   All tests must pass before any commit (`dotnet test` must show zero failures)

## Windows Installer and Package Management

### WiX Toolset Guidelines

-   Use WiX Toolset 6.0.1 for creating MSI installers
-   Follow WiX 6 syntax and schema (v4/wxs namespace)
-   Use auto-generated ProductCode (`ProductCode="*"`) to enable proper version upgrades
-   Maintain stable UpgradeCode across all versions for upgrade detection
-   Use automatic version binding from executable: `!(bind.FileVersion.RVToolsMerge.exe)`
-   Include proper upgrade logic with `<MajorUpgrade>` element
-   Add application to PATH environment variable for command-line access
-   Integrate with Add/Remove Programs using proper registry properties
-   Include license agreement display when License.rtf is present
-   Use WixUI_Minimal for clean, professional installation experience

### MSI Best Practices

-   Create MSI files for both x64 and ARM64 Windows architectures
-   Name MSI files with version and architecture: `RVToolsMerge-{version}-{runtime}.msi`
-   Ensure MSI files support silent installation for package managers
-   Test MSI installation, upgrade, and uninstallation scenarios
-   Validate MSI files using Windows SDK tools when available
-   Include proper file versioning and metadata in the executable
-   Consider code signing for production releases to avoid security warnings

### Windows Package Manager (winget) Integration

-   Maintain winget manifest templates in `.github/winget-templates/`
-   Use auto-generated ProductCode in MSI for proper upgrade handling
-   Ensure stable UpgradeCode across versions for package upgrade detection
-   Include proper publisher information matching winget manifest
-   Generate SHA256 hashes automatically for winget manifest integrity
-   Test winget manifest syntax before release
-   Follow winget manifest schema v1.6.0 specifications
-   Include comprehensive package metadata (description, license, tags, documentation links)
-   Design MSI for silent installation compatibility with winget

## Pull Request Process

1. Update the README.md or documentation with details of changes if appropriate
2. Update the CHANGELOG.md with details of changes
3. The PR should work for Windows, macOS, and Linux (cross-platform compatibility)
4. Ensure all automated checks pass
5. PRs require approval from at least one maintainer before merging

## Commit Guidelines

-   Use the present tense ("Add feature" not "Added feature")
-   Use the imperative mood ("Move cursor to..." not "Moves cursor to...")
-   Limit the first line to 72 characters or less
-   Reference issues and pull requests after the first line
-   Consider using conventional commits format:
    -   feat: A new feature
    -   fix: A bug fix
    -   docs: Documentation only changes
    -   style: Changes that do not affect the meaning of the code
    -   refactor: A code change that neither fixes a bug nor adds a feature
    -   perf: A code change that improves performance
    -   test: Adding missing tests or correcting existing tests
    -   chore: Changes to the build process or auxiliary tools

## Communication

-   Use GitHub Issues for bug reports and feature requests
-   Use GitHub Discussions for questions and discussions
-   For sensitive matters, contact the maintainers directly

## License

By contributing, you agree that your contributions will be licensed under the project's MIT License.
