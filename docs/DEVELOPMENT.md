# Development Guide

This document provides information for developers who want to build, test, or contribute to RVToolsMerge.

## Technical Architecture

RVToolsMerge is built with .NET 10 and follows modern C# development practices:

### Key Components

-   **ClosedXML**: High-performance Excel file handling
-   **Spectre.Console**: Rich console UI with progress bars, tables, and colors
-   **Spectre.Console.Cli**: Command-line parsing and help generation framework
-   **Modern C# Features**: Using the latest C# features like records, pattern matching, and nullable reference types

## Building from Source

### Prerequisites

-   .NET 10.0 SDK or later

### Basic Build

```bash
git clone https://github.com/sbroenne/RVToolsMerge.git
cd RVToolsMerge
dotnet build -c Release
```

### Creating Deployable Packages

By default, `dotnet build` creates a framework-dependent build. For self-contained applications that don't require .NET runtime:

```bash
# For Windows x64
dotnet publish -c Release -r win-x64

# For Windows ARM64
dotnet publish -c Release -r win-arm64

# For Linux x64
dotnet publish -c Release -r linux-x64

# For macOS ARM64 (Apple Silicon)
dotnet publish -c Release -r osx-arm64
```

Output will be in `bin/Release/net10.0/{RID}/publish` directory, where `{RID}` is the runtime identifier.

### Development Workflow

When running from source with parameters, use a double-dash (`--`) to separate the `dotnet run` command from the application parameters:

```bash
# Basic syntax
dotnet run -- [options] [inputPath] [outputFile]

# Example
dotnet run -- -m -i C:\RVTools\Exports C:\Output\Merged.xlsx
```

## Development Standards

This project follows strict development standards:

-   **Coding Style**: C# coding best practices with PascalCase for public members, camelCase for private fields
-   **Documentation**: XML documentation for all public methods and classes
-   **Error Handling**: Robust exception handling and validation
-   **Code Coverage**: Comprehensive test coverage (currently 76%) with detailed reports - see [Code Coverage Documentation](code-coverage.md)
-   **Testing Approach**: Integration tests use real file system operations while unit tests use mocking for isolated component testing

## Continuous Integration

This project uses GitHub Actions for automated workflows:

| Workflow              | Purpose                                             |
| --------------------- | --------------------------------------------------- |
| **Build & Test**      | Validates code on every push and PR                 |
| **Code Quality**      | Static analysis and test coverage                   |
| **Security**          | CodeQL scanning and vulnerability checks            |
| **Version & Release** | Manual version bumps with optional release creation |
| **Dependencies**      | Automated dependency management with Dependabot     |
| **Workflow Cleanup**  | Automated cleanup of old workflow runs              |

Detailed CI/CD documentation is available in [continuous-integration.md](continuous-integration.md).

## Version Management

The project follows [Semantic Versioning](https://semver.org/):

-   **major** (1.0.0 → 2.0.0): Incompatible API changes
-   **minor** (1.0.0 → 1.1.0): New backward-compatible functionality
-   **patch** (1.0.0 → 1.0.1): Backward-compatible bug fixes

Version bumping and release creation are managed through a single GitHub Actions workflow that can be triggered manually. The workflow supports both version bumping only or version bumping with immediate release creation.

## Project Structure

The project follows standard GitHub repository best practices:

```
RVToolsMerge/
├── .github/                      # GitHub-specific files (workflows)
├── docs/                         # Documentation files
├── src/                          # Source code
│   └── RVToolsMerge/            # .NET project
│       ├── Configuration/       # Configuration settings
│       ├── Exceptions/          # Custom exception classes
│       ├── Models/              # Data models and DTOs
│       ├── Services/            # Service implementations
│       │   └── Interfaces/      # Service interfaces
│       └── UI/                  # User interface components
│           ├── Console/         # Console UI implementations
│           └── Interfaces/      # UI interfaces
├── tests/                        # Test projects
│   ├── RVToolsMerge.IntegrationTests/ # Integration tests using real file operations
│   └── RVToolsMerge.UnitTests/        # Unit tests with mocking for isolated testing
├── CHANGELOG.md                  # Version history
├── CONTRIBUTING.md               # Guidelines for contributors
├── LICENSE                       # MIT License file
├── README.md                     # This file
├── run.bat                       # Windows script to run the application
├── run.sh                        # Linux/macOS script to run the application
└── SECURITY.md                   # Security policy
```

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](../CONTRIBUTING.md) for detailed guidelines on how to contribute to this project.
