namespace GHB.DP2.Domain.Procurement.PJp005;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PJp005ProcurementSuppliesDivisionId
{
    public static PJp005ProcurementSuppliesDivisionId New() => From(Guid.CreateVersion7());
}

public class PJp005ProcurementSuppliesDivision : AuditableEntity<PJp005ProcurementSuppliesDivisionId>
{
    public override PJp005ProcurementSuppliesDivisionId Id { get; init; }

    public PJp005Id PJp005Id { get; init; }

    public UserId SuUserId { get; init; }

    public string FullName { get; init; }

    public string FullPositionName { get; init; }

    public int Sequence { get; private set; }

    public virtual PJp005 PJp005 { get; init; }

    public virtual SuUser SuUser { get; init; }

    public static PJp005ProcurementSuppliesDivision CreatePJp005ProcurementSuppliesDivision(
        PJp005Id pJp005Id,
        UserId userId,
        string fullName,
        string positionName,
        int sequence)
    {
        return new PJp005ProcurementSuppliesDivision
        {
            Id = PJp005ProcurementSuppliesDivisionId.New(),
            PJp005Id = pJp005Id,
            SuUserId = userId,
            FullName = fullName,
            FullPositionName = positionName,
            Sequence = sequence,
        };
    }

    public static PJp005ProcurementSuppliesDivision CreateInspectionProcurementSuppliesDivision(
        PJp005Id pJp005Id,
        UserId userId,
        string fullName,
        string positionName,
        int sequence)
    {
        return new PJp005ProcurementSuppliesDivision
        {
            Id = PJp005ProcurementSuppliesDivisionId.New(),
            PJp005Id = pJp005Id,
            SuUserId = userId,
            FullName = fullName,
            FullPositionName = positionName,
            Sequence = sequence,
        };
    }

    public Unit Update(int sequence)
    {
        this.Sequence = sequence;

        return unit;
    }
}