# Excel Import/Export Services

The Excel Import and Export Services provide a generic, reusable way to import Excel files and export data to Excel files, with seamless mapping between Excel columns and strongly-typed C# objects in the GHB DP2 system.

## Features

### Import Service
- **Generic Type Mapping**: Import Excel data to any C# class with parameterless constructor
- **Stream-Based Processing**: Memory-efficient processing for large files
- **Attribute-Based Mapping**: Use `ExcelColumnAttribute` for flexible column mapping
- **Multiple Data Types**: Support for string, numeric, DateTime, bool, enum, and nullable types
- **Error Handling**: Comprehensive error handling with detailed exception information
- **Batch Processing**: Configurable batch sizes for optimal memory usage
- **Security**: File size validation and input sanitization

### Export Service
- **Generic Type Export**: Export any C# object collection to Excel format
- **Template Support**: Use existing Excel templates with pre-defined formatting
- **Byte Array & Stream Output**: Choose between byte array or stream output formats
- **Attribute-Based Mapping**: Reuse the same `ExcelColumnAttribute` for consistent mapping
- **Performance Optimized**: Batch processing and memory-efficient export for large datasets
- **Styling Support**: Basic styling, auto-fit columns, and custom formatting options
- **Cancellation Support**: Async operations with cancellation token support

## Quick Start

### 1. Dependency Injection Setup

The services are already registered in the DI container:

```csharp
// In Program.cs (already configured)
services.AddExcelImportService();
services.AddExcelExportService();
```

### 2. Basic Usage

```csharp
public class Employee
{
    public string? Name { get; set; }
    public string? Department { get; set; }
    public int Age { get; set; }
    public decimal Salary { get; set; }
    public bool IsActive { get; set; }
}

// In your controller or service
[HttpPost("import-employees")]
public async Task<IActionResult> ImportEmployees(IFormFile file)
{
    using var stream = file.OpenReadStream();
    var employees = await excelImportService.ImportExcelAsync<Employee>(stream);
    
    // Process imported employees
    return Ok(employees);
}
```

### 3. Advanced Usage with Attributes

```csharp
public class DetailedEmployee
{
    [ExcelColumn("Full Name", IsRequired = true)]
    public string? FullName { get; set; }
    
    [ExcelColumn("Employee ID", IsRequired = true)]
    public int EmployeeId { get; set; }
    
    [ExcelColumn("Birth Date", DateFormat = "yyyy-MM-dd")]
    public DateTime? BirthDate { get; set; }
    
    [ExcelColumn("Hire Date", DateFormat = "dd/MM/yyyy", IsRequired = true)]
    public DateTime HireDate { get; set; }
    
    [ExcelColumn("Monthly Salary", NumberFormat = "C")]
    public decimal MonthlySalary { get; set; }
    
    [ExcelColumn("Is Manager", DefaultValue = false)]
    public bool IsManager { get; set; }
    
    [ExcelColumn("Performance", DefaultValue = PerformanceRating.Good)]
    public PerformanceRating PerformanceRating { get; set; }
    
    [ExcelColumn(Ignore = true)]
    public string? InternalNotes { get; set; } // Will not be mapped
}

public enum PerformanceRating
{
    Poor = 1,
    Fair = 2,
    Good = 3,
    Excellent = 4
}
```

### 4. Custom Options

```csharp
var options = new ExcelImportOptions
{
    SkipHeader = true,
    WorksheetIndex = 0,
    BatchSize = 1000,
    MaxFileSizeBytes = 10 * 1024 * 1024, // 10MB
    ContinueOnMappingError = false,
    TrimStrings = true,
    ColumnMappings = new Dictionary<string, string>
    {
        { "Name", "Employee Name" },
        { "Department", "Dept" }
    }
};

var result = await excelImportService.ImportExcelAsync<Employee>(stream, options);
```

## ExcelColumnAttribute Properties

