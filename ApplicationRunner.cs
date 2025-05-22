//-----------------------------------------------------------------------
// <copyright file="ApplicationRunner.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using System.Reflection;
using RVToolsMerge.Models;
using RVToolsMerge.Services;
using RVToolsMerge.Services.Interfaces;
using Spectre.Console;

namespace RVToolsMerge;

/// <summary>
/// Runs the main application logic, separating it from the entry point.
/// </summary>
public class ApplicationRunner
{
    private readonly ConsoleUIService _consoleUiService;
    private readonly IMergeService _mergeService;
    private readonly ICommandLineParser _commandLineParser;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationRunner"/> class.
    /// </summary>
    /// <param name="consoleUiService">The console UI service.</param>
    /// <param name="mergeService">The merge service.</param>
    /// <param name="commandLineParser">The command line parser.</param>
    public ApplicationRunner(
        ConsoleUIService consoleUiService,
        IMergeService mergeService,
        ICommandLineParser commandLineParser)
    {
        _consoleUiService = consoleUiService;
        _mergeService = mergeService;
        _commandLineParser = commandLineParser;
    }

    /// <summary>
    /// Runs the application with the provided arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RunAsync(string[] args)
    {
        // Get version and product information
        var appInfo = GetApplicationInfo();
        _consoleUiService.DisplayHeader(appInfo.ProductName, appInfo.VersionString);

        // Parse command line options
        var options = new MergeOptions();
        bool helpRequested = _commandLineParser.ParseArguments(args, options, out string? inputPath, out string? outputPath);

        if (helpRequested)
        {
            _consoleUiService.ShowHelp(Assembly.GetExecutingAssembly().GetName().Name ?? "RVToolsMerge");
            return;
        }        if (inputPath == null)
        {
            _consoleUiService.DisplayError("Input path is required.");
            _consoleUiService.DisplayInfo("Use --help to see usage information.");
            return;
        }

        // Validate input path
        if (!ValidateInputPath(inputPath, out bool isInputFile, out bool isInputDirectory, out string[] excelFiles))
        {
            return;
        }

        // Display selected options
        _consoleUiService.DisplayOptions(options);

        try
        {            // Process the files
            var validationIssues = new List<ValidationIssue>();
            await _mergeService.MergeFilesAsync(excelFiles, outputPath!, options, validationIssues);

            _consoleUiService.WriteLine();
            _consoleUiService.MarkupLineInterpolated($"[green]Successfully merged files.[/] Output saved to: [blue]{outputPath}[/]");

            _consoleUiService.WriteLine();
            _consoleUiService.MarkupLineInterpolated($"Thank you for using [green]{appInfo.ProductName}[/]");
        }
        catch (Exception ex)
        {
            HandleException(ex, options.DebugMode);
        }
    }

    /// <summary>
    /// Gets application version and product information.
    /// </summary>
    /// <returns>A tuple containing product name and version string.</returns>
    public (string ProductName, string VersionString) GetApplicationInfo()
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
    /// Validates the input path and gets the list of Excel files to process.
    /// </summary>
    /// <param name="inputPath">The input path to validate.</param>
    /// <param name="isInputFile">Output whether the input is a file.</param>
    /// <param name="isInputDirectory">Output whether the input is a directory.</param>
    /// <param name="excelFiles">Output array of Excel files to process.</param>
    /// <returns>True if the input path is valid, false otherwise.</returns>
    private bool ValidateInputPath(string inputPath, out bool isInputFile, out bool isInputDirectory, out string[] excelFiles)
    {
        isInputFile = File.Exists(inputPath);
        isInputDirectory = Directory.Exists(inputPath);
        excelFiles = Array.Empty<string>();        if (!isInputFile && !isInputDirectory)
        {
            _consoleUiService.MarkupLineInterpolated($"[red]Error:[/] Input path '[yellow]{inputPath}[/]' does not exist as either a file or directory.");
            _consoleUiService.DisplayInfo("Use --help to see usage information.");
            return false;
        }

        // Get Excel files - either the single file or all Excel files in the directory
        if (isInputFile)
        {            // Check if the file has the correct extension
            if (!Path.GetExtension(inputPath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                _consoleUiService.MarkupLineInterpolated($"[red]Error:[/] Input file '[yellow]{inputPath}[/]' must be an Excel file (.xlsx).");
                return false;
            }
            excelFiles = new[] { inputPath };
            _consoleUiService.MarkupLineInterpolated($"[green]Processing single Excel file: {Path.GetFileName(inputPath)}[/]");
        }
        else // isInputDirectory
        {            excelFiles = Directory.GetFiles(inputPath, "*.xlsx");
            if (excelFiles.Length == 0)
            {
                _consoleUiService.MarkupLineInterpolated($"[yellow]No Excel files found in '{inputPath}'.[/]");
                return false;
            }
            _consoleUiService.MarkupLineInterpolated($"[green]Found {excelFiles.Length} Excel files to process in directory.[/]");
        }

        return true;
    }    /// <summary>
    /// Handles exceptions that occur during application execution.
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
            _consoleUiService.DisplayInfo(ex.StackTrace);
        }
        else
        {
            _consoleUiService.DisplayInfo("[grey]Use --debug flag to see detailed error information.[/]");
        }
    }
}
