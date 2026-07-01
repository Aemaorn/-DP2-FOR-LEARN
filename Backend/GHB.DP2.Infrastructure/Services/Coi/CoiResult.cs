namespace GHB.DP2.Infrastructure.Services.Coi;

using System.Text.Json.Serialization;

public abstract class CoiResultBase
{
    [JsonPropertyName("message")]
    public string Message { get; init; }

    [JsonPropertyName("status")]
    public bool Status { get; init; }
}

public class CoiResult : CoiResultBase
{
    [JsonPropertyName("data")]
    public CoiInfo[] Data { get; init; }
}

public class CoiInfo
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("emp_code")]
    public string EmployeeCode { get; init; }

    [JsonPropertyName("emp_name")]
    public string EmployeeName { get; init; }

    [JsonPropertyName("position_name")]
    public string PositionName { get; init; }

    [JsonPropertyName("division")]
    public string DivisionName { get; init; }

    [JsonPropertyName("coi_name")]
    public string CoiName { get; init; }

    [JsonPropertyName("ssn")]
    public string? Ssn { get; init; }

    [JsonPropertyName("relationid")]
    public int RelationId { get; init; }

    [JsonPropertyName("relation")]
    public string RelationName { get; init; }
}