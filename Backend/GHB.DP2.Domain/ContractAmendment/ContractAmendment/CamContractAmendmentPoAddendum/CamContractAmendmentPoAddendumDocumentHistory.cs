namespace GHB.DP2.Domain.ContractAmendment.ContractAmendment.CamContractAmendmentPoAddendum;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CamContractAmendmentPoAddendumDocumentHistoryId
{
    public static CamContractAmendmentPoAddendumDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum CamContractAmendmentPoAddendumDocumentType
{
    /// <summary>
    /// เอกสารบันทึกต่อท้ายสัญญา
    /// </summary>
    ContractAddendum,

    /// <summary>
    /// เอกสารขออนุมัติแก้ไขใบสั่ง/สัญญา
    /// </summary>
    ContractAmendmentRequest,
}

public class CamContractAmendmentPoAddendumDocumentHistory : DocumentHistory<CamContractAmendmentPoAddendumDocumentHistoryId>
{
    public override CamContractAmendmentPoAddendumDocumentHistoryId Id { get; init; }

    public CamContractAmendmentPoAddendumDocumentType DocumentType { get; init; }

    public CamContractAmendmentPoAddendumStatus StatusState { get; init; }

    public virtual CamContractAmendmentPoAddendum CamContractAmendmentPoAddendum { get; init; }

    public static CamContractAmendmentPoAddendumDocumentHistory Create(
        CamContractAmendmentPoAddendumDocumentType documentType,
        CamContractAmendmentPoAddendumStatus statusState,
        string version,
        FileId fileId,
        bool isReplaced = false)
    {
        return new CamContractAmendmentPoAddendumDocumentHistory
        {
            Id = CamContractAmendmentPoAddendumDocumentHistoryId.New(),
            DocumentType = documentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplaced,
        };
    }
}