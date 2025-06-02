//-----------------------------------------------------------------------
// <copyright file="ConfigurationAndEdgeCaseTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using ClosedXML.Excel;
using RVToolsMerge.Configuration;
using RVToolsMerge.Models;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

[Collection("SpectreConsole")]
/// <summary>
/// Tests for configuration settings and edge cases that improve coverage.
/// </summary>
public class ConfigurationAndEdgeCaseTests : IntegrationTestBase
{
    /// <summary>
    /// Tests different merge options combinations.
    /// </summary>
    [Theory]
    [InlineData(true, true, true, false)]
    [InlineData(false, true, false, true)]
    [InlineData(true, false, true, true)]
    [InlineData(false, false, false, false)]
    public async Task MergeFiles_WithVariousOptions_ProcessesCorrectly(
        bool anonymizeData, 
        bool onlyMandatoryColumns, 
        bool includeSourceFileName, 
        bool skipRowsWithEmptyMandatoryValues)
    {
        // Arrange
        var file = TestDataGenerator.CreateValidRVToolsFile("test.xlsx", numVMs: 3);
        string[] filePaths = [file];
        string outputPath = GetOutputFilePath($"output_options_{anonymizeData}_{onlyMandatoryColumns}_{includeSourceFileName}_{skipRowsWithEmptyMandatoryValues}.xlsx");
        
        var options = new MergeOptions
        {
            AnonymizeData = anonymizeData,
            OnlyMandatoryColumns = onlyMandatoryColumns,
            IncludeSourceFileName = includeSourceFileName,
            SkipRowsWithEmptyMandatoryValues = skipRowsWithEmptyMandatoryValues,
            IgnoreMissingOptionalSheets = false,
            SkipInvalidFiles = true,
            DebugMode = false
        };
        
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    /// <summary>
    /// Tests merging files with debug mode enabled.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithDebugMode_ProcessesCorrectly()
    {
        // Arrange
        var file = TestDataGenerator.CreateValidRVToolsFile("debug_test.xlsx", numVMs: 2);
        string[] filePaths = [file];
        string outputPath = GetOutputFilePath("debug_output.xlsx");
        
        var options = CreateDefaultMergeOptions();
        options.DebugMode = true;
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    /// <summary>
    /// Tests merging files with ignore missing optional sheets disabled.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithIgnoreMissingOptionalSheetsDisabled_ProcessesCorrectly()
    {
        // Arrange
        var file = TestDataGenerator.CreateValidRVToolsFile("no_ignore_test.xlsx", numVMs: 2, includeAllSheets: false);
        string[] filePaths = [file];
        string outputPath = GetOutputFilePath("no_ignore_output.xlsx");
        
        var options = CreateDefaultMergeOptions();
        options.IgnoreMissingOptionalSheets = false;
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    /// <summary>
    /// Tests ExcelService with worksheets containing very large numbers of columns.
    /// </summary>
    [Fact]
    public void ExcelService_WithManyColumns_HandlesCorrectly()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("TestSheet");
        
        // Create many columns
        for (int i = 1; i <= 50; i++)
        {
            worksheet.Cell(1, i).Value = $"Column{i}";
        }
        
        var commonColumns = new List<string> { "Column1", "Column25", "Column50" };

        // Act
        var mapping = ExcelService.GetColumnMapping(worksheet, commonColumns);
        var columnNames = ExcelService.GetColumnNames(worksheet);

        // Assert
        Assert.Equal(3, mapping.Count);
        Assert.Equal(50, columnNames.Count);
    }

    /// <summary>
    /// Tests ValidationService with empty mandatory column list.
    /// </summary>
    [Fact]
    public void ValidationService_WithEmptyMandatoryColumns_ReturnsValid()
    {
        // Arrange
        var rowData = new XLCellValue[] { "VM1", "poweredOn", "template1" };
        var emptyMandatoryColumns = new List<int>();

        // Act
        var result = ValidationService.HasEmptyMandatoryValues(rowData, emptyMandatoryColumns);

        // Assert
        Assert.False(result, "Should return false when no mandatory columns are specified");
    }

    /// <summary>
    /// Tests AnonymizationService with various column types not typically anonymized.
    /// </summary>
    [Theory]
    [InlineData("CPUs", "4")]
    [InlineData("Memory", "8192")]
    [InlineData("Template", "FALSE")]
    [InlineData("Random Column", "random value")]
    public void AnonymizationService_WithNonAnonymizableColumns_ReturnsOriginal(string columnName, string value)
    {
        // Arrange
        var columnIndices = new Dictionary<string, int> { { columnName, 0 } };
        var originalValue = (XLCellValue)value;

        // Act
        var result = AnonymizationService.AnonymizeValue(originalValue, 0, columnIndices, "test.xlsx");

        // Assert
        Assert.Equal(originalValue, result);
    }

    /// <summary>
    /// Tests ExcelService GetColumnNames with mixed data types in headers.
    /// </summary>
    [Fact]
    public void ExcelService_GetColumnNames_WithMixedDataTypes_HandlesCorrectly()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("TestSheet");
        
        worksheet.Cell(1, 1).Value = "String Column";
        worksheet.Cell(1, 2).Value = 42;                    // Number
        worksheet.Cell(1, 3).Value = DateTime.Now;          // Date
        worksheet.Cell(1, 4).Value = true;                  // Boolean
        worksheet.Cell(1, 5).Value = 3.14159;              // Decimal

        // Act
        var columnNames = ExcelService.GetColumnNames(worksheet);

        // Assert
        Assert.Equal(5, columnNames.Count);
        Assert.Contains("String Column", columnNames);
        Assert.Contains("42", columnNames);
        Assert.Contains("TRUE", columnNames); // Boolean is formatted as "TRUE"
    }

    /// <summary>
    /// Tests ExcelService with sheets that have the same name in different casing.
    /// </summary>
    [Fact]
    public void ExcelService_SheetExists_WithDifferentCasing_ReturnsTrue()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("vINFO");  // Different casing

        // Act & Assert
        Assert.True(ExcelService.SheetExists(workbook, "vinfo"));
        Assert.True(ExcelService.SheetExists(workbook, "VINFO"));
        Assert.True(ExcelService.SheetExists(workbook, "VInfo"));
    }

