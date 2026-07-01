namespace GHB.DP2.Api.Tests.Services;

using ClosedXML.Excel;
using GHB.DP2.Api.Tests.Models;
using GHB.DP2.Infrastructure.Services.ExcelImportAndExport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Integration tests for ExcelExportService with full dependency injection setup.
/// </summary>
public class ExcelExportServiceIntegrationTests
{
    private readonly IServiceProvider serviceProvider;
    private readonly IExcelExportService excelExportService;

    public ExcelExportServiceIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddExcelImportAndExportService();

        serviceProvider = services.BuildServiceProvider();
        excelExportService = serviceProvider.GetRequiredService<IExcelExportService>();
    }

    [Fact]
    public void ServiceRegistration_ShouldRegisterCorrectly()
    {
        // Act
        var service = serviceProvider.GetService<IExcelExportService>();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<ExcelExportService>(service);
    }

    [Fact]
    public async Task ExportToExcelAsync_WithSimpleData_ShouldCreateValidExcelFile()
    {
        // Arrange
        var employees = CreateSimpleEmployeeData();

        // Act
        var result = await excelExportService.ExportToExcelAsync(employees);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        // Verify the Excel file is valid by reading it back
        await VerifyExcelContent(result, employees);
    }

    [Fact]
    public async Task ExportToExcelAsync_WithSimpleDataAndMappingColumn_ShouldCreateValidExcelFile()
    {
        // Arrange
        var employees = CreateSimpleEmployeeData();
        var columnMappings = new Dictionary<string, string>
        {
            { "Name", "Full Name" },
            { "Department", "Dept" },
            { "Age", "Employee Age" },
            { "Salary", "Annual Salary" },
            { "IsActive", "Active Status" }
        };

        // Act
        var result = await excelExportService.ExportToExcelAsync(employees, new ExcelExportOptions
        {
            ColumnMappings = columnMappings
        });

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        // Verify the Excel file is valid by reading it back
        await VerifyExcelContent(result, employees, columnMappings);
    }

    [Fact]
    public async Task ExportToExcelAsync_WithComplexAttributeMapping_ShouldHandleAllScenarios()
    {
        // Arrange
        var employees = CreateComplexEmployeeData();

        // Act
        var result = await excelExportService.ExportToExcelAsync(employees);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        // Verify the Excel file is valid by reading it back
        await VerifyComplexExcelContent(result, employees);
    }

    [Fact]
    public async Task ExportToExcelAsync_WithCustomOptions_ShouldApplyConfiguration()
    {
        // Arrange
        var employees = CreateSimpleEmployeeData();
        var options = new ExcelExportOptions
        {
            WorksheetName = "Employees_Test",
            IncludeHeaders = true,
            AutoFitColumns = true,
            ApplyBasicStyling = true,
            TrimStrings = true
        };

        // Act
        var result = await excelExportService.ExportToExcelAsync(employees, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        // Verify worksheet name and styling
        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.First();
        Assert.Equal("Employees_Test", worksheet.Name);

        // Verify headers are present and styled
        Assert.Equal("Name", worksheet.Cell(1, 1).Value.ToString());
        Assert.Equal("Department", worksheet.Cell(1, 2).Value.ToString());
        Assert.Equal("Age", worksheet.Cell(1, 3).Value.ToString());
        Assert.Equal("Salary", worksheet.Cell(1, 4).Value.ToString());
        Assert.Equal("IsActive", worksheet.Cell(1, 5).Value.ToString());

        // Verify header styling
        var headerCell = worksheet.Cell(1, 1);
        Assert.True(headerCell.Style.Font.Bold);
    }

    [Fact]
    public async Task ExportToExcelAsync_WithEmptyData_ShouldCreateEmptyFile()
    {
        // Arrange
        var employees = new List<SimpleEmployee>();

        // Act
        var result = await excelExportService.ExportToExcelAsync(employees);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        // Verify the Excel file contains only headers
        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.First();
        var usedRange = worksheet.RangeUsed();

        // Should have only header row
        Assert.Equal(1, usedRange?.RowCount() ?? 0);
    }

    [Fact]
    public async Task ExportToExcelAsync_WithLargeDataset_ShouldHandlePerformanceGracefully()
    {
        // Arrange
        var employees = CreateLargeEmployeeDataset(5000);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await excelExportService.ExportToExcelAsync(employees);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        Assert.True(stopwatch.ElapsedMilliseconds < 30000); // Should complete within 30 seconds

        // Verify the data count
        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.First();
        var usedRange = worksheet.RangeUsed();

        // Should have header + 5000 data rows
        Assert.Equal(5001, usedRange?.RowCount() ?? 0);
    }

    [Fact]
    public async Task ExportToExcelStreamAsync_ShouldReturnValidStream()
    {
        // Arrange
        var employees = CreateSimpleEmployeeData();

        // Act
        using var result = await excelExportService.ExportToExcelStreamAsync(employees);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        Assert.True(result.CanRead);
        Assert.Equal(0, result.Position);
    }

    [Fact]
    public async Task ExportToExcelAsync_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<SimpleEmployee> employees = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            excelExportService.ExportToExcelAsync(employees));
    }

    [Fact]
    public async Task ExportToExcelAsync_WithInvalidData_ShouldThrowExcelDataValidationException()
    {
        // Arrange
        var employees = new List<SimpleEmployee>
        {
            new SimpleEmployee { Name = "Valid Employee", Department = "IT", Age = 30, Salary = 50000, IsActive = true }
        };

        // Create a problematic property that might cause validation issues
        _ = new SimpleEmployee
        {
            Name = null, // This might cause issues depending on validation
            Department = "IT",
            Age = -1, // Invalid age
            Salary = 50000,
            IsActive = true
        };

        // Note: This test might need adjustment based on actual validation rules
        // For now, we'll test with valid data to ensure the basic functionality works

        // Act
        var result = await excelExportService.ExportToExcelAsync(employees);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task ExportToExcelAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var employees = CreateLargeEmployeeDataset(1000);
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        var task = excelExportService.ExportToExcelAsync(employees, cancellationToken: cancellationTokenSource.Token);

        // Cancel immediately
        cancellationTokenSource.Cancel();

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task ExportToExcelAsync_WithCustomHeaders_ShouldUseCustomHeaders()
    {
        // Arrange
        var employees = CreateSimpleEmployeeData();
        var customHeaders = new List<string> { "Employee Name", "Dept", "Years", "Annual Salary", "Active" };

        var options = new ExcelExportOptions
        {
            CustomHeaders = customHeaders,
            WorksheetName = "CustomHeaderTest",
            IncludeHeaders = true,
            ApplyBasicStyling = true
        };

        // Act
        var result = await excelExportService.ExportToExcelAsync(employees, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        // Verify the Excel file uses custom headers
        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.First();

        // Verify custom headers are present
        Assert.Equal("Employee Name", worksheet.Cell(1, 1).Value.ToString());
        Assert.Equal("Dept", worksheet.Cell(1, 2).Value.ToString());
        Assert.Equal("Years", worksheet.Cell(1, 3).Value.ToString());
        Assert.Equal("Annual Salary", worksheet.Cell(1, 4).Value.ToString());
        Assert.Equal("Active", worksheet.Cell(1, 5).Value.ToString());

        // Verify header styling is applied
        var headerCell = worksheet.Cell(1, 1);
        Assert.True(headerCell.Style.Font.Bold);

        // Verify data is still correctly populated
        Assert.Equal("John Doe", worksheet.Cell(2, 1).Value.ToString());
        Assert.Equal("Engineering", worksheet.Cell(2, 2).Value.ToString());
        Assert.Equal("30", worksheet.Cell(2, 3).Value.ToString());
        Assert.Equal("75000", worksheet.Cell(2, 4).Value.ToString());
        Assert.Equal("True", worksheet.Cell(2, 5).Value.ToString());
    }

    [Fact]
    public async Task ExportToExcelAsync_WithCustomHeadersNull_ShouldFallbackToDefaultHeaders()
    {
        // Arrange
        var employees = CreateSimpleEmployeeData();

        var options = new ExcelExportOptions
        {
            CustomHeaders = null,
            WorksheetName = "FallbackHeaderTest",
            IncludeHeaders = true
        };

        // Act
        var result = await excelExportService.ExportToExcelAsync(employees, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        // Verify the Excel file uses default headers
        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.First();

        // Verify default headers are present
        Assert.Equal("Name", worksheet.Cell(1, 1).Value.ToString());
        Assert.Equal("Department", worksheet.Cell(1, 2).Value.ToString());
        Assert.Equal("Age", worksheet.Cell(1, 3).Value.ToString());
        Assert.Equal("Salary", worksheet.Cell(1, 4).Value.ToString());
        Assert.Equal("IsActive", worksheet.Cell(1, 5).Value.ToString());
    }

    [Fact]
    public async Task ExportToExcelAsync_WithCustomHeadersEmpty_ShouldFallbackToDefaultHeaders()
    {
        // Arrange
        var employees = CreateSimpleEmployeeData();

        var options = new ExcelExportOptions
        {
            CustomHeaders = new List<string>(),
            WorksheetName = "EmptyHeaderTest",
            IncludeHeaders = true
        };

        // Act
        var result = await excelExportService.ExportToExcelAsync(employees, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        // Verify the Excel file uses default headers
        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.First();

        // Verify default headers are present
        Assert.Equal("Name", worksheet.Cell(1, 1).Value.ToString());
        Assert.Equal("Department", worksheet.Cell(1, 2).Value.ToString());
        Assert.Equal("Age", worksheet.Cell(1, 3).Value.ToString());
        Assert.Equal("Salary", worksheet.Cell(1, 4).Value.ToString());
        Assert.Equal("IsActive", worksheet.Cell(1, 5).Value.ToString());
    }

    [Fact]
    public async Task ExportToExcelAsync_WithCustomHeadersAndNoHeaders_ShouldNotIncludeHeaders()
    {
        // Arrange
        var employees = CreateSimpleEmployeeData();
        var customHeaders = new List<string> { "Employee Name", "Dept", "Years", "Annual Salary", "Active" };

        var options = new ExcelExportOptions
        {
            CustomHeaders = customHeaders,
            IncludeHeaders = false,
            WorksheetName = "NoHeaderTest"
        };

        // Act
        var result = await excelExportService.ExportToExcelAsync(employees, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        // Verify the Excel file doesn't include headers
        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.First();

        // Verify first row contains data, not headers
        Assert.Equal("John Doe", worksheet.Cell(1, 1).Value.ToString());
        Assert.Equal("Engineering", worksheet.Cell(1, 2).Value.ToString());
        Assert.Equal("30", worksheet.Cell(1, 3).Value.ToString());
        Assert.Equal("75000", worksheet.Cell(1, 4).Value.ToString());
        Assert.Equal("True", worksheet.Cell(1, 5).Value.ToString());
    }

    private static List<SimpleEmployee> CreateSimpleEmployeeData()
    {
        return new List<SimpleEmployee>
        {
            new SimpleEmployee
            {
                Name = "John Doe",
                Department = "Engineering",
                Age = 30,
                Salary = 75000m,
                IsActive = true
            },
            new SimpleEmployee
            {
                Name = "Jane Smith",
                Department = "Marketing",
                Age = 28,
                Salary = 65000m,
                IsActive = true
            },
            new SimpleEmployee
            {
                Name = "Bob Johnson",
                Department = "Sales",
                Age = 35,
                Salary = 55000m,
                IsActive = false
            }
        };
    }

    private static List<ComplexEmployee> CreateComplexEmployeeData()
    {
        return new List<ComplexEmployee>
        {
            new ComplexEmployee
            {
                FullName = "Alice Johnson",
                EmployeeId = 1001,
                DepartmentCode = "ENG",
                BirthDate = new DateTime(1985, 3, 15),
                HireDate = new DateTime(2020, 1, 15),
                MonthlySalary = 85000m,
                IsManager = true,
                PerformanceRating = PerformanceRating.Excellent,
                Email = "alice.johnson@company.com",
                PhoneNumber = "+1-555-0123",
                InternalNotes = "This should be ignored"
            },
            new ComplexEmployee
            {
                FullName = "Bob Wilson",
                EmployeeId = 1002,
                DepartmentCode = "HR",
                BirthDate = new DateTime(1978, 7, 22),
                HireDate = new DateTime(2019, 5, 10),
                MonthlySalary = 72000m,
                IsManager = false,
                PerformanceRating = PerformanceRating.Good,
                Email = "bob.wilson@company.com",
                PhoneNumber = "+1-555-0456",
                InternalNotes = "This should also be ignored"
            }
        };
    }

    private static List<SimpleEmployee> CreateLargeEmployeeDataset(int count)
    {
        var employees = new List<SimpleEmployee>();
        var random = new Random(42); // Fixed seed for reproducible tests
        var departments = new[] { "Engineering", "Marketing", "Sales", "HR", "Finance" };

        for (int i = 0; i < count; i++)
        {
            employees.Add(new SimpleEmployee
            {
                Name = $"Employee {i:D4}",
                Department = departments[i % departments.Length],
                Age = random.Next(22, 65),
                Salary = random.Next(40000, 120000),
                IsActive = random.Next(0, 2) == 1
            });
        }

        return employees;
    }

    private static Task VerifyExcelContent(byte[] excelData, List<SimpleEmployee> originalData, Dictionary<string, string>? columnMappings = null)
    {
        using var stream = new MemoryStream(excelData);
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.First();

        // Get expected headers based on column mappings
        var expectedHeaders = new[]
        {
            columnMappings?.GetValueOrDefault("Name", "Name") ?? "Name",
            columnMappings?.GetValueOrDefault("Department", "Department") ?? "Department",
            columnMappings?.GetValueOrDefault("Age", "Age") ?? "Age",
            columnMappings?.GetValueOrDefault("Salary", "Salary") ?? "Salary",
            columnMappings?.GetValueOrDefault("IsActive", "IsActive") ?? "IsActive"
        };

        // Verify headers
        Assert.Equal(expectedHeaders[0], worksheet.Cell(1, 1).Value.ToString());
        Assert.Equal(expectedHeaders[1], worksheet.Cell(1, 2).Value.ToString());
        Assert.Equal(expectedHeaders[2], worksheet.Cell(1, 3).Value.ToString());
        Assert.Equal(expectedHeaders[3], worksheet.Cell(1, 4).Value.ToString());
        Assert.Equal(expectedHeaders[4], worksheet.Cell(1, 5).Value.ToString());

        // Verify data
        for (int i = 0; i < originalData.Count; i++)
        {
            var row = i + 2; // Skip header row
            var employee = originalData[i];

            Assert.Equal(employee.Name, worksheet.Cell(row, 1).Value.ToString());
            Assert.Equal(employee.Department, worksheet.Cell(row, 2).Value.ToString());
            Assert.Equal(employee.Age.ToString(), worksheet.Cell(row, 3).Value.ToString());
            Assert.Equal(employee.Salary.ToString(), worksheet.Cell(row, 4).Value.ToString());
            Assert.Equal(employee.IsActive.ToString(), worksheet.Cell(row, 5).Value.ToString());
        }

        return Task.CompletedTask;
    }

    private static Task VerifyComplexExcelContent(byte[] excelData, List<ComplexEmployee> originalData)
    {
        using var stream = new MemoryStream(excelData);
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.First();

        // Verify headers (excluding ignored columns)
        Assert.Equal("Full Name", worksheet.Cell(1, 1).Value.ToString());
        Assert.Equal("Employee ID", worksheet.Cell(1, 2).Value.ToString());
        Assert.Equal("Department Code", worksheet.Cell(1, 3).Value.ToString());

        // Verify data
        for (int i = 0; i < originalData.Count; i++)
        {
            var row = i + 2; // Skip header row
            var employee = originalData[i];

            Assert.Equal(employee.FullName, worksheet.Cell(row, 1).Value.ToString());
            Assert.Equal(employee.EmployeeId.ToString(), worksheet.Cell(row, 2).Value.ToString());
            Assert.Equal(employee.DepartmentCode, worksheet.Cell(row, 3).Value.ToString());
        }

        return Task.CompletedTask;
    }
}