namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentExtendChange;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct ContractAmendmentExtendChangeDocumentHistoryId
{
    public static ContractAmendmentExtendChangeDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum ExtendChangeAcceptorDocumentType
{
    ExtendChange,
    Approved,
}

public class CamContractAmendmentExtendChangeDocumentHistory : DocumentHistory<ContractAmendmentExtendChangeDocumentHistoryId>
{
    public override ContractAmendmentExtendChangeDocumentHistoryId Id { get; init; }

    public ExtendChangeAcceptorDocumentType DocumentType { get; init; }

    public ContractAmendmentExtendChangeStatus StatusState { get; init; }

    public static CamContractAmendmentExtendChangeDocumentHistory Create(
        ExtendChangeAcceptorDocumentType documentType,
        ContractAmendmentExtendChangeStatus statusState,
        string version,
        Codehard.FileService.Contracts.ValueObjects.FileId fileId,
        bool isReplace)
    {
        return new CamContractAmendmentExtendChangeDocumentHistory
        {
            Id = ContractAmendmentExtendChangeDocumentHistoryId.New(),
            DocumentType = documentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace,
        };
    }

    /// <summary>
    /// Updates the FileId to point to a different file (used for versioning).
    /// </summary>
    public void UpdateFileId(Codehard.FileService.Contracts.ValueObjects.FileId newFileId)
    {
        this.FileId = newFileId;
    }
}