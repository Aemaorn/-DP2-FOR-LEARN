namespace GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CaContractDraftEditVendorCheckerAttachmentId
{
    public static CaContractDraftEditVendorCheckerAttachmentId New() => From(Guid.CreateVersion7());
}

public class CaContractDraftEditVendorCheckerAttachments : AuditableEntity<CaContractDraftEditVendorCheckerAttachmentId>
{
    public ParameterCode DocumentTypeCode { get; private set; }

    public override CaContractDraftEditVendorCheckerAttachmentId Id { get; init; }

    public EntrepreneurAttachmentType Type { get; init; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public bool IsPublic { get; private set; }

    public int Sequence { get; private set; }

    public virtual SuParameter DocumentType { get; init; }

    public static CaContractDraftEditVendorCheckerAttachments Create(
        ParameterCode documentTypeCode,
        FileId fileId,
        string fileName,
        EntrepreneurAttachmentType type,
        int sequence,
        bool isPublic)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        return new CaContractDraftEditVendorCheckerAttachments
        {
            Id = CaContractDraftEditVendorCheckerAttachmentId.New(),
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            Type = type,
            FileId = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public CaContractDraftEditVendorCheckerAttachments SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public CaContractDraftEditVendorCheckerAttachments SetIsPublic(bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }

    public Unit SetDocumentType(ParameterCode documentTypeCode)
    {
        this.DocumentTypeCode = documentTypeCode;

        return unit;
    }

    public CaContractDraftEditVendorCheckerAttachments Clone()
    {
        return new CaContractDraftEditVendorCheckerAttachments
        {
            Id = CaContractDraftEditVendorCheckerAttachmentId.New(),
            Sequence = this.Sequence,
            DocumentTypeCode = this.DocumentTypeCode,
            FileId = this.FileId,
            FileName = this.FileName,
            IsPublic = this.IsPublic,
        };
    }
}
