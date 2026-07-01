namespace GHB.DP2.Domain.Plan;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;

public class PlanAcceptor : AcceptorInfoEntity
{
    public override AcceptorId Id { get; init; }

    public PlanId PlanId { get; init; }

    public virtual Plan Plan { get; init; }

    public static PlanAcceptor Create(
        AcceptorType type,
        SuUser user,
        int sequence,
        BusinessUnitId workBusinessUnitId)
    {
        var acceptor = new PlanAcceptor
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

    public new PlanAcceptor Clone(BusinessUnitId workBusinessUnitId)
    {
        var newAcceptor = new PlanAcceptor
        {
            Id = AcceptorId.New(),
            PlanId = this.PlanId,
        };

        if (this.User.Employee.View is null)
        {
            throw new ArgumentException("View is null");
        }

        newAcceptor.SetType(this.Type)
                   .SetUser(
                       this.User.Id,
                       this.User.EmployeeCode,
                       this.User.Employee.View!.FullName,
                       this.User.Employee.ConvertPositionName(workBusinessUnitId),
                       this.User.Employee.View!.BusinessUnitName)
                   .SetSequence(this.Sequence)
                   .SetActive()
                   .Draft();

        return newAcceptor;
    }

    public static PlanAcceptor CreateWithPending(
        AcceptorType type,
        SuUser user,
        int sequence)
    {
        var acceptor = new PlanAcceptor
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
                    .Pending();

        return acceptor;
    }
}