//-----------------------------------------------------------------------
// <copyright file="MergeService.cs" company="Stefan Broenner"> ">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
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

    /// <summary>
    /// Initializes a new instance of the <see cref="MergeService"/> class.
    /// </summary>
    /// <param name="excelService">The Excel service.</param>
    /// <param name="validationService">The validation service.</param>
    /// <param name="anonymizationService">The anonymization service.</param>
    /// <param name="consoleUiService">The console UI service.</param>
    public MergeService(
        IExcelService excelService,
        IValidationService validationService,
        IAnonymizationService anonymizationService,
        ConsoleUIService consoleUiService)
    {
        _excelService = excelService;
        _validationService = validationService;
        _anonymizationService = anonymizationService;
        _consoleUiService = consoleUiService;
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
    {        // Ensure we have files to process
        if (filePaths.Length == 0)
        {
            throw new ArgumentException("No files specified for merging.", nameof(filePaths));
        }

        // Track which files to process (all by default)
        var validFilePaths = new List<string>(filePaths);

        // Validate files first
        _consoleUiService.DisplayInfo("[bold]Validating files...[/]");// Track which sheets actually exist in all files
        var availableSheets = new List<string>(SheetConfiguration.RequiredSheets);

        // First pass to check which files are valid
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
                // Create a validation task
                var validationTask = ctx.AddTask("[green]Validating files[/]", maxValue: validFilePaths.Count);

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

                    validationTask.Increment(1);
                }
            });        // Display validation issues immediately after validation is complete
        if (validationIssues.Count > 0)
        {
            _consoleUiService.WriteLine();
            _consoleUiService.WriteRule("[yellow]Validation Issues[/]", "grey");

            // Group issues by filename
            var groupedIssues = validationIssues
                .GroupBy(issue => issue.FileName)
                .OrderBy(group => group.Key)
                .ToList();

            var table = new Table();
            table.AddColumn(new TableColumn("File Name").LeftAligned());
            table.AddColumn(new TableColumn("Status").Centered());
            table.AddColumn(new TableColumn("Details").LeftAligned());

            foreach (var group in groupedIssues)
            {
                var filename = group.Key;
                var fileIssues = group.ToList();

                // Determine the overall status for this file
                bool anySkipped = fileIssues.Any(issue => issue.Skipped);
                string status = anySkipped ? "[yellow]Skipped[/]" : "[green]Processed with warning[/]";

                // Combine all validation errors for this file
                var errorDetails = fileIssues
                    .Select(issue => issue.ValidationError)
                    .Distinct()
                    .Select(error => $"• {error}")
                    .ToList();

                string details = string.Join("\n", errorDetails);

                table.AddRow(
                    $"[cyan]{filename}[/]",
                    status,
                    $"[grey]{details}[/]"
                );
            }            table.Border(TableBorder.Rounded);
            _consoleUiService.Write(table);
            _consoleUiService.WriteLine();

            int totalFiles = groupedIssues.Count;
            int totalIssues = validationIssues.Count;
            _consoleUiService.MarkupLineInterpolated($"[yellow]Total of {totalIssues} validation issues across {totalFiles} files.[/]");
            _consoleUiService.WriteLine();
        }

        // Check if we have valid files to process
        if (validFilePaths.Count == 0)
        {
            throw new FileValidationException("No valid files to process.");
        }

        // Dictionary to store merged data for each sheet - using XLCellValue arrays to preserve data types
        var mergedData = new Dictionary<string, List<XLCellValue[]>>();

        // Dictionary to store common columns for each sheet
        var commonColumns = new Dictionary<string, List<string>>();        // Dictionary to store column indices for anonymization
        var anonymizeColumnIndices = new Dictionary<string, Dictionary<string, int>>();

        _consoleUiService.DisplayInfo("[bold]Analyzing columns...[/]");

        // First determine what columns are available across all files for each sheet
        foreach (var sheetName in availableSheets)
        {
            var allFileColumns = new List<List<string>>();            // First analyze all files to collect column information
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
                    // Create main task for analysis
                    var fileAnalysisTask = ctx.AddTask($"[cyan]Analyzing '{sheetName}' sheet[/]", maxValue: validFilePaths.Count);

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
                        }                        catch (IOException ioEx) when (options.DebugMode)
                        {
                            // On Linux, provide more verbose error information for filesystem issues
                            _consoleUiService.MarkupLineInterpolated($"[yellow]Warning:[/] IO issue with file '{Path.GetFileName(filePath)}': {ioEx.Message}");
                        }
                        fileAnalysisTask.Increment(1);
                    }
                });

            // If only mandatory columns are requested, filter the common columns
            if (options.OnlyMandatoryColumns && SheetConfiguration.MandatoryColumns.TryGetValue(sheetName, out var mandatoryColumns))
            {
                var columnsInAllFiles = allFileColumns.Count > 0
                    ? allFileColumns.Aggregate((a, b) => a.Intersect(b).ToList())
                    : [];

                foreach (var col in mandatoryColumns)
                {                    if (!columnsInAllFiles.Contains(col))
                    {
                        _consoleUiService.MarkupLineInterpolated($"[yellow]Warning:[/] Mandatory column '[cyan]{col}[/]' for sheet '[green]{sheetName}[/]' is missing from common columns.");
                    }
                }

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

            // Setup anonymization column indices for this sheet
            if (options.AnonymizeData)
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
        }        _consoleUiService.DisplayInfo("[bold]Extracting data from files...[/]");

        // Display anonymization message if enabled
        if (options.AnonymizeData)
        {
            _consoleUiService.DisplayInfo("[yellow]Anonymization enabled[/] - VM, DNS Name, Cluster, Host, and Datacenter names will be anonymized.");
        }        // Extract data using only common columns
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

                    foreach (var filePath in validFilePaths)
                    {
                        var fileName = Path.GetFileName(filePath);
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
                                rowData[sourceFileColumnIndex] = Path.GetFileName(filePath);
                            }

                            mergedData[sheetName].Add(rowData);
                        }

                        sheetTask.Increment(1);
                    }
                }
            });        // Write merged data to output file
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
                    }                    // Save the output file
                    _consoleUiService.DisplayInfo("[cyan]Saving file to disk...[/]");
                    await Task.Run(() => workbook.SaveAs(outputPath));
                    outputTask.Increment(1);
                }
            });

        // Display summary
        var summaryTable = new Table();
        summaryTable.AddColumn(new TableColumn("Category").LeftAligned());
        summaryTable.AddColumn(new TableColumn("Details").RightAligned());
        summaryTable.AddRow("[cyan]Files Processed[/]", $"[green]{validFilePaths.Count}[/] of [green]{filePaths.Length}[/]");
        summaryTable.AddRow("[cyan]Sheets Included[/]", $"[green]{availableSheets.Count}[/]");

        int totalRows = 0;
        foreach (var sheetName in availableSheets)
        {
            int rowCount = mergedData[sheetName].Count - 1; // Subtract 1 for header row
            int colCount = commonColumns.TryGetValue(sheetName, out var cols) ? cols.Count : 0;
            totalRows += rowCount;
            summaryTable.AddRow(
                $"[yellow]{sheetName}[/] rows",
                $"[green]{rowCount}[/] ([grey]{colCount} columns[/])"
            );
        }        summaryTable.Border(TableBorder.Rounded);
        _consoleUiService.Write(summaryTable);

        // Display anonymization summary if enabled
        if (options.AnonymizeData)
        {
            _consoleUiService.WriteLine();
            _consoleUiService.WriteRule("[yellow]Anonymization Summary[/]", "grey");
            var table = new Table();
            table.AddColumn(new TableColumn("Category").Centered());
            table.AddColumn(new TableColumn("Count").Centered());

            var stats = _anonymizationService.GetAnonymizationStatistics();
            foreach (var kvp in stats)
            {
                table.AddRow($"[cyan]{kvp.Key}[/]", $"[green]{kvp.Value}[/]");            }

            table.Border(TableBorder.Rounded);
            _consoleUiService.Write(table);
        }
    }
}
