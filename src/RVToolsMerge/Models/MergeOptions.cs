//-----------------------------------------------------------------------
// <copyright file="MergeOptions.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

namespace RVToolsMerge.Models;

/// <summary>
/// Configuration options for the merge operation.
/// </summary>
public class MergeOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to ignore missing optional sheets.
    /// When true, the merge operation will continue even if some input files are missing optional worksheets.
    /// Default is false.
    /// </summary>
    public bool IgnoreMissingOptionalSheets { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to process all sheets found in RVTools exports.
    /// When true, all sheets beyond the core 4 (vInfo, vHost, vPartition, vMemory) will be discovered and processed.
    /// This option is mutually exclusive with AnonymizeData.
    /// Default is false.
    /// </summary>
    public bool ProcessAllSheets { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to skip invalid files during processing.
    /// When true, files that cannot be processed will be skipped rather than causing the operation to fail.
    /// Default is false.
    /// </summary>
    public bool SkipInvalidFiles { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to anonymize sensitive data in the output.
    /// When true, sensitive information such as VM names and IP addresses will be replaced with anonymized values.
    /// Default is false.
    /// </summary>
    public bool AnonymizeData { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to include only mandatory columns in the output.
    /// When true, only essential columns required for basic functionality will be included.
    /// Default is false.
    /// </summary>
    public bool OnlyMandatoryColumns { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to add a column with the source file name for each row.
    /// When true, each row in the merged output will include the name of the original source file.
    /// Default is false.
    /// </summary>
    public bool IncludeSourceFileName { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to exclude rows that have empty values in mandatory columns.
    /// When true, rows with missing required data will be omitted from the merged output.
    /// Default is false.
    /// </summary>
    public bool SkipRowsWithEmptyMandatoryValues { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to enable detailed debug logging and output.
    /// When true, additional diagnostic information will be generated during the merge process.
    /// Default is false.
    /// </summary>
    public bool DebugMode { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to enable Azure Migrate validation rules.
    /// When true, additional validation will be performed and a separate file will be created for rows that fail validation.
    /// Default is false.
    /// </summary>
    public bool EnableAzureMigrateValidation { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of vInfo rows to process.
    /// When set to a positive value, only the first N rows from the vInfo sheet will be included in the output.
    /// This is useful for creating sample files with anonymization and mandatory columns features.
    /// When null or zero, all rows will be processed.
    /// Default is null (no limit).
    /// </summary>
    public int? MaxVInfoRows { get; set; } = null;
}
