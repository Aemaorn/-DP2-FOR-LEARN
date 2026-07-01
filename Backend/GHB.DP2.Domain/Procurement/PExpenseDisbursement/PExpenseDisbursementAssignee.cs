namespace GHB.DP2.Domain.Procurement.PExpenseDisbursement;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PExpenseDisbursementAssigneeId
{
    public static PExpenseDisbursementAssigneeId New() => From(Guid.CreateVersion7());
}

public partial class PExpenseDisbursementAssignee : AssigneeInfoEntity<PExpenseDisbursementAssigneeId>
{
    public override PExpenseDisbursementAssigneeId Id { get; init; }

    public PExpenseDisbursementId PExpenseDisbursementId { get; init; }

    public virtual PExpenseDisbursement PExpenseDisbursement { get; init; }

    public static PExpenseDisbursementAssignee Create(
        AssigneeGroup group,
        AssigneeType type,
        SuUser user,
        int sequence,
        string? remark)
    {
        if (user.Employee.View is null)
        {
            throw new ArgumentException("User does not have view assigned.");
        }

        var assignee = new PExpenseDisbursementAssignee
        {
            Id = PExpenseDisbursementAssigneeId.New(),
        };

        _ = assignee.SetGroup(group)
                    .SetType(type)
                    .SetUser(
                        user.Id,
                        user.EmployeeCode,
                        user.Employee.View.FullName,
                        user.Employee.View?.FullPositionName ?? string.Empty,
                        user.Employee.View?.BusinessUnitName ?? string.Empty)
                    .SetSequence(sequence)
                    .SetRemark(remark)
                    .Draft();

        return assignee;
    }
}