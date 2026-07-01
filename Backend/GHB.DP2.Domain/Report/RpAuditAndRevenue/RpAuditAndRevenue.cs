namespace GHB.DP2.Domain.Report.RpAuditAndRevenue;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using LanguageExt;
using GHB.DP2.Domain.SystemUtility;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct RpAuditAndRevenueId
{
    public static RpAuditAndRevenueId New() => From(Guid.CreateVersion7());
}

public enum RpAuditAndRevenueStatus
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

public partial class RpAuditAndRevenue : AuditableEntity<RpAuditAndRevenueId>, IHasSoftDelete, IHasActivityInfo
{
    public override RpAuditAndRevenueId Id { get; init; }

    public RpAuditAndRevenueStatus Status { get; private set; }

    public string DocumentNumber { get; private set; }

    public DateTimeOffset DocumentDate { get; private set; }

    public DateTimeOffset SignStartDate { get; private set; }

    public DateTimeOffset SignEndDate { get; private set; }

    public DateTimeOffset DeliveryDate { get; private set; }

    public virtual ICollection<RpAuditAndRevenueDetail> Details { get; set; }

    public virtual ICollection<RpAuditRevenueAcceptor> Acceptors { get; set; }

    public virtual IReadOnlyCollection<RpAuditAndRevenueAttachment> Attachments { get; private set; }

    public virtual IReadOnlyCollection<RpAuditAndRevenueDocumentHistory> DocumentHistories { get; private set; }

