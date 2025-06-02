//-----------------------------------------------------------------------
// <copyright file="OutputPathValidationTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using RVToolsMerge.Services;
using RVToolsMerge.Services.Interfaces;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Tests for output path validation to ensure directories exist before processing.
/// </summary>
public class OutputPathValidationTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that CommandLineParser correctly parses output paths.
    /// </summary>
    [Fact]
    public void CommandLineParser_NonExistentOutputDirectory_ReturnsCorrectPath()
    {
        // Arrange
        var parser = ServiceProvider.GetRequiredService<ICommandLineParser>();
        var inputFile = TestDataGenerator.CreateValidRVToolsFile("input.xlsx", numVMs: 1);
        var outputPath = "/nonexistent/directory/output.xlsx";
        var args = new[] { inputFile, outputPath };
        var options = new RVToolsMerge.Models.MergeOptions();

        // Act
        bool helpRequested = parser.ParseArguments(args, options, out string? parsedInput, out string? parsedOutput);

        // Assert
        Assert.False(helpRequested);
        Assert.Equal(inputFile, parsedInput);
        Assert.Equal(outputPath, parsedOutput);
    }

    /// <summary>
    /// Tests that CommandLineParser correctly handles default output paths.
    /// </summary>
    [Fact]
    public void CommandLineParser_DefaultOutputPath_ReturnsCorrectPath()
    {
        // Arrange
        var parser = ServiceProvider.GetRequiredService<ICommandLineParser>();
        var inputFile = TestDataGenerator.CreateValidRVToolsFile("input.xlsx", numVMs: 1);
        var args = new[] { inputFile };
        var options = new RVToolsMerge.Models.MergeOptions();

        // Act
        bool helpRequested = parser.ParseArguments(args, options, out string? parsedInput, out string? parsedOutput);

        // Assert
        Assert.False(helpRequested);
        Assert.Equal(inputFile, parsedInput);
        Assert.EndsWith("RVTools_Merged.xlsx", parsedOutput); // Should end with the expected filename
    }

    /// <summary>
    /// Tests that CommandLineParser correctly handles filename-only output paths.
    /// </summary>
    [Fact]
    public void CommandLineParser_FilenameOnlyOutput_ReturnsCorrectPath()
    {
        // Arrange
        var parser = ServiceProvider.GetRequiredService<ICommandLineParser>();
        var inputFile = TestDataGenerator.CreateValidRVToolsFile("input.xlsx", numVMs: 1);
        var outputPath = "output.xlsx";
        var args = new[] { inputFile, outputPath };
        var options = new RVToolsMerge.Models.MergeOptions();

        // Act
        bool helpRequested = parser.ParseArguments(args, options, out string? parsedInput, out string? parsedOutput);

        // Assert
        Assert.False(helpRequested);
        Assert.Equal(inputFile, parsedInput);
        Assert.Equal(outputPath, parsedOutput);
    }

    /// <summary>
    /// Tests that CommandLineParser handles existing directory output paths.
    /// </summary>
    [Fact]
    public void CommandLineParser_ExistingDirectoryOutput_ReturnsCorrectPath()
    {
        // Arrange
        var parser = ServiceProvider.GetRequiredService<ICommandLineParser>();
        var inputFile = TestDataGenerator.CreateValidRVToolsFile("input.xlsx", numVMs: 1);
        var existingDir = Path.Combine(Path.GetTempPath(), "existing_dir");
        Directory.CreateDirectory(existingDir);
        var outputPath = Path.Combine(existingDir, "output.xlsx");
        var args = new[] { inputFile, outputPath };
        var options = new RVToolsMerge.Models.MergeOptions();

        // Act
        bool helpRequested = parser.ParseArguments(args, options, out string? parsedInput, out string? parsedOutput);

        // Assert
        Assert.False(helpRequested);
        Assert.Equal(inputFile, parsedInput);
        Assert.Equal(outputPath, parsedOutput);
    }
}
