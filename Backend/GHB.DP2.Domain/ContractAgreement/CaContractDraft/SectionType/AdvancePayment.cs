namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;

using GHB.DP2.Domain.SystemUtility;

public record AdvancePayment(
    bool? IsIncluded,
    decimal? Amount,
    decimal? Percentage,
    int? DueDate,
    ParameterCode? ConditionCode)
{
    public static AdvancePayment Default => new(null, null, null, null, null);
}