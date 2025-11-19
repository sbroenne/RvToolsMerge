//-----------------------------------------------------------------------
// <copyright file="InstallerConfigurationTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using System.IO.Abstractions;
using System.Xml.Linq;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Integration tests for installer configuration validation.
/// </summary>
public class InstallerConfigurationTests
{
    private readonly IFileSystem _fileSystem;

    public InstallerConfigurationTests()
    {
        _fileSystem = new FileSystem();
    }

    [Fact]
    public void WixConfiguration_Should_SupportUpgrades()
    {
        // Arrange
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = GetProjectRoot(baseDirectory);
        string wixFilePath = Path.Combine(projectRoot, "installer", "RVToolsMerge.wxs");

        // Act & Assert
        Assert.True(_fileSystem.File.Exists(wixFilePath), $"WiX configuration file not found at: {wixFilePath}");

        string wixContent = _fileSystem.File.ReadAllText(wixFilePath);
        XDocument wixDoc = XDocument.Parse(wixContent);

        // Define the WiX namespace
        XNamespace wixNs = "http://wixtoolset.org/schemas/v4/wxs";

        // Validate Package element exists
        XElement? packageElement = wixDoc.Descendants(wixNs + "Package").FirstOrDefault();
        Assert.NotNull(packageElement);

        // Validate ProductCode is set to auto-generate (*)
        string? productCode = packageElement.Attribute("ProductCode")?.Value;
        Assert.Equal("*", productCode);

        // Validate UpgradeCode is set to the expected stable value
        string? upgradeCode = packageElement.Attribute("UpgradeCode")?.Value;
        Assert.Equal("A7B8C9D0-E1F2-4A5B-8C9D-0E1F2A5B8C9D", upgradeCode);

        // Validate MajorUpgrade element exists for upgrade support
        XElement? majorUpgradeElement = wixDoc.Descendants(wixNs + "MajorUpgrade").FirstOrDefault();
        Assert.NotNull(majorUpgradeElement);

        // Validate downgrade error message is configured
        string? downgradeMessage = majorUpgradeElement.Attribute("DowngradeErrorMessage")?.Value;
        Assert.NotNull(downgradeMessage);
        Assert.Contains("newer version", downgradeMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WixConfiguration_Should_UseAutomaticVersionBinding()
    {
        // Arrange
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = GetProjectRoot(baseDirectory);
        string wixFilePath = Path.Combine(projectRoot, "installer", "RVToolsMerge.wxs");

        // Act & Assert
        Assert.True(_fileSystem.File.Exists(wixFilePath), $"WiX configuration file not found at: {wixFilePath}");

        string wixContent = _fileSystem.File.ReadAllText(wixFilePath);
        XDocument wixDoc = XDocument.Parse(wixContent);

        // Define the WiX namespace
        XNamespace wixNs = "http://wixtoolset.org/schemas/v4/wxs";

        // Validate Package element uses automatic version binding
        XElement? packageElement = wixDoc.Descendants(wixNs + "Package").FirstOrDefault();
        Assert.NotNull(packageElement);

        string? version = packageElement.Attribute("Version")?.Value;
        Assert.Equal("!(bind.FileVersion.RVToolsMerge.exe)", version);
    }

    [Fact]
    public void WixConfiguration_Should_HaveStableManufacturerAndName()
    {
        // Arrange
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = GetProjectRoot(baseDirectory);
        string wixFilePath = Path.Combine(projectRoot, "installer", "RVToolsMerge.wxs");

        // Act & Assert
        Assert.True(_fileSystem.File.Exists(wixFilePath), $"WiX configuration file not found at: {wixFilePath}");

        string wixContent = _fileSystem.File.ReadAllText(wixFilePath);
        XDocument wixDoc = XDocument.Parse(wixContent);

        // Define the WiX namespace
        XNamespace wixNs = "http://wixtoolset.org/schemas/v4/wxs";

        // Validate Package element has required attributes
        XElement? packageElement = wixDoc.Descendants(wixNs + "Package").FirstOrDefault();
        Assert.NotNull(packageElement);

        string? name = packageElement.Attribute("Name")?.Value;
        Assert.Equal("RVToolsMerge", name);

        string? manufacturer = packageElement.Attribute("Manufacturer")?.Value;
        Assert.Equal("Stefan Broenner", manufacturer);

        string? language = packageElement.Attribute("Language")?.Value;
        Assert.Equal("1033", language);
    }

    /// <summary>
    /// Finds the project root directory by looking for the solution file.
    /// </summary>
    /// <param name="startDirectory">Directory to start searching from.</param>
    /// <returns>Path to the project root directory.</returns>
    private static string GetProjectRoot(string startDirectory)
    {
        DirectoryInfo? current = new(startDirectory);

        while (current != null)
        {
            if (current.GetFiles("*.sln").Length > 0)
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        throw new InvalidOperationException("Could not find project root directory containing .sln file");
    }
}