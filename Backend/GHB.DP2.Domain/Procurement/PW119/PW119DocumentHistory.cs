namespace GHB.DP2.Domain.Procurement.Pw119;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct Pw119DocumentHistoryId
{
    public static Pw119DocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum Pw119DocumentType
{
    Approval,
    WinnerAnnouncement,
}

public class Pw119DocumentHistory : DocumentHistory<Pw119DocumentHistoryId>
{
    public override Pw119DocumentHistoryId Id { get; init; }

    public Pw119DocumentType DocumentType { get; init; }

    public Pw119Status StatusState { get; init; }

    public static Pw119DocumentHistory Create(
        Pw119DocumentType documentType,
        Pw119Status statusState,
        string version,
        FileId fileId,
        bool? isReplace = false)
    {
        return new Pw119DocumentHistory
        {
            Id = Pw119DocumentHistoryId.New(),
            DocumentType = documentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}