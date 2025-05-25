//-----------------------------------------------------------------------
// <copyright file="TestDataGenerator.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using System.IO.Abstractions;
using ClosedXML.Excel;
using RVToolsMerge.Configuration;

namespace RVToolsMerge.IntegrationTests.Utilities;

/// <summary>
/// Generates synthetic RVTools Excel files for testing.
/// </summary>
public class TestDataGenerator
{
    private readonly IFileSystem _fileSystem;
    private readonly string _testDataDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestDataGenerator"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system to use.</param>
    /// <param name="testDataDirectory">Directory where test files will be saved.</param>
    public TestDataGenerator(IFileSystem fileSystem, string testDataDirectory)
    {
        _fileSystem = fileSystem;
        _testDataDirectory = testDataDirectory;

        // Create test data directory if it doesn't exist
        if (!_fileSystem.Directory.Exists(_testDataDirectory))
        {
            _fileSystem.Directory.CreateDirectory(_testDataDirectory);
        }
    }

    /// <summary>
    /// Creates a basic valid RVTools Excel file with all required sheets.
    /// </summary>
    /// <param name="fileName">Name of the file to create.</param>
    /// <param name="numVMs">Number of VM entries to generate.</param>
    /// <param name="numHosts">Number of host entries to generate.</param>
    /// <param name="includeAllSheets">Whether to include all sheets or just the required ones.</param>
    /// <returns>The full path to the created file.</returns>
    public string CreateValidRVToolsFile(string fileName, int numVMs = 5, int numHosts = 2, bool includeAllSheets = true)
    {
        string filePath = _fileSystem.Path.Combine(_testDataDirectory, fileName);

        using var workbook = new XLWorkbook();

        // Always create required sheets
        CreateVInfoSheet(workbook, numVMs);

        if (includeAllSheets)
        {
            CreateVHostSheet(workbook, numHosts);
            CreateVPartitionSheet(workbook, numVMs);
            CreateVMemorySheet(workbook, numVMs);
        }

        workbook.SaveAs(filePath);
        return filePath;
    }

