namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;

using GHB.DP2.Domain.SystemUtility;

public class Guarantee
{
    public bool? IsSubmitted { get; init; }

    public ParameterCode? TypeCode { get; init; }

    public decimal? Amount { get; init; }

    public decimal? Percentage { get; init; }

    public string? ReferenceNumber { get; init; }

    public DateTimeOffset? GuaranteeDate { get; init; }

    public ParameterCode? BankCode { get; init; }

    public string? BankBranch { get; init; }

    public string? BankAccountNumber { get; init; }

    public DateTimeOffset? BankCollateralStartDate { get; init; }

    public DateTimeOffset? BankCollateralEndDate { get; init; }

    public string? OtherDetails { get; init; }

    public virtual SuParameter? Type { get; init; }

    public virtual SuParameter? Bank { get; init; }

    public Guarantee()
    {
        // Ef Core constructor
    }

    public Guarantee(
        bool? isSubmitted,
        ParameterCode? typeCode,
        decimal? amount,
        decimal? percentage,
        string? referenceNumber,
        ParameterCode? bankCode,
        string? bankBranch,
        string? bankAccountNumber,
        DateTimeOffset? bankCollateralStartDate,
        DateTimeOffset? bankCollateralEndDate,
        DateTimeOffset? guaranteeDate,
        string? otherDetails)
    {
        this.IsSubmitted = isSubmitted;
        this.TypeCode = typeCode;
        this.Amount = amount;
        this.Percentage = percentage;
        this.ReferenceNumber = referenceNumber;
        this.BankCode = bankCode;
        this.BankBranch = bankBranch;
        this.BankAccountNumber = bankAccountNumber;
        this.BankCollateralStartDate = bankCollateralStartDate;
        this.BankCollateralEndDate = bankCollateralEndDate;
        this.GuaranteeDate = guaranteeDate;
        this.OtherDetails = otherDetails;
    }

    public static Guarantee Default =>
        new()
        {
            IsSubmitted = null,
            TypeCode = null,
            Amount = null,
            Percentage = null,
        };
}