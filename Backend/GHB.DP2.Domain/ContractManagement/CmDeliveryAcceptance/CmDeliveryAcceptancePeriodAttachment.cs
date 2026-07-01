namespace GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct CmDeliveryAcceptancePeriodAttachmentId
{
    public static CmDeliveryAcceptancePeriodAttachmentId New() => From(Guid.CreateVersion7());
}

public class CmDeliveryAcceptancePeriodAttachment : AuditableEntity<CmDeliveryAcceptancePeriodAttachmentId>
{
    public override CmDeliveryAcceptancePeriodAttachmentId Id { get; init; }

    public CmDeliveryAcceptancePeriodId CmDeliveryAcceptancePeriodId { get; init; }

    public ParameterCode DocumentTypeCode { get; private set; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public bool IsPublic { get; private set; }

    public int Sequence { get; private set; }

    public virtual CmDeliveryAcceptancePeriod CmDeliveryAcceptancePeriod { get; init; }

    public virtual SuParameter DocumentType { get; init; }

    public static CmDeliveryAcceptancePeriodAttachment Create(
        CmDeliveryAcceptancePeriodId cmDeliveryAcceptancePeriodId,
        ParameterCode documentTypeCode,
        FileId fileId,
        string fileName,
        int sequence,
        bool isPublic)
    {
        return new CmDeliveryAcceptancePeriodAttachment
        {
            Id = CmDeliveryAcceptancePeriodAttachmentId.New(),
            CmDeliveryAcceptancePeriodId = cmDeliveryAcceptancePeriodId,
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            FileId = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public CmDeliveryAcceptancePeriodAttachment SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public CmDeliveryAcceptancePeriodAttachment SetIsPublic(bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }

    public Unit SetDocumentType(ParameterCode documentTypeCode)
    {
        this.DocumentTypeCode = documentTypeCode;

        return unit;
    }
}
