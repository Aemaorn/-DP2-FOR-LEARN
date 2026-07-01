namespace GHB.DP2.Application.Dtos;

using System.ComponentModel;
using GHB.DP2.Application.Contracts;

public record AttachmentsDto(
    [property: Description("รหัสประเภทเอกสาร")]
    string DocumentTypeCode,
    [property: Description("เอกสารแนบ")]
    FileAttachments[] FileAttachments);

public record FileAttachments(
    [property: Description("รหัสไฟล์")]
    Guid FileId,
    [property: Description("ชื่อไฟล์")]
    string FileName,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("เปิดเผยสาธารณะ")]
    bool IsPublic,
    [property: Description("ผู้อัปโหลด")]
    Guid CreatedBy);

public record AttachmentsDtoWithId(
    [property: Description("รหัสประเภทเอกสาร")]
    string DocumentTypeCode,
    [property: Description("เอกสารแนบ")]
    FileAttachmentsWithId[] FileAttachments) : IHasDocumentTypeAttachments;

public record AttachmentsWithOutTypeDto(
    [property: Description("เอกสารแนบ")]
    IEnumerable<FileAttachmentsWithId> FileAttachments);

public record FileAttachmentsWithId(
    [property: Description("รหัสข้อมูล")] Guid? Id,
    [property: Description("รหัสไฟล์")] Guid FileId,
    [property: Description("ชื่อไฟล์")] string FileName,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("เปิดเผยสาธารณะ")]
    bool IsPublic,
    [property: Description("ผู้อัปโหลด")] Guid CreatedBy) : IHasSequenceFileAttachment;

public record ComparingAttachmentsDto(
    [property: Description("รหัสข้อมูล")] Guid? Id,
    [property: Description("รหัสไฟล์")] Guid FileId,
    [property: Description("ชื่อไฟล์")] string FileName,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("เปิดเผยสาธารณะ")]
    bool IsPublic);
