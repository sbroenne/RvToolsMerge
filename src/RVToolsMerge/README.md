# RVToolsMerge Source Code

This directory contains the source code for the RVToolsMerge application.

## Project Structure

```
RVToolsMerge/                 # Main project directory
├── Configuration/            # Configuration settings for the application
├── Exceptions/               # Custom exception classes
├── Input/                    # Sample input files for testing
├── Models/                   # Data models and DTOs
├── Output/                   # Directory for output files
├── Services/                 # Service implementations
│   └── Interfaces/           # Service interfaces
├── TestData/                 # Test data files
├── ApplicationRunner.cs      # Main application runner
├── Program.cs                # Entry point
└── RVToolsMerge.csproj       # Project file
```

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
