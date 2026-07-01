namespace GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmContractGuaranteeReturnDocumentHistoryId
{
    public static CmContractGuaranteeReturnDocumentHistoryId New() => From(Guid.CreateVersion7());
}

public enum CmContractGuaranteeReturnDocumentType
{
    ApprovalCmContractGuaranteeReturn,
    CmContractGuaranteeReturnResule, // พี่บิ๊กบอกให้ใช้ Typo ไปเลย ถ้าจะแก้ไปถามพี่บิ๊กเอาเอง
}

public class CmContractGuaranteeReturnDocumentHistory : DocumentHistory<CmContractGuaranteeReturnDocumentHistoryId>
{
    public override CmContractGuaranteeReturnDocumentHistoryId Id { get; init; }

    public CmContractGuaranteeReturnDocumentType DocumentType { get; init; }

    public CmContractGuaranteeReturnStatus StatusState { get; init; }

    public virtual CmContractGuaranteeReturn CmContractGuaranteeReturn { get; init; }

    public static CmContractGuaranteeReturnDocumentHistory Create(
        CmContractGuaranteeReturnDocumentType documentType,
        CmContractGuaranteeReturnStatus statusState,
        string version,
        FileId fileId,
        bool isReplaced = false)
    {
        return new CmContractGuaranteeReturnDocumentHistory
        {
            Id = CmContractGuaranteeReturnDocumentHistoryId.New(),
            DocumentType = documentType,
            StatusState = statusState,
            Version = version,
            FileId = fileId,
            IsReplaced = isReplaced,
        };
    }
}