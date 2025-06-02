//-----------------------------------------------------------------------
// <copyright file="OutputPathValidationTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using RVToolsMerge.Services;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Tests for output path validation to ensure directories exist before processing.
/// </summary>
public class OutputPathValidationTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that ApplicationRunner validates output path directories exist.
    /// </summary>
    [Fact]
    public async Task ApplicationRunner_OutputPathDirectoryDoesNotExist_DoesNotCreateFile()
    {
        // Arrange
        var validInputFile = TestDataGenerator.CreateValidRVToolsFile("input.xlsx", numVMs: 2);
        var nonExistentPath = "/path/does/not/exist/output.xlsx";
        
        var args = new[] { validInputFile, nonExistentPath };
        var applicationRunner = ServiceProvider.GetRequiredService<ApplicationRunner>();

        // Act
        await applicationRunner.RunAsync(args);

        // Assert - The output file should not be created when directory doesn't exist
        Assert.False(FileSystem.File.Exists(nonExistentPath));
    }

    /// <summary>
    /// Tests that output paths with just filenames (no directory) are valid.
    /// </summary>
    [Fact]
    public async Task ApplicationRunner_OutputPathFilenameOnly_IsValid()
    {
        // Arrange
        var validInputFile = TestDataGenerator.CreateValidRVToolsFile("input.xlsx", numVMs: 2);
        var outputFilename = "output.xlsx";
        
        var args = new[] { validInputFile, outputFilename };
        var applicationRunner = ServiceProvider.GetRequiredService<ApplicationRunner>();

        // Act & Assert - Should not throw exception
        await applicationRunner.RunAsync(args);
        
        // Should create the output file in the current directory
        Assert.True(FileSystem.File.Exists(outputFilename));
    }

    /// <summary>
    /// Tests that output paths with existing directories are valid.
    /// </summary>
    [Fact]
    public async Task ApplicationRunner_OutputPathExistingDirectory_IsValid()
    {
        // Arrange
        var validInputFile = TestDataGenerator.CreateValidRVToolsFile("input.xlsx", numVMs: 2);
        var existingDir = "/tmp/existing_output_dir";
        FileSystem.Directory.CreateDirectory(existingDir);
        var outputPath = FileSystem.Path.Combine(existingDir, "output.xlsx");
        
        var args = new[] { validInputFile, outputPath };
        var applicationRunner = ServiceProvider.GetRequiredService<ApplicationRunner>();

        // Act & Assert - Should not throw exception
        await applicationRunner.RunAsync(args);
        
        // Should create the output file in the specified directory
        Assert.True(FileSystem.File.Exists(outputPath));
    }

    /// <summary>
    /// Tests that default output path (no output specified) works correctly.
    /// </summary>
    [Fact]
    public async Task ApplicationRunner_DefaultOutputPath_IsValid()
    {
        // Arrange
        var validInputFile = TestDataGenerator.CreateValidRVToolsFile("input.xlsx", numVMs: 2);
        
        var args = new[] { validInputFile }; // No output path specified
        var applicationRunner = ServiceProvider.GetRequiredService<ApplicationRunner>();

        // Act & Assert - Should not throw exception
        await applicationRunner.RunAsync(args);
        
        // Should create the default output file
        Assert.True(FileSystem.File.Exists("RVTools_Merged.xlsx"));
    }

    /// <summary>
    /// Tests that creating output directory structure validation through Command Line Parser.
    /// </summary>
    [Fact]
    public void CommandLineParser_NonExistentOutputDirectory_ReturnsCorrectPath()
    {
        // Arrange
        var parser = ServiceProvider.GetRequiredService<CommandLineParser>();
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
}
