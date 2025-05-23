//-----------------------------------------------------------------------
// <copyright file="ConsoleUIService.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using RVToolsMerge.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace RVToolsMerge.Services;

/// <summary>
/// Service for handling console UI interactions.
/// </summary>
public class ConsoleUIService
{
    /// <summary>
    /// Displays application header with version information.
    /// </summary>
    /// <param name="productName">The product name to display.</param>
    /// <param name="version">The version to display.</param>
    public void DisplayHeader(string productName, string version)
    {
        AnsiConsole.Write(
            new FigletText(productName)
                .Color(Color.Green)
                .Centered()
        );
        AnsiConsole.MarkupLine($"[yellow]v{version}[/]");
        AnsiConsole.Write(new Rule().RuleStyle("grey"));

        AnsiConsole.MarkupLineInterpolated($"[bold green]{productName}[/] - Merges multiple RVTools Excel files into a single file");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays selected options in a table.
    /// </summary>
    /// <param name="options">The merge options to display.</param>
    public void DisplayOptions(MergeOptions options)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Selected Options:[/]");
        var optionsTable = CreateOptionsTable(options);
        AnsiConsole.Write(optionsTable);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Creates a table to display the selected options.
    /// </summary>
    /// <param name="options">The merge options to display.</param>
    /// <returns>A table with option statuses.</returns>
    private static Table CreateOptionsTable(MergeOptions options)
    {
        var optionsTable = new Table().BorderColor(Color.Grey);
        optionsTable.AddColumn(new TableColumn("Option").Centered());
        optionsTable.AddColumn(new TableColumn("Status").Centered());

        optionsTable.AddRow("[yellow]--ignore-missing-sheets[/]", FormatOptionStatus(options.IgnoreMissingOptionalSheets));
        optionsTable.AddRow("[yellow]--skip-invalid-files[/]", FormatOptionStatus(options.SkipInvalidFiles));
        optionsTable.AddRow("[yellow]--anonymize[/]", FormatOptionStatus(options.AnonymizeData));
        optionsTable.AddRow("[yellow]--only-mandatory-columns[/]", FormatOptionStatus(options.OnlyMandatoryColumns));
        optionsTable.AddRow("[yellow]--include-source[/]", FormatOptionStatus(options.IncludeSourceFileName));
        optionsTable.AddRow("[yellow]--skip-empty-values[/]", FormatOptionStatus(options.SkipRowsWithEmptyMandatoryValues));
        optionsTable.AddRow("[yellow]--debug[/]", FormatOptionStatus(options.DebugMode));

        return optionsTable;
    }

    /// <summary>
    /// Formats the status of an option.
    /// </summary>
    /// <param name="isEnabled">Whether the option is enabled.</param>
    /// <returns>A formatted string for the option status.</returns>
    private static string FormatOptionStatus(bool isEnabled) =>
        isEnabled ? "[green]Enabled[/]" : "[grey]Disabled[/]";

    /// <summary>
    /// Displays validation issues in a formatted table.
    /// </summary>
    /// <param name="issues">The list of validation issues to display.</param>
    public void DisplayValidationIssues(List<ValidationIssue> issues)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[yellow]Validation Issues[/]").RuleStyle("grey"));

        var groupedIssues = GroupValidationIssues(issues);
        var table = CreateValidationIssuesTable(groupedIssues);

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        int totalFiles = groupedIssues.Count;
        int totalIssues = issues.Count;
        AnsiConsole.MarkupLineInterpolated($"[yellow]Total of {totalIssues} validation issues across {totalFiles} files.[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Groups validation issues by filename.
    /// </summary>
    /// <param name="issues">The list of validation issues to group.</param>
    /// <returns>A list of issue groups.</returns>
    private static List<IGrouping<string, ValidationIssue>> GroupValidationIssues(List<ValidationIssue> issues)
    {
        return issues
            .GroupBy(issue => issue.FileName)
            .OrderBy(group => group.Key)
            .ToList();
    }

    /// <summary>
    /// Creates a table to display validation issues.
    /// </summary>
    /// <param name="groupedIssues">The grouped validation issues.</param>
    /// <returns>A table with validation issues.</returns>
    private static Table CreateValidationIssuesTable(List<IGrouping<string, ValidationIssue>> groupedIssues)
    {
        var table = new Table();
        table.AddColumn(new TableColumn("File Name").LeftAligned());
        table.AddColumn(new TableColumn("Status").Centered());
        table.AddColumn(new TableColumn("Details").LeftAligned());

        foreach (var group in groupedIssues)
        {
            var filename = group.Key;
            var fileIssues = group.ToList();

            // Determine the overall status for this file
            bool anySkipped = fileIssues.Any(issue => issue.Skipped);
            string status = anySkipped ? "[yellow]Skipped[/]" : "[green]Processed with warning[/]";

            // Combine all validation errors for this file
            var errorDetails = fileIssues
                .Select(issue => issue.ValidationError)
                .Distinct()
                .Select(error => $"• {error}")
                .ToList();

            string details = string.Join("\n", errorDetails);

            table.AddRow(
                $"[cyan]{filename}[/]",
                status,
                $"[grey]{details}[/]"
            );
        }

        table.Border(TableBorder.Rounded);
        return table;
    }

    /// <summary>
    /// Shows help information about the application.
    /// </summary>
    /// <param name="appName">The application name.</param>
    public void ShowHelp(string appName)
    {
        DisplayHelpUsage(appName);
        DisplayHelpArguments();
        DisplayHelpOptions();
        DisplayHelpExamples(appName);
        DisplaySupportedSheetsTable();
        DisplayHelpDownloadInfo();
    }

    /// <summary>
    /// Displays usage information in the help screen.
    /// </summary>
    /// <param name="appName">The application name.</param>
    private static void DisplayHelpUsage(string appName)
    {
        AnsiConsole.MarkupLine("[bold]USAGE:[/]");
        AnsiConsole.MarkupLineInterpolated($"  [cyan]{appName}[/] [grey][[options]] inputPath [[outputFile]][/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays arguments information in the help screen.
    /// </summary>
    private static void DisplayHelpArguments()
    {
        AnsiConsole.MarkupLine("[bold]ARGUMENTS:[/]");
        AnsiConsole.MarkupLine("  [green]inputPath[/]     Path to an Excel file or a folder containing RVTools Excel files.");
        AnsiConsole.MarkupLine("                [bold]Required[/]. Must be a valid file path or directory path.");
        AnsiConsole.MarkupLine("  [green]outputFile[/]    Path where the merged file will be saved.");
        AnsiConsole.MarkupLine("                Defaults to \"RVTools_Merged.xlsx\" in the current directory.");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays options information in the help screen.
    /// </summary>
    private static void DisplayHelpOptions()
    {
        AnsiConsole.MarkupLine("[bold]OPTIONS:[/]");
        AnsiConsole.MarkupLine("  [yellow]-h, --help, /?[/]            Show this help message and exit.");
        AnsiConsole.MarkupLine("  [yellow]-m, --ignore-missing-sheets[/]");
        AnsiConsole.MarkupLine("                            Ignore missing optional sheets (vHost, vPartition & vMemory).");
        AnsiConsole.MarkupLine("  [yellow]-i, --skip-invalid-files[/]  Skip files that don't meet validation requirements.");
        AnsiConsole.MarkupLine("  [yellow]-a, --anonymize[/]           Anonymize VM, DNS Name, IP Address, Cluster, Host, and Datacenter names.");
        AnsiConsole.MarkupLine("  [yellow]-M, --only-mandatory-columns[/]");
        AnsiConsole.MarkupLine("                            Include only mandatory columns in output.");
        AnsiConsole.MarkupLine("  [yellow]-s, --include-source[/]      Include source file name in output.");
        AnsiConsole.MarkupLine("  [yellow]-e, --skip-empty-values[/]   Skip rows with empty values in mandatory columns.");
        AnsiConsole.MarkupLine("                            By default, all rows are included regardless of empty values.");
        AnsiConsole.MarkupLine("  [yellow]-d, --debug[/]               Show detailed error information.");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays examples in the help screen.
    /// </summary>
    /// <param name="appName">The application name.</param>
    private static void DisplayHelpExamples(string appName)
    {
        AnsiConsole.MarkupLine("[bold]EXAMPLES:[/]");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] C:\\RVTools\\Data");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] C:\\RVTools\\Data\\SingleFile.xlsx");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-m[/] C:\\RVTools\\Data C:\\Reports\\Merged_RVTools.xlsx");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-a[/] C:\\RVTools\\Data\\RVTools.xlsx C:\\Reports\\Anonymized_RVTools.xlsx");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-M[/] C:\\RVTools\\Data C:\\Reports\\Mandatory_Columns.xlsx");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-a -M -s[/] C:\\RVTools\\Data C:\\Reports\\Complete_Analysis.xlsx");
        AnsiConsole.MarkupLine($"  [cyan]{appName}[/] [yellow]-e[/] C:\\RVTools\\Data C:\\Reports\\Skip_Empty_Values.xlsx");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays the supported sheets table in the help screen.
    /// </summary>
    private static void DisplaySupportedSheetsTable()
    {
        var table = new Table();
        table.AddColumn(new TableColumn("Sheet").LeftAligned());
        table.AddColumn(new TableColumn("Status").Centered());
        table.AddColumn(new TableColumn("Required Columns").LeftAligned());
        table.AddRow(
            "[green]vInfo[/]",
            "[bold green]Required[/]",
            "Template, SRM Placeholder, Powerstate, [bold]VM[/], [bold]CPUs[/], [bold]Memory[/], [bold]In Use MiB[/], [bold]OS according to the configuration file[/]"
        );
        table.AddRow(
            "[cyan]vHost[/]",
            "[yellow]Optional[/]",
            "[bold]Host[/], [bold]Datacenter[/], [bold]Cluster[/], CPU Model, Speed, # CPU, Cores per CPU, # Cores, CPU usage %, # Memory, Memory usage %"
        );
        table.AddRow(
            "[cyan]vPartition[/]",
            "[yellow]Optional[/]",
            "[bold]VM[/], [bold]Disk[/], [bold]Capacity MiB[/], [bold]Consumed MiB[/]"
        );
        table.AddRow(
            "[cyan]vMemory[/]",
            "[yellow]Optional[/]",
            "[bold]VM[/], [bold]Size MiB[/], [bold]Reservation[/]"
        );
        table.Border(TableBorder.Rounded);
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays download information in the help screen.
    /// </summary>
    private static void DisplayHelpDownloadInfo()
    {
        AnsiConsole.MarkupLine("[bold]DOWNLOADS:[/]");
        AnsiConsole.MarkupLine("  Latest releases are available at:");
        AnsiConsole.MarkupLine("  [link]https://github.com/sbroenne/RVToolsMerge/releases[/]");
    }

    /// <summary>
    /// Gets a user-friendly error message based on the exception type.
    /// </summary>
    /// <param name="ex">The exception to process.</param>
    /// <returns>A user-friendly error message.</returns>
    public string GetUserFriendlyErrorMessage(Exception ex) => ex switch
    {
        FileNotFoundException => $"File not found: {ex.Message}",
        UnauthorizedAccessException => "Access denied. Please check if you have the necessary permissions.",
        IOException ioEx when ioEx.Message.Contains("being used by another process") =>
            "The output file is being used by another application. Please close it and try again.",
        _ when ex.Message.Contains("No valid files to process") =>
            "No valid files to process. Ensure your input folder contains valid RVTools Excel files.",
        _ => ex.Message // For other exceptions, just return the message
    };

    /// <summary>
    /// Displays an error message with formatting.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    public void DisplayError(string message)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {message}");
    }

    /// <summary>
    /// Displays an informational message with formatting.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public void DisplayInfo(string message)
    {
        AnsiConsole.MarkupLine(message);
    }

    /// <summary>
    /// Displays a success message with formatting.
    /// </summary>
    /// <param name="message">The success message to display.</param>
    public void DisplaySuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]{message}[/]");
    }

    /// <summary>
    /// Writes a line of text to the console.
    /// </summary>
    public void WriteLine()
    {
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays a message with interpolated markup.
    /// </summary>
    /// <param name="message">The interpolated message with markup to display.</param>
    public void MarkupLineInterpolated(FormattableString message)
    {
        AnsiConsole.MarkupLineInterpolated(message);
    }

    /// <summary>
    /// Gets a progress context for tracking progress of operations.
    /// </summary>
    /// <returns>An AnsiConsole Progress object.</returns>
    public Progress Progress()
    {
        return AnsiConsole.Progress();
    }

    /// <summary>
    /// Writes a rule (horizontal line) to the console.
    /// </summary>
    /// <param name="title">The title of the rule.</param>
    /// <param name="style">The style to apply to the rule.</param>
    public void WriteRule(string title, string? style = null)
    {
        AnsiConsole.Write(style is null ? new Rule(title) : new Rule(title).RuleStyle(style));
    }

    /// <summary>
    /// Writes a renderable to the console.
    /// </summary>
    /// <param name="renderable">The renderable to write.</param>
    public void Write(IRenderable renderable)
    {
        AnsiConsole.Write(renderable);
    }
}
