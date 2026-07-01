namespace GHB.DP2.Application.Features.SystemUtility.SuParameter.DTO;

public class ParameterKeyValue
{
    public required string Key { get; init; }

    public required ParameterValues? Value { get; init; }

    public record ParameterValues(int Sequence, object? Value);
}