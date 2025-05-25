//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RVToolsMerge.Services;
using RVToolsMerge.Services.Interfaces;

namespace RVToolsMerge;

/// <summary>
/// The main program class containing the entry point for the application.
/// </summary>
public class Program // Changed to public for better testability
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        var application = serviceProvider.GetRequiredService<ApplicationRunner>();
        await application.RunAsync(args);
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
        services.AddSingleton<ICommandLineParser, CommandLineParser>();
        services.AddSingleton<ApplicationRunner>();
    }
}
