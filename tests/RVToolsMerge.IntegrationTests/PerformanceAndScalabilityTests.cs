//-----------------------------------------------------------------------
// <copyright file="PerformanceAndScalabilityTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using System.IO.Abstractions.TestingHelpers;
using ClosedXML.Excel;
using RVToolsMerge.Models;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

[Collection("SpectreConsole")]
/// <summary>
/// Tests for performance, scalability, and comprehensive coverage scenarios.
/// </summary>
public class PerformanceAndScalabilityTests : IntegrationTestBase
{
    /// <summary>
    /// Tests merging multiple files with anonymization enabled.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithAnonymizationAndMultipleFiles_ProcessesCorrectly()
    {
        // Arrange
        var file1 = TestDataGenerator.CreateValidRVToolsFile("anon1.xlsx", numVMs: 3);
        var file2 = TestDataGenerator.CreateValidRVToolsFile("anon2.xlsx", numVMs: 4);
        var file3 = TestDataGenerator.CreateValidRVToolsFile("anon3.xlsx", numVMs: 2);
        
        string[] filePaths = [file1, file2, file3];
        string outputPath = GetOutputFilePath("anonymized_multiple_output.xlsx");
        
        var options = CreateDefaultMergeOptions();
        options.AnonymizeData = true;
        options.IncludeSourceFileName = true;
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(File.Exists(outputPath));
        
        // Check that anonymization map file is created
        var anonymizationMapPath = outputPath.Replace(".xlsx", "_AnonymizationMap.xlsx");
        Assert.True(File.Exists(anonymizationMapPath));
    }

    /// <summary>
    /// Tests merging with larger datasets to ensure scalability.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithLargeDatasets_ProcessesCorrectly()
    {
        // Arrange
        var file1 = TestDataGenerator.CreateValidRVToolsFile("large1.xlsx", numVMs: 20, numHosts: 5);
        var file2 = TestDataGenerator.CreateValidRVToolsFile("large2.xlsx", numVMs: 25, numHosts: 6);
        
        string[] filePaths = [file1, file2];
        string outputPath = GetOutputFilePath("large_datasets_output.xlsx");
        var options = CreateDefaultMergeOptions();
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    /// <summary>
    /// Tests ExcelService with worksheets containing very long cell values.
    /// </summary>
    [Fact]
    public void ExcelService_WithLongCellValues_HandlesCorrectly()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("TestSheet");
        
        var longValue = new string('A', 1000); // Very long string
        worksheet.Cell(1, 1).Value = "VM";
        worksheet.Cell(1, 2).Value = longValue;
        worksheet.Cell(1, 3).Value = "Powerstate";
        
        var commonColumns = new List<string> { "VM", "Powerstate" };

        // Act
        var mapping = ExcelService.GetColumnMapping(worksheet, commonColumns);
        var columnNames = ExcelService.GetColumnNames(worksheet);

        // Assert
        Assert.Equal(2, mapping.Count); // Should map VM and Powerstate, skip long value
        Assert.Equal(3, columnNames.Count);
        Assert.Contains(longValue, columnNames);
    }

    /// <summary>
    /// Tests ValidationService with complex data scenarios.
    /// </summary>
    [Fact]
    public void ValidationService_WithComplexRowData_ValidatesCorrectly()
    {
        // Arrange
        var complexRowData = new XLCellValue[]
        {
            "Complex-VM-Name-With-Dashes",
            "poweredOn",
            "Template-Name",
            "server.domain.local",
            "4",
            "8192",
            "4096",
            "Windows Server 2019",
            "Extra Data",
            "More Extra Data"
        };
        
        var mandatoryColumns = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 }; // First 8 columns

        // Act
        var hasEmptyValues = ValidationService.HasEmptyMandatoryValues(complexRowData, mandatoryColumns);

