namespace GHB.DP2.Domain.Procurement.Pw184;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;

public partial class Pw184Acceptor : AcceptorInfoEntity, IHasSoftDelete
{
    public override AcceptorId Id { get; init; }

    public Pw184Id Pw184Id { get; init; }

    public virtual Pw184 Pw184 { get; init; }

    public static Pw184Acceptor Create(
        AcceptorType type,
        SuUser user,
        int sequence)
    {
        var acceptor = new Pw184Acceptor
        {
            Id = AcceptorId.New(),
        };

        if (user.Employee.View is null)
            throw new ArgumentException("View is null");

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

    public static Pw184Acceptor CreateWithPending(
        AcceptorType type,
        SuUser user,
        int sequence)
    {
        var acceptor = new Pw184Acceptor
        {
            Id = AcceptorId.New(),
        };

        if (user.Employee.View is null)
            throw new ArgumentException("View is null");

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

    public Unit Update(AcceptorType type, SuUser user, int sequence, AcceptorStatus status)
    {
        this.SetType(type)
            .SetUser(
                user.Id,
                user.EmployeeCode,
                user.Employee.View!.FullName,
                user.Employee.View.FullPositionName,
                user.Employee.View.BusinessUnitName)
            .SetSequence(sequence)
            .SetActive()
            .SetStatus(status);

        return unit;
    }
}
