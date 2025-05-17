# RVTools Excel Merger

[![.NET Build and Test](https://github.com/sbroenne/RVToolsMerge/actions/workflows/dotnet.yml/badge.svg)](https://github.com/sbroenne/RvToolsMerge/actions/workflows/dotnet.yml)
[![CodeQL](https://github.com/sbroenne/RVToolsMerge/actions/workflows/codeql.yml/badge.svg)](https://github.com/sbroenne/RVToolsMerge/actions/workflows/codeql.yml)
[![GitHub Advanced Security](https://img.shields.io/badge/GitHub%20Advanced%20Security-enabled-brightgreen)](SECURITY.md)

A .NET console application that merges multiple RVTools Excel files into a single consolidated file. This tool helps combine multiple RVTools exports for consolidated reporting and analysis. It also allows anonymization of key columns (e.g. VM names)

> **Note:** This project utilizes GitHub Copilot to assist with code development, maintenance, and documentation.

## Features

- Merges all RVTools Excel files (XLSX format) from a specified folder into one consolidated file
- Extracts data from the following sheets:
  - vInfo (required)
  - vHost (optional)
  - vPartition (optional)
  - vMemory (optional)
- Validates mandatory columns in each sheet:
  - vInfo: Template, SRM Placeholder, Powerstate, VM, CPUs, Memory, In Use MiB, OS according to the VMware Tools
  - vHost: Host, Datacenter, Cluster, CPU Model, Speed, # CPU, Cores per CPU, # Cores, CPU usage %, # Memory, Memory usage %
  - vPartition: VM, Disk, Disk Path, Capacity MiB, Consumed MiB
  - vMemory: VM, Size MiB, Reservation
- Only includes columns that exist in all files for each respective sheet
- Validates that all required sheets exist in each file
- Configurable input folder and output file
- Option to ignore missing optional sheets (vHost, vPartition, and vMemory)
- Option to skip files that don't contain required sheets
- Option to anonymize VM, DNS, Cluster, Host, and Datacenter names (replaces with generic names like vm1, dns1, host1, etc.)
- Option to export only mandatory columns for each sheet
- Fast processing with minimal memory footprint

## Installation

### Option 1: Download the latest release (Recommended)
1. Go to the [Releases](https://github.com/sbroenne/RVToolsMerge/releases) page
2. Download the latest version for your platform:
   - `RVToolsMerge-windows-Release.zip` - For Windows (x64)
   - `RVToolsMerge-windows-arm64-Release.zip` - For Windows on ARM devices
   - `RVToolsMerge-linux-Release.zip` - For Linux (x64)
   - `RVToolsMerge-macos-arm64-Release.zip` - For macOS on Apple Silicon (M1/M2/M3)
3. Extract the contents to a folder of your choice

### Option 2: Build from source
1. Clone the repository
   ```
   git clone https://github.com/sbroenne/RVToolsMerge.git
   ```
2. Navigate to the project directory
   ```
   cd RVToolsMerge
   ```
3. Build the project
   ```
   dotnet build
   ```

#### Build Requirements
- .NET 9.0 SDK or later
- ClosedXML library (automatically installed via NuGet)

## Usage

```
RVToolsMerge [options] [inputFolder] [outputFile]
```

### Options
- `-h`, `--help`, `/?`: Show help message and exit
- `-m`, `--ignore-missing-optional-sheets`: Ignore missing optional sheets (vHost, vPartition & vMemory)
- `-i`, `--skip-invalid-files`: Skip files that don't contain required sheets instead of failing
- `-a`, `--anonymize`: Anonymize VM, DNS Name, Cluster, Host, and Datacenter names with generic identifiers
- `-o`, `--only-mandatory-columns`: Include only the mandatory columns for each sheet in the output file

**Note:** The `-m` and `-i` options cannot be used together as they have contradictory behaviors.

### Parameters

- `inputFolder` (optional): Path to the folder containing RVTools Excel files. Defaults to "input" subfolder in the current directory.
- `outputFile` (optional): Path where the merged file will be saved. Defaults to "RVTools_Merged.xlsx" in the current directory.

### Examples

1. Using default parameters:
```
RVToolsMerge
```

2. Specifying an input folder:
```
RVToolsMerge C:\RVTools\Data
```

3. Specifying both input folder and output file:
```
RVToolsMerge C:\RVTools\Data C:\Reports\Merged_RVTools.xlsx
```

4. Skipping validation for vHost, vPartition, and vMemory sheets:
```
RVToolsMerge -m C:\RVTools\Data
```

5. Skipping files that don't contain all required sheets:
```
RVToolsMerge -i C:\RVTools\Data
```

6. Anonymizing all sensitive names:
```
RVToolsMerge -a C:\RVTools\Data C:\Reports\Anonymized_RVTools.xlsx
```

7. Including only mandatory columns:
```
RVToolsMerge -o C:\RVTools\Data C:\Reports\Mandatory_Columns.xlsx
```

8. Combining options:
```
RVToolsMerge -a -m -o C:\RVTools\Data C:\Reports\Anonymized_Mandatory_Columns.xlsx
```

Options `-m` and `-i` cannot be used together as they have contradictory behaviors.

## Error Handling

By default, the application validates that all required sheets (vInfo, vHost, vPartition, vMemory) exist in each file before processing. It also validates that all mandatory columns exist in each sheet. If any file is missing a required sheet or mandatory column, the application will display an error message and exit.

When using the `-m` option, only the vInfo sheet is required, and optional sheets (vHost, vPartition, and vMemory) can be missing with warnings shown. Mandatory column validation still applies to all sheets that exist.

When using the `-i` option, files that don't contain the required sheets or mandatory columns will be skipped rather than causing the application to exit. The application will report which files were skipped and why.

When using the `-o` option, only the mandatory columns for each sheet will be included in the output file, regardless of what other columns might be common across all files.

## Development

### Development Approach

This project follows modern .NET development practices and employs the following techniques:

- **Test-Driven Development**: Core functionality is covered by unit tests
- **CI/CD Automation**: Comprehensive GitHub Actions workflows for quality assurance
- **AI-Assisted Development**: GitHub Copilot is used to enhance code quality, generate boilerplate, and assist with documentation
- **Clean Code Principles**: Focus on readability, maintainability, and SOLID principles

### Prerequisites

- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) or [Visual Studio Code](https://code.visualstudio.com/)
- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

### Building the Project

Clone the repository and build the solution:

```
git clone https://github.com/sbroenne/RVToolsMerge.git
cd RVToolsMerge
dotnet build
```

### Running the Tests

```
dotnet test
```

### CI/CD Pipeline

This project uses GitHub Actions for continuous integration and delivery:

| Workflow | Description |
|----------|-------------|
| Build and Test | Automatically builds and tests the application on every push and pull request |
| Release Creation | Automatically creates a release when a new tag is pushed |
| CodeQL Analysis | Security scanning for vulnerabilities |
| PR Validation | Additional validation for pull requests |
| Vulnerability Scan | Weekly security scanning with CodeQL |
| Secret Scanning | Identifies committed secrets and credentials |
| Dependency Review | Reviews dependency changes in pull requests |
| NuGet Vulnerability Scan | Checks for vulnerable NuGet packages |
| SBOM Generation | Creates Software Bill of Materials for releases |
| Auto Labeler | Automatic labeling of PRs based on files changed |
| Stale Management | Marks and closes stale issues and PRs |

## Publishing a Standalone Executable

The project is configured for multi-platform, single-file publishing. You can publish standalone executables for different platforms using:

```
dotnet publish -c Release
```

When run without additional parameters, this command produces a Windows x64 executable by default. The executable will be available at:

```
bin\Release\net9.0\win-x64\publish\RVToolsMerge.exe
```

The GitHub Actions workflow automatically builds the following platform versions when a release is created:

- Windows x64: Standard Windows desktop/server environments
- Windows ARM64: Windows on ARM devices (Surface Pro X, etc.)
- Linux x64: Linux desktop/server environments
- macOS ARM64: Apple Silicon Macs (M1, M2, M3, etc.)

These builds are available as separate downloadable artifacts from the GitHub release page.

### Manual Publishing for Specific Platforms

You can also manually publish for specific platforms by specifying the runtime identifier:

```
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

# Windows ARM64
dotnet publish -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

# macOS ARM64 (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

Each build will be output to its respective folder under `bin\Release\net9.0\[runtime-id]\publish\`.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- [RVTools](https://www.robware.net/rvtools/) - The excellent tool that creates the Excel files this application is designed to merge
- [ClosedXML](https://github.com/ClosedXML/ClosedXML) - .NET library for reading, manipulating and writing Excel files
- [GitHub Copilot](https://github.com/features/copilot) - AI pair programmer that assisted in code development and maintenance
- [Claude 3.7 Sonnet](https://www.anthropic.com/claude) - AI assistant that created most of the code for this project

## Development

### Recommended VS Code Extensions

This project includes a set of recommended VS Code extensions to enhance your development experience. When you open this repository in VS Code, you will be prompted to install these extensions. Alternatively, you can view and install them from the Extensions view by filtering on "Recommended".

Key extensions include:

- **C# Dev Kit** (ms-dotnettools.csdevkit): Comprehensive C# development tools
- **C#** (ms-dotnettools.csharp): Language support for C#
- **.NET Runtime Install Tool** (ms-dotnettools.vscode-dotnet-runtime): Install .NET runtime dependencies
- **EditorConfig** (editorconfig.editorconfig): Maintain consistent coding styles
- **Code Spell Checker** (streetsidesoftware.code-spell-checker): Spelling checker for source code
- **Coverage Gutters** (ryanluker.vscode-coverage-gutters): Display code coverage in the editor
- **GitHub Actions** (github.vscode-github-actions): GitHub Actions workflows support

For the full list, see the `.vscode/extensions.json` file.
