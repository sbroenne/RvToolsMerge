//-----------------------------------------------------------------------
// <copyright file="ValidationTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using ClosedXML.Excel;
using RVToolsMerge.Models;
using RVToolsMerge.Exceptions;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Tests for the validation functionality.
/// </summary>
public class ValidationTests : IntegrationTestBase
{
    /// <summary>
    /// Tests validation with missing required sheets.
    /// </summary>
    [Fact]
    public void ValidateFile_WithMissingRequiredSheet_DetectsIssue()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        // Create a workbook with a dummy sheet (not vInfo)
        workbook.AddWorksheet("DummySheet");
        var filePath = FileSystem.Path.Combine(TestInputDirectory, "missing_sheet.xlsx");
        workbook.SaveAs(filePath);
        
        var issues = new List<ValidationIssue>();
        
        // Act
        bool isValid = ValidationService.ValidateFile(filePath, false, issues);
        
        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(issues);
        Assert.Contains(issues, issue => issue.Skipped && issue.ValidationError.Contains("vInfo"));
    }
    
    /// <summary>
    /// Tests validation with missing mandatory columns.
    /// </summary>
    [Fact]
    public void ValidateFile_WithMissingMandatoryColumns_DetectsIssue()
    {
        // Arrange
        var filePath = TestDataGenerator.CreateInvalidRVToolsFile("missing_columns.xlsx");
        var issues = new List<ValidationIssue>();
        
        // Act
        bool isValid = ValidationService.ValidateFile(filePath, false, issues);
        
        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(issues);
        Assert.Contains(issues, issue => issue.Skipped && issue.ValidationError.Contains("mandatory column"));
    }
    
    /// <summary>
    /// Tests merging with invalid files throws an exception when not set to skip.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithInvalidFilesAndNoSkip_ThrowsException()
    {
        // Arrange
        var validFile = TestDataGenerator.CreateValidRVToolsFile("valid_test.xlsx", numVMs: 2);
        var invalidFile = TestDataGenerator.CreateInvalidRVToolsFile("invalid_test.xlsx");
        
        string[] filesToMerge = [validFile, invalidFile];
        string outputPath = GetOutputFilePath("exception_output.xlsx");
        var options = CreateDefaultMergeOptions();
        options.SkipInvalidFiles = false; // Don't skip invalid files
        var validationIssues = new List<ValidationIssue>();
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileValidationException>(
            async () => await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues));
        
        Assert.NotEmpty(validationIssues);
        Assert.Contains("invalid_test.xlsx", exception.Message);
    }
    
    /// <summary>
    /// Tests handling of non-existent files.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithNonExistentFile_HandlesError()
    {
        // Arrange
        string[] filesToMerge = ["/path/to/nonexistent.xlsx"];
        string outputPath = GetOutputFilePath("nonexistent_output.xlsx");
        var options = CreateDefaultMergeOptions();
        var validationIssues = new List<ValidationIssue>();
        
        // Create parent directory to avoid permission issues
        FileSystem.Directory.CreateDirectory("/path/to");
        
        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(
            async () => await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues));
    }
    
    /// <summary>
    /// Tests that options to ignore missing optional sheets works correctly.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithIgnoreMissingOptionalSheets_WorksWithMinimumSheets()
    {
        // Arrange
        // Create a file with only vInfo (minimum required sheet)
        var filePath = FileSystem.Path.Combine(TestInputDirectory, "min_sheets.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("vInfo");
            
            // Add minimum required columns for vInfo
            sheet.Cell(1, 1).Value = "VM";
            sheet.Cell(1, 2).Value = "Powerstate";
            sheet.Cell(1, 3).Value = "Template";
            sheet.Cell(1, 4).Value = "CPUs";
            sheet.Cell(1, 5).Value = "Memory";
            sheet.Cell(1, 6).Value = "In Use MiB";
            sheet.Cell(1, 7).Value = "OS according to the configuration file";
            sheet.Cell(1, 8).Value = "SRM Placeholder";
            
            // Add one data row
            sheet.Cell(2, 1).Value = "MinVM";
            sheet.Cell(2, 2).Value = "poweredOn";
            sheet.Cell(2, 3).Value = "FALSE";
            sheet.Cell(2, 4).Value = 2;
            sheet.Cell(2, 5).Value = 4096;
            sheet.Cell(2, 6).Value = 2048;
            sheet.Cell(2, 7).Value = "Windows Server 2019";
            sheet.Cell(2, 8).Value = "FALSE";
            
            workbook.SaveAs(filePath);
        }
        
        string[] filesToMerge = [filePath];
        string outputPath = GetOutputFilePath("ignore_optional_output.xlsx");
        var options = CreateDefaultMergeOptions();
        options.IgnoreMissingOptionalSheets = true; // Ignore missing optional sheets
        var validationIssues = new List<ValidationIssue>();
        
        // Act
        await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues);
        
        // Assert
        // Verify the output file exists
        Assert.True(FileSystem.File.Exists(outputPath));
        
        // Verify merged data - should have data from the input file
        using var resultWorkbook = new XLWorkbook(outputPath);
        Assert.True(resultWorkbook.Worksheets.Contains("vInfo"));
        var resultSheet = resultWorkbook.Worksheet("vInfo");
        var vmCount = resultSheet.RowCount() - 1; // Subtract header row
        
        // Should have 1 VM from the input file
        Assert.Equal(1, vmCount);
        
        // Non-critical validation warnings should exist for missing optional sheets
        Assert.Contains(validationIssues, issue => !issue.Skipped && issue.ValidationError.Contains("vHost"));
    }
}