    /// <summary>
    /// Creates a file with alternative header formats to test header mapping.
    /// </summary>
    /// <param name="fileName">Name of the file to create.</param>
    /// <param name="numVMs">Number of VM entries to generate.</param>
    /// <returns>The full path to the created file.</returns>
    public string CreateFileWithAlternativeHeaders(string fileName, int numVMs = 5)
    {
        string filePath = _fileSystem.Path.Combine(_testDataDirectory, fileName);

        using var workbook = new XLWorkbook();

        // Create vInfo sheet with alternative headers
        var vInfoSheet = workbook.AddWorksheet("vInfo");

        // Add all mandatory columns for vInfo with alternative header names
        vInfoSheet.Cell(1, 1).Value = "vInfoVMName";        // Should map to "VM"
        vInfoSheet.Cell(1, 2).Value = "vInfoPowerstate";    // Should map to "Powerstate"
        vInfoSheet.Cell(1, 3).Value = "vInfoTemplate";      // Should map to "Template"
        vInfoSheet.Cell(1, 4).Value = "vInfoCPUs";          // Should map to "CPUs"
        vInfoSheet.Cell(1, 5).Value = "vInfoMemory";        // Should map to "Memory"
        vInfoSheet.Cell(1, 6).Value = "vInfoInUse";         // Should map to "In Use MiB" 
        vInfoSheet.Cell(1, 7).Value = "vInfoOS";            // Should map to "OS according to the configuration file"
        vInfoSheet.Cell(1, 8).Value = "vInfoSRMPlaceHolder"; // Should map to "SRM Placeholder"

        // Add data rows
        for (int i = 1; i <= numVMs; i++)
        {
            vInfoSheet.Cell(i + 1, 1).Value = $"VM{i}";
            vInfoSheet.Cell(i + 1, 2).Value = i % 2 == 0 ? "poweredOn" : "poweredOff";
            vInfoSheet.Cell(i + 1, 3).Value = "FALSE";
            vInfoSheet.Cell(i + 1, 4).Value = 2 + (i % 3); // 2-4 CPUs
            vInfoSheet.Cell(i + 1, 5).Value = 4096 * i;    // Memory in MB
            vInfoSheet.Cell(i + 1, 6).Value = 2048 * i;    // In Use MiB
            vInfoSheet.Cell(i + 1, 7).Value = $"Windows Server 201{i % 2 + 8}";
            vInfoSheet.Cell(i + 1, 8).Value = "FALSE";
        }

        // Add additional required sheets with alternative headers

        // vHost sheet with alternative headers
        var vHostSheet = workbook.AddWorksheet("vHost");
        vHostSheet.Cell(1, 1).Value = "vHostName";           // Maps to "Host"
        vHostSheet.Cell(1, 2).Value = "vHostDatacenter";     // Maps to "Datacenter"
        vHostSheet.Cell(1, 3).Value = "vHostCluster";        // Maps to "Cluster"
        vHostSheet.Cell(1, 4).Value = "vHostCpuModel";       // Maps to "CPU Model"
        vHostSheet.Cell(1, 5).Value = "vHostCpuMhz";         // Maps to "Speed"
        vHostSheet.Cell(1, 6).Value = "vHostNumCPU";         // Maps to "# CPU"
        vHostSheet.Cell(1, 7).Value = "vHostCoresPerCPU";    // Maps to "Cores per CPU"
        vHostSheet.Cell(1, 8).Value = "vHostNumCpuCores";    // Maps to "# Cores"
        vHostSheet.Cell(1, 9).Value = "vHostOverallCpuUsage"; // Maps to "CPU usage %"
        vHostSheet.Cell(1, 10).Value = "vHostMemorySize";     // Maps to "# Memory"
        vHostSheet.Cell(1, 11).Value = "vHostOverallMemoryUsage"; // Maps to "Memory usage %"

        // Add sample data
        for (int i = 1; i <= 2; i++)
        {
            vHostSheet.Cell(i + 1, 1).Value = $"Host{i}";
            vHostSheet.Cell(i + 1, 2).Value = "Datacenter1";
            vHostSheet.Cell(i + 1, 3).Value = "Cluster1";
            vHostSheet.Cell(i + 1, 4).Value = "Intel Xeon Gold 6248R";
            vHostSheet.Cell(i + 1, 5).Value = 3000;
            vHostSheet.Cell(i + 1, 6).Value = 2;
            vHostSheet.Cell(i + 1, 7).Value = 12;
            vHostSheet.Cell(i + 1, 8).Value = 24;
            vHostSheet.Cell(i + 1, 9).Value = 35 + (i * 10);
            vHostSheet.Cell(i + 1, 10).Value = 192 * 1024;
            vHostSheet.Cell(i + 1, 11).Value = 40 + (i * 5);
        }

        // vPartition sheet with alternative headers
        var vPartitionSheet = workbook.AddWorksheet("vPartition");
        vPartitionSheet.Cell(1, 1).Value = "vPartitionVMName"; // Maps to "VM"
        vPartitionSheet.Cell(1, 2).Value = "vPartitionDisk";   // Maps to "Disk"
        vPartitionSheet.Cell(1, 3).Value = "vPartitionCapacityMiB"; // Maps to "Capacity MiB"
        vPartitionSheet.Cell(1, 4).Value = "vPartitionConsumedMiB"; // Maps to "Consumed MiB"

        // Add sample data
        int row = 2;
        for (int i = 1; i <= numVMs; i++)
        {
            // System disk
            vPartitionSheet.Cell(row, 1).Value = $"VM{i}";
            vPartitionSheet.Cell(row, 2).Value = "Hard disk 1";
            vPartitionSheet.Cell(row, 3).Value = 51200;
            vPartitionSheet.Cell(row, 4).Value = 25600;
            row++;
        }

        // vMemory sheet with alternative headers
        var vMemorySheet = workbook.AddWorksheet("vMemory");
        vMemorySheet.Cell(1, 1).Value = "vMemoryVMName";      // Maps to "VM"
        vMemorySheet.Cell(1, 2).Value = "vMemorySizeMiB";     // Maps to "Size MiB"
        vMemorySheet.Cell(1, 3).Value = "vMemoryReservation"; // Maps to "Reservation"

        // Add sample data
        for (int i = 1; i <= numVMs; i++)
        {
            vMemorySheet.Cell(i + 1, 1).Value = $"VM{i}";
            vMemorySheet.Cell(i + 1, 2).Value = 4096 * i;
            vMemorySheet.Cell(i + 1, 3).Value = i % 2 == 0 ? 2048 : 0;
        }

        workbook.SaveAs(filePath);
        return filePath;
    }

    /// <summary>
    /// Creates an invalid RVTools file (missing required sheet or columns).
    /// </summary>
    /// <param name="fileName">Name of the file to create.</param>
    /// <returns>The full path to the created file.</returns>
    public string CreateInvalidRVToolsFile(string fileName)
    {
        string filePath = _fileSystem.Path.Combine(_testDataDirectory, fileName);

        using var workbook = new XLWorkbook();

        // Create empty vInfo sheet without required columns
        var vInfoSheet = workbook.AddWorksheet("vInfo");
        vInfoSheet.Cell(1, 1).Value = "VM";
        vInfoSheet.Cell(1, 2).Value = "Powerstate";
        // Missing several required columns

        workbook.SaveAs(filePath);
        return filePath;
    }

