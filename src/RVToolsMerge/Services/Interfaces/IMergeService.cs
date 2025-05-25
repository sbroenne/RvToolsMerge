//-----------------------------------------------------------------------
// <copyright file="IMergeService.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using RVToolsMerge.Models;

namespace RVToolsMerge.Services.Interfaces;

/// <summary>
/// Interface for merging RVTools files.
/// </summary>
public interface IMergeService
{
    /// <summary>
    /// Merges multiple RVTools Excel files into a single file.
    /// </summary>
    /// <param name="filePaths">Array of file paths to process.</param>
    /// <param name="outputPath">Path where the merged file will be saved.</param>
    /// <param name="options">Merge configuration options.</param>
    /// <param name="validationIssues">List to store validation issues.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MergeFilesAsync(
        string[] filePaths,
        string outputPath,
        MergeOptions options,
        List<ValidationIssue> validationIssues);
}
