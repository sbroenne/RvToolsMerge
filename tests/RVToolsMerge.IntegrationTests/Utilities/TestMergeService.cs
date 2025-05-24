//-----------------------------------------------------------------------
// <copyright file="TestMergeService.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using RVToolsMerge.Models;
using RVToolsMerge.Services;
using RVToolsMerge.Services.Interfaces;
using System.IO.Abstractions;

namespace RVToolsMerge.IntegrationTests.Utilities;

/// <summary>
/// A specialized version of MergeService for testing that skips interactive UI components.
/// </summary>
public class TestMergeService : IMergeService
{
    private readonly IMergeService _realMergeService;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TestMergeService"/> class.
    /// </summary>
    /// <param name="excelService">Excel service.</param>
    /// <param name="validationService">Validation service.</param>
    /// <param name="anonymizationService">Anonymization service.</param>
    /// <param name="consoleUiService">Console UI service.</param>
    /// <param name="fileSystem">File system.</param>
    public TestMergeService(
        IExcelService excelService,
        IValidationService validationService,
        IAnonymizationService anonymizationService,
        ConsoleUIService consoleUiService,
        IFileSystem fileSystem)
    {
        _realMergeService = new MergeService(
            excelService,
            validationService, 
            anonymizationService,
            consoleUiService,
            fileSystem);
    }
    
    /// <summary>
    /// Merges multiple RVTools Excel files into a single file.
    /// </summary>
    /// <param name="filePaths">Array of file paths to process.</param>
    /// <param name="outputPath">Path where the merged file will be saved.</param>
    /// <param name="options">Merge configuration options.</param>
    /// <param name="validationIssues">List to store validation issues.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task MergeFilesAsync(string[] filePaths, string outputPath, MergeOptions options, List<ValidationIssue> validationIssues)
    {
        return _realMergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);
    }
}