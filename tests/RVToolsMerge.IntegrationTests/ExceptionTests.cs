//-----------------------------------------------------------------------
// <copyright file="ExceptionTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using RVToolsMerge.Exceptions;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Tests for custom exception classes to increase coverage.
/// </summary>
public class ExceptionTests
{
    [Fact]
    public void FileValidationException_WithMessage_CreatesExceptionCorrectly()
    {
        // Arrange
        const string message = "Test file validation error";
        
        // Act
        var exception = new FileValidationException(message);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }
    
    [Fact]
    public void FileValidationException_WithMessageAndInnerException_CreatesExceptionCorrectly()
    {
        // Arrange
        const string message = "Test file validation error";
        var innerException = new InvalidOperationException("Inner exception");
        
        // Act
        var exception = new FileValidationException(message, innerException);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }
    
    [Fact]
    public void InvalidFileException_WithMessage_CreatesExceptionCorrectly()
    {
        // Arrange
        const string message = "Test invalid file error";
        
        // Act
        var exception = new InvalidFileException(message);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }
    
    [Fact]
    public void InvalidFileException_WithMessageAndInnerException_CreatesExceptionCorrectly()
    {
        // Arrange
        const string message = "Test invalid file error";
        var innerException = new InvalidOperationException("Inner exception");
        
        // Act
        var exception = new InvalidFileException(message, innerException);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }
    
    [Fact]
    public void MissingRequiredSheetException_WithMessage_CreatesExceptionCorrectly()
    {
        // Arrange
        const string message = "Test missing required sheet error";
        
        // Act
        var exception = new MissingRequiredSheetException(message);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }
    
    [Fact]
    public void MissingRequiredSheetException_WithMessageAndInnerException_CreatesExceptionCorrectly()
    {
        // Arrange
        const string message = "Test missing required sheet error";
        var innerException = new InvalidOperationException("Inner exception");
        
        // Act
        var exception = new MissingRequiredSheetException(message, innerException);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }
    
    [Fact]
    public void NoValidFilesException_WithMessage_CreatesExceptionCorrectly()
    {
        // Arrange
        const string message = "Test no valid files error";
        
        // Act
        var exception = new NoValidFilesException(message);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }
    
    [Fact]
    public void NoValidFilesException_WithMessageAndInnerException_CreatesExceptionCorrectly()
    {
        // Arrange
        const string message = "Test no valid files error";
        var innerException = new InvalidOperationException("Inner exception");
        
        // Act
        var exception = new NoValidFilesException(message, innerException);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }
    
    [Fact]
    public void NoValidSheetsException_WithMessage_CreatesExceptionCorrectly()
    {
        // Arrange
        const string message = "Test no valid sheets error";
        
        // Act
        var exception = new NoValidSheetsException(message);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }
    
    [Fact]
    public void NoValidSheetsException_WithMessageAndInnerException_CreatesExceptionCorrectly()
    {
        // Arrange
        const string message = "Test no valid sheets error";
        var innerException = new InvalidOperationException("Inner exception");
        
        // Act
        var exception = new NoValidSheetsException(message, innerException);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }
}