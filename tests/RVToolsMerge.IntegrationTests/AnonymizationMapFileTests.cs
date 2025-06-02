//-----------------------------------------------------------------------
// <copyright file="AnonymizationMapFileTests.cs" company="Stefan Broenner">
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
/// Tests for the anonymization map file functionality.
/// </summary>
public class AnonymizationMapFileTests : IntegrationTestBase
{
    /// <summary>
    /// Tests that an anonymization map file is created when anonymization is enabled.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithAnonymization_CreatesAnonymizationMapFile()
    {
        // Arrange
        // Create test files with some data that will be anonymized
        var file1 = TestDataGenerator.CreateValidRVToolsFile("anon_file1.xlsx", numVMs: 3, numHosts: 2);
        var file2 = TestDataGenerator.CreateValidRVToolsFile("anon_file2.xlsx", numVMs: 2, numHosts: 1);

        string[] filesToMerge = [file1, file2];
        string outputPath = GetOutputFilePath("anonymized_output.xlsx");
        string expectedMapFilePath = GetOutputFilePath("anonymized_output_AnonymizationMapping.xlsx");

        // Create options with anonymization enabled
        var options = CreateDefaultMergeOptions();
        options.AnonymizeData = true;

        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues);

        // Assert
        // Verify both the output file and map file exist
        Assert.True(FileSystem.File.Exists(outputPath), "Output file should exist");
        Assert.True(FileSystem.File.Exists(expectedMapFilePath), "Anonymization map file should exist");

        // Verify the anonymization map file has expected structure by reading it
        using var mapWorkbook = new XLWorkbook(expectedMapFilePath);
        
        // Verify that anonymization map has sheets - the actual structure depends on implementation
        Assert.True(mapWorkbook.Worksheets.Count > 0, "Anonymization map should have at least one worksheet");

        // Verify the main output file exists and has data
        using var outputWorkbook = new XLWorkbook(outputPath);
        Assert.True(outputWorkbook.TryGetWorksheet("vInfo", out var vInfoSheet));
        var lastRow = vInfoSheet.LastRowUsed()?.RowNumber() ?? 1;
        Assert.True(lastRow > 1, "Output file should have data rows");
    }

    /// <summary>
    /// Tests that an anonymization map file is not created when anonymization is disabled.
    /// </summary>
    [Fact]
    public async Task MergeFiles_WithoutAnonymization_DoesNotCreateAnonymizationMapFile()
    {
        // Arrange
        var file1 = TestDataGenerator.CreateValidRVToolsFile("noanon_file1.xlsx", numVMs: 2);

        string[] filesToMerge = [file1];
        string outputPath = GetOutputFilePath("non_anonymized_output.xlsx");
        string mapFilePath = GetOutputFilePath("non_anonymized_output_AnonymizationMapping.xlsx");

        // Create options with anonymization disabled (default)
        var options = CreateDefaultMergeOptions();
        options.AnonymizeData = false;

        var validationIssues = new List<ValidationIssue>();

        // Act
        await MergeService.MergeFilesAsync(filesToMerge, outputPath, options, validationIssues);

        // Assert
        Assert.True(FileSystem.File.Exists(outputPath), "Output file should exist");
        Assert.False(FileSystem.File.Exists(mapFilePath), "Anonymization map file should not exist");
    }
}
