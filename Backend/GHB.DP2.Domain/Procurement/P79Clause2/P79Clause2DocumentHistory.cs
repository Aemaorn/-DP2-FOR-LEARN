namespace GHB.DP2.Domain.Procurement.P79Clause2;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct P79Clause2DocumentHistoryId
{
    public static P79Clause2DocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum P79Clause2DocumentType
{
    Approval,
    WinnerAnnouncement,
}

public class P79Clause2DocumentHistory : DocumentHistory<P79Clause2DocumentHistoryId>
{
    public override P79Clause2DocumentHistoryId Id { get; init; }

    public P79Clause2DocumentType DocumentType { get; init; }

    public P79Clause2Status StatusState { get; init; }

    public static P79Clause2DocumentHistory Create(
        P79Clause2DocumentType documentType,
        P79Clause2Status statusState,
        string version,
        FileId fileId,
        bool? isReplace = false)
    {
        return new P79Clause2DocumentHistory
        {
            Id = P79Clause2DocumentHistoryId.New(),
            DocumentType = documentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}