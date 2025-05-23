//-----------------------------------------------------------------------
// <copyright file="MergeOptionsTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using ClosedXML.Excel;
using RVToolsMerge.Models;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Tests for the various merge options.
/// </summary>
public class MergeOptionsTests : IntegrationTestBase
{
    /// <summary>
    /// Tests the anonymization option.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithAnonymization_AnonymizesData()
    {
        // Arrange
        var file = TestDataGenerator.CreateFileForAnonymizationTesting("anonymize_test.xlsx");
        
        string[] filesToMerge = [file];
        string outputPath = GetOutputFilePath("anonymized_output.xlsx");
        var options = CreateDefaultMergeOptions();
        options.AnonymizeData = true; // Enable anonymization
        var validationIssues = new List<ValidationIssue>();
        
        // Act
        await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues);
        
        // Assert
        // Verify the output file exists
        Assert.True(FileSystem.File.Exists(outputPath));
        
        // Verify anonymized data
        using var workbook = new XLWorkbook(outputPath);
        var vInfoSheet = workbook.Worksheet("vInfo");
        
        // Check for anonymized VM name and DNS name
        var vmName = vInfoSheet.Cell(2, vInfoSheet.FirstColumnUsed().ColumnNumber()).Value.ToString();
        var dnsName = vInfoSheet.Cell(2, 4).Value.ToString(); // Assuming DNS Name is in column 4
        var ipAddress = vInfoSheet.Cell(2, 9).Value.ToString(); // Assuming IP Address is in column 9
        
        // VM name should be anonymized (shouldn't contain "CONFIDENTIAL")
        Assert.DoesNotContain("CONFIDENTIAL", vmName ?? string.Empty);
        
        // DNS name should be anonymized (shouldn't contain domain parts)
        Assert.DoesNotContain("contoso.local", dnsName ?? string.Empty);
        
