//-----------------------------------------------------------------------
// <copyright file="ConsoleUIServiceTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

namespace RVToolsMerge.UnitTests;

/// <summary>
/// Unit tests for ConsoleUIService
/// </summary>
public class ConsoleUIServiceTests
{
    private readonly ConsoleUIService _service;

    public ConsoleUIServiceTests()
    {
        _service = new ConsoleUIService();
    }

    [Fact]
    public void DisplayHeader_WithValidInputs_DoesNotThrow()
    {
        // Arrange
        const string productName = "TestProduct";
        const string version = "1.0.0";

        // Act & Assert
        var exception = Record.Exception(() => _service.DisplayHeader(productName, version));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplayOptions_WithValidOptions_DoesNotThrow()
    {
        // Arrange
        var options = new MergeOptions
        {
            AnonymizeData = false,
            IgnoreMissingOptionalSheets = true,
            EnableAzureMigrateValidation = false
        };

        // Act & Assert
        var exception = Record.Exception(() => _service.DisplayOptions(options));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplayOptions_WithAnonymizationEnabled_DoesNotThrow()
    {
        // Arrange
        var options = new MergeOptions
        {
            AnonymizeData = true,
            SkipInvalidFiles = false,
            EnableAzureMigrateValidation = true
        };

        // Act & Assert
        var exception = Record.Exception(() => _service.DisplayOptions(options));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplayOptions_WithAllOptionsEnabled_DoesNotThrow()
    {
        // Arrange
        var options = new MergeOptions
        {
            AnonymizeData = true,
            IgnoreMissingOptionalSheets = true,
            SkipInvalidFiles = true,
            OnlyMandatoryColumns = true,
            IncludeSourceFileName = true,
            SkipRowsWithEmptyMandatoryValues = true,
            DebugMode = true,
            EnableAzureMigrateValidation = true
        };

        // Act & Assert
        var exception = Record.Exception(() => _service.DisplayOptions(options));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplayOptions_WithAllOptionsDisabled_DoesNotThrow()
    {
        // Arrange
        var options = new MergeOptions
        {
            AnonymizeData = false,
            IgnoreMissingOptionalSheets = false,
            SkipInvalidFiles = false,
            OnlyMandatoryColumns = false,
            IncludeSourceFileName = false,
            SkipRowsWithEmptyMandatoryValues = false,
            DebugMode = false,
            EnableAzureMigrateValidation = false
        };

        // Act & Assert
        var exception = Record.Exception(() => _service.DisplayOptions(options));
        Assert.Null(exception);
    }
}