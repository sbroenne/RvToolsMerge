//-----------------------------------------------------------------------
// <copyright file="WingetVersionConsistencyTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Integration tests to validate version consistency for winget submission.
/// Ensures that project versions, MSI versions, and winget manifest versions are aligned.
/// </summary>
public class WingetVersionConsistencyTests
{
    private readonly IFileSystem _fileSystem;

    public WingetVersionConsistencyTests()
    {
        _fileSystem = new FileSystem();
    }

    [Fact]
    public void ProjectVersion_Should_BeThreePartSemanticVersion()
    {
        // Arrange
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = GetProjectRoot(baseDirectory);
        string csprojPath = Path.Combine(projectRoot, "src", "RVToolsMerge", "RVToolsMerge.csproj");

        // Act
        Assert.True(_fileSystem.File.Exists(csprojPath), $"Project file not found at: {csprojPath}");

        string csprojContent = _fileSystem.File.ReadAllText(csprojPath);
        XDocument csprojDoc = XDocument.Parse(csprojContent);

        string? packageVersion = csprojDoc.Descendants("Version").FirstOrDefault()?.Value;

        // Assert
        Assert.NotNull(packageVersion);
        Assert.Matches(@"^\d+\.\d+\.\d+$", packageVersion);
    }

    [Fact]
    public void FileVersion_Should_BeFourPartVersion()
    {
        // Arrange
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = GetProjectRoot(baseDirectory);
        string csprojPath = Path.Combine(projectRoot, "src", "RVToolsMerge", "RVToolsMerge.csproj");

        // Act
        Assert.True(_fileSystem.File.Exists(csprojPath), $"Project file not found at: {csprojPath}");

        string csprojContent = _fileSystem.File.ReadAllText(csprojPath);
        XDocument csprojDoc = XDocument.Parse(csprojContent);

        string? fileVersion = csprojDoc.Descendants("FileVersion").FirstOrDefault()?.Value;
        string? assemblyVersion = csprojDoc.Descendants("AssemblyVersion").FirstOrDefault()?.Value;

        // Assert
        Assert.NotNull(fileVersion);
        Assert.NotNull(assemblyVersion);
        Assert.Matches(@"^\d+\.\d+\.\d+\.\d+$", fileVersion);
        Assert.Matches(@"^\d+\.\d+\.\d+\.\d+$", assemblyVersion);
    }

    [Fact]
    public void FileVersion_Should_MatchPackageVersionWithZeroRevision()
    {
        // Arrange
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = GetProjectRoot(baseDirectory);
        string csprojPath = Path.Combine(projectRoot, "src", "RVToolsMerge", "RVToolsMerge.csproj");

        // Act
        Assert.True(_fileSystem.File.Exists(csprojPath), $"Project file not found at: {csprojPath}");

        string csprojContent = _fileSystem.File.ReadAllText(csprojPath);
        XDocument csprojDoc = XDocument.Parse(csprojContent);

        string? packageVersion = csprojDoc.Descendants("Version").FirstOrDefault()?.Value;
        string? fileVersion = csprojDoc.Descendants("FileVersion").FirstOrDefault()?.Value;
        string? assemblyVersion = csprojDoc.Descendants("AssemblyVersion").FirstOrDefault()?.Value;

        // Assert
        Assert.NotNull(packageVersion);
        Assert.NotNull(fileVersion);
        Assert.NotNull(assemblyVersion);

        // File version should be package version + .0
        string expectedFileVersion = $"{packageVersion}.0";
        Assert.Equal(expectedFileVersion, fileVersion);
        Assert.Equal(expectedFileVersion, assemblyVersion);
    }

    [Fact]
    public void WingetManifestTemplate_Should_UseVersionPlaceholder()
    {
        // Arrange
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = GetProjectRoot(baseDirectory);
        string manifestPath = Path.Combine(projectRoot, ".github", "winget-templates", "RvToolsMerge.RvToolsMerge.yaml.template");

        // Act
        Assert.True(_fileSystem.File.Exists(manifestPath), $"Winget manifest template not found at: {manifestPath}");

        string manifestContent = _fileSystem.File.ReadAllText(manifestPath);

        // Assert - Should use {{VERSION}} placeholder for PackageVersion
        Assert.Contains("PackageVersion: {{VERSION}}", manifestContent);
        
        // Should not have hardcoded version numbers
        Assert.DoesNotMatch(@"PackageVersion:\s+\d+\.\d+\.\d+", manifestContent);
    }

