//-----------------------------------------------------------------------
// <copyright file="SheetConfiguration.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using System.Collections.Frozen;

namespace RVToolsMerge.Configuration;

/// <summary>
/// Contains configuration for RVTools sheets, required sheets, and column mappings.
/// </summary>
public static class SheetConfiguration
{
    /// <summary>
    /// Names of sheets that are required by default.
    /// </summary>
    public static readonly string[] RequiredSheets = ["vInfo", "vHost", "vPartition", "vMemory"];

    /// <summary>
    /// Names of sheets that are minimally required.
    /// </summary>
    public static readonly string[] MinimumRequiredSheets = ["vInfo"];

    /// <summary>
    /// Maps original RVTools column headers to their standardized names for each sheet.
    /// </summary>
    public static readonly FrozenDictionary<string, FrozenDictionary<string, string>> SheetColumnHeaderMappings =
        new Dictionary<string, FrozenDictionary<string, string>>
        {
            // vInfo sheet mappings
            ["vInfo"] = new Dictionary<string, string>
            {
                { "vInfoVMName", "VM" },
                { "vInfoUUID", "VM UUID" },
                { "vInfoPowerstate", "Powerstate" },
                { "vInfoTemplate", "Template" },
                { "vInfoGuestHostName", "DNS Name" },
                { "vInfoCPUs", "CPUs" },
                { "vInfoMemory", "Memory" },
                { "vInfoProvisioned", "Provisioned MiB" },
                { "vInfoInUse", "In Use MiB" },
                { "vInfoDataCenter", "Datacenter" },
                { "vInfoCluster", "Cluster" },
                { "vInfoHost", "Host" },
                { "vInfoSRMPlaceHolder", "SRM Placeholder" },
                { "vInfoOSTools", "OS according to the VMware Tools" },
                { "vInfoOS", "OS according to the configuration file" },
                { "vInfoPrimaryIPAddress", "Primary IP Address" },
                { "vInfoNetwork1",  "Network #1" },
                { "vInfoNetwork2",  "Network #2" },
                { "vInfoNetwork3",  "Network #3" },
                { "vInfoNetwork4",  "Network #4" },
                { "vInfoNetwork5",  "Network #5" },
                { "vInfoNetwork6",  "Network #6" },
                { "vInfoNetwork7",  "Network #7" },
                { "vInfoNetwork8",  "Network #8" },
                { "vInfoResourcepool",  "Resource pool" },
                { "vInfoFolder", "Folder" },
                { "vInfoCreateDate", "Creation Date" },
                { "vInfoNICs", "NICs" },
                { "vInfoNumVirtualDisks", "Disks" }
            }.ToFrozenDictionary(),

            // vHost sheet mappings
            ["vHost"] = new Dictionary<string, string>
            {
                { "vHostName", "Host" },
                { "vHostDatacenter", "Datacenter" },
                { "vHostCluster", "Cluster" },
                { "vHostvSANFaultDomainName", "vSAN Fault Domain Name" },
                { "vHostCpuModel", "CPU Model" },
                { "vHostCpuMhz", "Speed" },
                { "vHostNumCpu", "# CPU" },
                { "vHostCoresPerCPU", "Cores per CPU" },
                { "vHostNumCpuCores", "# Cores" },
                { "vHostOverallCpuUsage", "CPU usage %" },
                { "vHostMemorySize", "# Memory" },
                { "vHostOverallMemoryUsage", "Memory usage %" },
                { "vHostvCPUs", "# vCPUs" },
                { "vHostVCPUsPerCore", "vCPUs per Core" }
            }.ToFrozenDictionary(),

            // vPartition sheet mappings
            ["vPartition"] = new Dictionary<string, string>
            {
                { "vPartitionDisk", "Disk" },
                { "vPartitionVMName", "VM" },
                { "vPartitionUUID", "VM UUID" },
                { "vPartitionConsumedMiB", "Consumed MiB" },
                { "vPartitionCapacityMiB", "Capacity MiB" }
            }.ToFrozenDictionary(),

            // vMemory sheet mappings
            ["vMemory"] = new Dictionary<string, string>
            {
                { "vMemoryVMName", "VM" },
                { "vMemoryUUID", "VM UUID" },
                { "vMemorySizeMiB", "Size MiB" },
                { "vMemoryReservation", "Reservation" }
            }.ToFrozenDictionary()
        }.ToFrozenDictionary();

    /// <summary>
    /// Mandatory columns for each sheet type.
    /// </summary>
    public static readonly FrozenDictionary<string, string[]> MandatoryColumns = new Dictionary<string, string[]>
    {
        { "vInfo", ["VM UUID", "Template", "SRM Placeholder", "Powerstate", "VM", "CPUs", "Memory", "In Use MiB", "OS according to the configuration file", "Creation Date", "NICs", "Disks", "Provisioned MiB"] },
        { "vHost", ["Host", "Datacenter", "Cluster", "CPU Model", "Speed", "# CPU", "Cores per CPU", "# Cores", "CPU usage %", "# Memory", "Memory usage %"] },
        { "vPartition", ["VM UUID", "VM", "Disk", "Capacity MiB", "Consumed MiB"] },
        { "vMemory", ["VM UUID", "VM", "Size MiB", "Reservation"] }
    }.ToFrozenDictionary();
}
