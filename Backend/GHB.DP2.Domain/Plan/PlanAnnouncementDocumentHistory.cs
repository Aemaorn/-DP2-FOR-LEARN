namespace GHB.DP2.Domain.Plan;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PlanAnnouncementDocumentHistoryId
{
    public static PlanAnnouncementDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum PlanAnnouncementDocumentType
{
    Approve,
    Announcement,
}

public class PlanAnnouncementDocumentHistory : DocumentHistory<PlanAnnouncementDocumentHistoryId>
{
    public override PlanAnnouncementDocumentHistoryId Id { get; init; }

    public PlanAnnouncementDocumentType DocumentType { get; init; }

    public PlanAnnouncementStatus StatusState { get; init; }

    public static PlanAnnouncementDocumentHistory Create(
        PlanAnnouncementDocumentType documentType,
        PlanAnnouncementStatus statusState,
        string version,
        FileId fileId,
        bool isReplace)
    {
        return new PlanAnnouncementDocumentHistory
        {
            Id = PlanAnnouncementDocumentHistoryId.New(),
            DocumentType = documentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace,
        };
    }
}