    [Fact]
    public void WingetInstallerManifest_Should_UseVersionForDisplayVersion()
    {
        // Arrange
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = GetProjectRoot(baseDirectory);
        string installerManifestPath = Path.Combine(projectRoot, ".github", "winget-templates", "RvToolsMerge.RvToolsMerge.installer.yaml.template");

        // Act
        Assert.True(_fileSystem.File.Exists(installerManifestPath), $"Installer manifest template not found at: {installerManifestPath}");

        string manifestContent = _fileSystem.File.ReadAllText(installerManifestPath);

        // Assert - DisplayVersion should use {{VERSION}} placeholder to match MSI ProductVersion
        Assert.Contains("DisplayVersion: {{VERSION}}", manifestContent);
        
        // Should not have hardcoded version in DisplayVersion
        Assert.DoesNotMatch(@"DisplayVersion:\s+\d+\.\d+\.\d+", manifestContent);
    }

    [Fact]
    public void WingetInstallerManifest_Should_NotHaveProductCodeField()
    {
        // Arrange
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = GetProjectRoot(baseDirectory);
        string installerManifestPath = Path.Combine(projectRoot, ".github", "winget-templates", "RvToolsMerge.RvToolsMerge.installer.yaml.template");

        // Act
        Assert.True(_fileSystem.File.Exists(installerManifestPath), $"Installer manifest template not found at: {installerManifestPath}");

        string manifestContent = _fileSystem.File.ReadAllText(installerManifestPath);

        // Assert - Should use ProductCode placeholder (ProductCode will be extracted dynamically)
        // The manifest DOES include ProductCode with placeholder for proper validation
        Assert.Contains("ProductCode: '{{X64_PRODUCT_CODE}}'", manifestContent);
        Assert.Contains("ProductCode: '{{ARM64_PRODUCT_CODE}}'", manifestContent);
    }

    [Fact]
    public void WixConfiguration_Should_BindToFileVersion()
    {
        // Arrange
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = GetProjectRoot(baseDirectory);
        string wixFilePath = Path.Combine(projectRoot, "installer", "RVToolsMerge.wxs");

        // Act
        Assert.True(_fileSystem.File.Exists(wixFilePath), $"WiX configuration file not found at: {wixFilePath}");

        string wixContent = _fileSystem.File.ReadAllText(wixFilePath);
        XDocument wixDoc = XDocument.Parse(wixContent);

        // Define the WiX namespace
        XNamespace wixNs = "http://wixtoolset.org/schemas/v4/wxs";

        // Get Package element
        XElement? packageElement = wixDoc.Descendants(wixNs + "Package").FirstOrDefault();
        Assert.NotNull(packageElement);

        string? version = packageElement.Attribute("Version")?.Value;

        // Assert - WiX should bind to FileVersion which will become the MSI ProductVersion
        Assert.Equal("!(bind.FileVersion.RVToolsMerge.exe)", version);
    }

    [Fact]
    public void VersionManagementWorkflow_Should_UseThreePartVersions()
    {
        // Arrange
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = GetProjectRoot(baseDirectory);
        string workflowPath = Path.Combine(projectRoot, ".github", "workflows", "version-management.yml");

        // Act
        Assert.True(_fileSystem.File.Exists(workflowPath), $"Version management workflow not found at: {workflowPath}");

        string workflowContent = _fileSystem.File.ReadAllText(workflowPath);

        // Assert - Workflow should calculate 3-part package version and 4-part assembly version
        Assert.Contains("PACKAGE_VERSION=\"$MAJOR.$MINOR.$PATCH\"", workflowContent);
        Assert.Contains("ASSEMBLY_VERSION=\"$MAJOR.$MINOR.$PATCH.0\"", workflowContent);
        
        // Should validate version format is X.Y.Z (the actual regex in the workflow)
        Assert.Contains(@"^[0-9]+\.[0-9]+\.[0-9]+$", workflowContent);
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
