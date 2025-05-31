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
public class MergeServiceTests : IntegrationTestBase
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
        services.AddSingleton<IValidationService, ValidationService>();
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
        var options = new MergeOptions();
        var validationIssues = new List<ValidationIssue>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NoValidFilesException>(
            () => _mergeService.MergeFilesAsync(filePaths, _testOutputPath, options, validationIssues));
        
        Assert.Equal("No valid files to process after validation.", exception.Message);
    }

    [Fact]
    public async Task MergeFilesAsync_WithValidFiles_NoValidSheetsFound_ThrowsNoValidSheetsException()
    {
        // Arrange
        var filePath = "/test/input/test.xlsx";
        var filePaths = new[] { filePath };
        var options = new MergeOptions();
        var validationIssues = new List<ValidationIssue>();

        // Create a test Excel file with no valid sheets
        _mockFileSystem.AddDirectory("/test/input");
        await CreateTestExcelFileWithInvalidSheetsAsync(filePath);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NoValidSheetsException>(
            () => _mergeService.MergeFilesAsync(filePaths, _testOutputPath, options, validationIssues));
        
        Assert.Equal("No valid sheets found across the input files.", exception.Message);
    }

    [Fact]
    public async Task MergeFilesAsync_WithValidFile_ProcessesSuccessfully()
    {
        // Arrange
        var filePath = "/test/input/test.xlsx";
        var filePaths = new[] { filePath };
        var options = new MergeOptions();
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
        var options = new MergeOptions { AnonymizeData = true };
        var validationIssues = new List<ValidationIssue>();

        _mockFileSystem.AddDirectory("/test/input");
        await CreateTestExcelFileWithValidVInfoSheetAsync(filePath);

        // Act
        await _mergeService.MergeFilesAsync(filePaths, _testOutputPath, options, validationIssues);

        // Assert
        Assert.True(_mockFileSystem.FileExists(_testOutputPath));
        
        var expectedMapPath = "/test/output/merged_AnonymizationMap.txt";
        Assert.True(_mockFileSystem.FileExists(expectedMapPath));
    }

    [Fact]
    public async Task MergeFilesAsync_WithAzureMigrateValidation_CreatesFailedValidationFile()
    {
        // Arrange
        var filePath = "/test/input/test.xlsx";
        var filePaths = new[] { filePath };
        var options = new MergeOptions { EnableAzureMigrateValidation = true };
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
        var options = new MergeOptions();
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
        var options = new MergeOptions { DebugMode = true };
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
        var options = new MergeOptions();
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
        var options = new MergeOptions { OnlyMandatoryColumns = true };
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
    private async Task CreateTestExcelFileWithInvalidSheetsAsync(string filePath)
    {
        using var workbook = new XLWorkbook();
        
        // Add an empty sheet that won't be recognized as valid
        var worksheet = workbook.Worksheets.Add("InvalidSheet");
        worksheet.Cell(1, 1).Value = "Invalid";
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        _mockFileSystem.AddFile(filePath, new MockFileData(stream.ToArray()));
    }

    /// <summary>
    /// Creates a test Excel file with a valid vInfo sheet.
    /// </summary>
    private async Task CreateTestExcelFileWithValidVInfoSheetAsync(string filePath)
    {
        using var workbook = new XLWorkbook();
        
        var worksheet = workbook.Worksheets.Add("vInfo");
        worksheet.Cell(1, 1).Value = "VM";
        worksheet.Cell(1, 2).Value = "Host";
        worksheet.Cell(1, 3).Value = "vCenter";
        worksheet.Cell(2, 1).Value = "TestVM1";
        worksheet.Cell(2, 2).Value = "TestHost1";
        worksheet.Cell(2, 3).Value = "TestvCenter1";
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        _mockFileSystem.AddFile(filePath, new MockFileData(stream.ToArray()));
    }

    /// <summary>
    /// Creates a test Excel file with invalid data for Azure Migrate validation.
    /// </summary>
    private async Task CreateTestExcelFileWithInvalidAzureMigrateDataAsync(string filePath)
    {
        using var workbook = new XLWorkbook();
        
        var worksheet = workbook.Worksheets.Add("vInfo");
        worksheet.Cell(1, 1).Value = "VM";
        worksheet.Cell(1, 2).Value = "CPUs";
        worksheet.Cell(1, 3).Value = "Memory";
        worksheet.Cell(2, 1).Value = "TestVM1";
        worksheet.Cell(2, 2).Value = "invalid_cpu"; // Invalid data for Azure Migrate
        worksheet.Cell(2, 3).Value = "invalid_memory"; // Invalid data for Azure Migrate
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        _mockFileSystem.AddFile(filePath, new MockFileData(stream.ToArray()));
    }

    /// <summary>
    /// Creates a test Excel file with multiple sheets.
    /// </summary>
    private async Task CreateTestExcelFileWithMultipleSheetsAsync(string filePath)
    {
        using var workbook = new XLWorkbook();
        
        // Add vInfo sheet
        var vInfoSheet = workbook.Worksheets.Add("vInfo");
        vInfoSheet.Cell(1, 1).Value = "VM";
        vInfoSheet.Cell(1, 2).Value = "Host";
        vInfoSheet.Cell(2, 1).Value = "TestVM1";
        vInfoSheet.Cell(2, 2).Value = "TestHost1";
        
        // Add vCPU sheet
        var vCpuSheet = workbook.Worksheets.Add("vCPU");
        vCpuSheet.Cell(1, 1).Value = "VM";
        vCpuSheet.Cell(1, 2).Value = "CPUs";
        vCpuSheet.Cell(2, 1).Value = "TestVM1";
        vCpuSheet.Cell(2, 2).Value = "2";
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        _mockFileSystem.AddFile(filePath, new MockFileData(stream.ToArray()));
    }
}