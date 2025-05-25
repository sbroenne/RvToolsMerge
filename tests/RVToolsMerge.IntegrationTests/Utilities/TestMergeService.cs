//-----------------------------------------------------------------------
// <copyright file="TestMergeService.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using ClosedXML.Excel;
using RVToolsMerge.Configuration;
using RVToolsMerge.Exceptions;
using RVToolsMerge.Models;
using RVToolsMerge.Services;
using RVToolsMerge.Services.Interfaces;
using System.IO.Abstractions;

namespace RVToolsMerge.IntegrationTests.Utilities;

/// <summary>
/// A specialized version of MergeService for testing without using Spectre.Console's Progress features.
/// </summary>
public class TestMergeService : IMergeService
{
    private readonly IExcelService _excelService;
    private readonly IValidationService _validationService;
    private readonly IAnonymizationService _anonymizationService;
    private readonly ConsoleUIService _consoleUiService;
    private readonly IFileSystem _fileSystem;
    
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
        _excelService = excelService;
        _validationService = validationService;
        _anonymizationService = anonymizationService;
        _consoleUiService = consoleUiService;
        _fileSystem = fileSystem;
    }
    
    /// <summary>
    /// Merges multiple RVTools Excel files into a single file.
    /// This version is optimized for tests without using Spectre.Console's Progress features.
    /// </summary>
    /// <param name="filePaths">Array of file paths to process.</param>
    /// <param name="outputPath">Path where the merged file will be saved.</param>
    /// <param name="options">Merge configuration options.</param>
    /// <param name="validationIssues">List to store validation issues.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task MergeFilesAsync(string[] filePaths, string outputPath, MergeOptions options, List<ValidationIssue> validationIssues)
    {
        if (filePaths.Length == 0)
        {
            throw new ArgumentException("No files specified for merging.", nameof(filePaths));
        }

        // Ensure the output directory exists
        string outputDirectory = _fileSystem.Path.GetDirectoryName(outputPath)!;
        if (!_fileSystem.Directory.Exists(outputDirectory))
        {
            _fileSystem.Directory.CreateDirectory(outputDirectory);
        }

        // Track which files to process (all by default)
        var validFilePaths = new List<string>(filePaths);

        // Track which sheets exist in all files
        var availableSheets = new List<string>(SheetConfiguration.RequiredSheets);

        // Dictionary to store merged data for each sheet
        var mergedData = new Dictionary<string, List<XLCellValue[]>>();

        // Dictionary to store common columns for each sheet
        var commonColumns = new Dictionary<string, List<string>>();

        // Dictionary to store column indices for anonymization
        var anonymizeColumnIndices = new Dictionary<string, Dictionary<string, int>>();

        // Validate files first - no progress tracking in test mode
        await ValidateFilesAsync(validFilePaths, options, validationIssues);

        // Check if we have valid files to process
        if (validFilePaths.Count == 0)
        {
            throw new FileValidationException("No valid files to process.");
        }

        // Analyze columns in each sheet - no progress tracking in test mode
        await AnalyzeColumnsAsync(validFilePaths, availableSheets, options, commonColumns);

        // Setup anonymization indices
        SetupAnonymizationIndices(availableSheets, options, commonColumns, anonymizeColumnIndices);

        // Extract data from files - no progress tracking in test mode
        await ExtractDataFromFilesAsync(validFilePaths, availableSheets, options, commonColumns,
            anonymizeColumnIndices, mergedData, validationIssues);

        // Create output file - no progress tracking in test mode
        await CreateOutputFileAsync(outputPath, availableSheets, mergedData, commonColumns);
    }

    private async Task ValidateFilesAsync(List<string> validFilePaths, MergeOptions options, List<ValidationIssue> validationIssues)
    {
        await Task.Run(() =>
        {
            for (int i = validFilePaths.Count - 1; i >= 0; i--)
            {
                string filePath = validFilePaths[i];
                bool isValid = _validationService.ValidateFile(
                    filePath,
                    options.IgnoreMissingOptionalSheets,
                    validationIssues);

                if (!isValid && options.SkipInvalidFiles)
                {
                    validFilePaths.RemoveAt(i);
                }
            }
        });
    }

    private async Task AnalyzeColumnsAsync(
        List<string> validFilePaths,
        List<string> availableSheets,
        MergeOptions options,
        Dictionary<string, List<string>> commonColumns)
    {
        await Task.Run(() =>
        {
            // First determine what columns are available across all files for each sheet
            foreach (var sheetName in availableSheets)
            {
                var allFileColumns = new List<List<string>>();

                // First analyze all files to collect column information
                foreach (var filePath in validFilePaths)
                {
                    try
                    {
                        using (var workbook = new XLWorkbook(filePath))
                        {
                            if (_excelService.SheetExists(workbook, sheetName))
                            {
                                var worksheet = workbook.Worksheet(sheetName);
                                var columnNames = _excelService.GetColumnNames(worksheet);
                                allFileColumns.Add(columnNames);
                            }
                        }
                    }
                    catch (IOException ioEx) when (options.DebugMode)
                    {
                        // On Linux, provide more verbose error information for filesystem issues
                    }
                }

                // If only mandatory columns are requested, filter the common columns
                if (options.OnlyMandatoryColumns && SheetConfiguration.MandatoryColumns.TryGetValue(sheetName, out var mandatoryColumns))
                {
                    var columnsInAllFiles = allFileColumns.Count > 0
                        ? allFileColumns.Aggregate((a, b) => a.Intersect(b).ToList())
                        : [];

                    commonColumns[sheetName] = columnsInAllFiles.Intersect(mandatoryColumns).ToList();
                }
                else
                {
                    // Find columns that exist in all files for this sheet
                    var columnsInAllFiles = allFileColumns.Count > 0
                        ? allFileColumns.Aggregate((a, b) => a.Intersect(b).ToList())
                        : [];

                    // If includeSourceFileName option is enabled, prepare to add source file column
                    if (options.IncludeSourceFileName)
                    {
                        const string sourceFileColumnName = "Source File";
                        // Add source file column if it doesn't already exist
                        if (!columnsInAllFiles.Contains(sourceFileColumnName))
                        {
                            columnsInAllFiles.Add(sourceFileColumnName);
                        }
                    }

                    commonColumns[sheetName] = columnsInAllFiles;
                }
            }
        });
    }

    private void SetupAnonymizationIndices(
        List<string> availableSheets,
        MergeOptions options,
        Dictionary<string, List<string>> commonColumns,
        Dictionary<string, Dictionary<string, int>> anonymizeColumnIndices)
    {
        if (!options.AnonymizeData)
        {
            return;
        }

        foreach (var sheetName in availableSheets)
        {
            anonymizeColumnIndices[sheetName] = [];

            // VM Name
            int vmColIndex = commonColumns[sheetName].IndexOf("VM");
            if (vmColIndex >= 0) anonymizeColumnIndices[sheetName]["VM"] = vmColIndex;

            // DNS Name
            int dnsColIndex = commonColumns[sheetName].IndexOf("DNS Name");
            if (dnsColIndex >= 0) anonymizeColumnIndices[sheetName]["DNS Name"] = dnsColIndex;

            // Cluster Name
            int clusterColIndex = commonColumns[sheetName].IndexOf("Cluster");
            if (clusterColIndex >= 0) anonymizeColumnIndices[sheetName]["Cluster"] = clusterColIndex;

            // Host Name
            int hostColIndex = commonColumns[sheetName].IndexOf("Host");
            if (hostColIndex >= 0) anonymizeColumnIndices[sheetName]["Host"] = hostColIndex;

            // Datacenter Name
            int datacenterColIndex = commonColumns[sheetName].IndexOf("Datacenter");
            if (datacenterColIndex >= 0) anonymizeColumnIndices[sheetName]["Datacenter"] = datacenterColIndex;

            // IP Address
            int ipAddressColIndex = commonColumns[sheetName].IndexOf("Primary IP Address");
            if (ipAddressColIndex >= 0) anonymizeColumnIndices[sheetName]["Primary IP Address"] = ipAddressColIndex;
        }
    }

    private async Task ExtractDataFromFilesAsync(
        List<string> validFilePaths,
        List<string> availableSheets,
        MergeOptions options,
        Dictionary<string, List<string>> commonColumns,
        Dictionary<string, Dictionary<string, int>> anonymizeColumnIndices,
        Dictionary<string, List<XLCellValue[]>> mergedData,
        List<ValidationIssue> validationIssues)
    {
        await Task.Run(() =>
        {
            // Extract data using only common columns
            foreach (var sheetName in availableSheets)
            {
                mergedData[sheetName] = [
                    commonColumns[sheetName].Select(col => (XLCellValue)col).ToArray()
                ];

                foreach (var filePath in validFilePaths)
                {
                    var fileName = _fileSystem.Path.GetFileName(filePath);
                    using var workbook = new XLWorkbook(filePath);

                    if (!_excelService.SheetExists(workbook, sheetName))
                    {
                        continue;
                    }

                    var worksheet = workbook.Worksheet(sheetName);
                    var columnMapping = _excelService.GetColumnMapping(worksheet, commonColumns[sheetName]);

                    // Find the last row with data, handle null in case the worksheet is empty
                    var lastRowUsed = worksheet.LastRowUsed();
                    int lastRow = lastRowUsed is not null ? lastRowUsed.RowNumber() : 1;

                    // Find source file column index if the option is enabled
                    int sourceFileColumnIndex = -1;
                    if (options.IncludeSourceFileName)
                    {
                        sourceFileColumnIndex = commonColumns[sheetName].IndexOf("Source File");
                    }

                    // Prepare for mandatory column validation
                    var mandatoryCols = SheetConfiguration.MandatoryColumns.TryGetValue(sheetName, out var mcols)
                        ? mcols.Where(c => c != "OS according to the configuration file").ToList()
                        : [];
                    var mandatoryColIndices = mandatoryCols
                        .Select(col => commonColumns[sheetName].IndexOf(col))
                        .Where(idx => idx >= 0)
                        .ToList();

                    // Extract data rows
                    for (int row = 2; row <= lastRow; row++)
                    {
                        var rowData = new XLCellValue[commonColumns[sheetName].Count];

                        // Only fill data for columns that exist in this file
                        foreach (var mapping in columnMapping)
                        {
                            var cell = worksheet.Cell(row, mapping.FileColumnIndex);
                            var cellValue = cell.Value;

                            // Apply anonymization if needed
                            if (options.AnonymizeData && anonymizeColumnIndices.TryGetValue(sheetName, out var sheetAnonymizeCols))
                            {
                                cellValue = _anonymizationService.AnonymizeValue(
                                    cellValue,
                                    mapping.CommonColumnIndex,
                                    sheetAnonymizeCols);
                            }

                            // Store the value
                            rowData[mapping.CommonColumnIndex] = cellValue;
                        }

                        // Validate mandatory columns (except "OS according to the configuration file")
                        bool hasEmptyMandatory = _validationService.HasEmptyMandatoryValues(rowData, mandatoryColIndices);
                        if (hasEmptyMandatory)
                        {
                            validationIssues.Add(new ValidationIssue(
                                fileName,
                                false,
                                $"Row {row} in sheet '{sheetName}' has empty value(s) in mandatory column(s) (excluding 'OS according to the configuration file')."
                            ));

                            if (options.SkipRowsWithEmptyMandatoryValues)
                            {
                                continue; // Skip this row
                            }
                        }

                        // Add source file name if the option is enabled
                        if (options.IncludeSourceFileName && sourceFileColumnIndex >= 0)
                        {
                            rowData[sourceFileColumnIndex] = _fileSystem.Path.GetFileName(filePath);
                        }

                        mergedData[sheetName].Add(rowData);
                    }
                }
            }
        });
    }

    private async Task CreateOutputFileAsync(
        string outputPath,
        List<string> availableSheets,
        Dictionary<string, List<XLCellValue[]>> mergedData,
        Dictionary<string, List<string>> commonColumns)
    {
        await Task.Run(() =>
        {
            using (var workbook = new XLWorkbook())
            {
                foreach (var sheetName in availableSheets)
                {
                    var worksheet = workbook.Worksheets.Add(sheetName);

                    // Write data to sheet
                    for (int row = 0; row < mergedData[sheetName].Count; row++)
                    {
                        for (int col = 0; col < mergedData[sheetName][row].Length; col++)
                        {
                            var cell = worksheet.Cell(row + 1, col + 1);
                            var value = mergedData[sheetName][row][col];

                            // Use SetValue which handles the type conversion properly
                            cell.SetValue(value);
                        }
                    }

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();
                }

                // Save the output file
                workbook.SaveAs(outputPath);
            }
        });
    }
}