    /// <summary>
    /// Creates an RVTools file with specific data for anonymization testing.
    /// </summary>
    /// <param name="fileName">Name of the file to create.</param>
    /// <returns>The full path to the created file.</returns>
    public string CreateFileForAnonymizationTesting(string fileName)
    {
        string filePath = _fileSystem.Path.Combine(_testDataDirectory, fileName);

        using var workbook = new XLWorkbook();

        var vInfoSheet = workbook.AddWorksheet("vInfo");

        // Add headers
        vInfoSheet.Cell(1, 1).Value = "VM";
        vInfoSheet.Cell(1, 2).Value = "Powerstate";
        vInfoSheet.Cell(1, 3).Value = "Template";
        vInfoSheet.Cell(1, 4).Value = "DNS Name";
        vInfoSheet.Cell(1, 5).Value = "CPUs";
        vInfoSheet.Cell(1, 6).Value = "Memory";
        vInfoSheet.Cell(1, 7).Value = "In Use MiB";
        vInfoSheet.Cell(1, 8).Value = "OS according to the configuration file";
        vInfoSheet.Cell(1, 9).Value = "Primary IP Address";
        vInfoSheet.Cell(1, 10).Value = "SRM Placeholder";

        // Add data with items that should be anonymized
        vInfoSheet.Cell(2, 1).Value = "CONFIDENTIAL-SERVER-01";
        vInfoSheet.Cell(2, 2).Value = "poweredOn";
        vInfoSheet.Cell(2, 3).Value = "FALSE";
        vInfoSheet.Cell(2, 4).Value = "server01.contoso.local";
        vInfoSheet.Cell(2, 5).Value = 4;
        vInfoSheet.Cell(2, 6).Value = 8192;
        vInfoSheet.Cell(2, 7).Value = 4096;
        vInfoSheet.Cell(2, 8).Value = "Windows Server 2019";
        vInfoSheet.Cell(2, 9).Value = "192.168.1.100";
        vInfoSheet.Cell(2, 10).Value = "FALSE";

        workbook.SaveAs(filePath);
        return filePath;
    }

    /// <summary>
    /// Creates the vInfo sheet with sample data.
    /// </summary>
    /// <param name="workbook">The workbook to add the sheet to.</param>
    /// <param name="numVMs">Number of VM entries to generate.</param>
    private void CreateVInfoSheet(XLWorkbook workbook, int numVMs)
    {
        var vInfoSheet = workbook.AddWorksheet("vInfo");

        // Add headers - using standard names as defined in the app
        vInfoSheet.Cell(1, 1).Value = "VM";
        vInfoSheet.Cell(1, 2).Value = "Powerstate";
        vInfoSheet.Cell(1, 3).Value = "Template";
        vInfoSheet.Cell(1, 4).Value = "CPUs";
        vInfoSheet.Cell(1, 5).Value = "Memory";
        vInfoSheet.Cell(1, 6).Value = "In Use MiB";
        vInfoSheet.Cell(1, 7).Value = "OS according to the configuration file";
        vInfoSheet.Cell(1, 8).Value = "Datacenter";
        vInfoSheet.Cell(1, 9).Value = "Cluster";
        vInfoSheet.Cell(1, 10).Value = "Host";
        vInfoSheet.Cell(1, 11).Value = "SRM Placeholder";

        // Add data rows
        for (int i = 1; i <= numVMs; i++)
        {
            vInfoSheet.Cell(i + 1, 1).Value = $"TestVM{i:D2}";
            vInfoSheet.Cell(i + 1, 2).Value = i % 2 == 0 ? "poweredOn" : "poweredOff";
            vInfoSheet.Cell(i + 1, 3).Value = "FALSE";
            vInfoSheet.Cell(i + 1, 4).Value = 2 + (i % 3); // 2-4 CPUs
            vInfoSheet.Cell(i + 1, 5).Value = 4096 * i;    // Memory in MB
            vInfoSheet.Cell(i + 1, 6).Value = 2048 * i;    // In Use MiB
            vInfoSheet.Cell(i + 1, 7).Value = $"Windows Server 201{i % 2 + 8}";
            vInfoSheet.Cell(i + 1, 8).Value = "Datacenter1";
            vInfoSheet.Cell(i + 1, 9).Value = "Cluster1";
            vInfoSheet.Cell(i + 1, 10).Value = $"Host{i % 2 + 1}";
            vInfoSheet.Cell(i + 1, 11).Value = "FALSE";
        }
    }

