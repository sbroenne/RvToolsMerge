//-----------------------------------------------------------------------
// <copyright file="DynamicSheetDiscoveryTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using ClosedXML.Excel;
using RVToolsMerge.Models;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

[Collection("SpectreConsole")]
/// <summary>
/// Tests for dynamic sheet discovery functionality.
/// </summary>
public class DynamicSheetDiscoveryTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that unknown sheets are discovered and processed when ProcessAllSheets is enabled.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithUnknownSheets_DiscoversAndProcessesAllSheets()
    {
        // Arrange
        var testFile = TestDataGenerator.CreateFileWithUnknownSheets("unknown_sheets_test.xlsx", numVMs: 3);
        string outputPath = GetOutputFilePath("merged_unknown_sheets.xlsx");

        var options = CreateDefaultMergeOptions();
        options.ProcessAllSheets = true; // Enable dynamic discovery
        options.IgnoreMissingOptionalSheets = true; // Allow missing optional sheets

        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync([testFile], outputPath, options, validationIssues);

        // Assert
        Assert.True(FileSystem.File.Exists(outputPath), "Output file should exist");

        using var workbook = new XLWorkbook(outputPath);

        // Should have all discovered sheets: vInfo, vHost, vCPU, vDisk
        Assert.Equal(4, workbook.Worksheets.Count);
        Assert.True(workbook.Worksheets.Any(ws => ws.Name == "vInfo"), "Should have vInfo sheet");
        Assert.True(workbook.Worksheets.Any(ws => ws.Name == "vHost"), "Should have vHost sheet");
        Assert.True(workbook.Worksheets.Any(ws => ws.Name == "vCPU"), "Should have vCPU sheet (unknown)");
        Assert.True(workbook.Worksheets.Any(ws => ws.Name == "vDisk"), "Should have vDisk sheet (unknown)");

        // Verify vCPU sheet has data
        var vCPUSheet = workbook.Worksheet("vCPU");
        Assert.NotNull(vCPUSheet);
        Assert.Equal("VM", vCPUSheet.Cell(1, 1).Value.ToString());
        Assert.Equal("VM UUID", vCPUSheet.Cell(1, 2).Value.ToString());
        Assert.Equal("# CPU", vCPUSheet.Cell(1, 4).Value.ToString());

        // Should have 3 data rows (header + 3 VMs)
        int vCPURowCount = vCPUSheet.RowsUsed().Count();
        Assert.Equal(4, vCPURowCount);

        // Verify vDisk sheet has data
        var vDiskSheet = workbook.Worksheet("vDisk");
        Assert.NotNull(vDiskSheet);
        Assert.Equal("VM", vDiskSheet.Cell(1, 1).Value.ToString());
        Assert.Equal("Datastore", vDiskSheet.Cell(1, 2).Value.ToString());

        // Should have 3 data rows (header + 3 VMs)
        int vDiskRowCount = vDiskSheet.RowsUsed().Count();
        Assert.Equal(4, vDiskRowCount);
    }

    /// <summary>
    /// Tests that anonymization and ProcessAllSheets are mutually exclusive.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithAnonymizationAndAllSheets_ThrowsException()
    {
        // Arrange
        var testFile = TestDataGenerator.CreateFileWithUnknownSheets("unknown_sheets_anon_test.xlsx", numVMs: 3);
        string outputPath = GetOutputFilePath("merged_unknown_sheets_anonymized.xlsx");

        var options = CreateDefaultMergeOptions();
        options.ProcessAllSheets = true; // Enable dynamic discovery
        options.AnonymizeData = true; // Enable anonymization (should conflict)
        options.IgnoreMissingOptionalSheets = true; // Allow missing optional sheets

        var validationIssues = new List<ValidationIssue>();

        // Act & Assert
        // This should throw because anonymization and all-sheets are mutually exclusive
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await MergeService.MergeFilesAsync([testFile], outputPath, options, validationIssues));
    }

    /// <summary>
    /// Tests that merging multiple files with different unknown sheets combines all unique sheets.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithDifferentUnknownSheets_CombinesAllUniqueSheets()
    {
        // Arrange
        // Create first file with vCPU and vDisk sheets
        var testFile1 = TestDataGenerator.CreateFileWithUnknownSheets("file1_with_unknown.xlsx", numVMs: 2);

        // Create second file with only standard sheets plus one unknown sheet
        var testFile2Path = System.IO.Path.Combine(TestInputDirectory, "file2_with_unknown.xlsx");
        using (var workbook2 = new XLWorkbook())
        {
            // Add standard vInfo sheet with required columns
            var vInfoSheet = workbook2.AddWorksheet("vInfo");
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

            vInfoSheet.Cell(2, 1).Value = "TestVM99";
            vInfoSheet.Cell(2, 2).Value = "42008ee5-71f9-48d7-8e02-7e371f5a8b99";
            vInfoSheet.Cell(2, 3).Value = "poweredOn";
            vInfoSheet.Cell(2, 4).Value = false;
            vInfoSheet.Cell(2, 5).Value = 2;
            vInfoSheet.Cell(2, 6).Value = 4096;
            vInfoSheet.Cell(2, 7).Value = 2048;
            vInfoSheet.Cell(2, 8).Value = "Windows Server 2019";
            vInfoSheet.Cell(2, 9).Value = "Datacenter1";
            vInfoSheet.Cell(2, 10).Value = "Cluster1";
            vInfoSheet.Cell(2, 11).Value = "Host1";
            vInfoSheet.Cell(2, 12).Value = false;
            vInfoSheet.Cell(2, 13).Value = DateTime.Now.AddDays(-10).ToShortDateString();
            vInfoSheet.Cell(2, 14).Value = 1;
            vInfoSheet.Cell(2, 15).Value = 1;
            vInfoSheet.Cell(2, 16).Value = 8192;

            // Add a different unknown sheet: vNetwork
            var vNetworkSheet2 = workbook2.AddWorksheet("vNetwork");
            vNetworkSheet2.Cell(1, 1).Value = "VM";
            vNetworkSheet2.Cell(1, 2).Value = "Network";
            vNetworkSheet2.Cell(1, 3).Value = "Port Group";

            vNetworkSheet2.Cell(2, 1).Value = "TestVM99";
            vNetworkSheet2.Cell(2, 2).Value = "Network1";
            vNetworkSheet2.Cell(2, 3).Value = "VM Network";

            workbook2.SaveAs(testFile2Path);
        }

        string outputPath = GetOutputFilePath("merged_multiple_unknown_sheets.xlsx");

        var options = CreateDefaultMergeOptions();
        options.ProcessAllSheets = true; // Enable dynamic discovery
        options.IgnoreMissingOptionalSheets = true; // Allow missing optional sheets

        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync([testFile1, testFile2Path], outputPath, options, validationIssues);

        // Assert
        Assert.True(FileSystem.File.Exists(outputPath), "Output file should exist");

        using var workbook = new XLWorkbook(outputPath);

        // Should have all unique discovered sheets: vInfo, vHost, vCPU, vDisk, vNetwork
        Assert.True(workbook.Worksheets.Count >= 5, $"Should have at least 5 sheets, found {workbook.Worksheets.Count}");
        Assert.True(workbook.Worksheets.Any(ws => ws.Name == "vInfo"), "Should have vInfo sheet");
        Assert.True(workbook.Worksheets.Any(ws => ws.Name == "vHost"), "Should have vHost sheet");
        Assert.True(workbook.Worksheets.Any(ws => ws.Name == "vCPU"), "Should have vCPU sheet");
        Assert.True(workbook.Worksheets.Any(ws => ws.Name == "vDisk"), "Should have vDisk sheet");
        Assert.True(workbook.Worksheets.Any(ws => ws.Name == "vNetwork"), "Should have vNetwork sheet");

        // Verify vNetwork sheet exists and has data
        var vNetworkSheet = workbook.Worksheet("vNetwork");
        Assert.NotNull(vNetworkSheet);
        Assert.Equal("VM", vNetworkSheet.Cell(1, 1).Value.ToString());
        Assert.Equal("Network", vNetworkSheet.Cell(1, 2).Value.ToString());
    }

    /// <summary>
    /// Tests that standard mode (without ProcessAllSheets) only processes the 4 core sheets.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithoutProcessAllSheets_OnlyProcessesKnownSheets()
    {
        // Arrange
        // Create a file with all 4 known sheets PLUS unknown sheets
        var testFilePath = System.IO.Path.Combine(TestInputDirectory, "standard_mode_full_test.xlsx");
        using (var workbookBuilder = new XLWorkbook())
        {
            // Create all 4 known sheets with proper structure
            TestDataGenerator.CreateValidRVToolsFile("temp.xlsx", 2, 1, includeAllSheets: true);
            using var tempWorkbook = new XLWorkbook(System.IO.Path.Combine(TestInputDirectory, "temp.xlsx"));

            foreach (var knownSheet in new[] { "vInfo", "vHost", "vPartition", "vMemory" })
            {
                if (tempWorkbook.Worksheets.Contains(knownSheet))
                {
                    tempWorkbook.Worksheet(knownSheet).CopyTo(workbookBuilder, knownSheet);
                }
            }

            // Add unknown sheets
            var vCPUSheet = workbookBuilder.AddWorksheet("vCPU");
            vCPUSheet.Cell(1, 1).Value = "VM";
            vCPUSheet.Cell(1, 2).Value = "# CPU";
            vCPUSheet.Cell(2, 1).Value = "TestVM01";
            vCPUSheet.Cell(2, 2).Value = 2;

            workbookBuilder.SaveAs(testFilePath);
        }

        string outputPath = GetOutputFilePath("merged_standard_mode.xlsx");

        var options = CreateDefaultMergeOptions();
        options.ProcessAllSheets = false; // Standard mode: only process 4 core sheets

        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync([testFilePath], outputPath, options, validationIssues);

        // Assert
        Assert.True(FileSystem.File.Exists(outputPath), "Output file should exist");

        using var workbook = new XLWorkbook(outputPath);

        // In standard mode, should only have the 4 core sheets (vInfo, vHost, vPartition, vMemory)
        Assert.Equal(4, workbook.Worksheets.Count);
        Assert.True(workbook.Worksheets.Any(ws => ws.Name == "vInfo"), "Should have vInfo sheet");
        Assert.True(workbook.Worksheets.Any(ws => ws.Name == "vHost"), "Should have vHost sheet");
        Assert.True(workbook.Worksheets.Any(ws => ws.Name == "vPartition"), "Should have vPartition sheet");
        Assert.True(workbook.Worksheets.Any(ws => ws.Name == "vMemory"), "Should have vMemory sheet");

        // Unknown sheets should NOT be included in standard mode
        Assert.False(workbook.Worksheets.Any(ws => ws.Name == "vCPU"), "Should NOT have vCPU sheet in standard mode");
    }
}
