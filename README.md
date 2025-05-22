﻿# RVToolsMerge

[![.NET Build and Test](https://github.com/sbroenne/RVToolsMerge/actions/workflows/dotnet.yml/badge.svg)](https://github.com/sbroenne/RVToolsMerge/actions/workflows/dotnet.yml)
[![CodeQL](https://github.com/sbroenne/RVToolsMerge/actions/workflows/codeql.yml/badge.svg)](https://github.com/sbroenne/RVToolsMerge/actions/workflows/codeql.yml)
[![GitHub Advanced Security](https://img.shields.io/badge/GitHub%20Advanced%20Security-enabled-brightgreen)](SECURITY.md)

A modern, cross-platform console application for merging one or multiple [RVTools](https://www.robware.net/rvtools/) export files into a single consolidated file. Supports anonymization of sensitive data.

> Created by Stefan Broenner (github.com/sbroenne) and contributors

## Overview

RVToolsMerge is a powerful command-line utility designed to consolidate multiple RVTools exports into one Excel file, making it easier to analyze VMware environment data from different sources.

Built with .NET 9, this cross-platform tool efficiently processes individual RVTools Excel files or entire directories, intelligently merging data from key sheets while ensuring consistency across the consolidated output.

As an independent, open-source project, RVToolsMerge is *not affiliated with* the official RVTools application but works with its export data to provide additional functionality.

## Data Protection & Security

RVToolsMerge treats security and data protection as top priorities, especially when handling sensitive VMware infrastructure data:

### Key Data Protection Features

- **Complete Anonymization**: Automatically replace sensitive identifiers with generic equivalents:
  - VM names → vm1, vm2, vm3...
  - DNS names → dns1, dns2, dns3...
  - IP addresses → ip1, ip2, ip3...
  - Cluster names → cluster1, cluster2...
  - Host names → host1, host2...
  - Datacenter names → datacenter1, datacenter2...

- **Minimal Data Exposure**: The `--only-mandatory-columns` option limits data to essential fields only, preventing unnecessary exposure of sensitive information

- **Consistent Anonymization**: Maintains relationships between data points even after anonymization (same VM names are consistently replaced with the same anonymized values across all sheets)

- **Source Tracking Control**: Optional inclusion of source file information gives you control over data lineage visibility

These protection features make RVToolsMerge ideal for:
- Creating sanitized reports for vendors and consultants
- Sharing infrastructure data while maintaining confidentiality
- Generating documentation that doesn't expose sensitive internal naming
- Ensuring compliance with organization data-sharing policies

## Features

- **High-Performance Processing**: Built with .NET 9 and optimized for speed and memory efficiency
- **Rich Console Experience**: Beautiful console output with progress bars, status indicators, and colorful tables
- **Intelligent Sheet Handling**: Combines multiple RVTools exports while validating required sheets and columns
- **Flexible Processing Options**:
  - Processes single files or entire directories of RVTools exports
  - Includes only columns that appear in all source files
  - Optionally ignores missing optional sheets
  - Selectively includes only mandatory columns
  - Adds source file tracking for data lineage
- **Cross-Platform**: Runs on Windows, Linux, and macOS with native binaries for each platform
- **Enterprise-Grade Error Handling**: Comprehensive validation with clear, actionable error messages

## Supported Sheets and Required Columns

RVToolsMerge processes the following key sheets from RVTools exports:

### Required and Optional Sheets

| Sheet Name | Status | Description |
|------------|--------|-------------|
| **vInfo** | **Required** | Core VM information (CPU, memory, OS) |
| **vHost** | Optional | ESXi host configuration and performance data |
| **vPartition** | Optional | VM disk partition information |
| **vMemory** | Optional | VM memory configuration details |

The `vInfo` sheet must be present in all processed files. The other sheets are considered optional and can be handled according to your configuration.

### Mandatory Columns by Sheet

Each sheet has specific required columns that must be present for proper processing:

#### vInfo Sheet (Required)
- Template
- SRM Placeholder  
- Powerstate
- VM
- CPUs
- Memory
- In Use MiB
- OS according to the configuration file

#### vHost Sheet (Optional)
- Host
- Datacenter
- Cluster
- CPU Model
- Speed
- \# CPU
- Cores per CPU
- \# Cores
- CPU usage %
- \# Memory
- Memory usage %

#### vPartition Sheet (Optional)
- VM
- Disk
- Capacity MiB
- Consumed MiB

#### vMemory Sheet (Optional)
- VM
- Size MiB
- Reservation

## Column Name Mappings

The application standardizes column names across different RVTools exports. This helps handle variations in column naming between different versions of RVTools.

For a complete reference of all column mappings, see [Column Mappings Documentation](docs/ColumnMappings.md).

## Installation

Download the latest release for your platform from the [Releases page](https://github.com/sbroenne/RVToolsMerge/releases):

- Windows (x64): `RVToolsMerge-windows-Release.zip`
- Windows (ARM64): `RVToolsMerge-windows-arm64-Release.zip`
- Linux (x64): `RVToolsMerge-linux-Release.zip`
- macOS (ARM64): `RVToolsMerge-macos-arm64-Release.zip`

### Prerequisites

- No additional prerequisites required! The application is published as a self-contained executable with all dependencies included.
- Minimum disk space: ~30MB


## Usage

```
RVToolsMerge [options] inputPath [outputFile]
```

### Arguments

| Argument | Description | Default |
|----------|-------------|---------|
| `inputPath` | Path to an Excel file or folder containing RVTools exports | **Required** |
| `outputFile` | Path where the merged file will be saved | `./RVTools_Merged.xlsx` in current directory |

### Options

| Option | Description |
|--------|-------------|
| `-h, --help, /?` | Show the help message and exit |
| `-m, --ignore-missing-sheets` | Process files even when optional sheets are missing |
| `-i, --skip-invalid-files` | Skip files that don't meet validation requirements |
| `-a, --anonymize` | Anonymize VM, DNS, Cluster, Host, and Datacenter names |
| `-M, --only-mandatory-columns` | Include only mandatory columns in the output |
| `-s, --include-source` | Add a 'Source File' column to track data origin |
| `-e, --skip-empty-values` | Skip rows with empty values in mandatory columns |
| `-d, --debug` | Show detailed error information with stack traces |

### Advanced Options

```cmd
:: Skip files with missing optional sheets
RVToolsMerge.exe -m C:\RVTools\Exports

:: Skip invalid files entirely
RVToolsMerge.exe -i C:\RVTools\Exports

:: Skip invalid files and ignore missing optional sheets in valid files
RVToolsMerge.exe -i -m C:\RVTools\Exports

:: Anonymize sensitive data
RVToolsMerge.exe -a C:\RVTools\Exports\RVTools.xlsx C:\Reports\Anonymized.xlsx

:: Include only mandatory columns
RVToolsMerge.exe -M C:\RVTools\Exports

:: Include source file information
RVToolsMerge.exe -s C:\RVTools\Exports

:: Skip rows with empty mandatory values
RVToolsMerge.exe -e C:\RVTools\Exports

:: Combine multiple options
RVToolsMerge.exe -a -M -s -e C:\RVTools\Exports C:\Reports\Complete_Analysis.xlsx
```

## Anonymization and Data Protection

RVToolsMerge offers robust data protection features for handling sensitive infrastructure information:

### Anonymization (-a, --anonymize)

When using the anonymization option, the following data is consistently anonymized:

- VM names → vm1, vm2, vm3, etc.
- DNS names → dns1, dns2, dns3, etc.
- IP addresses → ip1, ip2, ip3, etc.
- Cluster names → cluster1, cluster2, cluster3, etc.
- Host names → host1, host2, host3, etc.
- Datacenter names → datacenter1, datacenter2, datacenter3, etc.

Anonymization maintains internal data relationships, ensuring that the same original value always maps to the same anonymized value throughout all sheets, preserving data integrity while protecting sensitive information.

### Mandatory Columns Only Mode (-M, --only-mandatory-columns)

The mandatory columns only mode provides an additional layer of data protection by:

- Including only essential columns required for analysis
- Excluding potentially sensitive columns that might contain organization-specific information
- Reducing the data footprint in the output file
- Limiting exposure of non-essential infrastructure details

This option is particularly useful when:
- Sharing reports with external parties
- Creating documentation for public consumption
- Generating simplified reports focused on specific metrics
- Ensuring compliance with data sharing policies

### Combined Data Protection

For maximum data protection, combine multiple options:

```
RVToolsMerge -a -M -s /path/to/inputs /path/to/output.xlsx
```

This produces a streamlined report with only essential columns containing fully anonymized data while preserving all analytical value and relationships between data points. The `-s` option adds source tracking for better data lineage.

## Validation Behavior and Error Handling

RVToolsMerge implements robust validation to ensure data integrity:

### Validation Rules

- **By default**: All required sheets with all mandatory columns must exist in all files
- **With `-m` (ignore-missing-sheets)**: 
  - The vInfo sheet remains required in all files
  - Optional sheets (vHost, vPartition, vMemory) can be missing
  - Missing mandatory columns in optional sheets will cause errors unless handled by other options
- **With `-i` (skip-invalid-files)**:
  - Files without the required vInfo sheet will be skipped
  - Files with vInfo sheet but missing mandatory vInfo columns will be skipped
  - Files with optional sheets having missing mandatory columns will be skipped
- **With `-e` (skip-empty-values)**:
  - Rows with empty values in mandatory columns will be skipped from the output
  - Without this option, rows with empty mandatory values are included (default behavior)
  - Useful when you want to only include complete data records
- **With both `-i` and `-m` together**:
  - Files without vInfo sheet will be skipped
  - Files with vInfo sheet but missing mandatory vInfo columns will be skipped
  - Files with complete vInfo sheet but missing optional sheets will be processed
  - Files with optional sheets missing mandatory columns will be processed, but those sheets may be excluded

## Sample Files

The project includes sample files in the `input` directory:

- `default.xlsx`: Standard RVTools export format with all required sheets and columns
- `altenativeColumnNames.xlsx`: Example file with alternative column naming variations

These files can be used to test the application and understand the expected format of RVTools exports.

## Troubleshooting

If you encounter issues while using RVToolsMerge:

| Issue | Recommended Action |
|-------|-------------------|
| General errors | Enable debug mode with `-d` to see detailed error information |
| Missing sheets errors | Use `-m` to ignore missing optional sheets |
| Files causing validation errors | Use `-i` to skip invalid files and continue processing others |
| Row count mismatches | By default, rows with empty mandatory values are included; use `-e` to skip them |
| Low memory issues | Process smaller batches of files |
| Permission errors | Ensure you have write access to the output folder |
| Excel file locked | Close any applications that might have the file open |
| Slow performance | Check for antivirus scanning; consider excluding the working folders |

## Technical Architecture

RVToolsMerge is built with .NET 9 and follows modern C# development practices:

### Key Components

- **ClosedXML**: High-performance Excel file handling
- **Spectre.Console**: Rich console UI with progress bars, tables, and colors
- **Modern C# Features**: Using the latest C# features like records, pattern matching, and nullable reference types


## Building from Source

### Prerequisites

- .NET 9.0 SDK or later

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

Output will be in `bin/Release/net9.0/{RID}/publish` directory, where `{RID}` is the runtime identifier.

### Development Workflow

When running from source with parameters, use a double-dash (`--`) to separate the `dotnet run` command from the application parameters:

```bash
# Basic syntax
dotnet run -- [options] [inputPath] [outputFile]

# Example
dotnet run -- -m -i C:\RVTools\Exports C:\Output\Merged.xlsx
```

## Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Standards

This project follows strict development standards:

- **Coding Style**: C# coding best practices with PascalCase for public members, camelCase for private fields
- **Documentation**: XML documentation for all public methods and classes
- **Error Handling**: Robust exception handling and validation


### Continuous Integration

This project uses GitHub Actions for automated workflows:

| Workflow | Purpose |
|----------|---------|
| **Build & Test** | Validates code on every push and PR |
| **Code Quality** | Static analysis and test coverage |
| **Security** | CodeQL scanning and vulnerability checks |
| **Release** | Creates versioned releases with artifacts |
| **Dependencies** | Automated dependency management with Dependabot |

Detailed CI/CD documentation is available in [CI-CD.md](/docs/CI-CD.md).

### Version Management

The project follows [Semantic Versioning](https://semver.org/):

- **major** (1.0.0 → 2.0.0): Incompatible API changes 
- **minor** (1.0.0 → 1.1.0): New backward-compatible functionality
- **patch** (1.0.0 → 1.0.1): Backward-compatible bug fixes

Version bumping is managed through GitHub Actions workflows.

## Security

This project takes security seriously, with multiple layers of protection:

### GitHub Advanced Security Features

| Feature | Status |
|---------|--------|
| CodeQL Analysis | ✅ Enabled |
| Dependency Review | ✅ Enabled |
| Secret Scanning | ✅ Enabled |
| Dependabot Alerts | ✅ Enabled |
| Security Policy | ✅ [View Policy](SECURITY.md) |

### Security Practices

- **Automated Scanning**: Regular code scanning for vulnerabilities
- **Dependency Management**: Automated updates for security patches
- **Secure Coding**: Following established security best practices
- **Vulnerability Reporting**: Clear process for reporting security issues

For details on reporting security vulnerabilities, please see our [Security Policy](SECURITY.md).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [RVTools](https://www.robware.net/rvtools/) by Robware - This independent project works with data exported from RVTools but is not affiliated with, endorsed by, or connected to Robware or the official RVTools product
- [ClosedXML](https://github.com/ClosedXML/ClosedXML) for excellent Excel file handling capabilities
- [Spectre.Console](https://spectreconsole.net/) for beautiful console output and UX
- [GitHub Copilot](https://github.com/features/copilot) and [Claude 3.7 Sonnet](https://www.anthropic.com/) for AI assistance in code generation and development

