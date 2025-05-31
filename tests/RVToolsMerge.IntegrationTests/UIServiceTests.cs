//-----------------------------------------------------------------------
// <copyright file="UIServiceTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using RVToolsMerge.Models;
using RVToolsMerge.Services;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Tests for UI services to increase coverage.
/// </summary>
public class UIServiceTests
{
    [Fact]
    public void ConsoleUIService_DisplayHeader_DoesNotThrow()
    {
        // Arrange
        var service = new ConsoleUIService();
        
        // Act & Assert - Should not throw
        service.DisplayHeader("RVToolsMerge", "1.0.0");
    }

    [Fact]
    public void ConsoleUIService_DisplayOptions_WithBasicOptions_DoesNotThrow()
    {
        // Arrange
        var service = new ConsoleUIService();
        var options = new MergeOptions
        {
            AnonymizeData = false,
            DebugMode = false,
            EnableAzureMigrateValidation = false
        };
        
        // Act & Assert - Should not throw
        service.DisplayOptions(options);
    }

    [Fact]
    public void ConsoleUIService_DisplayOptions_WithAllOptionsEnabled_DoesNotThrow()
    {
        // Arrange
        var service = new ConsoleUIService();
        var options = new MergeOptions
        {
            AnonymizeData = true,
            DebugMode = true,
            EnableAzureMigrateValidation = true,
            SkipInvalidFiles = true
        };
        
        // Act & Assert - Should not throw
        service.DisplayOptions(options);
    }

    [Fact]
    public void ConsoleUIService_DisplayInfo_DoesNotThrow()
    {
        // Arrange
        var service = new ConsoleUIService();
        
        // Act & Assert - Should not throw
        service.DisplayInfo("Test information message");
    }

    [Fact]
    public void ConsoleUIService_DisplayError_DoesNotThrow()
    {
        // Arrange
        var service = new ConsoleUIService();
        
        // Act & Assert - Should not throw
        service.DisplayError("Test error message");
    }

    [Fact]
    public void ConsoleUIService_DisplaySuccess_DoesNotThrow()
    {
        // Arrange
        var service = new ConsoleUIService();
        
        // Act & Assert - Should not throw
        service.DisplaySuccess("Test success message");
    }

    [Fact]
    public void ConsoleUIService_DisplayValidationIssues_WithEmptyList_DoesNotThrow()
    {
        // Arrange
        var service = new ConsoleUIService();
        var validationIssues = new List<ValidationIssue>();
        
        // Act & Assert - Should not throw
        service.DisplayValidationIssues(validationIssues);
    }

    [Fact]
    public void ConsoleUIService_DisplayValidationIssues_WithIssues_DoesNotThrow()
    {
        // Arrange
        var service = new ConsoleUIService();
        var validationIssues = new List<ValidationIssue>
        {
            new ValidationIssue("test.xlsx", false, "Test validation error")
        };
        
        // Act & Assert - Should not throw
        service.DisplayValidationIssues(validationIssues);
    }

    [Fact]
    public void ConsoleUIService_WriteLine_DoesNotThrow()
    {
        // Arrange
        var service = new ConsoleUIService();
        
        // Act & Assert - Should not throw
        service.WriteLine();
    }

    [Fact]
    public void ConsoleUIService_ShowHelp_DoesNotThrow()
    {
        // Arrange
        var service = new ConsoleUIService();
        
        // Act & Assert - Should not throw
        service.ShowHelp("RVToolsMerge");
    }

    [Fact]
    public void ConsoleUIService_GetUserFriendlyErrorMessage_WithDifferentExceptions_ReturnsMessages()
    {
        // Arrange
        var service = new ConsoleUIService();
        
        // Act & Assert
        var fileNotFoundMessage = service.GetUserFriendlyErrorMessage(new FileNotFoundException("File not found"));
        var unauthorizedMessage = service.GetUserFriendlyErrorMessage(new UnauthorizedAccessException("Access denied"));
        var genericMessage = service.GetUserFriendlyErrorMessage(new InvalidOperationException("Generic error"));
        
        Assert.NotNull(fileNotFoundMessage);
        Assert.NotNull(unauthorizedMessage);
        Assert.NotNull(genericMessage);
    }
}