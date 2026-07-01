namespace GHB.DP2.Domain.Procurement.PPurchaseOrderApproval;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PurchaseOrderApprovalContractId
{
    public static PurchaseOrderApprovalContractId New() => From(Guid.CreateVersion7());
}

public partial class PPurchaseOrderApprovalContract : AuditableEntity<PurchaseOrderApprovalContractId>, IHasSoftDelete
{
    public override PurchaseOrderApprovalContractId Id { get; init; }

    public PpTorDraftBudgetId? TorDraftBudgetId { get; private set; }

    public PPrincipleApprovalRentalBudgetId? PrincipleApprovalRentalBudgetId { get; private set; }

    public PpPurchaseRequisitionBudgetId? PpPurchaseRequisitionBudgetId { get; private set; }

    public int Sequence { get; private set; }

    public PurchaseOrderEntrepreneurId? PurchaseOrderEntrepreneurId { get; private set; }

    public PPrincipleApprovalRentalEntrepreneursId? PrincipleApprovalRentalEntrepreneursId { get; private set; }

    public PPurchaseOrderApprovalBudgetId? PPurchaseOrderApprovalBudgetId { get; private set; }

    public PPurchaseOrderApprovalEntrepreneursId? PPurchaseOrderApprovalEntrepreneursId { get; private set; }

    public string? ContractNumber { get; private set; }

    public bool HasEditContractNumber { get; private set; } = false;

    public decimal AgreedPrice { get; private set; }

    public string PoNumber { get; private set; }

    public string CommitteeType { get; private set; }

    public virtual PPurchaseOrderEntrepreneur? Entrepreneur { get; init; }

    public virtual PPrincipleApprovalRentalEntrepreneurs? PrincipleApprovalRentalEntrepreneurs { get; init; }

    public virtual PPurchaseOrderApprovalEntrepreneurs? PPurchaseOrderApprovalEntrepreneurs { get; init; }

    public virtual PPurchaseOrderApproval Approval { get; init; }

    public virtual PpTorDraftBudget? Budget { get; init; }

    public virtual PpPurchaseRequisitionBudget? PpPurchaseRequisitionBudget { get; init; }

    public virtual PPrincipleApprovalRentalBudget? PrincipleApprovalRentalBudget { get; init; }

    public virtual PPurchaseOrderApprovalBudget PPurchaseOrderApprovalBudget { get; init; }

    public record ContractInfoData(
        string? ContractNumber,
        bool HasEditContractNumber,
        decimal AgreedPrice,
        string PoNumber,
        string CommitteeType
    );

    public PPurchaseOrderApprovalContract SetPurchaseOrderData(
        PpTorDraftBudgetId torDraftBudgetId,
        PurchaseOrderEntrepreneurId purchaseOrderEntrepreneurId)
    {
        this.TorDraftBudgetId = torDraftBudgetId;
        this.PurchaseOrderEntrepreneurId = purchaseOrderEntrepreneurId;

        return this;
    }

    public PPurchaseOrderApprovalContract SetPrincipleApprovalRentalData(
        PPrincipleApprovalRentalBudgetId rentalBudgetId,
        PPrincipleApprovalRentalEntrepreneursId principleApprovalRentalEntrepreneursId)
    {
        this.PrincipleApprovalRentalBudgetId = rentalBudgetId;
        this.PrincipleApprovalRentalEntrepreneursId = principleApprovalRentalEntrepreneursId;

        return this;
    }

    public PPurchaseOrderApprovalContract SetPurchasePurchaseRequisitionData(
        PpPurchaseRequisitionBudgetId purchaseRequisitionBudgetId,
        PurchaseOrderEntrepreneurId purchaseOrderEntrepreneurId)
    {
        this.PpPurchaseRequisitionBudgetId = purchaseRequisitionBudgetId;
        this.PurchaseOrderEntrepreneurId = purchaseOrderEntrepreneurId;

        return this;
    }

    public PPurchaseOrderApprovalContract SetPPurchaseOrderApprovalData(
    PPurchaseOrderApprovalBudgetId purchaseOrderApprovalBudgetId,
    PPurchaseOrderApprovalEntrepreneursId purchaseOrderApprovalEntrepreneursId)
    {
        this.PPurchaseOrderApprovalBudgetId = purchaseOrderApprovalBudgetId;
        this.PPurchaseOrderApprovalEntrepreneursId = purchaseOrderApprovalEntrepreneursId;

        return this;
    }

    public static PPurchaseOrderApprovalContract Create(
        int sequence,
        ContractInfoData infoData)
    {
        return new PPurchaseOrderApprovalContract
        {
            Id = PurchaseOrderApprovalContractId.New(),
            Sequence = sequence,
            ContractNumber = infoData.ContractNumber,
            HasEditContractNumber = infoData.HasEditContractNumber,
            AgreedPrice = infoData.AgreedPrice,
            PoNumber = infoData.PoNumber,
            CommitteeType = infoData.CommitteeType ?? string.Empty,
        };
    }

    public PPurchaseOrderApprovalContract Update(
        int sequence)
    {
        this.Sequence = sequence;

        return this;
    }

    public PPurchaseOrderApprovalContract SetContractInformation(
        string? contractNumber,
        bool hasEditContractNumber,
        decimal agreedPrice,
        string poNumber,
        string committeeType)
    {
        this.ContractNumber = contractNumber;
        this.HasEditContractNumber = hasEditContractNumber;
        this.AgreedPrice = agreedPrice;
        this.PoNumber = poNumber;
        this.CommitteeType = committeeType ?? string.Empty;

        return this;
    }
}