//-----------------------------------------------------------------------
// <copyright file="MergeCommand.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using System.IO.Abstractions;
using System.Reflection;
using RVToolsMerge.Models;
using RVToolsMerge.Services;
using RVToolsMerge.Services.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RVToolsMerge.Commands;

/// <summary>
/// Command for merging RVTools Excel files.
/// </summary>
public class MergeCommand : AsyncCommand<MergeCommandSettings>
{
    private readonly ConsoleUIService _consoleUiService;
    private readonly IMergeService _mergeService;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="MergeCommand"/> class.
    /// </summary>
    /// <param name="consoleUiService">The console UI service.</param>
    /// <param name="mergeService">The merge service.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    public MergeCommand(
        ConsoleUIService consoleUiService,
        IMergeService mergeService,
        IFileSystem fileSystem)
    {
        _consoleUiService = consoleUiService;
        _mergeService = mergeService;
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Executes the merge command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code (0 for success, 1 for error).</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, MergeCommandSettings settings, CancellationToken cancellationToken)
    {
        // Get version and product information
        var appInfo = GetApplicationInfo();

        // Display header
        _consoleUiService.DisplayHeader(appInfo.ProductName, appInfo.VersionString);

        // Set default output path if not provided
        string outputPath = settings.OutputPath ?? "merged-output.xlsx";

        // Validate input path
        if (!ValidateInputPath(settings.InputPath, out bool isInputFile, out bool isInputDirectory, out string[] excelFiles))
        {
            return 1;
        }

        // Convert settings to MergeOptions for internal use
        var options = ConvertSettingsToOptions(settings);

        // Validate option compatibility
        if (options.AnonymizeData && options.ProcessAllSheets)
        {
            _consoleUiService.WriteLine();
            _consoleUiService.DisplayError("The --anonymize and --all-sheets options cannot be used together.");
            _consoleUiService.DisplayInfo("Anonymization is only supported for the core sheets (vInfo, vHost, vPartition, vMemory).");
            _consoleUiService.DisplayInfo("To anonymize data, remove the --all-sheets flag.");
            _consoleUiService.DisplayInfo("To process all sheets, remove the --anonymize flag.");
            return 1;
        }

        // Display selected options
        _consoleUiService.DisplayOptions(options);

        try
        {
            // Process the files
            var validationIssues = new List<ValidationIssue>();
            await _mergeService.MergeFilesAsync(excelFiles, outputPath, options, validationIssues);

            _consoleUiService.WriteLine();
            _consoleUiService.MarkupLineInterpolated($"[green]Successfully merged files.[/] Output saved to: [blue]{outputPath}[/]");

            _consoleUiService.WriteLine();
            _consoleUiService.MarkupLineInterpolated($"Thank you for using [green]{appInfo.ProductName}[/]");

            return 0;
        }
        catch (Exception ex)
        {
            HandleException(ex, settings.DebugMode);
            return 1;
        }
    }

    /// <summary>
    /// Converts command settings to merge options.
    /// </summary>
    /// <param name="settings">The command settings.</param>
    /// <returns>A MergeOptions instance.</returns>
    private static MergeOptions ConvertSettingsToOptions(MergeCommandSettings settings)
    {
        return new MergeOptions
        {
            IgnoreMissingOptionalSheets = settings.IgnoreMissingOptionalSheets,
            ProcessAllSheets = settings.AllSheets,
            SkipInvalidFiles = settings.SkipInvalidFiles,
            AnonymizeData = settings.AnonymizeData,
            OnlyMandatoryColumns = settings.OnlyMandatoryColumns,
            IncludeSourceFileName = settings.IncludeSourceFileName,
            SkipRowsWithEmptyMandatoryValues = settings.SkipRowsWithEmptyMandatoryValues,
            DebugMode = settings.DebugMode,
            EnableAzureMigrateValidation = settings.EnableAzureMigrateValidation,
            MaxVInfoRows = settings.MaxVInfoRows
        };
    }

    /// <summary>
    /// Gets application version and product information.
    /// </summary>
    /// <returns>A tuple containing product name and version string.</returns>
    private static (string ProductName, string VersionString) GetApplicationInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        string versionString = version is not null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";

        // Get product name from assembly attributes
        var productAttribute = Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute)) as AssemblyProductAttribute;
        string productName = productAttribute?.Product ?? "RVToolsMerger";

        return (productName, versionString);
    }

    /// <summary>
    /// Validates a path to ensure it doesn't contain directory traversal sequences.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is safe, false otherwise.</returns>
    private bool IsPathSafe(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        // Check for directory traversal patterns
        var normalizedPath = path.Replace('\\', '/');
        if (normalizedPath.Contains("../") || normalizedPath.Contains("..\\") ||
            normalizedPath.StartsWith("../") || normalizedPath.StartsWith("..\\") ||
            normalizedPath.EndsWith("/..") || normalizedPath.EndsWith("\\..") ||
            normalizedPath == "..")
        {
            return false;
        }

        // Get the full path and ensure it doesn't escape the current directory context
        try
        {
            var fullPath = _fileSystem.Path.GetFullPath(path);

            // For input files, we allow any valid path as long as it's not traversal
            // For output files, this validation should be called separately
            return !string.IsNullOrEmpty(fullPath);
        }
        catch
        {
            // If we can't get the full path, it's not safe
            return false;
        }
    }

    /// <summary>
    /// Validates the input path and gets the list of Excel files to process.
    /// </summary>
    /// <param name="inputPath">The input path to validate.</param>
    /// <param name="isInputFile">Output whether the input is a file.</param>
    /// <param name="isInputDirectory">Output whether the input is a directory.</param>
    /// <param name="excelFiles">Output array of Excel files to process.</param>
    /// <returns>True if the input path is valid, false otherwise.</returns>
    private bool ValidateInputPath(string inputPath, out bool isInputFile, out bool isInputDirectory, out string[] excelFiles)
    {
        isInputFile = false;
        isInputDirectory = false;
        excelFiles = Array.Empty<string>();

        // First, validate the path for security issues
        if (!IsPathSafe(inputPath))
        {
            _consoleUiService.MarkupLineInterpolated($"[red]Error:[/] Invalid path specified. Path traversal sequences are not allowed.");
            _consoleUiService.DisplayInfo("Use --help to see usage information.");
            return false;
        }

        isInputFile = _fileSystem.File.Exists(inputPath);
        isInputDirectory = _fileSystem.Directory.Exists(inputPath);

        if (!isInputFile && !isInputDirectory)
        {
            _consoleUiService.MarkupLineInterpolated($"[red]Error:[/] Input path '[yellow]{inputPath}[/]' does not exist as either a file or directory.");
            _consoleUiService.DisplayInfo("Use --help to see usage information.");
            return false;
        }

        // Get Excel files - either the single file or all Excel files in the directory
        if (isInputFile)
        {
            // Check if the file has the correct extension
            if (!_fileSystem.Path.GetExtension(inputPath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                _consoleUiService.MarkupLineInterpolated($"[red]Error:[/] Input file '[yellow]{inputPath}[/]' must be an Excel file (.xlsx).");
                return false;
            }
            excelFiles = new[] { inputPath };
            _consoleUiService.MarkupLineInterpolated($"[green]Processing single Excel file: {_fileSystem.Path.GetFileName(inputPath)}[/]");
        }
        else // isInputDirectory
        {
            excelFiles = _fileSystem.Directory.GetFiles(inputPath, "*.xlsx");
            if (excelFiles.Length == 0)
            {
                _consoleUiService.MarkupLineInterpolated($"[yellow]No Excel files found in '{inputPath}'.[/]");
                return false;
            }
            _consoleUiService.MarkupLineInterpolated($"[green]Found {excelFiles.Length} Excel files to process in directory.[/]");
        }

        return true;
    }

    /// <summary>
    /// Handles exceptions that occur during command execution.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="debugMode">Whether debug mode is enabled.</param>
    private void HandleException(Exception ex, bool debugMode)
    {
        _consoleUiService.DisplayError(_consoleUiService.GetUserFriendlyErrorMessage(ex));
        if (debugMode && ex.StackTrace != null)
        {
            _consoleUiService.WriteLine();
            _consoleUiService.DisplayInfo("[yellow]Debug information (stack trace):[/]");
            _consoleUiService.DisplayText(ex.StackTrace);
        }
        else
        {
            _consoleUiService.DisplayInfo("[grey]Use --debug flag to see detailed error information.[/]");
        }
    }
}
