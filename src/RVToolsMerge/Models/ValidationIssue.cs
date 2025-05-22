//-----------------------------------------------------------------------
// <copyright file="ValidationIssue.cs" company="Stefan Broenner"> ">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

namespace RVToolsMerge.Models;

/// <summary>
/// Record to store information about validation issues.
/// </summary>
public record ValidationIssue(string FileName, bool Skipped, string ValidationError);
