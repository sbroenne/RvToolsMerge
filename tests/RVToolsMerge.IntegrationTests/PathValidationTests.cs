//-----------------------------------------------------------------------
// <copyright file="PathValidationTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using System.IO.Abstractions.TestingHelpers;
using RVToolsMerge.Models;
using RVToolsMerge.Services;
using Xunit;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Tests for path validation security measures.
/// </summary>
[Collection("SpectreConsole")]
public class PathValidationTests : IntegrationTestBase
{
    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\windows\\system32\\config\\sam")]
    [InlineData("..")]
    [InlineData("../")]
    [InlineData("..\\")]
    [InlineData("valid/path/../../../etc")]
    [InlineData("C:\\path\\..\\..\\windows")]
    public void CommandLineParser_RejectsPathTraversalAttacks(string maliciousPath)
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var parser = new CommandLineParser(fileSystem);
        var options = new MergeOptions();
        var args = new[] { maliciousPath, "output.xlsx" };

        // Act
        bool helpRequested = parser.ParseArguments(args, options, out string? inputPath, out string? outputPath, out bool versionRequested);

        // Assert
        Assert.False(helpRequested);
        Assert.Null(inputPath); // Should be null because path is unsafe
    }

    [Theory]
    [InlineData("valid_file.xlsx")]
    [InlineData("C:\\Documents\\file.xlsx")]
    [InlineData("/home/user/documents/file.xlsx")]
    [InlineData("./local_file.xlsx")]
    [InlineData("subdir/file.xlsx")]
    public void CommandLineParser_AcceptsValidPaths(string validPath)
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var parser = new CommandLineParser(fileSystem);
        var options = new MergeOptions();
        var args = new[] { validPath };

        // Act
        bool helpRequested = parser.ParseArguments(args, options, out string? inputPath, out string? outputPath, out bool versionRequested);

        // Assert
        Assert.False(helpRequested);
        Assert.Equal(validPath, inputPath); // Should accept valid paths
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\windows\\system32\\config\\sam")]
    [InlineData("..")]
    public void CommandLineParser_RejectsPathTraversalInOutputPath(string maliciousPath)
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var parser = new CommandLineParser(fileSystem);
        var options = new MergeOptions();
        var args = new[] { "input.xlsx", maliciousPath };

        // Act
        bool helpRequested = parser.ParseArguments(args, options, out string? inputPath, out string? outputPath, out bool versionRequested);

        // Assert
        Assert.False(helpRequested);
        Assert.Null(outputPath); // Should be null because output path is unsafe
    }
}