        // Assert
        Assert.False(hasEmptyValues, "Complex valid row data should not have empty mandatory values");
    }

    /// <summary>
    /// Tests AnonymizationService with various special characters in data.
    /// </summary>
    [Theory]
    [InlineData("vm-with-dashes")]
    [InlineData("vm.with.dots")]
    [InlineData("vm_with_underscores")]
    [InlineData("vm@domain.com")]
    [InlineData("vm with spaces")]
    public void AnonymizationService_WithSpecialCharacters_HandlesCorrectly(string vmName)
    {
        // Arrange
        var columnIndices = new Dictionary<string, int> { { "VM", 0 } };
        var originalValue = (XLCellValue)vmName;

        // Act
        var result1 = AnonymizationService.AnonymizeValue(originalValue, 0, columnIndices, "test.xlsx");
        var result2 = AnonymizationService.AnonymizeValue(originalValue, 0, columnIndices, "test.xlsx");

        // Assert
        Assert.Equal(result1, result2); // Should be consistent
        Assert.NotEqual(originalValue, result1); // Should be anonymized
        Assert.StartsWith("vm", result1.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests merging files with all possible options enabled.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithAllOptionsEnabled_ProcessesCorrectly()
    {
        // Arrange
        var file = TestDataGenerator.CreateValidRVToolsFile("all_options.xlsx", numVMs: 5);
        string[] filePaths = [file];
        string outputPath = GetOutputFilePath("all_options_output.xlsx");
        
        var options = new MergeOptions
        {
            IgnoreMissingOptionalSheets = true,
            SkipInvalidFiles = true,
            AnonymizeData = true,
            OnlyMandatoryColumns = true,
            IncludeSourceFileName = true,
            SkipRowsWithEmptyMandatoryValues = true,
            DebugMode = true
        };
        
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    /// <summary>
    /// Tests ExcelService with empty worksheets.
    /// </summary>
    [Fact]
    public void ExcelService_WithEmptyWorksheet_HandlesGracefully()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("EmptySheet");
        
        var commonColumns = new List<string> { "VM", "Powerstate" };

        // Act
        var mapping = ExcelService.GetColumnMapping(worksheet, commonColumns);
        var columnNames = ExcelService.GetColumnNames(worksheet);

        // Assert
        Assert.Empty(mapping);
        Assert.Empty(columnNames);
    }

    /// <summary>
    /// Tests ValidationService with different row data lengths.
    /// </summary>
    [Theory]
    [InlineData(1)]   // Single column
    [InlineData(3)]   // Few columns
    [InlineData(10)]  // Many columns
    [InlineData(20)]  // Lots of columns
    public void ValidationService_WithDifferentRowLengths_HandlesCorrectly(int columnCount)
    {
        // Arrange
        var rowData = new XLCellValue[columnCount];
        for (int i = 0; i < columnCount; i++)
        {
            rowData[i] = $"Value{i}";
        }
        
        var mandatoryColumns = new List<int> { 0 }; // Only first column mandatory

        // Act
        var hasEmptyValues = ValidationService.HasEmptyMandatoryValues(rowData, mandatoryColumns);

        // Assert
        Assert.False(hasEmptyValues, $"Row with {columnCount} populated columns should be valid");
    }

    /// <summary>
    /// Tests AnonymizationService statistics with multiple calls.
    /// </summary>
    [Fact]
    public void AnonymizationService_StatisticsWithMultipleCalls_TracksCorrectly()
    {
        // Arrange
        var service = new RVToolsMerge.Services.AnonymizationService();
        var vmColumnIndices = new Dictionary<string, int> { { "VM", 0 } };
        var hostColumnIndices = new Dictionary<string, int> { { "Host", 0 } };

        // Act - Anonymize various values
        for (int i = 1; i <= 5; i++)
        {
            service.AnonymizeValue((XLCellValue)$"vm{i}", 0, vmColumnIndices, "file1.xlsx");
        }
        
        for (int i = 1; i <= 3; i++)
        {
            service.AnonymizeValue((XLCellValue)$"host{i}", 0, hostColumnIndices, "file1.xlsx");
        }

        var stats = service.GetAnonymizationStatistics();

        // Assert
        Assert.True(stats.ContainsKey("VMs"));
        Assert.True(stats.ContainsKey("Hosts"));
        Assert.Equal(5, stats["VMs"]["file1.xlsx"]);
        Assert.Equal(3, stats["Hosts"]["file1.xlsx"]);
    }

    /// <summary>
    /// Tests merging files with different sheet availability.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithDifferentSheetAvailability_HandlesCorrectly()
    {
        // Arrange
        var fullFile = TestDataGenerator.CreateValidRVToolsFile("full_sheets.xlsx", numVMs: 3, includeAllSheets: true);
        var minimalFile = TestDataGenerator.CreateValidRVToolsFile("minimal_sheets.xlsx", numVMs: 2, includeAllSheets: false);
        
        string[] filePaths = [fullFile, minimalFile];
        string outputPath = GetOutputFilePath("different_sheets_output.xlsx");
        
        var options = CreateDefaultMergeOptions();
        options.IgnoreMissingOptionalSheets = true;
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    /// <summary>
    /// Tests ExcelService with sheets containing only header rows.
    /// </summary>
    [Fact]
    public void ExcelService_WithHeaderOnlySheets_HandlesCorrectly()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("HeaderOnlySheet");
        
        // Add only headers, no data rows
        worksheet.Cell(1, 1).Value = "VM";
        worksheet.Cell(1, 2).Value = "Powerstate";
        worksheet.Cell(1, 3).Value = "Host";
        
        var commonColumns = new List<string> { "VM", "Powerstate", "Host" };

        // Act
        var mapping = ExcelService.GetColumnMapping(worksheet, commonColumns);
        var columnNames = ExcelService.GetColumnNames(worksheet);

        // Assert
        Assert.Equal(3, mapping.Count);
        Assert.Equal(3, columnNames.Count);
    }

    /// <summary>
    /// Tests file operations with unusual file names.
    /// </summary>
    [Theory]
    [InlineData("file with spaces.xlsx")]
    [InlineData("file-with-dashes.xlsx")]
    [InlineData("file_with_underscores.xlsx")]
    [InlineData("file.with.dots.xlsx")]
    public async Task MergeFiles_WithUnusualFileNames_ProcessesCorrectly(string fileName)
    {
        // Arrange
        var file = TestDataGenerator.CreateValidRVToolsFile(fileName, numVMs: 2);
        string[] filePaths = [file];
        string outputPath = GetOutputFilePath($"output_{fileName}");
        var options = CreateDefaultMergeOptions();
        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filePaths, outputPath, options, validationIssues);

        // Assert
        Assert.True(File.Exists(outputPath));
    }

    /// <summary>
    /// Tests ValidationService with Azure Migrate specific validation scenarios.
    /// </summary>
    [Fact]
    public void ValidationService_AzureMigrateValidation_HandlesEdgeCases()
    {
        // Arrange
        var validRow = new XLCellValue[]
        {
            "TestVM",           // VM
            "valid-uuid",       // UUID  
            "Windows Server",   // OS
            ""                  // Other columns
        };

        var vmUuids = new HashSet<string>();
        int vmCount = 0;

        // Act
        var result = ValidationService.ValidateRowForAzureMigrate(
            validRow, 
            vmUuidIndex: 1, 
            osConfigIndex: 2, 
            seenVmUuids: vmUuids, 
            vmCount: vmCount);

        // Assert
        Assert.Null(result); // Should be valid
        // Note: The actual vmUuids set modification and count increment happen in calling code
    }

    /// <summary>
    /// Tests memory efficiency with mock large file operations.
    /// </summary>
    [Fact]
    public void MemoryEfficiency_WithLargeDataStructures_HandlesCorrectly()
    {
        // Arrange - Simulate large amounts of data
        var largeColumnList = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            largeColumnList.Add($"Column{i}");
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("LargeSheet");
        
        // Add many columns
        for (int i = 0; i < 100; i++)
        {
            worksheet.Cell(1, i + 1).Value = $"Column{i}";
        }

        // Act
        var mapping = ExcelService.GetColumnMapping(worksheet, largeColumnList);
        var columnNames = ExcelService.GetColumnNames(worksheet);

        // Assert
        Assert.Equal(100, mapping.Count);
        Assert.Equal(100, columnNames.Count);
    }
}