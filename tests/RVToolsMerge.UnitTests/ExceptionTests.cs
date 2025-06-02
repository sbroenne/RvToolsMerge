//-----------------------------------------------------------------------
// <copyright file="ExceptionTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

namespace RVToolsMerge.UnitTests;

/// <summary>
/// Unit tests for custom exception classes
/// </summary>
public class ExceptionTests
{
    [Fact]
    public void InvalidFileException_WithMessage_SetsMessage()
    {
        // Arrange
        const string message = "Test error message";

        // Act
        var exception = new InvalidFileException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void InvalidFileException_WithMessageAndInnerException_SetsBoth()
    {
        // Arrange
        const string message = "Test error message";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new InvalidFileException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void NoValidFilesException_WithMessage_SetsMessage()
    {
        // Arrange
        const string message = "No valid files found";

        // Act
        var exception = new NoValidFilesException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void NoValidFilesException_WithMessageAndInnerException_SetsBoth()
    {
        // Arrange
        const string message = "No valid files found";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new NoValidFilesException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void NoValidSheetsException_WithMessage_SetsMessage()
    {
        // Arrange
        const string message = "No valid sheets found";

        // Act
        var exception = new NoValidSheetsException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void NoValidSheetsException_WithMessageAndInnerException_SetsBoth()
    {
        // Arrange
        const string message = "No valid sheets found";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new NoValidSheetsException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void MissingRequiredSheetException_WithMessage_SetsMessage()
    {
        // Arrange
        const string message = "Required sheet missing";

        // Act
        var exception = new MissingRequiredSheetException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void MissingRequiredSheetException_WithMessageAndInnerException_SetsBoth()
    {
        // Arrange
        const string message = "Required sheet missing";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new MissingRequiredSheetException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void FileValidationException_WithMessage_SetsMessage()
    {
        // Arrange
        const string message = "File validation failed";

        // Act
        var exception = new FileValidationException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void FileValidationException_WithMessageAndInnerException_SetsBoth()
    {
        // Arrange
        const string message = "File validation failed";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new FileValidationException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }
}