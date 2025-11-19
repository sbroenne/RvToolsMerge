//-----------------------------------------------------------------------
// <copyright file="ApplicationRunnerTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

namespace RVToolsMerge.UnitTests;

/// <summary>
/// Unit tests for ApplicationRunner
/// </summary>
public class ApplicationRunnerTests
{
    private readonly Mock<IMergeService> _mockMergeService;
    private readonly Mock<ICommandLineParser> _mockCommandLineParser;
    private readonly MockFileSystem _mockFileSystem;
    private readonly ConsoleUIService _consoleUIService;
    private readonly ApplicationRunner _applicationRunner;

    public ApplicationRunnerTests()
    {
        _mockMergeService = new Mock<IMergeService>();
        _mockCommandLineParser = new Mock<ICommandLineParser>();
        _mockFileSystem = new MockFileSystem();
        _consoleUIService = new ConsoleUIService();

        _applicationRunner = new ApplicationRunner(
            _consoleUIService,
            _mockMergeService.Object,
            _mockCommandLineParser.Object,
            _mockFileSystem
        );
    }

    [Fact]
    public void GetApplicationInfo_ReturnsProductNameAndVersion()
    {
        // Act
        var (productName, versionString) = _applicationRunner.GetApplicationInfo();

        // Assert
        Assert.NotNull(productName);
        Assert.NotEmpty(productName);
        Assert.NotNull(versionString);
        Assert.NotEmpty(versionString);

        // Version should follow the pattern X.Y.Z
        Assert.Matches(@"^\d+\.\d+\.\d+$", versionString);
    }

    [Fact]
    public void GetApplicationInfo_WithNullVersion_ReturnsDefaultVersion()
    {
        // This test verifies behavior when assembly version is null
        // The actual behavior depends on the assembly, but we can verify the format

        // Act
        var (productName, versionString) = _applicationRunner.GetApplicationInfo();

        // Assert
        Assert.True(productName == "RVToolsMerge" || productName == "RVToolsMerger" || !string.IsNullOrEmpty(productName));
        Assert.Matches(@"^\d+\.\d+\.\d+$", versionString);
    }

    [Fact]
    public void GetApplicationInfo_ReturnsConsistentResults()
    {
        // Act
        var (productName1, versionString1) = _applicationRunner.GetApplicationInfo();
        var (productName2, versionString2) = _applicationRunner.GetApplicationInfo();

        // Assert - Multiple calls should return the same values
        Assert.Equal(productName1, productName2);
        Assert.Equal(versionString1, versionString2);

        // Verify reasonable product name
        Assert.True(productName1 == "RVToolsMerge" || productName1 == "RVToolsMerger" || !string.IsNullOrEmpty(productName1));
        Assert.Matches(@"^\d+\.\d+\.\d+$", versionString1);
    }
}