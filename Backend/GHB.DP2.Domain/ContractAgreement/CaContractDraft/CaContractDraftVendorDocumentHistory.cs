namespace GHB.DP2.Domain.ContractAgreement.CaContractDraft;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CaContractDraftVendorDocumentHistoryId
{
    public static CaContractDraftVendorDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum CaContractDraftVendorDocumentType
{
    ContractDraft,
    ApprovalContractDraft, // ลงนาม
    ConfidentialContractDraft, // รักษาความลับ
}

public class CaContractDraftVendorDocumentHistory : DocumentHistory<CaContractDraftVendorDocumentHistoryId>
{
    public override CaContractDraftVendorDocumentHistoryId Id { get; init; }

    public CaContractDraftVendorDocumentType DocumentType { get; init; }

    public ContractDraftVendorStatus StatusState { get; init; }

    public static CaContractDraftVendorDocumentHistory Create(
        CaContractDraftVendorDocumentType vendorDocumentType,
        ContractDraftVendorStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace)
    {
        return new CaContractDraftVendorDocumentHistory
        {
            Id = CaContractDraftVendorDocumentHistoryId.New(),
            DocumentType = vendorDocumentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}