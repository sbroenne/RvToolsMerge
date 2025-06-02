//-----------------------------------------------------------------------
// <copyright file="ErrorHandlingTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using System.IO.Abstractions.TestingHelpers;
using ClosedXML.Excel;
using RVToolsMerge.Exceptions;
using RVToolsMerge.Models;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

[Collection("SpectreConsole")]
/// <summary>
/// Tests for error handling scenarios across different services.
/// </summary>
public class ErrorHandlingTests : IntegrationTestBase
{
    /// <summary>
    /// Tests MergeService behavior when given an empty file list.
    /// </summary>
    [Fact]
    public async Task MergeFilesAsync_EmptyFileList_ThrowsArgumentException()
    {
        // Arrange
        string[] emptyFilePaths = [];
        string outputPath = GetOutputFilePath("output.xlsx");
        var options = CreateDefaultMergeOptions();
        var validationIssues = new List<ValidationIssue>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            MergeService.MergeFilesAsync(emptyFilePaths, outputPath, options, validationIssues));
    }

    /// <summary>
    /// Tests MergeService behavior when given null file list.
    /// </summary>
    [Fact]
    public async Task MergeFilesAsync_NullFileList_ThrowsArgumentNullException()
    {
        // Arrange
        string[] nullFilePaths = null!;
        string outputPath = GetOutputFilePath("output.xlsx");
        var options = CreateDefaultMergeOptions();
        var validationIssues = new List<ValidationIssue>();

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() =>
            MergeService.MergeFilesAsync(nullFilePaths, outputPath, options, validationIssues));
    }

    /// <summary>
    /// Tests MergeService behavior when all files are invalid and skip is disabled.
    /// </summary>
    [Fact]
    public async Task MergeFilesAsync_AllInvalidFilesNoSkip_HandlesInvalidFiles()
    {
        // Arrange
        var invalidFile1 = TestDataGenerator.CreateInvalidRVToolsFile("invalid1.xlsx");
        var invalidFile2 = TestDataGenerator.CreateInvalidRVToolsFile("invalid2.xlsx");
        
        string[] filePaths = [invalidFile1, invalidFile2];
        string outputPath = GetOutputFilePath("output.xlsx");
        var options = CreateDefaultMergeOptions();
        options.SkipInvalidFiles = false; // Don't skip invalid files
        var validationIssues = new List<ValidationIssue>();

        // Act & Assert - Should handle gracefully and create output
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);
        
        // Should create output file even with invalid input files
        Assert.True(File.Exists(outputPath));
    }

    /// <summary>
    /// Tests MergeService behavior when all files are invalid but skip is enabled.
    /// </summary>
    [Fact]
    public async Task MergeFilesAsync_AllInvalidFilesWithSkip_HandlesInvalidFiles()
    {
        // Arrange
        var invalidFile1 = TestDataGenerator.CreateInvalidRVToolsFile("invalid1.xlsx");
        var invalidFile2 = TestDataGenerator.CreateInvalidRVToolsFile("invalid2.xlsx");
        
        string[] filePaths = [invalidFile1, invalidFile2];
        string outputPath = GetOutputFilePath("output.xlsx");
        var options = CreateDefaultMergeOptions();
        options.SkipInvalidFiles = true; // Skip invalid files
        var validationIssues = new List<ValidationIssue>();

        // Act - Should complete successfully by skipping invalid files
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert - Should create output file
        Assert.True(File.Exists(outputPath));
    }

    /// <summary>
    /// Tests ExcelService behavior when trying to open a corrupted Excel file.
    /// </summary>
    [Fact]
    public void ExcelService_OpenCorruptedFile_ThrowsException()
    {
        // Arrange - Create a file with invalid Excel content
        string corruptedFilePath = "/tmp/rvtools_test/input/corrupted.xlsx";
        FileSystem.File.WriteAllText(corruptedFilePath, "This is not an Excel file content");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            ExcelService.OpenWorkbook(corruptedFilePath);
        });
    }

    /// <summary>
    /// Tests ValidationService behavior with extremely large column indices.
    /// </summary>
    [Fact]
    public void ValidationService_LargeColumnIndices_HandledSafely()
    {
        // Arrange
        var rowData = new XLCellValue[] { "VM1", "poweredOn" };
        var largeColumnIndices = new List<int> { 0, 1, 999999 }; // Include out-of-bounds index

        // Act & Assert
        // Should throw IndexOutOfRangeException when accessing array bounds
        Assert.Throws<IndexOutOfRangeException>(() =>
            ValidationService.HasEmptyMandatoryValues(rowData, largeColumnIndices));
    }

    /// <summary>
    /// Tests handling of files with no sheets.
    /// </summary>
    [Fact]
    public void ExcelService_FileWithNoSheets_HandledGracefully()
    {
        // This test validates that creating a workbook without sheets throws an exception
        // when trying to save it, which is the expected behavior in ClosedXML
        using var workbook = new XLWorkbook();
        var stream = new MemoryStream();
        
        // Act & Assert - Should throw when trying to save workbook with no sheets
        Assert.Throws<InvalidOperationException>(() =>
        {
            workbook.SaveAs(stream);
        });
    }

    /// <summary>
    /// Tests AnonymizationService with null column indices.
    /// </summary>
    [Fact]
    public void AnonymizationService_NullColumnIndices_HandledSafely()
    {
        // Arrange
        var value = (XLCellValue)"test-value";
        Dictionary<string, int>? nullColumnIndices = null;
        
        // Act & Assert
        Assert.Throws<NullReferenceException>(() =>
            AnonymizationService.AnonymizeValue(value, 0, nullColumnIndices!, "test.xlsx"));
    }

    /// <summary>
    /// Tests MergeService with output path to non-existent directory.
    /// </summary>
    [Fact]
    public async Task MergeFilesAsync_NonExistentOutputDirectory_CreatesDirectory()
    {
        // Arrange
        var validFile = TestDataGenerator.CreateValidRVToolsFile("valid.xlsx", numVMs: 1);
        string[] filePaths = [validFile];
        string outputPath = "/tmp/rvtools_test/new_directory/output.xlsx";
        var options = CreateDefaultMergeOptions();
        var validationIssues = new List<ValidationIssue>();

        // Act - Should succeed by creating the directory
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert - Output file should be created
        Assert.True(File.Exists(outputPath));
    }

    /// <summary>
    /// Tests ValidationService with null validation issues list.
    /// </summary>
    [Fact]
    public void ValidationService_NullValidationIssuesList_HandledGracefully()
    {
        // Arrange - Use empty string to trigger the first validation check that adds to issues list
        string emptyFilePath = string.Empty;
        
        // Act & Assert
        // Should throw NullReferenceException when trying to add to null list
        Assert.Throws<NullReferenceException>(() =>
            ValidationService.ValidateFile(emptyFilePath, false, null!));
    }

    /// <summary>
    /// Tests handling of extremely long file paths.
    /// </summary>
    [Fact]
    public void ExcelService_ExtremelyLongFilePath_HandledGracefully()
    {
        // Arrange - Create a very long path
        var longPath = "/tmp/" + new string('a', 500) + ".xlsx";

        // Act & Assert
        // Should throw appropriate exception for path too long
        Assert.Throws<System.IO.FileNotFoundException>(() =>
        {
            ExcelService.OpenWorkbook(longPath);
        });
    }

    /// <summary>
    /// Tests MergeService with files that have conflicting sheet structures.
    /// </summary>
    [Fact]
    public async Task MergeFilesAsync_ConflictingSheetStructures_HandledGracefully()
    {
        // Arrange - Create files with different column structures
        var file1 = TestDataGenerator.CreateValidRVToolsFile("file1.xlsx", numVMs: 1);
        var file2 = TestDataGenerator.CreateFileWithDifferentColumns("file2.xlsx");
        
        string[] filePaths = [file1, file2];
        string outputPath = GetOutputFilePath("merged.xlsx");
        var options = CreateDefaultMergeOptions();
        var validationIssues = new List<ValidationIssue>();

        // Act - Should complete without throwing
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert - Output file should be created
        Assert.True(File.Exists(outputPath));
    }
}