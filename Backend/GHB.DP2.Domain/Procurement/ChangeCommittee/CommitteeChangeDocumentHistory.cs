namespace GHB.DP2.Domain.Procurement.ChangeCommittee;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CommitteeChangeDocumentHistoryId
{
    public static CommitteeChangeDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public class CommitteeChangeDocumentHistory : DocumentHistory<CommitteeChangeDocumentHistoryId>
{
    public override CommitteeChangeDocumentHistoryId Id { get; init; }

    public CommitteeChangeStatus StatusState { get; init; }

    public static CommitteeChangeDocumentHistory Create(
        CommitteeChangeStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace = false)
    {
        return new CommitteeChangeDocumentHistory
        {
            Id = CommitteeChangeDocumentHistoryId.New(),
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}
