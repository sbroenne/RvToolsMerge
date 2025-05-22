//-----------------------------------------------------------------------
// <copyright file="ExcelService.cs" company="Stefan Broenner"> ">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using ClosedXML.Excel;
using RVToolsMerge.Configuration;
using RVToolsMerge.Models;
using RVToolsMerge.Services.Interfaces;
using System.Collections.Frozen;

namespace RVToolsMerge.Services;

/// <summary>
/// Service for Excel file operations.
/// </summary>
public class ExcelService : IExcelService
{
    /// <summary>
    /// Checks if a sheet exists in a workbook.
    /// </summary>
    /// <param name="workbook">The workbook to check.</param>
    /// <param name="sheetName">The name of the sheet to check for.</param>
    /// <returns>True if the sheet exists; otherwise, false.</returns>
    public bool SheetExists(XLWorkbook workbook, string sheetName) =>
        workbook.Worksheets.Any(sheet => sheet.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets the column names from a worksheet, normalizing them using the per-sheet ColumnHeaderMapping.
    /// </summary>
    /// <param name="worksheet">The worksheet to extract column names from.</param>
    /// <returns>A list of normalized column names.</returns>
    public List<string> GetColumnNames(IXLWorksheet worksheet)
    {
        var columnNames = new List<string>();
        var headerRow = worksheet.Row(1);
        var lastColumnUsed = worksheet.LastColumnUsed();
        int lastColumn = lastColumnUsed is not null ? lastColumnUsed.ColumnNumber() : 1;

        // Use only the mapping for the current sheet, if available
        var sheetName = worksheet.Name;
        SheetConfiguration.SheetColumnHeaderMappings.TryGetValue(sheetName, out var mapping);

        for (int col = 1; col <= lastColumn; col++)
        {
            var cell = headerRow.Cell(col);
            var cellValue = cell.Value.ToString();
            if (!string.IsNullOrWhiteSpace(cellValue))
            {
                // Use the mapping if available for this sheet, otherwise keep the original name
                var normalizedName = mapping?.GetValueOrDefault(cellValue, cellValue) ?? cellValue;
                columnNames.Add(normalizedName);
            }
        }

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
        var mapping = new List<ColumnMapping>();
        // Get the first row
        var headerRow = worksheet.Row(1);
        // Find the last column with data
        var lastColumnUsed = worksheet.LastColumnUsed();
        int lastColumn = lastColumnUsed is not null ? lastColumnUsed.ColumnNumber() : 1;

        // Create a mapping between the file's column indices and the common column indices
        for (int fileColIndex = 1; fileColIndex <= lastColumn; fileColIndex++)
        {
            var cell = headerRow.Cell(fileColIndex);
            var cellValue = cell.Value;
            if (!string.IsNullOrWhiteSpace(cellValue.ToString()))
            {
                // Apply column header mapping for this sheet, if available
                var sheetName = worksheet.Name;
                SheetConfiguration.SheetColumnHeaderMappings.TryGetValue(sheetName, out var headerMapping);
                string originalName = cellValue.ToString();
                string mappedName = headerMapping is not null
                    ? ((IReadOnlyDictionary<string, string?>)headerMapping).GetValueOrDefault(originalName, null) ?? originalName
                    : originalName;

                int commonIndex = commonColumns.IndexOf(mappedName);
                if (commonIndex < 0 && headerMapping is not null)
                {
                    // If mapped name not found, try the original name from the sheet
                    commonIndex = commonColumns.IndexOf(originalName);
                }
                if (commonIndex >= 0)
                {
                    mapping.Add(new ColumnMapping(fileColIndex, commonIndex));
                }
            }
        }

        return mapping;
    }
}
