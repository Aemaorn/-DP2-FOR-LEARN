namespace GHB.DP2.Domain.Plan;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PlanAnnouncementSelectedId
{
    public static PlanAnnouncementSelectedId New() => From(Guid.CreateVersion7());
}

public partial class PlanAnnouncementSelected : AuditableEntity<PlanAnnouncementSelectedId>, IHasSoftDelete
{
    public override PlanAnnouncementSelectedId Id { get; init; }

    public PlanId PlanId { get; private set; }

    public PlanAnnouncementId PlanAnnouncementId { get; private set; }

    public virtual Plan Plan { get; init; }

    public virtual PlanAnnouncement PlanAnnouncement { get; init; }

    public static PlanAnnouncementSelected Create(
        PlanId planId,
        PlanAnnouncementId planAnnouncementId)
    {
        return new PlanAnnouncementSelected
        {
            Id = PlanAnnouncementSelectedId.New(),
            PlanId = planId,
            PlanAnnouncementId = planAnnouncementId,
        };
    }
}