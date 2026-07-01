namespace GHB.DP2.Domain.Procurement.PExpenseDisbursement;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PExpenseDisbursementAttachmentId
{
    public static PExpenseDisbursementAttachmentId New() => From(Guid.CreateVersion7());
}

public class PExpenseDisbursementAttachment : AuditableEntity<PExpenseDisbursementAttachmentId>
{
    public override PExpenseDisbursementAttachmentId Id { get; init; }

    public ParameterCode DocumentTypeCode { get; private set; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public bool IsPublic { get; private set; }

    public int Sequence { get; private set; }

    public bool IsExpenseAttachment { get; private set; }

    public virtual SuParameter DocumentType { get; init; }

    public static PExpenseDisbursementAttachment Create(
        ParameterCode documentTypeCode,
        FileId fileId,
        string fileName,
        int sequence,
        bool isPublic)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        return new PExpenseDisbursementAttachment
        {
            Id = PExpenseDisbursementAttachmentId.New(),
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            FileId = fileId,
            FileName = fileName,
            IsPublic = isPublic,
            IsExpenseAttachment = true,
        };
    }

    public PExpenseDisbursementAttachment SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public PExpenseDisbursementAttachment SetIsPublic(bool isPublic)
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