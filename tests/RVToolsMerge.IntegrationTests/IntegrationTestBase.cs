//-----------------------------------------------------------------------
// <copyright file="IntegrationTestBase.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using RVToolsMerge.IntegrationTests.Utilities;
using RVToolsMerge.Models;
using RVToolsMerge.Services;
using RVToolsMerge.Services.Interfaces;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Base class for integration tests, providing common setup and utilities.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    /// <summary>
    /// The mock file system used for testing.
    /// </summary>
    protected readonly MockFileSystem FileSystem;
    
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
    /// Initializes a new instance of the <see cref="IntegrationTestBase"/> class.
    /// </summary>
    protected IntegrationTestBase()
    {
        // Create a mock file system for testing with some necessary root directories
        FileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/tmp", new MockDirectoryData() },
            { "/tmp/rvtools_test", new MockDirectoryData() },
            { "/path", new MockDirectoryData() },
            { "/path/to", new MockDirectoryData() }
        });
        
        // Set up test directories
        TestInputDirectory = "/tmp/rvtools_test/input";
        TestOutputDirectory = "/tmp/rvtools_test/output";
        
        // Create the test directories
        FileSystem.Directory.CreateDirectory(TestInputDirectory);
        FileSystem.Directory.CreateDirectory(TestOutputDirectory);
        
        // Create test data generator
        TestDataGenerator = new TestDataGenerator(FileSystem, TestInputDirectory);
        
        // Set up DI container with the mock file system
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
        // Register the mock file system
        services.AddSingleton<IFileSystem>(FileSystem);
        
        // Register the mock console service instead of the real one
        services.AddSingleton<IConsoleService, MockConsoleService>();
        services.AddTransient<ConsoleUIService>();
        services.AddSingleton<IExcelService, ExcelService>();
        services.AddSingleton<IAnonymizationService, AnonymizationService>();
        services.AddSingleton<IValidationService, ValidationService>();
        services.AddSingleton<IMergeService, TestMergeService>();
        services.AddSingleton<ICommandLineParser, CommandLineParser>();
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
    /// Dispose of resources.
    /// </summary>
    public void Dispose()
    {
        ServiceProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}