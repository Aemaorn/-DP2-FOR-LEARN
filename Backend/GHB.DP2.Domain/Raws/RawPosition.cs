namespace GHB.DP2.Domain.Raws;

using Codehard.Common.DomainModel;
using Vogen;

[ValueObject<string>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PositionId;

public class RawPosition : Entity<PositionId>
{
    public override PositionId Id { get; init; }

    public string PositionCode { get; init; }

    public string Grade { get; private set; }

    public int Sequence { get; init; }

    public string Name { get; private set; }

    public string? Remark { get; init; }

    public string InRefCode { get; private set; }

    public string InRefLevel { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static RawPosition Create(
        string id,
        string grade,
        int sequence,
        string name,
        string inRefCode)
    {
        return new RawPosition
        {
            Id = PositionId.From(id),
            PositionCode = string.Empty,
            Grade = grade,
            Sequence = sequence,
            Name = name,
            InRefCode = inRefCode,
            InRefLevel = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public RawPosition Update(
        string grade,
        string name,
        string inRefCode)
    {
        this.Grade = grade;
        this.Name = name;
        this.InRefCode = inRefCode;
        this.UpdatedAt = DateTimeOffset.UtcNow;

        return this;
    }

    public RawPosition SetInRefCode(string inRefCode)
    {
        this.InRefCode = inRefCode;
        this.UpdatedAt = DateTimeOffset.UtcNow;

        return this;
    }
}