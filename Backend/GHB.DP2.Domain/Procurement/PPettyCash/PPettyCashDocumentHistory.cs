namespace GHB.DP2.Domain.Procurement.PPettyCash;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPettyCashDocumentHistoryId
{
    public static PPettyCashDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public class PPettyCashDocumentHistory : DocumentHistory<PPettyCashDocumentHistoryId>
{
    public override PPettyCashDocumentHistoryId Id { get; init; }

    public PettyCashStatus StatusState { get; init; }

    public static PPettyCashDocumentHistory Create(
        PettyCashStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace = false)
    {
        return new PPettyCashDocumentHistory
        {
            Id = PPettyCashDocumentHistoryId.New(),
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}