//-----------------------------------------------------------------------
// <copyright file="IConsoleService.cs" company="Stefan Broenner"> ">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using Spectre.Console;

namespace RVToolsMerge.Services.Interfaces;

/// <summary>
/// Interface for console operations to improve testability
/// </summary>
public interface IConsoleService
{
    /// <summary>
    /// Writes a line of text to the console with markup
    /// </summary>
    /// <param name="text">The text to write</param>
    void MarkupLine(string text);

    /// <summary>
    /// Writes a line of interpolated text to the console with markup
    /// </summary>
    /// <param name="text">The interpolated text to write</param>
    void MarkupLineInterpolated(FormattableString text);

    /// <summary>
    /// Writes a blank line to the console
    /// </summary>
    void WriteLine();

    /// <summary>
    /// Writes generic content to the console
    /// </summary>
    /// <typeparam name="T">The type of the content</typeparam>
    /// <param name="content">The content to write</param>
    void Write<T>(T content) where T : class;

    /// <summary>
    /// Creates and displays a progress bar
    /// </summary>
    /// <param name="action">The action to perform with the progress context</param>
    void RenderProgress(Action<ProgressContext> action);

    /// <summary>
    /// Creates and displays an async progress bar
    /// </summary>
    /// <param name="action">The async action to perform with the progress context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task RenderProgressAsync(Func<ProgressContext, Task> action);

    /// <summary>
    /// Displays a rule (horizontal line) with text
    /// </summary>
    /// <param name="title">The title to display in the rule</param>
    /// <param name="style">Optional style for the rule</param>
    void WriteRule(string title, string? style = null);
}
