namespace GHB.DP2.Domain.ContractManagement.CaContractDraftVendorAmendment;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CaContractDraftVendorAmendmentDocumentHistoryId
{
    public static CaContractDraftVendorAmendmentDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum CaContractDraftVendorAmendmentDocumentType
{
    Amendment,
}

public class CaContractDraftVendorAmendmentDocumentHistory : DocumentHistory<CaContractDraftVendorAmendmentDocumentHistoryId>
{
    public override CaContractDraftVendorAmendmentDocumentHistoryId Id { get; init; }

    public CaContractDraftVendorAmendmentDocumentType DocumentType { get; init; }

    public CaContractDraftVendorAmendmentStatus StatusState { get; init; }

    public static CaContractDraftVendorAmendmentDocumentHistory Create(
        CaContractDraftVendorAmendmentStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace)
    {
        return new CaContractDraftVendorAmendmentDocumentHistory
        {
            Id = CaContractDraftVendorAmendmentDocumentHistoryId.New(),
            DocumentType = CaContractDraftVendorAmendmentDocumentType.Amendment,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}
