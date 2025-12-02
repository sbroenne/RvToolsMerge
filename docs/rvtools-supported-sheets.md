# Supported Sheets and Required Columns

RVToolsMerge supports two processing modes for handling RVTools exports:

## Processing Modes

### Standard Mode (Default)

By default, RVToolsMerge processes only the four core sheets:

-   **vInfo** (required)
-   **vHost** (optional)
-   **vPartition** (optional)
-   **vMemory** (optional)

All four sheets must be present, or the operation will fail unless you use the `--ignore-missing-sheets` flag.

### All Sheets Mode (`--all-sheets`)

When using the `--all-sheets` flag (with `-A`), RVToolsMerge automatically discovers and processes **all sheets** present in your RVTools exports. This includes:

-   The 4 core sheets (vInfo, vHost, vPartition, vMemory)
-   Additional sheets like vCPU, vDisk, vNetwork, vFloppy, vCD, vUSB, vSnapshot, etc.
-   Any other sheets present in your RVTools export

**Important Notes**:
- The `--all-sheets` flag is **mutually exclusive** with the `--anonymize` flag (anonymization only works with the four core sheets)
- When using `--all-sheets`, missing optional sheets are **automatically tolerated** - you don't need to also specify `--ignore-missing-sheets`

### Ignore Missing Sheets (`--ignore-missing-sheets`)

The `--ignore-missing-sheets` flag (or `-i`) allows processing files that are missing optional sheets (vHost, vPartition, vMemory). Only vInfo is required when this flag is enabled.

**Note**: When using `--all-sheets`, this flag is automatically enabled, so you don't need to specify both.

## Core Sheets

The following sheets are the core sheets with strict validation:

| Sheet Name     | Status       | Description                                  |
| -------------- | ------------ | -------------------------------------------- |
| **vInfo**      | **Required** | Core VM information (CPU, memory, OS)        |
| **vHost**      | Optional     | ESXi host configuration and performance data |
| **vPartition** | Optional     | VM disk partition information                |
| **vMemory**    | Optional     | VM memory configuration details              |

The `vInfo` sheet must be present in all processed files. The other core sheets are validated but optional when using `--ignore-missing-sheets`.

## Additional Sheets (All Sheets Mode)

When using `--all-sheets`, any additional sheets found in your RVTools exports are automatically included in the merge process:

-   **vCPU** - CPU configuration details
-   **vDisk** - Disk information
-   **vNetwork** - Network adapter details
-   **vFloppy** - Floppy drive information
-   **vCD** - CD/DVD drive information
-   And any other sheets present in your RVTools export

These sheets are processed automatically based on their actual column headers, without strict validation. The tool will:

-   Identify common columns across all input files
-   Merge data from sheets with the same name
-   Preserve all columns found in the source files

**Note**: Anonymization (`--anonymize`) is not supported with `--all-sheets`. Anonymization only works with the four core sheets.

## Mandatory Columns by Sheet

Each core sheet has specific required columns that must be present for proper processing:

### vInfo Sheet (Required)

-   VM UUID
-   Template
-   SRM Placeholder
-   Powerstate
-   VM
-   CPUs
-   Memory
-   In Use MiB
-   OS according to the configuration file
-   Creation date
-   NICs
-   Disks
-   Provisioned MiB
-   Datacenter
-   Cluster
-   Host

### vHost Sheet (Optional)

-   Host
-   Datacenter
-   Cluster
-   CPU Model
-   Speed
-   \# CPU
-   Cores per CPU
-   \# Cores
-   CPU usage %
-   \# Memory
-   Memory usage %

### vPartition Sheet (Optional)

-   VM UUID
-   VM
-   Disk
-   Capacity MiB
-   Consumed MiB

### vMemory Sheet (Optional)

-   VM UUID
-   VM
-   Size MiB
-   Reservation
