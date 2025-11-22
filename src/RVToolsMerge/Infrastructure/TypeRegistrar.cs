//-----------------------------------------------------------------------
// <copyright file="TypeRegistrar.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace RVToolsMerge.Infrastructure;

/// <summary>
/// Type registrar for integrating Microsoft.Extensions.DependencyInjection with Spectre.Console.Cli.
/// </summary>
public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeRegistrar"/> class.
    /// </summary>
    /// <param name="services">The service collection to use.</param>
    public TypeRegistrar(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Registers a type with the dependency injection container.
    /// </summary>
    /// <param name="service">The service type to register.</param>
    /// <param name="implementation">The implementation type.</param>
    public void Register(Type service, Type implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    /// <summary>
    /// Registers an instance with the dependency injection container.
    /// </summary>
    /// <param name="service">The service type to register.</param>
    /// <param name="implementation">The implementation instance.</param>
    public void RegisterInstance(Type service, object implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    /// <summary>
    /// Registers a lazy type with the dependency injection container.
    /// </summary>
    /// <param name="service">The service type to register.</param>
    /// <param name="factory">The factory function to create the implementation.</param>
    public void RegisterLazy(Type service, Func<object> factory)
    {
        _services.AddSingleton(service, _ => factory());
    }

    /// <summary>
    /// Builds the service provider.
    /// </summary>
    /// <returns>A type resolver wrapping the service provider.</returns>
    public ITypeResolver Build()
    {
        return new TypeResolver(_services.BuildServiceProvider());
    }
}
