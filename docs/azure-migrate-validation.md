# Azure Migrate Validation

This document describes the Azure Migrate validation feature in RVToolsMerge, including limits and validation rules.

## Overview

The Azure Migrate validation feature in RVToolsMerge helps prepare VMware inventory data for import into Azure Migrate. It validates that the data meets Azure Migrate's requirements and constraints.

## Validation Rules

RVToolsMerge performs the following validations on VMware inventory data:

1. **VM UUID Validation**: Each VM must have a valid UUID.
2. **OS Configuration Validation**: Each VM must have a specified OS configuration.
3. **Duplicate UUID Check**: Each VM UUID must be unique.
4. **VM Count Limit**: Azure Migrate has a limit of 20,000 VMs per assessment.

## VM Count Limit

Azure Migrate has a hard limit of 20,000 VMs per assessment. When this limit is reached:

- RVToolsMerge will stop processing new VMs
- The validation results will show how many VMs were processed
- The validation results will show how many rows were skipped after the limit was reached
- The console output will indicate that the VM count limit was exceeded

Example console output when the limit is reached:

```
┌─────────────────────────────────┬───────┐
│ VM Count Limit Exceeded         │ 1     │
│ Rows Not Processed Due to Limit │ 2500  │
└─────────────────────────────────┴───────┘
```

## Validation Result Properties

The `AzureMigrateValidationResult` class provides the following properties:

- `FailedRows`: List of rows that failed validation
- `MissingVmUuidCount`: Count of rows that failed due to missing VM UUID
- `MissingOsConfigurationCount`: Count of rows that failed due to missing OS configuration
- `DuplicateVmUuidCount`: Count of rows that failed due to duplicate VM UUID
- `VmCountExceededCount`: Count of rows that failed due to exceeding the VM count limit
- `TotalVmsProcessed`: Total number of VMs processed before limits were reached
- `VmCountLimitReached`: Indicates whether the VM count limit was reached
- `RowsSkippedAfterLimitReached`: Count of rows that were skipped (not processed) after the VM count limit was reached
- `TotalFailedRows`: Total number of failed rows

## Recommended Approach for Large Environments

If you have more than 20,000 VMs to assess:

1. Split your VMware inventory into smaller segments
2. Process each segment separately with RVToolsMerge
3. Create multiple Azure Migrate assessments, one for each segment