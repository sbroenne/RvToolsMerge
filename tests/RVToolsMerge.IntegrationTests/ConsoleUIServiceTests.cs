//-----------------------------------------------------------------------
// <copyright file="ConsoleUIServiceTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using Moq;
using RVToolsMerge.Models;
using RVToolsMerge.Services;
using RVToolsMerge.Services.Interfaces;
using System.IO.Abstractions;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

[Collection("SpectreConsole")]
/// <summary>
/// Tests for the ConsoleUIService.
/// </summary>
public class ConsoleUIServiceTests : IntegrationTestBase
{
    private readonly ConsoleUIService _consoleUIService;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleUIServiceTests"/> class.
    /// </summary>
    public ConsoleUIServiceTests()
    {
        _consoleUIService = new ConsoleUIService();
    }
    
    /// <summary>
    /// Tests that GetUserFriendlyErrorMessage formats various errors.
    /// </summary>
    [Theory]
    [InlineData("File not found", "File not found")]
    [InlineData("Access to the path is denied", "Access denied")]
    [InlineData("being used by another process", "The output file is being used")]
    public void GetUserFriendlyErrorMessage_FormatsErrors(string exceptionMessage, string expectedSubstring)
    {
        // Arrange
        Exception exception;
        if (exceptionMessage == "File not found")
        {
            exception = new FileNotFoundException(exceptionMessage);
        }
        else if (exceptionMessage == "Access to the path is denied")
        {
            exception = new UnauthorizedAccessException(exceptionMessage);
        }
        else if (exceptionMessage == "being used by another process")
        {
            exception = new IOException(exceptionMessage);
        }
        else
        {
            exception = new Exception(exceptionMessage);
        }
        
        // Act
        string result = _consoleUIService.GetUserFriendlyErrorMessage(exception);
        
        // Assert
        Assert.Contains(expectedSubstring, result);
    }
    
    /// <summary>
    /// Tests that GetUserFriendlyErrorMessage handles unknown errors gracefully.
    /// </summary>
    [Fact]
    public void GetUserFriendlyErrorMessage_UnknownError_ReturnsMessage()
    {
        // Arrange
        var exception = new Exception("Unknown error message");
        
        // Act
        string result = _consoleUIService.GetUserFriendlyErrorMessage(exception);
        
        // Assert
        Assert.Equal("Unknown error message", result);
    }
    
    /// <summary>
    /// Tests that GetUserFriendlyErrorMessage handles errors with specific content.
    /// </summary>
    [Fact]
    public void GetUserFriendlyErrorMessage_NoValidFiles_ReturnsSpecificMessage()
    {
        // Arrange
        var exception = new Exception("No valid files to process");
        
        // Act
        string result = _consoleUIService.GetUserFriendlyErrorMessage(exception);
        
        // Assert
        Assert.Contains("No valid files to process", result);
        Assert.Contains("Ensure your input folder contains valid RVTools Excel files", result);
    }
}