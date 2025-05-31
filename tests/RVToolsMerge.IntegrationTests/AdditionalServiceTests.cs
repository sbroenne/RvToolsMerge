//-----------------------------------------------------------------------
// <copyright file="AdditionalServiceTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using ClosedXML.Excel;
using RVToolsMerge.Models;
using RVToolsMerge.Services;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Additional tests for services to increase coverage in uncovered areas.
/// </summary>
public class AdditionalServiceTests : IntegrationTestBase
{
    [Fact]
    public void ExcelService_GetColumnInformationOptimized_WithValidWorksheet_ReturnsColumnMappings()
    {
        // Arrange
        var service = new ExcelService(new System.IO.Abstractions.FileSystem());
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("TestSheet");
        
        // Setup headers
        worksheet.Cell(1, 1).Value = "VM";
        worksheet.Cell(1, 2).Value = "Host";
        worksheet.Cell(1, 3).Value = "vCenter";
        
        // Setup some data
        worksheet.Cell(2, 1).Value = "TestVM1";
        worksheet.Cell(2, 2).Value = "TestHost1";
        worksheet.Cell(2, 3).Value = "TestvCenter1";
        
        var expectedColumns = new List<string> { "VM", "Host", "vCenter" };
        
        // Act
        var result = service.GetColumnInformationOptimized(worksheet, expectedColumns);
        
        // Assert
        Assert.Equal(3, result.ColumnNames.Count);
        Assert.Equal(3, result.ColumnMappings.Count);
        Assert.Contains("VM", result.ColumnNames);
        Assert.Contains("Host", result.ColumnNames);
        Assert.Contains("vCenter", result.ColumnNames);
    }

    [Fact]
    public void ExcelService_GetColumnInformationOptimized_WithMissingColumns_ReturnsPartialMappings()
    {
        // Arrange
        var service = new ExcelService(new System.IO.Abstractions.FileSystem());
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("TestSheet");
        
        // Setup only some headers
        worksheet.Cell(1, 1).Value = "VM";
        worksheet.Cell(1, 3).Value = "vCenter"; // Skip column 2
        
        var expectedColumns = new List<string> { "VM", "Host", "vCenter" };
        
        // Act
        var result = service.GetColumnInformationOptimized(worksheet, expectedColumns);
        
        // Assert
        Assert.Equal(2, result.ColumnNames.Count); // Only VM and vCenter should be found
        Assert.Equal(2, result.ColumnMappings.Count); // Only VM and vCenter should be mapped
        Assert.Contains("VM", result.ColumnNames);
        Assert.Contains("vCenter", result.ColumnNames);
        Assert.DoesNotContain("Host", result.ColumnNames);
    }

    [Fact]
    public void ExcelService_GetColumnInformationOptimized_WithEmptyWorksheet_ReturnsEmptyMappings()
    {
        // Arrange
        var service = new ExcelService(new System.IO.Abstractions.FileSystem());
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("EmptySheet");
        
        var expectedColumns = new List<string> { "VM", "Host", "vCenter" };
        
        // Act
        var result = service.GetColumnInformationOptimized(worksheet, expectedColumns);
        
        // Assert
        Assert.Empty(result.ColumnNames);
        Assert.Empty(result.ColumnMappings);
    }

    [Fact]
    public void ExcelService_GetColumnInformationOptimized_WithExtraColumns_IgnoresUnexpectedColumns()
    {
        // Arrange
        var service = new ExcelService(new System.IO.Abstractions.FileSystem());
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("TestSheet");
        
        // Setup headers including extra ones
        worksheet.Cell(1, 1).Value = "VM";
        worksheet.Cell(1, 2).Value = "ExtraColumn1";
        worksheet.Cell(1, 3).Value = "Host";
        worksheet.Cell(1, 4).Value = "ExtraColumn2";
        worksheet.Cell(1, 5).Value = "vCenter";
        
        var expectedColumns = new List<string> { "VM", "Host", "vCenter" };
        
        // Act
        var result = service.GetColumnInformationOptimized(worksheet, expectedColumns);
        
        // Assert
        Assert.Equal(5, result.ColumnNames.Count); // All columns found
        Assert.Equal(3, result.ColumnMappings.Count); // Only expected columns mapped
        Assert.Contains("VM", result.ColumnNames);
        Assert.Contains("Host", result.ColumnNames);
        Assert.Contains("vCenter", result.ColumnNames);
        Assert.Contains("ExtraColumn1", result.ColumnNames);
        Assert.Contains("ExtraColumn2", result.ColumnNames);
    }

