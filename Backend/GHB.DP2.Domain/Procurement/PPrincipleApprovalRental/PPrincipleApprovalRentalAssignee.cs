namespace GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalAssigneeId
{
    public static PPrincipleApprovalRentalAssigneeId New() => From(Guid.CreateVersion7());
}

public partial class PPrincipleApprovalRentalAssignee : AssigneeInfoEntity<PPrincipleApprovalRentalAssigneeId>, IHasSoftDelete
{
    public override PPrincipleApprovalRentalAssigneeId Id { get; init; }

    public virtual PPrincipleApprovalRental PPrincipleApprovalRental { get; init; }

    public static PPrincipleApprovalRentalAssignee Create(
        AssigneeGroup assigneeGroup,
        AssigneeType assigneeType,
        SuUser user,
        int sequence)
    {
        var assignee = new PPrincipleApprovalRentalAssignee
        {
            Id = PPrincipleApprovalRentalAssigneeId.New(),
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
            .SetSequence(sequence);

        return assignee;
    }
}