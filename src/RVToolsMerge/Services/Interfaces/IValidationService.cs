//-----------------------------------------------------------------------
// <copyright file="IValidationService.cs" company="Stefan Broenner"> ">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using ClosedXML.Excel;
using RVToolsMerge.Models;

namespace RVToolsMerge.Services.Interfaces;

/// <summary>
/// Interface for validating RVTools files.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates a file for required sheets and mandatory columns.
    /// </summary>
    /// <param name="filePath">The path to the file to validate.</param>
    /// <param name="ignoreMissingOptionalSheets">Whether to ignore missing optional sheets.</param>
    /// <param name="issues">List to store validation issues.</param>
    /// <returns>True if the file is valid; otherwise, false.</returns>
    bool ValidateFile(string filePath, bool ignoreMissingOptionalSheets, List<ValidationIssue> issues);

    /// <summary>
    /// Checks if a row has empty values in mandatory columns.
    /// </summary>
    /// <param name="rowData">The row data to check.</param>
    /// <param name="mandatoryColumnIndices">Indices of mandatory columns.</param>
    /// <returns>True if any mandatory column has an empty value; otherwise, false.</returns>
    bool HasEmptyMandatoryValues(XLCellValue[] rowData, List<int> mandatoryColumnIndices);
}
