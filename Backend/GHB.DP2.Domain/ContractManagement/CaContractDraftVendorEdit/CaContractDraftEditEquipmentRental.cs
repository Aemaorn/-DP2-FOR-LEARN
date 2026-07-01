namespace GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractDraftEditEquipmentRentalId
{
    public static ContractDraftEditEquipmentRentalId New => From(Guid.CreateVersion7());
}

/// <summary>
/// ข้อมูลอุปกรณ์และเงื่อนไขการเช่า (Edit)
/// </summary>
public partial class CaContractDraftEditEquipmentRental : AuditableEntity<ContractDraftEditEquipmentRentalId>
{
    public override ContractDraftEditEquipmentRentalId Id { get; init; }

    public ContractDraftVendorEditId ContractDraftVendorEditId { get; init; }

    public LeaseCopier CopierLease { get; private set; }

    public LeaseCar CarLease { get; private set; }

    public LeaseDuration LeaseDuration { get; private set; }

    public virtual CaContractDraftVendorEdit CaContractDraftVendorEdit { get; init; }

    public CaContractDraftEditEquipmentRental SetCopierLease(LeaseCopier copierLease)
    {
        this.CopierLease = copierLease;

        return this;
    }

    public CaContractDraftEditEquipmentRental SetCarLease(LeaseCar carLease)
    {
        this.CarLease = carLease;

        return this;
    }

    public CaContractDraftEditEquipmentRental SetLeaseDuration(LeaseDuration leaseDuration)
    {
        this.LeaseDuration = leaseDuration;

        return this;
    }

    public static CaContractDraftEditEquipmentRental Create()
    {
        return new CaContractDraftEditEquipmentRental
        {
            Id = ContractDraftEditEquipmentRentalId.New,
            CopierLease = LeaseCopier.Default(),
            CarLease = LeaseCar.Default(),
            LeaseDuration = LeaseDuration.Default(),
        };
    }
}
