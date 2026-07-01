namespace GHB.DP2.Domain.Procurement.PPrincipleApproval;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

public enum CommitteeGroupType
{
    /// <summary>
    /// คณะกรรมการจัดเช่า
    /// </summary>
    RentCommittee,

    /// <summary>
    /// คณะกรรมการตรวจรับ
    /// </summary>
    AcceptanceCommittee,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalCommitteeId
{
    public static PPrincipleApprovalCommitteeId New() => From(Guid.CreateVersion7());
}

public partial class PPrincipleApprovalCommittee : AuditableEntity<PPrincipleApprovalCommitteeId>, IHasSoftDelete
{
    public override PPrincipleApprovalCommitteeId Id { get; init; }

    public CommitteeGroupType GroupType { get; private set; }

    public UserId SuUserId { get; private set; }

    public string FullName { get; private set; }

    public string FullPositionName { get; private set; }

    public ParameterCode CommitteePositionsCode { get; private set; }

    public string CommitteePositionsName { get; private set; }

    public int Sequence { get; private set; }

    public virtual PPrincipleApproval PPrincipleApproval { get; init; }

    public virtual SuUser User { get; private set; }

    public static PPrincipleApprovalCommittee Create(
        CommitteeGroupType groupType,
        SuUser user,
        ParameterCode committeePositionsCode,
        string committeePositionsName,
        int sequence)
    {
        if (user.Employee?.View == null)
        {
            throw new InvalidOperationException($"User {user.Id} ไม่มีข้อมูล Employee.View ที่จำเป็นสำหรับการสร้างกรรมการ.");
        }

        return new PPrincipleApprovalCommittee
        {
            Id = PPrincipleApprovalCommitteeId.New(),
            GroupType = groupType,
            SuUserId = user.Id,
            FullName = user.Employee.View.FullName,
            FullPositionName = user.Employee.View.FullPositionName,
            CommitteePositionsCode = committeePositionsCode,
            CommitteePositionsName = committeePositionsName,
            Sequence = sequence,
            User = user,
        };
    }

    public PPrincipleApprovalCommittee Update(
        CommitteeGroupType groupType,
        SuUser user,
        ParameterCode committeePositionsCode,
        string committeePositionsName,
        int sequence)
    {
        if (user.Employee?.View == null)
        {
            throw new InvalidOperationException($"User {user.Id} ไม่มีข้อมูล Employee.View");
        }

        this.GroupType = groupType;
        this.SuUserId = user.Id;
        this.FullName = user.Employee.View.FullName;
        this.FullPositionName = user.Employee.View.FullPositionName;
        this.CommitteePositionsCode = committeePositionsCode;
        this.CommitteePositionsName = committeePositionsName;
        this.Sequence = sequence;
        this.User = user;

        return this;
    }
}