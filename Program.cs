//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="RVTools">
//     Copyright © RVTools Team 2025
//     Created by Stephan Brönnecke (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using ClosedXML.Excel;
using Spectre.Console;
using System.Reflection;

namespace RVToolsMerge
{
    class Program
    {
        // Required sheet names
        private static readonly string[] RequiredSheets = { "vInfo", "vHost", "vPartition", "vMemory" };
        private static readonly string[] MinimumRequiredSheets = { "vInfo" };

        // Mandatory columns for each sheet
        private static readonly Dictionary<string, string[]> MandatoryColumns = new Dictionary<string, string[]>
        {
            { "vInfo", new[] { "Template", "SRM Placeholder", "Powerstate", "VM", "CPUs", "Memory", "In Use MiB", "OS according to the VMware Tools" } },
            { "vHost", new[] { "Host", "Datacenter", "Cluster", "CPU Model", "Speed", "# CPU", "Cores per CPU", "# Cores", "CPU usage %", "# Memory", "Memory usage %" } },
            { "vPartition", new[] { "VM", "Disk", "Capacity MiB", "Consumed MiB" } },
            { "vMemory", new[] { "VM", "Size MiB", "Reservation" } }
        };

        static void Main(string[] args)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            string versionString = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";

            // Get product name from assembly attributes
            var productAttribute = Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute)) as AssemblyProductAttribute;
            string productName = productAttribute?.Product ?? "RVTools Merger";

            // Enhanced header display with Spectre.Console
            AnsiConsole.MarkupLine($"[bold green]{productName}[/] [yellow]v{versionString}[/]");
            AnsiConsole.Write(new Rule().RuleStyle("grey"));

            bool ignoreMissingOptionalSheets = false;
            bool skipInvalidFiles = false;
            bool anonymizeData = false;
            bool onlyMandatoryColumns = false;

            // Process options
            var processedArgs = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-h" || args[i] == "--help" || args[i] == "/?")
                {
                    ShowHelp();
                    return;
                }
                else if (args[i] == "-m" || args[i] == "--ignore-missing-optional-sheets")
                {
                    ignoreMissingOptionalSheets = true;
                }
                else if (args[i] == "-i" || args[i] == "--skip-invalid-files")
                {
                    skipInvalidFiles = true;
                }
                else if (args[i] == "-a" || args[i] == "--anonymize")
                {
                    anonymizeData = true;
                }
                else if (args[i] == "-o" || args[i] == "--only-mandatory-columns")
                {
                    onlyMandatoryColumns = true;
                }
                else
                {
                    processedArgs.Add(args[i]);
                }
            }

            // Cannot use both skip options together as they have contradictory behavior
            if (ignoreMissingOptionalSheets && skipInvalidFiles)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] You cannot use both --ignore-missing-optional-sheets and --skip-invalid-files together.");
                AnsiConsole.MarkupLine("--ignore-missing-optional-sheets: Ignores missing optional sheets (vHost, vPartition & vMemory) but processes all files");
                AnsiConsole.MarkupLine("--skip-invalid-files: Keeps full sheet validation but skips non-compliant files");
                AnsiConsole.MarkupLine("Use --help for more information.");
                return;
            }

            // Get input folder (default to "input" if not specified)
            string inputFolder = processedArgs.Count > 0 ? processedArgs[0] : Path.Combine(Directory.GetCurrentDirectory(), "input");
            if (!Directory.Exists(inputFolder))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Input folder '[yellow]{inputFolder}[/]' does not exist.");
                AnsiConsole.MarkupLine("Use --help to see usage information.");
                return;
            }

            // Get output file path (default to "RVTools_Merged.xlsx" if not specified)
            string outputFile = processedArgs.Count > 1 ? processedArgs[1] : Path.Combine(Directory.GetCurrentDirectory(), "RVTools_Merged.xlsx");

            try
            {
                // Get all Excel files in the input folder
                var excelFiles = Directory.GetFiles(inputFolder, "*.xlsx");

                if (excelFiles.Length == 0)
                {
                    AnsiConsole.MarkupLine($"[yellow]No Excel files found in '{inputFolder}'.[/]");
                    return;
                }
                AnsiConsole.MarkupLine($"[green]Found {excelFiles.Length} Excel files to process.[/]");

                // Process the files
                MergeRVToolsFiles(excelFiles, outputFile, ignoreMissingOptionalSheets, skipInvalidFiles, anonymizeData, onlyMandatoryColumns);
                AnsiConsole.MarkupLine($"[green]Successfully merged files.[/] Output saved to: [blue]{outputFile}[/]");

                // Get product name from assembly attributes
                var finalProductAttr = Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyProductAttribute)) as AssemblyProductAttribute;
                string finalProdName = finalProductAttr?.Product ?? "RVToolsMerge";

                AnsiConsole.MarkupLine($"Thank you for using [green]{finalProdName}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                if (ex.StackTrace != null)
                {
                    AnsiConsole.WriteLine(ex.StackTrace);
                }
            }
        }

        static void MergeRVToolsFiles(string[] filePaths, string outputPath, bool ignoreMissingOptionalSheets = false, bool skipInvalidFiles = false, bool anonymizeData = false, bool onlyMandatoryColumns = false)
        {
            if (filePaths.Length == 0) return;

            // Dictionary to store merged data for each sheet - using string arrays to avoid casting issues
            var mergedData = new Dictionary<string, List<string[]>>();

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
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn()
                })
                .Start(ctx =>
                {
                    var validationTask = ctx.AddTask("[green]Validating files[/]", maxValue: validFilePaths.Count);

                    for (int i = validFilePaths.Count - 1; i >= 0; i--)
                    {
                        string filePath = validFilePaths[i];
                        string fileName = Path.GetFileName(filePath);
                        bool fileIsValid = true;

                        try
                        {
                            using (var workbook = new XLWorkbook(filePath))
                            {
                                // Always check for the minimum required sheets (vInfo must exist)
                                var missingMinimumSheets = MinimumRequiredSheets.Where(sheet => !SheetExists(workbook, sheet)).ToList();

                                if (missingMinimumSheets.Any())
                                {
                                    fileIsValid = false;
                                    if (skipInvalidFiles)
                                    {
                                        AnsiConsole.MarkupLine($"Skipping file '[yellow]{fileName}[/]' - missing required sheet(s): [red]{string.Join(", ", missingMinimumSheets)}[/]");
                                        skippedFiles.Add(fileName);
                                    }
                                    else
                                    {
                                        throw new Exception($"File '{fileName}' is missing required sheet(s): {string.Join(", ", missingMinimumSheets)}");
                                    }
                                }
                                else if (!ignoreMissingOptionalSheets)
                                {
                                    // Full validation for all sheets when not ignoring missing optional sheets
                                    var missingSheets = RequiredSheets.Where(sheet => !SheetExists(workbook, sheet)).ToList();

                                    if (missingSheets.Any())
                                    {
                                        fileIsValid = false;
                                        if (skipInvalidFiles)
                                        {
                                            AnsiConsole.MarkupLine($"Skipping file '[yellow]{fileName}[/]' - missing sheet(s): [red]{string.Join(", ", missingSheets)}[/]");
                                            skippedFiles.Add(fileName);
                                        }
                                        else
                                        {
                                            throw new Exception($"File '{fileName}' is missing required sheet(s): {string.Join(", ", missingSheets)}");
                                        }
                                    }
                                }

                                // Validate mandatory columns in each sheet that exists
                                if (fileIsValid)
                                {
                                    foreach (var sheetName in RequiredSheets)
                                    {
                                        if (SheetExists(workbook, sheetName))
                                        {
                                            var worksheet = workbook.Worksheet(sheetName);
                                            var columnNames = GetColumnNames(worksheet);

                                            // Check for mandatory columns
                                            if (MandatoryColumns.TryGetValue(sheetName, out var mandatoryColumns))
                                            {
                                                var missingColumns = mandatoryColumns.Where(col => !columnNames.Contains(col)).ToList();

                                                if (missingColumns.Any())
                                                {
                                                    fileIsValid = false;
                                                    if (skipInvalidFiles)
                                                    {
                                                        AnsiConsole.MarkupLine($"Skipping file '[yellow]{fileName}[/]' - sheet '[cyan]{sheetName}[/]' is missing mandatory column(s): [red]{string.Join(", ", missingColumns)}[/]");
                                                        skippedFiles.Add(fileName);
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        throw new Exception($"File '{fileName}', sheet '{sheetName}' is missing mandatory column(s): {string.Join(", ", missingColumns)}");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex) when (skipInvalidFiles)
                        {
                            // If we're skipping invalid files and there's any error opening the file, skip it
                            AnsiConsole.MarkupLine($"Skipping file '[yellow]{fileName}[/]' due to error: [red]{ex.Message}[/]");
                            fileIsValid = false;
                            skippedFiles.Add(fileName);
                        }

                        if (!fileIsValid && skipInvalidFiles)
                        {
                            validFilePaths.RemoveAt(i);
                        }

                        validationTask.Increment(1);
                    }

                    if (skippedFiles.Count > 0)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Skipped {skippedFiles.Count} invalid files out of {filePaths.Length}[/]");
                    }

                    if (validFilePaths.Count == 0)
                    {
                        throw new Exception("No valid files to process after skipping invalid files.");
                    }
                });

            // Second pass for valid files to determine which sheets are available
            if (ignoreMissingOptionalSheets)
            {
                foreach (var filePath in validFilePaths)
                {
                    var fileName = Path.GetFileName(filePath);

                    using (var workbook = new XLWorkbook(filePath))
                    {
                        for (int i = availableSheets.Count - 1; i >= 0; i--)
                        {
                            string sheet = availableSheets[i];
                            if (!MinimumRequiredSheets.Contains(sheet) && !SheetExists(workbook, sheet))
                            {
                                AnsiConsole.MarkupLine($"[yellow]Warning:[/] File '[cyan]{fileName}[/]' is missing optional sheet '[green]{sheet}[/]'");
                                availableSheets.RemoveAt(i);
                            }
                        }
                    }
                }

                if (ignoreMissingOptionalSheets && availableSheets.Count < RequiredSheets.Length)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] Some sheets are not available in all files. Only processing: [green]{string.Join(", ", availableSheets)}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[green]All files have the required sheets.[/]");
                }
            }

            AnsiConsole.MarkupLine($"[bold]Processing {validFilePaths.Count} valid files...[/]");
            AnsiConsole.MarkupLine("[bold]Analyzing columns...[/]");

            // First pass: Determine common columns across all files for each sheet
            AnsiConsole.Progress()
                .AutoClear(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn()
                })
                .Start(ctx =>
                {
                    var columnsTask = ctx.AddTask("[green]Analyzing columns[/]", maxValue: availableSheets.Count);

                    foreach (var sheetName in availableSheets)
                    {
                        var allFileColumns = new List<List<string>>();

                        var fileAnalysisTask = ctx.AddTask($"[cyan]Analyzing '{sheetName}' sheet[/]", maxValue: validFilePaths.Count);

                        foreach (var filePath in validFilePaths)
                        {
                            using (var workbook = new XLWorkbook(filePath))
                            {
                                var worksheet = workbook.Worksheet(sheetName);
                                var columnNames = GetColumnNames(worksheet);
                                allFileColumns.Add(columnNames);
                            }
                            fileAnalysisTask.Increment(1);
                        }

                        fileAnalysisTask.StopTask();

                        // Find columns that exist in all files for this sheet
                        var columnsInAllFiles = allFileColumns.Count > 0
                            ? allFileColumns.Aggregate((a, b) => a.Intersect(b).ToList())
                            : new List<string>();

                        // If only mandatory columns are requested, filter the common columns
                        if (onlyMandatoryColumns && MandatoryColumns.TryGetValue(sheetName, out var mandatoryColumns))
                        {
                            // Process mandatory column names for comparison
                            var processedMandatoryColumns = mandatoryColumns
                                .Select(col => col.Contains("vInfo") ? col.Replace("vInfo", "").Trim() : col)
                                .ToArray();

                            columnsInAllFiles = columnsInAllFiles.Intersect(processedMandatoryColumns).ToList();

                            // Make sure all mandatory columns are included
                            foreach (var col in processedMandatoryColumns)
                            {
                                if (!columnsInAllFiles.Contains(col))
                                {
                                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] Mandatory column '[cyan]{col}[/]' for sheet '[green]{sheetName}[/]' is missing from common columns.");
                                }
                            }
                        }

                        commonColumns[sheetName] = columnsInAllFiles;
                        AnsiConsole.MarkupLine($"Sheet '[green]{sheetName}[/]' has [yellow]{columnsInAllFiles.Count}[/] {(onlyMandatoryColumns ? "mandatory" : "common")} columns across all files.");

                        columnsTask.Increment(1);
                    }
                });

            AnsiConsole.MarkupLine("[bold]Extracting data from files...[/]");

            // Display anonymization message if enabled
            if (anonymizeData)
            {
                AnsiConsole.MarkupLine("[yellow]Anonymization enabled[/] - VM, DNS Name, Cluster, Host, and Datacenter names will be anonymized.");
            }

            // Second pass: Extract data using only common columns
            AnsiConsole.Progress()
                .AutoClear(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn()
                })
                .Start(ctx =>
                {
                    var extractionTask = ctx.AddTask("[green]Extracting data[/]", maxValue: availableSheets.Count);

                    foreach (var sheetName in availableSheets)
                    {
                        mergedData[sheetName] = new List<string[]>
                        {
                            commonColumns[sheetName].ToArray()
                        };

                        var sheetTask = ctx.AddTask($"[cyan]Processing '{sheetName}'[/]", maxValue: validFilePaths.Count);

                        foreach (var filePath in validFilePaths)
                        {
                            var fileName = Path.GetFileName(filePath);

                            using (var workbook = new XLWorkbook(filePath))
                            {
                                var worksheet = workbook.Worksheet(sheetName);
                                var columnMapping = GetColumnMapping(worksheet, commonColumns[sheetName]);

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

                                // Extract data rows
                                for (int row = 2; row <= lastRow; row++)
                                {
                                    var rowData = new string[commonColumns[sheetName].Count];

                                    // Initialize with default values
                                    for (int i = 0; i < rowData.Length; i++)
                                    {
                                        rowData[i] = string.Empty;
                                    }

                                    // Only fill data for columns that exist in this file
                                    foreach (var mapping in columnMapping)
                                    {
                                        var cell = worksheet.Cell(row, mapping.FileColumnIndex);
                                        string cellValue = cell.Value.ToString();

                                        // Apply anonymization if needed
                                        if (anonymizeData)
                                        {
                                            // VM Name
                                            if (anonymizeColumnIndices.TryGetValue("VM", out int vmColIndex) &&
                                                mapping.CommonColumnIndex == vmColIndex && !string.IsNullOrWhiteSpace(cellValue))
                                            {
                                                if (!vmNameMap.ContainsKey(cellValue))
                                                {
                                                    vmNameMap[cellValue] = $"vm{vmNameMap.Count + 1}";
                                                }
                                                cellValue = vmNameMap[cellValue];
                                            }

                                            // DNS Name
                                            if (anonymizeColumnIndices.TryGetValue("DNS Name", out int dnsColIndex) &&
                                                mapping.CommonColumnIndex == dnsColIndex && !string.IsNullOrWhiteSpace(cellValue))
                                            {
                                                if (!dnsNameMap.ContainsKey(cellValue))
                                                {
                                                    dnsNameMap[cellValue] = $"dns{dnsNameMap.Count + 1}";
                                                }
                                                cellValue = dnsNameMap[cellValue];
                                            }

                                            // Cluster Name
                                            if (anonymizeColumnIndices.TryGetValue("Cluster", out int clusterColIndex) &&
                                                mapping.CommonColumnIndex == clusterColIndex && !string.IsNullOrWhiteSpace(cellValue))
                                            {
                                                if (!clusterNameMap.ContainsKey(cellValue))
                                                {
                                                    clusterNameMap[cellValue] = $"cluster{clusterNameMap.Count + 1}";
                                                }
                                                cellValue = clusterNameMap[cellValue];
                                            }

                                            // Host Name
                                            if (anonymizeColumnIndices.TryGetValue("Host", out int hostColIndex) &&
                                                mapping.CommonColumnIndex == hostColIndex && !string.IsNullOrWhiteSpace(cellValue))
                                            {
                                                if (!hostNameMap.ContainsKey(cellValue))
                                                {
                                                    hostNameMap[cellValue] = $"host{hostNameMap.Count + 1}";
                                                }
                                                cellValue = hostNameMap[cellValue];
                                            }

                                            // Datacenter Name
                                            if (anonymizeColumnIndices.TryGetValue("Datacenter", out int datacenterColIndex) &&
                                                mapping.CommonColumnIndex == datacenterColIndex && !string.IsNullOrWhiteSpace(cellValue))
                                            {
                                                if (!datacenterNameMap.ContainsKey(cellValue))
                                                {
                                                    datacenterNameMap[cellValue] = $"datacenter{datacenterNameMap.Count + 1}";
                                                }
                                                cellValue = datacenterNameMap[cellValue];
                                            }
                                        }

                                        // Store the value
                                        rowData[mapping.CommonColumnIndex] = cellValue;
                                    }

                                    mergedData[sheetName].Add(rowData);
                                }
                            }

                            sheetTask.Increment(1);
                        }

                        sheetTask.StopTask();
                        extractionTask.Increment(1);
                    }
                });

            AnsiConsole.MarkupLine("[bold]Creating output file...[/]");

            AnsiConsole.Progress()
                .AutoClear(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn()
                })
                .Start(ctx =>
                {
                    var outputTask = ctx.AddTask("[green]Creating output file[/]", maxValue: availableSheets.Count + 1);

                    using (var workbook = new XLWorkbook())
                    {
                        foreach (var sheetName in availableSheets)
                        {
                            var worksheet = workbook.Worksheets.Add(sheetName);
                            var rowTask = ctx.AddTask($"[cyan]Writing '{sheetName}' sheet[/]", maxValue: mergedData[sheetName].Count);

                            // Write data to sheet
                            for (int row = 0; row < mergedData[sheetName].Count; row++)
                            {
                                for (int col = 0; col < mergedData[sheetName][row].Length; col++)
                                {
                                    // Use SetValue instead of directly assigning to Value property
                                    worksheet.Cell(row + 1, col + 1).SetValue(mergedData[sheetName][row][col]);
                                }
                                rowTask.Increment(1);
                            }

                            // Auto-fit columns
                            worksheet.Columns().AdjustToContents();
                            rowTask.StopTask();
                            outputTask.Increment(1);
                        }

                        // Save the output file
                        AnsiConsole.MarkupLine($"[cyan]Saving file to disk...[/]");
                        workbook.SaveAs(outputPath);
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
                AnsiConsole.MarkupLine($"[bold]Total:[/] [green]{totalAnonymized}[/] items anonymized");
            }
        }

        static bool SheetExists(XLWorkbook workbook, string sheetName)
        {
            return workbook.Worksheets.Any(sheet => sheet.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase));
        }

        static void ShowHelp()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            string versionString = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";

            // Get product name from assembly attributes
            var productAttribute = Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute)) as AssemblyProductAttribute;
            string productName = productAttribute?.Product ?? "RVTools Excel Merger";

            // Enhanced header display with Spectre.Console
            AnsiConsole.MarkupLine($"[bold green]{productName}[/] - Merges multiple RVTools Excel files into a single file");
            AnsiConsole.MarkupLine($"[yellow]Version {versionString}[/]");
            AnsiConsole.Write(new Rule().RuleStyle("grey"));
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold]USAGE:[/]");
            AnsiConsole.MarkupLine($"  [cyan]{Assembly.GetExecutingAssembly().GetName().Name}[/] [grey][[options]] [[inputFolder]] [[outputFile]][/]");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold]ARGUMENTS:[/]");
            AnsiConsole.MarkupLine("  [green]inputFolder[/]   Path to the folder containing RVTools Excel files.");
            AnsiConsole.MarkupLine("                Defaults to \"input\" subfolder in the current directory.");
            AnsiConsole.MarkupLine("  [green]outputFile[/]    Path where the merged file will be saved.");
            AnsiConsole.MarkupLine("                Defaults to \"RVTools_Merged.xlsx\" in the current directory.");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold]OPTIONS:[/]");
            AnsiConsole.MarkupLine("  [yellow]-h, --help, /?[/]            Show this help message and exit.");
            AnsiConsole.MarkupLine("  [yellow]-m, --ignore-missing-optional-sheets[/]");
            AnsiConsole.MarkupLine("                            Ignore missing optional sheets (vHost, vPartition & vMemory).");
            AnsiConsole.MarkupLine("                            Will still validate vInfo sheet exists.");
            AnsiConsole.MarkupLine("  [yellow]-i, --skip-invalid-files[/]  Skip files that don't contain all required sheets");
            AnsiConsole.MarkupLine("                            instead of failing with an error.");
            AnsiConsole.MarkupLine("  [yellow]-a, --anonymize[/]           Anonymize VM, DNS Name, Cluster, Host, and Datacenter");
            AnsiConsole.MarkupLine("                            columns with generic names (vm1, host1, etc.).");
            AnsiConsole.MarkupLine("  [yellow]-o, --only-mandatory-columns[/]");
            AnsiConsole.MarkupLine("                            Include only the mandatory columns for each sheet in the");
            AnsiConsole.MarkupLine("                            output file instead of all common columns.");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold]DESCRIPTION:[/]");
            AnsiConsole.MarkupLine("  This tool merges all RVTools Excel files (XLSX format) from the specified");
            AnsiConsole.MarkupLine("  folder into one consolidated file. It extracts data from the following sheets:");
            AnsiConsole.MarkupLine("    - [green]vInfo[/] (required)");
            AnsiConsole.MarkupLine("    - [cyan]vHost[/] (optional)");
            AnsiConsole.MarkupLine("    - [cyan]vPartition[/] (optional)");
            AnsiConsole.MarkupLine("    - [cyan]vMemory[/] (optional)");
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

            AnsiConsole.MarkupLine("[bold]Mandatory columns by sheet:[/]");
            var table = new Table();
            table.AddColumn(new TableColumn("Sheet").LeftAligned());
            table.AddColumn(new TableColumn("Mandatory Columns").LeftAligned());

            table.AddRow(
                "[green]vInfo[/]",
                "Template, SRM Placeholder, Powerstate, VM, CPUs, Memory, In Use MiB, OS according to the VMware Tools"
            );

            table.AddRow(
                "[cyan]vHost[/]",
                "Host, Datacenter, Cluster, CPU Model, Speed, # CPU, Cores per CPU, # Cores, CPU usage %, # Memory, Memory usage %"
            );

            table.AddRow(
                "[cyan]vPartition[/]",
                "VM, Disk, Capacity MiB, Consumed MiB"
            );

            table.AddRow(
                "[cyan]vMemory[/]",
                "VM, Size MiB, Reservation"
            );
            table.Border(TableBorder.Rounded);
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold]Validation behavior:[/]");
            AnsiConsole.MarkupLine("  - By default, all sheets must exist in all files");
            AnsiConsole.MarkupLine("  - When using [yellow]--ignore-missing-optional-sheets[/], optional sheets can be missing");
            AnsiConsole.MarkupLine("    with warnings shown. The vInfo sheet is always required.");
            AnsiConsole.MarkupLine("  - When using [yellow]--skip-invalid-files[/], files without required sheets will be skipped");
            AnsiConsole.MarkupLine("    and reported, but processing will continue with valid files.");
            AnsiConsole.MarkupLine("  - When using [yellow]--anonymize[/], sensitive names are replaced with generic identifiers");
            AnsiConsole.MarkupLine("    (vm1, dns1, host1, etc.) to protect sensitive information.");
            AnsiConsole.MarkupLine("  - When using [yellow]--only-mandatory-columns[/], only the mandatory columns for each sheet");
            AnsiConsole.MarkupLine("    are included in the output, regardless of what other columns might be common");
            AnsiConsole.MarkupLine("    across all files.");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("[bold]EXAMPLES:[/]");
            string appName = Assembly.GetExecutingAssembly().GetName().Name ?? "RVToolsMerge";
            AnsiConsole.MarkupLine($"  [cyan]{appName}[/] C:\\RVTools\\Data");
            AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-m[/] C:\\RVTools\\Data C:\\Reports\\Merged_RVTools.xlsx");
            AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]--ignore-missing-optional-sheets[/] C:\\RVTools\\Data");
            AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-i[/] C:\\RVTools\\Data");
            AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-a[/] C:\\RVTools\\Data C:\\Reports\\Anonymized_RVTools.xlsx");
            AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-o[/] C:\\RVTools\\Data C:\\Reports\\Mandatory_Columns.xlsx");
            AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-a -o[/] C:\\RVTools\\Data C:\\Reports\\Anonymized_Mandatory.xlsx");
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

        static List<string> GetColumnNames(IXLWorksheet worksheet)
        {
            var columnNames = new List<string>();

            // Get the first row
            var headerRow = worksheet.Row(1);

            // Find the last column with data
            int lastColumn = worksheet.LastColumnUsed().ColumnNumber();

            for (int col = 1; col <= lastColumn; col++)
            {
                var cellValue = headerRow.Cell(col).Value.ToString();
                if (!string.IsNullOrWhiteSpace(cellValue))
                {
                    columnNames.Add(cellValue);
                }
            }

            return columnNames;
        }

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
                var cellValue = headerRow.Cell(fileColIndex).Value.ToString();
                if (!string.IsNullOrWhiteSpace(cellValue))
                {
                    // Remove the code that processes column names
                    int commonIndex = commonColumns.IndexOf(cellValue);
                    if (commonIndex >= 0)
                    {
                        mapping.Add(new ColumnMapping
                        {
                            FileColumnIndex = fileColIndex,
                            CommonColumnIndex = commonIndex
                        });
                    }
                }
            }

            return mapping;
        }

        class ColumnMapping
        {
            public int FileColumnIndex { get; set; }
            public int CommonColumnIndex { get; set; }
        }
    }
}
