namespace GHB.DP2.Domain.Plan;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PlanDocumentHistoryId
{
    public static PlanDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum PlanDocumentType
{
    Plan,
    Announcement,
}

public class PlanDocumentHistory : DocumentHistory<PlanDocumentHistoryId>
{
    public override PlanDocumentHistoryId Id { get; init; }

    public PlanDocumentType DocumentType { get; init; }

    public PlanStatus StatusState { get; init; }

    public virtual Plan Plan { get; init; }

    public static PlanDocumentHistory Create(
        PlanDocumentType documentType,
        PlanStatus statusState,
        string version,
        FileId fileId,
        bool isReplace)
    {
        return new PlanDocumentHistory
        {
            Id = PlanDocumentHistoryId.New(),
            DocumentType = documentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace,
        };
    }
}