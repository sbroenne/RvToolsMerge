//-----------------------------------------------------------------------
// <copyright file="ICommandLineParser.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using RVToolsMerge.Models;

namespace RVToolsMerge.Services.Interfaces;

/// <summary>
/// Interface for command line parsing service.
/// </summary>
public interface ICommandLineParser
{
    /// <summary>
    /// Parses command line arguments into options and input/output paths.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="options">Merge options to populate.</param>
    /// <param name="inputPath">The resolved input path.</param>
    /// <param name="outputPath">The resolved output path.</param>
    /// <returns>True if help was requested, false otherwise.</returns>
    bool ParseArguments(string[] args, MergeOptions options, out string? inputPath, out string? outputPath);
}
