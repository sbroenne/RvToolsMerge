//-----------------------------------------------------------------------
// <copyright file="BasicMergeTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using ClosedXML.Excel;
using RVToolsMerge.Models;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

[Collection("SpectreConsole")]
/// <summary>
/// Tests for the basic merge functionality.
/// </summary>
public class BasicMergeTests : IntegrationTestBase
{
    /// <summary>
    /// Tests merging multiple valid files with all sheets.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithAllValidFiles_CreatesCorrectOutput()
    {
        // Arrange
        // Create multiple test files with different data
        var file1 = TestDataGenerator.CreateValidRVToolsFile("file1.xlsx", numVMs: 3, numHosts: 2);
        var file2 = TestDataGenerator.CreateValidRVToolsFile("file2.xlsx", numVMs: 2, numHosts: 1);

        string[] filesToMerge = [file1, file2];
        string outputPath = GetOutputFilePath("merged_output.xlsx");
        var options = CreateDefaultMergeOptions();
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues);

        // Assert
        // Verify the output file exists
        Assert.True(FileSystem.File.Exists(outputPath));

        // Verify merged data by reading the actual Excel file
        using var workbook = new XLWorkbook(outputPath);
        
        // Verify vInfo sheet exists and has correct data
        Assert.True(workbook.TryGetWorksheet("vInfo", out var vInfoSheet));
        var vInfoLastRow = vInfoSheet.LastRowUsed()?.RowNumber() ?? 1;
        // Should have 5 VMs total (3 from file1 + 2 from file2) + header row
        Assert.Equal(6, vInfoLastRow); // 5 data rows + 1 header row

        // Verify vHost sheet exists and has correct data  
        Assert.True(workbook.TryGetWorksheet("vHost", out var vHostSheet));
        var vHostLastRow = vHostSheet.LastRowUsed()?.RowNumber() ?? 1;
        // Should have 3 hosts total (2 from file1 + 1 from file2) + header row
        Assert.Equal(4, vHostLastRow); // 3 data rows + 1 header row

        // No validation issues should exist
        Assert.Empty(validationIssues);
    }

    /// <summary>
    /// Tests merging files with only minimum required sheets.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithOnlyRequiredSheets_CreatesValidOutput()
    {
        // Arrange
        // Create a file with only the vInfo sheet (minimum required)
        var file = TestDataGenerator.CreateValidRVToolsFile("min_required.xlsx", numVMs: 3, includeAllSheets: false);

        string[] filesToMerge = [file];
        string outputPath = GetOutputFilePath("merged_min_required.xlsx");
        var options = CreateDefaultMergeOptions();
        options.IgnoreMissingOptionalSheets = true; // Should allow files with only required sheets
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues);

        // Assert
        // Verify the output file exists
        Assert.True(FileSystem.File.Exists(outputPath));

        // Verify merged data by reading the actual Excel file
        using var workbook = new XLWorkbook(outputPath);
        
        // Verify vInfo sheet exists and has correct data
        Assert.True(workbook.TryGetWorksheet("vInfo", out var vInfoSheet));
        var vInfoLastRow = vInfoSheet.LastRowUsed()?.RowNumber() ?? 1;
        // Should have 3 VMs + header row (from min_required.xlsx)
        Assert.Equal(4, vInfoLastRow); // 3 data rows + 1 header row

        // Validation issues about missing optional sheets are expected but not skipped 
        // when IgnoreMissingOptionalSheets = true
        Assert.True(validationIssues.All(issue => !issue.Skipped && issue.ValidationError.Contains("Missing optional sheet")));
    }

    /// <summary>
    /// Tests merging files with different header formats (testing column mapping).
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithDifferentHeaderFormats_MapsCorrectly()
    {
        // Arrange
        // Create one file with standard headers and one with alternative headers
        var standardFile = TestDataGenerator.CreateValidRVToolsFile("standard_headers.xlsx", numVMs: 2);
        var alternativeFile = TestDataGenerator.CreateFileWithAlternativeHeaders("alternative_headers.xlsx", numVMs: 3);

        string[] filesToMerge = [standardFile, alternativeFile];
        string outputPath = GetOutputFilePath("merged_headers.xlsx");
        var options = CreateDefaultMergeOptions();
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues);

        // Assert
        // Verify the output file exists
        Assert.True(FileSystem.File.Exists(outputPath));

        // Verify merged data by reading the actual Excel file
        using var workbook = new XLWorkbook(outputPath);
        
        // Verify vInfo sheet exists and has correct data
        Assert.True(workbook.TryGetWorksheet("vInfo", out var vInfoSheet));
        var vInfoLastRow = vInfoSheet.LastRowUsed()?.RowNumber() ?? 1;
        // Should have 5 VMs total (2 from standardFile + 3 from alternativeFile) + header row
        Assert.Equal(6, vInfoLastRow); // 5 data rows + 1 header row

        // No validation issues should exist
        Assert.Empty(validationIssues);
    }
}
