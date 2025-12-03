# RVToolsMerge

[![.NET Build and Test](https://github.com/sbroenne/RVToolsMerge/actions/workflows/dotnet.yml/badge.svg)](https://github.com/sbroenne/RVToolsMerge/actions/workflows/dotnet.yml)
[![Unit Tests](https://img.shields.io/github/actions/workflow/status/sbroenne/RVToolsMerge/dotnet.yml?label=tests)](https://github.com/sbroenne/RVToolsMerge/actions/workflows/dotnet.yml)
[![Code Coverage](https://github.com/sbroenne/RVToolsMerge/raw/gh-pages/badges/coverage.svg)](https://github.com/sbroenne/RVToolsMerge/actions/workflows/code-coverage.yml)
[![Alternative Coverage](https://img.shields.io/badge/coverage-check%20report-brightgreen)](https://github.com/sbroenne/RVToolsMerge/actions/workflows/code-coverage.yml)
[![CodeQL](https://github.com/sbroenne/RVToolsMerge/actions/workflows/codeql.yml/badge.svg)](https://github.com/sbroenne/RVToolsMerge/actions/workflows/codeql.yml)
[![GitHub Advanced Security](https://img.shields.io/badge/GitHub%20Advanced%20Security-enabled-brightgreen)](SECURITY.md)

A modern, cross-platform console application for merging one or multiple [RVTools](https://www.robware.net/rvtools/) export files into a single consolidated file. Supports anonymization of sensitive data.

> Created by Stefan Broenner (github.com/sbroenne) and contributors

## What is RVTools?

[RVTools](https://www.robware.net/rvtools/) is a popular Windows application for exporting detailed inventory and configuration data from VMware vSphere environments to Excel files. It is widely used by VMware administrators for reporting and analysis.

## Overview

RVToolsMerge is a powerful command-line utility designed to consolidate multiple RVTools exports into one Excel file, making it easier to analyze VMware environment data from different sources.

Built with .NET 10, this cross-platform tool efficiently processes individual RVTools Excel files or entire directories, intelligently merging data from key sheets while ensuring consistency across the consolidated output.

As an independent, open-source project, RVToolsMerge is _not affiliated with_ the official RVTools application but works with its export data to provide additional functionality.

## Data Protection & Security

RVToolsMerge treats security and data protection as top priorities, especially when handling sensitive VMware infrastructure data:

-   **Complete Anonymization**: Replace VM names, DNS names, IP addresses, cluster names, host names, and datacenter names with generic equivalents
-   **Minimal Data Exposure**: Limit output to essential fields only with `--only-mandatory-columns`
-   **Consistent Relationships**: Anonymized values maintain data relationships within each file
-   **Source Tracking Control**: Optional inclusion of source file information

These features are ideal for creating sanitized reports, sharing data with vendors, and ensuring compliance with data-sharing policies. See [Anonymization and Data Protection](#anonymization-and-data-protection) for detailed usage.

## Features

-   **High-Performance Processing**: Built with .NET 10 and optimized for speed and memory efficiency
-   **Rich Console Experience**: Beautiful console output with progress bars, status indicators, and colorful tables
-   **Intelligent Sheet Handling**: Combines multiple RVTools exports while validating required sheets and columns
-   **Flexible Processing Options**:
    -   Processes single files or entire directories of RVTools exports
    -   Includes only columns that appear in all source files
    -   Optionally ignores missing optional sheets
    -   Selectively includes only mandatory columns
    -   Adds source file tracking for data lineage
-   **Cross-Platform**: Runs on Windows, Linux, and macOS with native binaries for each platform
-   **Enterprise-Grade Error Handling**: Comprehensive validation with clear, actionable error messages

## Supported Sheets and Required Columns

RVToolsMerge processes key sheets from RVTools exports with specific validation requirements for each sheet type.

For detailed information about supported sheets and their required columns, see [Supported Sheets Documentation](docs/rvtools-supported-sheets.md).

## Column Name Mappings

The application standardizes column names across different RVTools exports. This helps handle variations in column naming between different versions of RVTools.

For a complete reference of all column mappings, see [Column Mappings Documentation](docs/rvtools-column-mappings.md).

## Installation

Download the latest release for your platform from the [Releases page](https://github.com/sbroenne/RVToolsMerge/releases):

### Installation Instructions

1. Download the appropriate ZIP file for your platform
2. Extract the contents to a folder of your choice
3. Run the executable directly (no installation required)

### Prerequisites

-   No additional prerequisites required! The application is published as a self-contained executable with all dependencies included.
-   Minimum disk space: ~30MB

## Usage

```
RVToolsMerge [options] inputPath [outputFile]
```

### Arguments

| Argument     | Description                                                | Default                                      |
| ------------ | ---------------------------------------------------------- | -------------------------------------------- |
| `inputPath`  | Path to an Excel file or folder containing RVTools exports | **Required**                                 |
| `outputFile` | Path where the merged file will be saved                   | `./RVTools_Merged.xlsx` in current directory |

### Options

| Option                          | Description                                                                                     | Default |
| ------------------------------- | ----------------------------------------------------------------------------------------------- | ------- |
| `-h, --help, /?`                | Show the help message and exit                                                                  | N/A     |
| `-v, --version`                 | Show version information and exit                                                               | N/A     |
| `-i, --ignore-missing-sheets`   | Process files even when optional sheets are missing (automatically enabled with `--all-sheets`) | `false` |
| `-A, --all-sheets`              | Process all sheets in RVTools exports (mutually exclusive with `--anonymize`)                   | `false` |
| `-s, --skip-invalid-files`      | Skip files that don't meet validation requirements                                              | `false` |
| `-a, --anonymize`               | Anonymize VM, DNS, Cluster, Host, and Datacenter names                                          | `false` |
| `-M, --only-mandatory-columns`  | Include only mandatory columns in the output                                                    | `false` |
| `-f, --include-source-filename` | Add a 'Source File' column to track data origin                                                 | `false` |
| `-e, --skip-empty-values`       | Skip rows with empty values in mandatory columns                                                | `false` |
| `-z, --azure-migrate`           | Enable Azure Migrate validation requirements                                                    | `false` |
| `-d, --debug`                   | Show detailed error information with stack traces                                               | `false` |

### Advanced Options

```cmd
:: Skip files with missing optional sheets
RVToolsMerge.exe -i C:\RVTools\Exports

:: Process all sheets (vInfo, vHost, vPartition, vMemory, vCPU, vDisk, vNetwork, etc.)
:: Note: --all-sheets automatically tolerates missing optional sheets
RVToolsMerge.exe -A C:\RVTools\Exports

:: Skip invalid files entirely
RVToolsMerge.exe -s C:\RVTools\Exports

:: Skip invalid files and ignore missing optional sheets in valid files
RVToolsMerge.exe -s -i C:\RVTools\Exports

:: Anonymize sensitive data (core 4 sheets only)
RVToolsMerge.exe -a C:\RVTools\Exports\RVTools.xlsx C:\Reports\Anonymized.xlsx

:: Include only mandatory columns
RVToolsMerge.exe -M C:\RVTools\Exports

:: Include source file information
RVToolsMerge.exe -f C:\RVTools\Exports

:: Skip rows with empty mandatory values
RVToolsMerge.exe -e C:\RVTools\Exports

:: Enable Azure Migrate validation
RVToolsMerge.exe -z C:\RVTools\Exports C:\Reports\AzureMigrationReady.xlsx

:: Combine multiple options (without anonymization)
RVToolsMerge.exe -A -M -f -e C:\RVTools\Exports C:\Reports\Complete_Analysis.xlsx

:: Combine multiple options (with anonymization, core sheets only)
RVToolsMerge.exe -a -M -f -e C:\RVTools\Exports C:\Reports\Anonymized_Analysis.xlsx
```

## Anonymization and Data Protection

When using the `-a, --anonymize` option, the following data is consistently anonymized per file:

-   VM names → vm123_1, vm456_2, vm789_3, etc.
-   DNS names → dns123_1, dns456_2, dns789_3, etc.
-   IP addresses → ip123_1, ip456_2, ip789_3, etc.
-   Cluster names → cluster123_1, cluster456_2, etc.
-   Host names → host123_1, host456_2, etc.
-   Datacenter names → datacenter123_1, datacenter456_2, etc.

**Anonymization is only supported for the four core sheets** (vInfo, vHost, vPartition, vMemory). It cannot be used with the `--all-sheets` flag.

Anonymization maintains internal data relationships within each file, ensuring that the same original value always maps to the same anonymized value throughout all sheets. Values from different source files are anonymized to different values, preventing overlap.

When anonymization is enabled, an additional Excel file is created with the naming pattern `<output_filename>_AnonymizationMap.xlsx`, containing the mapping between original and anonymized values for de-anonymization if needed.

For more details on how anonymization is implemented, see the [Column Mappings Documentation](docs/rvtools-column-mappings.md).

## Azure Migrate Validation

The `-z, --azure-migrate` option validates your VMware inventory data for Azure Migrate compatibility:

-   Validates VM UUIDs are present and unique
-   Validates OS configuration data is present
-   Enforces the Azure Migrate limit of 20,000 VMs
-   Exports failed rows to `<output_filename>_FailedAzureMigrateValidation.xlsx`

For more details, see [Azure Migrate Validation Documentation](docs/azure-migrate-validation.md).

## Validation Behavior

RVToolsMerge implements strict validation by default. All processing options are disabled, meaning:

-   All required sheets with mandatory columns must exist in all files
-   Processing fails if any file doesn't meet requirements

Use options to modify this behavior:

| Option      | Effect                                                                   |
| ----------- | ------------------------------------------------------------------------ |
| `-s`        | Skip invalid files instead of failing                                    |
| `-i`        | Allow missing optional sheets (vHost, vPartition, vMemory)               |
| `-e`        | Exclude rows with empty mandatory values                                 |
| `-s` + `-i` | Most permissive: skip invalid files and tolerate missing optional sheets |

## Troubleshooting

| Issue                 | Solution                                          |
| --------------------- | ------------------------------------------------- |
| General errors        | Use `-d` for detailed error information           |
| Missing sheets errors | Use `-i` to ignore missing optional sheets        |
| Invalid file errors   | Use `-s` to skip invalid files                    |
| Low memory            | Process smaller batches of files                  |
| Permission errors     | Ensure write access to output folder              |
| Excel file locked     | Close applications using the file                 |
| Slow performance      | Check antivirus scanning; exclude working folders |

## Development

For developers who want to build from source, contribute, or understand the technical architecture, see the [Development Guide](docs/DEVELOPMENT.md).

## Security

This project takes security seriously, with multiple layers of protection:

### GitHub Advanced Security Features

| Feature           | Status                        |
| ----------------- | ----------------------------- |
| CodeQL Analysis   | ✅ Enabled                    |
| Dependency Review | ✅ Enabled                    |
| Secret Scanning   | ✅ Enabled                    |
| Dependabot Alerts | ✅ Enabled                    |
| Security Policy   | ✅ [View Policy](SECURITY.md) |

### Security Practices

-   **Automated Scanning**: Regular code scanning for vulnerabilities
-   **Dependency Management**: Automated updates for security patches
-   **Secure Coding**: Following established security best practices
-   **Vulnerability Reporting**: Clear process for reporting security issues

For details on reporting security vulnerabilities, please see our [Security Policy](SECURITY.md).

## Related Projects

-   [Excel MCP Server](https://sbroenne.github.io/mcp-server-excel/) - AI-powered Excel automation through the Model Context Protocol. Control Microsoft Excel with natural language through GitHub Copilot, Claude, and other AI assistants. Features 172 operations covering Power Query, DAX, VBA macros, PivotTables, Charts, and more.

**Ideal companion for RVTools analysis**: After merging your RVTools exports with RVToolsMerge, use Excel MCP Server to analyze the data using natural language—create PivotTables summarizing VM resources by cluster, build charts showing memory utilization trends, or write DAX measures for capacity planning—all through conversational AI commands.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines on how to contribute to this project.

## Acknowledgments

-   [RVTools](https://www.robware.net/rvtools/) by Robware - This independent project works with data exported from RVTools but is not affiliated with, endorsed by, or connected to Robware or the official RVTools product
-   [ClosedXML](https://github.com/ClosedXML/ClosedXML) for excellent Excel file handling capabilities
-   [Spectre.Console](https://spectreconsole.net/) for beautiful console output and UX
-   [GitHub Copilot](https://github.com/features/copilot) using various models for AI assistance in code generation and development
