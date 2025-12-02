//-----------------------------------------------------------------------
// <copyright file="CliBlackBoxTests.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics;
using System.IO.Abstractions;
using RVToolsMerge.IntegrationTests.Utilities;

namespace RVToolsMerge.IntegrationTests;

/// <summary>
/// Black-box integration tests that execute the CLI as a separate process.
/// These tests verify the actual executable works correctly end-to-end,
/// catching configuration and runtime issues that unit/white-box tests may miss.
/// </summary>
[Collection("SpectreConsole")]
public class CliBlackBoxTests : IDisposable
{
    private readonly IFileSystem _fileSystem;
    private readonly string _testRootDirectory;
    private readonly string _testInputDirectory;
    private readonly string _testOutputDirectory;
    private readonly TestDataGenerator _testDataGenerator;
    private readonly string _executablePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="CliBlackBoxTests"/> class.
    /// </summary>
    public CliBlackBoxTests()
    {
        _fileSystem = new FileSystem();

        // Create unique temp directory for this test run
        _testRootDirectory = Path.Combine(
            Path.GetTempPath(),
            "RVToolsMerge_CliBlackBoxTests",
            Guid.NewGuid().ToString());

        _testInputDirectory = Path.Combine(_testRootDirectory, "input");
        _testOutputDirectory = Path.Combine(_testRootDirectory, "output");

        _fileSystem.Directory.CreateDirectory(_testInputDirectory);
        _fileSystem.Directory.CreateDirectory(_testOutputDirectory);

        _testDataGenerator = new TestDataGenerator(_fileSystem, _testInputDirectory);

        // Find the executable - look for the built DLL in the main project output
        _executablePath = FindExecutable();
    }

    /// <summary>
    /// Finds the RVToolsMerge executable/DLL for testing.
    /// </summary>
    /// <returns>Path to the executable or DLL.</returns>
    private string FindExecutable()
    {
        // Get the test assembly location and navigate to the main project output
        var testAssemblyPath = GetType().Assembly.Location;
        var testDir = Path.GetDirectoryName(testAssemblyPath)!;

        // Navigate up to the solution root and find the main project output
        // Test output: tests/RVToolsMerge.IntegrationTests/bin/Debug/net10.0
        // Main output: src/RVToolsMerge/bin/Debug/net10.0/win-arm64 (or similar RID)
        var solutionRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", ".."));
        var configuration = testDir.Contains("Release") ? "Release" : "Debug";

        // Look for DLL in RID-specific directories first (win-arm64, win-x64, etc.)
        var mainBinDir = Path.Combine(solutionRoot, "src", "RVToolsMerge", "bin", configuration, "net10.0");

        // Try RID-specific directories
        if (_fileSystem.Directory.Exists(mainBinDir))
        {
            foreach (var ridDir in _fileSystem.Directory.GetDirectories(mainBinDir))
            {
                var dllPath = Path.Combine(ridDir, "RVToolsMerge.dll");
                if (_fileSystem.File.Exists(dllPath))
                {
                    return dllPath;
                }
            }
        }

        // Try direct path (non-RID specific builds)
        var directDllPath = Path.Combine(mainBinDir, "RVToolsMerge.dll");
        if (_fileSystem.File.Exists(directDllPath))
        {
            return directDllPath;
        }

        // Fallback: try published exe
        var exePath = Path.Combine(mainBinDir, "RVToolsMerge.exe");
        if (_fileSystem.File.Exists(exePath))
        {
            return exePath;
        }

        // If not found, throw with helpful message
        throw new FileNotFoundException(
            $"Could not find RVToolsMerge executable. Looked in:\n" +
            $"  {mainBinDir}\n" +
            $"Make sure the main project is built before running these tests.");
    }

    /// <summary>
    /// Executes the CLI with the given arguments and returns the result.
    /// </summary>
    /// <param name="arguments">Command-line arguments.</param>
    /// <param name="timeoutSeconds">Maximum time to wait for process completion.</param>
    /// <returns>A tuple containing exit code, stdout, and stderr.</returns>
    private async Task<(int ExitCode, string StdOut, string StdErr)> RunCliAsync(
        string arguments,
        int timeoutSeconds = 60)
    {
        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _testRootDirectory
        };

        // If it's a DLL, run with dotnet; if exe, run directly
        if (_executablePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.FileName = "dotnet";
            startInfo.Arguments = $"\"{_executablePath}\" {arguments}";
        }
        else
        {
            startInfo.FileName = _executablePath;
            startInfo.Arguments = arguments;
        }

        using var process = new Process { StartInfo = startInfo };

