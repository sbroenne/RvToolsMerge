//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="RVTools">
//     Copyright © RVTools Team 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using ClosedXML.Excel;
using Spectre.Console;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RVToolsMerge;

/// <summary>
/// The main program class containing the application logic for merging RVTools Excel files.
/// </summary>
class Program
{
    /// <summary>
    /// Names of sheets that are required by default.
    /// </summary>
    private static readonly string[] RequiredSheets = ["vInfo", "vHost", "vPartition", "vMemory"];

    /// <summary>
    /// Names of sheets that are minimally required.
    /// </summary>
    private static readonly string[] MinimumRequiredSheets = ["vInfo"];

    /// <summary>
    /// Record to store information about validation issues.
    /// </summary>
    private record ValidationIssue(string FileName, bool Skipped, string ValidationError);

    /// <summary>
    /// Record to store column mapping information.
    /// </summary>
    private record ColumnMapping(int FileColumnIndex, int CommonColumnIndex);

    /// <summary>
    /// Mandatory columns for each sheet type.
    /// </summary>
    private static readonly FrozenDictionary<string, string[]> MandatoryColumns = new Dictionary<string, string[]>
    {
        { "vInfo", ["Template", "SRM Placeholder", "Powerstate", "VM", "CPUs", "Memory", "In Use MiB", "OS according to the VMware Tools"] },
        { "vHost", ["Host", "Datacenter", "Cluster", "CPU Model", "Speed", "# CPU", "Cores per CPU", "# Cores", "CPU usage %", "# Memory", "Memory usage %"] },
        { "vPartition", ["VM", "Disk", "Capacity MiB", "Consumed MiB"] },
        { "vMemory", ["VM", "Size MiB", "Reservation"] }
    }.ToFrozenDictionary();

    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    static async Task Main(string[] args)
    {
        // Get version information from assembly
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        string versionString = version is not null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";

        // Get product name from assembly attributes
        var productAttribute = Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute)) as AssemblyProductAttribute;
        string productName = productAttribute?.Product ?? "RVTools Merger";

        // Enhanced header display with Spectre.Console
        AnsiConsole.MarkupLine($"[bold green]{productName}[/] [yellow]v{versionString}[/]");
        AnsiConsole.Write(new Rule().RuleStyle("grey"));

        // Command line options
        bool ignoreMissingOptionalSheets = false;
        bool skipInvalidFiles = false;
        bool anonymizeData = false;
        bool onlyMandatoryColumns = false;
        bool debugMode = false;
        bool includeSourceFileName = false;

        // Process options
        var processedArgs = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help" or "/?":
                    ShowHelp();
                    return;
                case "-m" or "--ignore-missing-sheets":
                    ignoreMissingOptionalSheets = true;
                    break;
                case "-i" or "--skip-invalid-files":
                    skipInvalidFiles = true;
                    break;
                case "-a" or "--anonymize":
                    anonymizeData = true;
                    break;
                case "-M" or "--only-mandatory-columns":
                    onlyMandatoryColumns = true;
                    break;
                case "-d" or "--debug":
                    debugMode = true;
                    break;
                case "-s" or "--include-source":
                    includeSourceFileName = true;
                    break;
                default:
                    processedArgs.Add(args[i]);
                    break;
            }
        }

        // Get input path (required)
        if (processedArgs.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Input path is required.");
            AnsiConsole.MarkupLine("Use --help to see usage information.");
            return;
        }

        string inputPath = processedArgs[0];

        // Determine if the input path is a file or directory
        bool isInputFile = File.Exists(inputPath);
        bool isInputDirectory = Directory.Exists(inputPath);

        if (!isInputFile && !isInputDirectory)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Error:[/] Input path '[yellow]{inputPath}[/]' does not exist as either a file or directory.");
            AnsiConsole.MarkupLine("Use --help to see usage information.");
            return;
        }

        // Get output file path (default to "RVTools_Merged.xlsx" if not specified)
        string outputFile = processedArgs.Count > 1
            ? processedArgs[1]
            : Path.Combine(Directory.GetCurrentDirectory(), "RVTools_Merged.xlsx");

        try
        {
            // Get Excel files - either the single file or all Excel files in the directory
            string[] excelFiles = [];
            if (isInputFile)
            {
                // Check if the file has the correct extension
                if (!Path.GetExtension(inputPath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Error:[/] Input file '[yellow]{inputPath}[/]' must be an Excel file (.xlsx).");
                    return;
                }
                excelFiles = [inputPath];
                AnsiConsole.MarkupLineInterpolated($"[green]Processing single Excel file: {Path.GetFileName(inputPath)}[/]");
            }
            else // isInputDirectory
            {
                excelFiles = Directory.GetFiles(inputPath, "*.xlsx");
                if (excelFiles.Length == 0)
                {
                    AnsiConsole.MarkupLineInterpolated($"[yellow]No Excel files found in '{inputPath}'.[/]");
                    return;
                }
                AnsiConsole.MarkupLineInterpolated($"[green]Found {excelFiles.Length} Excel files to process in directory.[/]");
            }

            // Process the files - collect validation issues
            var validationIssues = new List<ValidationIssue>();
            await MergeRVToolsFilesAsync(
                excelFiles,
                outputFile,
                ignoreMissingOptionalSheets,
                skipInvalidFiles,
                anonymizeData,
                onlyMandatoryColumns,
                includeSourceFileName,
                validationIssues,
                debugMode
            );

            AnsiConsole.MarkupLineInterpolated($"[green]Successfully merged files.[/] Output saved to: [blue]{outputFile}[/]");

            // Get product name from assembly attributes
            var finalProductAttr = Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyProductAttribute)) as AssemblyProductAttribute;
            string finalProdName = finalProductAttr?.Product ?? "RVToolsMerge";

            AnsiConsole.MarkupLineInterpolated($"Thank you for using [green]{finalProdName}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {GetUserFriendlyErrorMessage(ex)}");
            if (debugMode && ex.StackTrace != null)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Debug information (stack trace):[/]");
                AnsiConsole.WriteLine(ex.StackTrace);
            }
            else
            {
                AnsiConsole.MarkupLine("[grey]Use --debug flag to see detailed error information.[/]");
            }
        }
    }

    /// <summary>
    /// Displays validation issues in a formatted table.
    /// </summary>
    /// <param name="issues">The list of validation issues to display.</param>
    private static void DisplayValidationIssues(List<ValidationIssue> issues)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[yellow]Validation Issues[/]").RuleStyle("grey"));

        var table = new Table();
        table.AddColumn(new TableColumn("File Name").LeftAligned());
        table.AddColumn(new TableColumn("Status").Centered());
        table.AddColumn(new TableColumn("Details").LeftAligned());

        foreach (var issue in issues)
        {
            table.AddRow(
                $"[cyan]{issue.FileName}[/]",
                issue.Skipped ? "[yellow]Skipped[/]" : "[green]Processed with warning[/]",
                $"[grey]{issue.ValidationError}[/]"
            );
        }

        table.Border(TableBorder.Rounded);
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLineInterpolated($"[yellow]Total of {issues.Count} validation issues detected.[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Returns a user-friendly error message based on the exception type.
    /// </summary>
    /// <param name="ex">The exception to process.</param>
    /// <returns>A user-friendly error message.</returns>
    private static string GetUserFriendlyErrorMessage(Exception ex) => ex switch
    {
        FileNotFoundException => $"File not found: {ex.Message}",
        UnauthorizedAccessException => "Access denied. Please check if you have the necessary permissions.",
        IOException ioEx when ioEx.Message.Contains("being used by another process") =>
            "The output file is being used by another application. Please close it and try again.",
        _ when ex.Message.Contains("No valid files to process") =>
            "No valid files to process. Ensure your input folder contains valid RVTools Excel files.",
        _ => ex.Message // For other exceptions, just return the message
    };

    /// <summary>
    /// Merges multiple RVTools Excel files into a single consolidated file.
    /// </summary>
    /// <param name="filePaths">Array of file paths to process.</param>
    /// <param name="outputPath">Path where the merged file will be saved.</param>
    /// <param name="ignoreMissingOptionalSheets">Whether to ignore missing optional sheets.</param>
    /// <param name="skipInvalidFiles">Whether to skip invalid files.</param>
    /// <param name="anonymizeData">Whether to anonymize sensitive data.</param>
    /// <param name="onlyMandatoryColumns">Whether to include only mandatory columns.</param>
    /// <param name="includeSourceFileName">Whether to include source file name in output.</param>
    /// <param name="validationIssues">List to store validation issues.</param>
    /// <param name="debugMode">Whether debug mode is enabled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    static async Task MergeRVToolsFilesAsync(
        string[] filePaths,
        string outputPath,
        bool ignoreMissingOptionalSheets = false,
        bool skipInvalidFiles = false,
        bool anonymizeData = false,
        bool onlyMandatoryColumns = false,
        bool includeSourceFileName = false,
        List<ValidationIssue>? validationIssues = null,
        bool debugMode = false)
    {
        // Create validation issues list if not provided
        validationIssues ??= [];

        if (filePaths.Length == 0) return;

        // Dictionary to store merged data for each sheet - using XLCellValue arrays to preserve data types
        var mergedData = new Dictionary<string, List<XLCellValue[]>>();

        // Dictionary to store common columns for each sheet
        var commonColumns = new Dictionary<string, List<string>>();

        // Dictionaries for anonymization
        var vmNameMap = new Dictionary<string, string>();
        var dnsNameMap = new Dictionary<string, string>();
        var clusterNameMap = new Dictionary<string, string>();
        var hostNameMap = new Dictionary<string, string>();
        var datacenterNameMap = new Dictionary<string, string>();

        // Track which files to process (all by default)
        var validFilePaths = new List<string>(filePaths);
        var skippedFiles = new List<string>();

        // Validate required sheets in all files first
        AnsiConsole.MarkupLine("[bold]Validating files...[/]");
        // Track which sheets actually exist in all files
        var availableSheets = new List<string>(RequiredSheets);

        // First pass to check which files are valid
        AnsiConsole.Progress()
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

                // Add subtasks for each validation step
                var basicValidationTask = ctx.AddTask("[cyan]Basic validation[/]", maxValue: validFilePaths.Count);
                var requiredSheetTask = ctx.AddTask("[cyan]Checking required sheets[/]", maxValue: validFilePaths.Count);
                var columnValidationTask = ctx.AddTask("[cyan]Validating columns[/]", maxValue: validFilePaths.Count);

                for (int i = validFilePaths.Count - 1; i >= 0; i--)
                {
                    string filePath = validFilePaths[i];
                    string fileName = Path.GetFileName(filePath);
                    bool fileIsValid = true;

                    try
                    {
                        using (var workbook = new XLWorkbook(filePath))
                        {
                            basicValidationTask.Increment(1);

                            // Step 1: Validate vInfo sheet (always required)
                            if (!SheetExists(workbook, "vInfo"))
                            {
                                fileIsValid = false;
                                if (skipInvalidFiles)
                                {
                                    // Add to validation issues instead of outputting immediately
                                    validationIssues.Add(new ValidationIssue(
                                        fileName,
                                        true,
                                        "Missing essential 'vInfo' sheet which is required for processing."
                                    ));
                                    skippedFiles.Add(fileName);
                                }
                                else
                                {
                                    throw new FileValidationException(
                                        $"The file '{fileName}' is missing the essential 'vInfo' sheet. This sheet is required for processing. Use --skip-invalid-files to skip these files."
                                    );
                                }
                            }
                            else
                            {
                                // vInfo exists, check its mandatory columns
                                var worksheet = workbook.Worksheet("vInfo");
                                var columnNames = GetColumnNames(worksheet);

                                requiredSheetTask.Increment(1);

                                if (MandatoryColumns.TryGetValue("vInfo", out var mandatoryColumns))
                                {
                                    var missingColumns = mandatoryColumns.Where(col => !columnNames.Contains(col)).ToList();

                                    if (missingColumns.Count > 0)
                                    {
                                        fileIsValid = false;
                                        if (skipInvalidFiles)
                                        {
                                            validationIssues.Add(new ValidationIssue(
                                                fileName,
                                                true,
                                                $"'vInfo' sheet is missing mandatory column(s): {string.Join(", ", missingColumns)}"
                                            ));
                                            skippedFiles.Add(fileName);
                                        }
                                        else
                                        {
                                            throw new FileValidationException(
                                                $"The file '{fileName}', sheet 'vInfo' is missing required column(s): {string.Join(", ", missingColumns)}. These columns are necessary for proper data merging."
                                            );
                                        }
                                    }
                                }
                            }

                            // Step 2: If the file is still valid, check the optional sheets if they exist
                            if (fileIsValid)
                            {
                                // Check each optional sheet (vHost, vPartition, vMemory)
                                foreach (var sheetName in RequiredSheets.Where(s => s != "vInfo"))
                                {
                                    if (SheetExists(workbook, sheetName))
                                    {
                                        // Optional sheet exists, validate its mandatory columns
                                        var worksheet = workbook.Worksheet(sheetName);
                                        var columnNames = GetColumnNames(worksheet);

                                        if (MandatoryColumns.TryGetValue(sheetName, out var mandatoryColumns))
                                        {
                                            var missingColumns = mandatoryColumns.Where(col => !columnNames.Contains(col)).ToList();

                                            if (missingColumns.Count > 0)
                                            {
                                                // For optional sheets with missing columns:
                                                // If -i (skipInvalidFiles) is set, skip the entire file
                                                // If -m (ignoreMissingOptionalSheets) is set, just log a warning but keep the file
                                                if (skipInvalidFiles)
                                                {
                                                    fileIsValid = false;
                                                    validationIssues.Add(new ValidationIssue(
                                                        fileName,
                                                        true,
                                                        $"Sheet '{sheetName}' is missing mandatory column(s): {string.Join(", ", missingColumns)}"
                                                    ));
                                                    skippedFiles.Add(fileName);
                                                    break;
                                                }
                                                else if (!ignoreMissingOptionalSheets)
                                                {
                                                    // Only throw error if we're not ignoring optional sheets
                                                    throw new FileValidationException(
                                                        $"The file '{fileName}', sheet '{sheetName}' is missing required column(s): {string.Join(", ", missingColumns)}. Use --ignore-missing-optional-sheets to process files with missing or incomplete optional sheets."
                                                    );
                                                }
                                                else
                                                {
                                                    // Add a warning but continue processing the file
                                                    validationIssues.Add(new ValidationIssue(
                                                        fileName,
                                                        false,
                                                        $"Sheet '{sheetName}' has missing column(s): {string.Join(", ", missingColumns)}. This sheet may be excluded from processing."
                                                    ));
                                                }
                                            }
                                        }
                                    }
                                    else if (!ignoreMissingOptionalSheets)
                                    {
                                        // Optional sheet is missing and we're not ignoring missing sheets
                                        fileIsValid = false;
                                        if (skipInvalidFiles)
                                        {
                                            validationIssues.Add(new ValidationIssue(
                                                fileName,
                                                true,
                                                $"Missing optional sheet '{sheetName}'"
                                            ));
                                            skippedFiles.Add(fileName);
                                            break;
                                        }
                                        else
                                        {
                                            throw new FileValidationException(
                                                $"The file '{fileName}' is missing the '{sheetName}' sheet. Use --ignore-missing-optional-sheets to process files with missing optional sheets."
                                            );
                                        }
                                    }
                                    else
                                    {
                                        // Sheet is missing but we're ignoring missing sheets, add to validation issues
                                        validationIssues.Add(new ValidationIssue(
                                            fileName,
                                            false,
                                            $"Missing optional sheet '{sheetName}'. This sheet will be excluded from processing."
                                        ));
                                    }
                                }

                                columnValidationTask.Increment(1);
                            }
                            else
                            {
                                // If not valid after required sheet check, still increment the column validation task
                                // to keep progress consistent
                                columnValidationTask.Increment(1);
                            }
                        }
                    }
                    catch (Exception ex) when (skipInvalidFiles)
                    {
                        // If we're skipping invalid files and there's any error opening the file, skip it
                        var friendlyMessage = GetUserFriendlyErrorMessage(ex);
                        validationIssues.Add(new ValidationIssue(
                            fileName,
                            true,
                            $"Error: {friendlyMessage}"
                        ));
                        fileIsValid = false;
                        skippedFiles.Add(fileName);

                        // Ensure we increment all tasks even if we hit an exception
                        if (basicValidationTask.MaxValue > basicValidationTask.Value)
                            basicValidationTask.Increment(1);
                        if (requiredSheetTask.MaxValue > requiredSheetTask.Value)
                            requiredSheetTask.Increment(1);
                        if (columnValidationTask.MaxValue > columnValidationTask.Value)
                            columnValidationTask.Increment(1);
                    }

                    if (!fileIsValid && skipInvalidFiles)
                    {
                        validFilePaths.RemoveAt(i);
                    }

                    validationTask.Increment(1);
                }

                // Complete any tasks that might not have reached 100% due to skipping files
                basicValidationTask.StopTask();
                requiredSheetTask.StopTask();
                columnValidationTask.StopTask();

                if (skippedFiles.Count > 0)
                {
                    AnsiConsole.MarkupLineInterpolated($"[yellow]Warning:[/] Skipped [green]{skippedFiles.Count}[/] invalid files out of [green]{filePaths.Length}[/]. See validation issues for details.");
                }

                if (validFilePaths.Count == 0)
                {
                    throw new InvalidDataException("No valid files to process. Please check that your input folder contains valid RVTools Excel files with the required sheets.");
                }
            });

        // Display validation issues immediately after validation is complete
        if (validationIssues.Count > 0)
        {
            DisplayValidationIssues(validationIssues);
        }

        // Check if any of the optional sheets are missing
        // If we're ignoring missing optional sheets, we need to check how many files have missing optional sheets
        if (ignoreMissingOptionalSheets)
        {
            // Check how many file names have warning but not been skipped
            var processedWithWarningCount = validationIssues.Where(issue => !issue.Skipped).DistinctBy(issue => issue.FileName).Count();
            if (processedWithWarningCount > 0)
            {
                AnsiConsole.MarkupLineInterpolated($"[yellow]Warning:[/] {processedWithWarningCount} files have missing optional sheets. Only processing the vInfo sheet for these files. Your data will be incomplete.");
            }
        }

        AnsiConsole.MarkupLineInterpolated($"[bold]Processing {validFilePaths.Count} valid files...[/]");
        AnsiConsole.MarkupLine("[bold]Analyzing columns...[/]");

        // First determine what columns are available across all files for each sheet
        foreach (var sheetName in availableSheets)
        {
            var allFileColumns = new List<List<string>>();

            // First analyze all files to collect column information
            AnsiConsole.MarkupLineInterpolated($"Analyzing columns for sheet '[cyan]{sheetName}[/]'...");

            AnsiConsole.Progress()
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

                    // Add subtasks for different aspects of column analysis
                    var readingColumnsTask = ctx.AddTask($"[grey]Reading column headers[/]", maxValue: validFilePaths.Count);
                    var processingColumnsTask = ctx.AddTask($"[grey]Processing columns[/]", maxValue: validFilePaths.Count);

                    foreach (var filePath in validFilePaths)
                    {
                        try
                        {
                            using (var workbook = new XLWorkbook(filePath))
                            {
                                if (SheetExists(workbook, sheetName))
                                {
                                    var worksheet = workbook.Worksheet(sheetName);
                                    var columnNames = GetColumnNames(worksheet);
                                    allFileColumns.Add(columnNames);
                                }
                            }
                        }
                        catch (IOException ioEx) when (debugMode)
                        {
                            // On Linux, provide more verbose error information for filesystem issues
                            AnsiConsole.MarkupLineInterpolated($"[yellow]Warning:[/] IO issue with file '{Path.GetFileName(filePath)}': {ioEx.Message}");
                        }
                        readingColumnsTask.Increment(1);
                        processingColumnsTask.Increment(1);
                        fileAnalysisTask.Increment(1);
                    }

                    // Make sure all tasks complete
                    readingColumnsTask.StopTask();
                    processingColumnsTask.StopTask();
                });

            // If only mandatory columns are requested, filter the common columns
            if (onlyMandatoryColumns && MandatoryColumns.TryGetValue(sheetName, out var mandatoryColumns))
            {
                var processedMandatoryColumns = mandatoryColumns
                    .Select(col => col.Contains("vInfo") ? col.Replace("vInfo", "").Trim() : col)
                    .ToList();

                var columnsInAllFiles = allFileColumns.Count > 0
                    ? allFileColumns.Aggregate((a, b) => a.Intersect(b).ToList())
                    : [];

                foreach (var col in processedMandatoryColumns)
                {
                    if (!columnsInAllFiles.Contains(col))
                    {
                        AnsiConsole.MarkupLineInterpolated($"[yellow]Warning:[/] Mandatory column '[cyan]{col}[/]' for sheet '[green]{sheetName}[/]' is missing from common columns.");
                    }
                }

                commonColumns[sheetName] = columnsInAllFiles.Intersect(processedMandatoryColumns).ToList();
            }
            else
            {
                // Find columns that exist in all files for this sheet
                var columnsInAllFiles = allFileColumns.Count > 0
                    ? allFileColumns.Aggregate((a, b) => a.Intersect(b).ToList())
                    : [];

                // If includeSourceFileName option is enabled, prepare to add source file column
                if (includeSourceFileName)
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

            AnsiConsole.MarkupLineInterpolated($"Sheet '[green]{sheetName}[/]' has [yellow]{commonColumns[sheetName].Count}[/] {(onlyMandatoryColumns ? "mandatory" : "common")} columns across all files.");
        }

        AnsiConsole.MarkupLine("[bold]Extracting data from files...[/]");
        // Display anonymization message if enabled
        if (anonymizeData)
        {
            AnsiConsole.MarkupLine("[yellow]Anonymization enabled[/] - VM, DNS Name, Cluster, Host, and Datacenter names will be anonymized.");
        }

        // Second pass: Extract data using only common columns
        AnsiConsole.Progress()
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
                    var openingFilesTask = ctx.AddTask($"[grey]Opening files[/]", maxValue: validFilePaths.Count);
                    var readingDataTask = ctx.AddTask($"[grey]Reading data[/]", maxValue: validFilePaths.Count);
                    var processingDataTask = ctx.AddTask($"[grey]Processing {(anonymizeData ? "and anonymizing " : "")}data[/]", maxValue: validFilePaths.Count);

                    foreach (var filePath in validFilePaths)
                    {
                        var fileName = Path.GetFileName(filePath);
                        using var workbook = new XLWorkbook(filePath);
                        openingFilesTask.Increment(1);

                        if (!SheetExists(workbook, sheetName))
                        {
                            readingDataTask.Increment(1);
                            processingDataTask.Increment(1);
                            sheetTask.Increment(1);
                            continue;
                        }

                        var worksheet = workbook.Worksheet(sheetName);
                        var columnMapping = GetColumnMapping(worksheet, commonColumns[sheetName]);
                        readingDataTask.Increment(1);

                        // Find the last row with data
                        int lastRow = worksheet.LastRowUsed().RowNumber();

                        // Find columns to anonymize in this sheet
                        var anonymizeColumnIndices = new Dictionary<string, int>();
                        if (anonymizeData)
                        {
                            // VM Name
                            int vmColIndex = commonColumns[sheetName].IndexOf("VM");
                            if (vmColIndex >= 0) anonymizeColumnIndices["VM"] = vmColIndex;
                            // DNS Name
                            int dnsColIndex = commonColumns[sheetName].IndexOf("DNS Name");
                            if (dnsColIndex >= 0) anonymizeColumnIndices["DNS Name"] = dnsColIndex;
                            // Cluster Name
                            int clusterColIndex = commonColumns[sheetName].IndexOf("Cluster");
                            if (clusterColIndex >= 0) anonymizeColumnIndices["Cluster"] = clusterColIndex;
                            // Host Name
                            int hostColIndex = commonColumns[sheetName].IndexOf("Host");
                            if (hostColIndex >= 0) anonymizeColumnIndices["Host"] = hostColIndex;
                            // Datacenter Name
                            int datacenterColIndex = commonColumns[sheetName].IndexOf("Datacenter");
                            if (datacenterColIndex >= 0) anonymizeColumnIndices["Datacenter"] = datacenterColIndex;
                        }

                        // Find source file column index if the option is enabled
                        int sourceFileColumnIndex = -1;
                        if (includeSourceFileName)
                        {
                            sourceFileColumnIndex = commonColumns[sheetName].IndexOf("Source File");
                        }

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
                                if (anonymizeData)
                                {
                                    cellValue = AnonymizeValue(cellValue, mapping.CommonColumnIndex, anonymizeColumnIndices,
                                        vmNameMap, dnsNameMap, clusterNameMap, hostNameMap, datacenterNameMap);
                                }

                                // Store the value
                                rowData[mapping.CommonColumnIndex] = cellValue;
                            }

                            // Add source file name if the option is enabled
                            if (includeSourceFileName && sourceFileColumnIndex >= 0)
                            {
                                rowData[sourceFileColumnIndex] = Path.GetFileName(filePath);
                            }

                            mergedData[sheetName].Add(rowData);
                        }

                        processingDataTask.Increment(1);
                        sheetTask.Increment(1);
                    }

                    // Complete all subtasks for this sheet
                    openingFilesTask.StopTask();
                    readingDataTask.StopTask();
                    processingDataTask.StopTask();
                }
            });

        AnsiConsole.MarkupLine("[bold]Creating output file...[/]");
        await AnsiConsole.Progress()
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
                var preparingWorkbookTask = ctx.AddTask("[grey]Preparing workbook[/]", maxValue: 1);
                var writingDataTask = ctx.AddTask("[grey]Writing data[/]", maxValue: availableSheets.Count);
                var formattingTask = ctx.AddTask("[grey]Formatting and saving[/]", maxValue: availableSheets.Count + 1);

                using (var workbook = new XLWorkbook())
                {
                    preparingWorkbookTask.Increment(1);

                    foreach (var sheetName in availableSheets)
                    {
                        var worksheet = workbook.Worksheets.Add(sheetName);
                        var rowTask = ctx.AddTask($"[cyan]Writing '{sheetName}' sheet[/]", maxValue: mergedData[sheetName].Count);

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
                            rowTask.Increment(1);
                        }

                        writingDataTask.Increment(1);

                        // Auto-fit columns
                        worksheet.Columns().AdjustToContents();
                        formattingTask.Increment(1);
                        rowTask.StopTask();
                        outputTask.Increment(1);
                    }

                    // Save the output file
                    AnsiConsole.MarkupLine("[cyan]Saving file to disk...[/]");
                    await Task.Run(() => workbook.SaveAs(outputPath));
                    formattingTask.Increment(1);
                    outputTask.Increment(1);
                }

                // Complete all tasks
                preparingWorkbookTask.StopTask();
                writingDataTask.StopTask();
                formattingTask.StopTask();
            });

        AnsiConsole.Write(new Rule("[yellow]File Summary[/]").RuleStyle("grey"));
        var summaryTable = new Table();
        summaryTable.AddColumn(new TableColumn("Category").LeftAligned());
        summaryTable.AddColumn(new TableColumn("Details").RightAligned());
        summaryTable.AddRow("[cyan]Files Processed[/]", $"[green]{validFilePaths.Count}[/] of [green]{filePaths.Length}[/]");
        summaryTable.AddRow("[cyan]Sheets Included[/]", $"[green]{availableSheets.Count}[/]");
        int totalRows = 0;
        foreach (var sheetName in availableSheets)
        {
            totalRows += mergedData[sheetName].Count - 1; // Subtract 1 for header row
            summaryTable.AddRow($"[yellow]{sheetName}[/] rows", $"[green]{mergedData[sheetName].Count - 1}[/]");
        }
        summaryTable.AddRow("[bold]Total Data Rows[/]", $"[green]{totalRows}[/]");
        summaryTable.Border(TableBorder.Rounded);
        AnsiConsole.Write(summaryTable);

        // Display anonymization summary if enabled
        if (anonymizeData)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[yellow]Anonymization Summary[/]").RuleStyle("grey"));
            var table = new Table();
            table.AddColumn(new TableColumn("Category").Centered());
            table.AddColumn(new TableColumn("Count").Centered());
            table.AddRow("[cyan]VMs[/]", $"[green]{vmNameMap.Count}[/]");
            table.AddRow("[cyan]DNS Names[/]", $"[green]{dnsNameMap.Count}[/]");
            table.AddRow("[cyan]Clusters[/]", $"[green]{clusterNameMap.Count}[/]");
            table.AddRow("[cyan]Hosts[/]", $"[green]{hostNameMap.Count}[/]");
            table.AddRow("[cyan]Datacenters[/]", $"[green]{datacenterNameMap.Count}[/]");
            table.Border(TableBorder.Rounded);
            AnsiConsole.Write(table);

            int totalAnonymized = vmNameMap.Count + dnsNameMap.Count + clusterNameMap.Count + hostNameMap.Count + datacenterNameMap.Count;
            AnsiConsole.MarkupLineInterpolated($"[bold]Total:[/] [green]{totalAnonymized}[/] items anonymized");
        }
    }

    /// <summary>
    /// Checks if a sheet exists in a workbook.
    /// </summary>
    /// <param name="workbook">The workbook to check.</param>
    /// <param name="sheetName">The name of the sheet to check for.</param>
    /// <returns>True if the sheet exists; otherwise, false.</returns>
    static bool SheetExists(XLWorkbook workbook, string sheetName) =>
        workbook.Worksheets.Any(sheet => sheet.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Displays help information about the application.
    /// </summary>
    static void ShowHelp()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        string versionString = version is not null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";

        // Get product name from assembly attributes
        var productAttribute = Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute)) as AssemblyProductAttribute;
        string productName = productAttribute?.Product ?? "RVTools Excel Merger";

        // Enhanced header display with Spectre.Console
        AnsiConsole.MarkupLineInterpolated($"[bold green]{productName}[/] - Merges multiple RVTools Excel files into a single file");
        AnsiConsole.MarkupLineInterpolated($"[yellow]Version {versionString}[/]");
        AnsiConsole.Write(new Rule().RuleStyle("grey"));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]USAGE:[/]");
        AnsiConsole.MarkupLineInterpolated($"  [cyan]{Assembly.GetExecutingAssembly().GetName().Name}[/] [grey][[options]] inputPath [[outputFile]][/]");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]ARGUMENTS:[/]");
        AnsiConsole.MarkupLine("  [green]inputPath[/]     Path to an Excel file or a folder containing RVTools Excel files.");
        AnsiConsole.MarkupLine("                [bold]Required[/]. Must be a valid file path or directory path.");
        AnsiConsole.MarkupLine("  [green]outputFile[/]    Path where the merged file will be saved.");
        AnsiConsole.MarkupLine("                Defaults to \"RVTools_Merged.xlsx\" in the current directory.");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]OPTIONS:[/]");
        AnsiConsole.MarkupLine("  [yellow]-h, --help, /?[/]            Show this help message and exit.");
        AnsiConsole.MarkupLine("  [yellow]-m, --ignore-missing-sheets[/]");
        AnsiConsole.MarkupLine("                            Ignore missing optional sheets (vHost, vPartition & vMemory).");
        AnsiConsole.MarkupLine("                            Will still validate vInfo sheet exists.");
        AnsiConsole.MarkupLine("  [yellow]-i, --skip-invalid-files[/]  Skip files that don't meet validation requirements (no vInfo sheet or");
        AnsiConsole.MarkupLine("                            mandatory columns are missing) instead of failing with an error.");
        AnsiConsole.MarkupLine("                            Can be used with -m to skip files with missing vInfo sheet while");
        AnsiConsole.MarkupLine("                            processing others that may have missing optional sheets.");
        AnsiConsole.MarkupLine("  [yellow]-a, --anonymize[/]           Anonymize VM, DNS Name, Cluster, Host, and Datacenter");
        AnsiConsole.MarkupLine("                            columns with generic names (vm1, host1, etc.).");
        AnsiConsole.MarkupLine("  [yellow]-M, --only-mandatory-columns[/]");
        AnsiConsole.MarkupLine("                            Include only the mandatory columns for each sheet in the");
        AnsiConsole.MarkupLine("                            output file instead of all common columns.");
        AnsiConsole.MarkupLine("  [yellow]-s, --include-source[/]      Include a 'Source File' column in each sheet showing");
        AnsiConsole.MarkupLine("                            the name of the source file for each record.");
        AnsiConsole.MarkupLine("  [yellow]-d, --debug[/]               Show detailed error information including stack traces");
        AnsiConsole.MarkupLine("                            when errors occur.");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]DESCRIPTION:[/]");
        AnsiConsole.MarkupLine("  This tool merges all RVTools Excel files (XLSX format) from the specified");
        AnsiConsole.MarkupLine("  folder into one consolidated file. It extracts data from the following sheets:");
        AnsiConsole.MarkupLine("    - [green]vInfo[/] ([bold]required[/]): Virtual machine information (CPUs, memory, OS)");
        AnsiConsole.MarkupLine("    - [cyan]vHost[/] (optional): Host information (CPU, memory, cluster details)");
        AnsiConsole.MarkupLine("    - [cyan]vPartition[/] (optional): Virtual machine disk partition information");
        AnsiConsole.MarkupLine("    - [cyan]vMemory[/] (optional): Virtual machine memory configuration information");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Features:[/]");
        AnsiConsole.Markup("  - ");
        AnsiConsole.MarkupLine("Validates mandatory columns in each sheet");
        AnsiConsole.Markup("  - ");
        AnsiConsole.MarkupLine("Only includes columns that exist in all files for each respective sheet");
        AnsiConsole.Markup("  - ");
        AnsiConsole.MarkupLine("Anonymizes sensitive data when requested (VMs, DNS, Clusters, Hosts, Datacenters)");
        AnsiConsole.Markup("  - ");
        AnsiConsole.MarkupLine("Allows filtering to include only mandatory columns");
        AnsiConsole.Markup("  - ");
        AnsiConsole.MarkupLine("Works on multiple platforms (Windows, Linux, macOS)");
        AnsiConsole.Markup("  - ");
        AnsiConsole.MarkupLine("Minimal memory footprint with efficient processing");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Mandatory columns by sheet:[/]");
        var table = new Table();
        table.AddColumn(new TableColumn("Sheet").LeftAligned());
        table.AddColumn(new TableColumn("Status").Centered());
        table.AddColumn(new TableColumn("Required Columns").LeftAligned());
        table.AddRow(
            "[green]vInfo[/]",
            "[bold green]Required[/]",
            "Template, SRM Placeholder, Powerstate, [bold]VM[/], [bold]CPUs[/], [bold]Memory[/], [bold]In Use MiB[/], [bold]OS according to the VMware Tools[/]"
        );
        table.AddRow(
            "[cyan]vHost[/]",
            "[yellow]Optional[/]",
            "[bold]Host[/], [bold]Datacenter[/], [bold]Cluster[/], CPU Model, Speed, # CPU, Cores per CPU, # Cores, CPU usage %, # Memory, Memory usage %"
        );
        table.AddRow(
            "[cyan]vPartition[/]",
            "[yellow]Optional[/]",
            "[bold]VM[/], [bold]Disk[/], [bold]Capacity MiB[/], [bold]Consumed MiB[/]"
        );
        table.AddRow(
            "[cyan]vMemory[/]",
            "[yellow]Optional[/]",
            "[bold]VM[/], [bold]Size MiB[/], [bold]Reservation[/]"
        );
        table.Border(TableBorder.Rounded);
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Note: Bold column names indicate the most critical columns for analysis.[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Validation behavior:[/]");
        AnsiConsole.MarkupLine("  - By default, all sheets must exist in all files");
        AnsiConsole.MarkupLine("  - When using [yellow]--ignore-missing-sheets[/], optional sheets (vHost, vPartition, vMemory)");
        AnsiConsole.MarkupLine("    can be missing with warnings shown. The vInfo sheet is always required.");
        AnsiConsole.MarkupLine("  - When using [yellow]--skip-invalid-files[/], files without required sheets or missing mandatory");
        AnsiConsole.MarkupLine("    columns will be skipped and reported, but processing will continue with valid files.");
        AnsiConsole.MarkupLine("  - Both [yellow]--ignore-missing-sheets[/] and [yellow]--skip-invalid-files[/] can be used");
        AnsiConsole.MarkupLine("    together to skip files with missing vInfo sheet while processing others with potentially");
        AnsiConsole.MarkupLine("    missing optional sheets.");
        AnsiConsole.MarkupLine("  - When using [yellow]--anonymize[/], sensitive names are replaced with generic identifiers");
        AnsiConsole.MarkupLine("    (vm1, dns1, host1, etc.) to protect sensitive information.");
        AnsiConsole.MarkupLine("  - When using [yellow]--only-mandatory-columns[/], only the mandatory columns for each sheet");
        AnsiConsole.MarkupLine("    are included in the output, regardless of what other columns might be common");
        AnsiConsole.MarkupLine("    across all files.");
        AnsiConsole.MarkupLine("  - When using [yellow]--include-source[/], a 'Source File' column is added to each");
        AnsiConsole.MarkupLine("    sheet to show which source file each record came from, helping with data traceability.");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]EXAMPLES:[/]");
        string appName = Assembly.GetExecutingAssembly().GetName().Name ?? "RVToolsMerge";
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] C:\\RVTools\\Data");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] C:\\RVTools\\Data\\SingleFile.xlsx");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-m[/] C:\\RVTools\\Data C:\\Reports\\Merged_RVTools.xlsx");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]--ignore-missing-sheets[/] C:\\RVTools\\Data");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-i[/] C:\\RVTools\\Data");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-a[/] C:\\RVTools\\Data\\RVTools.xlsx C:\\Reports\\Anonymized_RVTools.xlsx");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-M[/] C:\\RVTools\\Data C:\\Reports\\Mandatory_Columns.xlsx");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-s[/] C:\\RVTools\\Data C:\\Reports\\With_Source_Files.xlsx");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-a -M -s[/] C:\\RVTools\\Data C:\\Reports\\Complete_Analysis.xlsx");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]DOWNLOADS:[/]");
        AnsiConsole.MarkupLine("  Latest releases for all supported platforms are available at:");
        AnsiConsole.MarkupLine("  [link]https://github.com/sbroenne/RVToolsMerge/releases[/]");
        string applicationName = Assembly.GetExecutingAssembly().GetName().Name ?? "RVToolsMerge";
        AnsiConsole.MarkupLine($"  - [cyan]{applicationName}-windows-Release.zip[/]       (Windows x64)");
        AnsiConsole.MarkupLine($"  - [cyan]{applicationName}-windows-arm64-Release.zip[/] (Windows ARM64)");
        AnsiConsole.MarkupLine($"  - [cyan]{applicationName}-linux-Release.zip[/]         (Linux x64)");
        AnsiConsole.MarkupLine($"  - [cyan]{applicationName}-macos-arm64-Release.zip[/]   (macOS ARM64)");
    }

    /// <summary>
    /// Gets the column names from a worksheet.
    /// </summary>
    /// <param name="worksheet">The worksheet to extract column names from.</param>
    /// <returns>A list of column names.</returns>
    static List<string> GetColumnNames(IXLWorksheet worksheet)
    {
        var columnNames = new List<string>();
        // Get the first row
        var headerRow = worksheet.Row(1);
        // Find the last column with data
        int lastColumn = worksheet.LastColumnUsed().ColumnNumber();

        for (int col = 1; col <= lastColumn; col++)
        {
            var cell = headerRow.Cell(col);
            var cellValue = cell.Value;
            if (!string.IsNullOrWhiteSpace(cellValue.ToString()))
            {
                columnNames.Add(cellValue.ToString()!);
            }
        }

        return columnNames;
    }

    /// <summary>
    /// Creates a mapping between column indices in the source worksheet and corresponding indices in the common columns collection.
    /// </summary>
    /// <param name="worksheet">The Excel worksheet to analyze for column mappings.</param>
    /// <param name="commonColumns">The list of common column names that should be included in the output.</param>
    /// <returns>A list of column mappings where each mapping connects a source file column index to its corresponding common column index.</returns>
    /// <remarks>
    /// This method is essential for the data extraction process as it allows us to locate data from source worksheets
    /// and place it in the correct position in the merged output. Only columns that exist in the commonColumns list
    /// will be included in the mappings.
    /// </remarks>
    static List<ColumnMapping> GetColumnMapping(IXLWorksheet worksheet, List<string> commonColumns)
    {
        var mapping = new List<ColumnMapping>();
        // Get the first row
        var headerRow = worksheet.Row(1);
        // Find the last column with data
        int lastColumn = worksheet.LastColumnUsed().ColumnNumber();

        // Create a mapping between the file's column indices and the common column indices
        for (int fileColIndex = 1; fileColIndex <= lastColumn; fileColIndex++)
        {
            var cell = headerRow.Cell(fileColIndex);
            var cellValue = cell.Value;
            if (!string.IsNullOrWhiteSpace(cellValue.ToString()))
            {
                int commonIndex = commonColumns.IndexOf(cellValue.ToString()!);
                if (commonIndex >= 0)
                {
                    mapping.Add(new ColumnMapping(fileColIndex, commonIndex));
                }
            }
        }

        return mapping;
    }

    /// <summary>
    /// Anonymizes a cell value based on its column type during the data merging process.
    /// </summary>
    /// <param name="value">The original cell value to potentially anonymize.</param>
    /// <param name="currentColumnIndex">The index of the current column in the common columns collection.</param>
    /// <param name="anonymizeColumnIndices">Dictionary mapping column names to their indices for columns that require anonymization.</param>
    /// <param name="vmNameMap">Dictionary that maps original VM names to their anonymized versions.</param>
    /// <param name="dnsNameMap">Dictionary that maps original DNS names to their anonymized versions.</param>
    /// <param name="clusterNameMap">Dictionary that maps original cluster names to their anonymized versions.</param>
    /// <param name="hostNameMap">Dictionary that maps original host names to their anonymized versions.</param>
    /// <param name="datacenterNameMap">Dictionary that maps original datacenter names to their anonymized versions.</param>
    /// <returns>The anonymized cell value if the column requires anonymization; otherwise, the original value.</returns>
    /// <remarks>
    /// This method maintains consistent anonymization across all sheets by using the mapping dictionaries.
    /// The same original name will always be anonymized to the same value throughout the entire workbook.
    /// The method handles the following column types for anonymization:
    /// - VM names (e.g., "vm1", "vm2")
    /// - DNS names (e.g., "dns1", "dns2")
    /// - Cluster names (e.g., "cluster1", "cluster2")
    /// - Host names (e.g., "host1", "host2")
    /// - Datacenter names (e.g., "datacenter1", "datacenter2")
    /// </remarks>
    private static XLCellValue AnonymizeValue(
        XLCellValue value,
        int currentColumnIndex,
        Dictionary<string, int> anonymizeColumnIndices,
        Dictionary<string, string> vmNameMap,
        Dictionary<string, string> dnsNameMap,
        Dictionary<string, string> clusterNameMap,
        Dictionary<string, string> hostNameMap,
        Dictionary<string, string> datacenterNameMap)
    {
        // VM Name
        if (anonymizeColumnIndices.TryGetValue("VM", out int vmColIndex) &&
            currentColumnIndex == vmColIndex)
        {
            return GetOrCreateAnonymizedName(value, vmNameMap, "vm");
        }
        // DNS Name
        else if (anonymizeColumnIndices.TryGetValue("DNS Name", out int dnsColIndex) &&
                currentColumnIndex == dnsColIndex)
        {
            return GetOrCreateAnonymizedName(value, dnsNameMap, "dns");
        }
        // Cluster Name
        else if (anonymizeColumnIndices.TryGetValue("Cluster", out int clusterColIndex) &&
                currentColumnIndex == clusterColIndex)
        {
            return GetOrCreateAnonymizedName(value, clusterNameMap, "cluster");
        }
        // Host Name
        else if (anonymizeColumnIndices.TryGetValue("Host", out int hostColIndex) &&
                currentColumnIndex == hostColIndex)
        {
            return GetOrCreateAnonymizedName(value, hostNameMap, "host");
        }
        // Datacenter Name
        else if (anonymizeColumnIndices.TryGetValue("Datacenter", out int datacenterColIndex) &&
                currentColumnIndex == datacenterColIndex)
        {
            return GetOrCreateAnonymizedName(value, datacenterNameMap, "datacenter");
        }
        // Return original value if no anonymization is needed
        return value;
    }

    /// <summary>
    /// Gets or creates an anonymized name for a given original value.
    /// </summary>
    /// <param name="originalValue">The original value to anonymize.</param>
    /// <param name="nameMap">The mapping dictionary to use.</param>
    /// <param name="prefix">The prefix to use for anonymized names.</param>
    /// <returns>The anonymized name.</returns>
    private static XLCellValue GetOrCreateAnonymizedName(
        XLCellValue originalValue,
        Dictionary<string, string> nameMap,
        string prefix)
    {
        var lookupValue = originalValue.ToString();
        if (string.IsNullOrWhiteSpace(lookupValue))
        {
            return originalValue; // Return original value if empty
        }

        if (!nameMap.TryGetValue(lookupValue, out string? value))
        {
            value = $"{prefix}{nameMap.Count + 1}";
            nameMap[lookupValue] = value;
        }
        return value;
    }

    /// <summary>
    /// Creates a platform-agnostic file path by combining path segments.
    /// </summary>
    /// <param name="paths">The path segments to combine.</param>
    /// <returns>A platform-appropriate file path.</returns>
    private static string CreatePlatformPath(params string[] paths)
    {
        return Path.Combine(paths);
    }
}

/// <summary>
/// Custom exception for file validation errors.
/// </summary>
public class FileValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public FileValidationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public FileValidationException(string message, Exception innerException) : base(message, innerException) { }
}
