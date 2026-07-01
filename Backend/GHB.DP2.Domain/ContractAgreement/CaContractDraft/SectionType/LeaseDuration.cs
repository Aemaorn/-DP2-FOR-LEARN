namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;

using GHB.DP2.Domain.SystemUtility;

public class LeaseDuration
{
    public RentalDurationInfo? Duration { get; init; }

    public ParameterCode? CountFromConditionCode { get; init; }

    public virtual SuParameter? CountFromCondition { get; init; }

    public LeaseDuration()
    {
        // Ef Core constructor
    }

    public LeaseDuration(RentalDurationInfo duration, ParameterCode? countFromConditionCode)
    {
        this.Duration = duration;
        this.CountFromConditionCode = countFromConditionCode;
    }

    public static LeaseDuration Default()
    {
        return new LeaseDuration
        {
            Duration = null,
            CountFromConditionCode = null,
        };
    }
}