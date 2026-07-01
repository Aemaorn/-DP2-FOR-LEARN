namespace GHB.DP2.Domain.Procurement.PPettyCash;

using System.Text.Json;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Constants;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

public enum GroupType
{
    /// <summary>
    /// ผู้ขอซื้อขอจ้าง
    /// </summary>
    ProcurementCommittee,

    /// <summary>
    /// ผู้ตรวจรับพัสดุ
    /// </summary>
    InspectionCommittee,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPettyCashCommitteeId
{
    public static PPettyCashCommitteeId New() => From(Guid.CreateVersion7());
}

public partial class PPettyCashCommittee : AuditableEntity<PPettyCashCommitteeId>
{
    public override PPettyCashCommitteeId Id { get; init; }

    public PettyCashId PettyCashId { get; init; }

    public GroupType GroupType { get; private set; }

    public UserId SuUserId { get; init; }

    public string FullName { get; private set; }

    public string FullPositionName { get; private set; }

    public ParameterCode CommitteePositionsCode { get; private set; }

    public string CommitteePositionsName { get; private set; }

    public int Sequence { get; private set; }

    public virtual PPettyCash PettyCash { get; init; }

    public virtual SuUser User { get; init; }

    public virtual SuParameter CommitteePositions { get; init; }

    public static PPettyCashCommittee Create(
        PettyCashId pettyCashId,
        GroupType groupType,
        UserId suUserId,
        string fullName,
        string fullPositionName,
        ParameterCode committeePositionsCode,
        string committeePositionsName,
        int sequence)
    {
        return new PPettyCashCommittee
        {
            Id = PPettyCashCommitteeId.New(),
            PettyCashId = pettyCashId,
            GroupType = groupType,
            SuUserId = suUserId,
            FullName = fullName,
            FullPositionName = fullPositionName,
            CommitteePositionsCode = committeePositionsCode,
            CommitteePositionsName = committeePositionsName,
            Sequence = sequence,
        };
    }

    public PPettyCashCommittee Update(
        string fullName,
        string fullPositionName,
        ParameterCode committeePositionsCode,
        string committeePositionsName,
        GroupType groupType,
        int sequence)
    {
        this.FullName = fullName;
        this.FullPositionName = fullPositionName;
        this.CommitteePositionsCode = committeePositionsCode;
        this.CommitteePositionsName = committeePositionsName;
        this.GroupType = groupType;
        this.Sequence = sequence;

        return this;
    }

    public bool IsCommittee()
    {
        var committeePosition =
            this.CommitteePositions.Values
                .GetValueOrDefault(PositionOnBoard.IsCommittee);

        if (committeePosition is null)
        {
            return false;
        }

        if (committeePosition.Value is null)
        {
            return false;
        }

        var isCommittee = (JsonElement)committeePosition.Value;

        return isCommittee.GetBoolean();
    }
}