| Property | Type | Description |
|----------|------|-------------|
| `ColumnName` | string | Excel column name to map to this property |
| `ColumnIndex` | int | Zero-based column index (alternative to ColumnName) |
| `IsRequired` | bool | Whether this field is required (throws exception if empty) |
| `Ignore` | bool | Whether to ignore this property during import |
| `DefaultValue` | object | Default value when Excel cell is empty |
| `DateFormat` | string | Custom date format for DateTime parsing |
| `NumberFormat` | string | Custom number format for numeric parsing |

## ExcelImportOptions Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SkipHeader` | bool | true | Skip the first row (header row) |
| `WorksheetIndex` | int | 0 | Zero-based worksheet index to read |
| `BatchSize` | int | 1000 | Number of rows to process per batch |
| `MaxFileSizeBytes` | long | 50MB | Maximum allowed file size |
| `ContinueOnMappingError` | bool | false | Continue processing when mapping errors occur |
| `TrimStrings` | bool | true | Trim whitespace from string values |
| `ColumnMappings` | Dictionary | empty | Custom property-to-column mappings |
| `CultureInfo` | CultureInfo | current | Culture for parsing dates and numbers |

## Error Handling

The service throws specific exceptions for different error scenarios:

### InvalidFileFormatException
```csharp
try 
{
    var result = await excelImportService.ImportExcelAsync<Employee>(stream);
}
catch (InvalidFileFormatException ex)
{
    // Handle invalid or corrupted Excel files
    logger.LogError("Invalid Excel file: {Message}", ex.Message);
}
```

### EmptyFileException
```csharp
catch (EmptyFileException ex)
{
    // Handle files with no data
    return BadRequest("Excel file contains no data");
}
```

### WorksheetNotFoundException
```csharp
catch (WorksheetNotFoundException ex)
{
    // Handle missing worksheets
    return BadRequest($"Worksheet {ex.WorksheetIndex} not found");
}
```

### DataMappingException
```csharp
catch (DataMappingException ex)
{
    // Handle data conversion errors
    logger.LogError("Mapping error at row {Row}, column {Column}: {Message}", 
        ex.RowNumber, ex.ColumnName, ex.Message);
}
```

## Performance Considerations

### Large Files
```csharp
var options = new ExcelImportOptions
{
    BatchSize = 500, // Smaller batches for very large files
    MaxFileSizeBytes = 100 * 1024 * 1024 // Increase size limit if needed
};
```

### Memory Usage
- The service processes files in batches to manage memory usage
- Stream-based processing avoids loading entire files into memory
- Adjust `BatchSize` based on available memory and row complexity

### Cancellation Support
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

