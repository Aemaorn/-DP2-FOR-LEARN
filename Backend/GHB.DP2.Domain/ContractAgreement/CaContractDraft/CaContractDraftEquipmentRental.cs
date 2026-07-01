namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft;

using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft.SectionType;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractEquipmentRental
{
    public static ContractEquipmentRental New => From(Guid.CreateVersion7());
}

/// <summary>
/// ข้อมูลอุปกรณ์และเงื่อนไขการเช่า
/// </summary>
public partial class CaContractDraftEquipmentRental : AuditableEntity<ContractEquipmentRental>
{
    public override ContractEquipmentRental Id { get; init; }

    public ContractDraftVendorId ContractDraftVendorId { get; init; }

    public LeaseCopier CopierLease { get; private set; }

    public LeaseCar CarLease { get; private set; }

    public LeaseDuration LeaseDuration { get; private set; }

    public virtual CaContractDraftVendor ContractDraftVendor { get; init; }

    public CaContractDraftEquipmentRental SetCopierLease(LeaseCopier copierLease)
    {
        this.CopierLease = copierLease;

        return this;
    }

    public CaContractDraftEquipmentRental SetCarLease(LeaseCar carLease)
    {
        this.CarLease = carLease;

        return this;
    }

    public CaContractDraftEquipmentRental SetLeaseDuration(LeaseDuration leaseDuration)
    {
        this.LeaseDuration = leaseDuration;

        return this;
    }

    public static CaContractDraftEquipmentRental Create()
    {
        return new CaContractDraftEquipmentRental
        {
            Id = ContractEquipmentRental.New,
            CopierLease = LeaseCopier.Default(),
            CarLease = LeaseCar.Default(),
            LeaseDuration = LeaseDuration.Default(),
        };
    }
}