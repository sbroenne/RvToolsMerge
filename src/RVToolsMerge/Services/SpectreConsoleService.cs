//-----------------------------------------------------------------------
// <copyright file="SpectreConsoleService.cs" company="Stefan Broenner"> ">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using RVToolsMerge.Services.Interfaces;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace RVToolsMerge.Services;

/// <summary>
/// Implementation of IConsoleService using Spectre.Console
/// </summary>
public class SpectreConsoleService : IConsoleService
{
    /// <summary>
    /// Writes a line of text to the console with markup
    /// </summary>
    /// <param name="text">The text to write</param>
    public void MarkupLine(string text) => AnsiConsole.MarkupLine(text);

    /// <summary>
    /// Writes a line of interpolated text to the console with markup
    /// </summary>
    /// <param name="text">The interpolated text to write</param>
    public void MarkupLineInterpolated(FormattableString text) => AnsiConsole.MarkupLineInterpolated(text);

    /// <summary>
    /// Writes a blank line to the console
    /// </summary>
    public void WriteLine() => AnsiConsole.WriteLine();

    /// <summary>
    /// Writes generic content to the console
    /// </summary>
    /// <typeparam name="T">The type of the content</typeparam>
    /// <param name="content">The content to write</param>
    public void Write<T>(T content) where T : class
    {
        if (content is Spectre.Console.Rendering.IRenderable renderable)
        {
            AnsiConsole.Write(renderable);
        }
        else
        {
            throw new NotSupportedException($"Write<T> only supports types implementing IRenderable. Type '{typeof(T).Name}' is not supported.");
        }
    }

    /// <summary>
    /// Creates and displays a progress bar
    /// </summary>
    /// <param name="action">The action to perform with the progress context</param>
    public void RenderProgress(Action<ProgressContext> action) =>
        AnsiConsole.Progress().Columns(
            new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            }).Start(action);

    /// <summary>
    /// Creates and displays an async progress bar
    /// </summary>
    /// <param name="action">The async action to perform with the progress context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task RenderProgressAsync(Func<ProgressContext, Task> action) =>
        await AnsiConsole.Progress().Columns(new ProgressColumn[]
        {
            new TaskDescriptionColumn(),
            new ProgressBarColumn(),
            new PercentageColumn(),
            new SpinnerColumn()
        }).StartAsync(action);

    /// <summary>
    /// Displays a rule (horizontal line) with text
    /// </summary>
    /// <param name="title">The title to display in the rule</param>
    /// <param name="style">Optional style for the rule</param>
    public void WriteRule(string title, string? style = null) =>
        AnsiConsole.Write(style is null ? new Rule(title) : new Rule(title).RuleStyle(style));
}
