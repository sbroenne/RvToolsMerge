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
    /// Anonymizes a cell value based on its column type.
    /// </summary>
    /// <param name="value">The original cell value.</param>
    /// <param name="currentColumnIndex">The index of the current column.</param>
    /// <param name="anonymizeColumnIndices">Dictionary mapping column names to indices for anonymization.</param>
    /// <returns>The anonymized cell value.</returns>
    XLCellValue AnonymizeValue(XLCellValue value, int currentColumnIndex, Dictionary<string, int> anonymizeColumnIndices);

    /// <summary>
    /// Gets the current anonymization statistics.
    /// </summary>
    /// <returns>Dictionary with counts of anonymized items by category.</returns>
    Dictionary<string, int> GetAnonymizationStatistics();
}
