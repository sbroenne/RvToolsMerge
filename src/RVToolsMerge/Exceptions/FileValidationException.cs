//-----------------------------------------------------------------------
// <copyright file="FileValidationException.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

namespace RVToolsMerge.Exceptions;

/// <summary>
/// Custom exception for file validation errors.
/// </summary>
public class FileValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public FileValidationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public FileValidationException(string message, Exception innerException) : base(message, innerException) { }
}
