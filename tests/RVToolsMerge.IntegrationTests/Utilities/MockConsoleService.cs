//-----------------------------------------------------------------------
// <copyright file="MockConsoleService.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using RVToolsMerge.Services.Interfaces;
using Spectre.Console;

namespace RVToolsMerge.IntegrationTests.Utilities;

/// <summary>
/// A mock implementation of IConsoleService for testing that doesn't use interactive features.
/// </summary>
public class MockConsoleService : IConsoleService
{
    /// <summary>
    /// Writes a line of text with markup to the console.
    /// </summary>
    /// <param name="text">The text with markup to write.</param>
    public void MarkupLine(string text)
    {
        // Do nothing in tests
    }

    /// <summary>
    /// Writes a line of text with markup interpolation to the console.
    /// </summary>
    /// <param name="text">The FormattableString containing the text with markup.</param>
    public void MarkupLineInterpolated(FormattableString text)
    {
        // Do nothing in tests
    }
    
    /// <summary>
    /// Writes a blank line to the console.
    /// </summary>
    public void WriteLine()
    {
        // Do nothing in tests
    }
    
    /// <summary>
    /// Writes generic content to the console.
    /// </summary>
    /// <typeparam name="T">The type of the content.</typeparam>
    /// <param name="content">The content to write.</param>
    public void Write<T>(T content) where T : class
    {
        // Do nothing in tests
    }
    
    /// <summary>
    /// Creates and displays a progress bar.
    /// </summary>
    /// <param name="action">The action to perform with the progress context.</param>
    public void RenderProgress(Action<ProgressContext> action)
    {
        // Do nothing in tests - we don't actually need to run the action
        // as the test is not checking the progress display
    }
    
    /// <summary>
    /// Creates and displays an async progress bar.
    /// </summary>
    /// <param name="action">The async action to perform with the progress context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task RenderProgressAsync(Func<ProgressContext, Task> action)
    {
        // Skip running the action in tests - we're not testing the progress UI
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Displays a rule (horizontal line) with text.
    /// </summary>
    /// <param name="title">The title to display in the rule.</param>
    /// <param name="style">Optional style for the rule.</param>
    public void WriteRule(string title, string? style = null)
    {
        // Do nothing in tests
    }
}