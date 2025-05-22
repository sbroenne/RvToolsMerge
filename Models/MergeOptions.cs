//-----------------------------------------------------------------------
// <copyright file="MergeOptions.cs" company="Stefan Broenner"> ">
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
    /// </summary>
    public bool IgnoreMissingOptionalSheets { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to skip invalid files.
    /// </summary>
    public bool SkipInvalidFiles { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to anonymize data.
    /// </summary>
    public bool AnonymizeData { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include only mandatory columns.
    /// </summary>
    public bool OnlyMandatoryColumns { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include the source file name.
    /// </summary>
    public bool IncludeSourceFileName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to skip rows with empty mandatory values.
    /// </summary>
    public bool SkipRowsWithEmptyMandatoryValues { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether debug mode is enabled.
    /// </summary>
    public bool DebugMode { get; set; }
}
