//-----------------------------------------------------------------------
// <copyright file="ColumnMapping.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

namespace RVToolsMerge.Models;

/// <summary>
/// Record to store column mapping information.
/// </summary>
public record ColumnMapping(int FileColumnIndex, int CommonColumnIndex);
