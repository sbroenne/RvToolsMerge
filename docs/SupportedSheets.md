# Supported Sheets and Required Columns

RVToolsMerge processes the following key sheets from RVTools exports:

## Required and Optional Sheets

| Sheet Name     | Status       | Description                                  |
| -------------- | ------------ | -------------------------------------------- |
| **vInfo**      | **Required** | Core VM information (CPU, memory, OS)        |
| **vHost**      | Optional     | ESXi host configuration and performance data |
| **vPartition** | Optional     | VM disk partition information                |
| **vMemory**    | Optional     | VM memory configuration details              |

The `vInfo` sheet must be present in all processed files. The other sheets are considered optional and can be handled according to your configuration.

## Mandatory Columns by Sheet

Each sheet has specific required columns that must be present for proper processing:

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
