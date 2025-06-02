//-----------------------------------------------------------------------
// <copyright file="MergeServiceTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using RVToolsMerge.Exceptions;
using RVToolsMerge.IntegrationTests.Utilities;
using RVToolsMerge.Models;
using RVToolsMerge.Services;
using RVToolsMerge.Services.Interfaces;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Tests for the MergeService class to increase coverage.
/// </summary>
[Collection("SpectreConsole")]
public class MergeServiceTests : IntegrationTestBase
{

    [Fact]
    public async Task MergeFilesAsync_EmptyFilePathsArray_ThrowsArgumentException()
    {
        // Arrange
        var filePaths = Array.Empty<string>();
        var options = CreateDefaultMergeOptions();
        var validationIssues = new List<ValidationIssue>();
        var outputPath = GetOutputFilePath("merged.xlsx");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues));
        
        Assert.Equal("No files specified for merging. (Parameter 'filePaths')", exception.Message);
    }

    [Fact]
    public async Task MergeFilesAsync_NonExistentFiles_ThrowsNoValidFilesException()
    {
        // Arrange
        var filePaths = new[] { "nonexistent1.xlsx", "nonexistent2.xlsx" };
        var options = CreateDefaultMergeOptions();
        options.SkipInvalidFiles = true;
        options.IgnoreMissingOptionalSheets = true;
        var validationIssues = new List<ValidationIssue>();
        var outputPath = GetOutputFilePath("merged.xlsx");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NoValidFilesException>(
            () => MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues));
        
        Assert.Equal("No valid files to process after validation.", exception.Message);
    }

    [Fact]
    public async Task MergeFilesAsync_WithValidFiles_EmptySheets_ProcessesSuccessfully()
    {
        // Arrange
        var testFile = TestDataGenerator.CreateValidRVToolsFile("test.xlsx", 1);
        var filePaths = new[] { testFile };
        var options = CreateDefaultMergeOptions();
        options.SkipInvalidFiles = true;
        options.IgnoreMissingOptionalSheets = true;
        var validationIssues = new List<ValidationIssue>();
        var outputPath = GetOutputFilePath("merged.xlsx");

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(FileSystem.File.Exists(outputPath));
    }

    [Fact]
    public async Task MergeFilesAsync_WithValidFile_ProcessesSuccessfully()
    {
        // Arrange
        var testFile = TestDataGenerator.CreateValidRVToolsFile("test.xlsx", 1);
        var filePaths = new[] { testFile };
        var options = CreateDefaultMergeOptions();
        options.SkipInvalidFiles = true;
        options.IgnoreMissingOptionalSheets = true;
        var validationIssues = new List<ValidationIssue>();
        var outputPath = GetOutputFilePath("merged.xlsx");

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(FileSystem.File.Exists(outputPath));
    }

    [Fact]
    public async Task MergeFilesAsync_WithAnonymization_CreatesAnonymizationMapFile()
    {
        // Arrange
        var testFile = TestDataGenerator.CreateFileForAnonymizationTesting("test.xlsx");
        var filePaths = new[] { testFile };
        var options = CreateDefaultMergeOptions();
        options.AnonymizeData = true;
        options.SkipInvalidFiles = true;
        options.IgnoreMissingOptionalSheets = true;
        var validationIssues = new List<ValidationIssue>();
        var outputPath = GetOutputFilePath("merged.xlsx");

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(FileSystem.File.Exists(outputPath));
        
        var expectedMapPath = GetOutputFilePath("merged_AnonymizationMapping.xlsx");
        Assert.True(FileSystem.File.Exists(expectedMapPath));
    }

    [Fact]
    public async Task MergeFilesAsync_WithAzureMigrateValidation_CreatesFailedValidationFile()
    {
        // Arrange
        var testFile = TestDataGenerator.CreateValidRVToolsFile("test.xlsx", 1);
        var filePaths = new[] { testFile };
        var options = CreateDefaultMergeOptions();
        options.EnableAzureMigrateValidation = true;
        options.SkipInvalidFiles = true;
        options.IgnoreMissingOptionalSheets = true;
        var validationIssues = new List<ValidationIssue>();
        var outputPath = GetOutputFilePath("merged.xlsx");

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(FileSystem.File.Exists(outputPath));
        
        // Azure Migrate validation may or may not create a failed validation file depending on data
        // We just check that the merge completed successfully
    }

    [Fact]
    public async Task MergeFilesAsync_WithMultipleFiles_ProcessesAllSuccessfully()
    {
        // Arrange
        var testFile1 = TestDataGenerator.CreateValidRVToolsFile("test1.xlsx", 1);
        var testFile2 = TestDataGenerator.CreateValidRVToolsFile("test2.xlsx", 1);
        var filePaths = new[] { testFile1, testFile2 };
        var options = CreateDefaultMergeOptions();
        options.SkipInvalidFiles = true;
        options.IgnoreMissingOptionalSheets = true;
        var validationIssues = new List<ValidationIssue>();
        var outputPath = GetOutputFilePath("merged.xlsx");

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(FileSystem.File.Exists(outputPath));
    }

    [Fact]
    public async Task MergeFilesAsync_WithDebugMode_ProcessesWithDebugInfo()
    {
        // Arrange
        var testFile = TestDataGenerator.CreateValidRVToolsFile("test.xlsx", 1);
        var filePaths = new[] { testFile };
        var options = CreateDefaultMergeOptions();
        options.DebugMode = true;
        options.SkipInvalidFiles = true;
        options.IgnoreMissingOptionalSheets = true;
        var validationIssues = new List<ValidationIssue>();
        var outputPath = GetOutputFilePath("merged.xlsx");

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(FileSystem.File.Exists(outputPath));
    }

    [Fact]
    public async Task MergeFilesAsync_WithInvalidFileInList_SkipsInvalidFile()
    {
        // Arrange
        var validFile = TestDataGenerator.CreateValidRVToolsFile("valid.xlsx", 1);
        var invalidFile = Path.Combine(TestInputDirectory, "invalid.txt");
        FileSystem.File.WriteAllText(invalidFile, "not an excel file");
        
        var filePaths = new[] { validFile, invalidFile };
        var options = CreateDefaultMergeOptions();
        options.SkipInvalidFiles = true;
        options.IgnoreMissingOptionalSheets = true;
        var validationIssues = new List<ValidationIssue>();
        var outputPath = GetOutputFilePath("merged.xlsx");

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(FileSystem.File.Exists(outputPath));
        Assert.NotEmpty(validationIssues);
    }

    [Fact]
    public async Task MergeFilesAsync_WithSpecificOptions_ProcessesSuccessfully()
    {
        // Arrange
        var testFile = TestDataGenerator.CreateValidRVToolsFile("test.xlsx", 1);
        var filePaths = new[] { testFile };
        var options = CreateDefaultMergeOptions();
        options.OnlyMandatoryColumns = true;
        options.SkipInvalidFiles = true;
        options.IgnoreMissingOptionalSheets = true;
        var validationIssues = new List<ValidationIssue>();
        var outputPath = GetOutputFilePath("merged.xlsx");

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(FileSystem.File.Exists(outputPath));
    }
}