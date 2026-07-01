namespace GHB.DP2.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when data validation fails during Excel export operations.
/// </summary>
public class ExcelDataValidationException : ExcelExportException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelDataValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ExcelDataValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelDataValidationException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ExcelDataValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelDataValidationException"/> class with detailed validation information.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="value">The value that failed validation.</param>
    /// <param name="rowIndex">The row index where the validation error occurred.</param>
    public ExcelDataValidationException(string message, string propertyName, object? value, int? rowIndex = null)
        : base(message)
    {
        this.PropertyName = propertyName;
        this.Value = value;
        this.RowIndex = rowIndex;
    }

    /// <summary>
    /// Gets the name of the property that failed validation.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// Gets the value that failed validation.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the row index where the validation error occurred.
    /// </summary>
    public int? RowIndex { get; }
}