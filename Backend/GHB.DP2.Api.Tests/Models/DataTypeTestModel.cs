using GHB.DP2.Infrastructure.Services.ExcelImportAndExport;

namespace GHB.DP2.Api.Tests.Models;

/// <summary>
/// Model for testing various data type conversions during Excel import.
/// </summary>
public class DataTypeTestModel
{
    [ExcelColumn(0)]
    public string? StringValue { get; set; }

    [ExcelColumn(1)]
    public int IntValue { get; set; }

    [ExcelColumn(2)]
    public int? NullableIntValue { get; set; }

    [ExcelColumn(3)]
    public int LongValue { get; set; }

    [ExcelColumn(4)]
    public decimal DecimalValue { get; set; }

    [ExcelColumn(5)]
    public double DoubleValue { get; set; }

    [ExcelColumn(6)]
    public float FloatValue { get; set; }

    [ExcelColumn(7)]
    public bool BoolValue { get; set; }

    [ExcelColumn(8)]
    public bool? NullableBoolValue { get; set; }

    [ExcelColumn(9, DateFormat = "yyyy-MM-dd")]
    public DateTime DateTimeValue { get; set; }

    [ExcelColumn(10)]
    public DateTime? NullableDateTimeValue { get; set; }

    [ExcelColumn(11)]
    public DateTime DateOnlyValue { get; set; }

    [ExcelColumn(12)]
    public DateTime? NullableDateOnlyValue { get; set; }

    [ExcelColumn(13)]
    public TestEnum EnumValue { get; set; }

    [ExcelColumn(14)]
    public TestEnum? NullableEnumValue { get; set; }

    [ExcelColumn(15)]
    public Guid GuidValue { get; set; }

    [ExcelColumn(16)]
    public Guid? NullableGuidValue { get; set; }
}

/// <summary>
/// Test enum for various enum parsing scenarios.
/// </summary>
public enum TestEnum
{
    None = 0,
    First = 1,
    Second = 2,
    Third = 3
}