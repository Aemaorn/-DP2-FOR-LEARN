using GHB.DP2.Infrastructure.Services.ExcelImportAndExport;

namespace GHB.DP2.Api.Tests.Models;

/// <summary>
/// Complex model with Excel column attributes for testing attribute-based mapping.
/// </summary>
public class ComplexEmployee
{
    [ExcelColumn("Full Name", IsRequired = true)]
    public string? FullName { get; set; }

    [ExcelColumn("Employee ID", IsRequired = true)]
    public int EmployeeId { get; set; }

    [ExcelColumn("Department Code")]
    public string? DepartmentCode { get; set; }

    [ExcelColumn("Birth Date", DateFormat = "yyyy-MM-dd")]
    public DateTime? BirthDate { get; set; }

    [ExcelColumn("Hire Date", DateFormat = "dd/MM/yyyy", IsRequired = true)]
    public DateTime HireDate { get; set; }

    [ExcelColumn("Monthly Salary", NumberFormat = "C")]
    public decimal MonthlySalary { get; set; }

    [ExcelColumn("Is Manager", DefaultValue = false)]
    public bool IsManager { get; set; }

    [ExcelColumn("Performance Rating")]
    public PerformanceRating? PerformanceRating { get; set; }

    [ExcelColumn("Email Address")]
    public string? Email { get; set; }

    [ExcelColumn("Phone Number")]
    public string? PhoneNumber { get; set; }

    [ExcelColumn(Ignore = true)]
    public string? InternalNotes { get; set; }
}

/// <summary>
/// Enum for testing enum mapping.
/// </summary>
public enum PerformanceRating
{
    Poor = 1,
    Fair = 2,
    Good = 3,
    Excellent = 4,
    Outstanding = 5
}