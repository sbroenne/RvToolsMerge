//-----------------------------------------------------------------------
// <copyright file="IAnonymizationService.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------
using ClosedXML.Excel;

namespace RVToolsMerge.Services.Interfaces;

/// <summary>
/// Interface for anonymizing RVTools data.
/// </summary>
public interface IAnonymizationService
{
    /// <summary>
    /// Adds or updates a column identifier for anonymization.
    /// </summary>
    /// <param name="columnName">The name of the column to anonymize.</param>
    /// <param name="prefix">The prefix to use for anonymized values.</param>
    void AddColumnIdentifier(string columnName, string prefix);

    /// <summary>
    /// Removes a column identifier from anonymization.
    /// </summary>
    /// <param name="columnName">The name of the column to remove.</param>
    void RemoveColumnIdentifier(string columnName);

    /// <summary>
    /// Gets all configured column identifiers.
    /// </summary>
    /// <returns>Dictionary mapping column names to their prefix.</returns>
    Dictionary<string, string> GetColumnIdentifiers();

    /// <summary>
    /// Anonymizes a cell value based on its column type.
    /// </summary>
    /// <param name="value">The original cell value.</param>
    /// <param name="currentColumnIndex">The index of the current column.</param>
    /// <param name="anonymizeColumnIndices">Dictionary mapping column names to indices for anonymization.</param>
    /// <param name="fileName">The name of the file being processed.</param>
    /// <returns>The anonymized cell value.</returns>
    XLCellValue AnonymizeValue(XLCellValue value, int currentColumnIndex, Dictionary<string, int> anonymizeColumnIndices, string fileName);

    /// <summary>
    /// Gets the current anonymization statistics.
    /// </summary>
    /// <returns>Dictionary with counts of anonymized items by column and file.</returns>
    Dictionary<string, Dictionary<string, int>> GetAnonymizationStatistics();

    /// <summary>
    /// Gets all anonymization mappings from original values to anonymized values.
    /// </summary>
    /// <returns>Dictionary mapping column names to dictionaries of file names to mappings of original-to-anonymized values.</returns>
    Dictionary<string, Dictionary<string, Dictionary<string, string>>> GetAnonymizationMappings();
}
