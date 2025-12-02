//-----------------------------------------------------------------------
// <copyright file="MergeCommandSettings.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using System.ComponentModel;
using Spectre.Console.Cli;

namespace RVToolsMerge.Commands;

/// <summary>
/// Command settings for the merge operation using Spectre.Console.Cli.
/// </summary>
public class MergeCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the input path (file or directory).
    /// </summary>
    [CommandArgument(0, "<INPUT_PATH>")]
    [Description("Path to an RVTools Excel file or directory containing multiple RVTools Excel files")]
    public string InputPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the output file path.
    /// </summary>
    [CommandArgument(1, "[OUTPUT_PATH]")]
    [Description("Path for the merged output Excel file (optional, defaults to 'merged-output.xlsx')")]
    public string? OutputPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ignore missing optional sheets.
    /// Note: This is automatically enabled when using --all-sheets.
    /// </summary>
    [CommandOption("-i|--ignore-missing-sheets")]
    [Description("Ignore missing optional sheets and continue processing (automatically enabled with --all-sheets)")]
    [DefaultValue(false)]
    public bool IgnoreMissingOptionalSheets { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to process all sheets in RVTools exports.
    /// </summary>
    [CommandOption("-A|--all-sheets")]
    [Description("Process all sheets in RVTools exports (mutually exclusive with --anonymize)")]
    [DefaultValue(false)]
    public bool AllSheets { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to skip invalid files during processing.
    /// </summary>
    [CommandOption("-s|--skip-invalid-files")]
    [Description("Skip files that cannot be processed instead of failing")]
    [DefaultValue(false)]
    public bool SkipInvalidFiles { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to anonymize sensitive data in the output.
    /// </summary>
    [CommandOption("-a|--anonymize")]
    [Description("Anonymize sensitive data such as VM names and IP addresses")]
    [DefaultValue(false)]
    public bool AnonymizeData { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include only mandatory columns in the output.
    /// </summary>
    [CommandOption("-M|--only-mandatory-columns")]
    [Description("Include only mandatory columns in the output")]
    [DefaultValue(false)]
    public bool OnlyMandatoryColumns { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to add a column with the source file name for each row.
    /// </summary>
    [CommandOption("-f|--include-source-filename")]
    [Description("Add a column with the source filename for each row")]
    [DefaultValue(false)]
    public bool IncludeSourceFileName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to exclude rows that have empty values in mandatory columns.
    /// </summary>
    [CommandOption("-e|--skip-empty-values")]
    [Description("Skip rows with empty values in mandatory columns")]
    [DefaultValue(false)]
    public bool SkipRowsWithEmptyMandatoryValues { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable detailed debug logging and output.
    /// </summary>
    [CommandOption("-d|--debug")]
    [Description("Enable detailed debug logging and output")]
    [DefaultValue(false)]
    public bool DebugMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable Azure Migrate validation rules.
    /// </summary>
    [CommandOption("-z|--azure-migrate")]
    [Description("Enable Azure Migrate validation rules")]
    [DefaultValue(false)]
    public bool EnableAzureMigrateValidation { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of vInfo rows to process.
    /// </summary>
    [CommandOption("--max-vinfo-rows <COUNT>")]
    [Description("Maximum number of vInfo rows to process (useful for creating samples)")]
    [DefaultValue(null)]
    public int? MaxVInfoRows { get; set; }
}
