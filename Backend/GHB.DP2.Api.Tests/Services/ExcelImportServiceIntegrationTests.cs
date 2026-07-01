
namespace GHB.DP2.Api.Tests.Services;

using GHB.DP2.Api.Tests.Models;
using GHB.DP2.Infrastructure.Services.ExcelImportAndExport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Integration tests for ExcelImportService with full dependency injection setup.
/// </summary>
public class ExcelImportServiceIntegrationTests
{
    private readonly IServiceProvider serviceProvider;
    private readonly IExcelImportService excelImportService;

    public ExcelImportServiceIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddExcelImportAndExportService();

        serviceProvider = services.BuildServiceProvider();
        excelImportService = serviceProvider.GetRequiredService<IExcelImportService>();
    }

    [Fact]
    public void ServiceRegistration_ShouldRegisterCorrectly()
    {
        // Act
        var service = serviceProvider.GetService<IExcelImportService>();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<ExcelImportService>(service);
    }

    [Fact]
    public async Task ImportExcelAsync_WithRealWorldData_ShouldProcessSuccessfully()
    {
        // Arrange
        var excelData = CreateRealisticEmployeeData();
        using var stream = new MemoryStream(excelData);

        // Act
        var result = await excelImportService.ImportExcelAsync<SimpleEmployee>(stream);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(3, result.Count);

        // Verify first employee
        var firstEmployee = result[0];
        Assert.Equal("John Doe", firstEmployee.Name);
        Assert.Equal("Engineering", firstEmployee.Department);
        Assert.Equal(30, firstEmployee.Age);
        Assert.Equal(75000m, firstEmployee.Salary);
        Assert.True(firstEmployee.IsActive);

        // Verify second employee
        var secondEmployee = result[1];
        Assert.Equal("Jane Smith", secondEmployee.Name);
        Assert.Equal("Marketing", secondEmployee.Department);
        Assert.Equal(28, secondEmployee.Age);
        Assert.Equal(65000m, secondEmployee.Salary);
        Assert.True(secondEmployee.IsActive);

        // Verify third employee
        var thirdEmployee = result[2];
        Assert.Equal("Bob Johnson", thirdEmployee.Name);
        Assert.Equal("Sales", thirdEmployee.Department);
        Assert.Equal(35, thirdEmployee.Age);
        Assert.Equal(55000m, thirdEmployee.Salary);
        Assert.False(thirdEmployee.IsActive);
    }

    [Fact]
    public async Task ImportExcelAsync_WithComplexAttributeMapping_ShouldHandleAllScenarios()
    {
        // Arrange
        var excelData = CreateComplexEmployeeDataWithAttributes();
        using var stream = new MemoryStream(excelData);

        // Act
        var result = await excelImportService.ImportExcelAsync<ComplexEmployee>(stream);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count);

        // Verify first employee
        var firstEmployee = result[0];
        Assert.Equal("Alice Johnson", firstEmployee.FullName);
        Assert.Equal(1001, firstEmployee.EmployeeId);
        Assert.Equal("ENG", firstEmployee.DepartmentCode);
        Assert.Equal(new DateTime(1985, 3, 15), firstEmployee.BirthDate);
        Assert.Equal(new DateTime(2020, 1, 15), firstEmployee.HireDate);
        Assert.Equal(85000m, firstEmployee.MonthlySalary);
        Assert.True(firstEmployee.IsManager);
        Assert.Equal(PerformanceRating.Excellent, firstEmployee.PerformanceRating);
        Assert.Equal("alice.johnson@company.com", firstEmployee.Email);
        Assert.Equal("555-0123", firstEmployee.PhoneNumber);

        // Verify second employee
        var secondEmployee = result[1];
        Assert.Equal("Charlie Brown", secondEmployee.FullName);
        Assert.Equal(1002, secondEmployee.EmployeeId);
        Assert.Equal("MKT", secondEmployee.DepartmentCode);
        Assert.Equal(new DateTime(1990, 8, 22), secondEmployee.BirthDate);
        Assert.Equal(new DateTime(2021, 6, 1), secondEmployee.HireDate);
        Assert.Equal(70000m, secondEmployee.MonthlySalary);
        Assert.False(secondEmployee.IsManager); // Should use default value
        Assert.Equal(PerformanceRating.Good, secondEmployee.PerformanceRating);
        Assert.Equal("charlie.brown@company.com", secondEmployee.Email);
        Assert.Equal("555-0456", secondEmployee.PhoneNumber);
    }

    [Fact]
    public async Task ImportExcelAsync_WithDifferentDataTypes_ShouldConvertCorrectly()
    {
        // Arrange
        var excelData = CreateDataTypeTestData();
        using var stream = new MemoryStream(excelData);

        // Act
        var result = await excelImportService.ImportExcelAsync<DataTypeTestModel>(stream);

        // Assert
        Assert.NotEmpty(result);
        var model = result[0];

        // Verify string conversion
        Assert.Equal("Sample Text", model.StringValue);

        // Verify numeric conversions
        Assert.Equal(42, model.IntValue);
        Assert.Equal(100, model.NullableIntValue);
        Assert.Equal(123.45m, model.DecimalValue);
        Assert.Equal(98.76, model.DoubleValue);
        Assert.Equal(12.34f, model.FloatValue);

        // Verify boolean conversion
        Assert.True(model.BoolValue);
        Assert.False(model.NullableBoolValue);

        // Verify date/time conversions
        Assert.Equal(new DateTime(2024, 1, 15), model.DateTimeValue);
        Assert.Equal(new DateTime(2023, 12, 25), model.NullableDateTimeValue);
        Assert.Equal(new DateTime(2024, 1, 1), model.DateOnlyValue);
        Assert.Equal(new DateTime(2023, 12, 31), model.NullableDateOnlyValue);

        // Verify enum conversion
        Assert.Equal(TestEnum.Second, model.EnumValue);
        Assert.Equal(TestEnum.Third, model.NullableEnumValue);

        // Verify GUID conversion
        Assert.Equal(new Guid("feb3b80a-a979-4ac5-bd1d-16b96bbf9e8c"), model.GuidValue);
        Assert.Equal(new Guid("9d28f8a6-eeac-49e8-941a-03d8ef7a403b"), model.NullableGuidValue);
    }

    [Fact]
    public async Task ImportExcelAsync_WithCustomOptions_ShouldRespectConfiguration()
    {
        // Arrange
        var excelData = CreateEmployeeDataWithCustomHeaders();
        using var stream = new MemoryStream(excelData);

        var options = new ExcelImportOptions
        {
            SkipHeader = true,
            BatchSize = 2,
            MaxFileSizeBytes = 1024 * 1024, // 1MB
            TrimStrings = true,
            ColumnMappings = new Dictionary<string, string>
            {
                { "Name", "Employee Name" },
                { "Department", "Dept" },
                { "Age", "Years" },
                { "Salary", "Monthly Pay" },
                { "IsActive", "Active" }
            }
        };

        // Act
        var result = await excelImportService.ImportExcelAsync<SimpleEmployee>(stream, options);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count);

        var firstEmployee = result[0];
        Assert.Equal("John Doe", firstEmployee.Name);
        Assert.Equal("IT", firstEmployee.Department);
        Assert.Equal(30, firstEmployee.Age);
        Assert.Equal(75000m, firstEmployee.Salary);
        Assert.True(firstEmployee.IsActive);
    }

    [Fact]
    public async Task ImportExcelAsync_WithLargeDataSet_ShouldHandleMemoryEfficiently()
    {
        // Arrange
        var excelData = CreateLargeDataSet();
        using var stream = new MemoryStream(excelData);

        var options = new ExcelImportOptions
        {
            BatchSize = 100,
            MaxFileSizeBytes = 10 * 1024 * 1024 // 10MB
        };

        // Act
        var result = await excelImportService.ImportExcelAsync<SimpleEmployee>(stream, options);

        // Assert
        Assert.Equal(2000, result.Count);

        // Verify first and last records to ensure correct processing
        var firstEmployee = result[0];
        Assert.Equal("Employee 1", firstEmployee.Name);
        Assert.Equal("Department 1", firstEmployee.Department);

        var lastEmployee = result[1999];
        Assert.Equal("Employee 2000", lastEmployee.Name);
        Assert.Equal("Department 0", lastEmployee.Department); // 2000 % 5 = 0
    }

    [Fact]
    public async Task ImportExcelAsync_WithMixedDataQuality_ShouldHandleGracefully()
    {
        // Arrange
        var excelData = CreateMixedQualityData();
        using var stream = new MemoryStream(excelData);

        var options = new ExcelImportOptions
        {
            ContinueOnMappingError = true,
            TrimStrings = true
        };

        // Act
        var result = await excelImportService.ImportExcelAsync<SimpleEmployee>(stream, options);

        // Assert
        Assert.NotEmpty(result);
        // Should have processed valid rows and skipped invalid ones
        Assert.True(result.Count > 0);

        // Verify that valid data was processed correctly
        var validEmployee = result.First(e => e.Name == "Valid Employee");
        Assert.Equal("Valid Employee", validEmployee.Name);
        Assert.Equal("HR", validEmployee.Department);
        Assert.Equal(25, validEmployee.Age);
        Assert.Equal(50000m, validEmployee.Salary);
        Assert.True(validEmployee.IsActive);
    }

    // Helper methods to create test data
    private static byte[] CreateRealisticEmployeeData()
    {
        // Get a xlsx file from Backend/GHB.DP2.Api.Tests/Templates/CreateRealisticEmployeeData.xlsx
        var templatePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Templates",
            "CreateRealisticEmployeeData.xlsx"
        );

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Excel template file not found at: {templatePath}");
        }

        return File.ReadAllBytes(templatePath);

    }

    private static byte[] CreateComplexEmployeeDataWithAttributes()
    {
        // Get a xlsx file from Backend/GHB.DP2.Api.Tests/Templates/CreateComplexEmployeeDataWithAttributes.xlsx
        var templatePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Templates",
            "CreateComplexEmployeeDataWithAttributes.xlsx"
        );

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Excel template file not found at: {templatePath}");
        }

        return File.ReadAllBytes(templatePath);
    }

    private static byte[] CreateDataTypeTestData()
    {
        // Get a xlsx file from Backend/GHB.DP2.Api.Tests/Templates/CreateDataTypeTestData.xlsx
        var templatePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Templates",
            "CreateDataTypeTestData.xlsx"
        );

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Excel template file not found at: {templatePath}");
        }

        return File.ReadAllBytes(templatePath);
    }

    private static byte[] CreateEmployeeDataWithCustomHeaders()
    {
        // Get a xlsx file from Backend/GHB.DP2.Api.Tests/Templates/CreateEmployeeDataWithCustomHeaders.xlsx
        var templatePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Templates",
            "CreateEmployeeDataWithCustomHeaders.xlsx"
        );

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Excel template file not found at: {templatePath}");
        }

        return File.ReadAllBytes(templatePath);
    }

    private static byte[] CreateLargeDataSet()
    {
        // Get a xlsx file from Backend/GHB.DP2.Api.Tests/Templates/CreateLargeDataSet.xlsx
        var templatePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Templates",
            "CreateLargeDataSet.xlsx"
        );

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Excel template file not found at: {templatePath}");
        }

        return File.ReadAllBytes(templatePath);
    }

    private static byte[] CreateMixedQualityData()
    {
        // Get a xlsx file from Backend/GHB.DP2.Api.Tests/Templates/CreateMixedQualityData.xlsx
        var templatePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Templates",
            "CreateMixedQualityData.xlsx"
        );

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Excel template file not found at: {templatePath}");
        }

        return File.ReadAllBytes(templatePath);
    }
}