try 
{
    var result = await excelImportService.ImportExcelAsync<Employee>(
        stream, cancellationToken: cts.Token);
}
catch (OperationCanceledException)
{
    // Handle timeout or user cancellation
}
```

## Best Practices

1. **Always use streams**: Pass file streams directly to avoid memory issues
2. **Validate input**: Check file extensions and sizes before processing
3. **Handle exceptions**: Implement proper error handling for all exception types
4. **Use attributes**: Leverage `ExcelColumnAttribute` for complex mappings
5. **Test with real data**: Test with actual Excel files from your users
6. **Monitor performance**: Track processing times for large files
7. **Log errors**: Log detailed error information for troubleshooting

## Examples

### Complete Controller Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class EmployeeController : ControllerBase
{
    private readonly IExcelImportService excelImportService;
    private readonly ILogger<EmployeeController> logger;

    public EmployeeController(
        IExcelImportService excelImportService,
        ILogger<EmployeeController> logger)
    {
        this.excelImportService = excelImportService;
        this.logger = logger;
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportEmployees(
        IFormFile file,
        [FromQuery] bool skipHeader = true,
        [FromQuery] bool continueOnError = false)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
        {
            return BadRequest("Invalid file format. Only Excel files are supported.");
        }

        try
        {
            var options = new ExcelImportOptions
            {
                SkipHeader = skipHeader,
                ContinueOnMappingError = continueOnError,
                MaxFileSizeBytes = 10 * 1024 * 1024 // 10MB limit
            };

            using var stream = file.OpenReadStream();
            var employees = await excelImportService.ImportExcelAsync<Employee>(stream, options);

            logger.LogInformation("Successfully imported {Count} employees from {FileName}", 
                employees.Count, file.FileName);

            return Ok(new { 
                Count = employees.Count, 
                Data = employees 
            });
        }
        catch (InvalidFileFormatException ex)
        {
            logger.LogError(ex, "Invalid file format for {FileName}", file.FileName);
            return BadRequest($"Invalid file format: {ex.Message}");
        }
        catch (EmptyFileException ex)
        {
            logger.LogWarning("Empty file uploaded: {FileName}", file.FileName);
            return BadRequest("The Excel file contains no data");
        }
        catch (DataMappingException ex)
        {
            logger.LogError(ex, "Data mapping error in {FileName} at row {Row}", 
                file.FileName, ex.RowNumber);
            return BadRequest($"Data error at row {ex.RowNumber}: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error importing {FileName}", file.FileName);
            return StatusCode(500, "An unexpected error occurred while processing the file");
        }
    }
}
```

---

# Excel Export Service

The Excel Export Service provides a powerful way to export C# object collections to Excel files with full control over formatting, styling, and data mapping.

## Quick Start

### 1. Basic Export Usage

```csharp
public class Employee
{
    public string? Name { get; set; }
    public string? Department { get; set; }
    public int Age { get; set; }
    public decimal Salary { get; set; }
    public bool IsActive { get; set; }
}

// In your controller or service
[HttpGet("export-employees")]
public async Task<IActionResult> ExportEmployees()
{
    var employees = await GetEmployeesFromDatabase();
    var excelData = await excelExportService.ExportToExcelAsync(employees);
    
    return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "employees.xlsx");
}
```

### 2. Export with Attribute Mapping

```csharp
public class DetailedEmployee
{
    [ExcelColumn("Full Name", IsRequired = true)]
    public string? FullName { get; set; }
    
    [ExcelColumn("Employee ID", IsRequired = true)]
    public int EmployeeId { get; set; }
    
    [ExcelColumn("Birth Date", DateFormat = "yyyy-MM-dd")]
    public DateTime? BirthDate { get; set; }
    
    [ExcelColumn("Hire Date", DateFormat = "dd/MM/yyyy")]
    public DateTime HireDate { get; set; }
    
    [ExcelColumn("Monthly Salary", NumberFormat = "C")]
    public decimal MonthlySalary { get; set; }
    
    [ExcelColumn("Is Manager", DefaultValue = false)]
    public bool IsManager { get; set; }
    
    [ExcelColumn(Ignore = true)]
    public string? InternalNotes { get; set; } // Will not be exported
}

// Export will use the column names and formatting specified in attributes
var excelData = await excelExportService.ExportToExcelAsync(employees);
```

### 3. Export with Custom Options

```csharp
var options = new ExcelExportOptions
{
    WorksheetName = "Employee Report",
    IncludeHeaders = true,
    AutoFitColumns = true,
    ApplyBasicStyling = true,
    BatchSize = 1000,
    MaxFileSizeBytes = 50 * 1024 * 1024, // 50MB
    TrimStrings = true,
    ColumnMappings = new Dictionary<string, string>
    {
        { "Name", "Employee Name" },
        { "Department", "Department Name" }
    }
};

var excelData = await excelExportService.ExportToExcelAsync(employees, options);
```

### 4. Template-Based Export

```csharp
// Use an existing Excel template with pre-defined formatting
var templatePath = Path.Combine("Templates", "employee-report-template.xlsx");
var excelData = await excelExportService.ExportToExcelAsync(employees, templatePath, options);
```

