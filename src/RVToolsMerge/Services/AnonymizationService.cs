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
    // Dictionaries for anonymization - mapping from filename to original value to anonymized value
    private readonly Dictionary<string, Dictionary<string, string>> _vmNameMap = [];
    private readonly Dictionary<string, Dictionary<string, string>> _dnsNameMap = [];
    private readonly Dictionary<string, Dictionary<string, string>> _clusterNameMap = [];
    private readonly Dictionary<string, Dictionary<string, string>> _hostNameMap = [];
    private readonly Dictionary<string, Dictionary<string, string>> _datacenterNameMap = [];
    private readonly Dictionary<string, Dictionary<string, string>> _ipAddressMap = [];

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
    /// <param name="fileName">The name of the file being processed.</param>
    /// <returns>The anonymized cell value.</returns>
    public XLCellValue AnonymizeValue(XLCellValue value, int currentColumnIndex, Dictionary<string, int> anonymizeColumnIndices, string fileName)
    {
        // VM Name
        if (ShouldAnonymizeColumn(anonymizeColumnIndices, VmColumnName, currentColumnIndex))
        {
            return GetOrCreateAnonymizedName(value, _vmNameMap, "vm", fileName);
        }
        // DNS Name
        else if (ShouldAnonymizeColumn(anonymizeColumnIndices, DnsNameColumnName, currentColumnIndex))
        {
            return GetOrCreateAnonymizedName(value, _dnsNameMap, "dns", fileName);
        }
        // Cluster Name
        else if (ShouldAnonymizeColumn(anonymizeColumnIndices, ClusterColumnName, currentColumnIndex))
        {
            return GetOrCreateAnonymizedName(value, _clusterNameMap, "cluster", fileName);
        }
        // Host Name
        else if (ShouldAnonymizeColumn(anonymizeColumnIndices, HostColumnName, currentColumnIndex))
        {
            return GetOrCreateAnonymizedName(value, _hostNameMap, "host", fileName);
        }
        // Datacenter Name
        else if (ShouldAnonymizeColumn(anonymizeColumnIndices, DatacenterColumnName, currentColumnIndex))
        {
            return GetOrCreateAnonymizedName(value, _datacenterNameMap, "datacenter", fileName);
        }
        // IP Address
        else if (ShouldAnonymizeColumn(anonymizeColumnIndices, IpAddressColumnName, currentColumnIndex))
        {
            return GetOrCreateAnonymizedName(value, _ipAddressMap, "ip", fileName);
        }
        // Return original value if no anonymization is needed
        return value;
    }

    /// <summary>
    /// Gets the current anonymization statistics.
    /// </summary>
    /// <returns>Dictionary with counts of anonymized items by category and file.</returns>
    public Dictionary<string, Dictionary<string, int>> GetAnonymizationStatistics()
    {
        var stats = new Dictionary<string, Dictionary<string, int>>();
        
        // Process VM names
        var vmStats = new Dictionary<string, int>();
        foreach (var fileMap in _vmNameMap)
        {
            vmStats[fileMap.Key] = fileMap.Value.Count;
        }
        stats["VMs"] = vmStats;
        
        // Process DNS names
        var dnsStats = new Dictionary<string, int>();
        foreach (var fileMap in _dnsNameMap)
        {
            dnsStats[fileMap.Key] = fileMap.Value.Count;
        }
        stats["DNS Names"] = dnsStats;
        
        // Process cluster names
        var clusterStats = new Dictionary<string, int>();
        foreach (var fileMap in _clusterNameMap)
        {
            clusterStats[fileMap.Key] = fileMap.Value.Count;
        }
        stats["Clusters"] = clusterStats;
        
        // Process host names
        var hostStats = new Dictionary<string, int>();
        foreach (var fileMap in _hostNameMap)
        {
            hostStats[fileMap.Key] = fileMap.Value.Count;
        }
        stats["Hosts"] = hostStats;
        
        // Process datacenter names
        var dcStats = new Dictionary<string, int>();
        foreach (var fileMap in _datacenterNameMap)
        {
            dcStats[fileMap.Key] = fileMap.Value.Count;
        }
        stats["Datacenters"] = dcStats;
        
        // Process IP addresses
        var ipStats = new Dictionary<string, int>();
        foreach (var fileMap in _ipAddressMap)
        {
            ipStats[fileMap.Key] = fileMap.Value.Count;
        }
        stats["IP Addresses"] = ipStats;
        
        return stats;
    }
    
    /// <summary>
    /// Gets all anonymization mappings from original values to anonymized values.
    /// </summary>
    /// <returns>Dictionary mapping category names to dictionaries of file names to mappings of original-to-anonymized values.</returns>
    public Dictionary<string, Dictionary<string, Dictionary<string, string>>> GetAnonymizationMappings()
    {
        return new Dictionary<string, Dictionary<string, Dictionary<string, string>>>
        {
            { "VMs", new Dictionary<string, Dictionary<string, string>>(_vmNameMap) },
            { "DNS Names", new Dictionary<string, Dictionary<string, string>>(_dnsNameMap) },
            { "Clusters", new Dictionary<string, Dictionary<string, string>>(_clusterNameMap) },
            { "Hosts", new Dictionary<string, Dictionary<string, string>>(_hostNameMap) },
            { "Datacenters", new Dictionary<string, Dictionary<string, string>>(_datacenterNameMap) },
            { "IP Addresses", new Dictionary<string, Dictionary<string, string>>(_ipAddressMap) }
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
    /// <param name="fileNameMap">The mapping dictionary to use.</param>
    /// <param name="prefix">The prefix to use for anonymized names.</param>
    /// <param name="fileName">The name of the file being processed.</param>
    /// <returns>The anonymized name.</returns>
    private static XLCellValue GetOrCreateAnonymizedName(
        XLCellValue originalValue,
        Dictionary<string, Dictionary<string, string>> fileNameMap,
        string prefix,
        string fileName)
    {
        var lookupValue = originalValue.ToString();
        if (string.IsNullOrWhiteSpace(lookupValue))
        {
            return originalValue; // Return original value if empty
        }

        // Ensure dictionary for this file exists
        if (!fileNameMap.TryGetValue(fileName, out Dictionary<string, string>? nameMap))
        {
            nameMap = [];
            fileNameMap[fileName] = nameMap;
        }

        if (!nameMap.TryGetValue(lookupValue, out string? value))
        {
            // Use the file name as part of the seed to ensure different files 
            // generate different anonymized values for the same original value
            int fileNameSeed = Math.Abs(fileName.GetHashCode());
            int counter = nameMap.Count + 1;
            
            // Create a unique value based on the file name seed and counter
            value = $"{prefix}{fileNameSeed % 1000}_{counter}";
            nameMap[lookupValue] = value;
        }
        return value;
    }
}
