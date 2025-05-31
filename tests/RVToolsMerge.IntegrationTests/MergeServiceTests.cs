//-----------------------------------------------------------------------
// <copyright file="MergeServiceTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using System.IO.Abstractions.TestingHelpers;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using RVToolsMerge.Exceptions;
using RVToolsMerge.Models;
using RVToolsMerge.Services;
using RVToolsMerge.Services.Interfaces;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Tests for the MergeService class to increase coverage.
/// </summary>
public class MergeServiceTests
{
    private readonly MergeService _mergeService;
    private readonly MockFileSystem _mockFileSystem;
    private readonly string _testOutputPath;

    public MergeServiceTests()
    {
        _mockFileSystem = new MockFileSystem();
        
        // Setup services with mock file system
        var services = new ServiceCollection();
        services.AddSingleton<IExcelService, ExcelService>();
        services.AddSingleton<IValidationService>(provider =>
        {
            var excelService = provider.GetRequiredService<IExcelService>();
            return new MockValidationService(excelService, _mockFileSystem);
        });
        services.AddSingleton<IAnonymizationService, AnonymizationService>();
        services.AddSingleton<ConsoleUIService>();
        services.AddSingleton<System.IO.Abstractions.IFileSystem>(_mockFileSystem);
        
        var serviceProvider = services.BuildServiceProvider();
        
        _mergeService = new MergeService(
            serviceProvider.GetRequiredService<IExcelService>(),
            serviceProvider.GetRequiredService<IValidationService>(),
            serviceProvider.GetRequiredService<IAnonymizationService>(),
            serviceProvider.GetRequiredService<ConsoleUIService>(),
            _mockFileSystem);
            
        _testOutputPath = "/test/output/merged.xlsx";
        _mockFileSystem.AddDirectory("/test/output");
    }

    [Fact]
    public async Task MergeFilesAsync_EmptyFilePathsArray_ThrowsArgumentException()
    {
        // Arrange
        var filePaths = Array.Empty<string>();
        var options = new MergeOptions();
        var validationIssues = new List<ValidationIssue>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _mergeService.MergeFilesAsync(filePaths, _testOutputPath, options, validationIssues));
        
