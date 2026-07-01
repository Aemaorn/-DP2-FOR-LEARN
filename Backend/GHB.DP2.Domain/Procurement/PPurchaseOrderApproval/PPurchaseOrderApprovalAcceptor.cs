namespace GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;

public partial class PPurchaseOrderApprovalAcceptor : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public virtual PPurchaseOrderApproval PPurchaseOrderApproval { get; init; }

    public record AcceptorInfoData(
        AcceptorType Type,
        UserId UserId,
        EmployeeCode EmployeeCode,
        string FullName,
        string FullPositionName,
        string BusinessUnitName,
        int Sequence
    );

    public static PPurchaseOrderApprovalAcceptor Create(
        AcceptorType type,
        SuUser user,
        int sequence,
        BusinessUnitId workBusinessUnitId)
    {
        var acceptor = new PPurchaseOrderApprovalAcceptor
        {
            Id = AcceptorId.New(),
        };

        if (user.Employee.View is null)
        {
            throw new ArgumentException("View is null");
        }

        _ = acceptor.SetType(type)
                    .SetUser(
                        user.Id,
                        user.EmployeeCode,
                        user.Employee.View.FullName,
                        user.Employee.ConvertPositionName(workBusinessUnitId),
                        user.Employee.View.BusinessUnitName)
                    .SetSequence(sequence)
                    .SetActive()
                    .Draft();

        return acceptor;
    }

    public Unit Update(
        AcceptorInfoData info,
        AcceptorStatus status,
        BusinessUnitId workBusinessUnitId)
    {
        var positionName =
            !this.User.IsNull()
                ? this.User.Employee.ConvertPositionName(workBusinessUnitId)
                : info.FullPositionName;

        this.SetType(info.Type)
            .SetUser(
                info.UserId,
                info.EmployeeCode,
                info.FullName,
                positionName,
                info.BusinessUnitName)
            .SetSequence(info.Sequence)
            .SetActive()
            .SetStatus(status);

        return unit;
    }
}