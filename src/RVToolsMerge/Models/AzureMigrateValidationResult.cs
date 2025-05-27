//-----------------------------------------------------------------------
// <copyright file="AzureMigrateValidationResult.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using ClosedXML.Excel;

namespace RVToolsMerge.Models;

/// <summary>
/// Represents the reason a row failed Azure Migrate validation.
/// </summary>
public enum AzureMigrateValidationFailureReason
{
    /// <summary>
    /// The VM UUID is missing or null.
    /// </summary>
    MissingVmUuid,

    /// <summary>
    /// The OS configuration is missing or null.
    /// </summary>
    MissingOsConfiguration,

    /// <summary>
    /// The VM UUID is not unique (duplicate).
    /// </summary>
    DuplicateVmUuid,

    /// <summary>
    /// The VM count exceeded the 20,000 limit.
    /// </summary>
    VmCountExceeded
}

/// <summary>
/// Represents a row that failed Azure Migrate validation.
/// </summary>
public class AzureMigrateValidationFailure
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureMigrateValidationFailure"/> class.
    /// </summary>
    /// <param name="rowData">The row data that failed validation.</param>
    /// <param name="reason">The reason for the validation failure.</param>
    public AzureMigrateValidationFailure(XLCellValue[] rowData, AzureMigrateValidationFailureReason reason)
    {
        RowData = rowData;
        Reason = reason;
    }

    /// <summary>
    /// Gets the row data that failed validation.
    /// </summary>
    public XLCellValue[] RowData { get; }

    /// <summary>
    /// Gets the reason for the validation failure.
    /// </summary>
    public AzureMigrateValidationFailureReason Reason { get; }
}

/// <summary>
/// Represents the result of Azure Migrate validation for a sheet.
/// </summary>
public class AzureMigrateValidationResult
{
    /// <summary>
    /// Gets or sets the list of rows that failed validation.
    /// </summary>
    public List<AzureMigrateValidationFailure> FailedRows { get; set; } = [];

    /// <summary>
    /// Gets or sets the count of rows that failed due to missing VM UUID.
    /// </summary>
    public int MissingVmUuidCount { get; set; }

    /// <summary>
    /// Gets or sets the count of rows that failed due to missing OS configuration.
    /// </summary>
    public int MissingOsConfigurationCount { get; set; }

    /// <summary>
    /// Gets or sets the count of rows that failed due to duplicate VM UUID.
    /// </summary>
    public int DuplicateVmUuidCount { get; set; }

    /// <summary>
    /// Gets or sets the count of rows that failed due to exceeding the VM count limit.
    /// </summary>
    public int VmCountExceededCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of VMs processed before limits were reached.
    /// </summary>
    public int TotalVmsProcessed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the VM count limit was reached.
    /// </summary>
    public bool VmCountLimitReached { get; set; }

    /// <summary>
    /// Gets or sets the count of rows that were skipped (not processed) after the VM count limit was reached.
    /// </summary>
    public int RowsSkippedAfterLimitReached { get; set; }

    /// <summary>
    /// Gets the total number of failed rows.
    /// </summary>
    public int TotalFailedRows => FailedRows.Count;
}