//-----------------------------------------------------------------------
// <copyright file="CommandLineTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.DependencyInjection;
using RVToolsMerge.Models;
using RVToolsMerge.Services;
using RVToolsMerge.Services.Interfaces;

namespace RVToolsMerge.IntegrationTests;

[Collection("SpectreConsole")]
/// <summary>
/// Tests for command line argument parsing and validation.
/// </summary>
public class CommandLineTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly ICommandLineParser _commandLineParser;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandLineTests"/> class.
    /// </summary>
    public CommandLineTests()
    {
        // Setup mock file system with test files and directories
        _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/tmp/rvtools_test/test.xlsx", new MockFileData("Excel file content") },
            { "/tmp/rvtools_test/test.txt", new MockFileData("Text file content") },
            { "/tmp/rvtools_test/subdir/test1.xlsx", new MockFileData("Excel file 1") },
            { "/tmp/rvtools_test/subdir/test2.xlsx", new MockFileData("Excel file 2") }
        });

        _fileSystem.AddDirectory("/tmp/rvtools_test/empty_dir");
        _commandLineParser = new CommandLineParser(_fileSystem);
    }

    /// <summary>
    /// Tests parsing of all command line options.
    /// </summary>
    [Fact]
    public void ParseArguments_AllOptions_SetsOptionsCorrectly()
    {
        // Arrange
        var options = new MergeOptions();
        string[] args =
        {
            "-m", "-i", "-a", "-M", "-s", "-e", "-d",
            "/tmp/rvtools_test/test.xlsx",
            "/tmp/output.xlsx"
        };

        // Act
        bool helpRequested = _commandLineParser.ParseArguments(args, options, out string? inputPath, out string? outputPath, out bool versionRequested);

        // Assert
        Assert.False(helpRequested);
        Assert.False(versionRequested);
        Assert.Equal("/tmp/rvtools_test/test.xlsx", inputPath);
        Assert.Equal("/tmp/output.xlsx", outputPath);
        Assert.True(options.IgnoreMissingOptionalSheets);
        Assert.True(options.SkipInvalidFiles);
        Assert.True(options.AnonymizeData);
        Assert.True(options.OnlyMandatoryColumns);
        Assert.True(options.IncludeSourceFileName);
        Assert.True(options.SkipRowsWithEmptyMandatoryValues);
        Assert.True(options.DebugMode);
    }

    /// <summary>
    /// Tests parsing with long-form option names.
    /// </summary>
    [Fact]
    public void ParseArguments_LongOptionNames_SetsOptionsCorrectly()
    {
        // Arrange
        var options = new MergeOptions();
        string[] args =
        {
            "--ignore-missing-sheets",
            "--skip-invalid-files",
            "--anonymize",
            "--only-mandatory-columns",
            "--include-source",
            "--skip-empty-values",
            "--debug",
            "/tmp/rvtools_test/test.xlsx",
            "/tmp/output.xlsx"
        };

        // Act
        bool helpRequested = _commandLineParser.ParseArguments(args, options, out string? inputPath, out string? outputPath, out bool versionRequested);

        // Assert
        Assert.False(helpRequested);
        Assert.Equal("/tmp/rvtools_test/test.xlsx", inputPath);
        Assert.Equal("/tmp/output.xlsx", outputPath);
        Assert.True(options.IgnoreMissingOptionalSheets);
        Assert.True(options.SkipInvalidFiles);
        Assert.True(options.AnonymizeData);
        Assert.True(options.OnlyMandatoryColumns);
        Assert.True(options.IncludeSourceFileName);
        Assert.True(options.SkipRowsWithEmptyMandatoryValues);
        Assert.True(options.DebugMode);
    }

    /// <summary>
    /// Tests that help option returns true.
    /// </summary>
    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    [InlineData("/?")]
    public void ParseArguments_HelpOption_ReturnsTrue(string helpOption)
    {
        // Arrange
        var options = new MergeOptions();
        string[] args = { helpOption };

        // Act
        bool helpRequested = _commandLineParser.ParseArguments(args, options, out string? inputPath, out string? outputPath, out bool versionRequested);

        // Assert
        Assert.True(helpRequested);
        Assert.False(versionRequested);
        Assert.Null(inputPath);
        Assert.Null(outputPath);
    }

    /// <summary>
    /// Tests that version option returns true for versionRequested.
    /// </summary>
    [Theory]
    [InlineData("-v")]
    [InlineData("--version")]
    public void ParseArguments_VersionOption_ReturnsVersionRequested(string versionOption)
    {
        // Arrange
        var options = new MergeOptions();
        string[] args = { versionOption };

        // Act
        bool helpRequested = _commandLineParser.ParseArguments(args, options, out string? inputPath, out string? outputPath, out bool versionRequested);

        // Assert
        Assert.False(helpRequested);
        Assert.True(versionRequested);
        Assert.Null(inputPath);
        Assert.Null(outputPath);
    }

    /// <summary>
    /// Tests that default output path is set when not provided.
    /// </summary>
    [Fact]
    public void ParseArguments_NoOutputPath_SetsDefaultOutputPath()
    {
        // Arrange
        var options = new MergeOptions();
        string[] args = { "/tmp/rvtools_test/test.xlsx" };
        string expectedOutput = _fileSystem.Path.Combine(_fileSystem.Directory.GetCurrentDirectory(), "RVTools_Merged.xlsx");

        // Act
        bool helpRequested = _commandLineParser.ParseArguments(args, options, out string? inputPath, out string? outputPath, out bool versionRequested);

        // Assert
        Assert.False(helpRequested);
        Assert.Equal("/tmp/rvtools_test/test.xlsx", inputPath);
        Assert.Equal(expectedOutput, outputPath);
    }

    /// <summary>
    /// Tests that no input path results in null input path.
    /// </summary>
    [Fact]
    public void ParseArguments_NoInputPath_SetsNullInputPath()
    {
        // Arrange
        var options = new MergeOptions();
        string[] args = { "-a", "-d" }; // Only options, no paths

        // Act
        bool helpRequested = _commandLineParser.ParseArguments(args, options, out string? inputPath, out string? outputPath, out bool versionRequested);

        // Assert
        Assert.False(helpRequested);
        Assert.Null(inputPath);
        Assert.NotNull(outputPath); // Default output path should still be set
    }

    /// <summary>
    /// Tests that options with mixed order are parsed correctly.
    /// </summary>
    [Fact]
    public void ParseArguments_MixedOrderOptions_ParsesCorrectly()
    {
        // Arrange
        var options = new MergeOptions();
        string[] args =
        {
            "/tmp/rvtools_test/test.xlsx",
            "-a",
            "/tmp/output.xlsx",
            "-d"
        };

        // Act
        bool helpRequested = _commandLineParser.ParseArguments(args, options, out string? inputPath, out string? outputPath, out bool versionRequested);

        // Assert
        Assert.False(helpRequested);
        Assert.Equal("/tmp/rvtools_test/test.xlsx", inputPath);
        Assert.Equal("/tmp/output.xlsx", outputPath);
        Assert.True(options.AnonymizeData);
        Assert.True(options.DebugMode);
    }

    /// <summary>
    /// Tests parsing of the MaxVInfoRows option.
    /// </summary>
    [Fact]
    public void ParseArguments_MaxVInfoRows_SetsOptionCorrectly()
    {
        // Arrange
        var options = new MergeOptions();
        string[] args =
        {
            "--max-vinfo-rows",
            "100",
            "/tmp/rvtools_test/test.xlsx",
            "/tmp/output.xlsx"
        };

        // Act
        bool helpRequested = _commandLineParser.ParseArguments(args, options, out string? inputPath, out string? outputPath, out bool versionRequested);

        // Assert
        Assert.False(helpRequested);
        Assert.Equal("/tmp/rvtools_test/test.xlsx", inputPath);
        Assert.Equal("/tmp/output.xlsx", outputPath);
        Assert.Equal(100, options.MaxVInfoRows);
    }

    /// <summary>
    /// Tests that invalid MaxVInfoRows values are ignored (for backward compatibility).
    /// </summary>
    [Theory]
    [InlineData("--max-vinfo-rows", "abc")] // Invalid number
    [InlineData("--max-vinfo-rows", "-10")] // Negative number
    [InlineData("--max-vinfo-rows", "0")] // Zero
    public void ParseArguments_InvalidMaxVInfoRows_IgnoresOption(string option, string value)
    {
        // Arrange
        var options = new MergeOptions();
        string[] args =
        {
            option,
            value,
            "/tmp/rvtools_test/test.xlsx"
        };

        // Act
        bool helpRequested = _commandLineParser.ParseArguments(args, options, out string? inputPath, out string? outputPath, out bool versionRequested);

        // Assert
        Assert.False(helpRequested);
        Assert.Equal("/tmp/rvtools_test/test.xlsx", inputPath);
        Assert.Null(options.MaxVInfoRows); // Should remain null (default)
    }

    /// <summary>
    /// Tests validation of input path in ApplicationRunner.
    /// </summary>
    [Fact]
    public void ValidateInputPath_ValidFile_ReturnsFileList()
    {
        // This test requires a mock of ApplicationRunner's ValidateInputPath method
        // Since it's private, we'll test it indirectly through a helper method

        // Arrange
        var mockConsoleService = new Utilities.MockConsoleService();
        var services = new ServiceCollection();
        services.AddSingleton<IConsoleService>(mockConsoleService);
        services.AddTransient<ConsoleUIService>();
        var serviceProvider = services.BuildServiceProvider();

        var testRunner = new TestApplicationRunner(
            serviceProvider.GetRequiredService<ConsoleUIService>(),
            null!, // MergeService not needed for this test
            _commandLineParser,
            _fileSystem);

        // Act
        bool isValid = testRunner.TestValidateInputPath(
            "/tmp/rvtools_test/test.xlsx",
            out bool isInputFile,
            out bool isInputDirectory,
            out string[] excelFiles);

        // Assert
        Assert.True(isValid);
        Assert.True(isInputFile);
        Assert.False(isInputDirectory);
        Assert.Single(excelFiles);
        Assert.Equal("/tmp/rvtools_test/test.xlsx", excelFiles[0]);
    }


    /// <summary>
    /// Tests validation of empty directory in ApplicationRunner.
    /// </summary>
    [Fact]
    public void ValidateInputPath_EmptyDirectory_ReturnsFalse()
    {
        // Arrange
        var mockConsoleService = new Utilities.MockConsoleService();
        var services = new ServiceCollection();
        services.AddSingleton<IConsoleService>(mockConsoleService);
        services.AddTransient<ConsoleUIService>();
        var serviceProvider = services.BuildServiceProvider();

        var testRunner = new TestApplicationRunner(
            serviceProvider.GetRequiredService<ConsoleUIService>(),
            null!, // MergeService not needed for this test
            _commandLineParser,
            _fileSystem);

        // Act
        bool isValid = testRunner.TestValidateInputPath(
            "/tmp/rvtools_test/empty_dir",
            out bool isInputFile,
            out bool isInputDirectory,
            out string[] excelFiles);

        // Assert
        Assert.False(isValid);
        Assert.False(isInputFile);
        Assert.True(isInputDirectory);
        Assert.Empty(excelFiles);
    }

    /// <summary>
    /// Tests validation of non-existent path in ApplicationRunner.
    /// </summary>
    [Fact]
    public void ValidateInputPath_NonExistentPath_ReturnsFalse()
    {
        // Arrange
        var mockConsoleService = new Utilities.MockConsoleService();
        var services = new ServiceCollection();
        services.AddSingleton<IConsoleService>(mockConsoleService);
        services.AddTransient<ConsoleUIService>();
        var serviceProvider = services.BuildServiceProvider();

        var testRunner = new TestApplicationRunner(
            serviceProvider.GetRequiredService<ConsoleUIService>(),
            null!, // MergeService not needed for this test
            _commandLineParser,
            _fileSystem);

        // Act
        bool isValid = testRunner.TestValidateInputPath(
            "/tmp/path/does/not/exist",
            out bool isInputFile,
            out bool isInputDirectory,
            out string[] excelFiles);

        // Assert
        Assert.False(isValid);
        Assert.False(isInputFile);
        Assert.False(isInputDirectory);
        Assert.Empty(excelFiles);
    }

    /// <summary>
    /// Tests validation of non-Excel file in ApplicationRunner.
    /// </summary>
    [Fact]
    public void ValidateInputPath_NonExcelFile_ReturnsFalse()
    {
        // Arrange
        var mockConsoleService = new Utilities.MockConsoleService();
        var services = new ServiceCollection();
        services.AddSingleton<IConsoleService>(mockConsoleService);
        services.AddTransient<ConsoleUIService>();
        var serviceProvider = services.BuildServiceProvider();

        var testRunner = new TestApplicationRunner(
            serviceProvider.GetRequiredService<ConsoleUIService>(),
            null!, // MergeService not needed for this test
            _commandLineParser,
            _fileSystem);

        // Act
        bool isValid = testRunner.TestValidateInputPath(
            "/tmp/rvtools_test/test.txt",
            out bool isInputFile,
            out bool isInputDirectory,
            out string[] excelFiles);

        // Assert
        Assert.False(isValid);
        Assert.True(isInputFile);
        Assert.False(isInputDirectory);
        Assert.Empty(excelFiles);
    }
}

