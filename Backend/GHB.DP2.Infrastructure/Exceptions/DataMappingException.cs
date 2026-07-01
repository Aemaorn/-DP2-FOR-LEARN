namespace GHB.DP2.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when data cannot be mapped to the target type during Excel import.
/// </summary>
public class DataMappingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataMappingException"/> class.
    /// </summary>
    public DataMappingException()
        : base("Data cannot be mapped to the target type.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataMappingException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DataMappingException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataMappingException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DataMappingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets or sets the property name that failed to map.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the column name or index that failed to map.
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// Gets or sets the row number where the mapping failed.
    /// </summary>
    public int? RowNumber { get; set; }

    /// <summary>
    /// Gets or sets the cell value that failed to map.
    /// </summary>
    public object? CellValue { get; set; }

    /// <summary>
    /// Gets or sets the target type that the data was being mapped to.
    /// </summary>
    public Type? TargetType { get; set; }
}