    public RpAuditAndRevenueDocumentHistory? LastAuditReportDocumentHistory =>
        this.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.AuditReport)
            .OrderVersions()
            .FirstOrDefault();

    public RpAuditAndRevenueDocumentHistory? LastAuditGeneralReportDocumentHistory =>
        this.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.AuditGeneralReport)
            .OrderVersions()
            .FirstOrDefault();

    public RpAuditAndRevenueDocumentHistory? LastRevenueReportDocumentHistory =>
        this.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.RevenueReport)
            .OrderVersions()
            .FirstOrDefault();

    public RpAuditAndRevenueDocumentHistory? LastDocumentByType(RpAuditAndRevenueDocumentType documentType) =>
        documentType switch
        {
            RpAuditAndRevenueDocumentType.AuditReport => this.LastAuditReportDocumentHistory,
            RpAuditAndRevenueDocumentType.AuditGeneralReport => this.LastAuditGeneralReportDocumentHistory,
            RpAuditAndRevenueDocumentType.RevenueReport => this.LastRevenueReportDocumentHistory,
            _ => null,
        };

    public RpAuditAndRevenueDocumentHistory? LastDraftAuditReportDocumentHistory =>
        this.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.AuditReport)
            .Where(d => d.StatusState == RpAuditAndRevenueStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public RpAuditAndRevenueDocumentHistory? LastDraftAuditGeneralReportDocumentHistory =>
        this.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.AuditGeneralReport)
            .Where(d => d.StatusState == RpAuditAndRevenueStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public RpAuditAndRevenueDocumentHistory? LastDraftRevenueReportDocumentHistory =>
        this.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.RevenueReport)
            .Where(d => d.StatusState == RpAuditAndRevenueStatus.Draft)
            .OrderVersions()
            .FirstOrDefault();

    public RpAuditAndRevenueDocumentHistory? LastWaitingApprovalAuditReportDocumentHistory =>
        this.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.AuditReport)
            .Where(d => d.StatusState == RpAuditAndRevenueStatus.WaitingApproval)
            .OrderVersions()
            .FirstOrDefault();

    public RpAuditAndRevenueDocumentHistory? LastWaitingApprovalAuditGeneralReportDocumentHistory =>
        this.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.AuditGeneralReport)
            .Where(d => d.StatusState == RpAuditAndRevenueStatus.WaitingApproval)
            .OrderVersions()
            .FirstOrDefault();

    public RpAuditAndRevenueDocumentHistory? LastWaitingApprovalRevenueReportDocumentHistory =>
        this.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.RevenueReport)
            .Where(d => d.StatusState == RpAuditAndRevenueStatus.WaitingApproval)
            .OrderVersions()
            .FirstOrDefault();

    public RpAuditAndRevenueDocumentHistory? LastWaitingApprovalAuditReportDocumentHistoryNotReplaced =>
        this.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.AuditReport)
            .Where(d => d.StatusState == RpAuditAndRevenueStatus.WaitingApproval)
            .Where(d => !d.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public RpAuditAndRevenueDocumentHistory? LastWaitingApprovalAuditGeneralReportDocumentHistoryNotReplaced =>
        this.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.AuditGeneralReport)
            .Where(d => d.StatusState == RpAuditAndRevenueStatus.WaitingApproval)
            .Where(d => !d.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public RpAuditAndRevenueDocumentHistory? LastWaitingApprovalRevenueReportDocumentHistoryNotReplaced =>
        this.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.RevenueReport)
            .Where(d => d.StatusState == RpAuditAndRevenueStatus.WaitingApproval)
            .Where(d => !d.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public RpAuditAndRevenueDocumentHistory? LastWaitingApprovalDocumentByTypeNotReplaced(RpAuditAndRevenueDocumentType documentType) =>
        documentType switch
        {
            RpAuditAndRevenueDocumentType.AuditReport => this.LastWaitingApprovalAuditReportDocumentHistoryNotReplaced,
            RpAuditAndRevenueDocumentType.AuditGeneralReport => this.LastWaitingApprovalAuditGeneralReportDocumentHistoryNotReplaced,
            RpAuditAndRevenueDocumentType.RevenueReport => this.LastWaitingApprovalRevenueReportDocumentHistoryNotReplaced,
            _ => null,
        };

    public RpAuditAndRevenueDocumentHistory? LastWaitingApprovalAuditReportDocumentHistoryReplaced =>
        this.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.AuditReport)
            .Where(d => d.StatusState == RpAuditAndRevenueStatus.WaitingApproval)
            .Where(d => d.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public RpAuditAndRevenueDocumentHistory? LastWaitingApprovalAuditGeneralReportDocumentHistoryReplaced =>
        this.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.AuditGeneralReport)
            .Where(d => d.StatusState == RpAuditAndRevenueStatus.WaitingApproval)
            .Where(d => d.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public RpAuditAndRevenueDocumentHistory? LastWaitingApprovalRevenueReportDocumentHistoryReplaced =>
        this.DocumentHistories
            .Where(d => d.DocumentType == RpAuditAndRevenueDocumentType.RevenueReport)
            .Where(d => d.StatusState == RpAuditAndRevenueStatus.WaitingApproval)
            .Where(d => d.IsReplaced)
            .OrderVersions()
            .FirstOrDefault();

    public RpAuditAndRevenueDocumentHistory? LastWaitingApprovalDocumentByTypeReplaced(RpAuditAndRevenueDocumentType documentType) =>
        documentType switch
        {
            RpAuditAndRevenueDocumentType.AuditReport => this.LastWaitingApprovalAuditReportDocumentHistoryReplaced,
            RpAuditAndRevenueDocumentType.AuditGeneralReport => this.LastWaitingApprovalAuditGeneralReportDocumentHistoryReplaced,
            RpAuditAndRevenueDocumentType.RevenueReport => this.LastWaitingApprovalRevenueReportDocumentHistoryReplaced,
            _ => null,
        };

    public RpAuditAndRevenueDocumentHistory? FirstWaitingApprovalNotReplacedInLatestMajor(RpAuditAndRevenueDocumentType documentType)
    {
        var waDocuments = this.DocumentHistories
            .Where(d => d.DocumentType == documentType)
            .Where(d => d.StatusState == RpAuditAndRevenueStatus.WaitingApproval)
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

    public Unit AddDocumentHistory(
        RpAuditAndRevenueDocumentType documentType,
        FileId fileId,
        bool isReplace,
        bool incrementMajor = false)
    {
        var histories = this.DocumentHistories.ToHashSet();

        var existingStatus =
            histories
                .Any(p => p.StatusState == this.Status && p.DocumentType == documentType);

        var version = this.DocumentHistories
                          .Where(p => p.DocumentType == documentType)
                          .NextVersion(incrementMajor || !existingStatus);

        histories.Add(
            RpAuditAndRevenueDocumentHistory.Create(
                documentType,
                this.Status,
                version,
                fileId,
                isReplace));

        this.DocumentHistories = histories;

        return unit;
    }

    public static RpAuditAndRevenue Create()
    {
        var newData = new RpAuditAndRevenue
        {
            Id = RpAuditAndRevenueId.New(),
            Status = RpAuditAndRevenueStatus.Draft,
            DocumentHistories = [],
            Acceptors = [],
            Details = [],
            Attachments = [],
        };

        newData.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.Create,
                string.Empty,
                newData.Status.ToString()));

        return newData;
    }

    public RpAuditAndRevenue SetValues(
        string documentNumber,
        DateTimeOffset documentDate,
        DateTimeOffset signStartDate,
        DateTimeOffset signEndDate,
        DateTimeOffset deliveryDate)
    {
        this.DocumentNumber = documentNumber;
        this.DocumentDate = documentDate;
        this.SignStartDate = signStartDate;
        this.SignEndDate = signEndDate;
        this.DeliveryDate = deliveryDate;

        return this;
    }

    public RpAuditAndRevenue SetStatus(RpAuditAndRevenueStatus status, string? remark = null)
    {
        this.Status = status;

        switch (status, this.Status)
        {
            case (RpAuditAndRevenueStatus.WaitingApproval, _):
                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.SendApprove,
                        string.Empty,
                        RpAuditAndRevenueStatus.WaitingApproval.ToString()));

                break;

            case (RpAuditAndRevenueStatus.Edit, _):
                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Recall,
                        string.Empty,
                        RpAuditAndRevenueStatus.Edit.ToString()));

                break;

            case (RpAuditAndRevenueStatus.Rejected, _):
                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Reject,
                        string.Empty,
                        RpAuditAndRevenueStatus.Rejected.ToString(),
                        remark));

                break;

            case (RpAuditAndRevenueStatus.Approved, _):
                this.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.Approved,
                        string.Empty,
                        RpAuditAndRevenueStatus.Approved.ToString(),
                        remark));

                break;
        }

        return this;
    }

    public RpAuditAndRevenue AddAcceptor(RpAuditRevenueAcceptor acceptor)
    {
        var acceptors = this.Acceptors?.ToList() ?? new List<RpAuditRevenueAcceptor>();
        acceptors.Add(acceptor);
        this.Acceptors = acceptors;

        return this;
    }

    public RpAuditAndRevenue RemoveAcceptor(RpAuditRevenueAcceptor acceptor)
    {
        var list = this.Acceptors?.ToList() ?? new List<RpAuditRevenueAcceptor>();
        list.Remove(acceptor);
        this.Acceptors = list;

        return this;
    }

    public RpAuditAndRevenue AddDetail(RpAuditAndRevenueDetail acceptor)
    {
        var acceptors = this.Details?.ToList() ?? new List<RpAuditAndRevenueDetail>();
        acceptors.Add(acceptor);
        this.Details = acceptors;

        return this;
    }

    public RpAuditAndRevenue RemoveDetail(RpAuditAndRevenueDetail acceptor)
    {
        var list = this.Details?.ToList() ?? new List<RpAuditAndRevenueDetail>();
        list.Remove(acceptor);
        this.Details = list;

        return this;
    }

    public RpAuditAndRevenue AddAttachment(RpAuditAndRevenueAttachment attachment)
    {
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

    public RpAuditAndRevenue RemoveAttachment(RpAuditAndRevenueAttachment attachment)
    {
        var list = this.Attachments.ToHashSet();
        list.Remove(attachment);
        this.Attachments = list;

        return this;
    }
}

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct RpAuditAndRevenueAttachmentId
{
    public static RpAuditAndRevenueAttachmentId New() => From(Guid.CreateVersion7());
}

