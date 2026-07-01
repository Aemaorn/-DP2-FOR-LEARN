namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;

public record RetentionPayment(
    bool? IsIncluded,
    decimal? Amount,
    decimal? Percentage)
{
    public static RetentionPayment Default => new(null, null, null);
}