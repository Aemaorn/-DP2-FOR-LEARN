namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorTrainingItemId
{
    public static PpTorTrainingItemId New() => From(Guid.CreateVersion7());
}

public class PpTorTrainingItem : AuditableEntity<PpTorTrainingItemId>
{
    public override PpTorTrainingItemId Id { get; init; }

    public int? Sequence { get; set; }

    public string? CourseName { get; set; }

    public int? PeriodDay { get; set; }

    public string? Place { get; set; }

    public int? TrainingCount { get; set; }

    public int? TotalPersonPerTime { get; set; }

    public virtual PpTorDraft PpTorDraft { get; init; }
}
