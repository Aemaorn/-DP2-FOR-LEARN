namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentWaiveOrReducePenalty;

using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct WaiveOrReducePenaltyDocumentHistoryId
{
    public static WaiveOrReducePenaltyDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum WaiveOrReducePenaltyDocumentType
{
    WaiveOrReducePenalty,
    Approved,
}

public class CamContractAmendmentWaiveOrReducePenaltyDocumentHistory : DocumentHistory<WaiveOrReducePenaltyDocumentHistoryId>
{
    public override WaiveOrReducePenaltyDocumentHistoryId Id { get; init; }

    public WaiveOrReducePenaltyDocumentType DocumentType { get; init; }

    public CamContractAmendmentWaiveOrReducePenaltyStatus StatusState { get; init; }

    public static CamContractAmendmentWaiveOrReducePenaltyDocumentHistory Create(
        WaiveOrReducePenaltyDocumentType documentType,
        CamContractAmendmentWaiveOrReducePenaltyStatus statusState,
        string version,
        Codehard.FileService.Contracts.ValueObjects.FileId fileId,
        bool isReplace)
    {
        return new CamContractAmendmentWaiveOrReducePenaltyDocumentHistory
        {
            Id = WaiveOrReducePenaltyDocumentHistoryId.New(),
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