/// <summary>
/// Helper class to expose ApplicationRunner's private methods for testing.
/// </summary>
public class TestApplicationRunner : RVToolsMerge.ApplicationRunner
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestApplicationRunner"/> class.
    /// </summary>
    public TestApplicationRunner(
        ConsoleUIService consoleUiService,
        IMergeService mergeService,
        ICommandLineParser commandLineParser,
        System.IO.Abstractions.IFileSystem fileSystem)
        : base(consoleUiService, mergeService, commandLineParser, fileSystem)
    {
    }

    /// <summary>
    /// Wrapper for private ValidateInputPath method.
    /// </summary>
    public bool TestValidateInputPath(string inputPath, out bool isInputFile, out bool isInputDirectory, out string[] excelFiles)
    {
        // Use reflection to call the private method
        var method = typeof(RVToolsMerge.ApplicationRunner).GetMethod(
            "ValidateInputPath",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        isInputFile = false;
        isInputDirectory = false;
        excelFiles = Array.Empty<string>();

        // Create parameters for the private method
        var parameters = new object[] { inputPath, isInputFile, isInputDirectory, excelFiles };
        var result = (bool)method!.Invoke(this, parameters)!;

        // Extract out parameters
        isInputFile = (bool)parameters[1];
        isInputDirectory = (bool)parameters[2];
        excelFiles = (string[])parameters[3];

        return result;
    }
}
