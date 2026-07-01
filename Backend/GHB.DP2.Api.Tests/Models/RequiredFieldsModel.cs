using GHB.DP2.Infrastructure.Services.ExcelImportAndExport;

namespace GHB.DP2.Api.Tests.Models;

/// <summary>
/// Model for testing required field validation and error scenarios.
/// </summary>
public class RequiredFieldsModel
{
    [ExcelColumn("Required String", IsRequired = true)]
    public string? RequiredString { get; set; }

    [ExcelColumn("Optional String")]
    public string? OptionalString { get; set; }

    [ExcelColumn("Required Number", IsRequired = true)]
    public int RequiredNumber { get; set; }

    [ExcelColumn("Optional Number")]
    public int? OptionalNumber { get; set; }

    [ExcelColumn("Required Date", IsRequired = true)]
    public DateTime RequiredDate { get; set; }

    [ExcelColumn("With Default", DefaultValue = "Default Value")]
    public string? WithDefault { get; set; }

    [ExcelColumn("Number With Default", DefaultValue = 100)]
    public int NumberWithDefault { get; set; }

    [ExcelColumn("Bool With Default", DefaultValue = true)]
    public bool BoolWithDefault { get; set; }
}