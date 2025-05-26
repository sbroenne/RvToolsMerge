//-----------------------------------------------------------------------
// <copyright file="AnonymizationServiceExtendedTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using ClosedXML.Excel;
using RVToolsMerge.Services;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

[Collection("SpectreConsole")]
/// <summary>
/// Extended tests for the AnonymizationService.
/// </summary>
public class AnonymizationServiceExtendedTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that GetAnonymizationStatistics returns non-null result.
    /// </summary>
    [Fact]
    public void GetAnonymizationStatistics_ReturnsNonNullResult()
    {
        // Act
        var result = AnonymizationService.GetAnonymizationStatistics();

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests anonymization of various column types.
    /// </summary>
    [Theory]
    [InlineData("VM", "test-vm-01", 0)]
    [InlineData("Datacenter", "dc-01", 0)]
    [InlineData("Cluster", "cluster-01", 0)]
    [InlineData("Host", "esxi-01.local", 0)]
    [InlineData("DNS Name", "server01.example.com", 0)]
    [InlineData("Primary IP Address", "192.168.1.10", 0)]
    public void AnonymizeValue_DifferentColumnTypes_AnonymizesCorrectly(string columnName, string value, int columnIndex)
    {
        // Arrange
        var columnIndices = new Dictionary<string, int> { { columnName, columnIndex } };
        var originalValue = (XLCellValue)value;

        // Act
        var result = AnonymizationService.AnonymizeValue(originalValue, columnIndex, columnIndices);

        // Assert
        Assert.NotEqual(originalValue, result);

        // Verify prefix based on column type
        string expectedPrefix = columnName.ToLowerInvariant().Split(' ')[0];
        // Special case for "Primary IP Address" which uses "ip" as prefix
        if (expectedPrefix == "primary")
        {
            expectedPrefix = "ip";
        }
        Assert.StartsWith(expectedPrefix, result.ToString().ToLowerInvariant());
    }

    /// <summary>
    /// Tests that different types of columns are anonymized with different prefixes.
    /// </summary>
    [Fact]
    public void AnonymizeValue_MultipleColumnTypes_UsesDifferentPrefixes()
    {
        // Arrange
        var vmColumnIndices = new Dictionary<string, int> { { "VM", 0 } };
        var hostColumnIndices = new Dictionary<string, int> { { "Host", 0 } };
        var ipColumnIndices = new Dictionary<string, int> { { "Primary IP Address", 0 } };

        var originalValue = (XLCellValue)"test-value";

        // Act
        var vmResult = AnonymizationService.AnonymizeValue(originalValue, 0, vmColumnIndices);
        var hostResult = AnonymizationService.AnonymizeValue(originalValue, 0, hostColumnIndices);
        var ipResult = AnonymizationService.AnonymizeValue(originalValue, 0, ipColumnIndices);

        // Assert - different columns should have different prefixes
        Assert.StartsWith("vm", vmResult.ToString().ToLowerInvariant());
        Assert.StartsWith("host", hostResult.ToString().ToLowerInvariant());
        Assert.StartsWith("ip", ipResult.ToString().ToLowerInvariant());

        // Different column types should produce different anonymized values
        Assert.NotEqual(vmResult, hostResult);
        Assert.NotEqual(vmResult, ipResult);
        Assert.NotEqual(hostResult, ipResult);
    }

    /// <summary>
    /// Tests that anonymization is consistent for the same input.
    /// </summary>
    [Fact]
    public void AnonymizeValue_SameInput_ProducesSameOutput()
    {
        // Arrange
        var columnIndices = new Dictionary<string, int> { { "VM", 0 } };
        var originalValue = (XLCellValue)"test-vm-01";

        // Act
        var result1 = AnonymizationService.AnonymizeValue(originalValue, 0, columnIndices);
        var result2 = AnonymizationService.AnonymizeValue(originalValue, 0, columnIndices);

        // Assert
        Assert.Equal(result1, result2);
    }

    /// <summary>
    /// Tests that empty values are not anonymized.
    /// </summary>
    [Fact]
    public void AnonymizeValue_EmptyValue_ReturnsOriginal()
    {
        // Arrange
        var columnIndices = new Dictionary<string, int> { { "VM", 0 } };
        var originalValue = (XLCellValue)string.Empty;

        // Act
        var result = AnonymizationService.AnonymizeValue(originalValue, 0, columnIndices);

        // Assert
        Assert.Equal(originalValue, result);
    }

    /// <summary>
    /// Tests that white space values are not anonymized.
    /// </summary>
    [Fact]
    public void AnonymizeValue_WhitespaceValue_ReturnsOriginal()
    {
        // Arrange
        var columnIndices = new Dictionary<string, int> { { "VM", 0 } };
        var originalValue = (XLCellValue)"   ";

        // Act
        var result = AnonymizationService.AnonymizeValue(originalValue, 0, columnIndices);

        // Assert
        Assert.Equal(originalValue, result);
    }

    /// <summary>
    /// Tests that anonymization statistics are updated correctly.
    /// </summary>
    [Fact]
    public void GetAnonymizationStatistics_AfterAnonymization_ReturnsCorrectCounts()
    {
        // Arrange - Use a new instance to isolate the test
        var service = new AnonymizationService();
        var vmColumnIndices = new Dictionary<string, int> { { "VM", 0 } };
        var hostColumnIndices = new Dictionary<string, int> { { "Host", 0 } };

        // Act - Anonymize multiple values
        service.AnonymizeValue((XLCellValue)"vm1", 0, vmColumnIndices);
        service.AnonymizeValue((XLCellValue)"vm2", 0, vmColumnIndices);
        service.AnonymizeValue((XLCellValue)"host1", 0, hostColumnIndices);

        var stats = service.GetAnonymizationStatistics();        // Assert
        Assert.Equal(2, stats["VMs"]);
        Assert.Equal(1, stats["Hosts"]);
        Assert.Equal(0, stats["Clusters"]); // Not anonymized
    }

    /// <summary>
    /// Tests that anonymization mappings are correct after anonymization.
    /// </summary>
    [Fact]
    public void GetAnonymizationMappings_AfterAnonymization_ReturnsCorrectMappings()
    {
        // Arrange - Use a new instance to isolate the test
        var service = new AnonymizationService();
        var vmColumnIndices = new Dictionary<string, int> { { "VM", 0 } };
        var hostColumnIndices = new Dictionary<string, int> { { "Host", 0 } };

        // Original values
        string vm1Original = "server1";
        string vm2Original = "server2";
        string hostOriginal = "host.example.com";

        // Act - Anonymize values
        var vm1Anonymized = service.AnonymizeValue((XLCellValue)vm1Original, 0, vmColumnIndices);
        var vm2Anonymized = service.AnonymizeValue((XLCellValue)vm2Original, 0, vmColumnIndices);
        var hostAnonymized = service.AnonymizeValue((XLCellValue)hostOriginal, 0, hostColumnIndices);

        // Get mappings
        var mappings = service.GetAnonymizationMappings();

        // Assert
        // Check that we have VM and Host mappings
        Assert.True(mappings.ContainsKey("VMs"));
        Assert.True(mappings.ContainsKey("Hosts"));

        // Check VM mappings
        Assert.Equal(2, mappings["VMs"].Count);
        Assert.Equal(vm1Anonymized.ToString(), mappings["VMs"][vm1Original]);
        Assert.Equal(vm2Anonymized.ToString(), mappings["VMs"][vm2Original]);
        // Check Host mappings
        Assert.Single(mappings["Hosts"]);
        Assert.Equal(hostAnonymized.ToString(), mappings["Hosts"][hostOriginal]);
        // Check empty mappings
        Assert.Empty(mappings["Clusters"]);
        Assert.Empty(mappings["Datacenters"]);
        Assert.Empty(mappings["DNS Names"]);
        Assert.Empty(mappings["IP Addresses"]);
    }
}
