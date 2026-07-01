namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;

using GHB.DP2.Domain.SystemUtility;

public class Penalty
{
    public bool IsPenalty { get; init; }

    public ParameterCode? TypeCode { get; init; }

    public decimal? Rate { get; init; }

    public decimal? Amount { get; init; }

    public ParameterCode? RateTypeCode { get; init; }

    public virtual SuParameter? Type { get; init; }

    public virtual SuParameter? RateType { get; init; }

    public Penalty()
    {
        // Ef Core constructor
    }

    public Penalty(
        bool isPenalty,
        ParameterCode? typeCode,
        decimal rate,
        decimal amount,
        ParameterCode? rateTypeCode)
    {
        this.IsPenalty = isPenalty;
        this.TypeCode = typeCode;
        this.Rate = rate;
        this.Amount = amount;
        this.RateTypeCode = rateTypeCode;
    }

    public Penalty(
        bool isPenalty)
    {
        this.IsPenalty = isPenalty;
    }

    public static Penalty Default => new()
    {
        IsPenalty = true,
        TypeCode = null,
        Rate = null,
        Amount = null,
        RateTypeCode = null,
    };
}