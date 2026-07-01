namespace GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Constants;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using System.Text.Json;
using Vogen;

public enum GroupType
{
    /// <summary>
    /// ผู้ตรวจรับพัสดุ-คณะกรรมการตรวจรับพัสดุ
    /// </summary>
    InspectionCommittee,
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPurchaseOrderApprovalCommitteeId
{
    public static PPurchaseOrderApprovalCommitteeId New() => From(Guid.CreateVersion7());
}

public partial class PPurchaseOrderApprovalCommittee : AuditableEntity<PPurchaseOrderApprovalCommitteeId>
{
    public override PPurchaseOrderApprovalCommitteeId Id { get; init; }

    public PurchaseOrderApprovalId PurchaseOrderApprovalId { get; init; }

    public GroupType GroupType { get; init; }

    public UserId SuUserId { get; init; }

    public string FullName { get; private set; }

    public string FullPositionName { get; private set; }

    public ParameterCode CommitteePositionsCode { get; private set; }

    public string CommitteePositionsName { get; private set; }

    public int Sequence { get; private set; }

    public virtual PPurchaseOrderApproval PPurchaseOrderApproval { get; init; }

    public virtual SuUser User { get; init; }

    public virtual SuParameter CommitteePositions { get; init; }

    public static PPurchaseOrderApprovalCommittee Create(
    PurchaseOrderApprovalId purchaseOrderApprovalId,
    GroupType groupType,
    UserId suUserId,
    string fullName,
    string fullPositionName,
    ParameterCode committeePositionsCode,
    string committeePositionsName,
    int sequence)
    {
        return new PPurchaseOrderApprovalCommittee
        {
            Id = PPurchaseOrderApprovalCommitteeId.New(),
            PurchaseOrderApprovalId = purchaseOrderApprovalId,
            GroupType = groupType,
            SuUserId = suUserId,
            FullName = fullName,
            CommitteePositionsCode = committeePositionsCode,
            CommitteePositionsName = committeePositionsName,
            FullPositionName = fullPositionName,
            Sequence = sequence,
        };
    }

    public Unit Update(
    ParameterCode committeePositionsCode,
    string committeePositionsName,
    int sequence)
    {
        this.CommitteePositionsCode = committeePositionsCode;
        this.CommitteePositionsName = committeePositionsName;
        this.Sequence = sequence;

        return unit;
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
