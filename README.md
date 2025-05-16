# RVTools Excel Merger

[![.NET Build and Test](https://github.com/sbroenne/RVToolsMerge/actions/workflows/dotnet.yml/badge.svg)](https://github.com/sbroenne/RvToolsMerge/actions/workflows/dotnet.yml)
[![CodeQL](https://github.com/sbroenne/RVToolsMerge/actions/workflows/codeql.yml/badge.svg)](https://github.com/sbroenne/RVToolsMerge/actions/workflows/codeql.yml)
[![GitHub Advanced Security](https://img.shields.io/badge/GitHub%20Advanced%20Security-enabled-brightgreen)](SECURITY.md)

A .NET console application that merges multiple RVTools Excel files into a single consolidated file. This tool helps combine multiple RVTools exports for consolidated reporting and analysis. It also allows anonymization of key columns (e.g. VM names)

## Features

- Merges all RVTools Excel files (XLSX format) from a specified folder into one consolidated file
- Extracts data from the following sheets:
  - vInfo
  - vHost
  - vPartition
- Only includes columns that exist in all files for each respective sheet
- Validates that all required sheets exist in each file
- Configurable input folder and output file
- Option to ignore missing optional sheets (vHost and vPartition)
- Option to skip files that don't contain required sheets
- Option to anonymize VM, DNS, Cluster, Host, and Datacenter names (replaces with generic names like vm1, dns1, host1, etc.)
- Fast processing with minimal memory footprint

## Requirements

- .NET 9.0 or later
- ClosedXML library (automatically installed via NuGet)

## Installation

### Option 1: Download the latest release (Recommended)
1. Go to the [Releases](https://github.com/sbroenne/RVToolsMerge/releases) page
2. Download the latest `RVToolsMerge.zip` file
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

## Usage

```
RVToolsMerge [options] [inputFolder] [outputFile]
```

### Options
- `-h`, `--help`, `/?`: Show help message and exit
- `-m`, `--ignore-missing-optional-sheets`: Ignore missing optional sheets (vHost & vPartition)
- `-i`, `--skip-invalid-files`: Skip files that don't contain required sheets instead of failing
- `-a`, `--anonymize`: Anonymize VM, DNS Name, Cluster, Host, and Datacenter names with generic identifiers

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

4. Skipping validation for vHost and vPartition sheets:
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

7. Combining options:
```
RVToolsMerge -a -m C:\RVTools\Data C:\Reports\Anonymized_RVTools.xlsx
```

Options `-m` and `-i` cannot be used together as they have contradictory behaviors.

## Error Handling

By default, the application validates that all required sheets (vInfo, vHost, vPartition) exist in each file before processing. If any file is missing a required sheet, the application will display an error message and exit.

When using the `-m` option, only the vInfo sheet is required, and optional sheets (vHost and vPartition) can be missing with warnings shown.

When using the `-i` option, files that don't contain the required sheets will be skipped rather than causing the application to exit. The application will report which files were skipped and why.

## Development

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

The project is already configured for single-file publishing. You can publish a standalone Windows executable using:

```
dotnet publish -c Release
```

This will produce a self-contained, single-file executable with the following settings (from the project file):
- Self-contained (no .NET installation required)
- Single file deployment
- Optimized with ReadyToRun compilation
- Windows x64 runtime

The output will be located in the `bin\Release\net9.0\win-x64\publish` folder.

For other platforms, edit the `RuntimeIdentifier` in the .csproj file before publishing:
- Windows ARM64: `win-arm64`
- Linux x64: `linux-x64`
- macOS x64: `osx-x64`
- macOS ARM64: `osx-arm64`

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
