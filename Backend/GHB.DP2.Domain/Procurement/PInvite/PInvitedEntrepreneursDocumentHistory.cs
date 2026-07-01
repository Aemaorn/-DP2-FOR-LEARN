namespace GHB.DP2.Domain.Procurement.PInvite;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PInvitedEntrepreneursDocumentHistoryId
{
    public static PInvitedEntrepreneursDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public class PInvitedEntrepreneursDocumentHistory : DocumentHistory<PInvitedEntrepreneursDocumentHistoryId>
{
    public override PInvitedEntrepreneursDocumentHistoryId Id { get; init; }

    public PInvitedEntrepreneursId PInvitedEntrepreneursId { get; init; }

    public PInviteStatus StatusState { get; init; }

    public virtual PInvitedEntrepreneurs InvitedEntrepreneur { get; private set; }

    public static PInvitedEntrepreneursDocumentHistory Create(
        PInvitedEntrepreneursId invitedEntrepreneursId,
        PInviteStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace = false)
    {
        return new PInvitedEntrepreneursDocumentHistory
        {
            Id = PInvitedEntrepreneursDocumentHistoryId.New(),
            PInvitedEntrepreneursId = invitedEntrepreneursId,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}
