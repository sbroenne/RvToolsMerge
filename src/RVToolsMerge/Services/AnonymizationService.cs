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

    // Constants for column identifiers
    private const string VmColumnName = "VM";
    private const string DnsNameColumnName = "DNS Name";
    private const string ClusterColumnName = "Cluster";
    private const string HostColumnName = "Host";
    private const string DatacenterColumnName = "Datacenter";
    private const string IpAddressColumnName = "Primary IP Address";

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
        if (ShouldAnonymizeColumn(anonymizeColumnIndices, VmColumnName, currentColumnIndex))
        {
            return GetOrCreateAnonymizedName(value, _vmNameMap, "vm");
        }
        // DNS Name
        else if (ShouldAnonymizeColumn(anonymizeColumnIndices, DnsNameColumnName, currentColumnIndex))
        {
            return GetOrCreateAnonymizedName(value, _dnsNameMap, "dns");
        }
        // Cluster Name
        else if (ShouldAnonymizeColumn(anonymizeColumnIndices, ClusterColumnName, currentColumnIndex))
        {
            return GetOrCreateAnonymizedName(value, _clusterNameMap, "cluster");
        }
        // Host Name
        else if (ShouldAnonymizeColumn(anonymizeColumnIndices, HostColumnName, currentColumnIndex))
        {
            return GetOrCreateAnonymizedName(value, _hostNameMap, "host");
        }
        // Datacenter Name
        else if (ShouldAnonymizeColumn(anonymizeColumnIndices, DatacenterColumnName, currentColumnIndex))
        {
            return GetOrCreateAnonymizedName(value, _datacenterNameMap, "datacenter");
        }
        // IP Address
        else if (ShouldAnonymizeColumn(anonymizeColumnIndices, IpAddressColumnName, currentColumnIndex))
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
            { "DNS Names", _dnsNameMap.Count },
            { "Clusters", _clusterNameMap.Count },
            { "Hosts", _hostNameMap.Count },
            { "Datacenters", _datacenterNameMap.Count },
            { "IP Addresses", _ipAddressMap.Count }
        };
    }
    
    /// <summary>
    /// Gets all anonymization mappings from original values to anonymized values.
    /// </summary>
    /// <returns>Dictionary mapping category names to dictionaries of original-to-anonymized value mappings.</returns>
    public Dictionary<string, Dictionary<string, string>> GetAnonymizationMappings()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "VMs", new Dictionary<string, string>(_vmNameMap) },
            { "DNS Names", new Dictionary<string, string>(_dnsNameMap) },
            { "Clusters", new Dictionary<string, string>(_clusterNameMap) },
            { "Hosts", new Dictionary<string, string>(_hostNameMap) },
            { "Datacenters", new Dictionary<string, string>(_datacenterNameMap) },
            { "IP Addresses", new Dictionary<string, string>(_ipAddressMap) }
        };
    }

    /// <summary>
    /// Determines if a column should be anonymized.
    /// </summary>
    /// <param name="anonymizeColumnIndices">Dictionary of column indices for anonymization.</param>
    /// <param name="columnName">The name of the column to check.</param>
    /// <param name="currentColumnIndex">The current column index being processed.</param>
    /// <returns>True if the column should be anonymized; otherwise, false.</returns>
    private static bool ShouldAnonymizeColumn(
        Dictionary<string, int> anonymizeColumnIndices,
        string columnName,
        int currentColumnIndex)
    {
        return anonymizeColumnIndices.TryGetValue(columnName, out int colIndex) &&
               currentColumnIndex == colIndex;
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
        if (String.IsNullOrWhiteSpace(lookupValue))
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
