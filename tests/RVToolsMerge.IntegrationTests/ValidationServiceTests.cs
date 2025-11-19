//-----------------------------------------------------------------------
// <copyright file="ValidationServiceTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using ClosedXML.Excel;
using RVToolsMerge.Models;

namespace RVToolsMerge.IntegrationTests;

[Collection("SpectreConsole")]
/// <summary>
/// Tests specifically targeting ValidationService methods.
/// </summary>
public class ValidationServiceTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that HasEmptyMandatoryValues correctly identifies empty values in mandatory columns.
    /// </summary>
    [Fact]
    public void HasEmptyMandatoryValues_WithEmptyMandatoryColumn_ReturnsTrue()
    {
        // Arrange
        var rowData = new XLCellValue[]
        {
            "VM01",         // VM Name - index 0
            "poweredOn",    // Powerstate - index 1
            "FALSE",        // Template - index 2
            string.Empty,   // CPUs - index 3 - EMPTY
            "4096",         // Memory - index 4
            "2048",         // In Use MiB - index 5
            "Windows",      // OS - index 6
            "FALSE"         // SRM Placeholder - index 7
        };

        // Mandatory column indices - including index 3 (CPUs) which is empty
        var mandatoryColumnIndices = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };

        // Act
        bool result = ValidationService.HasEmptyMandatoryValues(rowData, mandatoryColumnIndices);

        // Assert
        Assert.True(result, "Should detect empty value in mandatory column");
    }

    /// <summary>
    /// Tests that HasEmptyMandatoryValues correctly handles null values.
    /// </summary>
    [Fact]
    public void HasEmptyMandatoryValues_WithNullMandatoryColumn_ReturnsTrue()
    {
        // Arrange
        var rowData = new XLCellValue[]
        {
            "VM01",                     // VM Name - index 0
            "poweredOn",                // Powerstate - index 1
            "FALSE",                    // Template - index 2
            XLCellValue.FromObject(null), // CPUs - index 3 - NULL
            "4096",                     // Memory - index 4
            "2048",                     // In Use MiB - index 5
            "Windows",                  // OS - index 6
            "FALSE"                     // SRM Placeholder - index 7
        };

        // Mandatory column indices - including index 3 (CPUs) which is null
        var mandatoryColumnIndices = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };

        // Act
        bool result = ValidationService.HasEmptyMandatoryValues(rowData, mandatoryColumnIndices);

        // Assert
        Assert.True(result, "Should detect null value in mandatory column");
    }

    /// <summary>
    /// Tests that HasEmptyMandatoryValues correctly handles whitespace values.
    /// </summary>
    [Fact]
    public void HasEmptyMandatoryValues_WithWhitespaceMandatoryColumn_ReturnsTrue()
    {
        // Arrange
        var rowData = new XLCellValue[]
        {
            "VM01",         // VM Name - index 0
            "poweredOn",    // Powerstate - index 1
            "FALSE",        // Template - index 2
            "   ",          // CPUs - index 3 - WHITESPACE
            "4096",         // Memory - index 4
            "2048",         // In Use MiB - index 5
            "Windows",      // OS - index 6
            "FALSE"         // SRM Placeholder - index 7
        };

        // Mandatory column indices - including index 3 (CPUs) which is whitespace
        var mandatoryColumnIndices = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };

        // Act
        bool result = ValidationService.HasEmptyMandatoryValues(rowData, mandatoryColumnIndices);

        // Assert
        Assert.True(result, "Should detect whitespace value in mandatory column");
    }

    /// <summary>
    /// Tests that HasEmptyMandatoryValues returns false when all mandatory columns have values.
    /// </summary>
    [Fact]
    public void HasEmptyMandatoryValues_WithAllMandatoryColumnsPopulated_ReturnsFalse()
    {
        // Arrange
        var rowData = new XLCellValue[]
        {
            "VM01",         // VM Name - index 0
            "poweredOn",    // Powerstate - index 1
            "FALSE",        // Template - index 2
            "4",            // CPUs - index 3
            "4096",         // Memory - index 4
            "2048",         // In Use MiB - index 5
            "Windows",      // OS - index 6
            "FALSE"         // SRM Placeholder - index 7
        };

        // All mandatory column indices
        var mandatoryColumnIndices = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };

        // Act
        bool result = ValidationService.HasEmptyMandatoryValues(rowData, mandatoryColumnIndices);

        // Assert
        Assert.False(result, "Should not detect any empty values in mandatory columns");
    }

    /// <summary>
    /// Tests that HasEmptyMandatoryValues correctly ignores non-mandatory columns.
    /// </summary>
    [Fact]
    public void HasEmptyMandatoryValues_WithEmptyNonMandatoryColumn_ReturnsFalse()
    {
        // Arrange
        var rowData = new XLCellValue[]
        {
            "VM01",         // VM Name - index 0
            "poweredOn",    // Powerstate - index 1
            "FALSE",        // Template - index 2
            "4",            // CPUs - index 3
            "4096",         // Memory - index 4
            "2048",         // In Use MiB - index 5
            "Windows",      // OS - index 6
            "FALSE",        // SRM Placeholder - index 7
            string.Empty    // Non-mandatory column - index 8 - EMPTY
        };

        // Only specify indices 0-7 as mandatory (not 8)
        var mandatoryColumnIndices = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };

        // Act
        bool result = ValidationService.HasEmptyMandatoryValues(rowData, mandatoryColumnIndices);

        // Assert
        Assert.False(result, "Should ignore empty value in non-mandatory column");
    }

    /// <summary>
    /// Tests that HasEmptyMandatoryValues handles invalid indices correctly.
    /// </summary>
    [Fact]
    public void HasEmptyMandatoryValues_WithInvalidColumnIndex_HandlesSafely()
    {
        // Arrange
        var rowData = new XLCellValue[]
        {
            "VM01",         // VM Name - index 0
            "poweredOn",    // Powerstate - index 1
        };



        // Act & Assert
        // This test is mainly to verify the method doesn't throw an exception
        // for out-of-range indices, but the actual behavior is implementation-dependent
        // In the actual implementation, negative indices are filtered, but out-of-range
        // positive indices would cause an exception

        // We'll check that it doesn't throw for negative indices at least
        var mandatoryColumnIndices = new List<int> { 0, 1, -1 };
        bool result = ValidationService.HasEmptyMandatoryValues(rowData, mandatoryColumnIndices);

        // The method should filter out negative indices and just check the valid ones
        Assert.False(result);
    }

    /// <summary>
    /// Tests that ValidateRowForAzureMigrate correctly identifies rows with missing VM UUID.
    /// </summary>
    [Fact]
    public void ValidateRowForAzureMigrate_MissingVmUuid_ReturnsFailure()
    {
        // Arrange
        var rowData = new XLCellValue[]
        {
            "VM01",                     // VM Name - index 0
            XLCellValue.FromObject(null), // VM UUID - index 1 - NULL
            "Windows"                   // OS - index 2
        };
        var seenVmUuids = new HashSet<string>();
        var vmCount = 0;

        // Act
        var result = ValidationService.ValidateRowForAzureMigrate(rowData, 1, 2, seenVmUuids, vmCount);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AzureMigrateValidationFailureReason.MissingVmUuid, result);
    }

    /// <summary>
    /// Tests that ValidateRowForAzureMigrate correctly identifies rows with missing OS configuration.
    /// </summary>
    [Fact]
    public void ValidateRowForAzureMigrate_MissingOsConfig_ReturnsFailure()
    {
        // Arrange
        var rowData = new XLCellValue[]
        {
            "VM01",                     // VM Name - index 0
            "vm-uuid",                  // VM UUID - index 1
            XLCellValue.FromObject(null)  // OS - index 2 - NULL
        };
        var seenVmUuids = new HashSet<string>();
        var vmCount = 0;

        // Act
        var result = ValidationService.ValidateRowForAzureMigrate(rowData, 1, 2, seenVmUuids, vmCount);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AzureMigrateValidationFailureReason.MissingOsConfiguration, result);
    }

    /// <summary>
    /// Tests that ValidateRowForAzureMigrate correctly identifies rows with duplicate VM UUID.
    /// </summary>
    [Fact]
    public void ValidateRowForAzureMigrate_DuplicateVmUuid_ReturnsFailure()
    {
        // Arrange
        var rowData = new XLCellValue[]
        {
            "VM01",     // VM Name - index 0
            "vm-uuid",  // VM UUID - index 1
            "Windows"   // OS - index 2
        };
        var seenVmUuids = new HashSet<string> { "vm-uuid" };
        var vmCount = 1;

        // Act
        var result = ValidationService.ValidateRowForAzureMigrate(rowData, 1, 2, seenVmUuids, vmCount);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AzureMigrateValidationFailureReason.DuplicateVmUuid, result);
    }

    /// <summary>
    /// Tests that ValidateRowForAzureMigrate correctly identifies when VM count exceeds the limit.
    /// </summary>
    [Fact]
    public void ValidateRowForAzureMigrate_VmCountExceeded_ReturnsFailure()
    {
        // Arrange
        var rowData = new XLCellValue[]
        {
            "VM01",     // VM Name - index 0
            "vm-uuid",  // VM UUID - index 1
            "Windows"   // OS - index 2
        };
        var seenVmUuids = new HashSet<string>();
        var vmCount = 20000; // At limit

        // Act
        var result = ValidationService.ValidateRowForAzureMigrate(rowData, 1, 2, seenVmUuids, vmCount);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AzureMigrateValidationFailureReason.VmCountExceeded, result);
    }

    /// <summary>
    /// Tests that ValidateRowForAzureMigrate returns null for valid rows.
    /// </summary>
    [Fact]
    public void ValidateRowForAzureMigrate_ValidRow_ReturnsNull()
    {
        // Arrange
        var rowData = new XLCellValue[]
        {
            "VM01",     // VM Name - index 0
            "vm-uuid",  // VM UUID - index 1
            "Windows"   // OS - index 2
        };
        var seenVmUuids = new HashSet<string>();
        var vmCount = 1;

        // Act
        var result = ValidationService.ValidateRowForAzureMigrate(rowData, 1, 2, seenVmUuids, vmCount);

        // Assert
        Assert.Null(result);
        Assert.Contains("vm-uuid", seenVmUuids);
    }

    /// <summary>
    /// Tests tracking of rows skipped after VM count limit is reached.
    /// </summary>
    [Fact]
    public void RowsSkippedAfterLimitReached_TracksCorrectly()
    {
        // Arrange
        var validationResult = new AzureMigrateValidationResult
        {
            VmCountLimitReached = true
        };

        // Act
        validationResult.RowsSkippedAfterLimitReached = 10;

        // Assert
        Assert.Equal(10, validationResult.RowsSkippedAfterLimitReached);
    }
}
