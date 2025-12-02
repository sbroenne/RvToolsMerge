//-----------------------------------------------------------------------
// <copyright file="CommandLineTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.DependencyInjection;
using RVToolsMerge.Commands;
using RVToolsMerge.Infrastructure;
using RVToolsMerge.Models;
using RVToolsMerge.Services;
using RVToolsMerge.Services.Interfaces;
using Spectre.Console.Cli;

namespace RVToolsMerge.IntegrationTests;

[Collection("SpectreConsole")]
/// <summary>
/// Tests for command line argument parsing and validation using Spectre.Console.Cli.
/// </summary>
public class CommandLineTests
{
    private readonly MockFileSystem _fileSystem;

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
    }

    /// <summary>
    /// Creates a CommandApp for testing with the mock file system.
    /// Uses CommandApp&lt;TCommand&gt; pattern matching Program.cs for consistency.
    /// </summary>
    private CommandApp<MergeCommand> CreateTestApp()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem>(_fileSystem);
        services.AddSingleton<IConsoleService, Utilities.MockConsoleService>();
        services.AddTransient<ConsoleUIService>();
        services.AddSingleton<IExcelService, ExcelService>();
        services.AddSingleton<IAnonymizationService, AnonymizationService>();
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddSingleton<IMergeService, MergeService>();

        var registrar = new TypeRegistrar(services);
        return new CommandApp<MergeCommand>(registrar);
    }

    /// <summary>
    /// Tests parsing of help option.
    /// </summary>
    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    public async Task ParseArguments_HelpOption_ReturnsZero(string helpOption)
    {
        // Arrange
        var app = CreateTestApp();
        string[] args = { helpOption };

        // Act
        int result = await app.RunAsync(args);

        // Assert - Help should return 0
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Tests parsing of version option.
    /// Note: Spectre.Console.Cli returns -1 for version display (not an error, just convention).
    /// </summary>
    [Theory]
    [InlineData("--version")]
    public async Task ParseArguments_VersionOption_ReturnsVersionCode(string versionOption)
    {
        // Arrange
        var app = CreateTestApp();
        string[] args = { versionOption };

        // Act
        int result = await app.RunAsync(args);

        // Assert - Version display returns -1 in Spectre.Console.Cli (by convention, not an error)
        Assert.Equal(-1, result);
    }

    /// <summary>
    /// Tests that missing input path shows error.
    /// </summary>
    [Fact]
    public async Task ParseArguments_NoInputPath_ReturnsError()
    {
        // Arrange
        var app = CreateTestApp();
        string[] args = Array.Empty<string>();

        // Act
        int result = await app.RunAsync(args);

        // Assert - Missing required argument should return error
        Assert.NotEqual(0, result);
    }

    /// <summary>
    /// Tests MergeCommand directly with all options.
    /// </summary>
    [Fact]
    public void MergeCommandSettings_AllOptions_MapsCorrectly()
    {
        // Arrange
        var settings = new MergeCommandSettings
        {
            InputPath = "/tmp/rvtools_test/test.xlsx",
            OutputPath = "/tmp/output.xlsx",
            IgnoreMissingOptionalSheets = true,
            SkipInvalidFiles = true,
            AnonymizeData = true,
            OnlyMandatoryColumns = true,
            IncludeSourceFileName = true,
            SkipRowsWithEmptyMandatoryValues = true,
            DebugMode = true,
            EnableAzureMigrateValidation = true,
            MaxVInfoRows = 100
        };

        // Act - Convert to MergeOptions using reflection to access private method
        var commandType = typeof(MergeCommand);
        var method = commandType.GetMethod(
            "ConvertSettingsToOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var options = (MergeOptions)method!.Invoke(null, new object[] { settings })!;

        // Assert
        Assert.Equal(settings.IgnoreMissingOptionalSheets, options.IgnoreMissingOptionalSheets);
        Assert.Equal(settings.SkipInvalidFiles, options.SkipInvalidFiles);
        Assert.Equal(settings.AnonymizeData, options.AnonymizeData);
        Assert.Equal(settings.OnlyMandatoryColumns, options.OnlyMandatoryColumns);
        Assert.Equal(settings.IncludeSourceFileName, options.IncludeSourceFileName);
        Assert.Equal(settings.SkipRowsWithEmptyMandatoryValues, options.SkipRowsWithEmptyMandatoryValues);
        Assert.Equal(settings.DebugMode, options.DebugMode);
        Assert.Equal(settings.EnableAzureMigrateValidation, options.EnableAzureMigrateValidation);
        Assert.Equal(settings.MaxVInfoRows, options.MaxVInfoRows);
    }

    /// <summary>
    /// Tests validation of input path in MergeCommand.
    /// </summary>
    [Fact]
    public void ValidateInputPath_ValidFile_ReturnsFileList()
    {
        // Arrange
        var mockConsoleService = new Utilities.MockConsoleService();
        var services = new ServiceCollection();
        services.AddSingleton<IConsoleService>(mockConsoleService);
        services.AddTransient<ConsoleUIService>();
        var serviceProvider = services.BuildServiceProvider();

        var testCommand = new TestMergeCommand(
            serviceProvider.GetRequiredService<ConsoleUIService>(),
            null!, // MergeService not needed for this test
            _fileSystem);

        // Act
        bool isValid = testCommand.TestValidateInputPath(
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
    /// Tests validation of directory with files.
    /// </summary>
    [Fact]
    public void ValidateInputPath_DirectoryWithFiles_ReturnsFileList()
    {
        // Arrange
        var mockConsoleService = new Utilities.MockConsoleService();
        var services = new ServiceCollection();
        services.AddSingleton<IConsoleService>(mockConsoleService);
        services.AddTransient<ConsoleUIService>();
        var serviceProvider = services.BuildServiceProvider();

        var testCommand = new TestMergeCommand(
            serviceProvider.GetRequiredService<ConsoleUIService>(),
            null!, // MergeService not needed for this test
            _fileSystem);

        // Act
        bool isValid = testCommand.TestValidateInputPath(
            "/tmp/rvtools_test/subdir",
            out bool isInputFile,
            out bool isInputDirectory,
            out string[] excelFiles);

        // Assert
        Assert.True(isValid);
        Assert.False(isInputFile);
        Assert.True(isInputDirectory);
        Assert.Equal(2, excelFiles.Length);
    }

    /// <summary>
    /// Tests validation of empty directory in MergeCommand.
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

        var testCommand = new TestMergeCommand(
            serviceProvider.GetRequiredService<ConsoleUIService>(),
            null!, // MergeService not needed for this test
            _fileSystem);

        // Act
        bool isValid = testCommand.TestValidateInputPath(
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
    /// Tests validation of non-existent path in MergeCommand.
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

        var testCommand = new TestMergeCommand(
            serviceProvider.GetRequiredService<ConsoleUIService>(),
            null!, // MergeService not needed for this test
            _fileSystem);

        // Act
        bool isValid = testCommand.TestValidateInputPath(
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
    /// Tests validation of non-Excel file in MergeCommand.
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

        var testCommand = new TestMergeCommand(
            serviceProvider.GetRequiredService<ConsoleUIService>(),
            null!, // MergeService not needed for this test
            _fileSystem);

        // Act
        bool isValid = testCommand.TestValidateInputPath(
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

    /// <summary>
    /// Tests path traversal validation.
    /// </summary>
    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32")]
    [InlineData("test/../../etc/passwd")]
    public void ValidateInputPath_PathTraversal_ReturnsFalse(string maliciousPath)
    {
        // Arrange
        var mockConsoleService = new Utilities.MockConsoleService();
        var services = new ServiceCollection();
        services.AddSingleton<IConsoleService>(mockConsoleService);
        services.AddTransient<ConsoleUIService>();
        var serviceProvider = services.BuildServiceProvider();

        var testCommand = new TestMergeCommand(
            serviceProvider.GetRequiredService<ConsoleUIService>(),
            null!, // MergeService not needed for this test
            _fileSystem);

        // Act
        bool isValid = testCommand.TestValidateInputPath(
            maliciousPath,
            out bool isInputFile,
            out bool isInputDirectory,
            out string[] excelFiles);

        // Assert
        Assert.False(isValid);
    }
}

/// <summary>
/// Helper class to expose MergeCommand's private methods for testing.
/// </summary>
public class TestMergeCommand : MergeCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestMergeCommand"/> class.
    /// </summary>
    public TestMergeCommand(
        ConsoleUIService consoleUiService,
        IMergeService mergeService,
        IFileSystem fileSystem)
        : base(consoleUiService, mergeService, fileSystem)
    {
    }

    /// <summary>
    /// Wrapper for private ValidateInputPath method.
    /// </summary>
    public bool TestValidateInputPath(string inputPath, out bool isInputFile, out bool isInputDirectory, out string[] excelFiles)
    {
        // Use reflection to call the private method
        var method = typeof(MergeCommand).GetMethod(
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
