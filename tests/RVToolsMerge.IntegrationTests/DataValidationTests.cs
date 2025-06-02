//-----------------------------------------------------------------------
// <copyright file="DataValidationTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using RVToolsMerge.Exceptions;
using RVToolsMerge.IntegrationTests.Utilities;
using RVToolsMerge.Models;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Tests for data validation including vInfo data row validation and output path validation.
/// </summary>
[Collection("SpectreConsole")]
public class DataValidationTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that files with vInfo sheets containing no data rows fail validation.
    /// </summary>
    [Fact]
    public void ValidateFile_vInfoWithNoDataRows_FailsValidation()
    {
        // Arrange
        var fileName = "vinfo_no_data.xlsx";
        var testFile = TestDataGenerator.CreateFileWithNoDataRows(fileName);
        var validationIssues = new List<ValidationIssue>();

        // Act
        bool isValid = ValidationService.ValidateFile(testFile, false, validationIssues);

        // Assert
        Assert.False(isValid);
        Assert.Single(validationIssues);
        Assert.False(validationIssues[0].Skipped); // It's critical, so cannot be skipped
        Assert.Contains("no data rows", validationIssues[0].ValidationError);
        Assert.Contains("At least one VM entry is required", validationIssues[0].ValidationError);
    }

    /// <summary>
    /// Tests that files with vInfo sheets containing data rows pass validation.
    /// </summary>
    [Fact]
    public void ValidateFile_vInfoWithDataRows_PassesValidation()
    {
        // Arrange
        var fileName = "vinfo_with_data.xlsx";
        var testFile = TestDataGenerator.CreateValidRVToolsFile(fileName, numVMs: 2);
        var validationIssues = new List<ValidationIssue>();

        // Act
        bool isValid = ValidationService.ValidateFile(testFile, false, validationIssues);

        // Assert
        Assert.True(isValid);
        Assert.Empty(validationIssues);
    }

    /// <summary>
    /// Tests that merging files with no data rows throws appropriate exception.
    /// </summary>
    [Fact]
    public async Task MergeFilesAsync_FileWithNoDataRows_ThrowsInvalidFileException()
    {
        // Arrange
        var fileName = "vinfo_no_data.xlsx";
        var testFile = TestDataGenerator.CreateFileWithNoDataRows(fileName);
        var outputPath = Path.Combine(TestOutputDirectory, "merged_output.xlsx");
        var options = new MergeOptions();
        var validationIssues = new List<ValidationIssue>();

        // First validate the file to populate validation issues
        var isValid = ValidationService.ValidateFile(testFile, options.IgnoreMissingOptionalSheets, validationIssues);
        Assert.False(isValid);
        Assert.Single(validationIssues);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidFileException>(
            () => MergeService.MergeFilesAsync([testFile], outputPath, options, new List<ValidationIssue>()));

        Assert.Contains("invalid file found", exception.Message);
    }

    /// <summary>
    /// Tests that merging files with no data rows but skip invalid files enabled succeeds.
    /// </summary>
    [Fact]
    public async Task MergeFilesAsync_FileWithNoDataRowsSkipInvalid_SkipsFileAndContinues()
    {
        // Arrange
        var invalidFileName = "vinfo_no_data.xlsx";
        var validFileName = "vinfo_with_data.xlsx";
        var invalidFile = TestDataGenerator.CreateFileWithNoDataRows(invalidFileName);
        var validFile = TestDataGenerator.CreateValidRVToolsFile(validFileName, numVMs: 2);
        var outputPath = Path.Combine(TestOutputDirectory, "merged_output.xlsx");
        var options = new MergeOptions { SkipInvalidFiles = true };
        var validationIssues = new List<ValidationIssue>();

        // Pre-validate files to set up validation issues as the TestMergeService would
        var invalidFileIssues = new List<ValidationIssue>();
        var isValidInvalid = ValidationService.ValidateFile(invalidFile, options.IgnoreMissingOptionalSheets, invalidFileIssues);
        Assert.False(isValidInvalid);
        validationIssues.AddRange(invalidFileIssues);

        var validFileIssues = new List<ValidationIssue>();
        var isValidValid = ValidationService.ValidateFile(validFile, options.IgnoreMissingOptionalSheets, validFileIssues);
        Assert.True(isValidValid);
        validationIssues.AddRange(validFileIssues);

        // Act
        await MergeService.MergeFilesAsync([invalidFile, validFile], outputPath, options, validationIssues);

        // Assert
        Assert.True(File.Exists(outputPath));
        Assert.Single(invalidFileIssues);
        Assert.Contains("no data rows", invalidFileIssues[0].ValidationError);
    }

    /// <summary>
    /// Tests that validation correctly identifies vInfo sheets with only headers (row 1) as having no data.
    /// </summary>
    [Fact]
    public void ValidateFile_vInfoOnlyHeaders_CorrectlyIdentifiesNoData()
    {
        // Arrange
        var fileName = "vinfo_headers_only.xlsx";
        var testFile = TestDataGenerator.CreateFileWithNoDataRows(fileName);
        var validationIssues = new List<ValidationIssue>();

        // Act
        bool isValid = ValidationService.ValidateFile(testFile, false, validationIssues);

        // Assert
        Assert.False(isValid);
        Assert.Single(validationIssues);
        var issue = validationIssues[0];
        Assert.False(issue.Skipped); // It's critical, so cannot be skipped
        Assert.Equal(fileName, issue.FileName);
        Assert.Contains("vInfo", issue.ValidationError);
        Assert.Contains("no data rows", issue.ValidationError);
    }

    /// <summary>
    /// Tests that files with single data row (headers + 1 data row) pass validation.
    /// </summary>
    [Fact]
    public void ValidateFile_vInfoSingleDataRow_PassesValidation()
    {
        // Arrange
        var fileName = "vinfo_single_row.xlsx";
        var testFile = TestDataGenerator.CreateValidRVToolsFile(fileName, numVMs: 1);
        var validationIssues = new List<ValidationIssue>();

        // Act
        bool isValid = ValidationService.ValidateFile(testFile, false, validationIssues);

        // Assert
        Assert.True(isValid);
        Assert.Empty(validationIssues);
    }

    /// <summary>
    /// Tests that no data validation error occurs when ignore missing sheets is enabled and vInfo has no data.
    /// </summary>
    [Fact]
    public void ValidateFile_vInfoNoDataIgnoreMissing_StillFailsValidation()
    {
        // Arrange
        var fileName = "vinfo_no_data_ignore.xlsx";
        var testFile = TestDataGenerator.CreateFileWithNoDataRows(fileName);
        var validationIssues = new List<ValidationIssue>();

        // Act
        bool isValid = ValidationService.ValidateFile(testFile, true, validationIssues);

        // Assert - vInfo data validation should still fail even when ignoring missing sheets
        Assert.False(isValid);
        Assert.Single(validationIssues);
        Assert.Contains("no data rows", validationIssues[0].ValidationError);
    }
}