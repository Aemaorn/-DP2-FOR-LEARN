namespace GHB.DP2.Api.Tests.Models;

/// <summary>
/// Simple POCO model for basic Excel import testing.
/// </summary>
public class SimpleEmployee
{
    public string? Name { get; set; }

    public string? Department { get; set; }

    public int Age { get; set; }

    public decimal Salary { get; set; }

    public bool IsActive { get; set; }
}