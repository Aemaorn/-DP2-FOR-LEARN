namespace GHB.DP2.Domain.Plan;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;

public class PlanAnnouncementAcceptor : AcceptorInfoEntity
{
    public override AcceptorId Id { get; init; }

    public PlanAnnouncementId PlanAnnouncementId { get; init; }

    public virtual PlanAnnouncement PlanAnnouncement { get; init; }

    public static PlanAnnouncementAcceptor Create(
        AcceptorType type,
        SuUser user,
        int sequence)
    {
        if (user.Employee.View is null)
        {
            throw new ArgumentException("User does not have view assigned.");
        }

        var acceptor = new PlanAnnouncementAcceptor
        {
            Id = AcceptorId.New(),
        };

        _ = acceptor.SetType(type)
                    .SetUser(
                        user.Id,
                        user.EmployeeCode,
                        user.FullName,
                        user.Employee.View?.FullPositionName ?? string.Empty,
                        user.Employee.View?.BusinessUnitName ?? string.Empty)
                    .SetSequence(sequence)
                    .SetActive();

        return acceptor;
    }
}