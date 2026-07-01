namespace GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractDraftEditTermsConditionsId
{
    public static ContractDraftEditTermsConditionsId New() => From(Guid.CreateVersion7());
}

/// <summary>
/// เงื่อนไข การรับประกัน และหลักประกัน (Edit)
/// </summary>
public partial class CaContractDraftEditTermsConditions : AuditableEntity<ContractDraftEditTermsConditionsId>
{
    public override ContractDraftEditTermsConditionsId Id { get; init; }

    public ContractDraftVendorEditId ContractDraftVendorEditId { get; init; }

    public ParameterCode? DefectWarrantyTypeCode { get; private set; }

    public AdvancePayment AdvancePayment { get; private set; }

    public Warranty Warranty { get; private set; }

    public Penalty Penalty { get; private set; }

    public Guarantee Guarantee { get; private set; }

    public RetentionPayment RetentionPayment { get; private set; }

    public RedeliveryCorrection? RedeliveryCorrection { get; private set; }

    public virtual CaContractDraftVendorEdit CaContractDraftVendorEdit { get; init; }

    public virtual SuParameter? DefectWarrantyTypeNavigation { get; init; }

    public CaContractDraftEditTermsConditions SetDefectWarrantyTypeCode(ParameterCode? defectWarrantyTypeCode)
    {
        this.DefectWarrantyTypeCode = defectWarrantyTypeCode;

        return this;
    }

    public CaContractDraftEditTermsConditions SetAdvancePayment(AdvancePayment advancePayment)
    {
        this.AdvancePayment = advancePayment;

        return this;
    }

    public CaContractDraftEditTermsConditions SetRetentionPayment(RetentionPayment retentionPayment)
    {
        this.RetentionPayment = retentionPayment;

        return this;
    }

    public CaContractDraftEditTermsConditions SetWarranty(Warranty warranty)
    {
        this.Warranty = warranty;

        return this;
    }

    public CaContractDraftEditTermsConditions SetPenalty(Penalty penalty)
    {
        this.Penalty = penalty;

        return this;
    }

    public CaContractDraftEditTermsConditions SetGuarantee(Guarantee guarantee)
    {
        this.Guarantee = guarantee;

        return this;
    }

    public CaContractDraftEditTermsConditions SetRedeliveryCorrection(RedeliveryCorrection? redeliveryCorrection)
    {
        this.RedeliveryCorrection = redeliveryCorrection;

        return this;
    }

    public static CaContractDraftEditTermsConditions Create()
    {
        return new CaContractDraftEditTermsConditions
        {
            Id = ContractDraftEditTermsConditionsId.New(),
            Penalty = Penalty.Default,
            Guarantee = Guarantee.Default,
            Warranty = Warranty.Default,
            RedeliveryCorrection = RedeliveryCorrection.Default,
            AdvancePayment = AdvancePayment.Default,
            RetentionPayment = RetentionPayment.Default,
        };
    }
}
