# RVTools Excel Merger

[![.NET Build and Test](https://github.com/sbroenne/RVToolsMerge/actions/workflows/dotnet.yml/badge.svg)](https://github.com/sbroenne/RVToolsMerge/actions/workflows/dotnet.yml)
[![CodeQL](https://github.com/sbroenne/RVToolsMerge/actions/workflows/codeql.yml/badge.svg)](https://github.com/sbroenne/RVToolsMerge/actions/workflows/codeql.yml)
[![GitHub Advanced Security](https://img.shields.io/badge/GitHub%20Advanced%20Security-enabled-brightgreen)](SECURITY.md)

A cross-platform utility to merge multiple RVTools Excel export files into a single consolidated file.

## Overview

RVToolsMerge is a powerful command-line tool designed to combine multiple RVTools exports into one Excel file, making it easier to analyze VMware environment data from different sources or time periods.

This utility can process individual RVTools Excel files or all files in a specified folder, extracting and merging data from key sheets while ensuring consistency across the merged output.

## Features

- Combines multiple RVTools exports into a single Excel file
- Validates required sheets and mandatory columns
- Includes only columns that appear in all source files
- Optionally anonymizes sensitive data (VMs, DNS names, Clusters, Hosts, Datacenters)
- Provides detailed progress feedback and summary reports
- Option to filter for only mandatory columns
- Comprehensive error handling with user-friendly messages
- Cross-platform support (Windows, Linux, macOS)
- Efficient memory usage for processing large files
- Option to include source file names for traceability
- Ability to process either a single Excel file or an entire directory of files

## Sheets and Required Columns

The tool processes the following sheets from RVTools exports:

### Required Sheets

| Sheet | Status | Description |
|-------|--------|-------------|
| **vInfo** | **Required** | Virtual machine information including CPU, memory, and OS details |

### Optional Sheets

| Sheet | Status | Description |
|-------|--------|-------------|
| **vHost** | Optional | Host information including CPU, memory, and cluster details |
| **vPartition** | Optional | Virtual machine disk partition information |
| **vMemory** | Optional | Virtual machine memory configuration information |

At minimum, the `vInfo` sheet must be present in all Excel files being processed. The other sheets (`vHost`, `vPartition`, `vMemory`) are optional and can be skipped with the `--ignore-missing-optional-sheets` option.

### Required Columns by Sheet

Each sheet must contain certain mandatory columns for proper processing:

#### vInfo Sheet (Required)
- Template
- SRM Placeholder
- Powerstate
- VM
- CPUs
- Memory
- In Use MiB
- OS according to the VMware Tools

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

## Installation

### Download Pre-built Binaries

Download the latest release for your platform from the [Releases page](https://github.com/sbroenne/RVToolsMerge/releases):

- Windows (x64): `RVToolsMerge-windows-Release.zip`
- Windows (ARM64): `RVToolsMerge-windows-arm64-Release.zip`
- Linux (x64): `RVToolsMerge-linux-Release.zip`
- macOS (ARM64): `RVToolsMerge-macos-arm64-Release.zip`

### Building from Source

Prerequisites:
- .NET 9.0 SDK or later

```bash
git clone https://github.com/sbroenne/RVToolsMerge.git
cd RVToolsMerge
dotnet build -c Release
```

## Usage

```
RVToolsMerge [options] [inputPath] [outputFile]
```

### Arguments

- **inputPath**: Path to an Excel file or a folder containing RVTools Excel files. Defaults to the "input" subfolder in the current directory.
- **outputFile**: Path where the merged file will be saved. Defaults to "RVTools_Merged.xlsx" in the current directory.

### Options

- `-h, --help, /?`: Show help message and exit.
- `-m, --ignore-missing-sheets`: Ignore missing optional sheets (vHost, vPartition & vMemory). Will still validate vInfo sheet exists.
- `-i, --skip-invalid-files`: Skip files that don't contain all required sheets instead of failing with an error.
- `-a, --anonymize`: Anonymize VM, DNS Name, Cluster, Host, and Datacenter columns with generic names (vm1, host1, etc.).
- `-M, --only-mandatory-columns`: Include only the mandatory columns for each sheet in the output file instead of all common columns.
- `-s, --include-source`: Include a 'Source File' column in each sheet showing the name of the source file for each record.
- `-d, --debug`: Show detailed error information including stack traces when errors occur.

### Examples

```bash
# Basic usage with default input and output locations
RVToolsMerge

# Using the default input folder
RVToolsMerge C:\RVTools\Data

# Specify input file
RVToolsMerge C:\RVTools\Data\SingleFile.xlsx

# Specify input folder and output file
RVToolsMerge C:\RVTools\Data C:\Reports\Merged_RVTools.xlsx

# Process files with missing optional sheets
RVToolsMerge -m C:\RVTools\Data

# Skip invalid files
RVToolsMerge -i C:\RVTools\Data

# Anonymize sensitive data
RVToolsMerge -a C:\RVTools\Data\RVTools.xlsx C:\Reports\Anonymized_RVTools.xlsx

# Include only mandatory columns
RVToolsMerge -M C:\RVTools\Data C:\Reports\Mandatory_Columns.xlsx

# Include source file name in output
RVToolsMerge -s C:\RVTools\Data C:\Reports\With_Source_Files.xlsx

# Combine options
RVToolsMerge -a -M -s C:\RVTools\Data C:\Reports\Complete_Analysis.xlsx
```

## Validation Behavior

- By default, all sheets must exist in all files
- When using `--ignore-missing-sheets`, optional sheets (vHost, vPartition, vMemory) can be missing with warnings shown. The vInfo sheet is always required.
- When using `--skip-invalid-files`, files without required sheets will be skipped and reported, but processing will continue with valid files.
- When using `--anonymize`, sensitive names are replaced with generic identifiers (vm1, dns1, host1, etc.) to protect sensitive information.
- When using `--only-mandatory-columns`, only the mandatory columns for each sheet are included in the output, regardless of what other columns might be common across all files.
- When using `--include-source`, a 'Source File' column is added to each sheet to show which source file each record came from, helping with data traceability.

## Troubleshooting

- Use the `-d` or `--debug` flag to see detailed error information when problems occur
- If you encounter missing sheets errors, consider using the `-m` flag to ignore missing optional sheets
- If some files are causing errors, use the `-i` flag to skip invalid files
- For permission issues, ensure you have write access to the output folder

# Github Project

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Continuous Integration

This project uses GitHub Actions for CI/CD:

| Workflow | Description |
|----------|-------------|
| [dotnet.yml](/.github/workflows/dotnet.yml) | Builds and tests the application on every push and pull request |
| [build.yml](/.github/workflows/build.yml) | Reusable workflow for building the application across multiple platforms (Windows x64/ARM64, Linux, macOS ARM64) |
| [release.yml](/.github/workflows/release.yml) | Creates releases when tags are pushed |
| [codeql.yml](/.github/workflows/codeql.yml) | Security scanning with CodeQL |
| [pr-validation.yml](/.github/workflows/pr-validation.yml) | Additional validation for pull requests |
| [vulnerability-scan.yml](/.github/workflows/vulnerability-scan.yml) | Weekly security scanning with CodeQL |
| [dependency-review.yml](/.github/workflows/dependency-review.yml) | Reviews dependency changes in pull requests |
| [secret-scanning.yml](/.github/workflows/secret-scanning.yml) | Identifies committed secrets and credentials |
| [nuget-vulnerability-scan.yml](/.github/workflows/nuget-vulnerability-scan.yml) | Checks for vulnerable NuGet packages |
| [generate-sbom.yml](/.github/workflows/generate-sbom.yml) | Creates Software Bill of Materials for releases |
| [labeler.yml](/.github/workflows/labeler.yml) | Automatic labeling of PRs based on files changed |
| [stale.yml](/.github/workflows/stale.yml) | Marks and closes stale issues and PRs |

### Development Environment

- [EditorConfig](/.editorconfig) is used to maintain consistent coding styles
- GitHub issue and pull request templates are available
- Dependabot keeps dependencies up to date

## Security

### Security Overview

| Feature | Status |
|---------|--------|
| CodeQL Analysis | ✅ Enabled |
| Dependency Review | ✅ Enabled |
| Secret Scanning | ✅ Enabled |
| Dependabot Alerts | ✅ Enabled |
| SBOM Generation | ✅ Enabled |
| Security Policy | ✅ [View Policy](SECURITY.md) |

### Security Features

This repository is configured with GitHub Advanced Security features:

- **CodeQL Analysis**: Automatically scans code for vulnerabilities
- **Dependency Review****: Reviews dependencies for known vulnerabilities
- **Secret Scanning**: Prevents accidental commit of secrets
- **Dependabot Security Updates**: Automatically creates pull requests for security vulnerabilities
- **Software Bill of Materials (SBOM)**: Generated for each release to track components
- **NuGet Vulnerability Scanning**: Regularly checks for vulnerable packages

To report a security vulnerability, please see our [Security Policy](SECURITY.md).

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- [RVTools](https://www.robware.net/rvtools/) by Robware for the amazing virtualization documentation tool
- [ClosedXML](https://github.com/ClosedXML/ClosedXML) for Excel file handling
- [Spectre.Console](https://spectreconsole.net/) for beautiful console output