    /// <summary>
    /// Tests MergeService with very small datasets.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithSingleVM_ProcessesCorrectly()
    {
        // Arrange
        var file = TestDataGenerator.CreateValidRVToolsFile("single_vm.xlsx", numVMs: 1, numHosts: 1);
        string[] filePaths = [file];
        string outputPath = GetOutputFilePath("single_vm_output.xlsx");
        var options = CreateDefaultMergeOptions();
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    /// <summary>
    /// Tests ExcelService GetColumnMapping with no matching columns.
    /// </summary>
    [Fact]
    public void ExcelService_GetColumnMapping_WithNoMatches_ReturnsEmpty()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("TestSheet");
        worksheet.Cell(1, 1).Value = "NonExistentColumn1";
        worksheet.Cell(1, 2).Value = "NonExistentColumn2";

        var commonColumns = new List<string> { "VM", "Powerstate", "Host" };

        // Act
        var mapping = ExcelService.GetColumnMapping(worksheet, commonColumns);

        // Assert
        Assert.Empty(mapping);
    }

    /// <summary>
    /// Tests MergeService with files containing different amounts of data.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithDifferentDataSizes_ProcessesCorrectly()
    {
        // Arrange
        var smallFile = TestDataGenerator.CreateValidRVToolsFile("small.xlsx", numVMs: 1, numHosts: 1);
        var mediumFile = TestDataGenerator.CreateValidRVToolsFile("medium.xlsx", numVMs: 5, numHosts: 2);
        var largeFile = TestDataGenerator.CreateValidRVToolsFile("large.xlsx", numVMs: 10, numHosts: 3);
        
        string[] filePaths = [smallFile, mediumFile, largeFile];
        string outputPath = GetOutputFilePath("mixed_sizes_output.xlsx");
        var options = CreateDefaultMergeOptions();
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    /// <summary>
    /// Tests ValidationService HasEmptyMandatoryValues with various empty value types.
    /// </summary>
    [Theory]
    [InlineData("")]           // Empty string
    [InlineData("   ")]        // Whitespace
    [InlineData("\t")]         // Tab
    [InlineData("\n")]         // Newline
    public void ValidationService_HasEmptyMandatoryValues_WithVariousEmptyTypes_ReturnsTrue(string emptyValue)
    {
        // Arrange
        var rowData = new XLCellValue[] { "VM1", emptyValue, "template1" };
        var mandatoryColumns = new List<int> { 1 }; // Second column is mandatory and empty

        // Act
        var result = ValidationService.HasEmptyMandatoryValues(rowData, mandatoryColumns);

        // Assert
        Assert.True(result, $"Should detect empty value: '{emptyValue}'");
    }

    /// <summary>
    /// Tests SheetConfiguration constants for correctness.
    /// </summary>
    [Fact]
    public void SheetConfiguration_RequiredSheets_ContainsExpectedValues()
    {
        // Act & Assert
        Assert.Contains("vInfo", SheetConfiguration.RequiredSheets);
        Assert.Contains("vHost", SheetConfiguration.RequiredSheets);
        Assert.Contains("vPartition", SheetConfiguration.RequiredSheets);
        Assert.Contains("vMemory", SheetConfiguration.RequiredSheets);
    }

    /// <summary>
    /// Tests SheetConfiguration minimum required sheets.
    /// </summary>
    [Fact]
    public void SheetConfiguration_MinimumRequiredSheets_ContainsVInfo()
    {
        // Act & Assert
        Assert.Single(SheetConfiguration.MinimumRequiredSheets);
        Assert.Contains("vInfo", SheetConfiguration.MinimumRequiredSheets);
    }

    /// <summary>
    /// Tests that column header mappings exist for all required sheets.
    /// </summary>
    [Fact]
    public void SheetConfiguration_ColumnHeaderMappings_ExistForRequiredSheets()
    {
        // Act & Assert
        foreach (var sheet in SheetConfiguration.RequiredSheets)
        {
            Assert.True(SheetConfiguration.SheetColumnHeaderMappings.ContainsKey(sheet), 
                $"Column header mapping should exist for sheet: {sheet}");
        }
    }
}