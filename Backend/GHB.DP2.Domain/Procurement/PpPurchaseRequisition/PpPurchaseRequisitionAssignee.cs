namespace GHB.DP2.Domain.Procurement.PpPurchaseRequisition;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpPurchaseRequisitionAssigneeId
{
    public static PpPurchaseRequisitionAssigneeId New() => From(Guid.CreateVersion7());
}

public class PpPurchaseRequisitionAssignee : AssigneeInfoEntity<PpPurchaseRequisitionAssigneeId>
{
    public override PpPurchaseRequisitionAssigneeId Id { get; init; }

    public PpPurchaseRequisitionId PpPurchaseRequisitionId { get; init; }

    public virtual PpPurchaseRequisition PpPurchaseRequisition { get; init; }

    public static PpPurchaseRequisitionAssignee Create(
        AssigneeType type,
        SuUser user,
        int sequence)
    {
        var assignee = new PpPurchaseRequisitionAssignee
        {
            Id = PpPurchaseRequisitionAssigneeId.New(),
        };

        if (user.Employee.View is null)
        {
            throw new ArgumentException("User must have an associated employee.");
        }

        _ = assignee.SetType(type)
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