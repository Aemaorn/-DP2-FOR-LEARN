namespace GHB.DP2.Infrastructure.Services.ExcelImportAndExport;

/// <summary>
/// Attribute used to specify Excel column mapping for properties during import operations.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ExcelColumnAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelColumnAttribute"/> class with a column name.
    /// </summary>
    /// <param name="columnName">The name of the Excel column to map to this property.</param>
    /// <exception cref="ArgumentException">Thrown when the columnName is null or empty.</exception>
    public ExcelColumnAttribute(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));
        }

        this.ColumnName = columnName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelColumnAttribute"/> class with a column index.
    /// </summary>
    /// <param name="columnIndex">The zero-based index of the Excel column to map to this property.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the columnIndex is negative.</exception>
    public ExcelColumnAttribute(int columnIndex)
    {
        if (columnIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex), "Column index cannot be negative.");
        }

        this.ColumnIndex = columnIndex;
        this.ColumnName = string.Empty; // Set to empty when using index
    }

    public ExcelColumnAttribute()
    {
    }

    /// <summary>
    /// Gets the name of the Excel column to map to this property.
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// Gets or sets the zero-based index of the Excel column to map to this property.
    /// When set to -1 (default), the column name will be used for mapping.
    /// </summary>
    public int ColumnIndex { get; set; } = -1;

    /// <summary>
    /// Gets or sets a value indicating whether this property is required to have a value.
    /// When true, an exception will be thrown if the Excel cell is empty or null.
    /// Default is false.
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to ignore this property during Excel import.
    /// When true, this property will not be mapped from Excel data.
    /// Default is false.
    /// </summary>
    public bool Ignore { get; set; } = false;

    /// <summary>
    /// Gets or sets the default value to use when the Excel cell is empty or null.
    /// This is only applied when IsRequired is false.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the date format string to use when parsing DateTime values from Excel.
    /// If not specified, standard date parsing will be used.
    /// </summary>
    public string? DateFormat { get; set; }

    /// <summary>
    /// Gets or sets the number format string to use when parsing numeric values from Excel.
    /// If not specified, standard numeric parsing will be used.
    /// </summary>
    public string? NumberFormat { get; set; }

    /// <summary>
    /// Gets a value indicating whether this attribute uses column index for mapping.
    /// Returns true if ColumnIndex is >= 0, false if using ColumnName.
    /// </summary>
    public bool UsesColumnIndex => this.ColumnIndex >= 0;

    /// <summary>
    /// Gets a value indicating whether this attribute uses the column name for mapping.
    /// Returns true if ColumnName is not empty and ColumnIndex is -1.
    /// </summary>
    public bool UsesColumnName => !string.IsNullOrEmpty(this.ColumnName) && this.ColumnIndex == -1;
}