namespace GHB.DP2.Domain.Procurement.PExpenseDisbursement;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;

public partial class PExpenseDisbursementAcceptor : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public PExpenseDisbursementId PExpenseDisbursementId { get; init; }

    public virtual PExpenseDisbursement PExpenseDisbursement { get; init; }

    public record AcceptorInfoData(
        AcceptorType Type,
        UserId UserId,
        EmployeeCode EmployeeCode,
        string FullName,
        string FullPositionName,
        string BusinessUnitName,
        int Sequence
    );

    public static PExpenseDisbursementAcceptor Create(
        AcceptorType type,
        SuUser user,
        int sequence)
    {
        var acceptor = new PExpenseDisbursementAcceptor
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
                        user.Employee.View.FullPositionName,
                        user.Employee.View.BusinessUnitName)
                    .SetSequence(sequence)
                    .SetActive()
                    .Draft();

        return acceptor;
    }

    public Unit Update(
        AcceptorInfoData info,
        AcceptorStatus status)
    {
        this.SetType(info.Type)
            .SetUser(
                info.UserId,
                info.EmployeeCode,
                info.FullName,
                info.FullPositionName,
                info.BusinessUnitName)
            .SetSequence(info.Sequence)
            .SetActive()
            .SetStatus(status);

        return unit;
    }

    public bool ArePreviousAcceptorsApproved(IEnumerable<PExpenseDisbursementAcceptor> acceptors)
    {
        var previous = acceptors.Where(a => a.Type == this.Type && a.Sequence < this.Sequence && a.IsActive).ToList();

        return previous.Count == 0 || previous.All(a => a.Status == AcceptorStatus.Approved);
    }
}