### 5. Stream-Based Export

```csharp
// For large datasets, use stream-based export to reduce memory usage
using var stream = await excelExportService.ExportToExcelStreamAsync(employees, options);

// Stream the result directly to the client
return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "employees.xlsx");
```

## ExcelExportOptions Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `WorksheetName` | string | "Sheet1" | Name of the worksheet to create |
| `IncludeHeaders` | bool | true | Whether to include header row |
| `BatchSize` | int | 1000 | Number of rows to process per batch |
| `MaxFileSizeBytes` | long | 50MB | Maximum allowed file size |
| `CultureInfo` | CultureInfo | current | Culture for formatting dates and numbers |
| `ColumnMappings` | Dictionary | empty | Custom property-to-column mappings |
| `EnableEventTracking` | bool | false | Enable ClosedXML event tracking (affects performance) |
| `AutoFitColumns` | bool | true | Auto-fit column widths to content |
| `ApplyBasicStyling` | bool | true | Apply basic styling (headers, borders) |
| `TrimStrings` | bool | true | Trim whitespace from string values |

## Export Service Methods

### ExportToExcelAsync (Byte Array)
```csharp
Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, ExcelExportOptions? options = null, CancellationToken cancellationToken = default);
Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string templatePath, ExcelExportOptions? options = null, CancellationToken cancellationToken = default);
```

### ExportToExcelStreamAsync (Stream)
```csharp
Task<Stream> ExportToExcelStreamAsync<T>(IEnumerable<T> data, ExcelExportOptions? options = null, CancellationToken cancellationToken = default);
Task<Stream> ExportToExcelStreamAsync<T>(IEnumerable<T> data, string templatePath, ExcelExportOptions? options = null, CancellationToken cancellationToken = default);
```

## Error Handling

The export service throws specific exceptions for different error scenarios:

### ExcelExportException
```csharp
try 
{
    var result = await excelExportService.ExportToExcelAsync(data);
}
catch (ExcelExportException ex)
{
    // Handle general export errors
    logger.LogError("Export failed: {Message}", ex.Message);
}
```

### ExcelTemplateMissingException
```csharp
catch (ExcelTemplateMissingException ex)
{
    // Handle missing template files
    logger.LogError("Template not found: {TemplatePath}", ex.TemplatePath);
}
```

### ExcelDataValidationException
```csharp
catch (ExcelDataValidationException ex)
{
    // Handle data validation errors
    logger.LogError("Data validation error at row {Row}, property {Property}: {Message}", 
        ex.RowIndex, ex.PropertyName, ex.Message);
}
```

### ExcelFileSizeExceededException
```csharp
catch (ExcelFileSizeExceededException ex)
{
    // Handle file size limit exceeded
    logger.LogError("File size {ActualSize} exceeds maximum {MaxSize}", 
        ex.ActualSize, ex.MaxSize);
}
```

## Performance Considerations

### Large Datasets
```csharp
var options = new ExcelExportOptions
{
    BatchSize = 500, // Smaller batches for very large datasets
    EnableEventTracking = false, // Disable for better performance
    ApplyBasicStyling = false, // Disable styling for performance
    AutoFitColumns = false // Disable auto-fit for performance
};
```

### Memory Management
- The service processes data in batches to manage memory usage
- Use `ExportToExcelStreamAsync` for very large exports
- Monitor memory usage and adjust `BatchSize` accordingly
- Consider chunking very large datasets into multiple files

### Cancellation Support
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

