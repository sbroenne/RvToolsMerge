//-----------------------------------------------------------------------
// <copyright file="SpectreConsoleCollection.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using Xunit;

namespace RVToolsMerge.IntegrationTests.Utilities;

/// <summary>
/// Collection fixture to make tests using Spectre.Console run sequentially 
/// to avoid exclusivity mode issues.
/// </summary>
[CollectionDefinition("SpectreConsole")]
public class SpectreConsoleCollection : ICollectionFixture<SpectreConsoleFixture>
{
    // This class has no code, it's just the definition for the collection
}

/// <summary>
/// Fixture for the SpectreConsole collection, providing shared setup for tests.
/// </summary>
public class SpectreConsoleFixture
{
    // This class can be empty or contain any shared setup needed for tests
}