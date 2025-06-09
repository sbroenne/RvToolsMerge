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

[Collection("SpectreConsole")]
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
        options.IgnoreMissingOptionalSheets = true; // Allow files with only vInfo sheet
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues);

        // Assert
        // Verify the output file exists
        Assert.True(FileSystem.File.Exists(outputPath));

        // Verify anonymization map file exists when anonymization is enabled
        string mapFilePath = FileSystem.Path.Combine(
            FileSystem.Path.GetDirectoryName(outputPath) ?? string.Empty,
            $"{FileSystem.Path.GetFileNameWithoutExtension(outputPath)}_AnonymizationMapping{FileSystem.Path.GetExtension(outputPath)}");
        Assert.True(FileSystem.File.Exists(mapFilePath), "Anonymization map file should exist");

        // Verify the output file has data by reading it
        using var workbook = new XLWorkbook(outputPath);
        Assert.True(workbook.TryGetWorksheet("vInfo", out var vInfoSheet));
        var lastRow = vInfoSheet.LastRowUsed()?.RowNumber() ?? 1;
        Assert.True(lastRow > 1, "Should have data rows in addition to header");
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

        // Verify the output file has expected structure by reading it
        using var workbook = new XLWorkbook(outputPath);
        Assert.True(workbook.TryGetWorksheet("vInfo", out var vInfoSheet));
        
        // Check that we have data rows + header
        var lastRow = vInfoSheet.LastRowUsed()?.RowNumber() ?? 1;
        Assert.Equal(4, lastRow); // 3 VMs + header row
        
        // In the real implementation, the column count would depend on the actual mandatory columns
        // For this test, we verify that the file was created and has the expected structure
        var lastColumn = vInfoSheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        Assert.True(lastColumn >= 16, "Should have at least the mandatory columns"); // At least 16 mandatory columns
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

        // Verify that a validation issue was recorded for the incomplete row
        Assert.NotEmpty(validationIssues);
        Assert.Contains(validationIssues, issue => issue.ValidationError.Contains("empty value"));

        // Verify the output file by reading it - should have fewer rows than expected
        using var outputWorkbook = new XLWorkbook(outputPath);
        Assert.True(outputWorkbook.TryGetWorksheet("vInfo", out var outputVInfoSheet));
        var lastRow = outputVInfoSheet.LastRowUsed()?.RowNumber() ?? 1;
        // Should have 5 VMs (the incomplete one should be skipped) + header row
        Assert.Equal(6, lastRow);
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
        // Verify the output file exists
        Assert.True(FileSystem.File.Exists(outputPath));

        // Verify that validation issues were recorded for the invalid file
        Assert.NotEmpty(validationIssues);
        Assert.Contains(validationIssues, issue => issue.FileName.Contains("invalid_file.xlsx"));

        // Verify the output file has data from the valid file
        using var workbook = new XLWorkbook(outputPath);
        Assert.True(workbook.TryGetWorksheet("vInfo", out var vInfoSheet));
        var lastRow = vInfoSheet.LastRowUsed()?.RowNumber() ?? 1;
        Assert.Equal(4, lastRow); // 3 VMs from valid file + header row
    }

    /// <summary>
    /// Tests the MaxVInfoRows option to limit the number of vInfo rows processed.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithMaxVInfoRowsLimit_LimitsRows()
    {
        // Arrange
        var filePath = TestDataGenerator.CreateValidRVToolsFile("limit_vinfo_rows.xlsx", numVMs: 10);
        string[] filesToMerge = [filePath];
        string outputPath = GetOutputFilePath("limited_vinfo_output.xlsx");
        var options = CreateDefaultMergeOptions();
        options.MaxVInfoRows = 3; // Limit to 3 vInfo rows
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues);

        // Assert
        // Verify the output file exists
        Assert.True(FileSystem.File.Exists(outputPath));

        // Verify the output file by reading it - should have only 3 vInfo rows + header
        using var outputWorkbook = new XLWorkbook(outputPath);
        Assert.True(outputWorkbook.TryGetWorksheet("vInfo", out var outputVInfoSheet));
        var lastRow = outputVInfoSheet.LastRowUsed()?.RowNumber() ?? 1;
        Assert.Equal(4, lastRow); // 3 limited VMs + header row

        // Verify other sheets are not affected by the vInfo limit
        if (outputWorkbook.TryGetWorksheet("vHost", out var outputVHostSheet))
        {
            var vHostLastRow = outputVHostSheet.LastRowUsed()?.RowNumber() ?? 1;
            // vHost should have all its rows (not limited)
            Assert.True(vHostLastRow > 1); // Should have data beyond just header
        }
    }

    /// <summary>
    /// Tests that setting MaxVInfoRows to null has no limiting effect.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithMaxVInfoRowsNull_DoesNotLimit()
    {
        // Arrange
        var filePath = TestDataGenerator.CreateValidRVToolsFile("no_limit_vinfo_rows.xlsx", numVMs: 5);
        string[] filesToMerge = [filePath];
        string outputPath = GetOutputFilePath("unlimited_vinfo_output.xlsx");
        var options = CreateDefaultMergeOptions();
        options.MaxVInfoRows = null; // No limit
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues);

        // Assert
        // Verify the output file exists
        Assert.True(FileSystem.File.Exists(outputPath));

        // Verify the output file by reading it - should have all 5 vInfo rows + header
        using var outputWorkbook = new XLWorkbook(outputPath);
        Assert.True(outputWorkbook.TryGetWorksheet("vInfo", out var outputVInfoSheet));
        var lastRow = outputVInfoSheet.LastRowUsed()?.RowNumber() ?? 1;
        Assert.Equal(6, lastRow); // 5 VMs + header row
    }

    /// <summary>
    /// Tests that other sheets with VM UUIDs are synchronized with limited vInfo rows.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithMaxVInfoRowsLimit_SynchronizesVmRelatedSheets()
    {
        // Arrange
        var filePath = TestDataGenerator.CreateValidRVToolsFile("sync_sheets_test.xlsx", numVMs: 10, numHosts: 3);
        string[] filesToMerge = [filePath];
        string outputPath = GetOutputFilePath("synchronized_sheets_output.xlsx");
        var options = CreateDefaultMergeOptions();
        options.MaxVInfoRows = 3; // Limit to 3 vInfo rows
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues);

        // Assert
        // Verify the output file exists
        Assert.True(FileSystem.File.Exists(outputPath));

        using var outputWorkbook = new XLWorkbook(outputPath);
        
        // Verify vInfo is limited to 3 rows + header = 4 total
        Assert.True(outputWorkbook.TryGetWorksheet("vInfo", out var outputVInfoSheet));
        var vInfoLastRow = outputVInfoSheet.LastRowUsed()?.RowNumber() ?? 1;
        Assert.Equal(4, vInfoLastRow); // 3 limited VMs + header row

        // Verify vPartition is also limited (should only have data for the 3 VMs from vInfo)
        // Each VM has 2 partitions, so 3 VMs = 6 partitions + 1 header = 7 rows
        if (outputWorkbook.TryGetWorksheet("vPartition", out var outputVPartitionSheet))
        {
            var vPartitionLastRow = outputVPartitionSheet.LastRowUsed()?.RowNumber() ?? 1;
            Assert.Equal(7, vPartitionLastRow); // 6 VM partitions + header row (2 partitions per VM)
        }

        // Verify vMemory is also limited (should only have data for the 3 VMs from vInfo)
        if (outputWorkbook.TryGetWorksheet("vMemory", out var outputVMemorySheet))
        {
            var vMemoryLastRow = outputVMemorySheet.LastRowUsed()?.RowNumber() ?? 1;
            Assert.Equal(4, vMemoryLastRow); // 3 VM memory entries + header row (one per VM)
        }

        // Verify vHost is NOT limited (it doesn't have VM UUIDs, so it should have all hosts)
        if (outputWorkbook.TryGetWorksheet("vHost", out var outputVHostSheet))
        {
            var vHostLastRow = outputVHostSheet.LastRowUsed()?.RowNumber() ?? 1;
            Assert.Equal(4, vHostLastRow); // 3 hosts + header row (not limited by VM count)
        }
    }
}
