namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;

using GHB.DP2.Domain.SystemUtility;

public enum RedeliveryType
{
    Acceptance,
    Redelivery,
}

public class RedeliveryCorrection
{
    public RedeliveryType Type { get; init; }

    public string? Description { get; init; }

    public RentalDurationInfo? RentalDuration { get; init; }

    public int? RedeliveryDeadline { get; init; }

    public ParameterCode? RedeliveryDeadlineTypeCode { get; init; }

    public int? CorrectionDue { get; init; }

    public ParameterCode? CorrectionDueTypeCode { get; init; }

    public virtual SuParameter? RedeliveryDeadlineType { get; init; }

    public virtual SuParameter? CorrectionDueType { get; init; }

    public RedeliveryCorrection()
    {
        // Ef Core constructor
    }

    public static RedeliveryCorrection CreateAcceptance(
        string? description,
        RentalDurationInfo? rentalDuration)
    {
        return new RedeliveryCorrection
        {
            Type = RedeliveryType.Acceptance,
            Description = description,
            RentalDuration = rentalDuration,
        };
    }

    public static RedeliveryCorrection CreateRedelivery(
        int? redeliveryDeadline,
        ParameterCode? redeliveryDeadlineTypeCode,
        int? correctionDue,
        ParameterCode? correctionDueTypeCode)
    {
        return new RedeliveryCorrection
        {
            Type = RedeliveryType.Redelivery,
            RedeliveryDeadline = redeliveryDeadline,
            RedeliveryDeadlineTypeCode = redeliveryDeadlineTypeCode,
            CorrectionDue = correctionDue,
            CorrectionDueTypeCode = correctionDueTypeCode,
        };
    }

    public static RedeliveryCorrection Default =>
        new()
        {
            Type = RedeliveryType.Acceptance,
            Description = null,
            RentalDuration = null,
            RedeliveryDeadline = null,
            RedeliveryDeadlineTypeCode = null,
            CorrectionDue = null,
            CorrectionDueTypeCode = null,
        };
}