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
    // Dictionaries for anonymization
    private readonly Dictionary<string, string> _vmNameMap = [];
    private readonly Dictionary<string, string> _dnsNameMap = [];
    private readonly Dictionary<string, string> _clusterNameMap = [];
    private readonly Dictionary<string, string> _hostNameMap = [];
    private readonly Dictionary<string, string> _datacenterNameMap = [];
    private readonly Dictionary<string, string> _ipAddressMap = [];

    /// <summary>
    /// Anonymizes a cell value based on its column type.
    /// </summary>
    /// <param name="value">The original cell value.</param>
    /// <param name="currentColumnIndex">The index of the current column.</param>
    /// <param name="anonymizeColumnIndices">Dictionary mapping column names to indices for anonymization.</param>
    /// <returns>The anonymized cell value.</returns>
    public XLCellValue AnonymizeValue(XLCellValue value, int currentColumnIndex, Dictionary<string, int> anonymizeColumnIndices)
    {
        // VM Name
        if (anonymizeColumnIndices.TryGetValue("VM", out int vmColIndex) &&
            currentColumnIndex == vmColIndex)
        {
            return GetOrCreateAnonymizedName(value, _vmNameMap, "vm");
        }
        // DNS Name
        else if (anonymizeColumnIndices.TryGetValue("DNS Name", out int dnsColIndex) &&
                currentColumnIndex == dnsColIndex)
        {
            return GetOrCreateAnonymizedName(value, _dnsNameMap, "dns");
        }
        // Cluster Name
        else if (anonymizeColumnIndices.TryGetValue("Cluster", out int clusterColIndex) &&
                currentColumnIndex == clusterColIndex)
        {
            return GetOrCreateAnonymizedName(value, _clusterNameMap, "cluster");
        }
        // Host Name
        else if (anonymizeColumnIndices.TryGetValue("Host", out int hostColIndex) &&
                currentColumnIndex == hostColIndex)
        {
            return GetOrCreateAnonymizedName(value, _hostNameMap, "host");
        }
        // Datacenter Name
        else if (anonymizeColumnIndices.TryGetValue("Datacenter", out int datacenterColIndex) &&
                currentColumnIndex == datacenterColIndex)
        {
            return GetOrCreateAnonymizedName(value, _datacenterNameMap, "datacenter");
        }
        // IP Address
        else if (anonymizeColumnIndices.TryGetValue("Primary IP Address", out int ipAddressColIndex) &&
                currentColumnIndex == ipAddressColIndex)
        {
            return GetOrCreateAnonymizedName(value, _ipAddressMap, "ip");
        }
        // Return original value if no anonymization is needed
        return value;
    }

    /// <summary>
    /// Gets the current anonymization statistics.
    /// </summary>
    /// <returns>Dictionary with counts of anonymized items by category.</returns>
    public Dictionary<string, int> GetAnonymizationStatistics()
    {
        return new Dictionary<string, int>
        {
            { "VMs", _vmNameMap.Count },
            { "IP Address", _ipAddressMap.Count },
            { "DNS Names", _dnsNameMap.Count },
            { "Clusters", _clusterNameMap.Count },
            { "Hosts", _hostNameMap.Count },
            { "Datacenters", _datacenterNameMap.Count },
            { "IP Addresses", _ipAddressMap.Count }
        };
    }

    /// <summary>
    /// Gets or creates an anonymized name for a given original value.
    /// </summary>
    /// <param name="originalValue">The original value to anonymize.</param>
    /// <param name="nameMap">The mapping dictionary to use.</param>
    /// <param name="prefix">The prefix to use for anonymized names.</param>
    /// <returns>The anonymized name.</returns>
    private static XLCellValue GetOrCreateAnonymizedName(
        XLCellValue originalValue,
        Dictionary<string, string> nameMap,
        string prefix)
    {
        var lookupValue = originalValue.ToString();
        if (string.IsNullOrWhiteSpace(lookupValue))
        {
            return originalValue; // Return original value if empty
        }

        if (!nameMap.TryGetValue(lookupValue, out string? value))
        {
            value = $"{prefix}{nameMap.Count + 1}";
            nameMap[lookupValue] = value;
        }
        return value;
    }
}
