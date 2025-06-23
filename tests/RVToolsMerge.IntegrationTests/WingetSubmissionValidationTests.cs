using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.Diagnostics;
using YamlDotNet.Serialization;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Integration tests for validating the complete winget submission process readiness.
/// These tests ensure that all components required for winget submission are properly configured and functional.
/// </summary>
public class WingetSubmissionValidationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _projectRoot;

    public WingetSubmissionValidationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "RVToolsMerge_WingetSubmissionTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _projectRoot = GetProjectRootDirectory();
    }

    [Fact]
    public async Task WingetSubmissionProcess_ValidateComprehensiveSetup_ShouldPassAllValidations()
    {
        // Act: Run the comprehensive validation script
        var result = await RunPowerShellScript(
            Path.Combine(_projectRoot, ".github", "scripts", "validate-winget-setup.ps1"),
            $"-ProjectRoot \"{_projectRoot}\"");

        // Assert: Validation should pass
        result.ExitCode.Should().Be(0, $"Winget setup validation should pass. Output: {result.Output}");
        
        // Verify specific success indicators
        result.Output.Should().Contain("All critical tests passed! Winget submission setup is ready.");
        result.Output.Should().Contain("✅ Passed Tests (");
        
        // Verify all expected test components are validated
        var expectedValidations = new[]
        {
            "Winget templates exist",
            "Templates have required placeholders", 
            "Manifest generation script exists",
            "Manifest generation works",
            "Winget submission workflow exists",
            "Version management includes winget generation",
            "Winget documentation exists",
            "Package identifier consistency",
            "Template files follow naming conventions",
            "Installer manifest has required fields"
        };

        foreach (var validation in expectedValidations)
        {
            result.Output.Should().Contain(validation, $"Validation '{validation}' should be included in the output");
        }
    }

    [Fact]
    public void WingetWorkflowFiles_ShouldExistAndBeProperlyConfigured()
    {
        // Arrange: Define expected workflow files
        var workflowFiles = new Dictionary<string, string[]>
        {
            [".github/workflows/winget-submission.yml"] = new[]
            {
                "name: Winget Submission Preparation",
                "workflow_dispatch",
                "releaseTag:",
                "dryRun:",
                "WINGET_SUBMISSION_TOKEN",
                "RvToolsMerge.RvToolsMerge.yaml",
                "RvToolsMerge.RvToolsMerge.installer.yaml",
                "RvToolsMerge.RvToolsMerge.locale.en-US.yaml"
            },
            [".github/workflows/version-management.yml"] = new[]
            {
                "generate-winget-manifests:",
                "generate-winget-manifests.ps1"
            }
        };

        // Act & Assert: Verify each workflow file exists and contains required content
        foreach (var (workflowPath, requiredContent) in workflowFiles)
        {
            var fullPath = Path.Combine(_projectRoot, workflowPath);
            File.Exists(fullPath).Should().BeTrue($"Workflow file {workflowPath} should exist");

            var content = File.ReadAllText(fullPath);
            content.Should().NotBeNullOrEmpty($"Workflow file {workflowPath} should not be empty");

            foreach (var required in requiredContent)
            {
                content.Should().Contain(required, $"Workflow {workflowPath} should contain '{required}'");
            }
        }
    }

    [Fact]
    public async Task GeneratedManifests_ShouldComplyWithWingetSchemaRequirements()
    {
        // Arrange: Create test MSI files
        var version = "1.0.0";
        var x64MsiPath = Path.Combine(_testDirectory, "test-x64.msi");
        var arm64MsiPath = Path.Combine(_testDirectory, "test-arm64.msi");
        var outputDir = Path.Combine(_testDirectory, "manifests");
        var releaseNotes = "Test release for schema validation";

        await File.WriteAllTextAsync(x64MsiPath, "Test MSI content for x64");
        await File.WriteAllTextAsync(arm64MsiPath, "Test MSI content for ARM64");

        // Act: Generate manifests
        var result = await RunPowerShellScript(
            Path.Combine(_projectRoot, ".github", "scripts", "generate-winget-manifests.ps1"),
            $"-Version \"{version}\" -X64MsiPath \"{x64MsiPath}\" -Arm64MsiPath \"{arm64MsiPath}\" -OutputDir \"{outputDir}\" -ReleaseNotes \"{releaseNotes}\"");

        // Assert: Generation should succeed
        result.ExitCode.Should().Be(0, $"Manifest generation should succeed. Output: {result.Output}");

        // Validate schema compliance for each manifest type
        await ValidateVersionManifestSchema(Path.Combine(outputDir, "RvToolsMerge.RvToolsMerge.yaml"), version);
        await ValidateInstallerManifestSchema(Path.Combine(outputDir, "RvToolsMerge.RvToolsMerge.installer.yaml"), version);
        await ValidateLocaleManifestSchema(Path.Combine(outputDir, "RvToolsMerge.RvToolsMerge.locale.en-US.yaml"), version, releaseNotes);
    }

    [Fact]
    public void WingetDocumentation_ShouldBeComprehensiveAndAccurate()
    {
        // Arrange: Define expected documentation files and their required content
        var documentationChecks = new Dictionary<string, string[]>
        {
            ["docs/winget-submission-setup.md"] = new[]
            {
                "Winget Submission Workflow Setup",
                "Prerequisites",
                "Required Manifest Files",
                "WINGET_SUBMISSION_TOKEN",
                "RvToolsMerge.RvToolsMerge.yaml",
                "RvToolsMerge.RvToolsMerge.installer.yaml",
                "RvToolsMerge.RvToolsMerge.locale.en-US.yaml",
                "microsoft/winget-pkgs"
            },
            [".github/winget-templates/README.md"] = new[]
            {
                "winget",
                "template"
            }
        };

        // Act & Assert: Validate each documentation file
        foreach (var (docPath, requiredContent) in documentationChecks)
        {
            var fullPath = Path.Combine(_projectRoot, docPath);
            File.Exists(fullPath).Should().BeTrue($"Documentation file {docPath} should exist");

            var content = File.ReadAllText(fullPath);
            content.Should().NotBeNullOrEmpty($"Documentation file {docPath} should not be empty");

            foreach (var required in requiredContent)
            {
                content.Should().Contain(required, $"Documentation {docPath} should contain '{required}'");
            }
        }
    }

    [Fact]
    public void WingetTemplates_ShouldHaveConsistentPackageIdentifier()
    {
        // Arrange: Expected package identifier
        const string expectedPackageId = "RvToolsMerge.RvToolsMerge";
        var templateDir = Path.Combine(_projectRoot, ".github", "winget-templates");
        var templates = Directory.GetFiles(templateDir, "*.template");

        // Act & Assert: Verify package identifier consistency across all templates
        templates.Should().NotBeEmpty("Template directory should contain template files");

        foreach (var template in templates)
        {
            var content = File.ReadAllText(template);
            content.Should().Contain($"PackageIdentifier: {expectedPackageId}", 
                $"Template {Path.GetFileName(template)} should have consistent package identifier");
        }
    }

    [Fact]
    public void WingetTemplates_ShouldUseCurrentSchemaVersion()
    {
        // Arrange: Expected schema version
        const string expectedSchemaVersion = "ManifestVersion: 1.6.0";
        var templateDir = Path.Combine(_projectRoot, ".github", "winget-templates");
        var templates = Directory.GetFiles(templateDir, "*.template");

        // Act & Assert: Verify schema version consistency
        templates.Should().NotBeEmpty("Template directory should contain template files");

        foreach (var template in templates)
        {
            var content = File.ReadAllText(template);
            content.Should().Contain(expectedSchemaVersion, 
                $"Template {Path.GetFileName(template)} should use current schema version 1.6.0");
        }
    }

    private async Task ValidateVersionManifestSchema(string manifestPath, string expectedVersion)
    {
        File.Exists(manifestPath).Should().BeTrue("Version manifest should exist");
        
        var content = await File.ReadAllTextAsync(manifestPath);
        var deserializer = new DeserializerBuilder().Build();
        var manifest = deserializer.Deserialize(content);
        
        // Validate required fields for version manifest
        content.Should().Contain($"PackageVersion: {expectedVersion}");
        content.Should().Contain("ManifestType: version");
        content.Should().Contain("ManifestVersion: 1.6.0");
        content.Should().Contain("PackageIdentifier: RvToolsMerge.RvToolsMerge");
        content.Should().Contain("DefaultLocale: en-US");
    }

    private async Task ValidateInstallerManifestSchema(string manifestPath, string expectedVersion)
    {
        File.Exists(manifestPath).Should().BeTrue("Installer manifest should exist");
        
        var content = await File.ReadAllTextAsync(manifestPath);
        var deserializer = new DeserializerBuilder().Build();
        var manifest = deserializer.Deserialize(content);
        
        // Validate required fields for installer manifest
        content.Should().Contain($"PackageVersion: {expectedVersion}");
        content.Should().Contain("ManifestType: installer");
        content.Should().Contain("ManifestVersion: 1.6.0");
        content.Should().Contain("PackageIdentifier: RvToolsMerge.RvToolsMerge");
        content.Should().Contain("Architecture: x64");
        content.Should().Contain("Architecture: arm64");
        content.Should().Contain("InstallerType: wix");
        content.Should().Contain("MinimumOSVersion: 10.0.17763.0");
        content.Should().MatchRegex(@"InstallerSha256: [A-F0-9]{64}");
    }

    private async Task ValidateLocaleManifestSchema(string manifestPath, string expectedVersion, string expectedReleaseNotes)
    {
        File.Exists(manifestPath).Should().BeTrue("Locale manifest should exist");
        
        var content = await File.ReadAllTextAsync(manifestPath);
        var deserializer = new DeserializerBuilder().Build();
        var manifest = deserializer.Deserialize(content);
        
        // Validate required fields for locale manifest
        content.Should().Contain($"PackageVersion: {expectedVersion}");
        content.Should().Contain("ManifestType: defaultLocale");
        content.Should().Contain("ManifestVersion: 1.6.0");
        content.Should().Contain("PackageIdentifier: RvToolsMerge.RvToolsMerge");
        content.Should().Contain("PackageLocale: en-US");
        content.Should().Contain("Publisher: Stefan Broenner");
        content.Should().Contain("PackageName: RVToolsMerge");
        content.Should().Contain(expectedReleaseNotes);
        content.Should().Contain("License: MIT");
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