        // IP Address should be anonymized
        Assert.DoesNotContain("192.168.1.100", ipAddress ?? string.Empty);
    }
    
    /// <summary>
    /// Tests the option to include only mandatory columns.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithOnlyMandatoryColumns_ExcludesOptionalColumns()
    {
        // Arrange
        var file = TestDataGenerator.CreateValidRVToolsFile("full_columns.xlsx", numVMs: 3);
        
        string[] filesToMerge = [file];
        string outputPath = GetOutputFilePath("mandatory_columns_output.xlsx");
        var options = CreateDefaultMergeOptions();
        options.OnlyMandatoryColumns = true; // Only include mandatory columns
        var validationIssues = new List<ValidationIssue>();
        
        // Act
        await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues);
        
        // Assert
        // Verify the output file exists
        Assert.True(FileSystem.File.Exists(outputPath));
        
        // Verify the output file has only mandatory columns
        using var workbook = new XLWorkbook(outputPath);
        var sheet = workbook.Worksheet("vInfo");
        
        // Extract the header row to a list of column names
        var columnNames = new List<string>();
        foreach (var cell in sheet.Row(1).CellsUsed())
        {
            columnNames.Add(cell.Value.ToString()!);
        }
        
        // Check mandatory columns are included
        Assert.Contains("VM", columnNames);
        Assert.Contains("Template", columnNames);
        Assert.Contains("SRM Placeholder", columnNames);
        Assert.Contains("Powerstate", columnNames);
        Assert.Contains("CPUs", columnNames);
        Assert.Contains("Memory", columnNames);
        Assert.Contains("In Use MiB", columnNames);
        Assert.Contains("OS according to the configuration file", columnNames);
        
        // Check the number of columns - should match mandatory columns + source file if included
        int expectedColumns = 8; // Number of mandatory columns for vInfo
        if (options.IncludeSourceFileName)
        {
            expectedColumns++;
        }
        
        Assert.Equal(expectedColumns, columnNames.Count);
    }
    
    /// <summary>
    /// Tests the option to skip rows with empty mandatory values.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithSkipEmptyMandatory_OmitsIncompleteRows()
    {
        // Arrange
        // First, create a valid file
        var filePath = TestDataGenerator.CreateValidRVToolsFile("incomplete_rows.xlsx", numVMs: 5);
        
        // Then modify it to add a row with empty mandatory values
        using (var workbook = new XLWorkbook(filePath))
        {
            var vInfoSheet = workbook.Worksheet("vInfo");
            
            // Add a row with missing mandatory value (empty OS)
            vInfoSheet.Cell(7, 1).Value = "IncompleteVM";
            vInfoSheet.Cell(7, 2).Value = "poweredOn";
            vInfoSheet.Cell(7, 3).Value = "FALSE";
            vInfoSheet.Cell(7, 4).Value = 2;
            vInfoSheet.Cell(7, 5).Value = 4096;
            vInfoSheet.Cell(7, 6).Value = 2048;
            vInfoSheet.Cell(7, 7).Value = ""; // Empty OS (mandatory)
            vInfoSheet.Cell(7, 11).Value = "FALSE";
            
            workbook.Save();
        }
        
        string[] filesToMerge = [filePath];
        string outputPath = GetOutputFilePath("skip_empty_mandatory_output.xlsx");
        var options = CreateDefaultMergeOptions();
        options.SkipRowsWithEmptyMandatoryValues = true; // Skip rows with empty mandatory values
        var validationIssues = new List<ValidationIssue>();
        
        // Act
        await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues);
        
        // Assert
        // Verify the output file exists
        Assert.True(FileSystem.File.Exists(outputPath));
        
        // Verify that incomplete rows were skipped
        using var resultWorkbook = new XLWorkbook(outputPath);
        var resultSheet = resultWorkbook.Worksheet("vInfo");
        var vmCount = resultSheet.RowCount() - 1; // Subtract header row
        
        // Should have 5 VMs, not 6 (the incomplete one should be skipped)
        Assert.Equal(5, vmCount);
        
        // Check that the incomplete VM is not in the output
        var allVMNames = new List<string>();
        foreach (var row in resultSheet.RowsUsed().Skip(1)) // Skip header
        {
            var vmNameCell = row.Cell(row.Worksheet.FirstColumnUsed().ColumnNumber());
            allVMNames.Add(vmNameCell.Value.ToString() ?? string.Empty);
        }
        
        Assert.DoesNotContain("IncompleteVM", allVMNames);
    }
    
    /// <summary>
    /// Tests the option to skip invalid files.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithSkipInvalidFiles_ProcessesValidFilesOnly()
    {
        // Arrange
        var validFile = TestDataGenerator.CreateValidRVToolsFile("valid_file.xlsx", numVMs: 3);
        var invalidFile = TestDataGenerator.CreateInvalidRVToolsFile("invalid_file.xlsx");
        
        string[] filesToMerge = [validFile, invalidFile];
        string outputPath = GetOutputFilePath("skip_invalid_output.xlsx");
        var options = CreateDefaultMergeOptions();
        options.SkipInvalidFiles = true; // Skip invalid files
        var validationIssues = new List<ValidationIssue>();
        
        // Act
        await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues);
        
        // Assert
        // Verify the output file exists - should be created even with invalid files
        Assert.True(FileSystem.File.Exists(outputPath));
        
        // Verify merged data - should only have data from valid file
        using var workbook = new XLWorkbook(outputPath);
        var vInfoSheet = workbook.Worksheet("vInfo");
        var vmCount = vInfoSheet.RowCount() - 1; // Subtract header row
        
        // Should have 3 VMs (only from the valid file)
        Assert.Equal(3, vmCount);
        
        // Validation issues should exist for the invalid file
        Assert.NotEmpty(validationIssues);
        Assert.Contains(validationIssues, issue => issue.FileName == "invalid_file.xlsx");
        
        // Also verify that valid files were processed
        Assert.DoesNotContain(validationIssues, issue => issue.FileName == "valid_file.xlsx" && issue.Skipped);
    }
}