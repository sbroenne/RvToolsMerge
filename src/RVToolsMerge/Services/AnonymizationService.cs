//-----------------------------------------------------------------------
// <copyright file="AnonymizationService.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using ClosedXML.Excel;
using RVToolsMerge.Services.Interfaces;

namespace RVToolsMerge.Services;

/// <summary>
/// Service for anonymizing RVTools data.
/// </summary>
public class AnonymizationService : IAnonymizationService
{
    // Dynamic mapping of column name to file-specific original-to-anonymized mappings
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _anonymizationMaps = [];

    // Configuration for column identifiers - maps column name to prefix
    private readonly Dictionary<string, string> _columnIdentifiers = new()
    {
        { "VM", "vm" },
        { "DNS Name", "dns" },
        { "Cluster", "cluster" },
        { "Host", "host" },
        { "Datacenter", "datacenter" },
        { "Primary IP Address", "ip" }
    };

    /// <summary>
    /// Adds or updates a column identifier for anonymization.
    /// </summary>
    /// <param name="columnName">The name of the column to anonymize.</param>
    /// <param name="prefix">The prefix to use for anonymized values.</param>
    public void AddColumnIdentifier(string columnName, string prefix)
    {
        _columnIdentifiers[columnName] = prefix;

        // Initialize the mapping dictionary for this column if it doesn't exist
        if (!_anonymizationMaps.ContainsKey(columnName))
        {
            _anonymizationMaps[columnName] = [];
        }
    }

    /// <summary>
    /// Removes a column identifier from anonymization.
    /// </summary>
    /// <param name="columnName">The name of the column to remove.</param>
    public void RemoveColumnIdentifier(string columnName)
    {
        _columnIdentifiers.Remove(columnName);
    }

    /// <summary>
    /// Gets all configured column identifiers.
    /// </summary>
    /// <returns>Dictionary mapping column names to their prefix.</returns>
    public Dictionary<string, string> GetColumnIdentifiers()
    {
        return new Dictionary<string, string>(_columnIdentifiers);
    }

    /// <summary>
    /// Anonymizes a cell value based on its column type.
    /// </summary>
    /// <param name="value">The original cell value.</param>
    /// <param name="currentColumnIndex">The index of the current column.</param>
    /// <param name="anonymizeColumnIndices">Dictionary mapping column names to indices for anonymization.</param>
    /// <param name="fileName">The name of the file being processed.</param>
    /// <returns>The anonymized cell value.</returns>
    public XLCellValue AnonymizeValue(XLCellValue value, int currentColumnIndex, Dictionary<string, int> anonymizeColumnIndices, string fileName)
    {
        // Quick check for empty values to avoid unnecessary processing
        var stringValue = value.ToString();
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            return value;
        }

        // Find the column that matches the current index
        foreach (var kvp in anonymizeColumnIndices)
        {
            if (kvp.Value == currentColumnIndex && _columnIdentifiers.TryGetValue(kvp.Key, out string? prefix))
            {
                return GetOrCreateAnonymizedName(value, kvp.Key, prefix, fileName);
            }
        }

        // Return original value if no anonymization is needed
        return value;
    }/// <summary>
         /// Gets the current anonymization statistics.
         /// </summary>
         /// <returns>Dictionary with counts of anonymized items by column and file.</returns>
    public Dictionary<string, Dictionary<string, int>> GetAnonymizationStatistics()
    {
        var stats = new Dictionary<string, Dictionary<string, int>>();

        // Map internal column names to display names expected by tests
        var displayNameMapping = new Dictionary<string, string>
        {
            { "VM", "VMs" },
            { "Host", "Hosts" },
            { "Cluster", "Clusters" },
            { "Datacenter", "Datacenters" },
            { "DNS Name", "DNS Names" },
            { "Primary IP Address", "IP Addresses" }
        };

        // Initialize all categories with empty dictionaries
        foreach (var displayName in displayNameMapping.Values)
        {
            stats[displayName] = new Dictionary<string, int>();
        }

        // Populate with actual data
        foreach (var columnKvp in _anonymizationMaps)
        {
            var displayName = displayNameMapping.TryGetValue(columnKvp.Key, out string? mapped) ? mapped : columnKvp.Key;
            var columnStats = new Dictionary<string, int>();
            foreach (var fileMap in columnKvp.Value)
            {
                columnStats[fileMap.Key] = fileMap.Value.Count;
            }
            stats[displayName] = columnStats;
        }

        return stats;
    }    /// <summary>
         /// Gets all anonymization mappings from original values to anonymized values.
         /// </summary>
         /// <returns>Dictionary mapping column names to dictionaries of file names to mappings of original-to-anonymized values.</returns>
    public Dictionary<string, Dictionary<string, Dictionary<string, string>>> GetAnonymizationMappings()
    {
        var result = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

        // Map internal column names to display names expected by tests
        var displayNameMapping = new Dictionary<string, string>
        {
            { "VM", "VMs" },
            { "Host", "Hosts" },
            { "Cluster", "Clusters" },
            { "Datacenter", "Datacenters" },
            { "DNS Name", "DNS Names" },
            { "Primary IP Address", "IP Addresses" }
        };

        // Initialize all categories with empty dictionaries
        foreach (var displayName in displayNameMapping.Values)
        {
            result[displayName] = new Dictionary<string, Dictionary<string, string>>();
        }

        // Populate with actual data
        foreach (var columnKvp in _anonymizationMaps)
        {
            var displayName = displayNameMapping.TryGetValue(columnKvp.Key, out string? mapped) ? mapped : columnKvp.Key;
            result[displayName] = new Dictionary<string, Dictionary<string, string>>(columnKvp.Value);
        }

        return result;
    }

    /// <summary>
    /// Gets or creates an anonymized name for a given original value.
    /// </summary>
    /// <param name="originalValue">The original value to anonymize.</param>
    /// <param name="columnName">The column name for this anonymization.</param>
    /// <param name="prefix">The prefix to use for anonymized names.</param>
    /// <param name="fileName">The name of the file being processed.</param>
    /// <returns>The anonymized name.</returns>
    private XLCellValue GetOrCreateAnonymizedName(
        XLCellValue originalValue,
        string columnName,
        string prefix,
        string fileName)
    {
        var lookupValue = originalValue.ToString();
        if (string.IsNullOrWhiteSpace(lookupValue))
        {
            return originalValue; // Return original value if empty
        }

        // Ensure column exists in mappings
        if (!_anonymizationMaps.TryGetValue(columnName, out Dictionary<string, Dictionary<string, string>>? columnMap))
        {
            columnMap = [];
            _anonymizationMaps[columnName] = columnMap;
        }

        // Ensure dictionary for this file exists
        if (!columnMap.TryGetValue(fileName, out Dictionary<string, string>? nameMap))
        {
            nameMap = [];
            columnMap[fileName] = nameMap;
        }

        if (!nameMap.TryGetValue(lookupValue, out string? value))
        {
            // Use a more efficient hash-based approach for generating consistent values
            var combinedHash = HashCode.Combine(fileName, lookupValue, columnName);
            var fileIdentifier = Math.Abs(combinedHash) % 1000;
            var counter = nameMap.Count + 1;
            
            value = $"{prefix}{fileIdentifier}_{counter}";
            nameMap[lookupValue] = value;
        }
        return value;
    }
}
