namespace GHB.DP2.Domain.ContractAgreement.CaContractInvitation;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CaContractInvitationDocumentHistoryId
{
    public static CaContractInvitationDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum CaContractInvitationDocumentType
{
    ContractInvitation,
}

public class CaContractInvitationVendorsDocumentHistory : DocumentHistory<CaContractInvitationDocumentHistoryId>
{
    public override CaContractInvitationDocumentHistoryId Id { get; init; }

    public CaContractInvitationDocumentType DocumentType { get; init; }

    public ContractInvitationStatus StatusState { get; init; }

    public static CaContractInvitationVendorsDocumentHistory Create(
        CaContractInvitationDocumentType documentType,
        ContractInvitationStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace = false)
    {
        return new CaContractInvitationVendorsDocumentHistory
        {
            Id = CaContractInvitationDocumentHistoryId.New(),
            DocumentType = documentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
    }
}