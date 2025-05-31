//-----------------------------------------------------------------------
// <copyright file="ExcelService.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using System.Collections.Frozen;
using System.IO.Abstractions;
using ClosedXML.Excel;
using RVToolsMerge.Configuration;
using RVToolsMerge.Models;
using RVToolsMerge.Services.Interfaces;

namespace RVToolsMerge.Services;

/// <summary>
/// Service for Excel file operations.
/// </summary>
public class ExcelService : IExcelService
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelService"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    public ExcelService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Checks if a sheet exists in a workbook.
    /// </summary>
    /// <param name="workbook">The workbook to check.</param>
    /// <param name="sheetName">The name of the sheet to check for.</param>
    /// <returns>True if the sheet exists; otherwise, false.</returns>
    public bool SheetExists(XLWorkbook workbook, string sheetName) =>
        workbook.Worksheets.Any(sheet => sheet.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets both column names and column mapping from a worksheet in a single pass for better performance.
    /// </summary>
    /// <param name="worksheet">The worksheet to extract column information from.</param>
    /// <param name="commonColumns">The list of common column names to map to.</param>
    /// <returns>A tuple containing the column names and column mappings.</returns>
    public (List<string> ColumnNames, List<ColumnMapping> ColumnMappings) GetColumnInformationOptimized(
        IXLWorksheet worksheet, 
        List<string> commonColumns)
    {
        var columnNames = new List<string>();
        var mappings = new List<ColumnMapping>();
        
        var headerRow = worksheet.Row(1);
        var lastColumnUsed = worksheet.LastColumnUsed();
        int lastColumn = lastColumnUsed?.ColumnNumber() ?? 1;

        // Use only the mapping for the current sheet, if available
        var sheetName = worksheet.Name;
        SheetConfiguration.SheetColumnHeaderMappings.TryGetValue(sheetName, out var headerMapping);

        for (int col = 1; col <= lastColumn; col++)
        {
            var cell = headerRow.Cell(col);
            var cellValue = cell.Value.ToString();
            
            if (!string.IsNullOrWhiteSpace(cellValue))
            {
                // Get the normalized column name
                var normalizedName = headerMapping?.GetValueOrDefault(cellValue, cellValue) ?? cellValue;
                columnNames.Add(normalizedName);

                // Create mapping if this column exists in common columns
                int commonIndex = commonColumns.IndexOf(normalizedName);
                if (commonIndex < 0 && headerMapping is not null)
                {
                    // If mapped name not found, try the original name from the sheet
                    commonIndex = commonColumns.IndexOf(cellValue);
                }
                
                if (commonIndex >= 0)
                {
                    mappings.Add(new ColumnMapping(col, commonIndex));
                }
            }
        }

        return (columnNames, mappings);
    }

    /// <summary>
    /// Gets the column names from a worksheet, normalizing them using the per-sheet ColumnHeaderMapping.
    /// </summary>
    /// <param name="worksheet">The worksheet to extract column names from.</param>
    /// <returns>A list of normalized column names.</returns>
    public List<string> GetColumnNames(IXLWorksheet worksheet)
    {
        var (columnNames, _) = GetColumnInformationOptimized(worksheet, []);
        return columnNames;
    }

    /// <summary>
    /// Creates a mapping between column indices in the source worksheet and the common columns collection.
    /// </summary>
    /// <param name="worksheet">The worksheet to analyze for column mappings.</param>
    /// <param name="commonColumns">The list of common column names.</param>
    /// <returns>A list of column mappings.</returns>
    public List<ColumnMapping> GetColumnMapping(IXLWorksheet worksheet, List<string> commonColumns)
    {
        var (_, mappings) = GetColumnInformationOptimized(worksheet, commonColumns);
        return mappings;
    }

    /// <summary>
    /// Opens an Excel workbook from a file path.
    /// </summary>
    /// <param name="filePath">The path to the Excel file to open.</param>
    /// <returns>The opened workbook.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access to the file is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the file is not a valid Excel file or is corrupted.</exception>
    public XLWorkbook OpenWorkbook(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        if (!_fileSystem.File.Exists(filePath))
        {
            var fileName = _fileSystem.Path.GetFileName(filePath);
            throw new FileNotFoundException($"Excel file not found: {fileName}");
        }

        try
        {
            using var stream = _fileSystem.File.OpenRead(filePath);
            return new XLWorkbook(stream);
        }
        catch (UnauthorizedAccessException ex)
        {
            var fileName = _fileSystem.Path.GetFileName(filePath);
            throw new UnauthorizedAccessException($"Access denied to Excel file '{fileName}'. Please check file permissions.", ex);
        }
        catch (IOException ex)
        {
            var fileName = _fileSystem.Path.GetFileName(filePath);
            throw new InvalidOperationException($"Error reading Excel file '{fileName}'. The file may be in use by another application or corrupted: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex.Message.Contains("not a valid Excel file") || ex.Message.Contains("corrupted"))
        {
            var fileName = _fileSystem.Path.GetFileName(filePath);
            throw new InvalidOperationException($"Excel file '{fileName}' is not a valid Excel file or may be corrupted.", ex);
        }
        catch (Exception ex)
        {
            var fileName = _fileSystem.Path.GetFileName(filePath);
            throw new InvalidOperationException($"Unexpected error opening Excel file '{fileName}': {ex.Message}", ex);
        }
    }
}
