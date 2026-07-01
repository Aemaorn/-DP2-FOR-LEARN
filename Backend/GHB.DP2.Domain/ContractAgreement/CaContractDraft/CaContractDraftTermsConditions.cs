namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractDraftTermsConditionsId
{
    public static ContractDraftTermsConditionsId New() => From(Guid.CreateVersion7());
}

/// <summary>
/// เงื่อนไข การรับประกัน และหลักประกัน
/// </summary>
public partial class CaContractDraftTermsConditions : AuditableEntity<ContractDraftTermsConditionsId>
{
    public override ContractDraftTermsConditionsId Id { get; init; }

    public ContractDraftVendorId ContractDraftVendorId { get; init; }

    public ParameterCode? DefectWarrantyTypeCode { get; private set; }

    public AdvancePayment AdvancePayment { get; private set; }

    public Warranty Warranty { get; private set; }

    public Penalty Penalty { get; private set; }

    public Guarantee Guarantee { get; private set; }

    public RetentionPayment RetentionPayment { get; private set; }

    public RedeliveryCorrection? RedeliveryCorrection { get; private set; }

    public virtual CaContractDraftVendor ContractDraftVendor { get; init; }

    public virtual SuParameter? DefectWarrantyTypeNavigation { get; init; }

    public CaContractDraftTermsConditions SetDefectWarrantyTypeCode(ParameterCode? defectWarrantyTypeCode)
    {
        this.DefectWarrantyTypeCode = defectWarrantyTypeCode;

        return this;
    }

    public CaContractDraftTermsConditions SetAdvancePayment(AdvancePayment advancePayment)
    {
        this.AdvancePayment = advancePayment;

        return this;
    }

    public CaContractDraftTermsConditions SetRetentionPayment(RetentionPayment retentionPayment)
    {
        this.RetentionPayment = retentionPayment;

        return this;
    }

    public CaContractDraftTermsConditions SetWarranty(Warranty warranty)
    {
        this.Warranty = warranty;

        return this;
    }

    public CaContractDraftTermsConditions SetPenalty(Penalty penalty)
    {
        this.Penalty = penalty;

        return this;
    }

    public CaContractDraftTermsConditions SetGuarantee(Guarantee guarantee)
    {
        this.Guarantee = guarantee;

        return this;
    }

    public CaContractDraftTermsConditions SetRedeliveryCorrection(RedeliveryCorrection? redeliveryCorrection)
    {
        this.RedeliveryCorrection = redeliveryCorrection;

        return this;
    }

    public static CaContractDraftTermsConditions Create()
    {
        return new CaContractDraftTermsConditions
        {
            Id = ContractDraftTermsConditionsId.New(),
            Penalty = Penalty.Default,
            Guarantee = Guarantee.Default,
            Warranty = Warranty.Default,
            RedeliveryCorrection = RedeliveryCorrection.Default,
            AdvancePayment = AdvancePayment.Default,
            RetentionPayment = RetentionPayment.Default,
        };
    }
}