public class RpAuditAndRevenueAttachment : AuditableEntity<RpAuditAndRevenueAttachmentId>
{
    public ParameterCode DocumentTypeCode { get; private set; }

    public override RpAuditAndRevenueAttachmentId Id { get; init; }

    public FileId FileId { get; private init; }

    public string FileName { get; private init; }

    public bool IsPublic { get; private set; }

    public int Sequence { get; private set; }

    public virtual SuParameter DocumentType { get; init; }

    public static RpAuditAndRevenueAttachment Create(
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

        return new RpAuditAndRevenueAttachment
        {
            Id = RpAuditAndRevenueAttachmentId.New(),
            Sequence = sequence,
            DocumentTypeCode = documentTypeCode,
            FileId = fileId,
            FileName = fileName,
            IsPublic = isPublic,
        };
    }

    public RpAuditAndRevenueAttachment SetSequence(int sequence)
    {
        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a non-negative integer.");
        }

        this.Sequence = sequence;

        return this;
    }

    public RpAuditAndRevenueAttachment SetIsPublic(bool isPublic)
    {
        this.IsPublic = isPublic;

        return this;
    }

    public Unit SetDocumentType(ParameterCode documentTypeCode)
    {
        this.DocumentTypeCode = documentTypeCode;

        return unit;
    }

    public RpAuditAndRevenueAttachment Clone()
    {
        return new RpAuditAndRevenueAttachment
        {
            Id = RpAuditAndRevenueAttachmentId.New(),
            Sequence = this.Sequence,
            DocumentTypeCode = this.DocumentTypeCode,
            FileId = this.FileId,
            FileName = this.FileName,
            IsPublic = this.IsPublic,
        };
    }
}