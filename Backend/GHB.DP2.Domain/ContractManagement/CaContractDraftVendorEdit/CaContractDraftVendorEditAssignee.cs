namespace GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CaContractDraftVendorEditAssigneeId
{
    public static CaContractDraftVendorEditAssigneeId New() => From(Guid.CreateVersion7());
}

public partial class CaContractDraftVendorEditAssignee : AssigneeInfoEntity<CaContractDraftVendorEditAssigneeId>, IHasSoftDelete
{
    public override CaContractDraftVendorEditAssigneeId Id { get; init; }

    public virtual CaContractDraftVendorEdit CaContractDraftVendorEdit { get; init; }

    public static CaContractDraftVendorEditAssignee Create(
        AssigneeGroup assigneeGroup,
        AssigneeType assigneeType,
        SuUser user,
        int sequence)
    {
        var assignee = new CaContractDraftVendorEditAssignee
        {
            Id = CaContractDraftVendorEditAssigneeId.New(),
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
