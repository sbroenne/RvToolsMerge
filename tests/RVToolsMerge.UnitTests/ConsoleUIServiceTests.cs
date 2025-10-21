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

    [Fact]
    public void DisplayText_WithNormalText_DoesNotThrow()
    {
        // Arrange
        const string normalText = "This is a normal text message";

        // Act & Assert
        var exception = Record.Exception(() => _service.DisplayText(normalText));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplayText_WithStackTraceContainingSpecialCharacters_DoesNotThrow()
    {
        // Arrange
        const string stackTraceWithSpecialChars = @"   at RVToolsMerge.Program.Main(String[] args) in D:\source\RvToolsMerge\src\RVToolsMerge\Program.cs:line 33
   at RVToolsMerge.Program.<Main>(String[] args)";

        // Act & Assert
        // This should not throw a Spectre.Console markup parsing exception
        var exception = Record.Exception(() => _service.DisplayText(stackTraceWithSpecialChars));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplayText_WithMarkupLikeCharacters_DoesNotThrow()
    {
        // Arrange
        const string textWithMarkupChars = "Error in method <TestMethod> with value [test] and style {color}";

        // Act & Assert
        // This should not throw a markup parsing exception because DisplayText should treat it as raw text
        var exception = Record.Exception(() => _service.DisplayText(textWithMarkupChars));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplayText_WithEmptyString_DoesNotThrow()
    {
        // Arrange
        const string emptyText = "";

        // Act & Assert
        var exception = Record.Exception(() => _service.DisplayText(emptyText));
        Assert.Null(exception);
    }

    [Fact]
    public void DisplayText_WithNullString_DoesNotThrow()
    {
        // Arrange
        string? nullText = null;

        // Act & Assert
        var exception = Record.Exception(() => _service.DisplayText(nullText!));
        Assert.Null(exception);
    }
}
