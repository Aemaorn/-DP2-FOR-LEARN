namespace GHB.DP2.Domain.Report.RpContractCompletionByQuarter;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct RpContractCompletionByQuarterId
{
    public static RpContractCompletionByQuarterId New() => From(Guid.CreateVersion7());
}

public enum RpContractCompletionByQuarterStatus
{
    /// <summary>
    /// แบบร่าง
    /// </summary>
    Draft,

    /// <summary>
    /// เรียกคืนแก้ไข
    /// </summary>
    Edit,

    /// <summary>
    /// รออนุมัติ
    /// </summary>
    WaitingApproval,

    /// <summary>
    /// อนุมัติ
    /// </summary>
    Approved,

    /// <summary>
    /// ส่งกลับแก้ไข
    /// </summary>
    Rejected,
}

public partial class RpContractCompletionByQuarter : AuditableEntity<RpContractCompletionByQuarterId>, IHasSoftDelete, IHasActivityInfo
{
    public override RpContractCompletionByQuarterId Id { get; init; }

    public RpContractCompletionByQuarterStatus Status { get; private set; }

    public string DocumentNumber { get; private set; }

    public DateTimeOffset DocumentDate { get; private set; }

    public int Year { get; private set; }

    public int Quarter { get; private set; }

    public DateTimeOffset SignStartDate { get; private set; }

    public DateTimeOffset SignEndDate { get; private set; }

    public FileId? DocumentId { get; init; }

    public virtual ICollection<RpContractCompletionByQuarterDetail> Details { get; set; }

    public virtual ICollection<RpContractCompletionByQuarterAcceptor> Acceptors { get; set; }

    public virtual IReadOnlyCollection<RpContractCompletionByQuarterDocumentHistory> DocumentHistories { get; private set; }

    public virtual IReadOnlyCollection<RpContractCompletionByQuarterAttachment> Attachments { get; private set; }