try 
{
    var result = await excelExportService.ExportToExcelAsync(
        data, cancellationToken: cts.Token);
}
catch (OperationCanceledException)
{
    // Handle timeout or user cancellation
}
```

## Complete Controller Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class EmployeeController : ControllerBase
{
    private readonly IExcelExportService excelExportService;
    private readonly ILogger<EmployeeController> logger;

    public EmployeeController(
        IExcelExportService excelExportService,
        ILogger<EmployeeController> logger)
    {
        this.excelExportService = excelExportService;
        this.logger = logger;
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportEmployees(
        [FromQuery] string format = "xlsx",
        [FromQuery] bool includeHeaders = true,
        [FromQuery] string worksheetName = "Employees")
    {
        try
        {
            var employees = await GetEmployeesFromDatabase();
            
            var options = new ExcelExportOptions
            {
                WorksheetName = worksheetName,
                IncludeHeaders = includeHeaders,
                AutoFitColumns = true,
                ApplyBasicStyling = true,
                MaxFileSizeBytes = 50 * 1024 * 1024 // 50MB limit
            };

            var excelData = await excelExportService.ExportToExcelAsync(employees, options);

            logger.LogInformation("Successfully exported {Count} employees to Excel", employees.Count);

            return File(excelData, 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                $"employees_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }
        catch (ExcelExportException ex)
        {
            logger.LogError(ex, "Failed to export employees");
            return StatusCode(500, $"Export failed: {ex.Message}");
        }
        catch (ExcelFileSizeExceededException ex)
        {
            logger.LogError(ex, "Export file size exceeded limit");
            return BadRequest($"Export file too large: {ex.ActualSize} bytes exceeds limit of {ex.MaxSize} bytes");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during export");
            return StatusCode(500, "An unexpected error occurred during export");
        }
    }

    [HttpGet("export-with-template")]
    public async Task<IActionResult> ExportEmployeesWithTemplate(
        [FromQuery] string templateName = "employee-report-template.xlsx")
    {
        try
        {
            var employees = await GetEmployeesFromDatabase();
            var templatePath = Path.Combine("Templates", templateName);
            
            var options = new ExcelExportOptions
            {
                AutoFitColumns = true,
                ApplyBasicStyling = false // Template already has styling
            };

            var excelData = await excelExportService.ExportToExcelAsync(
                employees, templatePath, options);

            logger.LogInformation("Successfully exported {Count} employees using template {Template}", 
                employees.Count, templateName);

            return File(excelData, 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                $"employee_report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }
        catch (ExcelTemplateMissingException ex)
        {
            logger.LogError(ex, "Template not found: {TemplatePath}", ex.TemplatePath);
            return NotFound($"Template not found: {ex.TemplatePath}");
        }
        catch (ExcelExportException ex)
        {
            logger.LogError(ex, "Failed to export employees with template");
            return StatusCode(500, $"Export failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during template export");
            return StatusCode(500, "An unexpected error occurred during export");
        }
    }

    private async Task<List<Employee>> GetEmployeesFromDatabase()
    {
        // Your database access logic here
        return new List<Employee>();
    }
}
```

## Best Practices

1. **Choose the right output format**: Use byte arrays for small files, streams for large files
2. **Optimize for performance**: Disable styling and auto-fit for large datasets
3. **Handle exceptions**: Implement proper error handling for all exception types
4. **Monitor file sizes**: Set appropriate size limits and handle exceeded limits gracefully
5. **Use templates**: Leverage Excel templates for consistent formatting and branding
6. **Test with real data**: Test with actual data volumes and complexity
7. **Log performance**: Track export times and memory usage for monitoring
8. **Support cancellation**: Always pass cancellation tokens for long-running operations

## Template Usage

### Creating Templates
1. Create an Excel file with your desired formatting, headers, and styling
2. Save the file in your Templates directory
3. Use the template path in the export method

### Template Requirements
- Templates must be valid Excel files (.xlsx format)
- Data will be populated starting from the first available row
- Existing formatting and styling will be preserved
- Headers in templates will be used if `IncludeHeaders` is false

### Template Example Structure
```
Templates/
├── employee-report-template.xlsx
├── financial-report-template.xlsx
└── monthly-summary-template.xlsx
```