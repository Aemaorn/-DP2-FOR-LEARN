namespace GHB.DP2.Domain.Procurement.PpAppoint;

using System.Text.Json;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Constants;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpAppointMedianPriceCommitteeId
{
    public static PpAppointMedianPriceCommitteeId New() => From(Guid.CreateVersion7());
}

public partial class PpAppointMedianPriceCommittee : AuditableEntity<PpAppointMedianPriceCommitteeId>, IHasSoftDelete
{
    public override PpAppointMedianPriceCommitteeId Id { get; init; }

    public PpAppointId PpAppointId { get; init; }

    public UserId SuUserId { get; init; }

    public string FullName { get; init; }

    public string FullPositionName { get; init; }

    public ParameterCode CommitteePositionsCode { get; private set; }

    public string CommitteePositionsName { get; private set; }

    public int Sequence { get; private set; }

    public virtual PpAppoint PpAppoint { get; init; }

    public virtual SuUser User { get; init; }

    public virtual SuParameter CommitteePositions { get; init; }

    public static PpAppointMedianPriceCommittee Create(
        PpAppointId ppAppointId,
        UserId suUserId,
        string fullName,
        string fullPositionName,
        ParameterCode committeePositionsCode,
        string committeePositionsName,
        int sequence)
    {
        return new PpAppointMedianPriceCommittee
        {
            Id = PpAppointMedianPriceCommitteeId.New(),
            PpAppointId = ppAppointId,
            SuUserId = suUserId,
            FullName = fullName,
            FullPositionName = fullPositionName,
            CommitteePositionsCode = committeePositionsCode,
            CommitteePositionsName = committeePositionsName,
            Sequence = sequence,
        };
    }

    public PpAppointMedianPriceCommittee Clone(PpAppointId newId)
    {
        return new PpAppointMedianPriceCommittee
        {
            Id = PpAppointMedianPriceCommitteeId.New(),
            PpAppointId = newId,
            SuUserId = this.SuUserId,
            FullName = this.FullName,
            FullPositionName = this.FullPositionName,
            CommitteePositionsCode = this.CommitteePositionsCode,
            CommitteePositionsName = this.CommitteePositionsName,
            Sequence = this.Sequence,
        };
    }

    public Unit UpdatePositionCode(
        ParameterCode committeePositionsCode)
    {
        this.CommitteePositionsCode = committeePositionsCode;

        return unit;
    }

    public PpAppointMedianPriceCommittee UpdateSequence(int sequence)
    {
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