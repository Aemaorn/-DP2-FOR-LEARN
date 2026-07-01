namespace GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractDraftVendorEditComponentId
{
    public static ContractDraftVendorEditComponentId New() => From(Guid.CreateVersion7());
}

public partial class CaContractDraftVendorEditComponent : AuditableEntity<ContractDraftVendorEditComponentId>
{
    public override ContractDraftVendorEditComponentId Id { get; init; }

    public ContractDraftVendorEditId ContractDraftVendorEditId { get; init; }

    public string ComponentCode { get; private set; }

    public string ComponentName { get; private set; }

    public bool IsEdited { get; private set; }

    public virtual CaContractDraftVendorEdit CaContractDraftVendorEdit { get; init; }

    public CaContractDraftVendorEditComponent SetComponentCode(string componentCode)
    {
        this.ComponentCode = componentCode;

        return this;
    }

    public CaContractDraftVendorEditComponent SetComponentName(string componentName)
    {
        this.ComponentName = componentName;

        return this;
    }

    public CaContractDraftVendorEditComponent SetIsEdited(bool isEdited)
    {
        this.IsEdited = isEdited;

        return this;
    }

    public static CaContractDraftVendorEditComponent Create(
        ContractDraftVendorEditId contractDraftVendorEditId,
        string componentCode,
        string componentName,
        bool isEdited = false)
    {
        return new CaContractDraftVendorEditComponent
        {
            Id = ContractDraftVendorEditComponentId.New(),
            ContractDraftVendorEditId = contractDraftVendorEditId,
            ComponentCode = componentCode,
            ComponentName = componentName,
            IsEdited = isEdited,
        };
    }
}
