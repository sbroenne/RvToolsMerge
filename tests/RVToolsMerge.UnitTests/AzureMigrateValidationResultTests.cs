//-----------------------------------------------------------------------
// <copyright file="AzureMigrateValidationResultTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using ClosedXML.Excel;

namespace RVToolsMerge.UnitTests;

/// <summary>
/// Unit tests for Azure Migrate validation model classes
/// </summary>
public class AzureMigrateValidationResultTests
{
    [Fact]
    public void AzureMigrateValidationFailure_Constructor_SetsProperties()
    {
        // Arrange
        var rowData = new XLCellValue[] { "VM1", "Windows" };
        const AzureMigrateValidationFailureReason reason = AzureMigrateValidationFailureReason.MissingVmUuid;

        // Act
        var failure = new AzureMigrateValidationFailure(rowData, reason);

        // Assert
        Assert.Equal(rowData, failure.RowData);
        Assert.Equal(reason, failure.Reason);
    }

    [Fact]
    public void AzureMigrateValidationResult_DefaultConstructor_InitializesProperties()
    {
        // Act
        var result = new AzureMigrateValidationResult();

        // Assert
        Assert.NotNull(result.FailedRows);
        Assert.Empty(result.FailedRows);
        Assert.Equal(0, result.MissingVmUuidCount);
        Assert.Equal(0, result.MissingOsConfigurationCount);
        Assert.Equal(0, result.DuplicateVmUuidCount);
        Assert.Equal(0, result.VmCountExceededCount);
        Assert.Equal(0, result.TotalVmsProcessed);
        Assert.False(result.VmCountLimitReached);
        Assert.Equal(0, result.RowsSkippedAfterLimitReached);
        Assert.Equal(0, result.TotalFailedRows);
    }

    [Fact]
    public void AzureMigrateValidationResult_TotalFailedRows_ReturnsFailedRowsCount()
    {
        // Arrange
        var result = new AzureMigrateValidationResult();
        var rowData1 = new XLCellValue[] { "VM1" };
        var rowData2 = new XLCellValue[] { "VM2" };
        var failure1 = new AzureMigrateValidationFailure(rowData1, AzureMigrateValidationFailureReason.MissingVmUuid);
        var failure2 = new AzureMigrateValidationFailure(rowData2, AzureMigrateValidationFailureReason.MissingOsConfiguration);

        // Act
        result.FailedRows.Add(failure1);
        result.FailedRows.Add(failure2);

        // Assert
        Assert.Equal(2, result.TotalFailedRows);
    }

    [Fact]
    public void AzureMigrateValidationResult_PropertiesCanBeSet()
    {
        // Arrange
        var result = new AzureMigrateValidationResult();

        // Act
        result.MissingVmUuidCount = 5;
        result.MissingOsConfigurationCount = 3;
        result.DuplicateVmUuidCount = 2;
        result.VmCountExceededCount = 1;
        result.TotalVmsProcessed = 100;
        result.VmCountLimitReached = true;
        result.RowsSkippedAfterLimitReached = 10;

        // Assert
        Assert.Equal(5, result.MissingVmUuidCount);
        Assert.Equal(3, result.MissingOsConfigurationCount);
        Assert.Equal(2, result.DuplicateVmUuidCount);
        Assert.Equal(1, result.VmCountExceededCount);
        Assert.Equal(100, result.TotalVmsProcessed);
        Assert.True(result.VmCountLimitReached);
        Assert.Equal(10, result.RowsSkippedAfterLimitReached);
    }

    [Theory]
    [InlineData(AzureMigrateValidationFailureReason.MissingVmUuid)]
    [InlineData(AzureMigrateValidationFailureReason.MissingOsConfiguration)]
    [InlineData(AzureMigrateValidationFailureReason.DuplicateVmUuid)]
    [InlineData(AzureMigrateValidationFailureReason.VmCountExceeded)]
    public void AzureMigrateValidationFailureReason_AllValuesValid(AzureMigrateValidationFailureReason reason)
    {
        // Arrange
        var rowData = new XLCellValue[] { "test" };

        // Act
        var failure = new AzureMigrateValidationFailure(rowData, reason);

        // Assert
        Assert.Equal(reason, failure.Reason);
    }
}