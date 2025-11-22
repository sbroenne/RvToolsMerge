//-----------------------------------------------------------------------
// <copyright file="IntegrationTestBase.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RVToolsMerge.IntegrationTests.Utilities;
using RVToolsMerge.Models;
using RVToolsMerge.Services;
using RVToolsMerge.Services.Interfaces;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Base class for integration tests, providing common setup and utilities.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    /// <summary>
    /// The real file system used for testing with actual files.
    /// </summary>
    protected readonly IFileSystem FileSystem;

    /// <summary>
    /// Service provider containing the test dependencies.
    /// </summary>
    protected readonly ServiceProvider ServiceProvider;

    /// <summary>
    /// Test data generator for creating synthetic RVTools files.
    /// </summary>
    protected readonly TestDataGenerator TestDataGenerator;

    /// <summary>
    /// Directory for test input files.
    /// </summary>
    protected readonly string TestInputDirectory;

    /// <summary>
    /// Directory for test output files.
    /// </summary>
    protected readonly string TestOutputDirectory;

    /// <summary>
    /// Root temporary directory for this test instance.
    /// </summary>
    protected readonly string TestRootDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationTestBase"/> class.
    /// </summary>
    protected IntegrationTestBase()
    {
        // Use real file system for integration tests as required by Copilot instructions
        FileSystem = new FileSystem();

        // Create a unique temporary directory for this test instance
        TestRootDirectory = Path.Combine(Path.GetTempPath(), "RVToolsMerge_IntegrationTests", Guid.NewGuid().ToString());

        // Set up test directories
        TestInputDirectory = FileSystem.Path.Combine(TestRootDirectory, "input");
        TestOutputDirectory = FileSystem.Path.Combine(TestRootDirectory, "output");

        // Create the test directories
        FileSystem.Directory.CreateDirectory(TestInputDirectory);
        FileSystem.Directory.CreateDirectory(TestOutputDirectory);

        // Create test data generator
        TestDataGenerator = new TestDataGenerator(FileSystem, TestInputDirectory);

        // Set up DI container with the real file system
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Gets the merge service for testing.
    /// </summary>
    protected IMergeService MergeService => ServiceProvider.GetRequiredService<IMergeService>();

    /// <summary>
    /// Gets the Excel service for testing.
    /// </summary>
    protected IExcelService ExcelService => ServiceProvider.GetRequiredService<IExcelService>();

    /// <summary>
    /// Gets the validation service for testing.
    /// </summary>
    protected IValidationService ValidationService => ServiceProvider.GetRequiredService<IValidationService>();

    /// <summary>
    /// Gets the anonymization service for testing.
    /// </summary>
    protected IAnonymizationService AnonymizationService => ServiceProvider.GetRequiredService<IAnonymizationService>();

    /// <summary>
    /// Configure services for dependency injection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Register the real file system for integration tests
        services.AddSingleton<IFileSystem>(FileSystem);

        // Register the mock console service instead of the real one
        services.AddSingleton<IConsoleService, MockConsoleService>();
        services.AddTransient<ConsoleUIService>();
        services.AddSingleton<IExcelService, ExcelService>();
        services.AddSingleton<IAnonymizationService, AnonymizationService>();
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddSingleton<IMergeService, MergeService>(); // Use real MergeService for integration tests
    }

    /// <summary>
    /// Creates a standard output file path for use in tests.
    /// </summary>
    /// <param name="fileName">Output file name.</param>
    /// <returns>Full path to the output file.</returns>
    protected string GetOutputFilePath(string fileName)
    {
        return FileSystem.Path.Combine(TestOutputDirectory, fileName);
    }

    /// <summary>
    /// Creates a default MergeOptions instance for testing.
    /// </summary>
    /// <returns>A default MergeOptions object.</returns>
    protected MergeOptions CreateDefaultMergeOptions()
    {
        return new MergeOptions
        {
            IgnoreMissingOptionalSheets = false,
            SkipInvalidFiles = true,
            AnonymizeData = false,
            OnlyMandatoryColumns = false,
            IncludeSourceFileName = true,
            SkipRowsWithEmptyMandatoryValues = false,
            DebugMode = false
        };
    }

    /// <summary>
    /// Reads test info file to get sheet row counts.
    /// </summary>
    /// <param name="infoPath">Path to the test info file.</param>
    /// <returns>Dictionary mapping sheet names to row counts.</returns>
    protected Dictionary<string, int> ReadTestInfo(string infoPath)
    {
        var result = new Dictionary<string, int>();

        if (FileSystem.File.Exists(infoPath))
        {
            var content = FileSystem.File.ReadAllText(infoPath);
            var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out var count))
                {
                    result[parts[0]] = count;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets actual column info from a real Excel file by reading it.
    /// </summary>
    /// <param name="outputPath">Path to the output file.</param>
    /// <returns>Dictionary mapping sheet names to column counts.</returns>
    protected Dictionary<string, int> GetColumnInfo(string outputPath)
    {
        var result = new Dictionary<string, int>();

        if (!FileSystem.File.Exists(outputPath))
        {
            return result;
        }

        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook(outputPath);
            foreach (var worksheet in workbook.Worksheets)
            {
                var lastColumnUsed = worksheet.LastColumnUsed();
                result[worksheet.Name] = lastColumnUsed?.ColumnNumber() ?? 0;
            }
        }
        catch (Exception)
        {
            // If we can't read the file, return empty result
        }

        return result;
    }

    /// <summary>
    /// Dispose of resources and clean up temporary files/directories.
    /// </summary>
    public void Dispose()
    {
        try
        {
            ServiceProvider.Dispose();

            // Clean up temporary test directory as required by Copilot instructions
            if (FileSystem.Directory.Exists(TestRootDirectory))
            {
                FileSystem.Directory.Delete(TestRootDirectory, recursive: true);
            }
        }
        catch (Exception)
        {
            // Ignore cleanup errors to prevent test failures
        }

        GC.SuppressFinalize(this);
    }
}
