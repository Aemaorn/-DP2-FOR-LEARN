namespace GHB.DP2.Domain.Procurement.PJp005;

using GHB.DP2.Domain.Common;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PJp005CommitteeDutiesId
{
    public static PJp005CommitteeDutiesId New() => From(Guid.CreateVersion7());
}

public partial class PJp005CommitteeDuties : AuditableEntity<PJp005CommitteeDutiesId>
{
    public override PJp005CommitteeDutiesId Id { get; init; }

    public PJp005Id PJp005Id { get; init; }

    public PJp005CommitteeGroupType GroupType { get; init; }

    public string Description { get; private set; }

    public int Sequence { get; private set; }

    public virtual PJp005 PJp005 { get; init; }

    public static PJp005CommitteeDuties CreateCommitteeDuty(
        PJp005Id pJp005Id,
        string description,
        int sequence,
        PJp005CommitteeGroupType groupType)
    {
        return new PJp005CommitteeDuties
        {
            Id = PJp005CommitteeDutiesId.New(),
            PJp005Id = pJp005Id,
            Description = description,
            Sequence = sequence,
            GroupType = groupType,
        };
    }

    public Unit Update(
        string description,
        int sequence)
    {
        this.Description = description;
        this.Sequence = sequence;

        return unit;
    }
}