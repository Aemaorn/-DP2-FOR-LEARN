namespace GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CaContractDraftEditVendorDocumentHistoryId
{
    public static CaContractDraftEditVendorDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum CaContractDraftEditVendorDocumentType
{
    Amendment,
    AmendmentApprovalRequest,
}

public class CaContractDraftEditVendorDocumentHistory : DocumentHistory<CaContractDraftEditVendorDocumentHistoryId>
{
    public override CaContractDraftEditVendorDocumentHistoryId Id { get; init; }

    public CaContractDraftEditVendorDocumentType DocumentType { get; init; }

    public ContractDraftVendorEditStatus StatusState { get; init; }

    public static CaContractDraftEditVendorDocumentHistory Create(
        CaContractDraftEditVendorDocumentType vendorDocumentType,
        ContractDraftVendorEditStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace)
    {
        return new CaContractDraftEditVendorDocumentHistory
        {
            Id = CaContractDraftEditVendorDocumentHistoryId.New(),
            DocumentType = vendorDocumentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}