    public static RpContractCompletionByQuarter Create()
    {
        var report = new RpContractCompletionByQuarter
        {
            Id = RpContractCompletionByQuarterId.New(),
            DocumentHistories = [],
            Attachments = [],
        };

        report.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Create,
                $"สร้างข้อมูลสัญญาแล้วเสร็จตามไตรมาส",
                nameof(RpContractCompletionByQuarterStatus.Draft)));

        return report;
    }

    public RpContractCompletionByQuarter SetValues(
        string documentNumber,
        int year,
        int quarter,
        DateTimeOffset documentDate,
        DateTimeOffset signStartDate,
        DateTimeOffset signEndDate)
    {
        this.DocumentNumber = documentNumber;
        this.DocumentDate = documentDate;
        this.Year = year;
        this.Quarter = quarter;
        this.SignStartDate = signStartDate;
        this.SignEndDate = signEndDate;

        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                $"อัปเดตข้อมูลสัญญาแล้วเสร็จตามไตรมาส",
                this.Status.ToString()));

        return this;
    }

    public RpContractCompletionByQuarter SetStatus(RpContractCompletionByQuarterStatus status)
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                $"อัปเดตข้อมูลสัญญาแล้วเสร็จตามไตรมาส",
                status.ToString()));

        this.Status = status;

        return this;
    }

    public RpContractCompletionByQuarter AddAcceptor(RpContractCompletionByQuarterAcceptor acceptor)
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                $"อัปเดตข้อมูล ผู้มีอำนาจเห็นชอบ/อนุมัติ",
                this.Status.ToString()));

        var acceptors = this.Acceptors?.ToList() ?? new List<RpContractCompletionByQuarterAcceptor>();
        acceptors.Add(acceptor);
        this.Acceptors = acceptors;

        return this;
    }

    public RpContractCompletionByQuarter RemoveAcceptor(RpContractCompletionByQuarterAcceptor acceptor)
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Delete,
                $"ลบข้อมูล ผู้มีอำนาจเห็นชอบ/อนุมัติ",
                this.Status.ToString()));

        var list = this.Acceptors?.ToList() ?? new List<RpContractCompletionByQuarterAcceptor>();
        list.Remove(acceptor);
        this.Acceptors = list;

        return this;
    }

    public RpContractCompletionByQuarter AddDetail(RpContractCompletionByQuarterDetail acceptor)
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                $"อัปเดตข้อมูล รายการสัญญา",
                this.Status.ToString()));

        var acceptors = this.Details?.ToList() ?? new List<RpContractCompletionByQuarterDetail>();
        acceptors.Add(acceptor);
        this.Details = acceptors;

        return this;
    }

    public RpContractCompletionByQuarter RemoveDetail(RpContractCompletionByQuarterDetail acceptor)
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Delete,
                $"ลบข้อมูล รายการสัญญาิ",
                this.Status.ToString()));

        var list = this.Details?.ToList() ?? new List<RpContractCompletionByQuarterDetail>();
        list.Remove(acceptor);
        this.Details = list;

        return this;
    }

    public RpContractCompletionByQuarterDocumentHistory? LastedDraftDocument(RpContractCompletionByQuarterDocumentType documentType) =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == documentType)
            .Where(dh => dh.StatusState == RpContractCompletionByQuarterStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public RpContractCompletionByQuarterDocumentHistory? LastedNotReplacedDocument(RpContractCompletionByQuarterDocumentType documentType) =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == documentType)
            .Where(dh => dh is
            {
                StatusState: RpContractCompletionByQuarterStatus.WaitingApproval,
                IsReplaced: false,
            })
            .OrderVersions()
            .FirstOrDefault();

    public Unit AddDocumentHistory(
        RpContractCompletionByQuarterDocumentType documentType,
        FileId fileId,
        bool isReplace = false)
    {
        var histories = this.DocumentHistories.ToHashSet();

        var existingHistory =
            histories
                .Where(dh => dh.DocumentType == documentType)
                .OrderVersions()
                .FirstOrDefault();

        var isIncreaseMajorVersion =
            existingHistory is null ||
            existingHistory.StatusState != this.Status;

        var version =
            this.DocumentHistories
                .Where(dh => dh.DocumentType == documentType)
                .NextVersion(isIncreaseMajorVersion);

        histories.Add(
            RpContractCompletionByQuarterDocumentHistory.Create(
                documentType,
                this.Status,
                version,
                fileId,
                isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public Unit AddDocumentHistory(
        FileId fileId,
        bool isReplace,
        bool incrementMajor)
    {
        var histories = this.DocumentHistories.ToHashSet();

        var existingStatus =
            histories
                .Any(p => p.StatusState == this.Status && p.DocumentType == RpContractCompletionByQuarterDocumentType.Completion);

        var version = this.DocumentHistories
                          .Where(p => p.DocumentType == RpContractCompletionByQuarterDocumentType.Completion)
                          .NextVersion(incrementMajor || !existingStatus);

        histories.Add(
            RpContractCompletionByQuarterDocumentHistory.Create(
                RpContractCompletionByQuarterDocumentType.Completion,
                this.Status,
                version,
                fileId,
                isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public RpContractCompletionByQuarterDocumentHistory? LastDocument() =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == RpContractCompletionByQuarterDocumentType.Completion)
            .OrderVersions()
            .FirstOrDefault();

    public RpContractCompletionByQuarterDocumentHistory? LastWaitingApprovalDocumentReplaced() =>
        this.DocumentHistories
            .Where(dh => dh.DocumentType == RpContractCompletionByQuarterDocumentType.Completion)
            .Where(dh => dh is
            {
                StatusState: RpContractCompletionByQuarterStatus.WaitingApproval,
                IsReplaced: true,
            })
            .OrderVersions()
            .FirstOrDefault();

    public RpContractCompletionByQuarterDocumentHistory? FirstWaitingApprovalNotReplacedInLatestMajor()
    {
        var waDocuments = this.DocumentHistories
            .Where(d => d.DocumentType == RpContractCompletionByQuarterDocumentType.Completion)
            .Where(d => d.StatusState == RpContractCompletionByQuarterStatus.WaitingApproval)
            .ToList();

        if (!waDocuments.Any())
        {
            return null;
        }

        var latestMajor = waDocuments
            .Select(d => int.Parse(d.Version.Split('.')[0]))
            .Max();

        return waDocuments
            .Where(d => d.Version.StartsWith($"{latestMajor}."))
            .Where(d => !d.IsReplaced)
            .OrderBy(d => d.CreatedAt)
            .FirstOrDefault();
    }

    public RpContractCompletionByQuarter AddAttachment(RpContractCompletionByQuarterAttachment attachment)
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                $"อัปเดตเอกสารแนบ",
                this.Status.ToString()));

        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment), "Attachment cannot be null.");
        }

        if (this.Attachments.Contains(attachment))
        {
            throw new InvalidOperationException("Attachment already exists in the plan.");
        }

        var attachments = this.Attachments.ToHashSet();

        attachments.Add(attachment);

        this.Attachments = attachments;

        return this;
    }

    public RpContractCompletionByQuarter RemoveAttachment(RpContractCompletionByQuarterAttachment attachment)
    {
        this.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Delete,
                $"ลบเอกสารแนบ",
                this.Status.ToString()));

        var list = this.Attachments.ToHashSet();
        list.Remove(attachment);
        this.Attachments = list;

        return this;
    }
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct RpContractCompletionByQuarterAttachmentId
{
    public static RpContractCompletionByQuarterAttachmentId New() => From(Guid.CreateVersion7());
}

public class RpContractCompletionByQuarterAttachment : AuditableEntity<RpContractCompletionByQuarterAttachmentId>
{
    public ParameterCode DocumentTypeCode { get; private set; }

    public override RpContractCompletionByQuarterAttachmentId Id { get; init; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public bool IsPublic { get; private set; }

    public int Sequence { get; private set; }

    public virtual SuParameter DocumentType { get; init; }

    public static RpContractCompletionByQuarterAttachment Create(
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

        return new RpContractCompletionByQuarterAttachment
        {
            Id = RpContractCompletionByQuarterAttachmentId.New(),
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            FileId = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public RpContractCompletionByQuarterAttachment SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public RpContractCompletionByQuarterAttachment SetIsPublic(bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }

    public Unit SetDocumentType(ParameterCode documentTypeCode)
    {
        this.DocumentTypeCode = documentTypeCode;

        return unit;
    }

    public RpContractCompletionByQuarterAttachment Clone()
    {
        return new RpContractCompletionByQuarterAttachment
        {
            Id = RpContractCompletionByQuarterAttachmentId.New(),
            Sequence = this.Sequence,
            DocumentTypeCode = this.DocumentTypeCode,
            FileId = this.FileId,
            FileName = this.FileName,
            IsPublic = this.IsPublic,
        };
    }
}