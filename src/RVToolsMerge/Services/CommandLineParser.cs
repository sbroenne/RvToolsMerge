//-----------------------------------------------------------------------
// <copyright file="CommandLineParser.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using RVToolsMerge.Models;
using RVToolsMerge.Services.Interfaces;
using System.IO.Abstractions;

namespace RVToolsMerge.Services;

/// <summary>
/// Service for parsing command line arguments.
/// </summary>
public class CommandLineParser : ICommandLineParser
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandLineParser"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    public CommandLineParser(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Parses command line arguments into options and input/output paths.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="options">Merge options to populate.</param>
    /// <param name="inputPath">The resolved input path.</param>
    /// <param name="outputPath">The resolved output path.</param>
    /// <returns>True if help was requested, false otherwise.</returns>
    public bool ParseArguments(string[] args, MergeOptions options, out string? inputPath, out string? outputPath)
    {
        inputPath = null;
        outputPath = null;

        var processedArgs = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h" or "--help" or "/?":
                    return true;
                case "-m" or "--ignore-missing-sheets":
                    options.IgnoreMissingOptionalSheets = true;
                    break;
                case "-i" or "--skip-invalid-files":
                    options.SkipInvalidFiles = true;
                    break;
                case "-a" or "--anonymize":
                    options.AnonymizeData = true;
                    break;
                case "-M" or "--only-mandatory-columns":
                    options.OnlyMandatoryColumns = true;
                    break;
                case "-d" or "--debug":
                    options.DebugMode = true;
                    break;
                case "-s" or "--include-source":
                    options.IncludeSourceFileName = true;
                    break;
                case "-e" or "--skip-empty-values":
                    options.SkipRowsWithEmptyMandatoryValues = true;
                    break;
                default:
                    processedArgs.Add(args[i]);
                    break;
            }
        }        // Get input path (required)
        if (processedArgs.Count > 0)
        {
            inputPath = processedArgs[0];
        }

        // Get output file path
        if (processedArgs.Count > 1)
        {
            outputPath = processedArgs[1];
        }
        else
        {
            outputPath = _fileSystem.Path.Combine(_fileSystem.Directory.GetCurrentDirectory(), "RVTools_Merged.xlsx");
        }

        return false;
    }
}
