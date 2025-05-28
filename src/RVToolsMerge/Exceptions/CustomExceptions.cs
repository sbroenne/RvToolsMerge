//-----------------------------------------------------------------------
// <copyright file="CustomExceptions.cs" company="Stefan Broenner">
//     Copyright © Stefan Broenner 2025
//     Created by Stefan Broenner (github.com/sbroenne) and contributors
//     Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

namespace RVToolsMerge.Exceptions;

/// <summary>
/// Exception thrown when a file is invalid.
/// </summary>
[Serializable]
public class InvalidFileException : FileValidationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFileException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidFileException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFileException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InvalidFileException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when no valid files are found after validation.
/// </summary>
[Serializable]
public class NoValidFilesException : FileValidationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoValidFilesException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public NoValidFilesException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NoValidFilesException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public NoValidFilesException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when no valid sheets are found after validation.
/// </summary>
[Serializable]
public class NoValidSheetsException : FileValidationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoValidSheetsException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public NoValidSheetsException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NoValidSheetsException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public NoValidSheetsException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a required sheet is missing.
/// </summary>
[Serializable]
public class MissingRequiredSheetException : FileValidationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingRequiredSheetException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public MissingRequiredSheetException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MissingRequiredSheetException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public MissingRequiredSheetException(string message, Exception innerException) : base(message, innerException) { }
}