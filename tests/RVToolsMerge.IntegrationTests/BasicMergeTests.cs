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
        
        // Verify merged data using ClosedXML
        using var workbook = new XLWorkbook(outputPath);
        
        // Verify all expected sheets exist
        Assert.True(workbook.Worksheets.Contains("vInfo"));
        Assert.True(workbook.Worksheets.Contains("vHost"));
        Assert.True(workbook.Worksheets.Contains("vPartition"));
        Assert.True(workbook.Worksheets.Contains("vMemory"));
        
        // Verify sheet contents - should have data from all files
        var vInfoSheet = workbook.Worksheet("vInfo");
        var vmCount = vInfoSheet.RowCount() - 1; // Subtract header row
        
        // Should have 5 VMs total (3 from file1 + 2 from file2)
        Assert.Equal(5, vmCount);
        
        // Verify vHost sheet has 3 hosts (2 from file1 + 1 from file2)
        var vHostSheet = workbook.Worksheet("vHost");
        var hostCount = vHostSheet.RowCount() - 1;
        Assert.Equal(3, hostCount);
        
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
        
        // Verify merged data using ClosedXML
        using var workbook = new XLWorkbook(outputPath);
        
        // Verify the required sheet exists
        Assert.True(workbook.Worksheets.Contains("vInfo"));
        
        // Verify vInfo sheet has correct data
        var vInfoSheet = workbook.Worksheet("vInfo");
        var vmCount = vInfoSheet.RowCount() - 1; // Subtract header row
        Assert.Equal(3, vmCount);
        
        // No validation issues should exist
        Assert.Empty(validationIssues);
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
        
        // Verify merged data using ClosedXML
        using var workbook = new XLWorkbook(outputPath);
        
        // Check vInfo sheet
        var vInfoSheet = workbook.Worksheet("vInfo");
        var vmCount = vInfoSheet.RowCount() - 1; // Subtract header row
        
        // Should have 5 VMs total (2 from standardFile + 3 from alternativeFile)
        Assert.Equal(5, vmCount);
        
        // No validation issues should exist
        Assert.Empty(validationIssues);
    }
}