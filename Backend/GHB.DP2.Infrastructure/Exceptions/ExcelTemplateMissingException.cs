namespace GHB.DP2.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when an Excel template file is not found at the specified path.
/// </summary>
public class ExcelTemplateMissingException : ExcelExportException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelTemplateMissingException"/> class.
    /// </summary>
    /// <param name="templatePath">The path to the template file that was not found.</param>
    public ExcelTemplateMissingException(string templatePath)
        : base($"Excel template not found at path: {templatePath}")
    {
        this.TemplatePath = templatePath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelTemplateMissingException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="templatePath">The path to the template file that was not found.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ExcelTemplateMissingException(string templatePath, Exception innerException)
        : base($"Excel template not found at path: {templatePath}", innerException)
    {
        this.TemplatePath = templatePath;
    }

    /// <summary>
    /// Gets the path to the template file that was not found.
    /// </summary>
    public string TemplatePath { get; }
}