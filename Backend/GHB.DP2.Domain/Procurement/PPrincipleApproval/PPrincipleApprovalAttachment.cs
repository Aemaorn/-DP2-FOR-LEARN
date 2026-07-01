namespace GHB.DP2.Domain.Procurement.PPrincipleApproval;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct PPrincipleApprovalAttachmentId
{
    public static PPrincipleApprovalAttachmentId New() => From(Guid.CreateVersion7());
}

public class PPrincipleApprovalAttachment : AuditableEntity<PPrincipleApprovalAttachmentId>
{
    public override PPrincipleApprovalAttachmentId Id { get; init; }

    public PPrincipleApprovalId PPrincipleApprovalId { get; private init; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public int Sequence { get; private set; }

    public static PPrincipleApprovalAttachment Create(
        FileId fileId,
        string fileName,
        int sequence)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        return new PPrincipleApprovalAttachment
        {
            Id = PPrincipleApprovalAttachmentId.New(),
            Sequence = sequence,
            FileId = fileId,
            FileName = fileName,
        };
    }

    public PPrincipleApprovalAttachment SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public PPrincipleApprovalAttachment Clone()
    {
        return new PPrincipleApprovalAttachment
        {
            Id = PPrincipleApprovalAttachmentId.New(),
            Sequence = this.Sequence,
            FileId = this.FileId,
            FileName = this.FileName,
        };
    }
}