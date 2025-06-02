//-----------------------------------------------------------------------
// <copyright file="ProgramTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;

namespace RVToolsMerge.UnitTests;

/// <summary>
/// Unit tests for Program class
/// </summary>
public class ProgramTests
{
    [Fact]
    public void ConfigureServices_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Program.ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<IFileSystem>());
        Assert.NotNull(serviceProvider.GetService<IConsoleService>());
        Assert.NotNull(serviceProvider.GetService<ConsoleUIService>());
        Assert.NotNull(serviceProvider.GetService<IExcelService>());
        Assert.NotNull(serviceProvider.GetService<IAnonymizationService>());
        Assert.NotNull(serviceProvider.GetService<IValidationService>());
        Assert.NotNull(serviceProvider.GetService<IMergeService>());
        Assert.NotNull(serviceProvider.GetService<ICommandLineParser>());
        Assert.NotNull(serviceProvider.GetService<ApplicationRunner>());
    }

    [Fact]
    public void ConfigureServices_RegistersCorrectImplementations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Program.ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.IsType<FileSystem>(serviceProvider.GetService<IFileSystem>());
        Assert.IsType<SpectreConsoleService>(serviceProvider.GetService<IConsoleService>());
        Assert.IsType<ConsoleUIService>(serviceProvider.GetService<ConsoleUIService>());
        Assert.IsType<ExcelService>(serviceProvider.GetService<IExcelService>());
        Assert.IsType<AnonymizationService>(serviceProvider.GetService<IAnonymizationService>());
        Assert.IsType<ValidationService>(serviceProvider.GetService<IValidationService>());
        Assert.IsType<MergeService>(serviceProvider.GetService<IMergeService>());
        Assert.IsType<CommandLineParser>(serviceProvider.GetService<ICommandLineParser>());
        Assert.IsType<ApplicationRunner>(serviceProvider.GetService<ApplicationRunner>());
    }

    [Fact]
    public void ConfigureServices_RegistersSingletonServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Program.ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Get services twice to verify they're singletons
        var fileSystem1 = serviceProvider.GetService<IFileSystem>();
        var fileSystem2 = serviceProvider.GetService<IFileSystem>();
        Assert.Same(fileSystem1, fileSystem2);

        var consoleService1 = serviceProvider.GetService<IConsoleService>();
        var consoleService2 = serviceProvider.GetService<IConsoleService>();
        Assert.Same(consoleService1, consoleService2);

        var excelService1 = serviceProvider.GetService<IExcelService>();
        var excelService2 = serviceProvider.GetService<IExcelService>();
        Assert.Same(excelService1, excelService2);

        var anonymizationService1 = serviceProvider.GetService<IAnonymizationService>();
        var anonymizationService2 = serviceProvider.GetService<IAnonymizationService>();
        Assert.Same(anonymizationService1, anonymizationService2);

        var validationService1 = serviceProvider.GetService<IValidationService>();
        var validationService2 = serviceProvider.GetService<IValidationService>();
        Assert.Same(validationService1, validationService2);

        var mergeService1 = serviceProvider.GetService<IMergeService>();
        var mergeService2 = serviceProvider.GetService<IMergeService>();
        Assert.Same(mergeService1, mergeService2);

        var commandLineParser1 = serviceProvider.GetService<ICommandLineParser>();
        var commandLineParser2 = serviceProvider.GetService<ICommandLineParser>();
        Assert.Same(commandLineParser1, commandLineParser2);

        var applicationRunner1 = serviceProvider.GetService<ApplicationRunner>();
        var applicationRunner2 = serviceProvider.GetService<ApplicationRunner>();
        Assert.Same(applicationRunner1, applicationRunner2);
    }

    [Fact]
    public void ConfigureServices_RegistersTransientServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Program.ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Get ConsoleUIService twice to verify it's transient
        var consoleUIService1 = serviceProvider.GetService<ConsoleUIService>();
        var consoleUIService2 = serviceProvider.GetService<ConsoleUIService>();
        Assert.NotSame(consoleUIService1, consoleUIService2);
    }

    [Fact]
    public void ConfigureServices_AllServicesCanBeResolved()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Program.ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify all services can be resolved without circular dependencies
        var allServicesResolved = true;
        try
        {
            serviceProvider.GetRequiredService<IFileSystem>();
            serviceProvider.GetRequiredService<IConsoleService>();
            serviceProvider.GetRequiredService<ConsoleUIService>();
            serviceProvider.GetRequiredService<IExcelService>();
            serviceProvider.GetRequiredService<IAnonymizationService>();
            serviceProvider.GetRequiredService<IValidationService>();
            serviceProvider.GetRequiredService<IMergeService>();
            serviceProvider.GetRequiredService<ICommandLineParser>();
            serviceProvider.GetRequiredService<ApplicationRunner>();
        }
        catch (Exception)
        {
            allServicesResolved = false;
        }

        Assert.True(allServicesResolved, "All services should be resolvable");
    }
}