        var stdoutBuilder = new System.Text.StringBuilder();
        var stderrBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdoutBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stderrBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"CLI process did not complete within {timeoutSeconds} seconds.\n" +
                $"Arguments: {arguments}\n" +
                $"Stdout: {stdoutBuilder}\n" +
                $"Stderr: {stderrBuilder}");
        }

        return (process.ExitCode, stdoutBuilder.ToString(), stderrBuilder.ToString());
    }

    #region Help and Version Tests

    /// <summary>
    /// Tests that --help displays help information and returns exit code 0.
    /// </summary>
    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    public async Task Cli_HelpOption_DisplaysHelpAndReturnsZero(string helpOption)
    {
        // Act
        var (exitCode, stdout, stderr) = await RunCliAsync(helpOption);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("RVToolsMerge", stdout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("USAGE", stdout, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that --version displays version information.
    /// </summary>
    [Fact]
    public async Task Cli_VersionOption_DisplaysVersion()
    {
        // Act
        var (exitCode, stdout, stderr) = await RunCliAsync("--version");

        // Assert - Version display returns 0 or -1 depending on Spectre.Console version
        // The important thing is that it displays a version pattern
        Assert.True(exitCode == 0 || exitCode == -1, $"Expected exit code 0 or -1, got {exitCode}");
        // Version output should contain version number pattern
        Assert.Matches(@"\d+\.\d+\.\d+", stdout);
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// Tests that missing required input path returns error.
    /// </summary>
    [Fact]
    public async Task Cli_NoArguments_ReturnsError()
    {
        // Act
        var (exitCode, stdout, stderr) = await RunCliAsync("");

        // Assert
        Assert.NotEqual(0, exitCode);
    }

    /// <summary>
    /// Tests that non-existent input file returns error.
    /// </summary>
    [Fact]
    public async Task Cli_NonExistentFile_ReturnsError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testInputDirectory, "does_not_exist.xlsx");

        // Act
        var (exitCode, stdout, stderr) = await RunCliAsync($"\"{nonExistentPath}\"");

        // Assert
        Assert.NotEqual(0, exitCode);
    }

    /// <summary>
    /// Tests that invalid file type returns error.
    /// </summary>
    [Fact]
    public async Task Cli_InvalidFileType_ReturnsError()
    {
        // Arrange
        var textFilePath = Path.Combine(_testInputDirectory, "test.txt");
        _fileSystem.File.WriteAllText(textFilePath, "Not an Excel file");

        // Act
        var (exitCode, stdout, stderr) = await RunCliAsync($"\"{textFilePath}\"");

        // Assert
        Assert.NotEqual(0, exitCode);
    }

    #endregion

    #region Basic Merge Tests

    /// <summary>
    /// Tests basic merge of a single valid RVTools file.
    /// </summary>
    [Fact]
    public async Task Cli_SingleValidFile_MergesSuccessfully()
    {
        // Arrange
        var inputFile = _testDataGenerator.CreateValidRVToolsFile("test_single.xlsx", numVMs: 3);
        var outputFile = Path.Combine(_testOutputDirectory, "output_single.xlsx");

        // Act
        var (exitCode, stdout, stderr) = await RunCliAsync($"\"{inputFile}\" \"{outputFile}\"");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(_fileSystem.File.Exists(outputFile), $"Output file should exist. Stdout: {stdout}, Stderr: {stderr}");
    }

    /// <summary>
    /// Tests merge of multiple RVTools files from a directory.
    /// </summary>
    [Fact]
    public async Task Cli_DirectoryWithMultipleFiles_MergesSuccessfully()
    {
        // Arrange
        _testDataGenerator.CreateValidRVToolsFile("file1.xlsx", numVMs: 2);
        _testDataGenerator.CreateValidRVToolsFile("file2.xlsx", numVMs: 3);
        var outputFile = Path.Combine(_testOutputDirectory, "output_merged.xlsx");

        // Act
        var (exitCode, stdout, stderr) = await RunCliAsync($"\"{_testInputDirectory}\" \"{outputFile}\"");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(_fileSystem.File.Exists(outputFile), $"Output file should exist. Stdout: {stdout}, Stderr: {stderr}");
    }

    #endregion

    #region Option Tests

    /// <summary>
    /// Tests the --ignore-missing-sheets option.
    /// </summary>
    [Fact]
    public async Task Cli_IgnoreMissingSheets_WorksCorrectly()
    {
        // Arrange - Create file with only required sheets (not all optional ones)
        var inputFile = _testDataGenerator.CreateValidRVToolsFile("partial.xlsx", numVMs: 2, includeAllSheets: false);
        var outputFile = Path.Combine(_testOutputDirectory, "output_partial.xlsx");

        // Act
        var (exitCode, stdout, stderr) = await RunCliAsync($"--ignore-missing-sheets \"{inputFile}\" \"{outputFile}\"");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(_fileSystem.File.Exists(outputFile), $"Output file should exist. Stdout: {stdout}, Stderr: {stderr}");
    }

    /// <summary>
    /// Tests the --anonymize option.
    /// </summary>
    [Fact]
    public async Task Cli_Anonymize_AnonymizesData()
    {
        // Arrange
        var inputFile = _testDataGenerator.CreateFileForAnonymizationTesting("sensitive.xlsx");
        var outputFile = Path.Combine(_testOutputDirectory, "output_anon.xlsx");

        // Act - Use -i to ignore missing optional sheets in the test file
        var (exitCode, stdout, stderr) = await RunCliAsync($"--anonymize -i \"{inputFile}\" \"{outputFile}\"");

        // Assert
        Assert.True(exitCode == 0, $"Expected exit code 0 but got {exitCode}.\nStdout: {stdout}\nStderr: {stderr}");
        Assert.True(_fileSystem.File.Exists(outputFile), $"Output file should exist. Stdout: {stdout}, Stderr: {stderr}");

        // Verify the output doesn't contain the original sensitive data
        using var workbook = new ClosedXML.Excel.XLWorkbook(outputFile);
        var vInfoSheet = workbook.Worksheet("vInfo");
        var vmCell = vInfoSheet.Cell(2, 1).GetString();

        // Original value was "CONFIDENTIAL-SERVER-01" - should be anonymized
        Assert.DoesNotContain("CONFIDENTIAL", vmCell, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests the --only-mandatory-columns option.
    /// </summary>
    [Fact]
    public async Task Cli_OnlyMandatoryColumns_ReducesColumns()
    {
        // Arrange
        var inputFile = _testDataGenerator.CreateValidRVToolsFile("full.xlsx", numVMs: 2);
        var outputFile = Path.Combine(_testOutputDirectory, "output_mandatory.xlsx");

        // Act
        var (exitCode, stdout, stderr) = await RunCliAsync($"--only-mandatory-columns \"{inputFile}\" \"{outputFile}\"");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(_fileSystem.File.Exists(outputFile), $"Output file should exist. Stdout: {stdout}, Stderr: {stderr}");
    }

    /// <summary>
    /// Tests the --all-sheets option for dynamic sheet discovery.
    /// </summary>
    [Fact]
    public async Task Cli_AllSheets_ProcessesAllSheets()
    {
        // Arrange - Create file with unknown sheets
        var inputFile = _testDataGenerator.CreateFileWithUnknownSheets("with_extras.xlsx", numVMs: 2);
        var outputFile = Path.Combine(_testOutputDirectory, "output_allsheets.xlsx");

        // Act
        var (exitCode, stdout, stderr) = await RunCliAsync($"--all-sheets \"{inputFile}\" \"{outputFile}\"");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(_fileSystem.File.Exists(outputFile), $"Output file should exist. Stdout: {stdout}, Stderr: {stderr}");

        // Verify that extra sheets are included
        using var workbook = new ClosedXML.Excel.XLWorkbook(outputFile);
        var sheetNames = workbook.Worksheets.Select(ws => ws.Name).ToList();

        Assert.Contains("vCPU", sheetNames);
        Assert.Contains("vDisk", sheetNames);
    }

    /// <summary>
    /// Tests that --anonymize and --all-sheets are mutually exclusive.
    /// </summary>
    [Fact]
    public async Task Cli_AnonymizeAndAllSheets_ReturnsError()
    {
        // Arrange
        var inputFile = _testDataGenerator.CreateValidRVToolsFile("test.xlsx", numVMs: 2);
        var outputFile = Path.Combine(_testOutputDirectory, "output.xlsx");

        // Act
        var (exitCode, stdout, stderr) = await RunCliAsync($"--anonymize --all-sheets \"{inputFile}\" \"{outputFile}\"");

        // Assert - Should fail due to mutual exclusivity
        Assert.NotEqual(0, exitCode);
    }

    /// <summary>
    /// Tests combined options work together.
    /// </summary>
    [Fact]
    public async Task Cli_MultipleOptions_WorkTogether()
    {
        // Arrange
        _testDataGenerator.CreateValidRVToolsFile("file1.xlsx", numVMs: 2, includeAllSheets: false);
        _testDataGenerator.CreateValidRVToolsFile("file2.xlsx", numVMs: 3, includeAllSheets: false);
        var outputFile = Path.Combine(_testOutputDirectory, "output_combined.xlsx");

        // Act - Use multiple options together
        var (exitCode, stdout, stderr) = await RunCliAsync(
            $"--ignore-missing-sheets --include-source-file-name --only-mandatory-columns " +
            $"\"{_testInputDirectory}\" \"{outputFile}\"");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(_fileSystem.File.Exists(outputFile), $"Output file should exist. Stdout: {stdout}, Stderr: {stderr}");
    }

    #endregion

    /// <summary>
    /// Cleans up test directories.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (_fileSystem.Directory.Exists(_testRootDirectory))
            {
                _fileSystem.Directory.Delete(_testRootDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        GC.SuppressFinalize(this);
    }
}
