# RVToolsMerge Source Code

This directory contains the source code for the RVToolsMerge application.

## Project Structure

```
RVToolsMerge/                 # Main project directory
├── Configuration/            # Configuration settings for the application
├── Exceptions/               # Custom exception classes
├── Models/                   # Data models and DTOs
├── Services/                 # Service implementations
│   └── Interfaces/           # Service interfaces
├── UI/                       # User interface components
│   ├── Console/              # Console-specific UI implementations
│   └── Interfaces/           # UI interface definitions
├── ApplicationRunner.cs      # Main application runner
├── Program.cs                # Entry point
└── RVToolsMerge.csproj       # Project file
```

## Key Components

-   **ApplicationRunner**: Coordinates the overall application flow and command handling
-   **Services**:
    -   **MergeService**: Core functionality for merging RVTools Excel files
    -   **ExcelService**: Handles Excel file operations using ClosedXML
    -   **ValidationService**: Validates RVTools export files for required sheets and columns
    -   **AnonymizationService**: Provides data anonymization capabilities
    -   **ConsoleUIService**: Handles rich console output using Spectre.Console
    -   **CommandLineParser**: Parses command-line arguments and options

## Building and Running

From this directory:

```bash
# Build the project
dotnet build

# Run the project
dotnet run [options] inputPath [outputFile]
```

Or use the convenience scripts in the root directory:

```bash
# Windows
..\..\run.bat [options] inputPath [outputFile]

# Linux/macOS
../../run.sh [options] inputPath [outputFile]
```

For more detailed documentation, see the [README.md](../../README.md) in the root directory.
