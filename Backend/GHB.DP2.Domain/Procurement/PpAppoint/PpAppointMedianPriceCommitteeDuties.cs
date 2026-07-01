namespace GHB.DP2.Domain.Procurement.PpAppoint;

using GHB.DP2.Domain.Common;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpAppointMedianPriceCommitteeDutiesId
{
    public static PpAppointMedianPriceCommitteeDutiesId New() => From(Guid.CreateVersion7());
}

public partial class PpAppointMedianPriceCommitteeDuties : AuditableEntity<PpAppointMedianPriceCommitteeDutiesId>, IHasSoftDelete
{
    public override PpAppointMedianPriceCommitteeDutiesId Id { get; init; }

    public PpAppointId PpAppointId { get; init; }

    public string Description { get; private set; }

    public int Sequence { get; private set; }

    public virtual PpAppoint PpAppoint { get; init; }

    public static PpAppointMedianPriceCommitteeDuties Create(
        PpAppointId ppAppointId,
        string description,
        int sequence)
    {
        return new PpAppointMedianPriceCommitteeDuties
        {
            Id = PpAppointMedianPriceCommitteeDutiesId.New(),
            PpAppointId = ppAppointId,
            Description = description,
            Sequence = sequence,
        };
    }

    public PpAppointMedianPriceCommitteeDuties Clone(PpAppointId newId)
    {
        return new PpAppointMedianPriceCommitteeDuties
        {
            Id = PpAppointMedianPriceCommitteeDutiesId.New(),
            PpAppointId = newId,
            Description = this.Description,
            Sequence = this.Sequence,
        };
    }

    public Unit UpdateDescription(string description)
    {
        this.Description = description;

        return unit;
    }

    public Unit UpdateSequence(int sequence)
    {
        this.Sequence = sequence;

        return unit;
    }
}