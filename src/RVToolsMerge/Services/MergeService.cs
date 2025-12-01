//-----------------------------------------------------------------------
// <copyright file="MergeService.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using System.IO.Abstractions;
using ClosedXML.Excel;
using RVToolsMerge.Configuration;
using RVToolsMerge.Exceptions;
using RVToolsMerge.Models;
using RVToolsMerge.Services.Interfaces;
using Spectre.Console;

namespace RVToolsMerge.Services;

/// <summary>
/// Service for merging RVTools Excel files.
/// </summary>
public class MergeService : IMergeService
{
    private readonly IExcelService _excelService;
    private readonly IValidationService _validationService;
    private readonly IAnonymizationService _anonymizationService;
    private readonly ConsoleUIService _consoleUiService;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="MergeService"/> class.
    /// </summary>
    /// <param name="excelService">The Excel service.</param>
    /// <param name="validationService">The validation service.</param>
    /// <param name="anonymizationService">The anonymization service.</param>
    /// <param name="consoleUiService">The console UI service.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    public MergeService(
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
    /// </summary>
    /// <param name="filePaths">Array of file paths to process.</param>
    /// <param name="outputPath">Path where the merged file will be saved.</param>
    /// <param name="options">Merge configuration options.</param>
    /// <param name="validationIssues">List to store validation issues.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task MergeFilesAsync(
        string[] filePaths,
        string outputPath,
        MergeOptions options,
        List<ValidationIssue> validationIssues)
    {
        ValidateInputParameters(filePaths);

        // Validate that anonymization and all-sheets are not both enabled
        if (options.AnonymizeData && options.ProcessAllSheets)
        {
            throw new InvalidOperationException("Anonymization and processing all sheets cannot be enabled simultaneously. Anonymization is only supported for the core sheets (vInfo, vHost, vPartition, vMemory).");
        }

        // Copy the list to allow modification if we need to remove invalid files
        var validFilePaths = filePaths.ToList();

        // Validate files and process issues
        await ValidateFilesAsync(validFilePaths, options, validationIssues);

        if (validFilePaths.Count == 0)
        {
            throw new NoValidFilesException("No valid files to process after validation.");
        }

        // Determine available sheets
        var availableSheets = await AnalyzeSheetsAsync(validFilePaths, options);

        if (availableSheets.Count == 0)
        {
            throw new NoValidSheetsException("No valid sheets found across the input files.");
        }

        // Create a dictionary to store merged data (sheet name -> rows)
        var mergedData = new Dictionary<string, List<XLCellValue[]>>();

        // Create a dictionary to store common columns across all files (sheet name -> column names)
        var commonColumns = await IdentifyCommonColumnsAsync(validFilePaths, availableSheets, options);

        // Setup anonymization column indices if enabled
        var anonymizeColumnIndices = SetupAnonymizationColumnIndices(commonColumns, options);

        // Extract data from files
        Dictionary<string, AzureMigrateValidationResult>? azureMigrateResults = null;
        azureMigrateResults = await ExtractDataFromFilesAsync(validFilePaths, availableSheets, options, commonColumns,
            anonymizeColumnIndices, mergedData, validationIssues);

        // Create output file
        await CreateOutputFileAsync(outputPath, availableSheets, mergedData, commonColumns);

        // Create anonymization mapping file if anonymization is enabled
        if (options.AnonymizeData)
        {
            var anonymizationMappings = _anonymizationService.GetAnonymizationMappings();
            await CreateAnonymizationMapFileAsync(outputPath, anonymizationMappings);
        }

        // Create failed validation file if Azure Migrate validation is enabled
        if (options.EnableAzureMigrateValidation && azureMigrateResults != null && azureMigrateResults["vInfo"].TotalFailedRows > 0)
        {
            string failedValidationFilePath = _fileSystem.Path.Combine(
                _fileSystem.Path.GetDirectoryName(outputPath) ?? string.Empty,
                _fileSystem.Path.GetFileNameWithoutExtension(outputPath) + "_FailedAzureMigrateValidation.xlsx");

            await CreateAzureMigrateFailedValidationFileAsync(failedValidationFilePath, azureMigrateResults, commonColumns);

            // Display Azure Migrate validation statistics
            DisplayAzureMigrateValidationStatistics(azureMigrateResults, failedValidationFilePath);
        }

        // Display summary
        DisplaySummary(filePaths, validFilePaths, availableSheets, mergedData, commonColumns, options);
    }

    /// <summary>
    /// Validates input parameters for the merge operation.
    /// </summary>
    /// <param name="filePaths">Array of file paths to validate.</param>
    private void ValidateInputParameters(string[] filePaths)
    {
        if (filePaths.Length == 0)
        {
            throw new ArgumentException("No files specified for merging.", nameof(filePaths));
        }
    }

    /// <summary>
    /// Validates files and removes invalid ones if configured to do so.
    /// </summary>
    /// <param name="validFilePaths">List of file paths to validate and potentially modify.</param>
    /// <param name="options">Merge options.</param>
    /// <param name="validationIssues">List to store validation issues.</param>
    private async Task ValidateFilesAsync(List<string> validFilePaths, MergeOptions options, List<ValidationIssue> validationIssues)
    {
        _consoleUiService.DisplayInfo("[bold]Validating files...[/]");

        // First collect all validation results
        var fileValidationResults = new List<(string FilePath, bool IsValid, List<ValidationIssue> Issues)>();

        await Task.Run(() =>
        {
            _consoleUiService.Progress()
                .AutoClear(false)
                .Columns(
                [
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn()
                ])
                .Start(ctx =>
                {
                    var validationTask = ctx.AddTask("[green]Validating input files[/]", maxValue: validFilePaths.Count);

                    foreach (string filePath in validFilePaths)
                    {
                        var fileIssues = new List<ValidationIssue>();
                        bool isValid = _validationService.ValidateFile(filePath, options.IgnoreMissingOptionalSheets, fileIssues);

                        fileValidationResults.Add((filePath, isValid, fileIssues));
                        validationIssues.AddRange(fileIssues);

                        validationTask.Increment(1);
                    }
                });
        });

        // Display all validation issues together if any exist
        DisplayValidationIssues(validationIssues);

        // Now process the validation results based on options
        for (int i = validFilePaths.Count - 1; i >= 0; i--)
        {
            string filePath = validFilePaths[i];
            var validationResult = fileValidationResults.First(r => r.FilePath == filePath);

            if (!validationResult.IsValid && !options.SkipInvalidFiles)
            {
                // If file is not valid and we're not skipping invalid files, throw an exception

                var errorMessage = "At least one invalid file found. Use the -i or --skip-invalid-files option to skip invalid files.\n";
                throw new InvalidFileException(errorMessage);
            }

            if (!validationResult.IsValid && options.SkipInvalidFiles)
            {
                // Remove invalid file if skipping is enabled
                validFilePaths.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Displays validation issues in a formatted table.
    /// </summary>
    /// <param name="validationIssues">The list of validation issues to display.</param>
    private void DisplayValidationIssues(List<ValidationIssue> validationIssues)
    {
        if (validationIssues.Count > 0)
        {
            _consoleUiService.DisplayValidationIssues(validationIssues);
        }
    }

    /// <summary>
    /// Analyzes input files to determine which sheets are available across all files.
    /// </summary>
    /// <param name="validFilePaths">List of valid file paths to analyze.</param>
    /// <param name="options">Merge options.</param>
    /// <returns>A list of available sheet names.</returns>
    private async Task<List<string>> AnalyzeSheetsAsync(List<string> validFilePaths, MergeOptions options)
    {
        _consoleUiService.DisplayInfo("[bold]Analyzing input files...[/]");

        // Collect sheet names from all files
        var sheetsInFiles = new Dictionary<string, HashSet<string>>();

        await Task.Run(() =>
        {
            _consoleUiService.Progress()
                .AutoClear(false)
                .Columns(
                [
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn()
                ])
                .Start(ctx =>
                {
                    var analysisTask = ctx.AddTask("[green]Analyzing sheets[/]", maxValue: validFilePaths.Count);

                    foreach (var filePath in validFilePaths)
                    {
                        try
                        {
                            using var workbook = new XLWorkbook(filePath);
                            var fileSheets = new HashSet<string>();

                            foreach (var worksheet in workbook.Worksheets)
                            {
                                fileSheets.Add(worksheet.Name);
                            }

                            sheetsInFiles[filePath] = fileSheets;
                        }
                        catch (IOException ioEx)
                        {
                            // On Linux, provide more verbose error information for filesystem issues
                            _consoleUiService.MarkupLineInterpolated($"[yellow]Warning:[/] IO issue with file '{_fileSystem.Path.GetFileName(filePath)}': {ioEx.Message}");
                        }
                        analysisTask.Increment(1);
                    }
                });
        });

        // Check if vInfo is present in all files - it's required
        var allFilesHaveRequiredSheets = sheetsInFiles.Values.All(sheets => sheets.Contains("vInfo"));
        if (!allFilesHaveRequiredSheets)
        {
            throw new MissingRequiredSheetException("Some files are missing the required 'vInfo' sheet.");
        }

        // Find ALL unique sheets that are available across all files
        var allDiscoveredSheets = new HashSet<string>();
        foreach (var fileSheets in sheetsInFiles.Values)
        {
            foreach (var sheetName in fileSheets)
            {
                allDiscoveredSheets.Add(sheetName);
            }
        }

        // Determine which sheets to return based on options
        List<string> sheetsToReturn;
        if (options.ProcessAllSheets)
        {
            // Return all discovered sheets (dynamic discovery mode)
            sheetsToReturn = allDiscoveredSheets.ToList();
        }
        else
        {
            // Standard mode: only return the 4 core sheets if they exist
            sheetsToReturn = SheetConfiguration.RequiredSheets
                .Where(s => allDiscoveredSheets.Contains(s))
                .ToList();
        }

        _consoleUiService.MarkupLineInterpolated($"[cyan]Found sheets:[/] {string.Join(", ", sheetsToReturn)}");
        return sheetsToReturn;
    }

    /// <summary>
    /// Identifies columns that are common across all files for each sheet.
    /// </summary>
    /// <param name="validFilePaths">List of valid file paths to analyze.</param>
    /// <param name="availableSheets">List of available sheet names.</param>
    /// <param name="options">Merge options.</param>
    /// <returns>A dictionary mapping sheet names to lists of common column names.</returns>
    private async Task<Dictionary<string, List<string>>> IdentifyCommonColumnsAsync(
        List<string> validFilePaths,
        List<string> availableSheets,
        MergeOptions options)
    {
        _consoleUiService.DisplayInfo("[bold]Identifying common columns...[/]");

        var commonColumns = new Dictionary<string, List<string>>();

        await Task.Run(() =>
        {
            foreach (var sheetName in availableSheets)
            {
                var allFileColumns = new List<HashSet<string>>();

                _consoleUiService.Progress()
                    .AutoClear(false)
                    .Columns(
                    [
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new SpinnerColumn()
                    ])
                    .Start(ctx =>
                    {
                        var fileAnalysisTask = ctx.AddTask($"[cyan]Analyzing '{sheetName}' columns[/]", maxValue: validFilePaths.Count);

                        foreach (var filePath in validFilePaths)
                        {
                            try
                            {
                                using var workbook = new XLWorkbook(filePath);
                                if (_excelService.SheetExists(workbook, sheetName))
                                {
                                    var worksheet = workbook.Worksheet(sheetName);
                                    var columnNames = _excelService.GetColumnNames(worksheet);
                                    allFileColumns.Add(new HashSet<string>(columnNames));
                                }
                            }
                            catch (IOException ioEx)
                            {
                                // On Linux, provide more verbose error information for filesystem issues
                                _consoleUiService.MarkupLineInterpolated($"[yellow]Warning:[/] IO issue with file '{_fileSystem.Path.GetFileName(filePath)}': {ioEx.Message}");
                            }
                            fileAnalysisTask.Increment(1);
                        }
                    });

                // If only mandatory columns are requested, filter the common columns (only for known sheets)
                if (options.OnlyMandatoryColumns &&
                    SheetConfiguration.KnownSheets.Contains(sheetName) &&
                    SheetConfiguration.MandatoryColumns.TryGetValue(sheetName, out var mandatoryColumns))
                {
                    var columnsInAllFiles = allFileColumns.Count > 0
                        ? allFileColumns.Aggregate((current, next) => new HashSet<string>(current.Intersect(next)))
                            .Where(col => mandatoryColumns.Contains(col))
                            .ToList()
                        : [];

                    // Make sure at least mandatory columns are included
                    var mandatoryColumnsInAllFiles = new List<string>();
                    foreach (var column in mandatoryColumns)
                    {
                        if (allFileColumns.All(fileColumns => fileColumns.Contains(column)))
                        {
                            mandatoryColumnsInAllFiles.Add(column);
                        }
                    }

                    // Use only the mandatory columns that are present in all files
                    commonColumns[sheetName] = mandatoryColumnsInAllFiles;
                }
                else
                {
                    // Find columns that are present in all files (for both known and unknown sheets)
                    var columnsInAllFiles = allFileColumns.Count > 0
                        ? allFileColumns.Aggregate((current, next) => new HashSet<string>(current.Intersect(next))).ToList()
                        : [];

                    commonColumns[sheetName] = columnsInAllFiles;
                }
            }
        });

        // Add source file column if the option is enabled
        if (options.IncludeSourceFileName)
        {
            foreach (var sheetName in availableSheets)
            {
                commonColumns[sheetName].Add("Source File");
            }
        }

        return commonColumns;
    }

    /// <summary>
    /// Sets up anonymization column indices for each sheet.
    /// </summary>
    /// <param name="commonColumns">Dictionary of common columns for each sheet.</param>
    /// <param name="options">Merge options.</param>
    /// <returns>Dictionary mapping sheet names to dictionaries of column names to indices for anonymization.</returns>
    private Dictionary<string, Dictionary<string, int>> SetupAnonymizationColumnIndices(
        Dictionary<string, List<string>> commonColumns,
        MergeOptions options)
    {
        var anonymizeColumnIndices = new Dictionary<string, Dictionary<string, int>>();

        if (options.AnonymizeData)
        {
            // Get configured column identifiers from the service
            var columnIdentifiers = _anonymizationService.GetColumnIdentifiers();

            foreach (var sheetName in commonColumns.Keys)
            {
                var sheetAnonymizeCols = new Dictionary<string, int>();

                // Check all configured column identifiers against this sheet's columns
                foreach (var columnName in columnIdentifiers.Keys)
                {
                    AddColumnToAnonymize(commonColumns, sheetName, columnName, sheetAnonymizeCols);
                }

                if (sheetAnonymizeCols.Count > 0)
                {
                    anonymizeColumnIndices[sheetName] = sheetAnonymizeCols;
                }
            }
        }

        return anonymizeColumnIndices;
    }

    /// <summary>
    /// Adds a column to the anonymization index if it exists in the common columns.
    /// </summary>
    /// <param name="commonColumns">Dictionary of common columns for each sheet.</param>
    /// <param name="sheetName">The name of the sheet containing the column.</param>
    /// <param name="columnName">The name of the column to anonymize.</param>
    /// <param name="anonymizeColumns">Dictionary to add the column index to.</param>
    private static void AddColumnToAnonymize(
        Dictionary<string, List<string>> commonColumns,
        string sheetName,
        string columnName,
        Dictionary<string, int> anonymizeColumns)
    {
        int index = commonColumns[sheetName].IndexOf(columnName);
        if (index >= 0)
        {
            anonymizeColumns[columnName] = index;
        }
    }

    /// <summary>
    /// Extracts data from all valid files and merges it.
    /// </summary>
    /// <param name="validFilePaths">List of valid file paths to process.</param>
    /// <param name="availableSheets">List of available sheets to include.</param>
    /// <param name="options">Merge options.</param>
    /// <param name="commonColumns">Dictionary of common columns for each sheet.</param>
    /// <param name="anonymizeColumnIndices">Dictionary of column indices to anonymize.</param>
    /// <param name="mergedData">Dictionary to populate with merged data.</param>
    /// <param name="validationIssues">List to store validation issues.</param>
    /// <param name="azureMigrateResults">Output parameter for Azure Migrate validation results.</param>
    private async Task<Dictionary<string, AzureMigrateValidationResult>?> ExtractDataFromFilesAsync(
        List<string> validFilePaths,
        List<string> availableSheets,
        MergeOptions options,
        Dictionary<string, List<string>> commonColumns,
        Dictionary<string, Dictionary<string, int>> anonymizeColumnIndices,
        Dictionary<string, List<XLCellValue[]>> mergedData,
        List<ValidationIssue> validationIssues)
    {
        _consoleUiService.DisplayInfo("[bold]Extracting data from files...[/]");

        // Display anonymization message if enabled
        if (options.AnonymizeData)
        {
            _consoleUiService.DisplayInfo("[yellow]Anonymization enabled[/] - VM, DNS Name, IP Addresses, Cluster, Host, and Datacenter names will be anonymized.");
        }

        // Display Azure Migrate validation message if enabled
        if (options.EnableAzureMigrateValidation)
        {
            _consoleUiService.DisplayInfo("[yellow]Azure Migrate validation enabled[/] - Additional validation rules will be applied and rows that fail will be moved to a separate file.");
        }

        // Display MaxVInfoRows message if enabled
        if (options.MaxVInfoRows.HasValue)
        {
            _consoleUiService.DisplayInfo($"[yellow]vInfo row limiting enabled[/] - Only the first {options.MaxVInfoRows.Value} vInfo rows will be processed. Other sheets (vPartition, vMemory, vHost) will be filtered to match the selected VMs and hosts.");
        }

        // Create a dictionary to store Azure Migrate validation results if enabled
        Dictionary<string, AzureMigrateValidationResult>? azureMigrateValidationResults = null;
        if (options.EnableAzureMigrateValidation)
        {
            azureMigrateValidationResults = new Dictionary<string, AzureMigrateValidationResult>();
            foreach (var sheetName in availableSheets)
            {
                azureMigrateValidationResults[sheetName] = new AzureMigrateValidationResult();
            }
        }

        // Global counter for vInfo rows processed across all files
        int globalVInfoRowsProcessed = 0;

        // Set to store VM UUIDs from limited vInfo rows for sheet synchronization
        HashSet<string> includedVmUuids = new HashSet<string>();

        // Set to store Host names from limited vInfo rows for vHost sheet synchronization
        HashSet<string> includedHostNames = new HashSet<string>();

        await Task.Run(() =>
        {
            // Extract data using only common columns
            _consoleUiService.Progress()
                .AutoClear(false)
                .Columns(
                [
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn()
                ])
                .Start(ctx =>
                {
                    foreach (var sheetName in availableSheets)
                    {
                        mergedData[sheetName] = [
                            commonColumns[sheetName].Select(col => (XLCellValue)col).ToArray()
                        ];

                        var sheetTask = ctx.AddTask($"[cyan]Processing '{sheetName}'[/]", maxValue: validFilePaths.Count);

                        // For Azure Migrate validation of vInfo sheet, track unique VM UUIDs
                        HashSet<string>? seenVmUuids = null;
                        int vmUuidIndex = -1;
                        int osConfigIndex = -1;

                        if (options.EnableAzureMigrateValidation && sheetName == "vInfo")
                        {
                            seenVmUuids = new HashSet<string>();
                            vmUuidIndex = commonColumns[sheetName].IndexOf("VM UUID");
                            osConfigIndex = commonColumns[sheetName].IndexOf("OS according to the configuration file");
                        }

                        // Get VM UUID column index for sheet synchronization
                        int sheetVmUuidIndex = commonColumns[sheetName].IndexOf("VM UUID");

                        // Get Host column index for vHost sheet synchronization
                        int sheetHostIndex = commonColumns[sheetName].IndexOf("Host");

                        foreach (var filePath in validFilePaths)
                        {
                            var fileName = _fileSystem.Path.GetFileName(filePath);
                            using var workbook = new XLWorkbook(filePath);

                            if (!_excelService.SheetExists(workbook, sheetName))
                            {
                                sheetTask.Increment(1);
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

                            // Prepare for mandatory column validation (only for known sheets)
                            var mandatoryCols = SheetConfiguration.KnownSheets.Contains(sheetName) &&
                                                SheetConfiguration.MandatoryColumns.TryGetValue(sheetName, out var mcols)
                                ? mcols.Where(c => c != "OS according to the configuration file").ToList()
                                : [];
                            var mandatoryColIndices = mandatoryCols
                                .Select(col => commonColumns[sheetName].IndexOf(col))
                                .Where(idx => idx >= 0)
                                .ToList();

                            // Extract data rows
                            for (int row = 2; row <= lastRow; row++)
                            {
                                // Check vInfo row limit if applicable
                                if (sheetName == "vInfo" && options.MaxVInfoRows.HasValue && globalVInfoRowsProcessed >= options.MaxVInfoRows.Value)
                                {
                                    // We've reached the limit for vInfo rows, skip the rest
                                    break;
                                }

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
                                            sheetAnonymizeCols,
                                            fileName);
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

                                // Perform Azure Migrate validation if enabled
                                bool skipRowDueToAzureMigrateValidation = false;
                                if (options.EnableAzureMigrateValidation && sheetName == "vInfo" && azureMigrateValidationResults != null)
                                {
                                    var validationResult = azureMigrateValidationResults["vInfo"];

                                    // Check if we've reached the VM count limit
                                    if (validationResult.VmCountLimitReached)
                                    {
                                        skipRowDueToAzureMigrateValidation = true;
                                        validationResult.RowsSkippedAfterLimitReached++;
                                    }
                                    else
                                    {
                                        // Validate the row for Azure Migrate
                                        var failureReason = _validationService.ValidateRowForAzureMigrate(
                                            rowData,
                                            vmUuidIndex,
                                            osConfigIndex,
                                            seenVmUuids!,
                                            validationResult.TotalVmsProcessed);

                                        if (failureReason != null)
                                        {
                                            // Add to failed rows and skip
                                            validationResult.FailedRows.Add(new AzureMigrateValidationFailure(rowData, failureReason.Value));

                                            // Update counts based on failure reason
                                            switch (failureReason.Value)
                                            {
                                                case AzureMigrateValidationFailureReason.MissingVmUuid:
                                                    validationResult.MissingVmUuidCount++;
                                                    break;
                                                case AzureMigrateValidationFailureReason.MissingOsConfiguration:
                                                    validationResult.MissingOsConfigurationCount++;
                                                    break;
                                                case AzureMigrateValidationFailureReason.DuplicateVmUuid:
                                                    validationResult.DuplicateVmUuidCount++;
                                                    break;
                                                case AzureMigrateValidationFailureReason.VmCountExceeded:
                                                    validationResult.VmCountExceededCount++;
                                                    validationResult.VmCountLimitReached = true;

                                                    // Warn user that VM count limit has been reached
                                                    validationIssues.Add(new ValidationIssue(
                                                        fileName,
                                                        false,
                                                        "VM count limit of 20,000 has been reached for Azure Migrate. Additional VMs will not be included."
                                                    ));
                                                    break;
                                            }
                                            skipRowDueToAzureMigrateValidation = true;
                                        }
                                        else
                                        {
                                            // Row passed validation, increment VM count
                                            validationResult.TotalVmsProcessed++;
                                        }
                                    }
                                }

                                if (!skipRowDueToAzureMigrateValidation)
                                {
                                    // For vInfo sheet, collect VM UUID and Host if we have MaxVInfoRows limiting enabled
                                    if (sheetName == "vInfo" && options.MaxVInfoRows.HasValue)
                                    {
                                        if (sheetVmUuidIndex >= 0)
                                        {
                                            var vmUuid = rowData[sheetVmUuidIndex].ToString();
                                            if (!string.IsNullOrEmpty(vmUuid))
                                            {
                                                includedVmUuids.Add(vmUuid);
                                            }
                                        }

                                        if (sheetHostIndex >= 0)
                                        {
                                            var hostName = rowData[sheetHostIndex].ToString();
                                            if (!string.IsNullOrEmpty(hostName))
                                            {
                                                includedHostNames.Add(hostName);
                                            }
                                        }
                                    }

                                    // For non-vInfo sheets, check if we should filter based on vInfo limiting
                                    bool shouldIncludeRow = true;
                                    if (sheetName != "vInfo" && options.MaxVInfoRows.HasValue)
                                    {
                                        // Filter sheets with VM UUIDs based on included VM UUIDs
                                        if (sheetVmUuidIndex >= 0 && includedVmUuids.Count > 0)
                                        {
                                            var vmUuid = rowData[sheetVmUuidIndex].ToString();
                                            shouldIncludeRow = string.IsNullOrEmpty(vmUuid) || includedVmUuids.Contains(vmUuid);
                                        }
                                        // Filter vHost sheet based on included Host names
                                        else if (sheetName == "vHost" && sheetHostIndex >= 0 && includedHostNames.Count > 0)
                                        {
                                            var hostName = rowData[sheetHostIndex].ToString();
                                            shouldIncludeRow = string.IsNullOrEmpty(hostName) || includedHostNames.Contains(hostName);
                                        }
                                    }

                                    if (shouldIncludeRow)
                                    {
                                        mergedData[sheetName].Add(rowData);
                                    }

                                    // Increment global vInfo row counter if this is a vInfo sheet and row was included
                                    if (sheetName == "vInfo" && shouldIncludeRow)
                                    {
                                        globalVInfoRowsProcessed++;
                                    }
                                }
                            }

                            sheetTask.Increment(1);
                        }
                    }
                });
        });

        // Return the Azure Migrate validation results
        return options.EnableAzureMigrateValidation ? azureMigrateValidationResults : null;
    }

    /// <summary>
    /// Creates and saves the output file with merged data.
    /// </summary>
    /// <param name="outputPath">Path where the output file will be saved.</param>
    /// <param name="availableSheets">List of available sheets to include.</param>
    /// <param name="mergedData">Dictionary of merged data for each sheet.</param>
    /// <param name="commonColumns">Dictionary of common columns for each sheet.</param>
    private async Task CreateOutputFileAsync(
        string outputPath,
        List<string> availableSheets,
        Dictionary<string, List<XLCellValue[]>> mergedData,
        Dictionary<string, List<string>> commonColumns)
    {
        _consoleUiService.DisplayInfo("[bold]Creating output file...[/]");

        await _consoleUiService.Progress()
            .AutoClear(false)
            .Columns(
            [
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            ])
            .StartAsync(async ctx =>
            {
                var outputTask = ctx.AddTask("[green]Creating output file[/]", maxValue: availableSheets.Count + 1);

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
                        outputTask.Increment(1);
                    }

                    // Save the output file
                    _consoleUiService.DisplayInfo("[cyan]Saving file to disk...[/]");
                    await Task.Run(() => workbook.SaveAs(outputPath));
                    outputTask.Increment(1);
                }
            });
    }

    /// <summary>
    /// Creates and saves a file containing anonymization mappings.
    /// </summary>
    /// <param name="outputPath">Path where the output file was saved.</param>
    /// <param name="mappings">Dictionary of anonymization mappings.</param>
    private async Task CreateAnonymizationMapFileAsync(string outputPath, Dictionary<string, Dictionary<string, Dictionary<string, string>>> mappings)
    {
        string mapFilePath = _fileSystem.Path.Combine(
            _fileSystem.Path.GetDirectoryName(outputPath) ?? string.Empty,
            _fileSystem.Path.GetFileNameWithoutExtension(outputPath) + "_AnonymizationMapping.xlsx");

        _consoleUiService.DisplayInfo("[bold]Creating anonymization mapping file...[/]");

        await _consoleUiService.Progress()
            .AutoClear(false)
            .Columns(
            [
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            ])
            .StartAsync(async ctx =>
            {
                var mappingTask = ctx.AddTask("[green]Creating anonymization mapping file[/]", maxValue: mappings.Count + 1);

                using (var workbook = new XLWorkbook())
                {
                    foreach (var columnName in mappings.Keys)
                    {
                        if (mappings[columnName].Count == 0)
                        {
                            mappingTask.Increment(1);
                            continue;
                        }

                        var worksheet = workbook.Worksheets.Add(columnName);

                        // Add headers
                        worksheet.Cell(1, 1).Value = "File";
                        worksheet.Cell(1, 2).Value = "Original Value";
                        worksheet.Cell(1, 3).Value = "Anonymized Value";

                        // Style headers
                        var headerRow = worksheet.Row(1);
                        headerRow.Style.Font.Bold = true;

                        // Add data
                        int row = 2;
                        foreach (var fileEntry in mappings[columnName])
                        {
                            string fileName = fileEntry.Key;
                            var fileMap = fileEntry.Value;

                            foreach (var entry in fileMap)
                            {
                                worksheet.Cell(row, 1).Value = fileName;
                                worksheet.Cell(row, 2).Value = entry.Key;
                                worksheet.Cell(row, 3).Value = entry.Value;
                                row++;
                            }
                        }

                        // Auto-fit columns
                        worksheet.Columns().AdjustToContents();
                        mappingTask.Increment(1);
                    }

                    // Save the mapping file
                    _consoleUiService.DisplayInfo($"[cyan]Saving anonymization mapping file to: {mapFilePath}[/]");
                    await Task.Run(() => workbook.SaveAs(mapFilePath));
                    mappingTask.Increment(1);
                }
            });
    }

    /// <summary>
    /// Creates and saves a file containing rows that failed Azure Migrate validation.
    /// </summary>
    /// <param name="failedValidationFilePath">Path where the failed validation file will be saved.</param>
    /// <param name="azureMigrateResults">Dictionary of Azure Migrate validation results.</param>
    /// <param name="commonColumns">Dictionary of common columns for each sheet.</param>
    private async Task CreateAzureMigrateFailedValidationFileAsync(
        string failedValidationFilePath,
        Dictionary<string, AzureMigrateValidationResult> azureMigrateResults,
        Dictionary<string, List<string>> commonColumns)
    {
        _consoleUiService.DisplayInfo("[bold]Creating Azure Migrate failed validation file...[/]");

        await _consoleUiService.Progress()
            .AutoClear(false)
            .Columns(
            [
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            ])
            .StartAsync(async ctx =>
            {
                var outputTask = ctx.AddTask("[yellow]Creating failed validation file[/]", maxValue: azureMigrateResults.Count + 1);

                using (var workbook = new XLWorkbook())
                {
                    foreach (var sheetName in azureMigrateResults.Keys)
                    {
                        var validationResult = azureMigrateResults[sheetName];

                        if (validationResult.FailedRows.Count == 0)
                        {
                            outputTask.Increment(1);
                            continue;
                        }

                        var worksheet = workbook.Worksheets.Add(sheetName);

                        // Write headers
                        for (int col = 0; col < commonColumns[sheetName].Count; col++)
                        {
                            worksheet.Cell(1, col + 1).Value = commonColumns[sheetName][col];
                        }

                        // Add failure reason column
                        worksheet.Cell(1, commonColumns[sheetName].Count + 1).Value = "Failure Reason";

                        // Style headers
                        var headerRow = worksheet.Row(1);
                        headerRow.Style.Font.Bold = true;

                        // Write data rows
                        int row = 2;
                        foreach (var failedRow in validationResult.FailedRows)
                        {
                            // Write row data
                            for (int col = 0; col < failedRow.RowData.Length; col++)
                            {
                                worksheet.Cell(row, col + 1).Value = failedRow.RowData[col];
                            }

                            // Write failure reason
                            string failureReason = GetFailureReasonDescription(failedRow.Reason);
                            worksheet.Cell(row, commonColumns[sheetName].Count + 1).Value = failureReason;

                            row++;
                        }

                        // Auto-fit columns
                        worksheet.Columns().AdjustToContents();
                        outputTask.Increment(1);
                    }

                    // Save the file
                    _consoleUiService.DisplayInfo($"[cyan]Saving failed validation file to: {failedValidationFilePath}[/]");
                    await Task.Run(() => workbook.SaveAs(failedValidationFilePath));
                    outputTask.Increment(1);
                }
            });
    }

    /// <summary>
    /// Displays Azure Migrate validation statistics.
    /// </summary>
    /// <param name="azureMigrateResults">Dictionary of Azure Migrate validation results.</param>
    /// <param name="failedValidationFilePath">Path to the failed validation file.</param>
    private void DisplayAzureMigrateValidationStatistics(
        Dictionary<string, AzureMigrateValidationResult> azureMigrateResults,
        string failedValidationFilePath)
    {
        var vInfoResults = azureMigrateResults["vInfo"];

        // Create a table for validation statistics
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.Expand();

        // Add columns
        table.AddColumn("Validation Issue");
        table.AddColumn(new TableColumn("Count").Centered());

        // Add data rows
        table.AddRow("[yellow]Missing VM UUID[/]", vInfoResults.MissingVmUuidCount.ToString());
        table.AddRow("[yellow]Missing OS Configuration[/]", vInfoResults.MissingOsConfigurationCount.ToString());
        table.AddRow("[yellow]Duplicate VM UUID[/]", vInfoResults.DuplicateVmUuidCount.ToString());

        if (vInfoResults.VmCountLimitReached)
        {
            table.AddRow("[red]VM Count Limit Exceeded[/]", "True");
            table.AddRow("[red]Rows Not Processed Due to Limit[/]", vInfoResults.RowsSkippedAfterLimitReached.ToString());
        }

        table.AddRow("[green]Total VMs Processed[/]", vInfoResults.TotalVmsProcessed.ToString());
        table.AddRow("[red]Total Failed Rows[/]", vInfoResults.TotalFailedRows.ToString());

        _consoleUiService.WriteLine();
        _consoleUiService.DisplayInfo("[bold yellow]Azure Migrate Validation Results[/]");
        _consoleUiService.Write(table);

        _consoleUiService.MarkupLineInterpolated($"[cyan]Failed rows saved to: {_fileSystem.Path.GetFileName(failedValidationFilePath)}[/]");
        _consoleUiService.WriteLine();
    }

    /// <summary>
    /// Gets a human-readable description for a validation failure reason.
    /// </summary>
    /// <param name="reason">The validation failure reason.</param>
    /// <returns>A description of the failure reason.</returns>
    private string GetFailureReasonDescription(AzureMigrateValidationFailureReason reason)
    {
        return reason switch
        {
            AzureMigrateValidationFailureReason.MissingVmUuid => "Missing VM UUID",
            AzureMigrateValidationFailureReason.MissingOsConfiguration => "Missing OS Configuration",
            AzureMigrateValidationFailureReason.DuplicateVmUuid => "Duplicate VM UUID",
            AzureMigrateValidationFailureReason.VmCountExceeded => "VM Count Limit Exceeded (20,000 VMs maximum)",
            _ => "Unknown validation failure"
        };
    }

    /// <summary>
    /// Displays a summary of the merge operation.
    /// </summary>
    /// <param name="originalFilePaths">The original file paths.</param>
    /// <param name="validFilePaths">The valid file paths after validation.</param>
    /// <param name="availableSheets">The available sheets included in the merge.</param>
    /// <param name="mergedData">The merged data.</param>
    /// <param name="commonColumns">The common columns for each sheet.</param>
    /// <param name="options">The merge options used.</param>
    private void DisplaySummary(
        string[] originalFilePaths,
        List<string> validFilePaths,
        List<string> availableSheets,
        Dictionary<string, List<XLCellValue[]>> mergedData,
        Dictionary<string, List<string>> commonColumns,
        MergeOptions options)
    {
        _consoleUiService.WriteLine();
        _consoleUiService.WriteRule("Summary", "cyan");

        int totalFiles = originalFilePaths.Length;
        int skippedFiles = totalFiles - validFilePaths.Count;
        int totalSheets = availableSheets.Count;

        _consoleUiService.MarkupLineInterpolated($"[bold]Files:[/] Processed {validFilePaths.Count} valid files (skipped {skippedFiles} invalid files)");
        _consoleUiService.MarkupLineInterpolated($"[bold]Sheets:[/] Included {totalSheets} sheets ({string.Join(", ", availableSheets)})");

        // Display per-sheet stats
        _consoleUiService.WriteLine();
        var table = new Table();
        table.AddColumn("Sheet");
        table.AddColumn("Rows");
        table.AddColumn("Columns");

        foreach (var sheetName in availableSheets)
        {
            table.AddRow(
                $"[cyan]{sheetName}[/]",
                $"{mergedData[sheetName].Count - 1}", // -1 to exclude header row
                $"{commonColumns[sheetName].Count}"
            );
        }

        table.Border(TableBorder.Rounded);
        _consoleUiService.Write(table);

        // Display anonymization stats if enabled
        if (options.AnonymizeData)
        {
            DisplayAnonymizationSummary();
        }

        _consoleUiService.WriteLine();
    }

    /// <summary>
    /// Displays a summary of anonymization statistics.
    /// </summary>
    private void DisplayAnonymizationSummary()
    {
        _consoleUiService.WriteLine();
        _consoleUiService.WriteRule("Anonymization Summary", "yellow");

        var anonymizationStats = _anonymizationService.GetAnonymizationStatistics();

        // Create a table to display anonymization totals by column
        var table = new Table();
        table.AddColumn("Column");
        table.AddColumn("Total Items Anonymized");

        // Calculate totals for each column
        foreach (var columnName in anonymizationStats.Keys)
        {
            var fileStats = anonymizationStats[columnName];
            int totalForColumn = fileStats.Values.Sum();

            if (totalForColumn > 0)
            {
                table.AddRow(
                    $"[cyan]{columnName}[/]",
                    totalForColumn.ToString()
                );
            }
        }

        table.Border(TableBorder.Rounded);
        _consoleUiService.Write(table);
    }
}
