//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="RVTools">
//     Copyright © RVTools Team 2025
//     Created by Stephan Brönnecke (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using ClosedXML.Excel;
using System.Reflection;

namespace RVToolsMerge
{    class Program
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
        };        static void Main(string[] args)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            string versionString = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";

            // Get product name from assembly attributes
            var productAttribute = Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute)) as AssemblyProductAttribute;
            string productName = productAttribute?.Product ?? "RVTools Merger";

            Console.WriteLine($"{productName} v{versionString}");
            Console.WriteLine("----------------------------------------");
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
                }                else
                {
                    processedArgs.Add(args[i]);
                }
            }

            // Cannot use both skip options together as they have contradictory behavior
            if (ignoreMissingOptionalSheets && skipInvalidFiles)
            {
                Console.WriteLine("Error: You cannot use both --ignore-missing-optional-sheets and --skip-invalid-files together.");
                Console.WriteLine("--ignore-missing-optional-sheets: Ignores missing optional sheets (vHost, vPartition & vMemory) but processes all files");
                Console.WriteLine("--skip-invalid-files: Keeps full sheet validation but skips non-compliant files");
                Console.WriteLine("Use --help for more information.");
                return;
            }

            // Get input folder (default to "input" if not specified)
            string inputFolder = processedArgs.Count > 0 ? processedArgs[0] : Path.Combine(Directory.GetCurrentDirectory(), "input");
            if (!Directory.Exists(inputFolder))
            {
                Console.WriteLine($"Error: Input folder '{inputFolder}' does not exist.");
                Console.WriteLine("Use --help to see usage information.");
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
                    Console.WriteLine($"No Excel files found in '{inputFolder}'.");
                    return;                }
                Console.WriteLine($"Found {excelFiles.Length} Excel files to process.");

                // Process the files
                MergeRVToolsFiles(excelFiles, outputFile, ignoreMissingOptionalSheets, skipInvalidFiles, anonymizeData, onlyMandatoryColumns);
                Console.WriteLine($"Successfully merged files. Output saved to: {outputFile}");

                // Get product name from assembly attributes
                var finalProductAttr = Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyProductAttribute)) as AssemblyProductAttribute;
                string finalProdName = finalProductAttr?.Product ?? "RVToolsMerge";

                Console.WriteLine($"Thank you for using {finalProdName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
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
            Console.WriteLine("Validating files...");            // Track which sheets actually exist in all files
            var availableSheets = new List<string>(RequiredSheets);

            // First pass to check which files are valid
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
                                Console.WriteLine($"Skipping file '{fileName}' - missing required sheet(s): {string.Join(", ", missingMinimumSheets)}");
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
                                    Console.WriteLine($"Skipping file '{fileName}' - missing sheet(s): {string.Join(", ", missingSheets)}");
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
                                    var columnNames = GetColumnNames(worksheet);                                    // Check for mandatory columns
                                    if (MandatoryColumns.TryGetValue(sheetName, out var mandatoryColumns))
                                    {
                                        // Process mandatory column names for comparison
                                        var processedMandatoryColumns = mandatoryColumns
                                            .Select(col => col.Contains("vInfo") ? col.Replace("vInfo", "").Trim() : col)
                                            .ToArray();

                                        var missingColumns = processedMandatoryColumns.Where(col => !columnNames.Contains(col)).ToList();

                                        if (missingColumns.Any())
                                        {
                                            fileIsValid = false;
                                            if (skipInvalidFiles)
                                            {
                                                Console.WriteLine($"Skipping file '{fileName}' - sheet '{sheetName}' is missing mandatory column(s): {string.Join(", ", missingColumns)}");
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
                    Console.WriteLine($"Skipping file '{fileName}' due to error: {ex.Message}");
                    fileIsValid = false;
                    skippedFiles.Add(fileName);
                }

                if (!fileIsValid && skipInvalidFiles)
                {
                    validFilePaths.RemoveAt(i);
                }
            }

            if (skippedFiles.Count > 0)
            {
                Console.WriteLine($"Skipped {skippedFiles.Count} invalid files out of {filePaths.Length}");

                if (validFilePaths.Count == 0)
                {
                    throw new Exception("No valid files to process after skipping invalid files.");
                }
            }

            // Second pass for valid files to determine which sheets are available
            if (ignoreMissingOptionalSheets)
            {
                foreach (var filePath in validFilePaths)
                {
                    using (var workbook = new XLWorkbook(filePath))
                    {
                        var fileName = Path.GetFileName(filePath);

                        // When skipping validation, track which optional sheets are actually available in all files
                        for (int i = availableSheets.Count - 1; i >= 0; i--)
                        {
                            string sheet = availableSheets[i];
                            if (!MinimumRequiredSheets.Contains(sheet) && !SheetExists(workbook, sheet))
                            {
                                Console.WriteLine($"Warning: File '{fileName}' is missing optional sheet '{sheet}'");
                                availableSheets.RemoveAt(i);
                            }
                        }
                    }
                }
            }

            if (ignoreMissingOptionalSheets && availableSheets.Count < RequiredSheets.Length)
            {
                Console.WriteLine($"Warning: Some sheets are not available in all files. Only processing: {string.Join(", ", availableSheets)}");
            }
            else
            {
                Console.WriteLine("All files have the required sheets.");
            }

            Console.WriteLine($"Processing {validFilePaths.Count} valid files...");
            Console.WriteLine("Analyzing columns...");            // First pass: Determine common columns across all files for each sheet
            foreach (var sheetName in availableSheets)
            {
                var allFileColumns = new List<List<string>>();

                foreach (var filePath in validFilePaths)
                {
                    using (var workbook = new XLWorkbook(filePath))
                    {
                        var worksheet = workbook.Worksheet(sheetName);
                        var columnNames = GetColumnNames(worksheet);
                        allFileColumns.Add(columnNames);
                    }
                }

                // Find columns that exist in all files for this sheet
                var columnsInAllFiles = allFileColumns.Count > 0
                    ? allFileColumns.Aggregate((a, b) => a.Intersect(b).ToList())
                    : new List<string>();                // If only mandatory columns are requested, filter the common columns
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
                            Console.WriteLine($"Warning: Mandatory column '{col}' for sheet '{sheetName}' is missing from common columns.");
                        }
                    }
                }

                commonColumns[sheetName] = columnsInAllFiles;
                Console.WriteLine($"Sheet '{sheetName}' has {columnsInAllFiles.Count} {(onlyMandatoryColumns ? "mandatory" : "common")} columns across all files.");
            }

            // Second pass: Extract data using only common columns
            Console.WriteLine("Extracting data...");

            // Display anonymization message if enabled
            if (anonymizeData)
            {
                Console.WriteLine("Anonymization enabled - VM, DNS Name, Cluster, Host, and Datacenter names will be anonymized.");
            }

            foreach (var sheetName in availableSheets)
            {
                mergedData[sheetName] = new List<string[]>
                {
                    commonColumns[sheetName].ToArray()
                };  // Add header row first

                // Process each file
                foreach (var filePath in validFilePaths)
                {
                    var fileName = Path.GetFileName(filePath);
                    Console.WriteLine($"Processing '{sheetName}' in {fileName}...");

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
                }
            }

            // Create output file
            Console.WriteLine("Creating output file...");
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
                            // Use SetValue instead of directly assigning to Value property
                            worksheet.Cell(row + 1, col + 1).SetValue(mergedData[sheetName][row][col]);
                        }
                    }

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();
                }

                // Save the output file
                workbook.SaveAs(outputPath);
            }

            // Display anonymization summary if enabled
            if (anonymizeData)
            {
                Console.WriteLine("\nAnonymization Summary:");
                Console.WriteLine($"  VMs:         {vmNameMap.Count} unique names anonymized");
                Console.WriteLine($"  DNS Names:   {dnsNameMap.Count} unique names anonymized");
                Console.WriteLine($"  Clusters:    {clusterNameMap.Count} unique names anonymized");
                Console.WriteLine($"  Hosts:       {hostNameMap.Count} unique names anonymized");
                Console.WriteLine($"  Datacenters: {datacenterNameMap.Count} unique names anonymized");
                Console.WriteLine($"  Total:       {vmNameMap.Count + dnsNameMap.Count + clusterNameMap.Count + hostNameMap.Count + datacenterNameMap.Count} items anonymized");
            }
        }

        static bool SheetExists(XLWorkbook workbook, string sheetName)
        {
            return workbook.Worksheets.Any(sheet => sheet.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase));
        }        static void ShowHelp()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            string versionString = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";

            // Get product name from assembly attributes
            var productAttribute = Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute)) as AssemblyProductAttribute;
            string productName = productAttribute?.Product ?? "RVTools Excel Merger";

            Console.WriteLine($"{productName} - Merges multiple RVTools Excel files into a single file");
            Console.WriteLine($"Version {versionString}");
            Console.WriteLine("===========================================================");            Console.WriteLine();
            Console.WriteLine("USAGE:");
            Console.WriteLine("  {0} [options] [inputFolder] [outputFile]", Assembly.GetExecutingAssembly().GetName().Name);
            Console.WriteLine();
            Console.WriteLine("ARGUMENTS:");
            Console.WriteLine("  inputFolder   Path to the folder containing RVTools Excel files.");
            Console.WriteLine("                Defaults to \"input\" subfolder in the current directory.");
            Console.WriteLine("  outputFile    Path where the merged file will be saved.");
            Console.WriteLine("                Defaults to \"RVTools_Merged.xlsx\" in the current directory.");
            Console.WriteLine();
            Console.WriteLine("OPTIONS:");
            Console.WriteLine("  -h, --help, /?            Show this help message and exit.");
            Console.WriteLine("  -m, --ignore-missing-optional-sheets");
            Console.WriteLine("                            Ignore missing optional sheets (vHost, vPartition & vMemory).");
            Console.WriteLine("                            Will still validate vInfo sheet exists.");
            Console.WriteLine("  -i, --skip-invalid-files  Skip files that don't contain all required sheets");
            Console.WriteLine("                            instead of failing with an error.");
            Console.WriteLine("  -a, --anonymize           Anonymize VM, DNS Name, Cluster, Host, and Datacenter");
            Console.WriteLine("                            columns with generic names (vm1, host1, etc.).");
            Console.WriteLine("  -o, --only-mandatory-columns");
            Console.WriteLine("                            Include only the mandatory columns for each sheet in the");
            Console.WriteLine("                            output file instead of all common columns.");
            Console.WriteLine();
            Console.WriteLine("  Note: --ignore-missing-optional-sheets and --skip-invalid-files cannot be used together.");
            Console.WriteLine();
            Console.WriteLine("DESCRIPTION:");
            Console.WriteLine("  This tool merges all RVTools Excel files (XLSX format) from the specified");
            Console.WriteLine("  folder into one consolidated file. It extracts data from the following sheets:");
            Console.WriteLine("    - vInfo (required)");
            Console.WriteLine("    - vHost (optional)");
            Console.WriteLine("    - vPartition (optional)");
            Console.WriteLine("    - vMemory (optional)");
            Console.WriteLine();
            Console.WriteLine("  Features:");
            Console.WriteLine("  - Validates mandatory columns in each sheet");
            Console.WriteLine("  - Only includes columns that exist in all files for each respective sheet");
            Console.WriteLine("  - Anonymizes sensitive data when requested (VMs, DNS, Clusters, Hosts, Datacenters)");
            Console.WriteLine("  - Allows filtering to include only mandatory columns");
            Console.WriteLine("  - Works on multiple platforms (Windows, Linux, macOS)");
            Console.WriteLine("  - Minimal memory footprint with efficient processing");
            Console.WriteLine();
            Console.WriteLine("  Mandatory columns by sheet:");
            Console.WriteLine("   * vInfo: Template, SRM Placeholder, Powerstate, VM, CPUs, Memory, In Use MiB,");
            Console.WriteLine("            OS according to the VMware Tools");
            Console.WriteLine("   * vHost: Host, Datacenter, Cluster, CPU Model, Speed, # CPU, Cores per CPU,");
            Console.WriteLine("            # Cores, CPU usage %, # Memory, Memory usage %");
            Console.WriteLine("   * vPartition: VM, Disk, Capacity MiB, Consumed MiB");
            Console.WriteLine("   * vMemory: VM, Size MiB, Reservation");
            Console.WriteLine();
            Console.WriteLine("  Validation behavior:");
            Console.WriteLine("  - By default, all sheets must exist in all files");
            Console.WriteLine("  - When using --ignore-missing-optional-sheets, optional sheets can be missing");
            Console.WriteLine("    with warnings shown. The vInfo sheet is always required.");
            Console.WriteLine("  - When using --skip-invalid-files, files without required sheets will be skipped");
            Console.WriteLine("    and reported, but processing will continue with valid files.");
            Console.WriteLine("  - When using --anonymize, sensitive names are replaced with generic identifiers");
            Console.WriteLine("    (vm1, dns1, host1, etc.) to protect sensitive information.");
            Console.WriteLine("  - When using --only-mandatory-columns, only the mandatory columns for each sheet");
            Console.WriteLine("    are included in the output, regardless of what other columns might be common");
            Console.WriteLine("    across all files.");
            Console.WriteLine();            Console.WriteLine("EXAMPLES:");
            Console.WriteLine("  {0}", Assembly.GetExecutingAssembly().GetName().Name);
            Console.WriteLine("  {0} C:\\RVTools\\Data", Assembly.GetExecutingAssembly().GetName().Name);
            Console.WriteLine("  {0} -m C:\\RVTools\\Data C:\\Reports\\Merged_RVTools.xlsx", Assembly.GetExecutingAssembly().GetName().Name);
            Console.WriteLine("  {0} --ignore-missing-optional-sheets C:\\RVTools\\Data", Assembly.GetExecutingAssembly().GetName().Name);
            Console.WriteLine("  {0} -i C:\\RVTools\\Data", Assembly.GetExecutingAssembly().GetName().Name);
            Console.WriteLine("  {0} -a C:\\RVTools\\Data C:\\Reports\\Anonymized_RVTools.xlsx", Assembly.GetExecutingAssembly().GetName().Name);
            Console.WriteLine("  {0} -o C:\\RVTools\\Data C:\\Reports\\Mandatory_Columns.xlsx", Assembly.GetExecutingAssembly().GetName().Name);            Console.WriteLine("  {0} -a -o C:\\RVTools\\Data C:\\Reports\\Anonymized_Mandatory.xlsx", Assembly.GetExecutingAssembly().GetName().Name);
            Console.WriteLine();            Console.WriteLine("DOWNLOADS:");
            Console.WriteLine("  Latest releases for all supported platforms are available at:");
            Console.WriteLine("  https://github.com/sbroenne/RVToolsMerge/releases");
            string applicationName = Assembly.GetExecutingAssembly().GetName().Name ?? "RVToolsMerge";
            Console.WriteLine("  - {0}-windows-Release.zip       (Windows x64)", applicationName);
            Console.WriteLine("  - {0}-windows-arm64-Release.zip (Windows ARM64)", applicationName);
            Console.WriteLine("  - {0}-linux-Release.zip         (Linux x64)", applicationName);
            Console.WriteLine("  - {0}-macos-arm64-Release.zip   (macOS ARM64)", applicationName);
        }

        static List<string> GetColumnNames(IXLWorksheet worksheet)
        {
            var columnNames = new List<string>();

            // Get the first row
            var headerRow = worksheet.Row(1);

            // Find the last column with data
            int lastColumn = worksheet.LastColumnUsed().ColumnNumber();            for (int col = 1; col <= lastColumn; col++)
            {
                var cellValue = headerRow.Cell(col).Value.ToString();
                if (!string.IsNullOrWhiteSpace(cellValue))
                {
                    // Remove "vInfo" from column name if present
                    if (cellValue.Contains("vInfo"))
                    {
                        cellValue = cellValue.Replace("vInfo", "");
                        cellValue = cellValue.Trim(); // Remove any extra spaces that might be left
                    }
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
            int lastColumn = worksheet.LastColumnUsed().ColumnNumber();            // Create a mapping between the file's column indices and the common column indices
            for (int fileColIndex = 1; fileColIndex <= lastColumn; fileColIndex++)
            {
                var cellValue = headerRow.Cell(fileColIndex).Value.ToString();
                if (!string.IsNullOrWhiteSpace(cellValue))
                {
                    // Process column name to remove vInfo if present
                    string processedCellValue = cellValue;
                    if (processedCellValue.Contains("vInfo"))
                    {
                        processedCellValue = processedCellValue.Replace("vInfo", "").Trim();
                    }

                    int commonIndex = commonColumns.IndexOf(processedCellValue);
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
