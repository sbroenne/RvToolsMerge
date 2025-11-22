//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using System.IO.Abstractions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RVToolsMerge.Commands;
using RVToolsMerge.Infrastructure;
using RVToolsMerge.Services;
using RVToolsMerge.Services.Interfaces;
using Spectre.Console.Cli;

namespace RVToolsMerge;

/// <summary>
/// The main program class containing the entry point for the application.
/// </summary>
public class Program
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code (0 for success, non-zero for error).</returns>
    public static async Task<int> Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);

        // Create the command app with dependency injection
        var registrar = new TypeRegistrar(services);
        var app = new CommandApp<MergeCommand>(registrar);

        // Configure the command app
        app.Configure(config =>
        {
            config.SetApplicationName("RVToolsMerge");

            // Get version from assembly
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            string versionString = version is not null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";

            config.SetApplicationVersion(versionString);

            config.ValidateExamples();

            // Add examples to help
            config.AddExample("RVToolsFile.xlsx");
            config.AddExample("RVToolsFile.xlsx", "output.xlsx");
            config.AddExample("C:\\RVToolsExports", "merged-output.xlsx");
            config.AddExample("--anonymize", "--only-mandatory-columns", "input.xlsx", "output.xlsx");
            config.AddExample("--ignore-missing-sheets", "--skip-invalid-files", "C:\\exports");
        });

        // Run the command app
        return await app.RunAsync(args);
    }

    /// <summary>
    /// Configure services for dependency injection.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IFileSystem>(_ => new FileSystem());
        services.AddSingleton<IConsoleService, SpectreConsoleService>();
        services.AddTransient<ConsoleUIService>();
        services.AddSingleton<IExcelService, ExcelService>();
        services.AddSingleton<IAnonymizationService, AnonymizationService>();
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddSingleton<IMergeService, MergeService>();
    }
}
