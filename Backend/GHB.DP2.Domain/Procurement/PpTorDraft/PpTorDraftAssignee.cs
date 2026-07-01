namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorDraftAssigneeId
{
    public static PpTorDraftAssigneeId New() => From(Guid.CreateVersion7());
}

public partial class PpTorDraftAssignee : AssigneeInfoEntity<PpTorDraftAssigneeId>, IHasSoftDelete
{
    public override PpTorDraftAssigneeId Id { get; init; }

    public virtual PpTorDraft PpTorDraft { get; init; }

    public static PpTorDraftAssignee Create(
        AssigneeGroup assigneeGroup,
        AssigneeType assigneeType,
        SuUser user,
        int sequence)
    {
        return Create(PpTorDraftAssigneeId.New(), assigneeGroup, assigneeType, user, sequence);
    }

    public static PpTorDraftAssignee Create(
        PpTorDraftAssigneeId id,
        AssigneeGroup assigneeGroup,
        AssigneeType assigneeType,
        SuUser user,
        int sequence)
    {
        var assignee = new PpTorDraftAssignee
        {
            Id = id,
        };

        if (user.Employee.View is null)
        {
            throw new ArgumentException("User must have an associated employee.");
        }

        _ = assignee
            .SetGroup(assigneeGroup)
            .SetType(assigneeType)
            .SetUser(
                user.Id,
                user.EmployeeCode,
                user.Employee.View.FullName,
                user.Employee.View.FullPositionName,
                user.Employee.View.BusinessUnitName)
            .SetSequence(sequence)
            .Draft();

        return assignee;
    }

    public PpTorDraftAssignee Clone()
    {
        var assignee = new PpTorDraftAssignee
        {
            Id = PpTorDraftAssigneeId.New(),
        };

        assignee.SetGroup(this.Group)
                .SetType(this.Type)
                .SetUser(
                this.UserId,
                this.EmployeeCode,
                this.FullName,
                this.PositionName,
                this.BusinessUnitName)
            .SetSequence(this.Sequence)
            .Draft();

        return assignee;
    }
}