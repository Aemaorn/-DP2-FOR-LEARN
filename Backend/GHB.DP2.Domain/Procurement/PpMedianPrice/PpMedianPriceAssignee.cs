namespace GHB.DP2.Domain.Procurement.PpMedianPrice;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct MedianPriceAssigneeId
{
    public static MedianPriceAssigneeId New() => From(Guid.CreateVersion7());
}

public partial class PpMedianPriceAssignee : AssigneeInfoEntity<MedianPriceAssigneeId>, IHasSoftDelete
{
    public override MedianPriceAssigneeId Id { get; init; }

    public virtual PpMedianPrice MedianPrice { get; init; }

    public PpMedianPriceAssignee Clone()
    {
        var newAssignee = new PpMedianPriceAssignee
        {
            Id = MedianPriceAssigneeId.New(),
        };

        newAssignee.SetGroup(this.Group)
                   .SetType(this.Type)
                   .SetSequence(this.Sequence)
                   .SetUser(
                       this.UserId,
                       this.EmployeeCode,
                       this.FullName,
                       this.PositionName,
                       this.BusinessUnitName)
                   .SetDelegatee(this.DelegateeId)
                   .Draft();

        return newAssignee;
    }

    public static PpMedianPriceAssignee Create(
        AssigneeGroup assigneeGroup,
        AssigneeType assigneeType,
        SuUser user,
        int sequence)
    {
        return Create(MedianPriceAssigneeId.New(), assigneeGroup, assigneeType, user, sequence);
    }

    public static PpMedianPriceAssignee Create(
        MedianPriceAssigneeId id,
        AssigneeGroup assigneeGroup,
        AssigneeType assigneeType,
        SuUser user,
        int sequence)
    {
        var assignee = new PpMedianPriceAssignee
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
}