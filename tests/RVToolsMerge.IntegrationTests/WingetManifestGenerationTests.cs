using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.Diagnostics;
using YamlDotNet.Serialization;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Integration tests for winget manifest generation functionality.
/// Tests the PowerShell script that generates winget manifests from templates.
/// </summary>
public class WingetManifestGenerationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _scriptsDirectory;
    private readonly string _templatesDirectory;

    public WingetManifestGenerationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "RVToolsMerge_WingetTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        
        // Get the project root directory
        var projectRoot = GetProjectRootDirectory();
        _scriptsDirectory = Path.Combine(projectRoot, ".github", "scripts");
        _templatesDirectory = Path.Combine(projectRoot, ".github", "winget-templates");
    }

    [Fact]
    public async Task GenerateWingetManifests_WithValidInputs_ShouldCreateValidManifests()
    {
        // Arrange
        var version = "1.2.3";
        var x64MsiPath = Path.Combine(_testDirectory, "test-x64.msi");
        var arm64MsiPath = Path.Combine(_testDirectory, "test-arm64.msi");
        var outputDir = Path.Combine(_testDirectory, "manifests");
        var releaseNotes = "Test release for integration testing";

        // Create test MSI files
        await File.WriteAllTextAsync(x64MsiPath, "Test MSI content for x64");
        await File.WriteAllTextAsync(arm64MsiPath, "Test MSI content for ARM64");

        // Act
        var result = await RunPowerShellScript(
            Path.Combine(_scriptsDirectory, "generate-winget-manifests.ps1"),
            $"-Version \"{version}\" -X64MsiPath \"{x64MsiPath}\" -Arm64MsiPath \"{arm64MsiPath}\" -OutputDir \"{outputDir}\" -ReleaseNotes \"{releaseNotes}\"");

        // Assert
        result.ExitCode.Should().Be(0, $"Script should succeed. Output: {result.Output}");
        // Check for success message - handle cross-platform output differences
        result.Output.Should().MatchRegex(@"All winget manifests generated successfully");

        // Verify all expected manifest files are created
        var expectedFiles = new[]
        {
            "RvToolsMerge.RvToolsMerge.yaml",
            "RvToolsMerge.RvToolsMerge.installer.yaml", 
            "RvToolsMerge.RvToolsMerge.locale.en-US.yaml"
        };

        foreach (var expectedFile in expectedFiles)
        {
            var filePath = Path.Combine(outputDir, expectedFile);
            File.Exists(filePath).Should().BeTrue($"Expected manifest file {expectedFile} should exist");
        }

        // Verify manifest content
        await ValidateManifestContent(outputDir, version, releaseNotes);
    }

    [Fact]
    public async Task GenerateWingetManifests_WithInvalidVersion_ShouldFail()
    {
        // Arrange
        var invalidVersion = "invalid.version.format";
        var x64MsiPath = Path.Combine(_testDirectory, "test-x64.msi");
        var arm64MsiPath = Path.Combine(_testDirectory, "test-arm64.msi");
        var outputDir = Path.Combine(_testDirectory, "manifests");

        // Create test MSI files
        await File.WriteAllTextAsync(x64MsiPath, "Test MSI content");
        await File.WriteAllTextAsync(arm64MsiPath, "Test MSI content");

        // Act
        var result = await RunPowerShellScript(
            Path.Combine(_scriptsDirectory, "generate-winget-manifests.ps1"),
            $"-Version \"{invalidVersion}\" -X64MsiPath \"{x64MsiPath}\" -Arm64MsiPath \"{arm64MsiPath}\" -OutputDir \"{outputDir}\"");

        // Assert
        result.ExitCode.Should().NotBe(0, "Script should fail with invalid version");
        result.Output.Should().Contain("Invalid version format");
    }

    [Fact]
    public async Task GenerateWingetManifests_WithMissingMsiFile_ShouldFail()
    {
        // Arrange
        var version = "1.0.0";
        var x64MsiPath = Path.Combine(_testDirectory, "missing-x64.msi");
        var arm64MsiPath = Path.Combine(_testDirectory, "test-arm64.msi");
        var outputDir = Path.Combine(_testDirectory, "manifests");

        // Create only one MSI file (the other is missing)
        await File.WriteAllTextAsync(arm64MsiPath, "Test MSI content");

        // Act
        var result = await RunPowerShellScript(
            Path.Combine(_scriptsDirectory, "generate-winget-manifests.ps1"),
            $"-Version \"{version}\" -X64MsiPath \"{x64MsiPath}\" -Arm64MsiPath \"{arm64MsiPath}\" -OutputDir \"{outputDir}\"");

        // Assert
        result.ExitCode.Should().NotBe(0, "Script should fail with missing MSI file");
        result.Output.Should().Contain("MSI file not found");
    }

    [Fact]
    public async Task GenerateWingetManifests_WithValidateWithWingetFlag_ShouldHandleWingetUnavailability()
    {
        // Skip test on non-Windows platforms where winget is not available
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On non-Windows platforms, just verify that the script runs without validation
            var nonWinVersion = "1.2.3";
            var nonWinX64MsiPath = Path.Combine(_testDirectory, "test-x64.msi");
            var nonWinArm64MsiPath = Path.Combine(_testDirectory, "test-arm64.msi");
            var nonWinOutputDir = Path.Combine(_testDirectory, "manifests");

            // Create test MSI files
            await File.WriteAllTextAsync(nonWinX64MsiPath, "Test MSI content for x64");
            await File.WriteAllTextAsync(nonWinArm64MsiPath, "Test MSI content for ARM64");

            // Act: Run without validation flag since winget is not available on non-Windows
            var nonWinResult = await RunPowerShellScript(
                Path.Combine(_scriptsDirectory, "generate-winget-manifests.ps1"),
                $"-Version \"{nonWinVersion}\" -X64MsiPath \"{nonWinX64MsiPath}\" -Arm64MsiPath \"{nonWinArm64MsiPath}\" -OutputDir \"{nonWinOutputDir}\"");

            // Assert: Should succeed without validation
            nonWinResult.ExitCode.Should().Be(0, "Script should succeed on non-Windows platforms without validation");
            nonWinResult.Output.Should().MatchRegex(@"All winget manifests generated successfully");
            return;
        }

        // Windows-specific test for winget validation handling
        // Arrange
        var version = "1.2.3";
        var x64MsiPath = Path.Combine(_testDirectory, "test-x64.msi");
        var arm64MsiPath = Path.Combine(_testDirectory, "test-arm64.msi");
        var outputDir = Path.Combine(_testDirectory, "manifests");

        // Create test MSI files
        await File.WriteAllTextAsync(x64MsiPath, "Test MSI content for x64");
        await File.WriteAllTextAsync(arm64MsiPath, "Test MSI content for ARM64");

        // Act
        var result = await RunPowerShellScript(
            Path.Combine(_scriptsDirectory, "generate-winget-manifests.ps1"),
            $"-Version \"{version}\" -X64MsiPath \"{x64MsiPath}\" -Arm64MsiPath \"{arm64MsiPath}\" -OutputDir \"{outputDir}\" -ValidateWithWinget");

        // Assert
        result.ExitCode.Should().Be(0, "Script should succeed even when winget is not available");
        result.Output.Should().Contain("Winget is not available or not installed. Validation will be skipped");
        // Check for success message - handle cross-platform output differences
        result.Output.Should().MatchRegex(@"Manifest generation completed without validation");
    }

    [Fact]
    public void WingetTemplates_ShouldExistAndBeValid()
    {
        // Arrange
        var expectedTemplates = new[]
        {
            "RvToolsMerge.RvToolsMerge.yaml.template",
            "RvToolsMerge.RvToolsMerge.installer.yaml.template",
            "RvToolsMerge.RvToolsMerge.locale.en-US.yaml.template"
        };

        // Act & Assert
        foreach (var template in expectedTemplates)
        {
            var templatePath = Path.Combine(_templatesDirectory, template);
            File.Exists(templatePath).Should().BeTrue($"Template file {template} should exist");

            var content = File.ReadAllText(templatePath);
            content.Should().NotBeNullOrEmpty($"Template {template} should have content");
            
            // Verify template has placeholder tokens
            content.Should().Contain("{{VERSION}}", $"Template {template} should contain VERSION placeholder");
        }
    }

    private async Task ValidateManifestContent(string outputDir, string expectedVersion, string expectedReleaseNotes)
    {
        // Validate version manifest
        var versionManifest = await File.ReadAllTextAsync(Path.Combine(outputDir, "RvToolsMerge.RvToolsMerge.yaml"));
        versionManifest.Should().Contain($"PackageVersion: {expectedVersion}");
        versionManifest.Should().Contain("ManifestType: version");
        versionManifest.Should().Contain("ManifestVersion: 1.6.0");

        // Validate installer manifest  
        var installerManifest = await File.ReadAllTextAsync(Path.Combine(outputDir, "RvToolsMerge.RvToolsMerge.installer.yaml"));
        installerManifest.Should().Contain($"PackageVersion: {expectedVersion}");
        installerManifest.Should().Contain("ManifestType: installer");
        installerManifest.Should().Contain("InstallerType: wix");
        installerManifest.Should().Contain("Architecture: x64");
        installerManifest.Should().Contain("Architecture: arm64");
        
        // Should contain SHA256 hashes
        installerManifest.Should().MatchRegex(@"InstallerSha256: [A-F0-9]{64}");

        // Validate locale manifest
        var localeManifest = await File.ReadAllTextAsync(Path.Combine(outputDir, "RvToolsMerge.RvToolsMerge.locale.en-US.yaml"));
        localeManifest.Should().Contain($"PackageVersion: {expectedVersion}");
        localeManifest.Should().Contain("ManifestType: defaultLocale");
        localeManifest.Should().Contain(expectedReleaseNotes);
        localeManifest.Should().Contain("Publisher: Stefan Broenner");
        localeManifest.Should().Contain("PackageName: RVToolsMerge");

        // Validate YAML syntax by attempting to parse
        var deserializer = new DeserializerBuilder().Build();
        foreach (var manifestFile in Directory.GetFiles(outputDir, "*.yaml"))
        {
            var content = await File.ReadAllTextAsync(manifestFile);
            var action = () => deserializer.Deserialize(content);
            action.Should().NotThrow($"Manifest {Path.GetFileName(manifestFile)} should be valid YAML");
        }
    }

    private async Task<(int ExitCode, string Output)> RunPowerShellScript(string scriptPath, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh",
            Arguments = $"\"{scriptPath}\" {arguments}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        var combinedOutput = output;
        if (!string.IsNullOrEmpty(error))
        {
            combinedOutput += Environment.NewLine + "STDERR:" + Environment.NewLine + error;
        }

        return (process.ExitCode, combinedOutput);
    }

    private static string GetProjectRootDirectory()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var directoryInfo = new DirectoryInfo(currentDirectory);

        while (directoryInfo != null && !File.Exists(Path.Combine(directoryInfo.FullName, "RVToolsMerge.sln")))
        {
            directoryInfo = directoryInfo.Parent;
        }

        if (directoryInfo == null)
        {
            throw new InvalidOperationException("Could not find project root directory with RVToolsMerge.sln");
        }

        return directoryInfo.FullName;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}