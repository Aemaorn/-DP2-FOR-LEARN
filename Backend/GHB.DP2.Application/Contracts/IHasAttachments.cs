namespace GHB.DP2.Application.Contracts;

using GHB.DP2.Application.Dtos;

public interface IHasAttachmentsWithId
{
    AttachmentsDtoWithId[] Attachments { get; }
}

public interface IHasDocumentTypeAttachments
{
    string DocumentTypeCode { get; }

    FileAttachmentsWithId[] FileAttachments { get; }
}

public interface IHasSequenceFileAttachment
{
    Guid FileId { get; }

    string FileName { get; }

    int Sequence { get; }
}