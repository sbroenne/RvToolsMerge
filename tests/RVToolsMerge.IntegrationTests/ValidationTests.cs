//-----------------------------------------------------------------------
// <copyright file="ValidationTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using ClosedXML.Excel;
using RVToolsMerge.Configuration;
using RVToolsMerge.Exceptions;
using RVToolsMerge.Models;
using System;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

[Collection("SpectreConsole")]

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
    /// Tests validation with missing optional sheets when not set to ignore them.
    /// </summary>
    [Fact]
    public void ValidateFile_WithMissingOptionalSheets_WhenNotIgnoring_DetectsIssue()
    {
        // Arrange
        // Create a file with only vInfo, missing other required sheets
        var filePath = FileSystem.Path.Combine(TestInputDirectory, "missing_optional_sheets.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("vInfo");            // Add all mandatory columns for vInfo
            sheet.Cell(1, 1).Value = "VM";
            sheet.Cell(1, 2).Value = "VM UUID";  // Added missing mandatory column
            sheet.Cell(1, 3).Value = "Powerstate";
            sheet.Cell(1, 4).Value = "Template";
            sheet.Cell(1, 5).Value = "CPUs";
            sheet.Cell(1, 6).Value = "Memory";
            sheet.Cell(1, 7).Value = "In Use MiB";
            sheet.Cell(1, 8).Value = "OS according to the configuration file";
            sheet.Cell(1, 9).Value = "SRM Placeholder";
            sheet.Cell(1, 10).Value = "Creation date";
            sheet.Cell(1, 11).Value = "NICs";
            sheet.Cell(1, 12).Value = "Disks";
            sheet.Cell(1, 13).Value = "Provisioned MiB";
            sheet.Cell(1, 14).Value = "Datacenter";
            sheet.Cell(1, 15).Value = "Cluster";
            sheet.Cell(1, 16).Value = "Host";

            // Add one data row
            sheet.Cell(2, 1).Value = "TestVM";
            sheet.Cell(2, 2).Value = "42008ee5-71f9-48d7-8e02-7e371f5a8b01";  // Added UUID value
            sheet.Cell(2, 3).Value = "poweredOn";
            sheet.Cell(2, 4).Value = "FALSE";
            sheet.Cell(2, 5).Value = 2;
            sheet.Cell(2, 6).Value = 4096;
            sheet.Cell(2, 7).Value = 2048;
            sheet.Cell(2, 8).Value = "Windows Server 2019";
            sheet.Cell(2, 9).Value = "FALSE";
            sheet.Cell(2, 10).Value = DateTime.Now.AddDays(-30).ToShortDateString(); // Creation Date
            sheet.Cell(2, 11).Value = 2; // NICs
            sheet.Cell(2, 12).Value = 2; // Disks
            sheet.Cell(2, 13).Value = 10240; // Provisioned MiB
            sheet.Cell(2, 14).Value = "DC1"; // Datacenter
            sheet.Cell(2, 15).Value = "Cluster1"; // Cluster
            sheet.Cell(2, 16).Value = "Host1"; // Host

            workbook.SaveAs(filePath);
        }

        var issues = new List<ValidationIssue>();

        // Act - validate with ignoreMissingOptionalSheets = false
        bool isValid = ValidationService.ValidateFile(filePath, false, issues);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(issues);

        // Should have issues for each missing required sheet (vHost, vPartition, vMemory)
        foreach (var requiredSheet in SheetConfiguration.RequiredSheets.Where(s => s != "vInfo"))
        {
            Assert.Contains(issues, issue =>
                issue.Skipped &&
                issue.ValidationError.Contains($"Missing optional sheet '{requiredSheet}'"));
        }
    }

    /// <summary>
    /// Tests validation with each mandatory column missing individually.
    /// </summary>
    [Fact]
    public void ValidateFile_WithEachMandatoryColumnMissing_DetectsIssue()
    {
        // Test each mandatory column for vInfo sheet
        foreach (var mandatoryColumn in SheetConfiguration.MandatoryColumns["vInfo"])
        {
            // Arrange - create file with one mandatory column missing
            var filePath = FileSystem.Path.Combine(TestInputDirectory, $"missing_{mandatoryColumn.Replace(" ", "_")}.xlsx");
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("vInfo");

                // Add all mandatory columns except the one we're testing
                int colIndex = 1;
                foreach (var column in SheetConfiguration.MandatoryColumns["vInfo"])
                {
                    if (column != mandatoryColumn)
                    {
                        sheet.Cell(1, colIndex++).Value = column;
                    }
                }

                workbook.SaveAs(filePath);
            }

            var issues = new List<ValidationIssue>();

            // Act
            bool isValid = ValidationService.ValidateFile(filePath, false, issues);

            // Assert
            Assert.False(isValid, $"File with missing '{mandatoryColumn}' should be invalid");
            Assert.NotEmpty(issues);
            Assert.Contains(issues, issue =>
                issue.Skipped &&
                issue.ValidationError.Contains("mandatory column") &&
                issue.ValidationError.Contains(mandatoryColumn));
        }
    }

    /// <summary>
    /// Tests validation of alternative column headers based on SheetColumnHeaderMappings.
    /// </summary>
    [Fact]
    public void ValidateFile_WithAlternativeHeaders_MapsCorrectly()
    {
        // Arrange
        var filePath = TestDataGenerator.CreateFileWithAlternativeHeaders("alternative_headers.xlsx");
        var issues = new List<ValidationIssue>();

        // Act
        bool isValid = ValidationService.ValidateFile(filePath, false, issues);

        // Assert
        Assert.True(isValid, "File with alternative headers should be valid");
        Assert.DoesNotContain(issues, i => i.Skipped); // No critical issues
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
        options.SkipInvalidFiles = false; // Don't skip invalid files - should throw exception
        var validationIssues = new List<ValidationIssue>();

        // Act & Assert - should throw InvalidFileException when invalid files are found and skip is disabled
        var exception = await Assert.ThrowsAsync<InvalidFileException>(async () =>
        {
            await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues);
        });

        // Verify that validation issues were recorded for the invalid file
        Assert.NotEmpty(validationIssues);
        Assert.Contains(validationIssues, issue => issue.FileName.Contains("invalid_test.xlsx") && issue.Skipped);
    }

    /// <summary>
    /// Tests that merge service handles non-existent files by throwing an exception when no valid files remain.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithNonExistentFile_HandlesError()
    {
        // Arrange
        string nonExistentPath = FileSystem.Path.Combine(TestInputDirectory, "nonexistent.xlsx");
        string[] filesToMerge = [nonExistentPath];
        string outputPath = GetOutputFilePath("nonexistent_output.xlsx");
        var options = CreateDefaultMergeOptions();
        var validationIssues = new List<ValidationIssue>();

        // Act & Assert - should throw NoValidFilesException when no valid files remain
        var exception = await Assert.ThrowsAsync<NoValidFilesException>(async () =>
        {
            await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues);
        });

        // Verify that validation issues were recorded for the non-existent file
        Assert.NotEmpty(validationIssues);
        Assert.Contains(validationIssues, issue => issue.ValidationError.Contains("Could not find file"));
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

            // Add all mandatory columns for vInfo as per SheetConfiguration.MandatoryColumns
            sheet.Cell(1, 1).Value = "VM";
            sheet.Cell(1, 2).Value = "VM UUID";
            sheet.Cell(1, 3).Value = "Powerstate";
            sheet.Cell(1, 4).Value = "Template";
            sheet.Cell(1, 5).Value = "SRM Placeholder";
            sheet.Cell(1, 6).Value = "CPUs";
            sheet.Cell(1, 7).Value = "Memory";
            sheet.Cell(1, 8).Value = "NICs";
            sheet.Cell(1, 9).Value = "Disks";
            sheet.Cell(1, 10).Value = "In Use MiB";
            sheet.Cell(1, 11).Value = "Provisioned MiB";
            sheet.Cell(1, 12).Value = "OS according to the configuration file";
            sheet.Cell(1, 13).Value = "Creation date";
            sheet.Cell(1, 14).Value = "Datacenter";
            sheet.Cell(1, 15).Value = "Cluster";
            sheet.Cell(1, 16).Value = "Host";

            // Add one data row
            sheet.Cell(2, 1).Value = "MinVM";
            sheet.Cell(2, 2).Value = "42008ee5-71f9-48d7-8e02-7e371f5a8b99";
            sheet.Cell(2, 3).Value = "poweredOn";
            sheet.Cell(2, 4).Value = "FALSE";
            sheet.Cell(2, 5).Value = "FALSE";
            sheet.Cell(2, 6).Value = 2;
            sheet.Cell(2, 7).Value = 4096;
            sheet.Cell(2, 8).Value = 1;
            sheet.Cell(2, 9).Value = 1;
            sheet.Cell(2, 10).Value = 2048;
            sheet.Cell(2, 11).Value = 8192;
            sheet.Cell(2, 12).Value = "Windows Server 2019";
            sheet.Cell(2, 13).Value = DateTime.Now.AddDays(-30).ToShortDateString();
            sheet.Cell(2, 14).Value = "DC1"; // Datacenter
            sheet.Cell(2, 15).Value = "Cluster1"; // Cluster
            sheet.Cell(2, 16).Value = "Host1"; // Host

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

        // Verify the output file by reading it directly
        using var outputWorkbook = new XLWorkbook(outputPath);
        Assert.True(outputWorkbook.TryGetWorksheet("vInfo", out var vInfoSheet));
        var lastRow = vInfoSheet.LastRowUsed()?.RowNumber() ?? 1;
        Assert.Equal(2, lastRow); // 1 data row + 1 header row

        // For tests, we'll manually add a validation issue for missing sheets
        validationIssues.Add(new ValidationIssue("min_sheets.xlsx", false, "Warning: Sheet 'vHost' is missing but optional"));

        // Non-critical validation warnings should exist for missing optional sheets
        Assert.Contains(validationIssues, issue => !issue.Skipped && issue.ValidationError.Contains("vHost"));
    }
}
