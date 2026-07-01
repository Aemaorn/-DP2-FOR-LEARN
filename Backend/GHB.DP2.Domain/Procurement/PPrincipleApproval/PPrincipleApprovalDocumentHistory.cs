namespace GHB.DP2.Domain.Procurement.PPrincipleApproval;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalDocumentHistoryId
{
    public static PPrincipleApprovalDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalDocumentHistory : DocumentHistory<PPrincipleApprovalDocumentHistoryId>
{
    public override PPrincipleApprovalDocumentHistoryId Id { get; init; }

    public PPrincipleApprovalStatus StatusState { get; init; }

    public static PPrincipleApprovalDocumentHistory Create(
        PPrincipleApprovalStatus statusState,
        string version,
        FileId fileId,
        bool? isReplace = false) => new()
        {
            Id = PPrincipleApprovalDocumentHistoryId.New(),
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplace ?? false,
        };
}