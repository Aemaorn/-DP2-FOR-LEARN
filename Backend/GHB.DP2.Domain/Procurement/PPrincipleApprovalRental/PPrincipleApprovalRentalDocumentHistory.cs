namespace GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalRentalDocumentHistoryId
{
    public static PPrincipleApprovalRentalDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum PPrincipleApprovalRentalDocumentType
{
    Approval,
    Winner,
}

public class PPrincipleApprovalRentalDocumentHistory : DocumentHistory<PPrincipleApprovalRentalDocumentHistoryId>
{
    public override PPrincipleApprovalRentalDocumentHistoryId Id { get; init; }

    public PPrincipleApprovalRentalDocumentType DocumentType { get; init; }

    public PPrincipleApprovalRentalStatus StatusState { get; init; }

    public static PPrincipleApprovalRentalDocumentHistory Create(
        PPrincipleApprovalRentalDocumentType documentType,
        PPrincipleApprovalRentalStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace = false) => new()
        {
            Id = PPrincipleApprovalRentalDocumentHistoryId.New(),
            DocumentType = documentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
}