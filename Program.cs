//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Stefan Broenner"> ">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using System.Collections.Frozen;
using System.Reflection;
using ClosedXML.Excel;
using Spectre.Console;

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
    /// Maps original RVTools column headers to their standardized names for each sheet.
    /// </summary>
    private static readonly FrozenDictionary<string, FrozenDictionary<string, string>> SheetColumnHeaderMappings =
        new Dictionary<string, FrozenDictionary<string, string>>
        {
            // vInfo sheet mappings
            ["vInfo"] = new Dictionary<string, string>
            {
                { "vInfoVMName", "VM" },
                { "vInfoPowerstate", "Powerstate" },
                { "vInfoTemplate", "Template" },
                { "vInfoCPUs", "CPUs" },
                { "vInfoMemory", "Memory" },
                { "vInfoProvisioned", "Provisioned MiB" },
                { "vInfoInUse", "In Use MiB" },
                { "vInfoDataCenter", "Datacenter" },
                { "vInfoCluster", "Cluster" },
                { "vInfoHost", "Host" },
                { "vInfoSRMPlaceHolder", "SRM Placeholder" },
                { "vInfoOSTools", "OS according to the VMware Tools" },
                { "vInfoOS", "OS according to the configuration file" }
            }.ToFrozenDictionary(),

            // vHost sheet mappings
            ["vHost"] = new Dictionary<string, string>
            {
                { "vHostName", "Host" },
                { "vHostDatacenter", "Datacenter" },
                { "vHostCluster", "Cluster" },
                { "vHostvSANFaultDomainName", "vSAN Fault Domain Name" },
                { "vHostCpuModel", "CPU Model" },
                { "vHostCpuMhz", "Speed" },
                { "vHostNumCPU", "# CPU" },
                { "vHostNumCpu", "# CPU" },
                { "vHostCoresPerCPU", "Cores per CPU" },
                { "vHostNumCpuCores", "# Cores" },
                { "vHostOverallCpuUsage", "CPU usage %" },
                { "vHostMemorySize", "# Memory" },
                { "vHostOverallMemoryUsage", "Memory usage %" },
                { "vHostvCPUs", "# vCPUs" },
                { "vHostVCPUsPerCore", "vCPUs per Core" }
            }.ToFrozenDictionary(),

            // vPartition sheet mappings
            ["vPartition"] = new Dictionary<string, string>
            {
                { "vPartitionDisk", "Disk" },
                { "vPartitionVMName", "VM" },
                { "vPartitionConsumedMiB", "Consumed MiB" },
                { "vPartitionCapacityMiB", "Capacity MiB" }
            }.ToFrozenDictionary(),

            // vMemory sheet mappings
            ["vMemory"] = new Dictionary<string, string>
            {
                { "vMemoryVMName", "VM" },
                { "vMemorySizeMiB", "Size MiB" },
                { "vMemoryReservation", "Reservation" }
            }.ToFrozenDictionary()
        }.ToFrozenDictionary();

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
        { "vInfo", ["Template", "SRM Placeholder", "Powerstate", "VM", "CPUs", "Memory", "In Use MiB", "OS according to the configuration file"] },
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
        AnsiConsole.Write(
            new FigletText(productName)
                .Color(Color.Green)
                .Centered()
        );
        AnsiConsole.MarkupLine($"[yellow]v{versionString}[/]");
        AnsiConsole.Write(new Rule().RuleStyle("grey"));

        AnsiConsole.MarkupLineInterpolated($"[bold green]{productName}[/] - Merges multiple RVTools Excel files into a single file");

        // Command line options
        bool ignoreMissingOptionalSheets = false;
        bool skipInvalidFiles = false;
        bool anonymizeData = false;
        bool onlyMandatoryColumns = false;
        bool debugMode = false;
        bool includeSourceFileName = false;
        bool skipRowsWithEmptyMandatoryValues = false; // Default is now false (include empty values)

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
                case "-e" or "--skip-empty-values":
                    skipRowsWithEmptyMandatoryValues = true; // Now flag enables skipping
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

        // Display selected options
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Selected Options:[/]");
        var optionsTable = new Table().BorderColor(Color.Grey);
        optionsTable.AddColumn(new TableColumn("Option").Centered());
        optionsTable.AddColumn(new TableColumn("Status").Centered());

        optionsTable.AddRow("[yellow]--ignore-missing-sheets[/]", ignoreMissingOptionalSheets ? "[green]Enabled[/]" : "[grey]Disabled[/]");
        optionsTable.AddRow("[yellow]--skip-invalid-files[/]", skipInvalidFiles ? "[green]Enabled[/]" : "[grey]Disabled[/]");
        optionsTable.AddRow("[yellow]--anonymize[/]", anonymizeData ? "[green]Enabled[/]" : "[grey]Disabled[/]");
        optionsTable.AddRow("[yellow]--only-mandatory-columns[/]", onlyMandatoryColumns ? "[green]Enabled[/]" : "[grey]Disabled[/]");
        optionsTable.AddRow("[yellow]--include-source[/]", includeSourceFileName ? "[green]Enabled[/]" : "[grey]Disabled[/]");
        optionsTable.AddRow("[yellow]--skip-empty-values[/]", skipRowsWithEmptyMandatoryValues ? "[green]Enabled[/]" : "[grey]Disabled[/]");
        optionsTable.AddRow("[yellow]--debug[/]", debugMode ? "[green]Enabled[/]" : "[grey]Disabled[/]");

        AnsiConsole.Write(optionsTable);
        AnsiConsole.WriteLine();

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
                debugMode,
                skipRowsWithEmptyMandatoryValues
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
    /// Displays validation issues in a formatted table, grouped by file name.
    /// </summary>
    /// <param name="issues">The list of validation issues to display.</param>
    private static void DisplayValidationIssues(List<ValidationIssue> issues)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[yellow]Validation Issues[/]").RuleStyle("grey"));

        // Group issues by filename
        var groupedIssues = issues
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
        }

        table.Border(TableBorder.Rounded);
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        int totalFiles = groupedIssues.Count;
        int totalIssues = issues.Count;
        AnsiConsole.MarkupLineInterpolated($"[yellow]Total of {totalIssues} validation issues across {totalFiles} files.[/]");
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
    /// <param name="skipRowsWithEmptyMandatoryValues">Whether to include rows with empty mandatory values.</param>
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
        bool debugMode = false,
        bool skipRowsWithEmptyMandatoryValues = false)
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

                // Remove subtask progress bars
                for (int i = validFilePaths.Count - 1; i >= 0; i--)
                {
                    string filePath = validFilePaths[i];
                    string fileName = Path.GetFileName(filePath);
                    bool fileIsValid = true;

                    try
                    {
                        using (var workbook = new XLWorkbook(filePath))
                        {
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
                                            $"Missing optional sheet '{sheetName}'."
                                        ));
                                    }
                                }
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
                    }

                    if (!fileIsValid && skipInvalidFiles)
                    {
                        validFilePaths.RemoveAt(i);
                    }

                    validationTask.Increment(1);
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
                    // Create main task for analysis - removing subtasks
                    var fileAnalysisTask = ctx.AddTask($"[cyan]Analyzing '{sheetName}' sheet[/]", maxValue: validFilePaths.Count);

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
                        fileAnalysisTask.Increment(1);
                    }
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

                    foreach (var filePath in validFilePaths)
                    {
                        var fileName = Path.GetFileName(filePath);
                        using var workbook = new XLWorkbook(filePath);

                        if (!SheetExists(workbook, sheetName))
                        {
                            sheetTask.Increment(1);
                            continue;
                        }

                        var worksheet = workbook.Worksheet(sheetName);
                        var columnMapping = GetColumnMapping(worksheet, commonColumns[sheetName]);

                        // Find the last row with data, handle null in case the worksheet is empty
                        var lastRowUsed = worksheet.LastRowUsed();
                        int lastRow = lastRowUsed is not null ? lastRowUsed.RowNumber() : 1;

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

                        // Prepare for mandatory column validation (except "OS according to the configuration file")
                        var mandatoryCols = MandatoryColumns.TryGetValue(sheetName, out var mcols)
                            ? mcols.Where(c => c != "OS according to the configuration file").ToList()
                            : new List<string>();
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
                                if (anonymizeData)
                                {
                                    cellValue = AnonymizeValue(cellValue, mapping.CommonColumnIndex, anonymizeColumnIndices,
                                        vmNameMap, dnsNameMap, clusterNameMap, hostNameMap, datacenterNameMap);
                                }

                                // Store the value
                                rowData[mapping.CommonColumnIndex] = cellValue;
                            }

                            // Validate mandatory columns (except "OS according to the configuration file")
                            bool hasEmptyMandatory = mandatoryColIndices.Any(idx =>
                                idx >= 0 &&
                                (EqualityComparer<XLCellValue>.Default.Equals(rowData[idx], default) ||
                                 string.IsNullOrWhiteSpace(rowData[idx].ToString()))
                            );
                            if (hasEmptyMandatory)
                            {
                                validationIssues?.Add(new ValidationIssue(
                                    fileName,
                                    false,
                                    $"Row {row} in sheet '{sheetName}' has empty value(s) in mandatory column(s) (excluding 'OS according to the configuration file')."
                                ));
                                // INVERTED: Now we only skip if flag is enabled
                                if (skipRowsWithEmptyMandatoryValues) {
                                    continue; // skip this row
                                }
                            }

                            // Add source file name if the option is enabled
                            if (includeSourceFileName && sourceFileColumnIndex >= 0)
                            {
                                rowData[sourceFileColumnIndex] = Path.GetFileName(filePath);
                            }

                            mergedData[sheetName].Add(rowData);
                        }

                        sheetTask.Increment(1);
                    }
                }
            });

        // After data extraction and before creating the output file
        foreach (var sheetName in availableSheets)
        {
            var expectedRowCount = mergedData[sheetName].Count;

            // Calculate the actual row count, but subtract rows that would be skipped due to validation
            int skippedRows = 0;
            var actualRowCount = validFilePaths.Sum(filePath =>
            {
                using var workbook = new XLWorkbook(filePath);
                if (!SheetExists(workbook, sheetName))
                    return 0;

                var worksheet = workbook.Worksheet(sheetName);
                var lastRowUsed = worksheet.LastRowUsed();

                if (lastRowUsed is null)
                    return 0;

                int rowCount = lastRowUsed.RowNumber() - 1; // subtract header row

                // Count rows that would be skipped due to validation
                if (MandatoryColumns.TryGetValue(sheetName, out var mcols))
                {
                    var mandatoryCols = mcols.Where(c => c != "OS according to the configuration file").ToList();
                    var columnMapping = GetColumnMapping(worksheet, commonColumns[sheetName]);

                    // Map mandatory column names to column indices in the worksheet
                    var mandatoryColIndices = new List<int>();
                    foreach (var colName in mandatoryCols)
                    {
                        int commonIndex = commonColumns[sheetName].IndexOf(colName);
                        if (commonIndex >= 0)
                        {
                            // Find the corresponding file column index
                            var fileColMapping = columnMapping.FirstOrDefault(m => m.CommonColumnIndex == commonIndex);
                            if (fileColMapping != null)
                            {
                                mandatoryColIndices.Add(fileColMapping.FileColumnIndex);
                            }
                        }
                    }

                    // Count rows with empty mandatory values
                    for (int row = 2; row <= lastRowUsed.RowNumber(); row++)
                    {
                        bool hasEmptyMandatory = mandatoryColIndices.Any(colIndex =>
                        {
                            var cell = worksheet.Cell(row, colIndex);
                            return EqualityComparer<XLCellValue>.Default.Equals(cell.Value, default) ||
                                   string.IsNullOrWhiteSpace(cell.Value.ToString());
                        });

                        if (hasEmptyMandatory)
                        {
                            skippedRows++;
                        }
                    }
                }

                return rowCount;
            }) + 1; // add header row back

            // Subtract the number of skipped rows from the actual count
            actualRowCount -= skippedRows;

            // Add a verbose warning about skipped rows instead of throwing an exception
            if (expectedRowCount != actualRowCount)
            {
                AnsiConsole.MarkupLineInterpolated(
                    $"[yellow]Warning:[/] Row count mismatch detected for sheet '{sheetName}'. Expected {actualRowCount} rows, but found {expectedRowCount} rows in merged data. {(skipRowsWithEmptyMandatoryValues ? $"This may be due to {skippedRows} rows being skipped because they had empty mandatory values." : "This may be due to other data inconsistencies in the source files.")}"
                );
            }
        }

        // Proceed with creating the output file
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
                    AnsiConsole.MarkupLine("[cyan]Saving file to disk...[/]");
                    await Task.Run(() => workbook.SaveAs(outputPath));
                    outputTask.Increment(1);
                }
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
            int rowCount = mergedData[sheetName].Count - 1; // Subtract 1 for header row
            int colCount = commonColumns.TryGetValue(sheetName, out var cols) ? cols.Count : 0;
            totalRows += rowCount;
            summaryTable.AddRow(
                $"[yellow]{sheetName}[/] rows",
                $"[green]{rowCount}[/] ([grey]{colCount} columns[/])"
            );
        }

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
        // Get version and product info
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        string versionString = version is not null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        var productAttribute = Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute)) as AssemblyProductAttribute;
        string productName = productAttribute?.Product ?? "RVTools Excel Merger";

        AnsiConsole.MarkupLineInterpolated($"[bold green]{productName}[/] - Merges multiple RVTools Excel files into a single file");
        AnsiConsole.MarkupLineInterpolated($"[yellow]Version {versionString}[/]");
        AnsiConsole.Write(new Rule().RuleStyle("grey"));
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]USAGE:[/]");
        AnsiConsole.MarkupLineInterpolated($"  [cyan]{Assembly.GetExecutingAssembly().GetName().Name}[/] [grey][[options]] inputPath [[outputFile]][/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]ARGUMENTS:[/]");
        AnsiConsole.MarkupLine("  [green]inputPath[/]     Path to an Excel file or a folder containing RVTools Excel files.");
        AnsiConsole.MarkupLine("                [bold]Required[/]. Must be a valid file path or directory path.");
        AnsiConsole.MarkupLine("  [green]outputFile[/]    Path where the merged file will be saved.");
        AnsiConsole.MarkupLine("                Defaults to \"RVTools_Merged.xlsx\" in the current directory.");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]OPTIONS:[/]");
        AnsiConsole.MarkupLine("  [yellow]-h, --help, /?[/]            Show this help message and exit.");
        AnsiConsole.MarkupLine("  [yellow]-m, --ignore-missing-sheets[/]");
        AnsiConsole.MarkupLine("                            Ignore missing optional sheets (vHost, vPartition & vMemory).");
        AnsiConsole.MarkupLine("  [yellow]-i, --skip-invalid-files[/]  Skip files that don't meet validation requirements.");
        AnsiConsole.MarkupLine("  [yellow]-a, --anonymize[/]           Anonymize VM, DNS Name, Cluster, Host, and Datacenter names.");
        AnsiConsole.MarkupLine("  [yellow]-M, --only-mandatory-columns[/]");
        AnsiConsole.MarkupLine("                            Include only mandatory columns in output.");
        AnsiConsole.MarkupLine("  [yellow]-s, --include-source[/]      Include source file name in output.");
        AnsiConsole.MarkupLine("  [yellow]-e, --skip-empty-values[/]   Skip rows with empty values in mandatory columns.");
        AnsiConsole.MarkupLine("                            By default, all rows are included regardless of empty values.");
        AnsiConsole.MarkupLine("  [yellow]-d, --debug[/]               Show detailed error information.");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]EXAMPLES:[/]");
        string appName = Assembly.GetExecutingAssembly().GetName().Name ?? "RVToolsMerge";
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] C:\\RVTools\\Data");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] C:\\RVTools\\Data\\SingleFile.xlsx");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-m[/] C:\\RVTools\\Data C:\\Reports\\Merged_RVTools.xlsx");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-a[/] C:\\RVTools\\Data\\RVTools.xlsx C:\\Reports\\Anonymized_RVTools.xlsx");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-M[/] C:\\RVTools\\Data C:\\Reports\\Mandatory_Columns.xlsx");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-a -M -s[/] C:\\RVTools\\Data C:\\Reports\\Complete_Analysis.xlsx");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-e[/] C:\\RVTools\\Data C:\\Reports\\Skip_Empty_Values.xlsx");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]REQUIRED SHEETS AND COLUMNS:[/]");
        var table = new Table();
        table.AddColumn(new TableColumn("Sheet").LeftAligned());
        table.AddColumn(new TableColumn("Status").Centered());
        table.AddColumn(new TableColumn("Required Columns").LeftAligned());
        table.AddRow(
            "[green]vInfo[/]",
            "[bold green]Required[/]",
            "Template, SRM Placeholder, Powerstate, [bold]VM[/], [bold]CPUs[/], [bold]Memory[/], [bold]In Use MiB[/], [bold]OS according to the configuration file[/]"
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

        AnsiConsole.MarkupLine("[bold]DOWNLOADS:[/]");
        AnsiConsole.MarkupLine("  Latest releases are available at:");
        AnsiConsole.MarkupLine("  [link]https://github.com/sbroenne/RVToolsMerge/releases[/]");
    }

    /// <summary>
    /// Gets the column names from a worksheet, normalizing them using the per-sheet ColumnHeaderMapping.
    /// </summary>
    /// <param name="worksheet">The worksheet to extract column names from.</param>
    /// <returns>A list of normalized column names.</returns>
    private static List<string> GetColumnNames(IXLWorksheet worksheet)
    {
        var columnNames = new List<string>();
        var headerRow = worksheet.Row(1);
        var lastColumnUsed = worksheet.LastColumnUsed();
        int lastColumn = lastColumnUsed is not null ? lastColumnUsed.ColumnNumber() : 1;

        // Use only the mapping for the current sheet, if available
        var sheetName = worksheet.Name;
        SheetColumnHeaderMappings.TryGetValue(sheetName, out var mapping);

        for (int col = 1; col <= lastColumn; col++)
        {
            var cell = headerRow.Cell(col);
            var cellValue = cell.Value.ToString();
            if (!string.IsNullOrWhiteSpace(cellValue))
            {
                // Use the mapping if available for this sheet, otherwise keep the original name
                var normalizedName = mapping?.GetValueOrDefault(cellValue, cellValue) ?? cellValue;
                columnNames.Add(normalizedName);
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
        var lastColumnUsed = worksheet.LastColumnUsed();
        int lastColumn = lastColumnUsed is not null ? lastColumnUsed.ColumnNumber() : 1;

        // Create a mapping between the file's column indices and the common column indices
        for (int fileColIndex = 1; fileColIndex <= lastColumn; fileColIndex++)
        {
            var cell = headerRow.Cell(fileColIndex);
            var cellValue = cell.Value;
            if (!string.IsNullOrWhiteSpace(cellValue.ToString()))
            {
                // Apply column header mapping for this sheet, if available
                var sheetName = worksheet.Name;
                SheetColumnHeaderMappings.TryGetValue(sheetName, out var headerMapping);
                string originalName = cellValue.ToString();
                string mappedName = headerMapping is not null
                    ? ((IReadOnlyDictionary<string, string?>)headerMapping).GetValueOrDefault(originalName, null) ?? originalName
                    : originalName;

                int commonIndex = commonColumns.IndexOf(mappedName);
                if (commonIndex < 0 && headerMapping is not null)
                {
                    // If mapped name not found, try the original name from the sheet
                    commonIndex = commonColumns.IndexOf(originalName);
                }
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
