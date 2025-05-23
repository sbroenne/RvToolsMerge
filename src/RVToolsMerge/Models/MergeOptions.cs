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
{    /// <summary>
     /// Gets or sets a value indicating whether to ignore missing optional sheets.
     /// Default is false.
     /// </summary>
    public bool IgnoreMissingOptionalSheets { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to skip invalid files.
    /// Default is true.
    /// </summary>
    public bool SkipInvalidFiles { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to anonymize data.
    /// Default is false.
    /// </summary>
    public bool AnonymizeData { get; set; } = false;    /// <summary>
                                                        /// Gets or sets a value indicating whether to include only mandatory columns.
                                                        /// Default is false.
                                                        /// </summary>
    public bool OnlyMandatoryColumns { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to include the source file name.
    /// Default is true.
    /// </summary>
    public bool IncludeSourceFileName { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to skip rows with empty mandatory values.
    /// Default is false.
    /// </summary>
    public bool SkipRowsWithEmptyMandatoryValues { get; set; } = false;    /// <summary>
                                                                           /// Gets or sets a value indicating whether debug mode is enabled.
                                                                           /// Default is false.
                                                                           /// </summary>
    public bool DebugMode { get; set; } = false;
}
