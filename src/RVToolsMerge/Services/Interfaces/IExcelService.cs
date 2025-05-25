//-----------------------------------------------------------------------
// <copyright file="IExcelService.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using ClosedXML.Excel;
using RVToolsMerge.Models;

namespace RVToolsMerge.Services.Interfaces;

/// <summary>
/// Interface for Excel file operations.
/// </summary>
public interface IExcelService
{
    /// <summary>
    /// Checks if a sheet exists in a workbook.
    /// </summary>
    /// <param name="workbook">The workbook to check.</param>
    /// <param name="sheetName">The name of the sheet to check for.</param>
    /// <returns>True if the sheet exists; otherwise, false.</returns>
    bool SheetExists(XLWorkbook workbook, string sheetName);

    /// <summary>
    /// Gets the column names from a worksheet.
    /// </summary>
    /// <param name="worksheet">The worksheet to extract column names from.</param>
    /// <returns>A list of column names.</returns>
    List<string> GetColumnNames(IXLWorksheet worksheet);

    /// <summary>
    /// Creates a mapping between column indices in the source worksheet and the common columns collection.
    /// </summary>
    /// <param name="worksheet">The worksheet to analyze for column mappings.</param>
    /// <param name="commonColumns">The list of common column names.</param>
    /// <returns>A list of column mappings.</returns>
    List<ColumnMapping> GetColumnMapping(IXLWorksheet worksheet, List<string> commonColumns);

    /// <summary>
    /// Opens an Excel workbook from a file path.
    /// </summary>
    /// <param name="filePath">The path to the Excel file to open.</param>
    /// <returns>The opened workbook.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file is not found.</exception>
    XLWorkbook OpenWorkbook(string filePath);
}
