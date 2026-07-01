namespace GHB.DP2.Domain.ContractManagement.CmContractTermination;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmContractTerminationAttachmentId
{
    public static CmContractTerminationAttachmentId New() => From(Guid.CreateVersion7());
}

public class CmContractTerminationAttachment : AuditableEntity<CmContractTerminationAttachmentId>
{
    public override CmContractTerminationAttachmentId Id { get; init; }

    public CmContractTerminationId CmContractTerminationId { get; init; }

    public ParameterCode DocumentTypeCode { get; private set; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public bool IsPublic { get; private set; }

    public int Sequence { get; private set; }

    public virtual CmContractTermination CmContractTermination { get; init; }

    public virtual SuParameter DocumentTypeCodeNavigation { get; init; }

    public static CmContractTerminationAttachment Create(
        CmContractTerminationId cmContractTerminationId,
        ParameterCode documentTypeCode,
        FileId fileId,
        string fileName,
        int sequence,
        bool isPublic)
    {
        return new CmContractTerminationAttachment
        {
            Id = CmContractTerminationAttachmentId.New(),
            CmContractTerminationId = cmContractTerminationId,
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            FileId = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public CmContractTerminationAttachment SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public CmContractTerminationAttachment SetIsPublic(bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }

    public CmContractTerminationAttachment SetDocumentType(ParameterCode documentTypeCode)
    {
        this.DocumentTypeCode = documentTypeCode;

        return this;
    }
}