        Assert.Equal("No files specified for merging. (Parameter 'filePaths')", exception.Message);
    }

    [Fact]
    public async Task MergeFilesAsync_NonExistentFiles_ThrowsNoValidFilesException()
    {
        // Arrange
        var filePaths = new[] { "/test/nonexistent1.xlsx", "/test/nonexistent2.xlsx" };
        var options = new MergeOptions { SkipInvalidFiles = true, IgnoreMissingOptionalSheets = true };
        var validationIssues = new List<ValidationIssue>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NoValidFilesException>(
            () => _mergeService.MergeFilesAsync(filePaths, _testOutputPath, options, validationIssues));
        
        Assert.Equal("No valid files to process after validation.", exception.Message);
    }

    [Fact]
    public async Task MergeFilesAsync_WithValidFiles_EmptySheets_ProcessesSuccessfully()
    {
        // Arrange
        var filePath = "/test/input/test.xlsx";
        var filePaths = new[] { filePath };
        var options = new MergeOptions { SkipInvalidFiles = true, IgnoreMissingOptionalSheets = true };
        var validationIssues = new List<ValidationIssue>();

        // Create a test Excel file with valid structure but no data
        _mockFileSystem.AddDirectory("/test/input");
        await CreateTestExcelFileWithInvalidSheetsAsync(filePath);

        // Act
        await _mergeService.MergeFilesAsync(filePaths, _testOutputPath, options, validationIssues);

        // Assert
        Assert.True(_mockFileSystem.FileExists(_testOutputPath));
    }

    [Fact]
    public async Task MergeFilesAsync_WithValidFile_ProcessesSuccessfully()
    {
        // Arrange
        var filePath = "/test/input/test.xlsx";
        var filePaths = new[] { filePath };
        var options = new MergeOptions { SkipInvalidFiles = true, IgnoreMissingOptionalSheets = true };
        var validationIssues = new List<ValidationIssue>();

        // Create a test Excel file with valid vInfo sheet
        _mockFileSystem.AddDirectory("/test/input");
        await CreateTestExcelFileWithValidVInfoSheetAsync(filePath);

        // Act
        await _mergeService.MergeFilesAsync(filePaths, _testOutputPath, options, validationIssues);

        // Assert
        Assert.True(_mockFileSystem.FileExists(_testOutputPath));
    }

    [Fact]
    public async Task MergeFilesAsync_WithAnonymization_CreatesAnonymizationMapFile()
    {
        // Arrange
        var filePath = "/test/input/test.xlsx";
        var filePaths = new[] { filePath };
        var options = new MergeOptions { AnonymizeData = true, SkipInvalidFiles = true, IgnoreMissingOptionalSheets = true };
        var validationIssues = new List<ValidationIssue>();

        _mockFileSystem.AddDirectory("/test/input");
        await CreateTestExcelFileWithValidVInfoSheetAsync(filePath);

        // Act
        await _mergeService.MergeFilesAsync(filePaths, _testOutputPath, options, validationIssues);

        // Assert
        Assert.True(_mockFileSystem.FileExists(_testOutputPath));
        
        var expectedMapPath = "/test/output/merged_AnonymizationMapping.xlsx";
        Assert.True(_mockFileSystem.FileExists(expectedMapPath));
    }

    [Fact]
    public async Task MergeFilesAsync_WithAzureMigrateValidation_CreatesFailedValidationFile()
    {
        // Arrange
        var filePath = "/test/input/test.xlsx";
        var filePaths = new[] { filePath };
        var options = new MergeOptions { EnableAzureMigrateValidation = true, SkipInvalidFiles = true, IgnoreMissingOptionalSheets = true };
        var validationIssues = new List<ValidationIssue>();

        _mockFileSystem.AddDirectory("/test/input");
        await CreateTestExcelFileWithInvalidAzureMigrateDataAsync(filePath);

        // Act
        await _mergeService.MergeFilesAsync(filePaths, _testOutputPath, options, validationIssues);

        // Assert
        Assert.True(_mockFileSystem.FileExists(_testOutputPath));
        
        var expectedFailedPath = "/test/output/merged_FailedAzureMigrateValidation.xlsx";
        Assert.True(_mockFileSystem.FileExists(expectedFailedPath));
    }

    [Fact]
    public async Task MergeFilesAsync_WithMultipleFiles_ProcessesAllSuccessfully()
    {
        // Arrange
        var filePath1 = "/test/input/test1.xlsx";
        var filePath2 = "/test/input/test2.xlsx";
        var filePaths = new[] { filePath1, filePath2 };
        var options = new MergeOptions { SkipInvalidFiles = true, IgnoreMissingOptionalSheets = true };
        var validationIssues = new List<ValidationIssue>();

        _mockFileSystem.AddDirectory("/test/input");
        await CreateTestExcelFileWithValidVInfoSheetAsync(filePath1);
        await CreateTestExcelFileWithValidVInfoSheetAsync(filePath2);

        // Act
        await _mergeService.MergeFilesAsync(filePaths, _testOutputPath, options, validationIssues);

        // Assert
        Assert.True(_mockFileSystem.FileExists(_testOutputPath));
    }

    [Fact]
    public async Task MergeFilesAsync_WithDebugMode_ProcessesWithDebugInfo()
    {
        // Arrange
        var filePath = "/test/input/test.xlsx";
        var filePaths = new[] { filePath };
        var options = new MergeOptions { DebugMode = true, SkipInvalidFiles = true, IgnoreMissingOptionalSheets = true };
        var validationIssues = new List<ValidationIssue>();

        _mockFileSystem.AddDirectory("/test/input");
        await CreateTestExcelFileWithValidVInfoSheetAsync(filePath);

        // Act
        await _mergeService.MergeFilesAsync(filePaths, _testOutputPath, options, validationIssues);

        // Assert
        Assert.True(_mockFileSystem.FileExists(_testOutputPath));
    }

    [Fact]
    public async Task MergeFilesAsync_WithInvalidFileInList_SkipsInvalidFile()
    {
        // Arrange
        var validFilePath = "/test/input/valid.xlsx";
        var invalidFilePath = "/test/input/invalid.txt";
        var filePaths = new[] { validFilePath, invalidFilePath };
        var options = new MergeOptions { SkipInvalidFiles = true, IgnoreMissingOptionalSheets = true };
        var validationIssues = new List<ValidationIssue>();

        _mockFileSystem.AddDirectory("/test/input");
        await CreateTestExcelFileWithValidVInfoSheetAsync(validFilePath);
        _mockFileSystem.AddFile(invalidFilePath, new MockFileData("not an excel file"));

        // Act
        await _mergeService.MergeFilesAsync(filePaths, _testOutputPath, options, validationIssues);

        // Assert
        Assert.True(_mockFileSystem.FileExists(_testOutputPath));
        Assert.NotEmpty(validationIssues);
    }

    [Fact]
    public async Task MergeFilesAsync_WithSpecificOptions_ProcessesSuccessfully()
    {
        // Arrange
        var filePath = "/test/input/test.xlsx";
        var filePaths = new[] { filePath };
        var options = new MergeOptions { OnlyMandatoryColumns = true, SkipInvalidFiles = true, IgnoreMissingOptionalSheets = true };
        var validationIssues = new List<ValidationIssue>();

        _mockFileSystem.AddDirectory("/test/input");
        await CreateTestExcelFileWithMultipleSheetsAsync(filePath);

        // Act
        await _mergeService.MergeFilesAsync(filePaths, _testOutputPath, options, validationIssues);

        // Assert
        Assert.True(_mockFileSystem.FileExists(_testOutputPath));
    }

    /// <summary>
    /// Creates a test Excel file with invalid sheets for testing exception scenarios.
    /// </summary>
    private Task CreateTestExcelFileWithInvalidSheetsAsync(string filePath)
    {
        using var workbook = new XLWorkbook();
        
        // Create a vInfo sheet with mandatory columns but no data rows
        // This should pass file validation but fail when trying to process data
        var vInfoSheet = workbook.Worksheets.Add("vInfo");
        
        // Add all mandatory columns for vInfo sheet but no data
        vInfoSheet.Cell(1, 1).Value = "VM UUID";
        vInfoSheet.Cell(1, 2).Value = "VM";
        vInfoSheet.Cell(1, 3).Value = "Template";
        vInfoSheet.Cell(1, 4).Value = "SRM Placeholder";
        vInfoSheet.Cell(1, 5).Value = "Powerstate";
        vInfoSheet.Cell(1, 6).Value = "CPUs";
        vInfoSheet.Cell(1, 7).Value = "Memory";
        vInfoSheet.Cell(1, 8).Value = "NICs";
        vInfoSheet.Cell(1, 9).Value = "Disks";
        vInfoSheet.Cell(1, 10).Value = "In Use MiB";
        vInfoSheet.Cell(1, 11).Value = "Provisioned MiB";
        vInfoSheet.Cell(1, 12).Value = "OS according to the configuration file";
        vInfoSheet.Cell(1, 13).Value = "Creation date";
        
        // Don't add any data rows - this should result in "no valid sheets" after processing
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        _mockFileSystem.AddFile(filePath, new MockFileData(stream.ToArray()));
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a test Excel file with a valid vInfo sheet.
    /// </summary>
    private Task CreateTestExcelFileWithValidVInfoSheetAsync(string filePath)
    {
        using var workbook = new XLWorkbook();
        
        var worksheet = workbook.Worksheets.Add("vInfo");
        
        // Add all mandatory columns for vInfo sheet
        worksheet.Cell(1, 1).Value = "VM UUID";
        worksheet.Cell(1, 2).Value = "VM";
        worksheet.Cell(1, 3).Value = "Template";
        worksheet.Cell(1, 4).Value = "SRM Placeholder";
        worksheet.Cell(1, 5).Value = "Powerstate";
        worksheet.Cell(1, 6).Value = "CPUs";
        worksheet.Cell(1, 7).Value = "Memory";
        worksheet.Cell(1, 8).Value = "NICs";
        worksheet.Cell(1, 9).Value = "Disks";
        worksheet.Cell(1, 10).Value = "In Use MiB";
        worksheet.Cell(1, 11).Value = "Provisioned MiB";
        worksheet.Cell(1, 12).Value = "OS according to the configuration file";
        worksheet.Cell(1, 13).Value = "Creation date";
        worksheet.Cell(1, 14).Value = "Host";
        worksheet.Cell(1, 15).Value = "vCenter";
        
        // Add test data
        worksheet.Cell(2, 1).Value = "uuid-1234";
        worksheet.Cell(2, 2).Value = "TestVM1";
        worksheet.Cell(2, 3).Value = "ubuntu-template";
        worksheet.Cell(2, 4).Value = "false";
        worksheet.Cell(2, 5).Value = "poweredOn";
        worksheet.Cell(2, 6).Value = "2";
        worksheet.Cell(2, 7).Value = "4096";
        worksheet.Cell(2, 8).Value = "1";
        worksheet.Cell(2, 9).Value = "1";
        worksheet.Cell(2, 10).Value = "2048";
        worksheet.Cell(2, 11).Value = "4096";
        worksheet.Cell(2, 12).Value = "Ubuntu Linux";
        worksheet.Cell(2, 13).Value = "2023-01-01";
        worksheet.Cell(2, 14).Value = "TestHost1";
        worksheet.Cell(2, 15).Value = "TestvCenter1";
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        _mockFileSystem.AddFile(filePath, new MockFileData(stream.ToArray()));
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a test Excel file with invalid data for Azure Migrate validation.
    /// </summary>
    private Task CreateTestExcelFileWithInvalidAzureMigrateDataAsync(string filePath)
    {
        using var workbook = new XLWorkbook();
        
        var worksheet = workbook.Worksheets.Add("vInfo");
        
        // Add all mandatory columns for vInfo sheet
        worksheet.Cell(1, 1).Value = "VM UUID";
        worksheet.Cell(1, 2).Value = "VM";
        worksheet.Cell(1, 3).Value = "Template";
        worksheet.Cell(1, 4).Value = "SRM Placeholder";
        worksheet.Cell(1, 5).Value = "Powerstate";
        worksheet.Cell(1, 6).Value = "CPUs";
        worksheet.Cell(1, 7).Value = "Memory";
        worksheet.Cell(1, 8).Value = "NICs";
        worksheet.Cell(1, 9).Value = "Disks";
        worksheet.Cell(1, 10).Value = "In Use MiB";
        worksheet.Cell(1, 11).Value = "Provisioned MiB";
        worksheet.Cell(1, 12).Value = "OS according to the configuration file";
        worksheet.Cell(1, 13).Value = "Creation date";
        
        // Add test data with missing VM UUID (to trigger Azure Migrate validation failure)
        worksheet.Cell(2, 1).Value = ""; // Empty VM UUID - this should trigger MissingVmUuid failure
        worksheet.Cell(2, 2).Value = "TestVM1";
        worksheet.Cell(2, 3).Value = "ubuntu-template";
        worksheet.Cell(2, 4).Value = "false";
        worksheet.Cell(2, 5).Value = "poweredOn";
        worksheet.Cell(2, 6).Value = "2";
        worksheet.Cell(2, 7).Value = "4096";
        worksheet.Cell(2, 8).Value = "1";
        worksheet.Cell(2, 9).Value = "1";
        worksheet.Cell(2, 10).Value = "2048";
        worksheet.Cell(2, 11).Value = "4096";
        worksheet.Cell(2, 12).Value = ""; // Empty OS configuration - this should trigger MissingOsConfiguration failure
        worksheet.Cell(2, 13).Value = "2023-01-01";
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        _mockFileSystem.AddFile(filePath, new MockFileData(stream.ToArray()));
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a test Excel file with multiple sheets.
    /// </summary>
    private Task CreateTestExcelFileWithMultipleSheetsAsync(string filePath)
    {
        using var workbook = new XLWorkbook();
        
        // Add vInfo sheet with all mandatory columns
        var vInfoSheet = workbook.Worksheets.Add("vInfo");
        vInfoSheet.Cell(1, 1).Value = "VM UUID";
        vInfoSheet.Cell(1, 2).Value = "VM";
        vInfoSheet.Cell(1, 3).Value = "Template";
        vInfoSheet.Cell(1, 4).Value = "SRM Placeholder";
        vInfoSheet.Cell(1, 5).Value = "Powerstate";
        vInfoSheet.Cell(1, 6).Value = "CPUs";
        vInfoSheet.Cell(1, 7).Value = "Memory";
        vInfoSheet.Cell(1, 8).Value = "NICs";
        vInfoSheet.Cell(1, 9).Value = "Disks";
        vInfoSheet.Cell(1, 10).Value = "In Use MiB";
        vInfoSheet.Cell(1, 11).Value = "Provisioned MiB";
        vInfoSheet.Cell(1, 12).Value = "OS according to the configuration file";
        vInfoSheet.Cell(1, 13).Value = "Creation date";
        vInfoSheet.Cell(1, 14).Value = "Host";
        
        vInfoSheet.Cell(2, 1).Value = "uuid-1234";
        vInfoSheet.Cell(2, 2).Value = "TestVM1";
        vInfoSheet.Cell(2, 3).Value = "ubuntu-template";
        vInfoSheet.Cell(2, 4).Value = "false";
        vInfoSheet.Cell(2, 5).Value = "poweredOn";
        vInfoSheet.Cell(2, 6).Value = "2";
        vInfoSheet.Cell(2, 7).Value = "4096";
        vInfoSheet.Cell(2, 8).Value = "1";
        vInfoSheet.Cell(2, 9).Value = "1";
        vInfoSheet.Cell(2, 10).Value = "2048";
        vInfoSheet.Cell(2, 11).Value = "4096";
        vInfoSheet.Cell(2, 12).Value = "Ubuntu Linux";
        vInfoSheet.Cell(2, 13).Value = "2023-01-01";
        vInfoSheet.Cell(2, 14).Value = "TestHost1";
        
        // Add vCPU sheet (optional)
        var vCpuSheet = workbook.Worksheets.Add("vCPU");
        vCpuSheet.Cell(1, 1).Value = "VM";
        vCpuSheet.Cell(1, 2).Value = "CPUs";
        vCpuSheet.Cell(2, 1).Value = "TestVM1";
        vCpuSheet.Cell(2, 2).Value = "2";
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        _mockFileSystem.AddFile(filePath, new MockFileData(stream.ToArray()));
        
        return Task.CompletedTask;
    }
}

/// <summary>
/// Mock validation service that works with mock file system for testing.
/// </summary>
public class MockValidationService : IValidationService
{
    private readonly IExcelService _excelService;
    private readonly MockFileSystem _mockFileSystem;

    public MockValidationService(IExcelService excelService, MockFileSystem mockFileSystem)
    {
        _excelService = excelService;
        _mockFileSystem = mockFileSystem;
    }

    public bool ValidateFile(string filePath, bool ignoreMissingOptionalSheets, List<ValidationIssue> issues)
    {
        try
        {
            using var workbook = _excelService.OpenWorkbook(filePath);
            
            // Simple validation - just check if vInfo sheet exists with required columns
            var vInfoSheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("vInfo", StringComparison.OrdinalIgnoreCase));
            if (vInfoSheet == null)
            {
                issues.Add(new ValidationIssue(
                    System.IO.Path.GetFileName(filePath),
                    true,
                    "Missing essential 'vInfo' sheet which is required for processing."
                ));
                return false;
            }

            // Check for mandatory columns in vInfo sheet
            var requiredColumns = new[] { "VM UUID", "VM", "Template", "SRM Placeholder", "Powerstate", "CPUs", "Memory", "NICs", "Disks", "In Use MiB", "Provisioned MiB", "OS according to the configuration file", "Creation date" };
            var headerRow = vInfoSheet.Row(1);
            var presentColumns = new List<string>();
            
            for (int col = 1; col <= (headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0); col++)
            {
                var cellValue = headerRow.Cell(col).GetString();
                if (!string.IsNullOrWhiteSpace(cellValue))
                {
                    presentColumns.Add(cellValue);
                }
            }

            var missingColumns = requiredColumns.Where(col => !presentColumns.Contains(col)).ToArray();
            if (missingColumns.Length > 0)
            {
                issues.Add(new ValidationIssue(
                    System.IO.Path.GetFileName(filePath),
                    true,
                    $"'vInfo' sheet is missing mandatory column(s): {string.Join(", ", missingColumns)}"
                ));
                return false;
            }

            // Check for missing optional sheets only if not ignoring them
            if (!ignoreMissingOptionalSheets)
            {
                var optionalSheets = new[] { "vHost", "vPartition", "vMemory" };
                foreach (var sheetName in optionalSheets)
                {
                    if (!workbook.Worksheets.Any(w => w.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase)))
                    {
                        issues.Add(new ValidationIssue(
                            System.IO.Path.GetFileName(filePath),
                            false,
                            $"Missing optional sheet '{sheetName}'"
                        ));
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            issues.Add(new ValidationIssue(
                System.IO.Path.GetFileName(filePath),
                true,
                $"Unexpected error validating file: {ex.Message}"
            ));
            return false;
        }
    }

    public bool HasEmptyMandatoryValues(XLCellValue[] rowData, List<int> mandatoryColumnIndices)
    {
        return mandatoryColumnIndices.Any(index => 
            index < rowData.Length && 
            (rowData[index].IsBlank || string.IsNullOrWhiteSpace(rowData[index].GetText())));
    }

    public AzureMigrateValidationFailureReason? ValidateRowForAzureMigrate(
        XLCellValue[] rowData,
        int vmUuidIndex,
        int osConfigIndex,
        HashSet<string> seenVmUuids,
        int vmCount)
    {
        // Check VM count limit
        if (vmCount >= 20000)
        {
            return AzureMigrateValidationFailureReason.VmCountExceeded;
        }

        // Check for missing VM UUID
        if (vmUuidIndex < rowData.Length && 
            (rowData[vmUuidIndex].IsBlank || string.IsNullOrWhiteSpace(rowData[vmUuidIndex].GetText())))
        {
            return AzureMigrateValidationFailureReason.MissingVmUuid;
        }

        // Check for missing OS configuration  
        if (osConfigIndex < rowData.Length && 
            (rowData[osConfigIndex].IsBlank || string.IsNullOrWhiteSpace(rowData[osConfigIndex].GetText())))
        {
            return AzureMigrateValidationFailureReason.MissingOsConfiguration;
        }

        // Check for duplicate VM UUID
        var vmUuid = rowData[vmUuidIndex].GetText();
        if (!string.IsNullOrWhiteSpace(vmUuid) && seenVmUuids.Contains(vmUuid))
        {
            return AzureMigrateValidationFailureReason.DuplicateVmUuid;
        }

        return null; // No validation failures
    }
}