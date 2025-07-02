# Changelog

All notable changes to the RVToolsMerge project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.4.0] - 2025-07-02

### Added

-   Added new mandatory columns for the vInfo sheet:
    -   Creation Date
    -   NICs
    -   Disks
    -   Provisioned MiB (already existed as a column mapping but now marked as mandatory)
-   Enhanced winget manifest template with proper ProductCode formatting
-   Improved cross-platform compatibility and documentation

### Changed

-   Updated to .NET 9 for better performance and cross-platform support
-   Enhanced console UI with Spectre.Console for better user experience
-   Improved error handling and validation messages

### Fixed

-   Fixed ProductCode formatting in winget manifest templates to include proper quotes around GUIDs

## [1.0.1] - 2025-05-22

### Added

-   Improved error handling for files with missing mandatory columns
-   Support for anonymizing Datacenter names

### Changed

-   Project structure reorganized to follow GitHub best practices
-   Updated build workflows to support the new project structure

### Fixed

-   Fixed an issue with file path handling on Linux systems
-   Improved thread synchronization for Excel processing

## [1.0.0] - 2025-04-15

### Added

-   Initial release
-   Support for merging multiple RVTools Excel files
-   Anonymization of sensitive data (VM names, DNS names, IP addresses, cluster names, host names)
-   Filtering to include only mandatory columns
-   Validation of RVTools export files
-   Cross-platform support (Windows, Linux, macOS)
-   Command-line interface with multiple options