    [Fact]
    public void ValidationService_ValidateRowForAzureMigrate_WithValidData_ReturnsNull()
    {
        // Arrange
        var excelService = new ExcelService(new System.IO.Abstractions.FileSystem());
        var service = new ValidationService(excelService);
        var rowData = new XLCellValue[]
        {
            "TestVM",      // VM name
            "2",          // CPUs - valid number
            "4096",       // Memory - valid number
            "Active",     // State
            "TestHost"    // Host
        };
        var vmUuidColumnIndex = 0;
        var osConfigurationColumnIndex = 3;
        var usedVmUuids = new HashSet<string>();
        var rowNumber = 2;
        
        // Act
        var result = service.ValidateRowForAzureMigrate(rowData, vmUuidColumnIndex, osConfigurationColumnIndex, usedVmUuids, rowNumber);
        
        // Assert
        Assert.Null(result);
        Assert.Contains("TestVM", usedVmUuids);
    }

    [Fact]
    public void ValidationService_ValidateRowForAzureMigrate_WithMissingVmUuid_ReturnsExpectedReason()
    {
        // Arrange
        var excelService = new ExcelService(new System.IO.Abstractions.FileSystem());
        var service = new ValidationService(excelService);
        var rowData = new XLCellValue[]
        {
            "",           // Empty VM name
            "2",          // CPUs
            "4096",       // Memory
            "Active",     // State
            "TestHost"    // Host
        };
        var vmUuidColumnIndex = 0;
        var osConfigurationColumnIndex = 3;
        var usedVmUuids = new HashSet<string>();
        var rowNumber = 2;
        
        // Act
        var result = service.ValidateRowForAzureMigrate(rowData, vmUuidColumnIndex, osConfigurationColumnIndex, usedVmUuids, rowNumber);
        
        // Assert
        Assert.Equal(Models.AzureMigrateValidationFailureReason.MissingVmUuid, result);
    }

    [Fact]
    public void ValidationService_ValidateRowForAzureMigrate_WithDuplicateVmUuid_ReturnsExpectedReason()
    {
        // Arrange
        var excelService = new ExcelService(new System.IO.Abstractions.FileSystem());
        var service = new ValidationService(excelService);
        var rowData = new XLCellValue[]
        {
            "TestVM",      // VM name - will be duplicate
            "2",          // CPUs
            "4096",       // Memory
            "Active",     // State
            "TestHost"    // Host
        };
        var vmUuidColumnIndex = 0;
        var osConfigurationColumnIndex = 3;
        var usedVmUuids = new HashSet<string> { "TestVM" }; // Already contains TestVM
        var rowNumber = 2;
        
        // Act
        var result = service.ValidateRowForAzureMigrate(rowData, vmUuidColumnIndex, osConfigurationColumnIndex, usedVmUuids, rowNumber);
        
        // Assert
        Assert.Equal(Models.AzureMigrateValidationFailureReason.DuplicateVmUuid, result);
    }

    [Fact]
    public void ValidationService_ValidateRowForAzureMigrate_WithMissingOsConfiguration_ReturnsExpectedReason()
    {
        // Arrange
        var excelService = new ExcelService(new System.IO.Abstractions.FileSystem());
        var service = new ValidationService(excelService);
        var rowData = new XLCellValue[]
        {
            "TestVM",      // VM name
            "2",          // CPUs
            "4096",       // Memory
            ""            // Missing OS configuration
        };
        var vmUuidColumnIndex = 0;
        var osConfigurationColumnIndex = 3;
        var usedVmUuids = new HashSet<string>();
        var rowNumber = 2;
        
        // Act
        var result = service.ValidateRowForAzureMigrate(rowData, vmUuidColumnIndex, osConfigurationColumnIndex, usedVmUuids, rowNumber);
        
        // Assert
        Assert.Equal(Models.AzureMigrateValidationFailureReason.MissingOsConfiguration, result);
    }
}