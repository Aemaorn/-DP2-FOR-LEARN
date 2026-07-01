namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft.Dto;

using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.SystemUtility;

public sealed class Attachment
{
    public Guid? Id { get; init; }

    public string TypeCode { get; init; }

    public string? Description { get; init; }

    public int? PageNumber { get; init; }

    public int Sequence { get; init; }

    public string? FormatOtherName { get; init; }

    public string? TypeLabel { get; init; }

    public AttachmentFile[] Files { get; init; }

    public CaContractDraftVendorsAttachment MapToEntity()
    {
        var attachment =
            Optional(this.Id)
                .Map(id => CaContractDraftVendorsAttachment.Create(
                    id,
                    ParameterCode.From(this.TypeCode),
                    this.Description,
                    this.PageNumber,
                    this.Sequence,
                    this.FormatOtherName))
                .IfNone(CaContractDraftVendorsAttachment.Create(
                    ParameterCode.From(this.TypeCode),
                    this.Description,
                    this.PageNumber,
                    this.Sequence,
                    this.FormatOtherName));

        _ = this.Files
                .Map(file => file.MapToEntity())
                .Map(attachment.AddFile)
                .ToHashSet();

        return attachment;
    }

    public static Attachment FromEntity(
        CaContractDraftVendorsAttachment attachment)
    {
        return new Attachment
        {
            Id = attachment.Id.Value,
            TypeCode = attachment.TypeCode.Value,
            Description = attachment.Description,
            PageNumber = attachment.PageNumber,
            Sequence = attachment.Sequence,
            FormatOtherName = attachment.FormatOtherName,
            TypeLabel = attachment.Type?.Label ?? string.Empty,
            Files = [.. attachment.Files.Map(AttachmentFile.FromEntity).OrderBy(x => x.Sequence)],
        };
    }
}

public sealed class AttachmentFile
{
    public Guid FileId { get; init; }

    public string FileName { get; init; }

    public string FileType { get; init; }

    public int Sequence { get; init; }

    public CaContractDraftVendorsAttachmentFile MapToEntity()
    {
        return CaContractDraftVendorsAttachmentFile.Create(
            this.FileId,
            this.FileName,
            this.FileType,
            this.Sequence);
    }

    public static AttachmentFile FromEntity(
        CaContractDraftVendorsAttachmentFile file)
    {
        return new AttachmentFile
        {
            FileId = file.Id.Value,
            FileName = file.FileName,
            FileType = file.FileType,
            Sequence = file.Sequence,
        };
    }
}