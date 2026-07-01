namespace GHB.DP2.Domain.Procurement.PpTorDraft;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PpTorDraftDocumentHistoryId
{
    public static PpTorDraftDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum PpTorDraftDocumentType
{
    Approval,
    Tor,
}

public class PpTorDraftDocumentHistory : DocumentHistory<PpTorDraftDocumentHistoryId>
{
    public override PpTorDraftDocumentHistoryId Id { get; init; }

    public PpTorDraftDocumentType DocumentType { get; init; }

    public TorDraftStatus StatusState { get; init; }

    public static PpTorDraftDocumentHistory Create(
        PpTorDraftDocumentType documentType,
        TorDraftStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace = false)
    {
        return new PpTorDraftDocumentHistory
        {
            Id = PpTorDraftDocumentHistoryId.New(),
            DocumentType = documentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}