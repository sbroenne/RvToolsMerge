//-----------------------------------------------------------------------
// <copyright file="ValidationService.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using ClosedXML.Excel;
using RVToolsMerge.Configuration;
using RVToolsMerge.Exceptions;
using RVToolsMerge.Models;
using RVToolsMerge.Services.Interfaces;

namespace RVToolsMerge.Services;

/// <summary>
/// Service for validating RVTools Excel files.
/// </summary>
public class ValidationService : IValidationService
{
    private readonly IExcelService _excelService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationService"/> class.
    /// </summary>
    /// <param name="excelService">The Excel service to use for file operations.</param>
    public ValidationService(IExcelService excelService)
    {
        _excelService = excelService;
    }

    /// <summary>
    /// Validates a file for required sheets and mandatory columns.
    /// </summary>
    /// <param name="filePath">The path to the file to validate.</param>
    /// <param name="ignoreMissingOptionalSheets">Whether to ignore missing optional sheets.</param>
    /// <param name="issues">List to store validation issues.</param>
    /// <returns>True if the file is valid; otherwise, false.</returns>
    public bool ValidateFile(string filePath, bool ignoreMissingOptionalSheets, List<ValidationIssue> issues)
    {
        string fileName = Path.GetFileName(filePath);
        bool fileIsValid = true;

        try
        {
            using var workbook = new XLWorkbook(filePath);

            // Step 1: Validate vInfo sheet (always required)
            fileIsValid = ValidateRequiredSheet(workbook, fileName, issues);

            // Step 2: If the file is still valid, check the optional sheets if they exist
            if (fileIsValid)
            {
                ValidateOptionalSheets(workbook, fileName, ignoreMissingOptionalSheets, issues, ref fileIsValid);
            }
        }
        catch (Exception ex)
        {
            // Any exception during validation means the file is invalid
            fileIsValid = false;
            issues.Add(new ValidationIssue(
                fileName,
                true,
                $"Error validating file: {ex.Message}"
            ));
        }

        return fileIsValid;
    }

    /// <summary>
    /// Validates the required vInfo sheet in a workbook.
    /// </summary>
    /// <param name="workbook">The workbook to validate.</param>
    /// <param name="fileName">The name of the file being validated.</param>
    /// <param name="issues">List to store validation issues.</param>
    /// <returns>True if the required sheet is valid; otherwise, false.</returns>
    private bool ValidateRequiredSheet(XLWorkbook workbook, string fileName, List<ValidationIssue> issues)
    {
        const string requiredSheet = "vInfo";

        if (!_excelService.SheetExists(workbook, requiredSheet))
        {
            issues.Add(new ValidationIssue(
                fileName,
                true,
                "Missing essential 'vInfo' sheet which is required for processing."
            ));
            return false;
        }

        // vInfo exists, check its mandatory columns
        var worksheet = workbook.Worksheet(requiredSheet);
        var columnNames = _excelService.GetColumnNames(worksheet);

        if (SheetConfiguration.MandatoryColumns.TryGetValue(requiredSheet, out var mandatoryColumns))
        {
            var missingColumns = mandatoryColumns.Where(col => !columnNames.Contains(col)).ToList();

            if (missingColumns.Count > 0)
            {
                issues.Add(new ValidationIssue(
                    fileName,
                    true,
                    $"'vInfo' sheet is missing mandatory column(s): {String.Join(", ", missingColumns)}"
                ));
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Validates optional sheets in a workbook.
    /// </summary>
    /// <param name="workbook">The workbook to validate.</param>
    /// <param name="fileName">The name of the file being validated.</param>
    /// <param name="ignoreMissingSheets">Whether to ignore missing optional sheets.</param>
    /// <param name="issues">List to store validation issues.</param>
    /// <param name="fileIsValid">Reference to the file validity flag.</param>
    private void ValidateOptionalSheets(
        XLWorkbook workbook,
        string fileName,
        bool ignoreMissingSheets,
        List<ValidationIssue> issues,
        ref bool fileIsValid)
    {
        // Check each optional sheet (vHost, vPartition, vMemory)
        foreach (var sheetName in SheetConfiguration.RequiredSheets.Where(s => s != "vInfo"))
        {
            if (_excelService.SheetExists(workbook, sheetName))
            {
                ValidateOptionalSheetColumns(workbook, fileName, sheetName, ignoreMissingSheets, issues, ref fileIsValid);
            }
            else if (!ignoreMissingSheets)
            {
                // Optional sheet is missing and we're not ignoring missing sheets
                fileIsValid = false;
                issues.Add(new ValidationIssue(
                    fileName,
                    true,
                    $"Missing optional sheet '{sheetName}'"
                ));
            }
            else
            {
                // Sheet is missing but we're ignoring missing sheets, add to validation issues
                issues.Add(new ValidationIssue(
                    fileName,
                    false,
                    $"Missing optional sheet '{sheetName}'."
                ));
            }
        }
    }

    /// <summary>
    /// Validates the columns in an optional sheet.
    /// </summary>
    /// <param name="workbook">The workbook to validate.</param>
    /// <param name="fileName">The name of the file being validated.</param>
    /// <param name="sheetName">The name of the sheet to validate.</param>
    /// <param name="ignoreMissingSheets">Whether to ignore missing sheets.</param>
    /// <param name="issues">List to store validation issues.</param>
    /// <param name="fileIsValid">Reference to the file validity flag.</param>
    private void ValidateOptionalSheetColumns(
        XLWorkbook workbook,
        string fileName,
        string sheetName,
        bool ignoreMissingSheets,
        List<ValidationIssue> issues,
        ref bool fileIsValid)
    {
        // Optional sheet exists, validate its mandatory columns
        var worksheet = workbook.Worksheet(sheetName);
        var columnNames = _excelService.GetColumnNames(worksheet);

        if (SheetConfiguration.MandatoryColumns.TryGetValue(sheetName, out var mandatoryColumns))
        {
            var missingColumns = mandatoryColumns.Where(col => !columnNames.Contains(col)).ToList();

            if (missingColumns.Count > 0)
            {
                // For optional sheets with missing columns, just log warning
                if (ignoreMissingSheets)
                {
                    // Add a warning but continue processing the file
                    issues.Add(new ValidationIssue(
                        fileName,
                        false,
                        $"Sheet '{sheetName}' has missing column(s): {String.Join(", ", missingColumns)}. This sheet may be excluded from processing."
                    ));
                }
                else
                {
                    // Missing columns in optional sheet but not ignoring - file is invalid
                    fileIsValid = false;
                    issues.Add(new ValidationIssue(
                        fileName,
                        true,
                        $"Sheet '{sheetName}' is missing mandatory column(s): {String.Join(", ", missingColumns)}"
                    ));
                }
            }
        }
    }

    /// <summary>
    /// Checks if a row has empty values in mandatory columns.
    /// </summary>
    /// <param name="rowData">The row data to check.</param>
    /// <param name="mandatoryColumnIndices">Indices of mandatory columns.</param>
    /// <returns>True if any mandatory column has an empty value; otherwise, false.</returns>
    public bool HasEmptyMandatoryValues(XLCellValue[] rowData, List<int> mandatoryColumnIndices)
    {
        return mandatoryColumnIndices.Any(idx =>
            idx >= 0 &&
            (EqualityComparer<XLCellValue>.Default.Equals(rowData[idx], default) ||
             String.IsNullOrWhiteSpace(rowData[idx].ToString()))
        );
    }
}
