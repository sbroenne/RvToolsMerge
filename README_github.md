# RVTools Excel Merger

[![.NET Build and Test](https://github.com/Ysbroenne/RVToolsMerge/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Ysbroenne/RVToolsMerge/actions/workflows/dotnet.yml)
[![CodeQL](https://github.com/Ysbroenne/RVToolsMerge/actions/workflows/codeql.yml/badge.svg)](https://github.com/Ysbroenne/RVToolsMerge/actions/workflows/codeql.yml)
[![GitHub Advanced Security](https://img.shields.io/badge/GitHub%20Advanced%20Security-enabled-brightgreen)](SECURITY.md)

A .NET console application that merges multiple RVTools Excel files into a single consolidated file. This tool helps VMware administrators combine multiple RVTools exports for consolidated reporting and analysis.

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
- Option to anonymize VM, DNS, Cluster, Host, and Datacenter names with generic identifiers
- Fast processing with minimal memory footprint
  
## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) or later

### Installation

#### Option 1: Download the latest release
1. Go to the [Releases](https://github.com/Ysbroenne/RVToolsMerge/releases) page
2. Download the latest `RVToolsMerge.zip` file
3. Extract the contents to a folder of your choice

#### Option 2: Build from source
1. Clone the repository
   ```
   git clone https://github.com/Ysbroenne/RVToolsMerge.git
   ```
2. Navigate to the project directory
   ```
   cd RVToolsMerge
   ```
3. Build the project
   ```
   dotnet build
   ```
   
### Usage

```
RVToolsMerge [options] [inputFolder] [outputFile]
```

#### Arguments:
- `inputFolder`: Path to the folder containing RVTools Excel files. Defaults to "input" subfolder in the current directory.
- `outputFile`: Path where the merged file will be saved. Defaults to "RVTools_Merged.xlsx" in the current directory.

#### Options:
- `-h, --help, /?`: Show help message and exit.
- `-m, --ignore-missing-optional-sheets`: Ignore missing optional sheets (vHost & vPartition). Will still validate vInfo sheet exists.
- `-i, --skip-invalid-files`: Skip files that don't contain all required sheets instead of failing with an error.
- `-a, --anonymize`: Anonymize VM, DNS Name, Cluster, Host, and Datacenter columns with generic names (vm1, host1, etc.).

Note: `--ignore-missing-optional-sheets` and `--skip-invalid-files` cannot be used together.

#### Examples:
```
RVToolsMerge
RVToolsMerge C:\RVTools\Data
RVToolsMerge -m C:\RVTools\Data C:\Reports\Merged_RVTools.xlsx
RVToolsMerge --ignore-missing-optional-sheets C:\RVTools\Data
RVToolsMerge -i C:\RVTools\Data
RVToolsMerge -a C:\RVTools\Data C:\Reports\Anonymized_RVTools.xlsx
```

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
| [build.yml](/.github/workflows/build.yml) | Reusable workflow for building the application |
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
- **Dependency Review**: Reviews dependencies for known vulnerabilities
- **Secret Scanning**: Prevents accidental commit of secrets
- **Dependabot Security Updates**: Automatically creates pull requests for security vulnerabilities
- **Software Bill of Materials (SBOM)**: Generated for each release to track components
- **NuGet Vulnerability Scanning**: Regularly checks for vulnerable packages

To report a security vulnerability, please see our [Security Policy](SECURITY.md).

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- [RVTools](https://www.robware.net/rvtools/) - The excellent tool that creates the Excel files this application is designed to merge.
