namespace GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmDeliveryAcceptancePeriodAssigneeId
{
    public static CmDeliveryAcceptancePeriodAssigneeId New() => From(Guid.CreateVersion7());
}

public partial class CmDeliveryAcceptancePeriodAssignee : AssigneeInfoEntity<CmDeliveryAcceptancePeriodAssigneeId>, IHasSoftDelete
{
    public override CmDeliveryAcceptancePeriodAssigneeId Id { get; init; }

    public CmDeliveryAcceptancePeriodId DeliveryAcceptancePeriodId { get; init; }

    public virtual CmDeliveryAcceptancePeriod DeliveryAcceptancePeriod { get; init; }

    public static CmDeliveryAcceptancePeriodAssignee Create(
        CmDeliveryAcceptancePeriodId deliveryAcceptancePeriodId,
        AssigneeGroup assigneeGroup,
        AssigneeType assigneeType,
        SuUser user,
        int sequence)
    {
        var assignee = new CmDeliveryAcceptancePeriodAssignee
        {
            Id = CmDeliveryAcceptancePeriodAssigneeId.New(),
            DeliveryAcceptancePeriodId = deliveryAcceptancePeriodId,
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
                user.FullName,
                user.Employee.View.FullPositionName,
                user.Employee.View.BusinessUnitName)
            .SetSequence(sequence)
            .Pending();

        return assignee;
    }
}