using System;
using System.IO;
using ClosedXML.Excel;

// Quick test to examine the actual data being generated
using var workbook = new XLWorkbook();

// Create vInfo sheet exactly as the test does
var vInfoSheet = workbook.AddWorksheet("vInfo");

// Add headers - using standard names as defined in SheetConfiguration.MandatoryColumns
vInfoSheet.Cell(1, 1).Value = "VM";
vInfoSheet.Cell(1, 2).Value = "VM UUID";
vInfoSheet.Cell(1, 3).Value = "Powerstate";
vInfoSheet.Cell(1, 4).Value = "Template";
vInfoSheet.Cell(1, 5).Value = "CPUs";
vInfoSheet.Cell(1, 6).Value = "Memory";
vInfoSheet.Cell(1, 7).Value = "In Use MiB";
vInfoSheet.Cell(1, 8).Value = "OS according to the configuration file";
vInfoSheet.Cell(1, 9).Value = "Datacenter";
vInfoSheet.Cell(1, 10).Value = "Cluster";
vInfoSheet.Cell(1, 11).Value = "Host";
vInfoSheet.Cell(1, 12).Value = "SRM Placeholder";
vInfoSheet.Cell(1, 13).Value = "Creation date";
vInfoSheet.Cell(1, 14).Value = "NICs";
vInfoSheet.Cell(1, 15).Value = "Disks";
vInfoSheet.Cell(1, 16).Value = "Provisioned MiB";

// Add data rows for first 10 VMs (as in the test)
int numVMs = 10;
for (int i = 1; i <= numVMs; i++)
{
    vInfoSheet.Cell(i + 1, 1).Value = $"VM{i}";
    vInfoSheet.Cell(i + 1, 2).Value = $"42008ee5-71f9-48d7-8e02-7e371f5a8b{i:D2}"; // UUID value
    vInfoSheet.Cell(i + 1, 3).Value = i % 2 == 0 ? "poweredOn" : "poweredOff";
    vInfoSheet.Cell(i + 1, 4).Value = "FALSE";
    vInfoSheet.Cell(i + 1, 5).Value = 2 + (i % 3); // 2-4 CPUs
    vInfoSheet.Cell(i + 1, 6).Value = 4096 * i;    // Memory in MB
    vInfoSheet.Cell(i + 1, 7).Value = 2048 * i;    // In Use MiB
    vInfoSheet.Cell(i + 1, 8).Value = $"Windows Server 201{i % 2 + 8}";
    vInfoSheet.Cell(i + 1, 9).Value = "Datacenter1";
    vInfoSheet.Cell(i + 1, 10).Value = "Cluster1";
    vInfoSheet.Cell(i + 1, 11).Value = $"Host{i}"; // Host
    vInfoSheet.Cell(i + 1, 12).Value = "FALSE"; // SRM Placeholder
    vInfoSheet.Cell(i + 1, 13).Value = DateTime.Now.AddDays(-i * 10).ToShortDateString(); // Creation Date
    vInfoSheet.Cell(i + 1, 14).Value = i % 3 + 1; // 1-3 NICs
    vInfoSheet.Cell(i + 1, 15).Value = i % 2 + 1; // 1-2 Disks
    vInfoSheet.Cell(i + 1, 16).Value = 8192 * i; // Provisioned MiB
}

string filePath = "/tmp/debug_vinfo.xlsx";
workbook.SaveAs(filePath);

Console.WriteLine("Generated vInfo data:");
Console.WriteLine("Row | VM   | Host");
Console.WriteLine("----|------|------");
for (int i = 2; i <= 4; i++) // First 3 VMs (rows 2-4)
{
    var vm = vInfoSheet.Cell(i, 1).Value.ToString();
    var host = vInfoSheet.Cell(i, 11).Value.ToString();
    Console.WriteLine($" {i}  | {vm}  | {host}");
}

// Now create vHost sheet
var vHostSheet = workbook.AddWorksheet("vHost");

// Add headers - aligned with SheetConfiguration.MandatoryColumns for vHost
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

// Add data rows (numHosts = 3)
int numHosts = 3;
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

workbook.Save();

Console.WriteLine("\nGenerated vHost data:");
Console.WriteLine("Row | Host");
Console.WriteLine("----|------");
for (int i = 2; i <= 4; i++) // All 3 hosts (rows 2-4)
{
    var host = vHostSheet.Cell(i, 1).Value.ToString();
    Console.WriteLine($" {i}  | {host}");
}

Console.WriteLine("\nFiltering logic test:");
var includedHosts = new System.Collections.Generic.HashSet<string>();

// Collect hosts from first 3 vInfo rows
for (int i = 2; i <= 4; i++)
{
    var hostName = vInfoSheet.Cell(i, 11).Value.ToString();
    includedHosts.Add(hostName);
    Console.WriteLine($"Adding host: {hostName}");
}

Console.WriteLine($"\nIncluded hosts: {string.Join(", ", includedHosts)}");

// Test filtering
int keepCount = 0;
for (int i = 2; i <= 4; i++)
{
    var hostName = vHostSheet.Cell(i, 1).Value.ToString();
    bool shouldKeep = includedHosts.Contains(hostName);
    Console.WriteLine($"Host {hostName}: {(shouldKeep ? "KEEP" : "FILTER")}");
    if (shouldKeep) keepCount++;
}

Console.WriteLine($"\nTotal kept: {keepCount} hosts");

File.Delete(filePath);