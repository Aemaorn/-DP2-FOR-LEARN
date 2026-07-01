namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Dto;

using System.Text.Json.Serialization;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using GHB.DP2.Domain.SystemUtility;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Acceptance), nameof(Acceptance))]
[JsonDerivedType(typeof(Redelivery), nameof(Redelivery))]
public abstract class RedeliveryBase
{
    public abstract RedeliveryCorrection MapToEntity();

    public static RedeliveryBase? FromEntity(RedeliveryCorrection? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return entity.Type switch
        {
            RedeliveryType.Acceptance => Acceptance.FromEntity(entity),
            RedeliveryType.Redelivery => Redelivery.FromEntity(entity),
            _ => null,
        };
    }
}

/// <summary>
/// การตรวจรับสัญญาแลกเปลี่ยน
/// </summary>
public class Acceptance : RedeliveryBase
{
    public string Description { get; init; }

    public RentalDurationInfo RentalDuration { get; init; }

    public override RedeliveryCorrection MapToEntity()
    {
        return RedeliveryCorrection.CreateAcceptance(
            this.Description,
            this.RentalDuration);
    }

    public new static Acceptance? FromEntity(RedeliveryCorrection entity)
    {
        if (entity.Description is null && entity.RentalDuration is null)
        {
            return null;
        }

        return new Acceptance
        {
            Description = entity.Description ?? string.Empty,
            RentalDuration = entity.RentalDuration ?? RentalDurationInfo.Default,
        };
    }
}

/// <summary>
/// การตรวจรับสัญญาเช่ารถยนต์
/// </summary>
public class Redelivery : RedeliveryBase
{
    public int RedeliveryDeadline { get; init; }

    public string RedeliveryDeadlineTypeCode { get; init; }

    public int CorrectionDue { get; init; }

    public string CorrectionDueTypeCode { get; init; }

    public override RedeliveryCorrection MapToEntity()
    {
        return RedeliveryCorrection.CreateRedelivery(
            this.RedeliveryDeadline,
            string.IsNullOrWhiteSpace(this.RedeliveryDeadlineTypeCode) ? null : ParameterCode.From(this.RedeliveryDeadlineTypeCode),
            this.CorrectionDue,
            string.IsNullOrWhiteSpace(this.CorrectionDueTypeCode) ? null : ParameterCode.From(this.CorrectionDueTypeCode));
    }

    public new static Redelivery? FromEntity(RedeliveryCorrection entity)
    {
        if (entity.RedeliveryDeadline is null &&
            entity.RedeliveryDeadlineTypeCode is null &&
            entity.CorrectionDue is null &&
            entity.CorrectionDueTypeCode is null)
        {
            return null;
        }

        return new Redelivery
        {
            RedeliveryDeadline = entity.RedeliveryDeadline ?? 0,
            RedeliveryDeadlineTypeCode = entity.RedeliveryDeadlineTypeCode?.Value ?? string.Empty,
            CorrectionDue = entity.CorrectionDue ?? 0,
            CorrectionDueTypeCode = entity.CorrectionDueTypeCode?.Value ?? string.Empty,
        };
    }
}