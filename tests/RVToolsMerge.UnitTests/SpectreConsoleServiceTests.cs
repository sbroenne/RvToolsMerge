//-----------------------------------------------------------------------
// <copyright file="SpectreConsoleServiceTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using Spectre.Console;
using Spectre.Console.Rendering;

namespace RVToolsMerge.UnitTests;

/// <summary>
/// Unit tests for SpectreConsoleService
/// </summary>
public class SpectreConsoleServiceTests
{
    private readonly SpectreConsoleService _service;

    public SpectreConsoleServiceTests()
    {
        _service = new SpectreConsoleService();
    }

    [Fact]
    public void MarkupLine_CallsAnsiConsoleMarkupLine()
    {
        // This test verifies that the method calls the underlying AnsiConsole method
        // We can't directly mock AnsiConsole, but we can test that the method doesn't throw
        // and follows the expected behavior patterns

        // Act & Assert
        // Should not throw exception for valid markup
        var exception = Record.Exception(() => _service.MarkupLine("Test message"));
        Assert.Null(exception);
    }

    [Fact]
    public void MarkupLineInterpolated_CallsAnsiConsoleMarkupLineInterpolated()
    {
        // Act & Assert
        // Should not throw exception for valid interpolated markup
        var exception = Record.Exception(() => _service.MarkupLineInterpolated($"Test message"));
        Assert.Null(exception);
    }

    [Fact]
    public void WriteLine_CallsAnsiConsoleWriteLine()
    {
        // Act & Assert
        // Should not throw exception
        var exception = Record.Exception(() => _service.WriteLine());
        Assert.Null(exception);
    }

    [Fact]
    public void Write_WithRenderable_CallsAnsiConsoleWrite()
    {
        // Arrange
        var table = new Table();

        // Act & Assert
        // Should not throw exception for valid renderable content
        var exception = Record.Exception(() => _service.Write(table));
        Assert.Null(exception);
    }

    [Fact]
    public void Write_WithNonRenderable_ThrowsNotSupportedException()
    {
        // Arrange
        var nonRenderable = "plain string";

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() => _service.Write(nonRenderable));
        Assert.Contains("Write<T> only supports types implementing IRenderable", exception.Message);
        Assert.Contains("String", exception.Message);
    }

    [Fact]
    public void RenderProgress_CallsAnsiConsoleProgress()
    {
        // Arrange
        bool actionCalled = false;
        void progressAction(ProgressContext ctx) => actionCalled = true;

        // Act & Assert
        // Should not throw exception and should call the action
        var exception = Record.Exception(() => _service.RenderProgress(progressAction));
        Assert.Null(exception);
        Assert.True(actionCalled);
    }

    [Fact]
    public async Task RenderProgressAsync_CallsAnsiConsoleProgressAsync()
    {
        // Arrange
        bool actionCalled = false;
        async Task ProgressAction(ProgressContext ctx)
        {
            actionCalled = true;
            await Task.Delay(1); // Simulate async work
        }

        // Act & Assert
        // Should not throw exception and should call the action
        var exception = await Record.ExceptionAsync(async () => await _service.RenderProgressAsync(ProgressAction));
        Assert.Null(exception);
        Assert.True(actionCalled);
    }

    [Fact]
    public void WriteRule_WithTitle_CallsAnsiConsoleWrite()
    {
        // Act & Assert
        // Should not throw exception for valid title
        var exception = Record.Exception(() => _service.WriteRule("Test Rule"));
        Assert.Null(exception);
    }

    [Fact]
    public void WriteRule_WithTitleAndStyle_CallsAnsiConsoleWrite()
    {
        // Act & Assert
        // Should not throw exception for valid title and style
        var exception = Record.Exception(() => _service.WriteRule("Test Rule", "blue"));
        Assert.Null(exception);
    }

    [Fact]
    public void WriteRule_WithNullStyle_CallsAnsiConsoleWrite()
    {
        // Act & Assert
        // Should not throw exception when style is null
        var exception = Record.Exception(() => _service.WriteRule("Test Rule", null));
        Assert.Null(exception);
    }

    /// <summary>
    /// Test implementation of IRenderable for testing purposes
    /// </summary>
    private class TestRenderable : IRenderable
    {
        public Measurement Measure(RenderOptions options, int maxWidth) => new(1, 1);
        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => [];
    }

    [Fact]
    public void Write_WithCustomRenderable_CallsAnsiConsoleWrite()
    {
        // Arrange
        var renderable = new TestRenderable();

        // Act & Assert
        // Should not throw exception for custom renderable
        var exception = Record.Exception(() => _service.Write(renderable));
        Assert.Null(exception);
    }
}