    /// <summary>
    /// Creates the vHost sheet with sample data.
    /// </summary>
    /// <param name="workbook">The workbook to add the sheet to.</param>
    /// <param name="numHosts">Number of host entries to generate.</param>
    private void CreateVHostSheet(XLWorkbook workbook, int numHosts)
    {
        var vHostSheet = workbook.AddWorksheet("vHost");

        // Add headers
        vHostSheet.Cell(1, 1).Value = "Host";
        vHostSheet.Cell(1, 2).Value = "Datacenter";
        vHostSheet.Cell(1, 3).Value = "Cluster";
        vHostSheet.Cell(1, 4).Value = "CPU Model";
        vHostSheet.Cell(1, 5).Value = "Speed";
        vHostSheet.Cell(1, 6).Value = "# CPU";
        vHostSheet.Cell(1, 7).Value = "Cores per CPU";
        vHostSheet.Cell(1, 8).Value = "# Cores";
        vHostSheet.Cell(1, 9).Value = "CPU usage %";
        vHostSheet.Cell(1, 10).Value = "# Memory";
        vHostSheet.Cell(1, 11).Value = "Memory usage %";

        // Add data rows
        for (int i = 1; i <= numHosts; i++)
        {
            vHostSheet.Cell(i + 1, 1).Value = $"Host{i}";
            vHostSheet.Cell(i + 1, 2).Value = "Datacenter1";
            vHostSheet.Cell(i + 1, 3).Value = "Cluster1";
            vHostSheet.Cell(i + 1, 4).Value = "Intel Xeon Gold 6248R";
            vHostSheet.Cell(i + 1, 5).Value = 3000; // 3.0 GHz
            vHostSheet.Cell(i + 1, 6).Value = 2;    // 2 CPUs
            vHostSheet.Cell(i + 1, 7).Value = 12;   // 12 cores per CPU
            vHostSheet.Cell(i + 1, 8).Value = 24;   // 24 total cores
            vHostSheet.Cell(i + 1, 9).Value = 35 + (i * 10); // 35-55% CPU usage
            vHostSheet.Cell(i + 1, 10).Value = 192 * 1024; // 192 GB
            vHostSheet.Cell(i + 1, 11).Value = 40 + (i * 5); // 40-50% memory usage
        }
    }

    /// <summary>
    /// Creates the vPartition sheet with sample data.
    /// </summary>
    /// <param name="workbook">The workbook to add the sheet to.</param>
    /// <param name="numVMs">Number of VM entries to generate.</param>
    private void CreateVPartitionSheet(XLWorkbook workbook, int numVMs)
    {
        var vPartitionSheet = workbook.AddWorksheet("vPartition");

        // Add headers
        vPartitionSheet.Cell(1, 1).Value = "VM";
        vPartitionSheet.Cell(1, 2).Value = "Disk";
        vPartitionSheet.Cell(1, 3).Value = "Capacity MiB";
        vPartitionSheet.Cell(1, 4).Value = "Consumed MiB";

        // Add data rows - assume each VM has 2 disks
        int row = 2;
        for (int i = 1; i <= numVMs; i++)
        {
            // System disk
            vPartitionSheet.Cell(row, 1).Value = $"TestVM{i:D2}";
            vPartitionSheet.Cell(row, 2).Value = "Hard disk 1";
            vPartitionSheet.Cell(row, 3).Value = 51200; // 50 GB
            vPartitionSheet.Cell(row, 4).Value = 25600; // 25 GB used
            row++;

            // Data disk
            vPartitionSheet.Cell(row, 1).Value = $"TestVM{i:D2}";
            vPartitionSheet.Cell(row, 2).Value = "Hard disk 2";
            vPartitionSheet.Cell(row, 3).Value = 102400; // 100 GB
            vPartitionSheet.Cell(row, 4).Value = 51200; // 50 GB used
            row++;
        }
    }

    /// <summary>
    /// Creates the vMemory sheet with sample data.
    /// </summary>
    /// <param name="workbook">The workbook to add the sheet to.</param>
    /// <param name="numVMs">Number of VM entries to generate.</param>
    private void CreateVMemorySheet(XLWorkbook workbook, int numVMs)
    {
        var vMemorySheet = workbook.AddWorksheet("vMemory");

        // Add headers
        vMemorySheet.Cell(1, 1).Value = "VM";
        vMemorySheet.Cell(1, 2).Value = "Size MiB";
        vMemorySheet.Cell(1, 3).Value = "Reservation";

        // Add data rows
        for (int i = 1; i <= numVMs; i++)
        {
            vMemorySheet.Cell(i + 1, 1).Value = $"TestVM{i:D2}";
            vMemorySheet.Cell(i + 1, 2).Value = 4096 * i; // Memory size
            vMemorySheet.Cell(i + 1, 3).Value = i % 2 == 0 ? 2048 : 0; // Some VMs have reservations
        }
    }
}
