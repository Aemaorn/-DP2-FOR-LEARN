namespace GHB.DP2.Domain.Procurement.PpAppoint;

using GHB.DP2.Domain.Common;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpAppointTorDraftCommitteeDutiesId
{
    public static PpAppointTorDraftCommitteeDutiesId New() => From(Guid.CreateVersion7());
}

public partial class PpAppointTorDraftCommitteeDuties : AuditableEntity<PpAppointTorDraftCommitteeDutiesId>, IHasSoftDelete
{
    public override PpAppointTorDraftCommitteeDutiesId Id { get; init; }

    public PpAppointId PpAppointId { get; init; }

    public string Description { get; private set; }

    public int Sequence { get; private set; }

    public virtual PpAppoint PpAppoint { get; init; }

    public static PpAppointTorDraftCommitteeDuties Create(
        PpAppointId ppAppointId,
        string description,
        int sequence)
    {
        return new PpAppointTorDraftCommitteeDuties
        {
            Id = PpAppointTorDraftCommitteeDutiesId.New(),
            PpAppointId = ppAppointId,
            Description = description,
            Sequence = sequence,
        };
    }

    public PpAppointTorDraftCommitteeDuties Clone(
        PpAppointId newId)
    {
        return new PpAppointTorDraftCommitteeDuties
        {
            Id = PpAppointTorDraftCommitteeDutiesId.New(),
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