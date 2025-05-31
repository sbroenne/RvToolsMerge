//-----------------------------------------------------------------------
// <copyright file="ModelTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using RVToolsMerge.Models;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Tests for model classes to increase coverage.
/// </summary>
public class ModelTests
{
    [Fact]
    public void AzureMigrateValidationResult_PropertiesWork()
    {
        // Arrange & Act
        var rowData = new ClosedXML.Excel.XLCellValue[] { "TestVM", "TestHost" };
        var failure = new AzureMigrateValidationFailure(rowData, AzureMigrateValidationFailureReason.MissingVmUuid);
        var result = new AzureMigrateValidationResult
        {
            FailedRows = new List<AzureMigrateValidationFailure> { failure },
            MissingVmUuidCount = 1,
            TotalVmsProcessed = 100
        };
        
        // Assert
        Assert.Single(result.FailedRows);
        Assert.Equal(1, result.MissingVmUuidCount);
        Assert.Equal(100, result.TotalVmsProcessed);
        Assert.Equal(AzureMigrateValidationFailureReason.MissingVmUuid, result.FailedRows[0].Reason);
        Assert.Equal(rowData, result.FailedRows[0].RowData);
    }
    
    [Fact]
    public void AzureMigrateValidationFailure_PropertiesWork()
    {
        // Arrange & Act
        var rowData = new ClosedXML.Excel.XLCellValue[] { "TestVM", "2", "4096" };
        var failure = new AzureMigrateValidationFailure(rowData, AzureMigrateValidationFailureReason.DuplicateVmUuid);
        
        // Assert
        Assert.Equal(AzureMigrateValidationFailureReason.DuplicateVmUuid, failure.Reason);
        Assert.Equal(rowData, failure.RowData);
        Assert.Equal(3, failure.RowData.Length);
    }
    
    [Fact]
    public void AzureMigrateValidationResult_EmptyFailures_CreatesCorrectly()
    {
        // Arrange & Act
        var result = new AzureMigrateValidationResult
        {
            FailedRows = new List<AzureMigrateValidationFailure>(),
            MissingVmUuidCount = 0,
            VmCountLimitReached = false
        };
        
        // Assert
        Assert.Empty(result.FailedRows);
        Assert.Equal(0, result.MissingVmUuidCount);
        Assert.False(result.VmCountLimitReached);
        Assert.Equal(0, result.TotalFailedRows);
    }
    
    [Fact]
    public void AzureMigrateValidationResult_MultipleFailures_HandledCorrectly()
    {
        // Arrange & Act
        var rowData1 = new ClosedXML.Excel.XLCellValue[] { "VM1", "abc" };
        var rowData2 = new ClosedXML.Excel.XLCellValue[] { "VM2", "xyz" };
        var failure1 = new AzureMigrateValidationFailure(rowData1, AzureMigrateValidationFailureReason.MissingVmUuid);
        var failure2 = new AzureMigrateValidationFailure(rowData2, AzureMigrateValidationFailureReason.MissingOsConfiguration);
        
        var result = new AzureMigrateValidationResult
        {
            FailedRows = new List<AzureMigrateValidationFailure> { failure1, failure2 },
            MissingVmUuidCount = 1,
            MissingOsConfigurationCount = 1
        };
        
        // Assert
        Assert.Equal(2, result.FailedRows.Count);
        Assert.Equal(2, result.TotalFailedRows);
        Assert.Equal(1, result.MissingVmUuidCount);
        Assert.Equal(1, result.MissingOsConfigurationCount);
        Assert.Equal(AzureMigrateValidationFailureReason.MissingVmUuid, result.FailedRows[0].Reason);
        Assert.Equal(AzureMigrateValidationFailureReason.MissingOsConfiguration, result.FailedRows[1].Reason);
    }
    
    [Fact]
    public void AzureMigrateValidationFailure_AllReasons_Work()
    {
        // Arrange & Act
        var rowData = new ClosedXML.Excel.XLCellValue[] { "TestVM" };
        var failure1 = new AzureMigrateValidationFailure(rowData, AzureMigrateValidationFailureReason.MissingVmUuid);
        var failure2 = new AzureMigrateValidationFailure(rowData, AzureMigrateValidationFailureReason.MissingOsConfiguration);
        var failure3 = new AzureMigrateValidationFailure(rowData, AzureMigrateValidationFailureReason.DuplicateVmUuid);
        var failure4 = new AzureMigrateValidationFailure(rowData, AzureMigrateValidationFailureReason.VmCountExceeded);
        
        // Assert
        Assert.Equal(AzureMigrateValidationFailureReason.MissingVmUuid, failure1.Reason);
        Assert.Equal(AzureMigrateValidationFailureReason.MissingOsConfiguration, failure2.Reason);
        Assert.Equal(AzureMigrateValidationFailureReason.DuplicateVmUuid, failure3.Reason);
        Assert.Equal(AzureMigrateValidationFailureReason.VmCountExceeded, failure4.Reason);
    }
}